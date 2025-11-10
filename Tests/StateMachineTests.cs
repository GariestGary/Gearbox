using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Tests
{
    public class StateMachineTests
    {
        private GameObject testGameObject;
        private StateMachine stateMachine;
        
        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestStateMachine");
            stateMachine = testGameObject.AddComponent<StateMachine>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        // Test state definitions for testing
        [System.Serializable]
        public class TestStateA : StateDefinition
        {
            public bool entered = false;
            public bool exited = false;
            public int updateCount = 0;
            
            public override async UniTask OnEnter()
            {
                entered = true;
                await UniTask.CompletedTask;
            }
            
            public override async UniTask OnUpdate()
            {
                updateCount++;
                await UniTask.CompletedTask;
            }
            
            public override async UniTask OnExit()
            {
                exited = true;
                await UniTask.CompletedTask;
            }
        }
        
        [System.Serializable]
        public class TestStateB : StateDefinition
        {
            public bool entered = false;
            public bool exited = false;
            
            public override async UniTask OnEnter()
            {
                entered = true;
                await UniTask.CompletedTask;
            }
            
            public override async UniTask OnExit()
            {
                exited = true;
                await UniTask.CompletedTask;
            }
        }
        
        [System.Serializable]
        public class AsyncTestState : StateDefinition
        {
            public bool asyncOperationCompleted = false;
            
            public override async UniTask OnEnter()
            {
                await UniTask.Delay(100); // Simulate async operation
                asyncOperationCompleted = true;
            }
        }
        
        [UnityTest]
        public IEnumerator TestStateMachineInitialization()
        {
            // Arrange
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            stateMachine.Nodes.Add(nodeA);
            
            // Act - Initialize manually since Start() won't be called in tests
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            // Assert
            Assert.IsNotNull(stateMachine.CurrentState, "Current state should not be null after initialization");
            Assert.AreEqual(nodeA.id, stateMachine.CurrentState.id, "Current state should be the initial state");
            var stateA = stateMachine.CurrentState.state as TestStateA;
            Assert.IsTrue(stateA.entered, "OnEnter should have been called for initial state");
        }
        
        [UnityTest]
        public IEnumerator TestValidStateTransition()
        {
            // Arrange
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            var nodeB = new StateNode
            {
                id = "stateB",
                title = "State B",
                state = new TestStateB()
            };
            
            stateMachine.Nodes.Add(nodeA);
            stateMachine.Nodes.Add(nodeB);
            
            var transition = new StateTransition
            {
                fromId = "stateA",
                toId = "stateB"
            };
            stateMachine.Transitions.Add(transition);
            
            // Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            var stateA = nodeA.state as TestStateA;
            var stateB = nodeB.state as TestStateB;
            
            // Act - Transition to state B
            yield return stateMachine.TransitionToState("stateB").ToCoroutine();
            
            // Assert
            Assert.AreEqual(nodeB.id, stateMachine.CurrentState.id, "Current state should be state B after transition");
            Assert.IsTrue(stateA.exited, "State A should have exited");
            Assert.IsTrue(stateB.entered, "State B should have entered");
        }
        
        [UnityTest]
        public IEnumerator TestInvalidStateTransition()
        {
            // Arrange
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            var nodeB = new StateNode
            {
                id = "stateB",
                title = "State B",
                state = new TestStateB()
            };
            
            stateMachine.Nodes.Add(nodeA);
            stateMachine.Nodes.Add(nodeB);
            
            // No transition between A and B
            
            // Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            var stateA = nodeA.state as TestStateA;
            var stateB = nodeB.state as TestStateB;
            
            // Act - Try to transition to state B (should fail)
            yield return stateMachine.TransitionToState("stateB").ToCoroutine();
            
            // Assert
            Assert.AreEqual(nodeA.id, stateMachine.CurrentState.id, "Should still be in state A after invalid transition");
            Assert.IsFalse(stateA.exited, "State A should not have exited for invalid transition");
            Assert.IsFalse(stateB.entered, "State B should not have entered for invalid transition");
        }
        
        [UnityTest]
        public IEnumerator TestAsyncStateOperations()
        {
            // Arrange
            var asyncState = new StateNode
            {
                id = "asyncState",
                title = "Async State",
                IsInitialState = true,
                state = new AsyncTestState()
            };
            
            stateMachine.Nodes.Add(asyncState);
            
            // Act - Initialize and wait for async operation
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            // Assert
            Assert.IsNotNull(stateMachine.CurrentState, "Current state should not be null");
            var state = stateMachine.CurrentState.state as AsyncTestState;
            Assert.IsNotNull(state, "State should not be null");
            Assert.IsTrue(state.asyncOperationCompleted, "Async operation should have completed");
        }
        
        [UnityTest]
        public IEnumerator TestStateUpdateLoop()
        {
            // Arrange
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            stateMachine.Nodes.Add(nodeA);
            
            // Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            var stateA = nodeA.state as TestStateA;
            int initialUpdateCount = stateA.updateCount;
            
            // Act - Manually call TestUpdate multiple times to simulate Unity's Update loop
            for (int i = 0; i < 3; i++)
            {
                yield return stateMachine.TestUpdate().ToCoroutine();
                yield return new WaitForSeconds(0.05f); // Small delay between updates
            }
            
            // Assert
            Assert.Greater(stateA.updateCount, initialUpdateCount, "Update count should have increased after multiple Update calls");
        }
        
        [UnityTest]
        public IEnumerator TestMultipleTransitions()
        {
            // Arrange - Create a chain of states: A -> B -> C
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            var nodeB = new StateNode
            {
                id = "stateB",
                title = "State B",
                state = new TestStateB()
            };
            
            var nodeC = new StateNode
            {
                id = "stateC",
                title = "State C",
                state = new TestStateA() // Reuse TestStateA type
            };
            
            stateMachine.Nodes.Add(nodeA);
            stateMachine.Nodes.Add(nodeB);
            stateMachine.Nodes.Add(nodeC);
            
            // Create transitions: A->B and B->C
            stateMachine.Transitions.Add(new StateTransition { fromId = "stateA", toId = "stateB" });
            stateMachine.Transitions.Add(new StateTransition { fromId = "stateB", toId = "stateC" });
            
            // Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            var stateA = nodeA.state as TestStateA;
            var stateB = nodeB.state as TestStateB;
            var stateC = nodeC.state as TestStateA;
            
            // Act - Transition through the chain
            yield return stateMachine.TransitionToState("stateB").ToCoroutine();
            yield return stateMachine.TransitionToState("stateC").ToCoroutine();
            
            // Assert
            Assert.AreEqual(nodeC.id, stateMachine.CurrentState.id, "Current state should be state C");
            Assert.IsTrue(stateA.exited, "State A should have exited");
            Assert.IsTrue(stateB.entered, "State B should have entered");
            Assert.IsTrue(stateB.exited, "State B should have exited");
            Assert.IsTrue(stateC.entered, "State C should have entered");
        }
        
        [UnityTest]
        public IEnumerator TestTransitionToNonExistentState()
        {
            // Arrange
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = new TestStateA()
            };
            
            stateMachine.Nodes.Add(nodeA);
            
            // Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            var stateA = nodeA.state as TestStateA;
            
            // Expect the error log
            LogAssert.Expect(LogType.Error, "State with ID 'nonExistentState' not found.");
            
            // Act - Try to transition to non-existent state
            yield return stateMachine.TransitionToState("nonExistentState").ToCoroutine();
            
            // Assert
            Assert.AreEqual(nodeA.id, stateMachine.CurrentState.id, "Should still be in state A after invalid transition");
            Assert.IsFalse(stateA.exited, "State A should not have exited for non-existent state");
        }
        
        [UnityTest]
        public IEnumerator TestInitialStateWithoutExplicitSetting()
        {
            // Arrange - Create nodes without setting initial state
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                state = new TestStateA()
            };
            
            var nodeB = new StateNode
            {
                id = "stateB",
                title = "State B",
                state = new TestStateB()
            };
            
            stateMachine.Nodes.Add(nodeA);
            stateMachine.Nodes.Add(nodeB);
            
            // Act - Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            // Assert - Should use first node as initial state
            Assert.IsNotNull(stateMachine.CurrentState, "Current state should not be null");
            Assert.AreEqual(nodeA.id, stateMachine.CurrentState.id, "Should use first node as initial state");
        }
        
        [UnityTest]
        public IEnumerator TestStateWithNullStateDefinition()
        {
            // Arrange - Create node with null state
            var nodeA = new StateNode
            {
                id = "stateA",
                title = "State A",
                IsInitialState = true,
                state = null
            };
            
            stateMachine.Nodes.Add(nodeA);
            
            // Act - Initialize state machine
            yield return stateMachine.InitializeStateMachine().ToCoroutine();
            
            // Assert - Should not crash and should set current state
            Assert.IsNotNull(stateMachine.CurrentState, "Current state should not be null");
            Assert.AreEqual(nodeA.id, stateMachine.CurrentState.id, "Current state should be set even with null state definition");
        }
    }
}