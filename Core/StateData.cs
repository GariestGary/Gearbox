using System;
using System.Collections.Generic;
using UnityEngine;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public class StateData
    {
        public string Name;
        public string StateTypeName;
        public bool IsInitialState;
        public List<string> TransitionNames = new List<string>();

        [SerializeReference] public StateDefinition Instance;

        public Type GetStateType()
        {
            return string.IsNullOrEmpty(StateTypeName) ? null : Type.GetType(StateTypeName);
        }

        public void SetStateType(Type type)
        {
            StateTypeName = type?.AssemblyQualifiedName;
        }
    }
}