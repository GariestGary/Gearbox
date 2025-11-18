using System;
using System.Collections.Generic;
using System.Reflection;
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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var statesProperty = serializedObject.FindProperty("_states");
            var initializeOnStartProperty = serializedObject.FindProperty("_initializeOnStart");

            GUILayout.Space(8);
            if (initializeOnStartProperty != null)
            {
                initializeOnStartProperty.boolValue = EditorGUILayout.Toggle("Initialize On Start", initializeOnStartProperty.boolValue);
            }
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            // Add State button
            if (GUILayout.Button("Add State"))
            {
                if (statesProperty != null)
                {
                    AddNewState(statesProperty);
                }
            }

            EditorGUILayout.Space();

            if (statesProperty != null)
            {
                for (int i = 0; i < statesProperty.arraySize; i++)
                {
                    DrawStateElement(statesProperty, i);
                } 
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStateElement(SerializedProperty statesProperty, int index)
        {
            var stateProperty = statesProperty.GetArrayElementAtIndex(index);
            var nameProperty = stateProperty.FindPropertyRelative("Name");
            stateProperty.FindPropertyRelative("StateTypeName");

            var foldoutId = $"state_{index}";
            
            _foldouts.TryAdd(foldoutId, true);

            var stateName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"State {index + 1}" : nameProperty.stringValue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;

            _foldouts[foldoutId] = EditorGUILayout.Foldout(_foldouts[foldoutId], stateName, true);
            GUILayout.FlexibleSpace(); // Add flexible space to push checkbox and button to the right
            DrawInitialStateCheckbox(statesProperty, index, stateProperty);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveState(statesProperty, index);
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

            }
            
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private void DrawInitialStateCheckbox(SerializedProperty statesProperty, int index, SerializedProperty stateProperty)
        {
            var isInitialStateProperty = stateProperty.FindPropertyRelative("IsInitialState");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Initial State", EditorStyles.boldLabel, GUILayout.Width(85));
            var newValue = EditorGUILayout.Toggle(isInitialStateProperty.boolValue, GUILayout.Width(32));
            
            if (!EditorGUI.EndChangeCheck()) return;
            
            // If enabling this checkbox, disable all others
            if (newValue)
            {
                if (statesProperty != null)
                {
                    for (int i = 0; i < statesProperty.arraySize; i++)
                    {
                        if (i == index) continue;

                        var otherStateProperty = statesProperty.GetArrayElementAtIndex(i);
                        var otherIsInitialProperty = otherStateProperty.FindPropertyRelative("IsInitialState");
                        otherIsInitialProperty.boolValue = false;
                    }
                }
            }
            
            isInitialStateProperty.boolValue = newValue;
            serializedObject.ApplyModifiedProperties();
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
                    var oldTypeName = stateTypeNameProperty.stringValue;
                    stateTypeNameProperty.stringValue = selectedType.AssemblyQualifiedName;

                    // Only create a new instance if the type actually changed
                    if (oldTypeName != selectedType.AssemblyQualifiedName)
                    {
                        CreateStateInstanceIfNeeded(stateProperty, selectedType);
                    }

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
                    if (!field.IsPublic && field.GetCustomAttributes(typeof(SerializeField), true).Length <= 0)
                    {
                        continue;
                    }
                    
                    var fieldProperty = instanceProperty.FindPropertyRelative(field.Name);
                    if (fieldProperty != null)
                    {
                        EditorGUILayout.PropertyField(fieldProperty, new GUIContent(ObjectNames.NicifyVariableName(field.Name)), true);
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


        private void AddNewState(SerializedProperty statesProperty)
        {
            statesProperty.InsertArrayElementAtIndex(statesProperty.arraySize);
            var newElementIndex = statesProperty.arraySize - 1;
            var newElement = statesProperty.GetArrayElementAtIndex(newElementIndex);
            newElement.FindPropertyRelative("Name").stringValue = $"State {statesProperty.arraySize}";
            newElement.FindPropertyRelative("StateTypeName").stringValue = "";

            var newIsInitialProperty = newElement.FindPropertyRelative("IsInitialState");
            var hasInitialState = HasInitialState(statesProperty, newElementIndex);
            newIsInitialProperty.boolValue = !hasInitialState;

            // Ensure only one initial state exists
            if (!hasInitialState)
            {
                SetInitialState(statesProperty, newElementIndex);
            }
            else
            {
                newIsInitialProperty.boolValue = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool HasInitialState(SerializedProperty statesProperty, int skipIndex = -1)
        {
            for (int i = 0; i < statesProperty.arraySize; i++)
            {
                if (i == skipIndex) continue;
                var state = statesProperty.GetArrayElementAtIndex(i);
                var isInitial = state.FindPropertyRelative("IsInitialState");
                if (isInitial != null && isInitial.boolValue)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetInitialState(SerializedProperty statesProperty, int indexToEnable)
        {
            for (int i = 0; i < statesProperty.arraySize; i++)
            {
                var state = statesProperty.GetArrayElementAtIndex(i);
                var isInitial = state.FindPropertyRelative("IsInitialState");
                if (isInitial == null) continue;
                isInitial.boolValue = i == indexToEnable;
            }
        }

        private void RemoveState(SerializedProperty statesProperty, int index)
        {
            statesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }

        private Type[] GetStateDefinitionTypes()
        {
            return GearboxTypeCache.GetStateDefinitionTypes();
        }
    }
}