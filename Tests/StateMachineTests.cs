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

        [UnityTest]
        public IEnumerator StateMachine_InitializeStateMachine_SetsCurrentState()
        {
            // Add a test state
            var stateData = new StateData
            {
                Name = "TestState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName,
                IsInitialState = true
            };
            stateMachine.States.Add(stateData);

            // Initialize the state machine
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Verify that CurrentState is set after initialization
            Assert.IsNotNull(stateMachine.CurrentState);
            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_InitializeStateMachine_UsesMarkedInitialState()
        {
            // Add multiple states, but mark the second one as initial
            var stateData1 = new StateData
            {
                Name = "IdleState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName,
                IsInitialState = false
            };
            var stateData2 = new StateData
            {
                Name = "MoveState",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName,
                IsInitialState = true
            };

            stateMachine.States.Add(stateData1);
            stateMachine.States.Add(stateData2);

            // Initialize the state machine
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Verify that the marked initial state (MoveState) is the current state
            Assert.IsNotNull(stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), stateMachine.CurrentState.GetType());
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

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByGenericType()
        {
            // Setup states of different types
            var idleState = new StateData
            {
                Name = "IdleState1",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveState = new StateData
            {
                Name = "MoveState1",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState);
            stateMachine.States.Add(moveState);

            // Initialize
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to IdleState by type
            var transitionTask = stateMachine.TransitionToState<IdleState>();
            yield return transitionTask.ToCoroutine();

            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByGenericTypeWithName()
        {
            // Setup multiple states of same type with different names
            var idleState1 = new StateData
            {
                Name = "Idle1",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var idleState2 = new StateData
            {
                Name = "Idle2",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState1);
            stateMachine.States.Add(idleState2);

            // Initialize
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to specific named IdleState
            var transitionTask = stateMachine.TransitionToState<IdleState>("Idle2");
            yield return transitionTask.ToCoroutine();

            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
            Assert.AreEqual("Idle2", stateMachine.States.Find(s => s.Instance == stateMachine.CurrentState).Name);
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByGenericTypeMultipleInstances()
        {
            // Setup multiple IdleState instances
            var idleState1 = new StateData
            {
                Name = "Idle1",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var idleState2 = new StateData
            {
                Name = "Idle2",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var idleState3 = new StateData
            {
                Name = "Idle3",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState1);
            stateMachine.States.Add(idleState2);
            stateMachine.States.Add(idleState3);

            // Initialize
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to IdleState (should select first one)
            var transitionTask = stateMachine.TransitionToState<IdleState>();
            yield return transitionTask.ToCoroutine();

            Assert.AreEqual(typeof(IdleState), stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByNameMultipleStates()
        {
            // Setup multiple states with same name
            var idleState1 = new StateData
            {
                Name = "DuplicateName",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveState1 = new StateData
            {
                Name = "DuplicateName",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState1);
            stateMachine.States.Add(moveState1);

            // Initialize
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to state with duplicate name (should select randomly)
            var transitionTask = stateMachine.TransitionToState("DuplicateName");
            yield return transitionTask.ToCoroutine();

            Assert.IsNotNull(stateMachine.CurrentState);
            var currentStateData = stateMachine.States.Find(s => s.Instance == stateMachine.CurrentState);
            Assert.AreEqual("DuplicateName", currentStateData.Name);
            // Could be either IdleState or MoveState due to random selection
            Assert.IsTrue(stateMachine.CurrentState.GetType() == typeof(IdleState) ||
                         stateMachine.CurrentState.GetType() == typeof(MoveState));
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByGenericTypeWithNameMultipleTypes()
        {
            // Setup states with same name but different types
            var idleState = new StateData
            {
                Name = "SharedName",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveState = new StateData
            {
                Name = "SharedName",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            stateMachine.States.Add(idleState);
            stateMachine.States.Add(moveState);

            // Initialize
            var initTask = stateMachine.InitializeStateMachine();
            yield return initTask.ToCoroutine();

            // Transition to specific type with name
            var transitionTask = stateMachine.TransitionToState<MoveState>("SharedName");
            yield return transitionTask.ToCoroutine();

            Assert.AreEqual(typeof(MoveState), stateMachine.CurrentState.GetType());
            Assert.AreEqual("SharedName", stateMachine.States.Find(s => s.Instance == stateMachine.CurrentState).Name);
        }

        [Test]
        public void StateMachine_TransitionToStateByGenericTypeNotFound()
        {
            // Setup state machine without the requested type
            var idleState = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            stateMachine.States.Add(idleState);

            // Don't initialize - we want to test the transition logic directly
            // The transition should handle the case where no states are initialized

            // This should not throw and should log an error
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "State of type 'MoveState' not found or not initialized.");
            Assert.DoesNotThrow(() => stateMachine.TransitionToState<MoveState>());
        }

        [Test]
        public void StateMachine_TransitionToStateByNameNotFound()
        {
            // Setup state machine without the requested name
            var idleState = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            stateMachine.States.Add(idleState);

            // This should not throw and should log an error
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "State 'NonExistentState' not found or not initialized.");
            Assert.DoesNotThrow(() => stateMachine.TransitionToState("NonExistentState"));
        }
    }
}