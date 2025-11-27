using System.Linq;
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
            _stateMachine.Initialize().Forget();

            Debug.Log("StateMachine Manual Test Started");
            Debug.Log($"Number of states: {_stateMachine.States.Count}");
            Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
            Debug.Log($"Press '{_transitionKey}' to cycle through states");
            Debug.Log($"Press '{_randomTransitionKey}' for random state transition");
        }

        private void Update()
        {
            if (_stateMachine == null || _stateMachine.CurrentState == null) return;

            // Cycle through all states by type
            if (Input.GetKeyDown(_transitionKey))
            {
                _currentTransitionIndex = (_currentTransitionIndex + 1) % _stateMachine.States.Count;
                var targetState = _stateMachine.States[_currentTransitionIndex].Instance;
                if (targetState != null)
                {
                    Debug.Log($"Transitioning to: {targetState.GetType().Name}");
                    _stateMachine.TransitionToState(targetState);
                    Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
            }

            // Random transition to any state
            if (Input.GetKeyDown(_randomTransitionKey))
            {
                var validStates = _stateMachine.States.Where(s => s.Instance != null).ToArray();
                if (validStates.Length > 0)
                {
                    var randomState = validStates[Random.Range(0, validStates.Length)].Instance;
                    Debug.Log($"Random transition to: {randomState.GetType().Name}");
                    _stateMachine.TransitionToState(randomState);
                    Debug.Log($"Current state: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
                else
                {
                    Debug.Log("No valid states available");
                }
            }
        }

        private void OnGUI()
        {
            if (_stateMachine == null) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"Current State: {_stateMachine.CurrentState?.GetType().Name ?? "None"}");
            GUI.Label(new Rect(10, 30, 300, 20), $"States Count: {_stateMachine.States.Count}");

            if (_stateMachine.States.Count > 0)
            {
                GUI.Label(new Rect(10, 50, 300, 20), "Available States:");
                var validStates = _stateMachine.States.Where(s => s.Instance != null).Take(5).ToArray();
                for (int i = 0; i < validStates.Length; i++)
                {
                    GUI.Label(new Rect(10, 70 + i * 20, 200, 20), $"- {validStates[i].Instance.GetType().Name}");
                }
            }

            GUI.Label(new Rect(10, Screen.height - 60, 400, 20), $"Press '{_transitionKey}' for sequential state transitions");
            GUI.Label(new Rect(10, Screen.height - 40, 400, 20), $"Press '{_randomTransitionKey}' for random state transitions");
        }
    }
}