using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace VolumeBox.Gearbox.Editor
{
    public class GearboxPreferences : ScriptableObject
    {
        [SerializeField]
        private List<UnityEditorInternal.AssemblyDefinitionAsset> assemblyDefinitions = new List<UnityEditorInternal.AssemblyDefinitionAsset>();

        public List<UnityEditorInternal.AssemblyDefinitionAsset> AssemblyDefinitions => assemblyDefinitions;

        private static GearboxPreferences _instance;
        public static GearboxPreferences Instance
        {
            get
            {
                if (_instance == null)
                {
                    var path = "Assets/Editor/GearboxPreferences.asset";
                    _instance = AssetDatabase.LoadAssetAtPath<GearboxPreferences>(path);

                    if (_instance == null)
                    {
                        // Create directories if they don't exist
                        var directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                        {
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