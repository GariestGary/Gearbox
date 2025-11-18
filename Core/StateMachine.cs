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

            // Set initial state if available
            var initialState = _states.Find(s => s.IsInitialState && s.Instance != null);
            if (initialState == null && _states.Count > 0)
            {
                // Fallback to first state if no initial state is marked
                initialState = _states.Find(s => s.Instance != null);
            }

            if (initialState != null)
            {
                await EnterState(initialState.Instance);
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
            var matchingStates = _states.FindAll(s => s.Name == stateName && s.Instance != null);
            if (matchingStates.Count == 0)
            {
                Debug.LogError($"State '{stateName}' not found or not initialized.");
                return;
            }

            // If multiple states have the same name, select one randomly
            var selectedState = matchingStates.Count == 1
                ? matchingStates[0]
                : matchingStates[UnityEngine.Random.Range(0, matchingStates.Count)];

            await PerformTransition(selectedState.Instance);
        }

        public async UniTask TransitionToState<T>() where T : StateDefinition
        {
            await TransitionToState<T>(null);
        }

        public async UniTask TransitionToState<T>(string stateName = null) where T : StateDefinition
        {
            StateData stateData = null;

            if (string.IsNullOrEmpty(stateName))
            {
                // Find first state of the specified type
                stateData = _states.Find(s => s.Instance != null && s.Instance.GetType() == typeof(T));
            }
            else
            {
                // Find state by type and name
                stateData = _states.Find(s => s.Instance != null && s.Instance.GetType() == typeof(T) && s.Name == stateName);
            }

            if (stateData == null || stateData.Instance == null)
            {
                var typeName = typeof(T).Name;
                var namePart = string.IsNullOrEmpty(stateName) ? "" : $" with name '{stateName}'";
                Debug.LogError($"State of type '{typeName}'{namePart} not found or not initialized.");
                return;
            }

            await PerformTransition(stateData.Instance);
        }


        private async UniTask PerformTransition(StateDefinition targetState)
        {
            Debug.Log(_currentStateInstance);
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

            await EnterState(targetState);
        }

        private async UniTask EnterState(StateDefinition state)
        {
            _currentStateInstance = state; // Set current state BEFORE calling OnEnter

            try
            {
                await state.OnEnter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during initial state enter for '{state.GetType().Name}': {ex.Message}");
            }
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
