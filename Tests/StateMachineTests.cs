using System.Collections;
using System.Linq;
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
                name = "TestState",
                stateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            stateMachine.States.Add(stateData);

            // Initialize should create the state instance
            stateMachine.InitializeStateMachine();

            Assert.AreEqual(1, stateMachine.States.Count);
            Assert.IsNotNull(stateMachine.States[0].instance);
            Assert.AreEqual(typeof(IdleState), stateMachine.States[0].instance.GetType());
        }

        [Test]
        public void StateMachine_TransitionToStateByName()
        {
            // Setup two states
            var idleState = new StateData
            {
                name = "Idle",
                stateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveState = new StateData
            {
                name = "Move",
                stateTypeName = typeof(MoveState).AssemblyQualifiedName
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
                name = "TestState",
                stateTypeName = typeof(IdleState).AssemblyQualifiedName,
                transitionNames = new System.Collections.Generic.List<string> { "State1", "State2" }
            };

            var idleState = new IdleState();
            stateData.instance = idleState;

            stateMachine.States.Add(stateData);

            var transitions = stateMachine.GetAvailableTransitions(idleState);
            Assert.AreEqual(2, transitions.Count);
            Assert.Contains("State1", transitions.ToList());
            Assert.Contains("State2", transitions.ToList());
        }

        [Test]
        public void StateMachine_TriggerTransition()
        {
            // Setup two states
            var state1Data = new StateData
            {
                name = "State1",
                stateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var state2Data = new StateData
            {
                name = "State2",
                stateTypeName = typeof(MoveState).AssemblyQualifiedName,
                transitionNames = new System.Collections.Generic.List<string> { "State1" }
            };

            stateMachine.States.Add(state1Data);
            stateMachine.States.Add(state2Data);

            // Set current state to State2
            var moveState = new MoveState();
            state2Data.instance = moveState;
            stateMachine.InitializeStateMachine();
            stateMachine.TransitionToState(moveState);

            // Trigger transition to State1 (index 0)
            stateMachine.TriggerTransition(moveState, 0);

            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_UpdateLoop()
        {
            // Setup state machine with a state that has update logic
            var moveStateData = new StateData
            {
                name = "Move",
                stateTypeName = typeof(MoveState).AssemblyQualifiedName
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

            Assert.AreEqual(typeof(IdleState).AssemblyQualifiedName, stateData.stateTypeName);
            Assert.AreEqual(typeof(IdleState), stateData.GetStateType());
        }

        [Test]
        public void StateData_GetStateType()
        {
            var stateData = new StateData
            {
                stateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            var stateType = stateData.GetStateType();

            Assert.AreEqual(typeof(MoveState), stateType);
        }
    }
}