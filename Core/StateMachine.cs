using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VolumeBox.Gearbox.Core
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField]
        private List<StateData> states = new List<StateData>();

        private StateDefinition currentState;

        public List<StateData> States => states;
        public StateDefinition CurrentState => currentState;

        private void Start()
        {
            InitializeStateMachine();
        }

        public async UniTask InitializeStateMachine()
        {
            // Clear current state
            currentState = null;

            // Instantiate state instances
            foreach (var stateData in states)
            {
                if (stateData.instance != null) continue;

                var stateType = stateData.GetStateType();
                if (stateType != null)
                {
                    try
                    {
                        stateData.instance = (StateDefinition)Activator.CreateInstance(stateType);
                        stateData.instance.StateMachine = this; // Set the reference to this StateMachine
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create instance of {stateType.Name}: {ex.Message}");
                    }
                }
            }

            // Set first state as initial if available
            if (states.Count > 0 && states[0].instance != null)
            {
                await EnterState(states[0].instance);
            }
            else
            {
                Debug.LogWarning("StateMachine has no valid states defined.");
            }
        }

        public async UniTask TransitionToState(StateDefinition targetState)
        {
            if (targetState == null)
            {
                Debug.LogError("Target state is null.");
                return;
            }

            var stateData = states.Find(s => s.instance == targetState);
            if (stateData == null)
            {
                Debug.LogError($"State '{targetState.GetType().Name}' is not part of this state machine.");
                return;
            }

            await PerformTransition(targetState);
        }

        public async UniTask TransitionToState(string stateName)
        {
            var stateData = states.Find(s => s.name == stateName);
            if (stateData == null || stateData.instance == null)
            {
                Debug.LogError($"State '{stateName}' not found or not initialized.");
                return;
            }

            await PerformTransition(stateData.instance);
        }

        public async UniTask TriggerTransition(StateDefinition fromState, int transitionIndex)
        {
            var stateData = states.Find(s => s.instance == fromState);
            if (stateData == null || transitionIndex < 0 || transitionIndex >= stateData.transitionNames.Count)
            {
                Debug.LogError("Invalid transition request.");
                return;
            }

            var targetStateName = stateData.transitionNames[transitionIndex];
            await TransitionToState(targetStateName);
        }

        public List<string> GetAvailableTransitions(StateDefinition state)
        {
            var stateData = states.Find(s => s.instance == state);
            return stateData?.transitionNames ?? new List<string>();
        }

        private async UniTask PerformTransition(StateDefinition targetState)
        {
            // Exit current state
            if (currentState != null)
            {
                try
                {
                    await currentState.OnExit();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during state exit for '{currentState.GetType().Name}': {ex.Message}");
                }
            }

            // Enter new state
            try
            {
                await targetState.OnEnter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during state enter for '{targetState.GetType().Name}': {ex.Message}");
            }

            currentState = targetState;
        }

        private async UniTask EnterState(StateDefinition state)
        {
            try
            {
                await state.OnEnter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during initial state enter for '{state.GetType().Name}': {ex.Message}");
            }
            currentState = state;
        }

        private async void Update()
        {
            if (currentState != null)
            {
                try
                {
                    await currentState.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{currentState.GetType().Name}': {ex.Message}");
                }
            }
        }

        // Public method for testing the update logic
        public async UniTask TestUpdate()
        {
            if (currentState != null)
            {
                try
                {
                    await currentState.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{currentState.GetType().Name}': {ex.Message}");
                }
            }
        }
    }
}
