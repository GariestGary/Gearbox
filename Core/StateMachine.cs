using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VolumeBox.Gearbox.Core
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField]
        private List<StateNode> nodes = new List<StateNode>();

        [SerializeField]
        private List<StateTransition> transitions = new List<StateTransition>();

        private StateNode currentState;
        private StateNode initialState;

        public List<StateNode> Nodes => nodes;
        public List<StateTransition> Transitions => transitions;
        public StateNode CurrentState => currentState;

        private void Start()
        {
            InitializeStateMachine();
        }

        public async UniTask InitializeStateMachine()
        {
            // Clear current state
            currentState = null;
            
            // Find initial state
            initialState = nodes.Find(n => n.IsInitialState);
            if (initialState == null && nodes.Count > 0)
            {
                initialState = nodes[0];
                Debug.LogWarning($"No initial state set. Using first state '{initialState.title}' as initial state.");
            }

            if (initialState != null)
            {
                await EnterState(initialState);
            }
            else if (nodes.Count == 0)
            {
                Debug.LogWarning("StateMachine has no states defined.");
            }
        }

        public async UniTask TransitionToState(string stateId)
        {
            var targetNode = nodes.Find(n => n.id == stateId);
            if (targetNode == null)
            {
                Debug.LogError($"State with ID '{stateId}' not found.");
                return;
            }

            await TransitionToState(targetNode);
        }

        public async UniTask TransitionToState(StateNode targetNode)
        {
            // Check if transition is valid
            if (currentState != null)
            {
                var hasTransition = transitions.Exists(t => t.fromId == currentState.id && t.toId == targetNode.id);
                if (!hasTransition)
                {
                    Debug.LogWarning($"No transition exists from '{currentState.title}' to '{targetNode.title}'. Transition blocked.");
                    return;
                }
            }

            await PerformTransition(targetNode);
        }

        private async UniTask PerformTransition(StateNode targetNode)
        {
            // Exit current state
            if (currentState != null && currentState.state != null)
            {
                try
                {
                    await currentState.state.OnExit();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during state exit for '{currentState.title}': {ex.Message}");
                }
            }

            // Enter new state
            if (targetNode.state != null)
            {
                try
                {
                    await targetNode.state.OnEnter();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during state enter for '{targetNode.title}': {ex.Message}");
                }
            }

            currentState = targetNode;
        }

        private async UniTask EnterState(StateNode state)
        {
            if (state.state != null)
            {
                try
                {
                    await state.state.OnEnter();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during initial state enter for '{state.title}': {ex.Message}");
                }
            }
            currentState = state;
        }

        private async void Update()
        {
            if (currentState != null && currentState.state != null)
            {
                try
                {
                    await currentState.state.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{currentState.title}': {ex.Message}");
                }
            }
        }

        // Public method for testing the update logic
        public async UniTask TestUpdate()
        {
            if (currentState != null && currentState.state != null)
            {
                try
                {
                    await currentState.state.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{currentState.title}': {ex.Message}");
                }
            }
        }
    }
}
