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

        private void OnEnable()
        {
            _statesProperty = serializedObject.FindProperty("_states");
            _initializeOnStartProperty = serializedObject.FindProperty("_initializeOnStart");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Space(8);
            
            if (_initializeOnStartProperty != null)
            {
                EditorGUILayout.PropertyField(_initializeOnStartProperty);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

            if (_statesProperty != null)
            {
                for (int i = 0; i < _statesProperty.arraySize; i++)
                {
                    DrawStateElement(_statesProperty, i);
                }
            }
            
            if (GUILayout.Button("Add State", GUILayout.Height(40)))
            {
                AddNewState(_statesProperty);
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawStateElement(SerializedProperty statesProperty, int index)
        {
            var stateProperty = statesProperty.GetArrayElementAtIndex(index);
            var instanceProperty = stateProperty.FindPropertyRelative("Instance");

            var foldoutId = $"state_{index}";
            _foldouts.TryAdd(foldoutId, false);

            var instance = instanceProperty.managedReferenceValue as StateDefinition;
            
            // Safe state name handling
            var stateName = instance == null || string.IsNullOrEmpty(instance.Name) ? "Unnamed State" : instance.Name;

            // Check if this state is the initial state (only if instance exists)
            var isInitialState = IsStateInitialState(stateProperty);

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
            
            GUILayout.FlexibleSpace(); // Add flexible space to push button to the right

            color = GUI.color;
            GUI.color = Color.red;

            if (GUILayout.Button(EditorGUIUtility.IconContent("CrossIcon"), GUILayout.Width(15), GUILayout.Height(15)))
            {
                RemoveState(statesProperty, index);
                return;
            }
            
            GUI.color = color;

            EditorGUILayout.EndHorizontal();


            if (_foldouts[foldoutId])
            {
                DrawSetInitialStateButton(stateProperty);
                
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(instanceProperty);

                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
            }
            
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        private bool IsStateInitialState(SerializedProperty stateProperty)
        {
            return stateProperty != null && stateProperty.FindPropertyRelative("IsInitial").boolValue;
        }

        private void ValidateInitialState()
        {
            serializedObject.Update();
            int arraySize = _statesProperty.arraySize;

            if (arraySize <= 0)
            {
                return;
            }
    
            for (int i = 0; i < arraySize; i++)
            {
                var prop = _statesProperty.GetArrayElementAtIndex(i);
                
                if (!prop.FindPropertyRelative("IsInitial").boolValue) continue;
                
                SetStateAsInitial(prop);
                return;
            }

            SetStateAsInitial(_statesProperty.GetArrayElementAtIndex(0));
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        private void DrawSetInitialStateButton(SerializedProperty stateProperty)
        {
            if (stateProperty == null) return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15); // Match indentation
            
            var isInitialState = IsStateInitialState(stateProperty);
            var buttonText = isInitialState ? "✓ Initial State" : "Set as Initial State";
            var buttonStyle = isInitialState ? EditorStyles.label : EditorStyles.miniButton;
            
            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(120)))
            {
                SetStateAsInitial(stateProperty);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void SetStateAsInitial(SerializedProperty stateProperty)
        {
            // Ensure we're working with the latest data
            serializedObject.Update();
    
            // Get the array size
            int arraySize = _statesProperty.arraySize;
    
            // Iterate through all states
            for (int i = 0; i < arraySize; i++)
            {
                var prop = _statesProperty.GetArrayElementAtIndex(i);
                var isInitialProp = prop.FindPropertyRelative("IsInitial");
        
                if (isInitialProp != null)
                {
                    // Set true only for the matching property, false for others
                    bool isTarget = SerializedProperty.EqualContents(stateProperty, prop);
                    isInitialProp.boolValue = isTarget;
                }
            }
    
            // Apply modifications
            serializedObject.ApplyModifiedProperties();
    
            // Force immediate repaint to show visual changes
            Repaint();
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
            ValidateInitialState();
        }

        private void RemoveState(SerializedProperty statesProperty, int index)
        {
            statesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            ValidateInitialState();
        }
    }
}