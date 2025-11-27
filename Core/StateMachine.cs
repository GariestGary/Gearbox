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
        private List<StateDefinition> _initializedStates = new();
        private StateDefinition _initialState;

        /// <summary>
        /// List of all configured states in this state machine.
        /// </summary>
        public List<StateDefinition> States => _initializedStates;

        /// <summary>
        /// Currently active state instance.
        /// </summary>
        public StateDefinition CurrentState { get; private set; }

        private void Start()
        {
            if (_initializeOnStart)
            {
                Initialize().Forget();
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

        public async UniTask Initialize()
        {
            CurrentState = null;

            // Instantiate state instances
            foreach (var stateData in _states)
            {
                InitializeStateData(stateData);
            }

            if (_initializedStates.Count <= 0)
            {
                return;
            }
            
            _initialState ??= _initializedStates[0];

            // Set initial state if available
            await EnterState(_initialState);
        }

        public void SetInitialState(StateDefinition state)
        {
            if (state == null || !_initializedStates.Contains(state))
            {
                return;
            }
            
            _initialState =  state;
        }

        private void InitializeStateData(StateData stateData)
        {
            var stateType = stateData.GetStateType();
            if (stateType == null) return;

            try
            {
                // Use existing instance if available (created in editor), otherwise create new one
                var state = stateData.Instance ?? (StateDefinition)Activator.CreateInstance(stateType);
                AddState(state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create instance of {stateType.Name}: {ex.Message}");
            }
        }

        public void AddState(StateDefinition state)
        {
            if (state == null || _initializedStates.Contains(state))
            {
                return;
            }
            
            state.StateMachine = this;
            _stateInitializeAction?.Invoke(state);
            _initializedStates.Add(state);
        }

        public void RemoveState(StateDefinition state)
        {
            if (state == null || !_initializedStates.Contains(state))
            {
                return;
            }

            _initializedStates.Remove(state);
        }

        public void Clear()
        {
            _initializedStates = new List<StateDefinition>();
            CurrentState = null;
            _stateInitializeAction = null;
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
        public async UniTask TransitionToNamed(string stateName, object data = null)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogError("State name cannot be null or empty.");
                return;
            }

            var matchingStates = _initializedStates.FindAll(s => s.Name == stateName && s != null);
            if (matchingStates.Count == 0)
            {
                Debug.LogError($"State '{stateName}' not found or not initialized.");
                return;
            }

            // If multiple states have the same name, select one randomly
            var selectedState = matchingStates.Count == 1
                ? matchingStates[0]
                : matchingStates[UnityEngine.Random.Range(0, matchingStates.Count)];

            await PerformTransition(selectedState, data);
        }

        /// <summary>
        /// Transitions to a state of the specified type, optionally filtered by name.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to</typeparam>
        /// <param name="stateName">Optional name filter. If null, selects the first state of type T</param>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionToNamed<T>(string stateName, object data = null) where T : StateDefinition
        {
            var stateData = string.IsNullOrEmpty(stateName) 
                ? _initializedStates.Find(s => s != null && s.GetType() == typeof(T)) 
                : _initializedStates.Find(s => s != null && s.GetType() == typeof(T) && s.Name == stateName);

            if (stateData == null)
            {
                var typeName = typeof(T).Name;
                var namePart = string.IsNullOrEmpty(stateName) ? "" : $" with name '{stateName}'";
                Debug.LogError($"State of type '{typeName}'{namePart} not found or not initialized.");
                return;
            }

            await PerformTransition(stateData, data);
        }

        /// <summary>
        /// Transitions to a state of the specified type from within a state context.
        /// This method automatically infers the target state type from the calling context.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to (automatically inferred)</typeparam>
        /// <param name="data">Optional data to pass to the OnEnter method</param>
        public async UniTask TransitionTo<T>(object data = null) where T : StateDefinition
        {
            await TransitionToNamed<T>(null, data);
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
                await state.Exit(toState);
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
                await state.Enter(fromState, data);
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

        public void DoUpdate(float delta)
        {
            CurrentState?.Update(delta);
        }
    }
}
