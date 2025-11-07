using UnityEditor;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    [CustomEditor(typeof(StateMachine))]
    public class StateMachineInspector : UnityEditor.Editor
    {
        private readonly System.Collections.Generic.Dictionary<string, bool> _foldouts = new System.Collections.Generic.Dictionary<string, bool>();

        public override void OnInspectorGUI()
        {
            var sm = (StateMachine)target;

            GUILayout.Space(4);
            if (GUILayout.Button("Open State Machine Graph"))
            {
                StateMachineGraphWindow.Open(sm);
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            foreach (var node in sm.Nodes)
            {
                if (node == null) { continue; }
                if (!_foldouts.ContainsKey(node.id)) { _foldouts[node.id] = true; }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                GUILayout.BeginHorizontal();
                _foldouts[node.id] = EditorGUILayout.Foldout(_foldouts[node.id], string.IsNullOrEmpty(node.title) ? "<Unnamed State>" : node.title, true);
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel = previousIndent;

                if (_foldouts[node.id])
                {
                    EditorGUI.indentLevel++;
                    var previousState = node.state;
                    node.state = (StateDefinition)EditorGUILayout.ObjectField("State", node.state, typeof(StateDefinition), false);
                    if (node.state != previousState)
                    {
                        EditorUtility.SetDirty(sm);
                    }

                    if (node.state != null)
                    {
                        DrawStateVariables(node.state);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawStateVariables(StateDefinition state)
        {
            var serializedState = new SerializedObject(state);
            var fields = state.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(StateVariableAttribute), true).Length == 0) { continue; }

                var property = serializedState.FindProperty(field.Name);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, true);
                }
                else
                {
                    object current = field.GetValue(state);
                    EditorGUILayout.LabelField(field.Name, current != null ? current.ToString() : "null");
                }
            }

            if (serializedState.hasModifiedProperties)
            {
                serializedState.ApplyModifiedProperties();
                EditorUtility.SetDirty(state);
            }
        }
    }
}