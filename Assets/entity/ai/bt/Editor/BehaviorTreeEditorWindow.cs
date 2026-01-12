#if UNITY_EDITOR
using System.Reflection;
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
            _inspectorView = new InspectorView(this);
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
        private BehaviorTreeEditorWindow _editorWindow;

        public InspectorView(BehaviorTreeEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
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

                // Tick Interval for actions/conditions
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Execution", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                int newTickInterval = EditorGUILayout.IntSlider(
                    new GUIContent("Tick Interval", "How often this node executes. 1 = every tick, 2 = every 2nd tick, etc."),
                    data.tickInterval,
                    1,
                    10);
                if (EditorGUI.EndChangeCheck())
                {
                    data.tickInterval = newTickInterval;
                    EditorUtility.SetDirty(_editorWindow.TreeAsset);
                }

                if (data.tickInterval > 1)
                {
                    EditorGUILayout.HelpBox($"This node will execute every {data.tickInterval} ticks. When skipped, it returns its cached result.", MessageType.Info);
                }
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
            bool isRoot = _editorWindow.TreeAsset?.RootNodeId == data.id;

            EditorGUI.BeginChangeCheck();
            bool newIsRoot = EditorGUILayout.Toggle("Is Root Node", isRoot);
            if (EditorGUI.EndChangeCheck() && newIsRoot != isRoot)
            {
                if (newIsRoot)
                {
                    _editorWindow.TreeAsset.RootNodeId = data.id;
                }
                else
                {
                    _editorWindow.TreeAsset.RootNodeId = null;
                }
                EditorUtility.SetDirty(_editorWindow.TreeAsset);
                _selectedNode.UpdateVisuals();
            }

            // Show children for composites (Selector, Sequence)
            if (data.nodeType == BTNodeType.Selector || data.nodeType == BTNodeType.Sequence)
            {
                EditorGUILayout.Space();
                DrawChildrenOrder(data, _editorWindow.TreeAsset);
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
            var parameters = data.parameters;
            if (parameters == null)
            {
                EditorGUILayout.LabelField("No configurable parameters.");
                return;
            }

            // Special handling for SubtreeParameters (cycle detection)
            if (parameters is SubtreeParameters sp)
            {
                DrawSubtreeParameters(sp);
                return;
            }

            // Use reflection for all other parameter types
            var type = parameters.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            EditorGUI.BeginChangeCheck();

            foreach (var field in fields)
            {
                DrawField(field, parameters);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_editorWindow.TreeAsset);
                _selectedNode?.UpdateVisuals();
            }
        }

        private void DrawSubtreeParameters(SubtreeParameters sp)
        {
            EditorGUI.BeginChangeCheck();
            var newAsset = (BehaviorTreeAsset)EditorGUILayout.ObjectField(
                "Subtree Asset",
                sp.subtreeAsset,
                typeof(BehaviorTreeAsset),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                // Validate for cycles before allowing assignment
                if (newAsset != null && _editorWindow.TreeAsset.WouldCreateCycle(newAsset))
                {
                    EditorUtility.DisplayDialog(
                        "Cycle Detected",
                        $"Cannot assign '{newAsset.name}' as a subtree because it would create a circular reference.",
                        "OK");
                }
                else
                {
                    sp.subtreeAsset = newAsset;
                    EditorUtility.SetDirty(_editorWindow.TreeAsset);
                    _selectedNode?.UpdateVisuals();
                }
            }

            // Show info about the referenced tree
            if (sp.subtreeAsset != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Subtree Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Nodes", sp.subtreeAsset.Nodes.Count.ToString());

                if (GUILayout.Button("Open Subtree in Editor"))
                {
                    Selection.activeObject = sp.subtreeAsset;
                }
            }
        }

        private void DrawField(FieldInfo field, object target)
        {
            var fieldType = field.FieldType;
            var fieldName = ObjectNames.NicifyVariableName(field.Name);
            var currentValue = field.GetValue(target);

            object newValue = currentValue;

            // Handle different field types
            if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(fieldName, (string)currentValue ?? "");
            }
            else if (fieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(fieldName, (int)currentValue);
            }
            else if (fieldType == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(fieldName, (float)currentValue);
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(fieldName, (bool)currentValue);
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(fieldName, (Vector2)currentValue);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(fieldName, (Vector3)currentValue);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(fieldName, (System.Enum)currentValue);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(fieldName, (UnityEngine.Object)currentValue, fieldType, false);
            }
            else
            {
                // Unsupported type - show as label
                EditorGUILayout.LabelField(fieldName, currentValue?.ToString() ?? "(null)");
                return;
            }

            // Update value if changed
            if (!Equals(newValue, currentValue))
            {
                field.SetValue(target, newValue);
            }
        }
    }
}
#endif
