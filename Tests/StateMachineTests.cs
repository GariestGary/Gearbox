using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VolumeBox.Gearbox.Core;
using VolumeBox.Gearbox.Examples;

namespace VolumeBox.Gearbox.Tests
{
    public class StateMachineTests
    {
        private GameObject testObject;
        private StateMachine stateMachine;

        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestStateMachine");
            stateMachine = testObject.AddComponent<StateMachine>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void StateMachine_InitializesEmpty()
        {
            Assert.AreEqual(0, stateMachine.States.Count);
            Assert.IsNull(stateMachine.CurrentState);
        }

        [Test]
        public void StateMachine_InitializesWithStates()
        {
            // Add a test state
            var stateData = new StateData
            {
                Name = "TestState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            stateMachine.States.Add(stateData);

            // Initialize should create the state instance
            stateMachine.InitializeStateMachine();

            Assert.AreEqual(1, stateMachine.States.Count);
            Assert.IsNotNull(stateMachine.States[0].Instance);
            Assert.AreEqual(typeof(IdleState), stateMachine.States[0].Instance.GetType());
        }

        [Test]
        public void StateMachine_TransitionToStateByName()
        {
            // Setup two states
            var idleState = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveState = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState);
            stateMachine.States.Add(moveState);

            stateMachine.InitializeStateMachine();

            // Transition to Move state
            stateMachine.TransitionToState("Move");

            Assert.IsNotNull(stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), stateMachine.CurrentState.GetType());
        }

        [Test]
        public void StateMachine_GetAvailableTransitions()
        {
            // Setup state with transitions
            var stateData = new StateData
            {
                Name = "TestState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName,
                TransitionNames = new System.Collections.Generic.List<string> { "State1", "State2" }
            };

            var idleState = new IdleState();
            stateData.Instance = idleState;

            stateMachine.States.Add(stateData);

            var transitions = stateMachine.GetAvailableTransitions(idleState);
            Assert.AreEqual(2, transitions.Count);
            Assert.Contains("State1", transitions.ToList());
            Assert.Contains("State2", transitions.ToList());
        }

        [UnityTest]
        public System.Collections.IEnumerator StateMachine_TriggerTransition()
        {
            // Setup two states
            var state1Data = new StateData
            {
                Name = "State1",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var state2Data = new StateData
            {
                Name = "State2",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName,
                TransitionNames = new System.Collections.Generic.List<string> { "State1" }
            };

            stateMachine.States.Add(state1Data);
            stateMachine.States.Add(state2Data);

            // Initialize the state machine first
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to State2
            var transitionTask = stateMachine.TransitionToState("State2");
            yield return transitionTask.ToCoroutine();

            // Now trigger transition to State1 (index 0)
            var triggerTask = stateMachine.TriggerTransition(stateMachine.CurrentState, 0);
            yield return triggerTask.ToCoroutine();

            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_UpdateLoop()
        {
            // Setup state machine with a state that has update logic
            var moveStateData = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };
            stateMachine.States.Add(moveStateData);

            stateMachine.InitializeStateMachine();

            // Wait for initialization
            yield return null;

            // Transition to move state
            stateMachine.TransitionToState("Move");

            // Wait a frame for update to run
            yield return null;

            // Verify state is active
            Assert.IsNotNull(stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), stateMachine.CurrentState.GetType());
        }

        [Test]
        public void StateData_SetStateType()
        {
            var stateData = new StateData();

            stateData.SetStateType(typeof(IdleState));

            Assert.AreEqual(typeof(IdleState).AssemblyQualifiedName, stateData.StateTypeName);
            Assert.AreEqual(typeof(IdleState), stateData.GetStateType());
        }

        [Test]
        public void StateData_GetStateType()
        {
            var stateData = new StateData
            {
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            var stateType = stateData.GetStateType();

            Assert.AreEqual(typeof(MoveState), stateType);
        }

        [UnityTest]
        public IEnumerator StateMachine_InstantiateActionWorks()
        {
            // Add a test state
            var stateData = new StateData
            {
                Name = "TestState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            stateMachine.States.Add(stateData);

            // Set up action to verify it's called
            bool actionCalled = false;
            stateMachine.SetStateInitializeAction((state) => actionCalled = true);

            // Initialize the state machine
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Assert that the action was invoked
            Assert.IsTrue(actionCalled);
            Assert.IsNotNull(stateMachine.States[0].Instance);
            Assert.AreEqual(typeof(IdleState), stateMachine.States[0].Instance.GetType());
        }
    }
}