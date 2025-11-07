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
        private readonly System.Collections.Generic.List<TransitionRenderInfo> _transitionRenderInfos = new System.Collections.Generic.List<TransitionRenderInfo>();

        private static readonly Vector2 NodeSize = new Vector2(160, 60);
        private static readonly Color GridMinorColor = new Color(0f, 0f, 0f, 0.2f);
        private static readonly Color GridMajorColor = new Color(0f, 0f, 0f, 0.4f);
        private static readonly Color EdgeColor = new Color(0.3f, 0.8f, 1f, 1f);
        private static readonly Color NodeColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color NodeSelectedColor = new Color(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color NodeBorderColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly float ArrowSize = 10f;
        private const float BaseEdgeWidth = 4f;
        private const float SelectedEdgeWidth = 6f;
        private const float EdgeSelectionRadius = 8f;
        private const float ReciprocalCurveOffset = 40f;

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
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (_stateMachine == null)
            {
                EditorGUILayout.HelpBox("No StateMachine selected.", MessageType.Info);
                return;
            }

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
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            Vector3 newOffset = new Vector3(_panOffset.x % gridSpacing, _panOffset.y % gridSpacing, 0);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                    new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                    new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
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
            Vector2 pos = node.position + _panOffset;
            return new Rect(pos, NodeSize);
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

                Rect fromRect = GetNodeRect(fromNode);
                Rect toRect = GetNodeRect(toNode);
                Vector2 fromCenter = fromRect.center;
                Vector2 toCenter = toRect.center;
                Vector2 direction = (toCenter - fromCenter).normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                // Check if there's a reciprocal transition (bidirectional connection)
                bool hasReciprocal = _stateMachine.Transitions.Exists(t =>
                    t != transition && t.fromId == transition.toId && t.toId == transition.fromId);

                Vector2 startPoint, endPoint;
                if (hasReciprocal)
                {
                    // Offset both transitions in the same perpendicular direction for parallel lines
                    const float offsetDistance = 8f;
                    Vector2 offset = perpendicular * offsetDistance;

                    Vector2 offsetFromCenter = fromCenter + offset;
                    Vector2 offsetToCenter = toCenter + offset;

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

        private Vector3[] BuildBezierPoints(Vector2 start, Vector2 control1, Vector2 control2, Vector2 end, int segments)
        {
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                points[i] = CalculateCubicBezierPoint(t, start, control1, control2, end);
            }
            return points;
        }

        private Vector3 CalculateCubicBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 point = uuu * p0;
            point += 3f * uu * t * p1;
            point += 3f * u * tt * p2;
            point += ttt * p3;
            return point;
        }

        private void DrawNode(StateNode node)
        {
            Rect rect = GetNodeRect(node);
            var bodyColor = node == _selectedNode ? NodeSelectedColor : NodeColor;
            EditorGUI.DrawRect(rect, bodyColor);
            // Border
            Handles.BeginGUI();
            Handles.color = NodeBorderColor;
            Handles.DrawAAPolyLine(2f, new Vector3[]
            {
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin)
            });
            Handles.color = Color.white;
            Handles.EndGUI();

            var titleRect = new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 18);
            node.title = EditorGUI.TextField(titleRect, node.title);

            // Handle mouse on node
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Complete a pending connection by clicking a target node
                if (_connectingFrom != null && rect.Contains(e.mousePosition))
                {
                    TryCreateTransition(_connectingFrom, node);
                    _connectingFrom = null;
                    e.Use();
                    return;
                }

                if (rect.Contains(e.mousePosition))
                {
                    _selectedNode = node;
                    _draggingNodeOffset = node.position - (e.mousePosition - _panOffset);
                    GUI.FocusControl(null);
                }
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && _selectedNode == node)
            {
                node.position = e.mousePosition - _panOffset + _draggingNodeOffset;
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
                bool isSelected = info.Transition == _selectedTransition;
                float width = isSelected ? SelectedEdgeWidth : BaseEdgeWidth;
                Color color = isSelected ? Color.yellow : EdgeColor;
                Handles.color = color;

                Handles.DrawAAPolyLine(width, info.Points);

                DrawArrow(info.ArrowTip, info.ArrowDirection);
            }

            if (_connectingFrom != null)
            {
                Rect fromRect = GetNodeRect(_connectingFrom);
                Vector2 fromCenter = fromRect.center;
                Handles.color = EdgeColor;
                Handles.DrawAAPolyLine(BaseEdgeWidth, new Vector3[] { fromCenter, Event.current.mousePosition });
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private bool TryGetLineRectIntersection(Vector2 a, Vector2 b, Rect rect, out Vector2 intersection)
        {
            // Intersect with each side of the rectangle and take the nearest to 'b' along the line
            bool found = false;
            float bestT = float.PositiveInfinity;
            Vector2 bestPoint = Vector2.zero;

            Vector2 dir = b - a;

            // Avoid division by zero by checking components
            if (Mathf.Abs(dir.x) > 1e-5f)
            {
                // Left edge x = rect.xMin
                float t = (rect.xMin - a.x) / dir.x;
                if (t >= 0f && t <= 1f)
                {
                    float y = a.y + t * dir.y;
                    if (y >= rect.yMin && y <= rect.yMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(rect.xMin, y); }
                    }
                }
                // Right edge x = rect.xMax
                t = (rect.xMax - a.x) / dir.x;
                if (t >= 0f && t <= 1f)
                {
                    float y = a.y + t * dir.y;
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
                float t = (rect.yMin - a.y) / dir.y;
                if (t >= 0f && t <= 1f)
                {
                    float x = a.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(x, rect.yMin); }
                    }
                }
                // Top edge y = rect.yMax
                t = (rect.yMax - a.y) / dir.y;
                if (t >= 0f && t <= 1f)
                {
                    float x = a.x + t * dir.x;
                    if (x >= rect.xMin && x <= rect.xMax)
                    {
                        found = true;
                        if (t < bestT) { bestT = t; bestPoint = new Vector2(x, rect.yMax); }
                    }
                }
            }

            intersection = bestPoint;
            return found;
        }

        private void DrawArrow(Vector2 tip, Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            Vector2 dir = direction.normalized;
            Vector2 back = tip - dir * ArrowSize;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 wingA = back + perp * (ArrowSize * 0.5f);
            Vector2 wingB = back - perp * (ArrowSize * 0.5f);

            Handles.DrawLine(tip, wingA);
            Handles.DrawLine(tip, wingB);
        }

        private void ProcessEvents(Event e)
        {
            if (e.type == EventType.MouseDown)
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
                    else
                    {
                        _selectedTransition = null;
                    }
                }

                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _isPanning = true;
                    GUI.FocusControl(null);
                }
                else if (e.button == 1)
                {
                    if (TryGetTransitionAtPosition(e.mousePosition, EdgeSelectionRadius, out var transition))
                    {
                        _selectedTransition = transition;
                        ShowTransitionContextMenu(transition);
                        e.Use();
                        return;
                    }

                    ShowContextMenu(e.mousePosition);
                }
            }
            else if (e.type == EventType.MouseDrag)
            {
                if (_isPanning)
                {
                    _panOffset += e.delta;
                    GUI.changed = true;
                }
                else if (_connectingFrom != null)
                {
                    // While drawing a connection, update every drag
                    Repaint();
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 2 || (e.button == 0 && e.alt))
                {
                    _isPanning = false;
                }
            }
            else if (e.type == EventType.MouseMove)
            {
                // Ensure the preview line follows the cursor
                if (_connectingFrom != null)
                {
                    Repaint();
                }
            }
            else if (e.type == EventType.KeyDown)
            {
                if (_selectedTransition != null && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
                {
                    _stateMachine.Transitions.Remove(_selectedTransition);
                    _selectedTransition = null;
                    MarkDirty();
                    GUI.changed = true;
                    e.Use();
                }
            }
        }

        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();
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
            GenericMenu menu = new GenericMenu();
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
            for (int i = _stateMachine.Nodes.Count - 1; i >= 0; i--)
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
            _stateMachine.Nodes.Add(node);
            MarkDirty();
        }

        private void DeleteNode(StateNode node)
        {
            _stateMachine.Transitions.RemoveAll(t => t.fromId == node.id || t.toId == node.id);
            _stateMachine.Nodes.Remove(node);
            MarkDirty();
        }

        private void TryCreateTransition(StateNode from, StateNode to)
        {
            bool exists = _stateMachine.Transitions.Exists(t => t.fromId == from.id && t.toId == to.id);
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
        }

        private bool TryGetTransitionAtPosition(Vector2 mousePosition, float maxDistance, out StateTransition transition)
        {
            transition = null;
            float bestDistance = maxDistance;

            foreach (var info in _transitionRenderInfos)
            {
                float distance = DistanceToPolyline(mousePosition, info.Points);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    transition = info.Transition;
                }
            }

            return transition != null;
        }

        private float DistanceToPolyline(Vector2 point, Vector3[] polyline)
        {
            float min = float.PositiveInfinity;
            for (int i = 0; i < polyline.Length - 1; i++)
            {
                float distance = DistancePointToSegment(point, polyline[i], polyline[i + 1]);
                if (distance < min)
                {
                    min = distance;
                }
            }
            return min;
        }

        private float DistancePointToSegment(Vector2 point, Vector3 a, Vector3 b)
        {
            Vector2 segment = (Vector2)(b - a);
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, a);
            }

            float t = Vector2.Dot(point - (Vector2)a, segment) / lengthSq;
            t = Mathf.Clamp01(t);
            Vector2 projection = (Vector2)a + segment * t;
            return Vector2.Distance(point, projection);
        }
    }
}
