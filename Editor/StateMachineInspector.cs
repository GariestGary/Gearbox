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
        private readonly Dictionary<string, bool> _foldouts = new();
        private SerializedProperty _statesProperty;
        private SerializedProperty _initializeOnStartProperty;
        private SerializedProperty _initialStateProperty;

        private void OnEnable()
        {
            _statesProperty = serializedObject.FindProperty("_states");
            _initializeOnStartProperty = serializedObject.FindProperty("_initializeOnStart");
            _initialStateProperty = serializedObject.FindProperty("_initialState");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(8);
            
            if (_initializeOnStartProperty != null)
            {
                _initializeOnStartProperty.boolValue = EditorGUILayout.Toggle("Initialize On Start", _initializeOnStartProperty.boolValue);
            }
            
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            if (GUILayout.Button("Add State"))
            {
                AddNewState(_statesProperty);
            }

            EditorGUILayout.Space();

            if (_statesProperty != null)
            {
                for (int i = 0; i < _statesProperty.arraySize; i++)
                {
                    DrawStateElement(_statesProperty, i);
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
            var instanceProperty = stateProperty.FindPropertyRelative("Instance");

            var foldoutId = $"state_{index}";
            _foldouts.TryAdd(foldoutId, true);

            // Safe state name handling
            var stateName = "State " + (index + 1);
            if (nameProperty != null && !string.IsNullOrEmpty(nameProperty.stringValue))
            {
                stateName = nameProperty.stringValue;
            }

            // Check if this state is the initial state (only if instance exists)
            var isInitialState = instanceProperty != null && IsStateInitialState(instanceProperty);

            // Create custom style with yellowish background for initial state
            var boxStyle = new GUIStyle(EditorStyles.helpBox);
            var color = GUI.color;
            
            if (isInitialState)
            {
                GUI.color = Color.yellow;
            }

            EditorGUILayout.BeginVertical(boxStyle);
            GUI.color = color;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;

            // Draw foldout with "Initial" label if this is the initial state
            _foldouts[foldoutId] = EditorGUILayout.Foldout(_foldouts[foldoutId], stateName, true);
            
            if (isInitialState)
            {
                var originalColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.7f); // Semi-transparent
                EditorGUILayout.LabelField("Initial", EditorStyles.boldLabel, GUILayout.Width(50));
                GUI.color = originalColor;
            }
            
            GUILayout.FlexibleSpace(); // Add flexible space to push button to the right

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
                if (nameProperty != null)
                {
                    EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
                }

                // Type dropdown
                DrawStateTypeDropdown(stateProperty);

                // Set as initial state button (only if instance exists)
                if (instanceProperty != null)
                {
                    DrawSetInitialStateButton(instanceProperty);
                }

                // State instance properties
                DrawStateInstanceProperties(stateProperty);

            }
            
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private bool IsStateInitialState(SerializedProperty instanceProperty)
        {
            if (_initialStateProperty == null || instanceProperty == null) return false;
            
            var currentInitialState = _initialStateProperty.managedReferenceValue;
            var thisStateInstance = instanceProperty.managedReferenceValue;
            
            return currentInitialState != null && thisStateInstance != null &&
                   currentInitialState.Equals(thisStateInstance);
        }

        private void DrawSetInitialStateButton(SerializedProperty instanceProperty)
        {
            if (instanceProperty == null || instanceProperty.managedReferenceValue == null) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15); // Match indentation
            
            var isInitialState = IsStateInitialState(instanceProperty);
            var buttonText = isInitialState ? "✓ Initial State" : "Set as Initial State";
            var buttonStyle = EditorStyles.miniButton;
            
            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(120)))
            {
                SetStateAsInitial(instanceProperty);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void SetStateAsInitial(SerializedProperty instanceProperty)
        {
            if (_initialStateProperty == null || instanceProperty == null) return;

            if (instanceProperty.managedReferenceValue is not StateDefinition stateInstance) return;
            
            _initialStateProperty.managedReferenceValue = stateInstance;
            serializedObject.ApplyModifiedProperties();
            
            // Force immediate repaint to show visual changes
            Repaint();
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
            if (statesProperty == null) return;

            statesProperty.InsertArrayElementAtIndex(statesProperty.arraySize);
            var newElementIndex = statesProperty.arraySize - 1;
            var newElement = statesProperty.GetArrayElementAtIndex(newElementIndex);
            
            var nameProperty = newElement.FindPropertyRelative("Name");
            var typeNameProperty = newElement.FindPropertyRelative("StateTypeName");

            if (nameProperty != null)
            {
                nameProperty.stringValue = $"State {statesProperty.arraySize}";
            }

            if (typeNameProperty != null)
            {
                typeNameProperty.stringValue = "";
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveState(SerializedProperty statesProperty, int index)
        {
            var stateProperty = statesProperty.GetArrayElementAtIndex(index);
            var instanceProperty = stateProperty.FindPropertyRelative("Instance");
            
            // If removing the initial state, clear the initial state reference
            if (_initialStateProperty != null && instanceProperty != null &&
                IsStateInitialState(instanceProperty))
            {
                _initialStateProperty.managedReferenceValue = null;
            }
            
            statesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
    }
}