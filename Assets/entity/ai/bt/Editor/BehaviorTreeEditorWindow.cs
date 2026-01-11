#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Starfire.Entity.AI.BT.Editor
{
    /// <summary>
    /// Editor window for visual behavior tree editing.
    /// </summary>
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private BehaviorTreeAsset _treeAsset;
        private BTGraphView _graphView;
        private InspectorView _inspectorView;

        [MenuItem("Window/Starfire/Behavior Tree Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.titleContent = new GUIContent("Behavior Tree");
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Selection.activeObject is BehaviorTreeAsset)
            {
                OpenWindow();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                OnSelectionChange();
            }
        }

        private void CreateGUI()
        {
            // Load stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/entity/ai/bt/Editor/BehaviorTreeEditor.uss");

            // Create graph view
            _graphView = new BTGraphView(this);
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);

            // Create inspector panel
            _inspectorView = new InspectorView();
            _inspectorView.style.position = Position.Absolute;
            _inspectorView.style.right = 0;
            _inspectorView.style.top = 0;
            _inspectorView.style.width = 300;
            _inspectorView.style.height = Length.Percent(100);
            _inspectorView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            rootVisualElement.Add(_inspectorView);

            // Create toolbar
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveTree();
                }

                GUILayout.FlexibleSpace();

                if (_treeAsset != null)
                {
                    GUILayout.Label(_treeAsset.name, EditorStyles.boldLabel);
                }

                GUILayout.EndHorizontal();
            });
            toolbar.style.height = 20;
            rootVisualElement.Insert(0, toolbar);

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // Subscribe to node selection
            _graphView.OnNodeSelected += OnNodeSelected;

            // Load selected asset
            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            var asset = Selection.activeObject as BehaviorTreeAsset;
            if (asset != null && asset != _treeAsset)
            {
                _treeAsset = asset;
                _graphView?.PopulateView(_treeAsset);
                _inspectorView?.ClearSelection();
            }
        }

        private void OnNodeSelected(BTNodeView nodeView)
        {
            _inspectorView?.UpdateSelection(nodeView);
        }

        private void SaveTree()
        {
            if (_treeAsset == null) return;

            EditorUtility.SetDirty(_treeAsset);
            AssetDatabase.SaveAssets();
        }

        public BehaviorTreeAsset TreeAsset => _treeAsset;
    }

    /// <summary>
    /// Inspector panel for editing selected node properties.
    /// </summary>
    public class InspectorView : VisualElement
    {
        private UnityEditor.Editor _editor;
        private BTNodeView _selectedNode;
        private IMGUIContainer _container;

        public InspectorView()
        {
            style.paddingTop = 30;
            style.paddingLeft = 10;
            style.paddingRight = 10;

            var header = new Label("Inspector");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            Add(header);

            _container = new IMGUIContainer(OnGUI);
            Add(_container);
        }

        public void UpdateSelection(BTNodeView nodeView)
        {
            _selectedNode = nodeView;

            if (_editor != null)
            {
                Object.DestroyImmediate(_editor);
            }
        }

        public void ClearSelection()
        {
            _selectedNode = null;
            if (_editor != null)
            {
                Object.DestroyImmediate(_editor);
                _editor = null;
            }
        }

        private void OnGUI()
        {
            if (_selectedNode == null || _selectedNode.NodeData == null)
            {
                EditorGUILayout.LabelField("Select a node to edit its properties.");
                return;
            }

            var data = _selectedNode.NodeData;

            EditorGUILayout.LabelField("Node Type", data.nodeType.ToString(), EditorStyles.boldLabel);

            if (data.nodeType == BTNodeType.Action)
            {
                EditorGUILayout.LabelField("Action Type", data.actionType);
            }

            EditorGUILayout.Space();

            // Draw parameters
            if (data.parameters != null)
            {
                EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
                DrawParameters(data);
            }

            EditorGUILayout.Space();

            // Root node toggle
            var window = EditorWindow.GetWindow<BehaviorTreeEditorWindow>();
            bool isRoot = window.TreeAsset?.RootNodeId == data.id;

            EditorGUI.BeginChangeCheck();
            bool newIsRoot = EditorGUILayout.Toggle("Is Root Node", isRoot);
            if (EditorGUI.EndChangeCheck() && newIsRoot != isRoot)
            {
                if (newIsRoot)
                {
                    window.TreeAsset.RootNodeId = data.id;
                }
                else
                {
                    window.TreeAsset.RootNodeId = null;
                }
                EditorUtility.SetDirty(window.TreeAsset);
                _selectedNode.UpdateVisuals();
            }

            // Show children for composites (Selector, Sequence)
            if (data.nodeType == BTNodeType.Selector || data.nodeType == BTNodeType.Sequence)
            {
                EditorGUILayout.Space();
                DrawChildrenOrder(data, window.TreeAsset);
            }
        }

        private void DrawChildrenOrder(BTNodeData data, BehaviorTreeAsset treeAsset)
        {
            EditorGUILayout.LabelField("Children (execution order)", EditorStyles.boldLabel);

            var children = treeAsset.GetChildConnections(data.id);

            if (children.Count == 0)
            {
                EditorGUILayout.HelpBox("No children connected. Connect nodes to this composite's output port.", MessageType.Info);
                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                var connection = children[i];
                var childNode = treeAsset.GetNode(connection.childId);
                if (childNode == null) continue;

                string childName = childNode.nodeType == BTNodeType.Action
                    ? childNode.actionType
                    : childNode.nodeType.ToString();

                EditorGUILayout.BeginHorizontal();

                // Index label
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));

                // Node name
                EditorGUILayout.LabelField(childName);

                // Move up button
                EditorGUI.BeginDisabledGroup(i == 0);
                if (GUILayout.Button("\u25b2", GUILayout.Width(25))) // Up arrow
                {
                    treeAsset.SwapChildOrder(data.id, i, i - 1);
                    EditorUtility.SetDirty(treeAsset);
                }
                EditorGUI.EndDisabledGroup();

                // Move down button
                EditorGUI.BeginDisabledGroup(i == children.Count - 1);
                if (GUILayout.Button("\u25bc", GUILayout.Width(25))) // Down arrow
                {
                    treeAsset.SwapChildOrder(data.id, i, i + 1);
                    EditorUtility.SetDirty(treeAsset);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawParameters(BTNodeData data)
        {
            var changed = false;

            switch (data.parameters)
            {
                case RepeaterParameters rp:
                    EditorGUI.BeginChangeCheck();
                    rp.repeatCount = EditorGUILayout.IntField("Repeat Count (-1 = forever)", rp.repeatCount);
                    changed = EditorGUI.EndChangeCheck();
                    break;

                case SetNextWaypointParameters sp:
                    EditorGUI.BeginChangeCheck();
                    sp.waypointsKey = EditorGUILayout.TextField("Waypoints Key", sp.waypointsKey);
                    sp.targetKey = EditorGUILayout.TextField("Target Key", sp.targetKey);
                    sp.indexKey = EditorGUILayout.TextField("Index Key", sp.indexKey);
                    changed = EditorGUI.EndChangeCheck();
                    break;

                case MoveToParameters mp:
                    EditorGUI.BeginChangeCheck();
                    mp.arrivalThreshold = EditorGUILayout.FloatField("Arrival Threshold", mp.arrivalThreshold);
                    mp.slowingMultiplier = EditorGUILayout.FloatField("Slowing Multiplier", mp.slowingMultiplier);
                    mp.targetKey = EditorGUILayout.TextField("Target Key", mp.targetKey);
                    changed = EditorGUI.EndChangeCheck();
                    break;

                default:
                    EditorGUILayout.LabelField("No configurable parameters.");
                    break;
            }

            if (changed)
            {
                var window = EditorWindow.GetWindow<BehaviorTreeEditorWindow>();
                EditorUtility.SetDirty(window.TreeAsset);
            }
        }
    }
}
#endif
