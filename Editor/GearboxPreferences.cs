using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VolumeBox.Gearbox.Editor
{
    public class GearboxPreferences : ScriptableObject
    {
        [SerializeField]
        private List<UnityEditorInternal.AssemblyDefinitionAsset> _assemblyDefinitions = new();

        public List<UnityEditorInternal.AssemblyDefinitionAsset> AssemblyDefinitions => _assemblyDefinitions;

        private static GearboxPreferences _instance;
        
        public static GearboxPreferences Instance
        {
            get
            {
                if (!_instance)
                {
                    var path = "Assets/Editor/GearboxPreferences.asset";
                    _instance = AssetDatabase.LoadAssetAtPath<GearboxPreferences>(path);

                    if (!_instance)
                    {
                        // Create directories if they don't exist
                        var directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                        {
                            if (directory != null) 
                                System.IO.Directory.CreateDirectory(directory);
                        }

                        _instance = CreateInstance<GearboxPreferences>();
                        AssetDatabase.CreateAsset(_instance, path);
                        AssetDatabase.SaveAssets();
                    }
                }
                return _instance;
            }
        }
    }
}