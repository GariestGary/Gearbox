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
            _serializedPreferences = new SerializedObject(GearboxPreferences.Instance);
            _assemblyDefinitionsProperty = _serializedPreferences.FindProperty("_assemblyDefinitions");
        }

        public void DrawGUI()
        {
            _serializedPreferences.Update();

            EditorGUILayout.LabelField("State Type Scanning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Add assembly definition files (.asmdef) to limit where Gearbox searches for state implementations. This improves performance significantly.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_assemblyDefinitionsProperty, new GUIContent("Assembly Definitions"), true);
            EditorGUILayout.HelpBox("Assembly-CSharp is always included by default. Add additional .asmdef files to scan for more state types.", MessageType.Info);

            if (_serializedPreferences.hasModifiedProperties)
            {
                _serializedPreferences.ApplyModifiedProperties();
                GearboxTypeCache.ClearCache(); // Clear cache when preferences change
            }
        }
    }

    public class GearboxPreferencesProvider : SettingsProvider
    {
        private readonly GearboxPreferencesWindow _preferencesWindow;

        private GearboxPreferencesProvider(string path, SettingsScope scopes, System.Collections.Generic.IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            _preferencesWindow = ScriptableObject.CreateInstance<GearboxPreferencesWindow>();
        }

        public override void OnGUI(string searchContext)
        {
            _preferencesWindow.DrawGUI();
        }

        [SettingsProvider]
        public static SettingsProvider CreateGearboxPreferencesProvider()
        {
            return new GearboxPreferencesProvider("Preferences/Gearbox", SettingsScope.User);
        }
    }
}