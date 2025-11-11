using System;
using System.Collections.Generic;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public class StateData
    {
        public string name;
        public string stateTypeName;
        public List<string> transitionNames = new List<string>();

        [NonSerialized]
        public StateDefinition instance;

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