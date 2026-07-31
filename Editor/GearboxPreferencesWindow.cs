using UnityEditor;
using UnityEngine;

namespace VolumeBox.Gearbox.Editor
{
    public class GearboxPreferencesWindow : EditorWindow
    {
        private SerializedObject _serializedPreferences;
        private SerializedProperty _assemblyDefinitionsProperty;

        public static void ShowWindow()
        {
            var window = GetWindow<GearboxPreferencesWindow>("Gearbox Preferences");
            window.Show();
        }

        private void OnEnable()
        {
            InitializeSerializedProperties();
        }

        private void OnGUI()
        {
            InitializeSerializedProperties();
            DrawGUI(_serializedPreferences, _assemblyDefinitionsProperty);
        }

        private void InitializeSerializedProperties()
        {
            if (_serializedPreferences != null && _serializedPreferences.targetObject != null)
            {
                return;
            }

            _serializedPreferences = new SerializedObject(GearboxPreferences.Instance);
            _assemblyDefinitionsProperty = _serializedPreferences.FindProperty("_assemblyDefinitions");
        }

        internal static void DrawGUI(SerializedObject serializedPreferences, SerializedProperty assemblyDefinitionsProperty)
        {
            serializedPreferences.Update();

            EditorGUILayout.LabelField("State Type Scanning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Add assembly definition files (.asmdef) to limit where Gearbox searches for state implementations. This improves performance significantly.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(assemblyDefinitionsProperty, new GUIContent("Assembly Definitions"), true);
            EditorGUILayout.HelpBox("Assembly-CSharp is always included by default. Add additional .asmdef files to scan for more state types.", MessageType.Info);

            if (serializedPreferences.hasModifiedProperties)
            {
                serializedPreferences.ApplyModifiedProperties();
            }
        }
    }

    public class GearboxPreferencesProvider : SettingsProvider
    {
        private SerializedObject _serializedPreferences;
        private SerializedProperty _assemblyDefinitionsProperty;

        private GearboxPreferencesProvider(string path, SettingsScope scopes, System.Collections.Generic.IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            _serializedPreferences = new SerializedObject(GearboxPreferences.Instance);
            _assemblyDefinitionsProperty = _serializedPreferences.FindProperty("_assemblyDefinitions");
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedPreferences == null || _serializedPreferences.targetObject == null)
            {
                _serializedPreferences = new SerializedObject(GearboxPreferences.Instance);
                _assemblyDefinitionsProperty = _serializedPreferences.FindProperty("_assemblyDefinitions");
            }

            GearboxPreferencesWindow.DrawGUI(_serializedPreferences, _assemblyDefinitionsProperty);
        }

        [SettingsProvider]
        public static SettingsProvider CreateGearboxPreferencesProvider()
        {
            return new GearboxPreferencesProvider("Preferences/Gearbox", SettingsScope.User);
        }
    }
}
