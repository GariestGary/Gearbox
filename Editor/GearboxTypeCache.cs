using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    public static class GearboxTypeCache
    {
        private static readonly Dictionary<string, Type[]> _cachedTypes = new();
        private static DateTime _lastCacheUpdate = DateTime.MinValue;

        public class StateTypeInfo
        {
            public Type Type { get; }
            public string CategoryPath { get; }
            public string[] CategoryParts { get; }

            public StateTypeInfo(Type type, string categoryPath)
            {
                Type = type;
                CategoryPath = categoryPath ?? "";
                CategoryParts = string.IsNullOrEmpty(CategoryPath) ? Array.Empty<string>() : CategoryPath.Split('/');
            }
        }

        public static Type[] GetStateDefinitionTypes()
        {
            return GetStateDefinitionTypesWithInfo().Select(info => info.Type).ToArray();
        }

        public static StateTypeInfo[] GetStateDefinitionTypesWithInfo()
        {
            // Cache for 5 seconds to avoid too frequent updates
            if ((DateTime.Now - _lastCacheUpdate).TotalSeconds < 5 && _cachedTypes.ContainsKey("StateDefinitions"))
            {
                return _cachedTypes["StateDefinitions"].Select(t => new StateTypeInfo(t, GetCategoryPath(t))).ToArray();
            }

            var preferences = GearboxPreferences.Instance;
            var stateTypes = new List<Type>();

            // Always include Assembly-CSharp
            var defaultAssembly = GetAssemblyByName("Assembly-CSharp");
            if (defaultAssembly != null)
            {
                AddStateTypesFromAssembly(defaultAssembly, stateTypes);
            }

            // Add types from specified assembly definitions
            foreach (var asmdef in preferences.AssemblyDefinitions)
            {
                if (asmdef == null) continue;

                // Get assembly name from asmdef
                var assemblyName = GetAssemblyNameFromAsmdef(asmdef);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    var assembly = GetAssemblyByName(assemblyName);
                    if (assembly != null)
                    {
                        AddStateTypesFromAssembly(assembly, stateTypes);
                    }
                }
            }

            var result = stateTypes.Distinct().OrderBy(t => t.Name).ToArray();
            _cachedTypes["StateDefinitions"] = result;
            _lastCacheUpdate = DateTime.Now;

            return result.Select(t => new StateTypeInfo(t, GetCategoryPath(t))).ToArray();
        }

        private static void AddStateTypesFromAssembly(Assembly assembly, List<Type> stateTypes)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(StateDefinition).IsAssignableFrom(t))
                    .ToArray();
                stateTypes.AddRange(types);
            }
            catch
            {
                // Skip assemblies that can't be loaded
            }
        }

        private static Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == name);
        }

        private static string GetAssemblyNameFromAsmdef(UnityEditorInternal.AssemblyDefinitionAsset asmdef)
        {
            try
            {
                // Parse the JSON to get the assembly name
                var json = asmdef.ToString();
                var data = JsonUtility.FromJson<AssemblyDefinitionData>(json);
                return data.name;
            }
            catch
            {
                return null;
            }
        }

        public static void ClearCache()
        {
            _cachedTypes.Clear();
            _lastCacheUpdate = DateTime.MinValue;
        }

        private static string GetCategoryPath(Type type)
        {
            var attribute = type.GetCustomAttribute<StateCategoryAttribute>();
            return attribute?.CategoryPath ?? "";
        }

        [Serializable]
        private class AssemblyDefinitionData
        {
            public string name;
        }
    }
}