using UnityEngine;
using System;
using System.Collections.Generic;

namespace VolumeBox.Gearbox.Core
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField]
        private List<StateNode> nodes = new List<StateNode>();

        [SerializeField]
        private List<StateTransition> transitions = new List<StateTransition>();

        public List<StateNode> Nodes => nodes;
        public List<StateTransition> Transitions => transitions;
    }
}
