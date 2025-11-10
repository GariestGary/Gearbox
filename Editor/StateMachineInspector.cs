using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    [CustomEditor(typeof(StateMachine))]
    public class StateMachineInspector : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private SerializedProperty _nodesProperty;

        private void OnEnable()
        {
            _nodesProperty = serializedObject.FindProperty("nodes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var sm = (StateMachine)target;

            GUILayout.Space(4);
            if (GUILayout.Button("Open State Machine Graph"))
            {
                StateMachineGraphWindow.Open(sm);
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            if (_nodesProperty != null)
            {
                for (int i = 0; i < _nodesProperty.arraySize; i++)
                {
                    var nodeProperty = _nodesProperty.GetArrayElementAtIndex(i);
                    var nodeIdProperty = nodeProperty.FindPropertyRelative("id");
                    var nodeTitleProperty = nodeProperty.FindPropertyRelative("title");
                    var stateProperty = nodeProperty.FindPropertyRelative("state");
                    
                    if (nodeIdProperty == null) continue;
                    
                    var nodeId = nodeIdProperty.stringValue;
                    if (!_foldouts.ContainsKey(nodeId)) { _foldouts[nodeId] = true; }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    var previousIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    GUILayout.BeginHorizontal();
                    _foldouts[nodeId] = EditorGUILayout.Foldout(_foldouts[nodeId], 
                        string.IsNullOrEmpty(nodeTitleProperty.stringValue) ? "<Unnamed State>" : nodeTitleProperty.stringValue, true);
                    GUILayout.EndHorizontal();
                    EditorGUI.indentLevel = previousIndent;

                    if (_foldouts[nodeId])
                    {
                        EditorGUI.indentLevel++;
                        
                        // Draw state type info
                        var stateTypeNameProperty = nodeProperty.FindPropertyRelative("stateTypeName");
                        var hasStateType = stateTypeNameProperty != null && !string.IsNullOrEmpty(stateTypeNameProperty.stringValue);
                        
                        if (hasStateType)
                        {
                            var stateType = Type.GetType(stateTypeNameProperty.stringValue);
                            EditorGUILayout.LabelField("Type", stateType != null ? stateType.Name : stateTypeNameProperty.stringValue);
                        }
                        
                        // Draw state variables
                        var node = sm.Nodes.Find(n => n.id == nodeId);
                        StateDefinition stateInstance = null;
                        
                        // Check runtime object first (most reliable)
                        if (node != null && node.state != null)
                        {
                            stateInstance = node.state;
                        }
                        // Then check SerializedProperty
                        else if (stateProperty != null)
                        {
                            stateInstance = stateProperty.managedReferenceValue as StateDefinition;
                        }
                        
                        // If we have a type name but no instance, create it
                        if (hasStateType && stateInstance == null)
                        {
                            var stateType = Type.GetType(stateTypeNameProperty.stringValue);
                            if (stateType != null)
                            {
                                try
                                {
                                    stateInstance = (StateDefinition)System.Activator.CreateInstance(stateType);
                                    
                                    // Set in both places
                                    if (node != null)
                                    {
                                        node.state = stateInstance;
                                        EditorUtility.SetDirty(sm);
                                    }
                                    if (stateProperty != null)
                                    {
                                        stateProperty.managedReferenceValue = stateInstance;
                                        serializedObject.ApplyModifiedProperties();
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogWarning($"Failed to create state instance: {ex.Message}");
                                }
                            }
                        }
                        
                        // Draw the state
                        if (stateInstance != null)
                        {
                            // Always try SerializedProperty first if available
                            if (stateProperty != null)
                            {
                                var propValue = stateProperty.managedReferenceValue as StateDefinition;
                                if (propValue != null)
                                {
                                    DrawStateVariables(stateProperty);
                                }
                                else
                                {
                                    // Property exists but value is null, draw directly and try to sync
                                    DrawStateVariablesDirect(stateInstance, stateProperty);
                                    // Try to sync it back
                                    stateProperty.managedReferenceValue = stateInstance;
                                    serializedObject.ApplyModifiedProperties();
                                }
                            }
                            else
                            {
                                DrawStateVariablesDirect(stateInstance);
                            }
                        }
                        else if (hasStateType)
                        {
                            EditorGUILayout.HelpBox("State type selected but instance not created. Re-select the state type in the graph window.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No state assigned. Select a state type in the graph window.", MessageType.Info);
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStateVariables(SerializedProperty stateProperty)
        {
            if (stateProperty == null) return;
            
            // Try to get the managed reference value
            var stateValue = stateProperty.managedReferenceValue;
            if (stateValue == null) return;
            
            DrawStateVariablesDirect(stateValue as StateDefinition, stateProperty);
        }
        
        private void DrawStateVariablesDirect(StateDefinition state, SerializedProperty stateProperty = null)
        {
            if (state == null) return;
            
            var stateType = state.GetType();
            var fields = stateType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(StateVariableAttribute), true).Length == 0) 
                { 
                    continue; 
                }

                // Try to use SerializedProperty if available
                if (stateProperty != null)
                {
                    var fieldProperty = stateProperty.FindPropertyRelative(field.Name);
                    if (fieldProperty != null)
                    {
                        EditorGUILayout.PropertyField(fieldProperty, new GUIContent(ObjectNames.NicifyVariableName(field.Name)), true);
                        continue;
                    }
                }
                
                // Fallback: use reflection to get/set values
                var value = field.GetValue(state);
                var fieldType = field.FieldType;
                
                EditorGUI.BeginChangeCheck();
                object newValue = null;
                
                if (fieldType == typeof(int))
                {
                    newValue = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(field.Name), (int)value);
                }
                else if (fieldType == typeof(float))
                {
                    newValue = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(field.Name), (float)value);
                }
                else if (fieldType == typeof(string))
                {
                    newValue = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(field.Name), (string)value);
                }
                else if (fieldType == typeof(bool))
                {
                    newValue = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(field.Name), (bool)value);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {
                    newValue = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), value as UnityEngine.Object, fieldType, true);
                }
                else
                {
                    // For other types, just display as read-only
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), value != null ? value.ToString() : "null");
                    EditorGUI.EndDisabledGroup();
                }
                
                if (EditorGUI.EndChangeCheck() && newValue != null)
                {
                    field.SetValue(state, newValue);
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}