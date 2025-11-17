using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VolumeBox.Gearbox.Core
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private List<StateData> _states = new();
        [SerializeField] private bool _initializeOnStart = true;

        private StateDefinition _currentStateInstance;
        private Action<StateDefinition> _stateInitializeAction;

        public List<StateData> States => _states;
        public StateDefinition CurrentState => _currentStateInstance;

        private void Start()
        {
            if (_initializeOnStart)
            {
                InitializeStateMachine().Forget();
            }
        }

        public void SetStateInitializeAction(Action<StateDefinition> action)
        {
            _stateInitializeAction = action;
        }

        public async UniTask InitializeStateMachine()
        {
            // Clear current state
            _currentStateInstance = null;

            // Instantiate state instances
            foreach (var stateData in _states)
            {
                var stateType = stateData.GetStateType();

                if (stateType == null) continue;

                try
                {
                    // Use existing instance if available (created in editor), otherwise create new one
                    if (stateData.Instance == null)
                    {
                        stateData.Instance = (StateDefinition)Activator.CreateInstance(stateType);
                    }

                    stateData.Instance.StateMachine = this; // Set the reference to this StateMachine

                    _stateInitializeAction?.Invoke(stateData.Instance);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of {stateType.Name}: {ex.Message}");
                }
            }

            // Set first state as initial if available
            if (_states.Count > 0 && _states[0].Instance != null)
            {
                await EnterState(_states[0].Instance);
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

            var stateData = _states.Find(s => s.Instance == targetState);
            if (stateData == null)
            {
                Debug.LogError($"State '{targetState.GetType().Name}' is not part of this state machine.");
                return;
            }

            await PerformTransition(targetState);
        }

        public async UniTask TransitionToState(string stateName)
        {
            var stateData = _states.Find(s => s.Name == stateName);
            if (stateData == null || stateData.Instance == null)
            {
                Debug.LogError($"State '{stateName}' not found or not initialized.");
                return;
            }

            await PerformTransition(stateData.Instance);
        }

        public async UniTask TriggerTransition(StateDefinition fromState, int transitionIndex)
        {
            var stateData = _states.Find(s => s.Instance == fromState);
            if (stateData == null || transitionIndex < 0 || transitionIndex >= stateData.TransitionNames.Count)
            {
                Debug.LogError("Invalid transition request.");
                return;
            }

            var targetStateName = stateData.TransitionNames[transitionIndex];
            await TransitionToState(targetStateName);
        }

        public List<string> GetAvailableTransitions(StateDefinition state)
        {
            var stateData = _states.Find(s => s.Instance == state);
            return stateData?.TransitionNames ?? new List<string>();
        }

        private async UniTask PerformTransition(StateDefinition targetState)
        {
            // Exit current state
            if (_currentStateInstance != null)
            {
                try
                {
                    await _currentStateInstance.OnExit();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during state exit for '{_currentStateInstance.GetType().Name}': {ex.Message}");
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

            _currentStateInstance = targetState;
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
            _currentStateInstance = state;
        }

        private async void Update()
        {
            if (_currentStateInstance != null)
            {
                try
                {
                    await _currentStateInstance.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{_currentStateInstance.GetType().Name}': {ex.Message}");
                }
            }
        }

        // Public method for testing the update logic
        public async UniTask TestUpdate()
        {
            if (_currentStateInstance != null)
            {
                try
                {
                    await _currentStateInstance.OnUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in state update for '{_currentStateInstance.GetType().Name}': {ex.Message}");
                }
            }
        }
    }
}
