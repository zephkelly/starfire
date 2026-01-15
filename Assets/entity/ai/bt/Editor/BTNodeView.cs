#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Starfire.Entity.AI.BT.Editor
{
    /// <summary>
    /// Visual representation of a behavior tree node in the graph editor.
    /// </summary>
    public class BTNodeView : Node
    {
        public BTNodeData NodeData { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public Action<BTNodeView> OnNodeSelected;
        public Action<BTNodeView, Vector2> OnNodeDoubleClicked;

        private double _lastClickTime;
        private const double DoubleClickThreshold = 0.3;

        public BTNodeView(BTNodeData nodeData)
        {
            NodeData = nodeData;
            title = GetNodeTitle();
            viewDataKey = nodeData.id;

            // Set node style based on type
            ApplyNodeStyle();

            // Create ports
            CreatePorts();

            // Add description label
            AddDescriptionLabel();

            // Register double-click handler
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return; // Only left click

            double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
            if (currentTime - _lastClickTime < DoubleClickThreshold)
            {
                // Double-click detected - get screen position for popup
                var screenPos = evt.mousePosition + this.GetPosition().position;
                OnNodeDoubleClicked?.Invoke(this, screenPos);
                evt.StopPropagation();
            }
            _lastClickTime = currentTime;
        }

        private string GetNodeTitle()
        {
            if (NodeData.nodeType == BTNodeType.Action)
            {
                return NodeData.actionType;
            }
            if (NodeData.nodeType == BTNodeType.Subtree)
            {
                var sp = NodeData.parameters as SubtreeParameters;
                return sp?.subtreeAsset != null ? $"Subtree: {sp.subtreeAsset.name}" : "Subtree (unassigned)";
            }
            return NodeData.nodeType.ToString();
        }

        private void ApplyNodeStyle()
        {
            // Add CSS class based on type
            switch (NodeData.nodeType)
            {
                case BTNodeType.Selector:
                case BTNodeType.Sequence:
                    AddToClassList("composite-node");
                    // Blue tint for composites
                    style.backgroundColor = new Color(0.2f, 0.3f, 0.5f, 0.8f);
                    break;

                case BTNodeType.Repeater:
                    AddToClassList("decorator-node");
                    // Yellow tint for decorators
                    style.backgroundColor = new Color(0.5f, 0.45f, 0.2f, 0.8f);
                    break;

                case BTNodeType.GuardedRepeater:
                    AddToClassList("decorator-node");
                    // Orange tint for guarded repeater (interruptible)
                    style.backgroundColor = new Color(0.6f, 0.35f, 0.15f, 0.8f);
                    break;

                case BTNodeType.Action:
                    AddToClassList("action-node");
                    // Green tint for actions
                    style.backgroundColor = new Color(0.2f, 0.4f, 0.2f, 0.8f);
                    break;

                case BTNodeType.Subtree:
                    AddToClassList("subtree-node");
                    // Purple tint for subtrees
                    style.backgroundColor = new Color(0.4f, 0.2f, 0.5f, 0.8f);
                    break;
            }

            // Minimum width
            style.minWidth = 150;
        }

        private void CreatePorts()
        {
            // Input port (except for potential root nodes)
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
            InputPort.portName = "";
            InputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(InputPort);

            // Output port for composites and decorators (not for Action or Subtree)
            if (NodeData.nodeType != BTNodeType.Action && NodeData.nodeType != BTNodeType.Subtree)
            {
                // Repeater has single child, GuardedRepeater has 2 (guard + body), composites have multi
                var capacity = NodeData.nodeType == BTNodeType.Repeater
                    ? Port.Capacity.Single
                    : Port.Capacity.Multi;

                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, capacity, typeof(bool));
                OutputPort.portName = "";
                OutputPort.style.flexDirection = FlexDirection.ColumnReverse;
                outputContainer.Add(OutputPort);
            }
        }

        private void AddDescriptionLabel()
        {
            var descLabel = new Label();
            descLabel.AddToClassList("node-description");
            descLabel.style.fontSize = 10;
            descLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            descLabel.style.marginTop = 4;

            switch (NodeData.nodeType)
            {
                case BTNodeType.Selector:
                    descLabel.text = "OR (first success)";
                    break;
                case BTNodeType.Sequence:
                    descLabel.text = "AND (all succeed)";
                    break;
                case BTNodeType.Repeater:
                    var repeatParams = NodeData.parameters as RepeaterParameters;
                    var count = repeatParams?.repeatCount ?? -1;
                    descLabel.text = count < 0 ? "Loop forever" : $"Repeat {count}x";
                    break;
                case BTNodeType.GuardedRepeater:
                    var guardedParams = NodeData.parameters as GuardedRepeaterParameters;
                    var guardedCount = guardedParams?.repeatCount ?? -1;
                    descLabel.text = guardedCount < 0 ? "Guard→Body (infinite)" : $"Guard→Body ({guardedCount}x)";
                    break;
                case BTNodeType.Action:
                    descLabel.text = GetActionDescription();
                    break;

                case BTNodeType.Subtree:
                    var sp = NodeData.parameters as SubtreeParameters;
                    descLabel.text = sp?.subtreeAsset != null ? "External tree" : "No tree assigned";
                    break;
            }

            mainContainer.Add(descLabel);
        }

        private string GetActionDescription()
        {
            if (NodeData.parameters == null) return "";

            // Use reflection to find a relevant key field for display
            var type = NodeData.parameters.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            // Look for common key fields in priority order
            string[] priorityFields = { "targetKey", "waypointsKey", "entityKey", "forceKey", "outputKey" };
            foreach (var fieldName in priorityFields)
            {
                var field = System.Array.Find(fields, f => f.Name == fieldName);
                if (field != null)
                {
                    var value = field.GetValue(NodeData.parameters);
                    if (value != null)
                    {
                        return $"{fieldName}: {value}";
                    }
                }
            }

            // Fallback: show first string field if any
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var value = field.GetValue(NodeData.parameters) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return $"{field.Name}: {value}";
                    }
                }
            }

            return "";
        }

        public void UpdateVisuals()
        {
            // Update title
            title = GetNodeTitle();

            // Update root node indicator (use false to avoid stealing focus from popups)
            var window = UnityEditor.EditorWindow.GetWindow<BehaviorTreeEditorWindow>(false);
            bool isRoot = window.TreeAsset?.RootNodeId == NodeData.id;

            RemoveFromClassList("root-node");
            if (isRoot)
            {
                AddToClassList("root-node");
            }

            // Refresh description
            var descLabel = mainContainer.Q<Label>(className: "node-description");
            if (descLabel != null)
            {
                switch (NodeData.nodeType)
                {
                    case BTNodeType.Repeater:
                        var repeatParams = NodeData.parameters as RepeaterParameters;
                        var count = repeatParams?.repeatCount ?? -1;
                        descLabel.text = count < 0 ? "Loop forever" : $"Repeat {count}x";
                        break;
                    case BTNodeType.GuardedRepeater:
                        var guardedParams = NodeData.parameters as GuardedRepeaterParameters;
                        var guardedCount = guardedParams?.repeatCount ?? -1;
                        descLabel.text = guardedCount < 0 ? "Guard→Body (infinite)" : $"Guard→Body ({guardedCount}x)";
                        break;
                    case BTNodeType.Action:
                        descLabel.text = GetActionDescription();
                        break;
                    case BTNodeType.Subtree:
                        var sp = NodeData.parameters as SubtreeParameters;
                        descLabel.text = sp?.subtreeAsset != null ? "External tree" : "No tree assigned";
                        break;
                }
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            NodeData.editorPosition = new Vector2(newPos.x, newPos.y);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }
    }
}
#endif
