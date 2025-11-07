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
        public StateDefinition state;
    }
}


