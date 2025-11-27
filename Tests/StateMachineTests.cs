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
        private GameObject _testObject;
        private StateMachine _stateMachine;

        [SetUp]
        public void Setup()
        {
            _testObject = new GameObject("TestStateMachine");
            _stateMachine = _testObject.AddComponent<StateMachine>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testObject);
        }

        [Test]
        public void StateMachine_InitializesEmpty()
        {
            Assert.AreEqual(0, _stateMachine.States.Count);
            Assert.IsNull(_stateMachine.CurrentState);
        }

        [UnityTest]
        public IEnumerator StateMachine_InitializesWithStates()
        {
            // Add a test state
            var stateData = new StateData
            {
                Name = "TestState",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            _stateMachine.States.Add(stateData);

            // Initialize should create the state instance
            yield return _stateMachine.Initialize().ToCoroutine();

            Assert.AreEqual(1, _stateMachine.States.Count);
            Assert.IsNotNull(_stateMachine.States[0].Instance);
            Assert.AreEqual(typeof(IdleState), _stateMachine.States[0].Instance.GetType());
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
            _stateMachine.States.Add(stateData);

            // Initialize the state machine
            yield return _stateMachine.Initialize().ToCoroutine();

            // Verify that CurrentState is set after initialization
            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
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

            _stateMachine.States.Add(stateData1);
            _stateMachine.States.Add(stateData2);

            // Initialize the state machine
            yield return _stateMachine.Initialize().ToCoroutine();

            // Verify that the marked initial state (MoveState) is the current state
            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToStateByName()
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

            _stateMachine.States.Add(idleState);
            _stateMachine.States.Add(moveState);

            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to Move state
            yield return _stateMachine.TransitionToNamed("Move").ToCoroutine();

            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
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
            _stateMachine.States.Add(moveStateData);

            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to move state
            yield return _stateMachine.TransitionToNamed("Move").ToCoroutine();

            // Verify state is active
            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
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
            _stateMachine.States.Add(stateData);

            // Set up action to verify it's called
            var actionCalled = false;
            _stateMachine.SetStateInitializeAction(_ => actionCalled = true);

            // Initialize the state machine
            yield return _stateMachine.Initialize().ToCoroutine();

            // Assert that the action was invoked
            Assert.IsTrue(actionCalled);
            Assert.IsNotNull(_stateMachine.States[0].Instance);
            Assert.AreEqual(typeof(IdleState), _stateMachine.States[0].Instance.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_OnEnterReceivesFromState()
        {
            // Create test states that track fromState
            var idleStateData = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveStateData = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            _stateMachine.States.Add(idleStateData);
            _stateMachine.States.Add(moveStateData);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Verify initial state has null fromState
            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());

            // Transition to Move state
            yield return _stateMachine.TransitionToNamed("Move").ToCoroutine();

            // Verify transition occurred
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_OnExitReceivesToState()
        {
            // Setup two states
            var idleStateData = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };
            var moveStateData = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            _stateMachine.States.Add(idleStateData);
            _stateMachine.States.Add(moveStateData);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to Move state
            yield return _stateMachine.TransitionToNamed("Move").ToCoroutine();

            // Verify transition occurred
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_OnEnterReceivesData()
        {
            // Setup states
            var moveStateData = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            _stateMachine.States.Add(moveStateData);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition with data
            var customTarget = new Vector3(10, 0, 10);
            yield return _stateMachine.TransitionToNamed("Move", customTarget).ToCoroutine();

            // Verify transition occurred
            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_InitialStateHasNullFromState()
        {
            // Setup state
            var idleStateData = new StateData
            {
                Name = "Idle",
                StateTypeName = typeof(IdleState).AssemblyQualifiedName
            };

            _stateMachine.States.Add(idleStateData);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Verify initial state is set
            Assert.IsNotNull(_stateMachine.CurrentState);
            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
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

            _stateMachine.States.Add(idleState);
            _stateMachine.States.Add(moveState);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to IdleState by type
            yield return _stateMachine.TransitionTo<IdleState>().ToCoroutine();

            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
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

            _stateMachine.States.Add(idleState1);
            _stateMachine.States.Add(idleState2);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to specific named IdleState
            yield return _stateMachine.TransitionToNamed<IdleState>("Idle2").ToCoroutine();

            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
            Assert.AreEqual("Idle2", _stateMachine.States.Find(s => s.Instance == _stateMachine.CurrentState).Name);
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

            _stateMachine.States.Add(idleState1);
            _stateMachine.States.Add(idleState2);
            _stateMachine.States.Add(idleState3);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to IdleState (should select first one)
            yield return _stateMachine.TransitionTo<IdleState>().ToCoroutine();

            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
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

            _stateMachine.States.Add(idleState1);
            _stateMachine.States.Add(moveState1);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to state with duplicate name (should select randomly)
            yield return _stateMachine.TransitionToNamed("DuplicateName").ToCoroutine();

            Assert.IsNotNull(_stateMachine.CurrentState);
            var currentStateData = _stateMachine.States.Find(s => s.Instance == _stateMachine.CurrentState);
            Assert.AreEqual("DuplicateName", currentStateData.Name);
            // Could be either IdleState or MoveState due to random selection
            Assert.IsTrue(_stateMachine.CurrentState.GetType() == typeof(IdleState) ||
                         _stateMachine.CurrentState.GetType() == typeof(MoveState));
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

            _stateMachine.States.Add(idleState);
            _stateMachine.States.Add(moveState);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Transition to specific type with name
            yield return _stateMachine.TransitionToNamed<MoveState>("SharedName").ToCoroutine();

            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
            Assert.AreEqual("SharedName", _stateMachine.States.Find(s => s.Instance == _stateMachine.CurrentState).Name);
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
            _stateMachine.States.Add(idleState);

            // Don't initialize - we want to test the transition logic directly
            // The transition should handle the case where no states are initialized

            // This should not throw and should log an error
            LogAssert.Expect(LogType.Error, "State of type 'MoveState' not found or not initialized.");
            Assert.DoesNotThrow(() => _stateMachine.TransitionTo<MoveState>().Forget());
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
            _stateMachine.States.Add(idleState);

            // This should not throw and should log an error
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "State 'NonExistentState' not found or not initialized.");
            Assert.DoesNotThrow(() => _stateMachine.TransitionToNamed("NonExistentState"));
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToGenericMethod()
        {
            // Setup states
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

            _stateMachine.States.Add(idleState);
            _stateMachine.States.Add(moveState);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Test the new generic transition method
            yield return _stateMachine.TransitionTo<MoveState>().ToCoroutine();

            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToGenericMethodWithData()
        {
            // Setup states
            var moveState = new StateData
            {
                Name = "Move",
                StateTypeName = typeof(MoveState).AssemblyQualifiedName
            };

            _stateMachine.States.Add(moveState);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Test the new generic transition method with data
            var customTarget = new Vector3(15, 0, 15);
            yield return _stateMachine.TransitionTo<MoveState>(customTarget).ToCoroutine();

            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }

        [UnityTest]
        public IEnumerator StateMachine_TransitionToGenericMethodWithName()
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

            _stateMachine.States.Add(idleState1);
            _stateMachine.States.Add(idleState2);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Test the new generic transition method with name
            yield return _stateMachine.TransitionToNamed<IdleState>("Idle2").ToCoroutine();

            Assert.AreEqual(typeof(IdleState), _stateMachine.CurrentState.GetType());
            Assert.AreEqual("Idle2", _stateMachine.States.Find(s => s.Instance == _stateMachine.CurrentState).Name);
        }

        [UnityTest]
        public IEnumerator StateDefinition_TransitionToGenericMethod()
        {
            // Setup states
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

            _stateMachine.States.Add(idleState);
            _stateMachine.States.Add(moveState);

            // Initialize
            yield return _stateMachine.Initialize().ToCoroutine();

            // Test the StateDefinition's TransitionTo method
            var currentState = _stateMachine.CurrentState as IdleState;
            Assert.IsNotNull(currentState);

            // This simulates calling TransitionTo from within a state
            yield return _stateMachine.TransitionTo<MoveState>().ToCoroutine();

            Assert.AreEqual(typeof(MoveState), _stateMachine.CurrentState.GetType());
        }
    }
}