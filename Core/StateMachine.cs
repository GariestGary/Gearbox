using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VolumeBox.Gearbox.Core
{
    /// <summary>
    /// Main state machine component that manages state transitions and lifecycle.
    /// Attach this to a GameObject and configure states in the inspector.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private List<StateData> _states = new();
        [SerializeField] private bool _initializeOnStart = true;

        private Action<StateDefinition> _stateInitializeAction;

        /// <summary>
        /// List of all configured states in this state machine.
        /// </summary>
        public List<StateData> States => _states;

        /// <summary>
        /// Currently active state instance.
        /// </summary>
        public StateDefinition CurrentState { get; private set; }

        private void Start()
        {
            if (_initializeOnStart)
            {
                InitializeStateMachine().Forget();
            }
        }

        /// <summary>
        /// Sets a callback that will be invoked when each state is initialized.
        /// Useful for dependency injection or custom initialization logic.
        /// </summary>
        /// <param name="action">Action to invoke with each state instance during initialization</param>
        public void SetStateInitializeAction(Action<StateDefinition> action)
        {
            _stateInitializeAction = action;
        }

        public async UniTask InitializeStateMachine()
        {
            CurrentState = null;

            // Instantiate state instances
            foreach (var stateData in _states)
            {
                InitializeStateData(stateData);
            }

            // Set initial state if available
            var initialState = FindInitialState();
            if (initialState != null)
            {
                await EnterState(initialState.Instance);
            }
            else if (_states.Count > 0)
            {
                Debug.LogWarning("StateMachine has no valid states defined.");
            }
        }

        private void InitializeStateData(StateData stateData)
        {
            var stateType = stateData.GetStateType();
            if (stateType == null) return;

            try
            {
                // Use existing instance if available (created in editor), otherwise create new one
                stateData.Instance ??= (StateDefinition)Activator.CreateInstance(stateType);

                stateData.Instance.StateMachine = this;
                _stateInitializeAction?.Invoke(stateData.Instance);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create instance of {stateType.Name}: {ex.Message}");
            }
        }

        private StateData FindInitialState()
        {
            // First, try to find a state marked as initial
            var initialState = _states.Find(s => s.IsInitialState && s.Instance != null);
            if (initialState != null) return initialState;

            // Fallback to first valid state if no initial state is marked
            return _states.Find(s => s.Instance != null);
        }

        /// <summary>
        /// Transitions to the specified state instance.
        /// </summary>
        /// <param name="targetState">The state instance to transition to</param>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionToState(StateDefinition targetState, object data = null)
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

            await PerformTransition(targetState, data);
        }

        /// <summary>
        /// Transitions to a state by name. If multiple states share the same name, one is selected randomly.
        /// </summary>
        /// <param name="stateName">Name of the state to transition to</param>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionToState(string stateName, object data = null)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogError("State name cannot be null or empty.");
                return;
            }

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

            await PerformTransition(selectedState.Instance, data);
        }

        /// <summary>
        /// Transitions to the first state of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to</typeparam>
        public async UniTask TransitionToState<T>(object data = null) where T : StateDefinition
        {
            await TransitionToState<T>(null, data);
        }

        /// <summary>
        /// Transitions to a state of the specified type, optionally filtered by name.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to</typeparam>
        /// <param name="stateName">Optional name filter. If null, selects the first state of type T</param>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionToState<T>(string stateName, object data = null) where T : StateDefinition
        {
            var stateData = string.IsNullOrEmpty(stateName) 
                ? _states.Find(s => s.Instance != null && s.Instance.GetType() == typeof(T)) 
                : _states.Find(s => s.Instance != null && s.Instance.GetType() == typeof(T) && s.Name == stateName);

            if (stateData?.Instance == null)
            {
                var typeName = typeof(T).Name;
                var namePart = string.IsNullOrEmpty(stateName) ? "" : $" with name '{stateName}'";
                Debug.LogError($"State of type '{typeName}'{namePart} not found or not initialized.");
                return;
            }

            await PerformTransition(stateData.Instance, data);
        }

        /// <summary>
        /// Transitions to a state of the specified type from within a state context.
        /// This method automatically infers the target state type from the calling context.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to (automatically inferred)</typeparam>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionTo<T>(object data = null) where T : StateDefinition
        {
            await TransitionToState<T>(null, data);
        }

        /// <summary>
        /// Triggers a transition by index from the specified state's transition list.
        /// </summary>
        /// <param name="fromState">The state to transition from</param>
        /// <param name="transitionIndex">Index of the transition in the state's TransitionNames list</param>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TriggerTransition(StateDefinition fromState, int transitionIndex, object data = null)
        {
            var stateData = _states.Find(s => s.Instance == fromState);
            if (stateData == null || transitionIndex < 0 || transitionIndex >= stateData.TransitionNames.Count)
            {
                Debug.LogError("Invalid transition request.");
                return;
            }

            var targetStateName = stateData.TransitionNames[transitionIndex];
            await TransitionToState(targetStateName, data);
        }

        /// <summary>
        /// Gets the list of available transition names from the specified state.
        /// </summary>
        /// <param name="state">The state to get transitions from</param>
        /// <returns>List of target state names, or empty list if state not found</returns>
        public List<string> GetAvailableTransitions(StateDefinition state)
        {
            var stateData = _states.Find(s => s.Instance == state);
            return stateData?.TransitionNames ?? new List<string>();
        }

        private async UniTask PerformTransition(StateDefinition targetState, object data = null)
        {
            var previousState = CurrentState;

            // Exit current state
            if (previousState != null)
            {
                await ExecuteStateExit(previousState, targetState);
            }

            // Enter new state
            await ExecuteStateEnter(targetState, previousState, data);

            CurrentState = targetState;
        }

        private async UniTask ExecuteStateExit(StateDefinition state, StateDefinition toState)
        {
            try
            {
                await state.OnExit(toState);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during state exit for '{state.GetType().Name}': {ex.Message}");
            }
        }

        private async UniTask ExecuteStateEnter(StateDefinition state, StateDefinition fromState, object data)
        {
            try
            {
                await state.OnEnter(fromState, data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during state enter for '{state.GetType().Name}': {ex.Message}");
            }
        }

        private async UniTask EnterState(StateDefinition state)
        {
            await ExecuteStateEnter(state, null, null);
            CurrentState = state;
        }

        private async void Update()
        {
            await ExecuteStateUpdate();
        }

        /// <summary>
        /// Public method for testing the update logic.
        /// </summary>
        public async UniTask TestUpdate()
        {
            await ExecuteStateUpdate();
        }

        private async UniTask ExecuteStateUpdate()
        {
            if (CurrentState == null) return;

            try
            {
                await CurrentState.OnUpdate();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in state update for '{CurrentState.GetType().Name}': {ex.Message}");
            }
        }
    }
}
