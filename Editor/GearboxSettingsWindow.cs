using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VolumeBox.Gearbox.Editor
{
    public class GearboxSettingsWindow : EditorWindow
    {
        private GearboxSettings _settings;
        private SerializedObject _serializedSettings;
        private SerializedProperty _assemblyGUIDsProperty;
        private ReorderableList _reorderableList;
        
        [MenuItem("Tools/Gearbox/Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<GearboxSettingsWindow>("Gearbox Settings");
            window.Show();
        }
        
        private void OnEnable()
        {
            _settings = GearboxSettings.Instance;
            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
                _assemblyGUIDsProperty = _serializedSettings.FindProperty("selectedAssemblyAssets");
                
                _reorderableList = new ReorderableList(_serializedSettings, _assemblyGUIDsProperty, true, true, true, true);
                _reorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Assembly Definitions");
                };
                
                _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = _assemblyGUIDsProperty.GetArrayElementAtIndex(index);
                    
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                };
                
                _reorderableList.onAddCallback = (ReorderableList l) =>
                {
                    _serializedSettings.Update();
                    _assemblyGUIDsProperty.arraySize++;
                    _assemblyGUIDsProperty.GetArrayElementAtIndex(_assemblyGUIDsProperty.arraySize - 1).objectReferenceValue = null;
                    _serializedSettings.ApplyModifiedProperties();
                    _settings.Save();
                };
                
                _reorderableList.onRemoveCallback = (ReorderableList l) =>
                {
                    _serializedSettings.Update();
                    _assemblyGUIDsProperty.DeleteArrayElementAtIndex(l.index);
                    _serializedSettings.ApplyModifiedProperties();
                    _settings.Save();
                };
                
                _reorderableList.onCanRemoveCallback = (ReorderableList l) =>
                {
                    return l.count > 0;
                };
                
                _reorderableList.onReorderCallback = (ReorderableList l) =>
                {
                    _serializedSettings.ApplyModifiedProperties();
                    _settings.Save();
                };
                
                _reorderableList.onChangedCallback = (ReorderableList l) =>
                {
                    _serializedSettings.ApplyModifiedProperties();
                    _settings.Save();
                };
            }
        }
        
        private void OnGUI()
        {
            if (_settings == null || _serializedSettings == null)
            {
                EditorGUILayout.HelpBox("Failed to load settings.", MessageType.Error);
                return;
            }
            
            _serializedSettings.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Assembly Definition Selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select assembly definitions to search for state types. Drag & drop .asmdef files or use the + button to add them.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            _reorderableList.DoLayoutList();
            
            _serializedSettings.ApplyModifiedProperties();
        }
    }
}

