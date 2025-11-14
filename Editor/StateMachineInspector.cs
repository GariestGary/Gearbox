using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
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
            var nameProperty = stateProperty.FindPropertyRelative("name");
            var stateTypeNameProperty = stateProperty.FindPropertyRelative("stateTypeName");
            var transitionsProperty = stateProperty.FindPropertyRelative("transitionNames");

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
                DrawStateTypeDropdown(stateTypeNameProperty);

                // State instance properties
                DrawStateInstanceProperties(stateProperty);

                // Transitions
                EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
                DrawTransitionsList(transitionsProperty);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStateTypeDropdown(SerializedProperty stateTypeNameProperty)
        {
            var types = GetStateDefinitionTypes();
            var typeNames = types.Select(t => t.Name).ToArray();
            var currentTypeName = stateTypeNameProperty.stringValue;
            var currentIndex = string.IsNullOrEmpty(currentTypeName) ? -1 :
                Array.IndexOf(typeNames, Type.GetType(currentTypeName)?.Name ?? "");

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup("Type", currentIndex, typeNames);
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < types.Length)
            {
                stateTypeNameProperty.stringValue = types[newIndex].AssemblyQualifiedName;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStateInstanceProperties(SerializedProperty stateProperty)
        {
            var stateTypeName = stateProperty.FindPropertyRelative("stateTypeName").stringValue;
            if (string.IsNullOrEmpty(stateTypeName)) return;

            var stateType = Type.GetType(stateTypeName);
            if (stateType == null) return;

            // Get the instance property
            var instanceProperty = stateProperty.FindPropertyRelative("instance");

            // Create managed reference if needed
            var managedRef = instanceProperty.managedReferenceValue;
            if (managedRef == null || managedRef.GetType() != stateType)
            {
                try
                {
                    managedRef = (StateDefinition)Activator.CreateInstance(stateType);
                    instanceProperty.managedReferenceValue = managedRef;
                    serializedObject.ApplyModifiedProperties();
                }
                catch (Exception ex)
                {
                    EditorGUILayout.HelpBox($"Failed to create state instance: {ex.Message}", MessageType.Error);
                    return;
                }
            }

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
            newElement.FindPropertyRelative("name").stringValue = $"State {_statesProperty.arraySize}";
            newElement.FindPropertyRelative("stateTypeName").stringValue = "";
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