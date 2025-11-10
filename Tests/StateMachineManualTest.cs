using UnityEngine;
using Cysharp.Threading.Tasks;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Tests
{
    public class StateMachineManualTest : MonoBehaviour
    {
        [Header("Test State Machine")]
        public StateMachine stateMachine;
        
        [Header("Debug Controls")]
        [SerializeField] private string targetStateId = "";
        
        private void Start()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<StateMachine>();
            }
            
            if (stateMachine != null)
            {
                Debug.Log("StateMachine Manual Test Started");
                LogCurrentState();
            }
        }
        
        private void Update()
        {
            // You can add keyboard controls here for testing
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TransitionToState("stateA");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TransitionToState("stateB");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TransitionToState("stateC");
            }
        }
        
        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            if (stateMachine != null && stateMachine.CurrentState != null)
            {
                Debug.Log($"Current State: {stateMachine.CurrentState.title} (ID: {stateMachine.CurrentState.id})");
            }
            else
            {
                Debug.Log("No current state or StateMachine not found");
            }
        }
        
        [ContextMenu("Transition To Target State")]
        public void TransitionToTargetState()
        {
            if (!string.IsNullOrEmpty(targetStateId))
            {
                TransitionToState(targetStateId);
            }
            else
            {
                Debug.LogWarning("Target State ID is empty");
            }
        }
        
        public async void TransitionToState(string stateId)
        {
            if (stateMachine != null)
            {
                Debug.Log($"Attempting transition to state: {stateId}");
                await stateMachine.TransitionToState(stateId);
                LogCurrentState();
            }
        }
        
        [ContextMenu("List All States")]
        private void ListAllStates()
        {
            if (stateMachine != null)
            {
                Debug.Log($"StateMachine has {stateMachine.Nodes.Count} states:");
                foreach (var node in stateMachine.Nodes)
                {
                    string initialStateMarker = node.IsInitialState ? " [INITIAL]" : "";
                    Debug.Log($"- {node.title} (ID: {node.id}){initialStateMarker}");
                }
                
                Debug.Log($"StateMachine has {stateMachine.Transitions.Count} transitions:");
                foreach (var transition in stateMachine.Transitions)
                {
                    var fromNode = stateMachine.Nodes.Find(n => n.id == transition.fromId);
                    var toNode = stateMachine.Nodes.Find(n => n.id == transition.toId);
                    string fromName = fromNode?.title ?? "Unknown";
                    string toName = toNode?.title ?? "Unknown";
                    Debug.Log($"- {fromName} -> {toName}");
                }
            }
        }
    }
}