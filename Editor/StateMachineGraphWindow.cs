using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    public class StateMachineGraphWindow : EditorWindow
    {
        private StateMachine _stateMachine;
        private Vector2 _panOffset;
        private Vector2 _draggingNodeOffset;
        private StateNode _selectedNode;
        private bool _isPanning;
        private StateNode _connectingFrom;
        private StateTransition _selectedTransition;
        private readonly List<TransitionRenderInfo> _transitionRenderInfos = new();
        private readonly Dictionary<string, bool> _nodeExpandedStates = new();
        private SerializedObject _serializedStateMachine;
        private SerializedProperty _nodesProperty;
        private float _zoom = 1.0f;

        private static readonly Vector2 BaseNodeSize = new(250, 80);
        private const float FieldHeight = 18f;
        private const float FieldSpacing = 2f;
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 2.0f;
        private const float ZoomStep = 0.1f;
        private static readonly Color GridMinorColor = new(0f, 0f, 0f, 0.2f);
        private static readonly Color GridMajorColor = new(0f, 0f, 0f, 0.4f);
        private static readonly Color EdgeColor = new(0.3f, 0.8f, 1f, 1f);
        private static readonly Color NodeColor = new(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color NodeSelectedColor = new(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color NodeBorderColor = new(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color InitialStateBorderColor = new(0.0f, 0.8f, 0.0f, 1f);
        private const float ArrowSize = 10f;
        private const float BaseEdgeWidth = 4f;
        private const float SelectedEdgeWidth = 6f;
        private const float EdgeSelectionRadius = 8f;

        private struct TransitionRenderInfo
        {
            public StateTransition Transition;
            public Vector3[] Points;
            public Vector2 ArrowTip;
            public Vector2 ArrowDirection;
        }

        public static void Open(StateMachine stateMachine)
        {
            var window = GetWindow<StateMachineGraphWindow>("State Machine Graph");
            window._stateMachine = stateMachine;
            window.RefreshSerializedObject();
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            RefreshSerializedObject();
        }
        
        private void RefreshSerializedObject()
        {
            if (_stateMachine != null)
            {
                _serializedStateMachine = new SerializedObject(_stateMachine);
                _nodesProperty = _serializedStateMachine.FindProperty("nodes");
            }
        }
        
        private void OnDisable()
        {
            _serializedStateMachine = null;
            _nodesProperty = null;
        }
        
        private bool IsNodeExpanded(StateNode node)
        {
            _nodeExpandedStates.TryAdd(node.id, false);
            return _nodeExpandedStates[node.id];
        }
        
        private void SetNodeExpanded(StateNode node, bool expanded)
        {
            _nodeExpandedStates[node.id] = expanded;
        }
        
        private float GetNodeHeight(StateNode node)
        {
            var height = BaseNodeSize.y;
            
            if (!IsNodeExpanded(node) || node.state == null) return height;
            
            height += 18; // Foldout height
            var stateType = node.state.GetType();
            var fields = stateType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(StateVariableAttribute), true).Length <= 0) continue;
                
                var fieldType = field.FieldType;
                if (fieldType == typeof(Vector2) || fieldType == typeof(Vector3))
                {
                    height += FieldHeight * 2 + FieldSpacing; // Vector fields take more space
                }
                else
                {
                    height += FieldHeight + FieldSpacing;
                }
            }
            height += 10; // Extra padding
            return height;
        }

        private void OnGUI()
        {
            if (_stateMachine == null)
            {
                EditorGUILayout.HelpBox("No StateMachine selected.", MessageType.Info);
                return;
            }
            
            // Toolbar with settings
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                GearboxSettingsWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            DrawGrid(20, 0.2f, GridMinorColor);
            DrawGrid(100, 0.4f, GridMajorColor);

            BuildTransitionRenderCache();
            ProcessEvents(Event.current);

            DrawEdges();
            DrawNodes();

            if (GUI.changed || _connectingFrom != null)
            {
                Repaint();
            }
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            var scaledGridSpacing = gridSpacing * _zoom;
            var widthDivs = Mathf.CeilToInt(position.width / scaledGridSpacing);
            var heightDivs = Mathf.CeilToInt(position.height / scaledGridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            var newOffset = new Vector3(_panOffset.x % scaledGridSpacing, _panOffset.y % scaledGridSpacing, 0);

            for (var i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(scaledGridSpacing * i, -scaledGridSpacing, 0) + newOffset,
                    new Vector3(scaledGridSpacing * i, position.height, 0f) + newOffset);
            }

            for (var j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-scaledGridSpacing, scaledGridSpacing * j, 0) + newOffset,
                    new Vector3(position.width, scaledGridSpacing * j, 0f) + newOffset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            foreach (var node in _stateMachine.Nodes)
            {
                DrawNode(node);
            }
        }

        private Rect GetNodeRect(StateNode node)
        {
            var pos = (node.position * _zoom) + _panOffset;
            var height = GetNodeHeight(node) * _zoom;
            return new Rect(pos, new Vector2(BaseNodeSize.x * _zoom, height));
        }

        private void BuildTransitionRenderCache()
        {
            _transitionRenderInfos.Clear();
            
            if (_stateMachine == null) return;

            foreach (var transition in _stateMachine.Transitions)
            {
                var fromNode = _stateMachine.Nodes.Find(n => n.id == transition.fromId);
                var toNode = _stateMachine.Nodes.Find(n => n.id == transition.toId);
                if (fromNode == null || toNode == null) continue;

                var renderInfo = new TransitionRenderInfo { Transition = transition };

                var fromRect = GetNodeRect(fromNode);
                var toRect = GetNodeRect(toNode);
                var fromCenter = fromRect.center;
                var toCenter = toRect.center;
                var direction = (toCenter - fromCenter).normalized;
                var perpendicular = new Vector2(-direction.y, direction.x);

                // Check if there's a reciprocal transition (bidirectional connection)
                var hasReciprocal = _stateMachine.Transitions.Exists(t =>
                    t != transition && t.fromId == transition.toId && t.toId == transition.fromId);

                Vector2 startPoint, endPoint;
                if (hasReciprocal)
                {
                    // Offset both transitions in the same perpendicular direction for parallel lines
                    const float offsetDistance = 8f;
                    var offset = perpendicular * offsetDistance;

                    var offsetFromCenter = fromCenter + offset;
                    var offsetToCenter = toCenter + offset;

                    startPoint = TryGetLineRectIntersection(offsetFromCenter, offsetToCenter, fromRect, out var intersection)
                        ? intersection : offsetFromCenter;
                    endPoint = TryGetLineRectIntersection(offsetFromCenter, offsetToCenter, toRect, out intersection)
                        ? intersection : offsetToCenter;
                }
                else
                {
                    // Direct connection without offset
                    startPoint = TryGetLineRectIntersection(fromCenter, toCenter, fromRect, out var intersection)
                        ? intersection : fromCenter;
                    endPoint = TryGetLineRectIntersection(fromCenter, toCenter, toRect, out intersection)
                        ? intersection : toCenter;
                }

                renderInfo.Points = new Vector3[] { startPoint, endPoint };
                renderInfo.ArrowTip = endPoint;
                renderInfo.ArrowDirection = direction;

                _transitionRenderInfos.Add(renderInfo);
            }
        }

        private void DrawNode(StateNode node)
        {
            // Calculate screen-space rect for mouse interactions
            var screenRect = GetNodeRect(node);
            
            // Apply zoom scaling matrix for the entire node (border, fill, and contents)
            var originalMatrix = GUI.matrix;
            
            // Create a matrix that combines pan offset, zoom, and node position
            var nodeGraphPos = node.position;
            var nodeTranslationMatrix = Matrix4x4.Translate(new Vector3(nodeGraphPos.x, nodeGraphPos.y, 0));
            var scaleMatrix = Matrix4x4.Scale(new Vector3(_zoom, _zoom, 1f));
            var panTranslationMatrix = Matrix4x4.Translate(new Vector3(_panOffset.x, _panOffset.y, 0));
            
            GUI.matrix = panTranslationMatrix * scaleMatrix * nodeTranslationMatrix;
            
            // Calculate node rect in local space (starting from origin)
            var localNodeRect = new Rect(0, 0, BaseNodeSize.x, GetNodeHeight(node));
            
            // Draw node background and border with the matrix applied
            var bodyColor = node == _selectedNode ? NodeSelectedColor : NodeColor;
            EditorGUI.DrawRect(localNodeRect, bodyColor);
            
            // Border
            Handles.BeginGUI();
            var borderColor = node.IsInitialState ? InitialStateBorderColor : NodeBorderColor;
            var borderWidth = node.IsInitialState ? 3f : 2f;
            Handles.color = borderColor;
            Handles.DrawAAPolyLine(borderWidth, new Vector3[]
            {
                new(localNodeRect.xMin, localNodeRect.yMin),
                new(localNodeRect.xMax, localNodeRect.yMin),
                new(localNodeRect.xMax, localNodeRect.yMax),
                new(localNodeRect.xMin, localNodeRect.yMax),
                new(localNodeRect.xMin, localNodeRect.yMin)
            });
            Handles.color = Color.white;
            Handles.EndGUI();
            
            float currentY = 8;
            
            // Title field
            var titleRect = new Rect(8, currentY, BaseNodeSize.x - 16, 18);
            node.title = EditorGUI.TextField(titleRect, node.title);
            currentY += 20;
            
            // Type dropdown
            GUI.BeginGroup(new Rect(8, currentY, BaseNodeSize.x - 16, 18));
            EditorGUI.BeginChangeCheck();
            var types = TypeDropdown.GetInheritedTypes(typeof(StateDefinition));
            var typeNames = types.Select(t => t.Name).ToArray();
            var currentType = node.GetStateType();
            var currentIndex = currentType != null ? types.IndexOf(currentType) : -1;
            if (currentIndex < 0) currentIndex = 0;
            var newIndex = EditorGUI.Popup(new Rect(0, 0, BaseNodeSize.x - 16, 18), currentIndex, typeNames);
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < types.Count)
            {
                var selectedType = types[newIndex];
                node.SetStateType(selectedType);
                if (node.state == null || node.state.GetType() != selectedType)
                {
                    node.state = (StateDefinition)Activator.CreateInstance(selectedType);
                    MarkDirty();
                }
            }
            GUI.EndGroup();
            currentY += 20;
            
            // Expand/collapse button
            var expandRect = new Rect(8, currentY, BaseNodeSize.x - 16, 16);
            var isExpanded = IsNodeExpanded(node);
            var hasState = node.state != null;
            
            if (hasState)
            {
                var newExpanded = EditorGUI.Foldout(expandRect, isExpanded, "Variables", true);
                if (newExpanded != isExpanded)
                {
                    SetNodeExpanded(node, newExpanded);
                }
                currentY += 18;
                
                // Draw state variables if expanded
                if (isExpanded)
                {
                    DrawStateVariablesInNode(node, localNodeRect, ref currentY);
                }
            }
            
            // Restore original matrix
            GUI.matrix = originalMatrix;
            // Handle mouse on node
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Complete a pending connection by clicking a target node
                if (_connectingFrom != null && screenRect.Contains(e.mousePosition))
                {
                    TryCreateTransition(_connectingFrom, node);
                    _connectingFrom = null;
                    e.Use();
                    return;
                }

                if (screenRect.Contains(e.mousePosition))
                {
                    _selectedNode = node;
                    _draggingNodeOffset = node.position - ((e.mousePosition - _panOffset) / _zoom);
                    GUI.FocusControl(null);
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && _selectedNode == node)
            {
                node.position = ((e.mousePosition - _panOffset) / _zoom) + _draggingNodeOffset;
                MarkDirty();
                GUI.changed = true;
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _selectedNode = null;
            }
        }

        private void DrawEdges()
        {
            Handles.BeginGUI();

            foreach (var info in _transitionRenderInfos)
            {
                var isSelected = info.Transition == _selectedTransition;
                var width = isSelected ? SelectedEdgeWidth : BaseEdgeWidth;
                var color = isSelected ? Color.yellow : EdgeColor;
                Handles.color = color;

                Handles.DrawAAPolyLine(width, info.Points);

                DrawArrow(info.ArrowTip, info.ArrowDirection);
            }

            if (_connectingFrom != null)
            {
                var fromRect = GetNodeRect(_connectingFrom);
                var fromCenter = fromRect.center;
                Handles.color = EdgeColor;
                Handles.DrawAAPolyLine(BaseEdgeWidth, new Vector3[] { fromCenter, Event.current.mousePosition });
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private static bool TryGetLineRectIntersection(Vector2 a, Vector2 b, Rect rect, out Vector2 intersection)
        {
            // Intersect with each side of the rectangle and take the nearest to 'b' along the line
            var found = false;
            var bestT = float.PositiveInfinity;
            var bestPoint = Vector2.zero;

            var dir = b - a;

            // Avoid division by zero by checking components
            if (Mathf.Abs(dir.x) > 1e-5f)
            {
                // Left edge x = rect.xMin
                var t = (rect.xMin - a.x) / dir.x;
                if (t is >= 0f and <= 1f)
                {
                    var y = a.y + t * dir.y;
                    if (y >= rect.yMin && y <= rect.yMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(rect.xMin, y); }
                    }
                }
                // Right edge x = rect.xMax
                t = (rect.xMax - a.x) / dir.x;
                if (t is >= 0f and <= 1f)
                {
                    var y = a.y + t * dir.y;
                    if (y >= rect.yMin && y <= rect.yMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(rect.xMax, y); }
                    }
                }
            }

            if (Mathf.Abs(dir.y) > 1e-5f)
            {
                // Bottom edge y = rect.yMin
                var t = (rect.yMin - a.y) / dir.y;
                if (t is >= 0f and <= 1f)
                {
                    var x = a.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(x, rect.yMin); }
                    }
                }
                // Top edge y = rect.yMax
                t = (rect.yMax - a.y) / dir.y;
                if (t is >= 0f and <= 1f)
                {
                    var x = a.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                    {
                        found = true;
                        if (t < bestT) 
                        {
                            bestPoint = new Vector2(x, rect.yMax); 
                        }
                    }
                }
            }

            intersection = bestPoint;
            return found;
        }

        private static void DrawArrow(Vector2 tip, Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var dir = direction.normalized;
            var back = tip - dir * ArrowSize;
            var perp = new Vector2(-dir.y, dir.x);
            var wingA = back + perp * (ArrowSize * 0.5f);
            var wingB = back - perp * (ArrowSize * 0.5f);

            Handles.DrawLine(tip, wingA);
            Handles.DrawLine(tip, wingB);
        }

        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                {
                    if (e.button == 0)
                    {
                        if (TryGetTransitionAtPosition(e.mousePosition, EdgeSelectionRadius, out var transition))
                        {
                            _selectedTransition = transition;
                            GUI.changed = true;
                            e.Use();
                            return;
                        }

                        _selectedTransition = null;
                    }

                    switch (e.button)
                    {
                        case 0 when e.alt:
                            _isPanning = true;
                            GUI.FocusControl(null);
                            break;
                        case 2: // Middle mouse button for panning
                            _isPanning = true;
                            GUI.FocusControl(null);
                            break;
                        case 1 when TryGetTransitionAtPosition(e.mousePosition, EdgeSelectionRadius, out var transition):
                            _selectedTransition = transition;
                            ShowTransitionContextMenu(transition);
                            e.Use();
                            return;
                        case 1:
                            ShowContextMenu(e.mousePosition);
                            break;
                    }

                    break;
                }
                case EventType.MouseDrag when _isPanning:
                    _panOffset += e.delta;
                    GUI.changed = true;
                    break;
                case EventType.MouseDrag:
                {
                    if (_connectingFrom != null)
                    {
                        // While drawing a connection, update every drag
                        Repaint();
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if (e.button == 2 || (e.button == 0 && e.alt))
                    {
                        _isPanning = false;
                    }

                    break;
                }
                case EventType.MouseMove:
                {
                    // Ensure the preview line follows the cursor
                    if (_connectingFrom != null)
                    {
                        Repaint();
                    }

                    break;
                }
                case EventType.ScrollWheel:
                {
                    var oldZoom = _zoom;
                    _zoom = Mathf.Clamp(_zoom - e.delta.y * ZoomStep * 0.1f, MinZoom, MaxZoom);
                    
                    // Adjust pan offset to zoom towards mouse position
                    if (Mathf.Abs(oldZoom - _zoom) > 0.001f)
                    {
                        var mouseWorldPos = (e.mousePosition - _panOffset) / oldZoom;
                        _panOffset = e.mousePosition - mouseWorldPos * _zoom;
                        GUI.changed = true;
                    }
                    e.Use();
                    break;
                }
                case EventType.KeyDown:
                {
                    if (_selectedTransition != null && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
                    {
                        _stateMachine.Transitions.Remove(_selectedTransition);
                        _selectedTransition = null;
                        MarkDirty();
                        GUI.changed = true;
                        e.Use();
                    }

                    break;
                }
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            var menu = new GenericMenu();
            var hovered = GetNodeAtPosition(mousePosition);

            if (hovered == null)
            {
                menu.AddItem(new GUIContent("Add State"), false, () =>
                {
                    AddNode(mousePosition - _panOffset);
                });
            }
            else
            {
                var node = hovered;
                menu.AddItem(new GUIContent("Make Transition"), false, () =>
                {
                    _connectingFrom = node;
                });
                
                // Add "Make as initial" option
                if (!node.IsInitialState)
                {
                    menu.AddItem(new GUIContent("Make as initial"), false, () =>
                    {
                        SetAsInitialState(node);
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Make as initial"));
                }
                
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Delete State"), false, () =>
                {
                    if (_selectedNode == node)
                    {
                        _selectedNode = null;
                    }
                    DeleteNode(node);
                });
            }

            menu.ShowAsContext();
        }

        private void ShowTransitionContextMenu(StateTransition transition)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Transition"), false, () =>
            {
                _stateMachine.Transitions.Remove(transition);
                if (_selectedTransition == transition)
                {
                    _selectedTransition = null;
                }
                MarkDirty();
                GUI.changed = true;
            });
            menu.ShowAsContext();
        }

        private StateNode GetNodeAtPosition(Vector2 mousePosition)
        {
            for (var i = _stateMachine.Nodes.Count - 1; i >= 0; i--)
            {
                var node = _stateMachine.Nodes[i];
                if (GetNodeRect(node).Contains(mousePosition))
                {
                    return node;
                }
            }
            return null;
        }

        private void AddNode(Vector2 graphPosition)
        {
            var node = new StateNode
            {
                id = GUID.Generate().ToString(),
                title = "State",
                position = graphPosition
            };
            
            // If this is the first node, make it the initial state
            if (_stateMachine.Nodes.Count == 0)
            {
                node.IsInitialState = true;
            }
            
            _stateMachine.Nodes.Add(node);
            MarkDirty();
        }

        private void DeleteNode(StateNode node)
        {
            // If deleting the initial state, set another node as initial if available
            if (node.IsInitialState && _stateMachine.Nodes.Count > 1)
            {
                var otherNode = _stateMachine.Nodes.Find(n => n != node);
                if (otherNode != null)
                {
                    otherNode.IsInitialState = true;
                }
            }
            
            _stateMachine.Transitions.RemoveAll(t => t.fromId == node.id || t.toId == node.id);
            _stateMachine.Nodes.Remove(node);
            MarkDirty();
        }

        private void SetAsInitialState(StateNode node)
        {
            // Clear initial state from all other nodes
            foreach (var otherNode in _stateMachine.Nodes)
            {
                if (otherNode != node)
                {
                    otherNode.IsInitialState = false;
                }
            }
            
            // Set this node as initial state
            node.IsInitialState = true;
            MarkDirty();
        }

        private void TryCreateTransition(StateNode from, StateNode to)
        {
            var exists = _stateMachine.Transitions.Exists(t => t.fromId == from.id && t.toId == to.id);
            if (exists) { return; }

            _stateMachine.Transitions.Add(new StateTransition
            {
                fromId = from.id,
                toId = to.id
            });
            MarkDirty();
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(_stateMachine);
            if (_serializedStateMachine != null)
            {
                _serializedStateMachine.Update();
            }
        }
        
        private void DrawStateVariablesInNode(StateNode node, Rect nodeRect, ref float currentY)
        {
            if (node.state == null) return;
            
            // Update serialized object
            if (_serializedStateMachine != null)
            {
                _serializedStateMachine.Update();
            }
            
            // Find the node property
            SerializedProperty nodeProperty = null;
            SerializedProperty stateProperty = null;
            
            if (_nodesProperty != null)
            {
                for (int i = 0; i < _nodesProperty.arraySize; i++)
                {
                    var prop = _nodesProperty.GetArrayElementAtIndex(i);
                    var idProp = prop.FindPropertyRelative("id");
                    if (idProp != null && idProp.stringValue == node.id)
                    {
                        nodeProperty = prop;
                        stateProperty = prop.FindPropertyRelative("state");
                        break;
                    }
                }
            }
            
            var stateType = node.state.GetType();
            var fields = stateType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(StateVariableAttribute), true).Length == 0)
                {
                    continue;
                }
                
                var fieldRect = new Rect(nodeRect.x + 8, currentY, nodeRect.width - 16, FieldHeight);
                
                // Try to use SerializedProperty first
                if (stateProperty != null)
                {
                    var fieldProperty = stateProperty.FindPropertyRelative(field.Name);
                    if (fieldProperty != null)
                    {
                        EditorGUI.PropertyField(fieldRect, fieldProperty, new GUIContent(ObjectNames.NicifyVariableName(field.Name)), false);
                        currentY += FieldHeight + FieldSpacing;
                        continue;
                    }
                }
                
                // Fallback to reflection-based editing
                var value = field.GetValue(node.state);
                var fieldType = field.FieldType;
                EditorGUI.BeginChangeCheck();
                object newValue = null;
                
                if (fieldType == typeof(int))
                {
                    newValue = EditorGUI.IntField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (int)value);
                }
                else if (fieldType == typeof(float))
                {
                    newValue = EditorGUI.FloatField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (float)value);
                }
                else if (fieldType == typeof(string))
                {
                    newValue = EditorGUI.TextField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (string)value);
                }
                else if (fieldType == typeof(bool))
                {
                    newValue = EditorGUI.Toggle(fieldRect, ObjectNames.NicifyVariableName(field.Name), (bool)value);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {
                    newValue = EditorGUI.ObjectField(fieldRect, ObjectNames.NicifyVariableName(field.Name), value as UnityEngine.Object, fieldType, true);
                }
                else if (fieldType == typeof(Vector2))
                {
                    newValue = EditorGUI.Vector2Field(fieldRect, ObjectNames.NicifyVariableName(field.Name), (Vector2)value);
                    currentY += FieldHeight; // Vector2 field takes more space
                }
                else if (fieldType == typeof(Vector3))
                {
                    newValue = EditorGUI.Vector3Field(fieldRect, ObjectNames.NicifyVariableName(field.Name), (Vector3)value);
                    currentY += FieldHeight; // Vector3 field takes more space
                }
                else if (fieldType == typeof(Color))
                {
                    newValue = EditorGUI.ColorField(fieldRect, ObjectNames.NicifyVariableName(field.Name), (Color)value);
                }
                else
                {
                    // For other types, just display as read-only
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.LabelField(fieldRect, ObjectNames.NicifyVariableName(field.Name), value != null ? value.ToString() : "null");
                    EditorGUI.EndDisabledGroup();
                }
                
                if (EditorGUI.EndChangeCheck() && newValue != null)
                {
                    field.SetValue(node.state, newValue);
                    MarkDirty();
                }
                
                currentY += FieldHeight + FieldSpacing;
            }
            
            // Apply changes to serialized object
            if (_serializedStateMachine != null && _serializedStateMachine.hasModifiedProperties)
            {
                _serializedStateMachine.ApplyModifiedProperties();
            }
        }

        private bool TryGetTransitionAtPosition(Vector2 mousePosition, float maxDistance, out StateTransition transition)
        {
            transition = null;
            var bestDistance = maxDistance;

            foreach (var info in _transitionRenderInfos)
            {
                var distance = DistanceToPolyline(mousePosition, info.Points);
                
                if (!(distance < bestDistance)) continue;
                
                bestDistance = distance;
                transition = info.Transition;
            }

            return transition != null;
        }

        private static float DistanceToPolyline(Vector2 point, Vector3[] polyline)
        {
            var min = float.PositiveInfinity;
            for (var i = 0; i < polyline.Length - 1; i++)
            {
                var distance = DistancePointToSegment(point, polyline[i], polyline[i + 1]);
                
                if (distance < min)
                {
                    min = distance;
                }
            }
            return min;
        }

        private static float DistancePointToSegment(Vector2 point, Vector3 a, Vector3 b)
        {
            var segment = (Vector2)(b - a);
            var lengthSq = segment.sqrMagnitude;
            
            if (lengthSq <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, a);
            }

            var t = Vector2.Dot(point - (Vector2)a, segment) / lengthSq;
            t = Mathf.Clamp01(t);
            var projection = (Vector2)a + segment * t;
            return Vector2.Distance(point, projection);
        }
    }
}
