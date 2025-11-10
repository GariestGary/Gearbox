using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VolumeBox.Gearbox.Editor
{
    public class TypeDropdown
    {
        private List<Type> types;
        private string[] typeNames;
        public int selectedIndex;
        private Type baseType;
    
        public Type SelectedType => types != null && types.Count > 0 && selectedIndex >= 0 && selectedIndex < types.Count ? types[selectedIndex] : null;
    
        public TypeDropdown(Type baseType = null)
        {
            this.baseType = baseType ?? typeof(MonoBehaviour);
            RefreshTypes();
        }
    
        public void RefreshTypes()
        {
            types = GetInheritedTypes(baseType);
            typeNames = types.Select(t => t.FullName).ToArray();
            selectedIndex = types.Count > 0 ? 0 : -1;
        }
    
        public void Draw(string label = "Type")
        {
            if (typeNames == null || typeNames.Length == 0)
            {
                EditorGUILayout.HelpBox($"No types found inheriting from {baseType.Name}", MessageType.Info);
                return;
            }
        
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, typeNames);
            if (!newIndex.Equals(selectedIndex))
            {
                selectedIndex = newIndex;
            }
        }
    
        public void SetBaseType(Type newBaseType)
        {
            baseType = newBaseType;
            RefreshTypes();
        }
        
        public static List<Type> GetInheritedTypes(Type baseType)
        {
            var settings = GearboxSettings.Instance;
            var selectedAssemblies = settings.GetSelectedAssemblies();
            
            // Always include Assembly-CSharp
            try
            {
                var assemblyCSharp = System.Reflection.Assembly.Load("Assembly-CSharp");
                if (assemblyCSharp != null && !selectedAssemblies.Contains(assemblyCSharp))
                {
                    selectedAssemblies.Add(assemblyCSharp);
                }
            }
            catch
            {
                // Assembly-CSharp might not exist
            }
            
            if (selectedAssemblies.Count == 0)
            {
                return new List<Type>();
            }
            
            return selectedAssemblies
                .SelectMany(assembly => 
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(type => 
                    baseType.IsAssignableFrom(type) && 
                    !type.IsAbstract && 
                    type != baseType)
                .ToList();
        }
    }
}