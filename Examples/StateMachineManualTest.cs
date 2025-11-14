using Cysharp.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Tests
{
    /// <summary>
    /// Manual test scene for the state machine system.
    /// Attach this to a GameObject with a StateMachine component and run in Play mode.
    /// </summary>
    public class StateMachineManualTest : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private KeyCode _transitionKey = KeyCode.Space;
        [SerializeField] private KeyCode _randomTransitionKey = KeyCode.R;

        private StateMachine _stateMachine;
        private int _currentTransitionIndex = 0;

        private void Start()
        {
            _stateMachine = GetComponent<StateMachine>();

            if (_stateMachine == null)
            {
                Debug.LogError("StateMachineManualTest requires a StateMachine component!");
                return;
            }

            // Initialize the state machine
            _stateMachine.InitializeStateMachine().Forget();

            Debug.Log("StateMachine Manual Test Started");
            Debug.Log($"Number of states: {_stateMachine.States.Count}");
            Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
            Debug.Log($"Press '{_transitionKey}' to trigger next transition");
            Debug.Log($"Press '{_randomTransitionKey}' for random transition");
        }

        private void Update()
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null) return;

            // Trigger next transition in sequence
            if (Input.GetKeyDown(_transitionKey))
            {
                var transitions = _stateMachine.GetAvailableTransitions(_stateMachine.CurrentState);
                if (transitions.Count > 0)
                {
                    var nextStateName = transitions[_currentTransitionIndex % transitions.Count];
                    Debug.Log($"Transitioning to: {nextStateName}");

                    _stateMachine.TransitionToState(nextStateName);
                    _currentTransitionIndex++;

                    Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
                else
                {
                    Debug.Log("No available transitions from current state");
                }
            }

            // Random transition
            if (Input.GetKeyDown(_randomTransitionKey))
            {
                var transitions = _stateMachine.GetAvailableTransitions(_stateMachine.CurrentState);
                if (transitions.Count > 0)
                {
                    var randomIndex = Random.Range(0, transitions.Count);
                    var randomStateName = transitions[randomIndex];
                    Debug.Log($"Random transition to: {randomStateName}");

                    _stateMachine.TransitionToState(randomStateName);

                    Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
                else
                {
                    Debug.Log("No available transitions from current state");
                }
            }
        }

        private void OnGUI()
        {
            if (_stateMachine == null) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"Current State: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
            GUI.Label(new Rect(10, 30, 300, 20), $"States Count: {_stateMachine.States.Count}");

            if (_stateMachine.CurrentState != null)
            {
                var transitions = _stateMachine.GetAvailableTransitions(_stateMachine.CurrentState);
                GUI.Label(new Rect(10, 50, 300, 20), $"Available Transitions: {transitions.Count}");

                for (int i = 0; i < transitions.Count && i < 5; i++)
                {
                    GUI.Label(new Rect(10, 70 + i * 20, 200, 20), $"- {transitions[i]}");
                }
            }

            GUI.Label(new Rect(10, Screen.height - 60, 400, 20), $"Press '{_transitionKey}' for sequential transitions");
            GUI.Label(new Rect(10, Screen.height - 40, 400, 20), $"Press '{_randomTransitionKey}' for random transitions");
        }
    }
}