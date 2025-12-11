using System;
using System.Collections.Generic;
using UnityEngine;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public class StateData
    {
        public bool IsInitial;

        [SerializeReference, SerializeReferenceDropdown] public StateDefinition Instance;
    }
}