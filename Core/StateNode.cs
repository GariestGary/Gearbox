using System;
using UnityEngine;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public class StateNode
    {
        public string id;
        public string title;
        public Vector2 position;
        
        [SerializeReference]
        public StateDefinition state;
        
        [SerializeField]
        private string stateTypeName;

        [SerializeField]
        private bool isInitialState;
        
        public string StateTypeName
        {
            get => stateTypeName;
            set => stateTypeName = value;
        }

        public bool IsInitialState
        {
            get => isInitialState;
            set => isInitialState = value;
        }
        
        public Type GetStateType()
        {
            if (string.IsNullOrEmpty(stateTypeName))
                return null;
                
            return Type.GetType(stateTypeName);
        }
        
        public void SetStateType(Type type)
        {
            if (type != null)
            {
                stateTypeName = type.AssemblyQualifiedName;
            }
            else
            {
                stateTypeName = null;
            }
        }
    }
}


