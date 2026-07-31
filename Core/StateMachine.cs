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
        [SerializeField] private bool _updateAutomatically = true;
        
        private StateDefinition _initialState;

        private Action<StateDefinition> _stateInitializeAction;
        private List<StateDefinition> _initializedStates = new();
        private bool _isTransitioning;

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

        private void Update()
        {
            if (_updateAutomatically && !_isTransitioning)
            {
                DoUpdate(Time.deltaTime);
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
            if (!TryBeginTransition("initialize the state machine"))
            {
                return;
            }

            try
            {
                if (CurrentState != null && !await ExecuteStateExit(CurrentState, null))
                {
                    return;
                }

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
            
                // Use serialized initial state or fallback to first state
                var initialState = _initialState ?? _initializedStates[0];

                // Set initial state if available
                await EnterState(initialState);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void SetInitialState(StateDefinition state)
        {
            if (state == null || !_initializedStates.Contains(state))
            {
                return;
            }
            
            _initialState = state;
        }

        private void InitializeStateData(StateData stateData)
        {
            if (stateData?.Instance == null)
            {
                return;
            }

            //If initial state was not set before initializing then set it from inspector state
            if (stateData.IsInitial && _initialState == null)
            {
                _initialState = stateData.Instance;
            }
            
            AddState(stateData.Instance);
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
            RemoveStateAsync(state).Forget();
        }

        public async UniTask RemoveStateAsync(StateDefinition state)
        {
            if (state == null || !_initializedStates.Contains(state))
            {
                return;
            }

            if (!TryBeginTransition("remove a state"))
            {
                return;
            }

            try
            {
                if (CurrentState == state)
                {
                    if (!await ExecuteStateExit(state, null))
                    {
                        return;
                    }

                    CurrentState = null;
                }

                _initializedStates.Remove(state);
                if (_initialState == state)
                {
                    _initialState = null;
                }

                state.StateMachine = null;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void Clear()
        {
            ClearAsync().Forget();
        }

        public async UniTask ClearAsync()
        {
            if (!TryBeginTransition("clear the state machine"))
            {
                return;
            }

            try
            {
                if (CurrentState != null && !await ExecuteStateExit(CurrentState, null))
                {
                    return;
                }

                foreach (var state in _initializedStates)
                {
                    if (state != null)
                    {
                        state.StateMachine = null;
                    }
                }

                _initializedStates.Clear();
                CurrentState = null;
                _initialState = null;
                _stateInitializeAction = null;
            }
            finally
            {
                _isTransitioning = false;
            }
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

            if (!_initializedStates.Contains(targetState))
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

            var matchingStates = _initializedStates.FindAll(s => s != null && s.Name == stateName);
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
            if (!TryBeginTransition($"transition to '{targetState.Name ?? targetState.GetType().Name}'"))
            {
                return;
            }

            try
            {
                var previousState = CurrentState;

                // Exit current state. Keep it current if its exit hook fails.
                if (previousState != null && !await ExecuteStateExit(previousState, targetState))
                {
                    return;
                }

                CurrentState = null;

                // A state is active only after its enter hook completes successfully.
                if (await ExecuteStateEnter(targetState, previousState, data))
                {
                    CurrentState = targetState;
                }
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private async UniTask<bool> ExecuteStateExit(StateDefinition state, StateDefinition toState)
        {
            try
            {
                await state.Exit(toState);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private async UniTask<bool> ExecuteStateEnter(StateDefinition state, StateDefinition fromState, object data)
        {
            try
            {
                await state.Enter(fromState, data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private async UniTask EnterState(StateDefinition state)
        {
            if (await ExecuteStateEnter(state, null, null))
            {
                CurrentState = state;
            }
        }

        private bool TryBeginTransition(string operation)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"Cannot {operation} while another state-machine operation is in progress.", this);
                return false;
            }

            _isTransitioning = true;
            return true;
        }

        public void DoUpdate(float delta)
        {
            CurrentState?.Update(delta);
        }
    }
}
