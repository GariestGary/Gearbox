using UnityEngine;
using VolumeBox.Gearbox.Core;
using VolumeBox.Gearbox.Examples;

namespace VolumeBox.Gearbox.Tests
{
    /// <summary>
    /// Manual test scene for the state machine system.
    /// Attach this to a GameObject with a StateMachine component and run in Play mode.
    /// </summary>
    public class StateMachineManualTest : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private KeyCode transitionKey = KeyCode.Space;
        [SerializeField] private KeyCode randomTransitionKey = KeyCode.R;

        private StateMachine stateMachine;
        private int currentTransitionIndex = 0;

        private void Start()
        {
            stateMachine = GetComponent<StateMachine>();

            if (stateMachine == null)
            {
                Debug.LogError("StateMachineManualTest requires a StateMachine component!");
                return;
            }

            // Initialize the state machine
            stateMachine.InitializeStateMachine();

            Debug.Log("StateMachine Manual Test Started");
            Debug.Log($"Number of states: {stateMachine.States.Count}");
            Debug.Log($"Current state: {stateMachine.CurrentState?.GetType().Name ?? "None"}");
            Debug.Log($"Press '{transitionKey}' to trigger next transition");
            Debug.Log($"Press '{randomTransitionKey}' for random transition");
        }

        private void Update()
        {
            if (stateMachine == null || stateMachine.CurrentState == null) return;

            // Trigger next transition in sequence
            if (Input.GetKeyDown(transitionKey))
            {
                var transitions = stateMachine.GetAvailableTransitions(stateMachine.CurrentState);
                if (transitions.Count > 0)
                {
                    var nextStateName = transitions[currentTransitionIndex % transitions.Count];
                    Debug.Log($"Transitioning to: {nextStateName}");

                    stateMachine.TransitionToState(nextStateName);
                    currentTransitionIndex++;

                    Debug.Log($"Current state: {stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
                else
                {
                    Debug.Log("No available transitions from current state");
                }
            }

            // Random transition
            if (Input.GetKeyDown(randomTransitionKey))
            {
                var transitions = stateMachine.GetAvailableTransitions(stateMachine.CurrentState);
                if (transitions.Count > 0)
                {
                    var randomIndex = Random.Range(0, transitions.Count);
                    var randomStateName = transitions[randomIndex];
                    Debug.Log($"Random transition to: {randomStateName}");

                    stateMachine.TransitionToState(randomStateName);

                    Debug.Log($"Current state: {stateMachine.CurrentState?.GetType().Name ?? "None"}");
                }
                else
                {
                    Debug.Log("No available transitions from current state");
                }
            }
        }

        private void OnGUI()
        {
            if (stateMachine == null) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"Current State: {stateMachine.CurrentState?.GetType().Name ?? "None"}");
            GUI.Label(new Rect(10, 30, 300, 20), $"States Count: {stateMachine.States.Count}");

            if (stateMachine.CurrentState != null)
            {
                var transitions = stateMachine.GetAvailableTransitions(stateMachine.CurrentState);
                GUI.Label(new Rect(10, 50, 300, 20), $"Available Transitions: {transitions.Count}");

                for (int i = 0; i < transitions.Count && i < 5; i++)
                {
                    GUI.Label(new Rect(10, 70 + i * 20, 200, 20), $"- {transitions[i]}");
                }
            }

            GUI.Label(new Rect(10, Screen.height - 60, 400, 20), $"Press '{transitionKey}' for sequential transitions");
            GUI.Label(new Rect(10, Screen.height - 40, 400, 20), $"Press '{randomTransitionKey}' for random transitions");
        }
    }
}