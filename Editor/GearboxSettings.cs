using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VolumeBox.Gearbox.Editor
{
    [Serializable]
    public class GearboxSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Scripts/Gearbox/Editor/GearboxSettings.asset";
        private const string EditorPrefsKey = "Gearbox_SelectedAssemblyGUIDs";
        
        [SerializeField]
        private List<AssemblyDefinitionAsset> selectedAssemblyAssets = new List<AssemblyDefinitionAsset>();
        
        public List<AssemblyDefinitionAsset> SelectedAssemblyAssets => selectedAssemblyAssets;
        
        public List<string> SelectedAssemblyGUIDs
        {
            get
            {
                var guids = new List<string>();
                foreach (var asset in selectedAssemblyAssets)
                {
                    if (asset != null)
                    {
                        var path = AssetDatabase.GetAssetPath(asset);
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            guids.Add(guid);
                        }
                    }
                }
                return guids;
            }
        }
        
        private static GearboxSettings _instance;
        
        public static GearboxSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<GearboxSettings>(SettingsPath);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GearboxSettings>();
                        // Try to load from EditorPrefs as fallback (migration from old GUID-based system)
                        var guidsString = EditorPrefs.GetString(EditorPrefsKey, "");
                        if (!string.IsNullOrEmpty(guidsString))
                        {
                            var guids = guidsString.Split(';');
                            foreach (var guid in guids)
                            {
                                if (!string.IsNullOrEmpty(guid))
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(guid);
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                                        if (asset != null)
                                        {
                                            _instance.selectedAssemblyAssets.Add(asset);
                                        }
                                    }
                                }
                            }
                        }
                        AssetDatabase.CreateAsset(_instance, SettingsPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return _instance;
            }
        }
        
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            // Also save to EditorPrefs as backup
            var guids = SelectedAssemblyGUIDs;
            EditorPrefs.SetString(EditorPrefsKey, string.Join(";", guids));
        }
        
        public List<System.Reflection.Assembly> GetSelectedAssemblies()
        {
            var assemblies = new List<System.Reflection.Assembly>();
            var allLoadedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var asset in selectedAssemblyAssets)
            {
                if (asset == null) continue;
                
                var json = asset.text;
                var asmdefData = JsonUtility.FromJson<AssemblyDefinitionData>(json);
                if (asmdefData != null && !string.IsNullOrEmpty(asmdefData.name))
                {
                    // First try to find already loaded assembly
                    var assembly = allLoadedAssemblies.FirstOrDefault(a => a.GetName().Name == asmdefData.name);
                    
                    if (assembly == null)
                    {
                        // Try to load it
                        try
                        {
                            assembly = System.Reflection.Assembly.Load(asmdefData.name);
                        }
                        catch
                        {
                            // Assembly might not be compiled yet, skip it
                        }
                    }
                    
                    if (assembly != null)
                    {
                        assemblies.Add(assembly);
                    }
                }
            }
            return assemblies;
        }
        
        [Serializable]
        private class AssemblyDefinitionData
        {
            public string name;
        }
    }
}

