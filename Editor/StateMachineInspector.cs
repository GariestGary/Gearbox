using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    [CustomEditor(typeof(StateMachine))]
    public class StateMachineInspector : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private SerializedProperty _statesProperty;
        private SerializedProperty _initializeOnStartProperty;

        private void OnEnable()
        {
            _statesProperty = serializedObject.FindProperty("_states");
            _initializeOnStartProperty = serializedObject.FindProperty("_initializeOnStart");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sm = (StateMachine)target;

            GUILayout.Space(8);
            _initializeOnStartProperty.boolValue = EditorGUILayout.Toggle("Initialize On Start",  _initializeOnStartProperty.boolValue);
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            // Add State button
            if (GUILayout.Button("Add State"))
            {
                AddNewState();
            }

            EditorGUILayout.Space();

            if (_statesProperty != null)
            {
                for (int i = 0; i < _statesProperty.arraySize; i++)
                {
                    DrawStateElement(i);
                } 
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStateElement(int index)
        {
            var stateProperty = _statesProperty.GetArrayElementAtIndex(index);
            var nameProperty = stateProperty.FindPropertyRelative("Name");
            var stateTypeNameProperty = stateProperty.FindPropertyRelative("StateTypeName");
            var transitionsProperty = stateProperty.FindPropertyRelative("TransitionNames");

            var foldoutId = $"state_{index}";
            if (!_foldouts.ContainsKey(foldoutId)) { _foldouts[foldoutId] = true; }

            var stateName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"State {index + 1}" : nameProperty.stringValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;
            
            _foldouts[foldoutId] = EditorGUILayout.Foldout(_foldouts[foldoutId], stateName, true);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveState(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            if (_foldouts[foldoutId])
            {
                // Name field
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));

                // Type dropdown
                DrawStateTypeDropdown(stateProperty);

                // State instance properties
                DrawStateInstanceProperties(stateProperty);

                // Transitions
                EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
                DrawTransitionsList(transitionsProperty);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStateTypeDropdown(SerializedProperty stateProperty)
        {
            var stateTypeNameProperty = stateProperty.FindPropertyRelative("StateTypeName");
            var currentTypeName = stateTypeNameProperty.stringValue;
            var currentType = string.IsNullOrEmpty(currentTypeName) ? null : Type.GetType(currentTypeName);
            var displayName = currentType?.Name ?? "Select State Type";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Type");

            if (GUILayout.Button(displayName, EditorStyles.popup))
            {
                var dropdown = new StateTypeAdvancedDropdown(new AdvancedDropdownState(), selectedType =>
                {
                    stateTypeNameProperty.stringValue = selectedType.AssemblyQualifiedName;

                    // Create instance immediately when type is selected
                    CreateStateInstanceIfNeeded(stateProperty, selectedType);

                    serializedObject.ApplyModifiedProperties();
                });
                dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStateInstanceProperties(SerializedProperty stateProperty)
        {
            var stateTypeName = stateProperty.FindPropertyRelative("StateTypeName").stringValue;
            if (string.IsNullOrEmpty(stateTypeName)) return;

            var stateType = Type.GetType(stateTypeName);
            if (stateType == null) return;

            // Get the instance property
            var instanceProperty = stateProperty.FindPropertyRelative("Instance");

            // Create managed reference if needed
            if (instanceProperty.managedReferenceValue == null)
            {
                CreateStateInstanceIfNeeded(stateProperty, stateType);
            }

            var managedRef = instanceProperty.managedReferenceValue;
            if (managedRef != null && managedRef.GetType() == stateType)
            {
                // Draw all serializable fields
                var fields = stateType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length > 0)
                    {
                        var fieldProperty = instanceProperty.FindPropertyRelative(field.Name);
                        if (fieldProperty != null)
                        {
                            EditorGUILayout.PropertyField(fieldProperty, new GUIContent(ObjectNames.NicifyVariableName(field.Name)), true);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("State instance is not properly initialized.", MessageType.Warning);
            }
        }

        private void CreateStateInstanceIfNeeded(SerializedProperty stateProperty, Type stateType)
        {
            var instanceProperty = stateProperty.FindPropertyRelative("Instance");
            try
            {
                var managedRef = (StateDefinition)Activator.CreateInstance(stateType);
                instanceProperty.managedReferenceValue = managedRef;
                serializedObject.ApplyModifiedProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create state instance: {ex.Message}");
            }
        }

        private void DrawTransitionsList(SerializedProperty transitionsProperty)
        {
            var sm = (StateMachine)target;
            var stateNames = sm.States.Select(s => string.IsNullOrEmpty(s.Name) ? "Unnamed" : s.Name).ToArray();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Available States", EditorStyles.miniBoldLabel);

            for (int i = 0; i < transitionsProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Transition {i + 1}", GUILayout.Width(80));

                var transitionProperty = transitionsProperty.GetArrayElementAtIndex(i);
                var currentValue = transitionProperty.stringValue;
                var currentIndex = Array.IndexOf(stateNames, currentValue);

                EditorGUI.BeginChangeCheck();
                var newIndex = EditorGUILayout.Popup(currentIndex, stateNames);
                if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < stateNames.Length)
                {
                    transitionProperty.stringValue = stateNames[newIndex];
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    transitionsProperty.DeleteArrayElementAtIndex(i);
                    break; // Exit loop to avoid index issues
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Transition"))
            {
                transitionsProperty.InsertArrayElementAtIndex(transitionsProperty.arraySize);
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewState()
        {
            _statesProperty.InsertArrayElementAtIndex(_statesProperty.arraySize);
            var newElement = _statesProperty.GetArrayElementAtIndex(_statesProperty.arraySize - 1);
            newElement.FindPropertyRelative("Name").stringValue = $"State {_statesProperty.arraySize}";
            newElement.FindPropertyRelative("StateTypeName").stringValue = "";
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveState(int index)
        {
            _statesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        private Type[] GetStateDefinitionTypes()
        {
            return GearboxTypeCache.GetStateDefinitionTypes();
        }
    }
}