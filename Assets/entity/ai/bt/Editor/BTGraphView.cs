#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Starfire.Entity.AI.BT.Editor
{
    /// <summary>
    /// GraphView for visual behavior tree editing.
    /// </summary>
    public class BTGraphView : GraphView
    {
        public event Action<BTNodeView> OnNodeSelected;

        private BehaviorTreeEditorWindow _editorWindow;
        private BehaviorTreeAsset _treeAsset;

        public BTGraphView(BehaviorTreeEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;

            // Add grid background
            Insert(0, new GridBackground());

            // Add manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Load stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/entity/ai/bt/Editor/BehaviorTreeEditor.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Register callbacks
            graphViewChanged += OnGraphViewChanged;
        }

        public void PopulateView(BehaviorTreeAsset treeAsset)
        {
            _treeAsset = treeAsset;

            // Clear existing content
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (_treeAsset == null) return;

            // Create node views for each node
            var nodeViewMap = new Dictionary<string, BTNodeView>();
            foreach (var nodeData in _treeAsset.Nodes)
            {
                var nodeView = CreateNodeView(nodeData);
                nodeViewMap[nodeData.id] = nodeView;
            }

            // Create edges for connections
            foreach (var connection in _treeAsset.Connections)
            {
                if (nodeViewMap.TryGetValue(connection.parentId, out var parentView) &&
                    nodeViewMap.TryGetValue(connection.childId, out var childView))
                {
                    var edge = parentView.OutputPort.ConnectTo(childView.InputPort);
                    AddElement(edge);
                }
            }
        }

        private BTNodeView CreateNodeView(BTNodeData nodeData)
        {
            var nodeView = new BTNodeView(nodeData);
            nodeView.OnNodeSelected = OnNodeSelected;
            nodeView.OnNodeDoubleClicked = OnNodeDoubleClicked;
            nodeView.SetPosition(new Rect(nodeData.editorPosition, Vector2.zero));
            AddElement(nodeView);
            return nodeView;
        }

        private void OnNodeDoubleClicked(BTNodeView nodeView, Vector2 graphPosition)
        {
            if (_treeAsset == null || nodeView?.NodeData?.parameters == null) return;

            // Convert graph position to screen position
            var windowPosition = _editorWindow.position;
            var screenPos = new Rect(
                windowPosition.x + graphPosition.x,
                windowPosition.y + graphPosition.y + 50, // Offset below node
                0, 0);

            // Show popup
            var popup = new BTNodePopup(nodeView, _treeAsset);
            UnityEditor.PopupWindow.Show(screenPos, popup);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_treeAsset == null) return graphViewChange;

            // Handle removed elements
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is BTNodeView nodeView)
                    {
                        _treeAsset.RemoveNode(nodeView.NodeData.id);
                    }
                    else if (element is Edge edge)
                    {
                        var parentView = edge.output.node as BTNodeView;
                        var childView = edge.input.node as BTNodeView;
                        if (parentView != null && childView != null)
                        {
                            _treeAsset.RemoveConnection(parentView.NodeData.id, childView.NodeData.id);
                        }
                    }
                }
            }

            // Handle moved elements
            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is BTNodeView nodeView)
                    {
                        nodeView.NodeData.editorPosition = element.GetPosition().position;
                        EditorUtility.SetDirty(_treeAsset);
                    }
                }
            }

            // Handle new edges
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var parentView = edge.output.node as BTNodeView;
                    var childView = edge.input.node as BTNodeView;
                    if (parentView != null && childView != null)
                    {
                        _treeAsset.AddConnection(parentView.NodeData.id, childView.NodeData.id);
                    }
                }
            }

            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node).ToList();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_treeAsset == null) return;

            var mousePosition = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            // Composites
            evt.menu.AppendAction("Add Composite/Selector", _ => CreateNode(BTNodeType.Selector, mousePosition));
            evt.menu.AppendAction("Add Composite/Sequence", _ => CreateNode(BTNodeType.Sequence, mousePosition));
            evt.menu.AppendAction("Add Composite/Parallel", _ => CreateNode(BTNodeType.Parallel, mousePosition));

            // Decorators
            evt.menu.AppendAction("Add Decorator/Repeater", _ => CreateNode(BTNodeType.Repeater, mousePosition));
            evt.menu.AppendAction("Add Decorator/Guarded Repeater", _ => CreateNode(BTNodeType.GuardedRepeater, mousePosition));

            // Subtree
            evt.menu.AppendAction("Add Subtree", _ => CreateNode(BTNodeType.Subtree, mousePosition));

            // Actions - organized by entity type and category
            evt.menu.AppendSeparator();
            foreach (var entityType in BTActionRegistry.GetEntityTypes())
            {
                foreach (var categoryGroup in BTActionRegistry.GetActionsByEntityType(entityType))
                {
                    foreach (var actionInfo in categoryGroup.OrderBy(a => a.Name))
                    {
                        var actionName = actionInfo.Name; // Capture for closure
                        evt.menu.AppendAction(
                            $"Add Action/{entityType}/{categoryGroup.Key}/{actionName}",
                            _ => CreateActionNode(actionName, mousePosition));
                    }
                }
            }
        }

        private void CreateNode(BTNodeType nodeType, Vector2 position)
        {
            var nodeData = _treeAsset.AddNode(nodeType, position);
            CreateNodeView(nodeData);
            EditorUtility.SetDirty(_treeAsset);
        }

        private void CreateActionNode(string actionType, Vector2 position)
        {
            var nodeData = _treeAsset.AddActionNode(actionType, position);
            CreateNodeView(nodeData);
            EditorUtility.SetDirty(_treeAsset);
        }
    }
}
#endif
