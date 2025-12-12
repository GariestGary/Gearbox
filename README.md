# 🎮 Gearbox - Async State Machine

<div align="center">

**A powerful, flexible state machine system for Unity with async/await support**

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

*Pure code-driven state management with visual inspector interface*


</div>

---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🚀 Quick Start](#-quick-start)
- [📖 API Reference](#-api-reference)
- [🎯 State Definition](#-state-definition)
- [⚙️ Configuration](#️-configuration)
- [📚 Examples](#-examples)
- [🧪 Testing](#-testing)
- [🔧 Advanced Usage](#-advanced-usage)
- [❓ FAQ](#-faq)
- [📝 License](#-license)

---

## ✨ Features

### 🎯 **Core Features**
- ✅ **Async/Await Support** - Fully asynchronous state transitions with UniTask
- ✅ **Type-Safe** - Strong typing with compile-time checks and generic methods
- ✅ **Inspector Integration** - Visual state configuration with foldouts and color coding
- ✅ **Transition Management** - Multiple transition methods (by name, type, instance)
- ✅ **Performance Optimized** - Cached type scanning and efficient updates
- ✅ **Data Passing** - Pass arbitrary data objects during state transitions

### 🎨 **Developer Experience**
- ✅ **Zero Boilerplate** - Simple state classes with automatic serialization
- ✅ **Unity Integration** - Direct access to `transform`, `GetComponent<T>`, `GetComponentInChildren<T>`, etc.
- ✅ **Dependency Injection** - `SetStateInitializeAction` for custom initialization logic

### 🔧 **Architecture**
- ✅ **Serializable States** - All `[SerializeField]` and public fields automatically saved
- ✅ **Runtime Flexibility** - Add/remove states dynamically with `AddState()`/`RemoveState()`
- ✅ **Lifecycle Hooks** - `OnEnter(fromState, data)`, `OnUpdate(delta)`, `OnExit(toState)`

---

## 🚀 Quick Start

### 1. Create a State Class

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

public class MyCustomState : StateDefinition
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private Color stateColor = Color.blue;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        Debug.Log($"Entering custom state from {(from?.GetType().Name ?? "null")}!");
        GetComponent<Renderer>().material.color = stateColor;
        
        // Use data parameter if provided
        if (data is float customSpeed)
        {
            speed = customSpeed;
            Debug.Log($"Custom speed set: {speed}");
        }
        
        await UniTask.CompletedTask;
    }

    protected override void OnUpdate(float delta)
    {
        transform.Translate(Vector3.forward * speed * delta);
    }

    protected override async UniTask OnExit(StateDefinition to)
    {
        Debug.Log($"Exiting custom state, transitioning to {(to?.GetType().Name ?? "null")}!");
        await UniTask.CompletedTask;
    }
}
```

### 2. Setup State Machine

1. **Add Component**: Attach `StateMachine` to any GameObject
2. **Add States**: Click **"Add State"** in the inspector
3. **Configure States**:
   - Enter a **Name** (e.g., "Idle", "Move", "Attack")
   - Select **Type** from dropdown (shows all `StateDefinition` subclasses)
   - Configure **Properties** (automatically displayed)

### 3. Runtime Usage

```csharp
// Get reference to state machine
var stateMachine = GetComponent<StateMachine>();

// Initialize (usually called automatically in Start if _initializeOnStart is true)
await stateMachine.Initialize();

// Transition by state name
await stateMachine.TransitionToNamed("Move");

// Transition by state name with data
await stateMachine.TransitionToNamed("Attack", customDamage: 25f);

// Transition by type (generic)
await stateMachine.TransitionTo<MoveState>();

// Transition by type with data
await stateMachine.TransitionTo<AttackState>(customTarget: enemyTransform);

// Transition by type with name filter (if multiple states of same type)
await stateMachine.TransitionToNamed<IdleState>("SpecialIdle");

// Manual update (if not using automatic updates)
stateMachine.DoUpdate(Time.deltaTime);
```

---

## 📖 API Reference

### StateMachine Component

| Method/Property | Description |
|-----------------|-------------|
| `States` | List of initialized state instances (read-only) |
| `CurrentState` | Currently active state instance |
| `Initialize()` | Initialize state instances and set initial state (async) |
| `SetStateInitializeAction(Action<StateDefinition>)` | Set callback for custom state initialization |
| `SetInitialState(StateDefinition)` | Programmatically set initial state |
| `AddState(StateDefinition)` | Add state instance at runtime |
| `RemoveState(StateDefinition)` | Remove state instance at runtime |
| `Clear()` | Clear all states and reset state machine |
| `TransitionToNamed(string, object)` | Transition to state by name with optional data |
| `TransitionToNamed<T>(string, object)` | Transition to state by type with name filter and data |
| `TransitionToState(StateDefinition, object)` | Transition to specific state instance with data |
| `TransitionTo<T>(object)` | Transition to state by type (generic, inferred) with data |
| `DoUpdate(float)` | Manual update call (pass Time.deltaTime) |

### StateDefinition Base Class

| Property/Method | Description |
|-----------------|-------------|
| `StateMachine` | Reference to owning StateMachine component |
| `Name` | State name (serialized) |
| `transform` | Shortcut to StateMachine.transform |
| `gameObject` | Shortcut to StateMachine.gameObject |
| `GetComponent<T>()` | Get component from StateMachine GameObject |
| `GetComponentInChildren<T>()` | Get component in children from StateMachine GameObject |
| `GetComponentInParent<T>()` | Get component in parent from StateMachine GameObject |
| `OnEnter(StateDefinition from, object data)` | Called when entering state (async) |
| `OnUpdate(float delta)` | Called every frame while active |
| `OnExit(StateDefinition to)` | Called when exiting state (async) |

### StateData Structure (Serialized)

| Property | Description |
|----------|-------------|
| `IsInitial` | Whether this state is the initial state |
| `Instance` | Serialized reference to StateDefinition instance |

---

## 🎯 State Definition

### Basic Structure

All states inherit from `StateDefinition`:

```csharp
using Cysharp.Threading.Tasks;
using VolumeBox.Gearbox.Core;

public class MyState : StateDefinition
{
    // Serializable fields are automatically saved/loaded
    [SerializeField] private float duration = 2.0f;
    [SerializeField] private Vector3 targetPosition;

    // Public fields also work
    public bool isActive = true;

    // Async lifecycle methods with proper signatures
    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        Debug.Log($"Entering MyState from {(from?.GetType().Name ?? "initial")}");
        // Use data parameter if needed
        await UniTask.CompletedTask;
    }
    
    protected override void OnUpdate(float delta)
    {
        // Update logic here
    }
    
    protected override async UniTask OnExit(StateDefinition to)
    {
        Debug.Log($"Exiting MyState, going to {(to?.GetType().Name ?? "unknown")}");
        await UniTask.CompletedTask;
    }
}
```

### Unity Integration

States have full access to Unity APIs:

```csharp
public class PhysicsState : StateDefinition
{
    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        // Direct access to transform
        transform.position = Vector3.zero;

        // Get components from the StateMachine's GameObject
        var rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);

        // Access child/parent components
        var childRenderer = GetComponentInChildren<Renderer>();
        var parent = GetComponentInParent<Transform>();
        
        await UniTask.CompletedTask;
    }
}
```

### State Communication

States can communicate through the StateMachine:

```csharp
public class AIState : StateDefinition
{
    protected override void OnUpdate(float delta)
    {
        // Check conditions and trigger transitions
        if (ShouldAttack())
        {
            // Transition using type-safe generic method
            StateMachine.TransitionTo<AttackState>().Forget();
        }
    }

    private bool ShouldAttack()
    {
        // Access other components for decision making
        var health = GetComponent<HealthComponent>();
        return health.currentHealth < health.maxHealth * 0.3f;
    }
}
```

---

## ⚙️ Configuration

### Assembly Scanning

For large projects, configure which assemblies to scan:

1. Go to **Edit → Preferences → Gearbox**
2. Add `.asmdef` files to the **Assembly Definitions** list
3. **Assembly-CSharp** is always included by default

This significantly improves performance by limiting type discovery to relevant assemblies.

### State Properties

All `[SerializeField]` and `public` fields are automatically serialized:

```csharp
public class ConfigurableState : StateDefinition
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private AnimationCurve easingCurve;

    [Header("Visual Settings")]
    public Color activeColor = Color.red;
    public Material overrideMaterial;

    // Private fields are not serialized
    private float currentProgress;
}
```

---

## 📚 Examples

### Complete State Machine Setup

```csharp
// 1. Create states
[StateCategory("AI/Basic")]
public class IdleState : StateDefinition
{
    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        Debug.Log("AI is now idle");
        GetComponent<Renderer>().material.color = Color.gray;
        await UniTask.CompletedTask;
    }
}

[StateCategory("AI/Basic")]
public class PatrolState : StateDefinition
{
    [SerializeField] private Transform[] waypoints;
    private int currentWaypoint;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        Debug.Log("Starting patrol");
        GetComponent<Renderer>().material.color = Color.blue;
        await UniTask.CompletedTask;
    }

    protected override void OnUpdate(float delta)
    {
        if (waypoints.Length == 0) return;

        var target = waypoints[currentWaypoint].position;
        transform.position = Vector3.MoveTowards(transform.position, target, 2f * delta);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        }
    }
}

// 2. Configure in Inspector:
// States:
//   - Name: "Idle", Type: IdleState, Transitions: ["Patrol"]
//   - Name: "Patrol", Type: PatrolState, Transitions: ["Idle"]

// 3. Runtime usage
await stateMachine.Initialize(); // Starts with "Idle"
await stateMachine.TransitionToNamed("Patrol"); // Switch to patrol
```

### Event-Driven States

```csharp
public class WaitingForInputState : StateDefinition
{
    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        Debug.Log("Waiting for player input...");
        await UniTask.CompletedTask;
    }

    protected override void OnUpdate(float delta)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Use Forget() for fire-and-forget async calls in synchronous methods
            StateMachine.TransitionToNamed("Gameplay").Forget();
        }
    }
}
```

### Timer-Based States

```csharp
public class TimedState : StateDefinition
{
    [SerializeField] private float duration = 3f;
    private float startTime;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        startTime = Time.time;
        Debug.Log($"Starting timed state for {duration}s");
        await UniTask.CompletedTask;
    }

    protected override void OnUpdate(float delta)
    {
        if (Time.time - startTime >= duration)
        {
            // Transition to next state when timer expires
            StateMachine.TransitionToNamed("NextState").Forget();
        }
    }
}
```

### Data Passing Example

```csharp
public class DataDrivenState : StateDefinition
{
    [SerializeField] private float defaultSpeed = 5f;
    private float currentSpeed;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        // Use data parameter to customize state behavior
        currentSpeed = data is float speed ? speed : defaultSpeed;
        Debug.Log($"State entered with speed: {currentSpeed}");
        await UniTask.CompletedTask;
    }

    protected override void OnUpdate(float delta)
    {
        transform.Translate(Vector3.forward * currentSpeed * delta);
    }
}

// Usage: Pass data when transitioning
await stateMachine.TransitionToNamed<DataDrivenState>("FastMove", 10f);
// or
await stateMachine.TransitionToNamed("DataDriven", customSpeed: 15f);
```

### Dynamic State Management

```csharp
public class StateFactory : MonoBehaviour
{
    private StateMachine stateMachine;

    void Start()
    {
        stateMachine = GetComponent<StateMachine>();

        // Create state instances programmatically
        var jumpState = new JumpState { Name = "Jump" };
        var dashState = new DashState { Name = "Dash" };

        // Add states to the state machine
        stateMachine.AddState(jumpState);
        stateMachine.AddState(dashState);

        // Set initial state
        stateMachine.SetInitialState(jumpState);

        // Reinitialize to apply changes
        stateMachine.Initialize().Forget();
    }
}
```

### Dependency Injection with SetStateInitializeAction

```csharp
public class StateDependencyInjector : MonoBehaviour
{
    private void Start()
    {
        var stateMachine = GetComponent<StateMachine>();
        
        // Inject dependencies into all states during initialization
        stateMachine.SetStateInitializeAction(state =>
        {
            if (state is IRequiresAudio audioState)
            {
                audioState.AudioSystem = GetComponent<AudioSystem>();
            }
            
            if (state is IRequiresConfig configState)
            {
                configState.Config = GetComponent<GameConfig>();
            }
        });
        
        // Initialize with injected dependencies
        stateMachine.Initialize().Forget();
    }
}

public interface IRequiresAudio
{
    AudioSystem AudioSystem { get; set; }
}

public interface IRequiresConfig
{
    GameConfig Config { get; set; }
}
```

### Runtime State Modification

```csharp
public class RuntimeStateModifier : MonoBehaviour
{
    private StateMachine stateMachine;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Add new state at runtime
            var newState = new CustomState { Name = "RuntimeAdded" };
            stateMachine.AddState(newState);
            
            // Transition to the new state
            stateMachine.TransitionToNamed("RuntimeAdded").Forget();
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Remove current state (if not active)
            if (stateMachine.CurrentState != null &&
                stateMachine.CurrentState.Name != "EssentialState")
            {
                stateMachine.RemoveState(stateMachine.CurrentState);
            }
        }
    }
}
```

### Manual Update Loop Integration

```csharp
public class FixedUpdateStateMachine : MonoBehaviour
{
    private StateMachine stateMachine;

    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        stateMachine.Initialize().Forget();
    }

    void FixedUpdate()
    {
        // Manually update state machine with fixed delta time
        stateMachine.DoUpdate(Time.fixedDeltaTime);
    }
}

public class CustomUpdateLoop : MonoBehaviour
{
    private StateMachine stateMachine;
    private float accumulatedTime;

    void Start()
    {
        stateMachine = GetComponent<StateMachine>();
        stateMachine.Initialize().Forget();
    }

    void Update()
    {
        // Custom time scaling
        float scaledDelta = Time.deltaTime * Time.timeScale;
        stateMachine.DoUpdate(scaledDelta);
    }
}
```

### State Communication Patterns

```csharp
// Method 1: Event-based communication
public class EventDrivenState : StateDefinition
{
    public static event Action<EventDrivenState> OnEntered;
    public static event Action<EventDrivenState> OnExited;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        OnEntered?.Invoke(this);
        await UniTask.CompletedTask;
    }

    protected override async UniTask OnExit(StateDefinition to)
    {
        OnExited?.Invoke(this);
        await UniTask.CompletedTask;
    }
}

// Method 2: Shared data component
public class SharedDataState : StateDefinition
{
    private SharedDataComponent sharedData;

    protected override async UniTask OnEnter(StateDefinition from, object data)
    {
        sharedData = GetComponent<SharedDataComponent>();
        sharedData.CurrentState = GetType().Name;
        sharedData.StateEnterTime = Time.time;
        await UniTask.CompletedTask;
    }
}

// Method 3: Message passing via StateMachine
public class MessagePassingState : StateDefinition
{
    public void SendMessageToOtherStates(string message)
    {
        foreach (var state in StateMachine.States)
        {
            if (state != this && state is IMessageReceiver receiver)
            {
                receiver.ReceiveMessage(message);
            }
        }
    }
}

public interface IMessageReceiver
{
    void ReceiveMessage(string message);
}
```

### Performance Considerations

- **Assembly Scanning**: Configure via **Edit → Preferences → Gearbox** to limit scanned assemblies
- **State Updates**: Keep `OnUpdate` methods lightweight; use `DoUpdate` for manual control
- **Object Pooling**: Reuse state instances when possible with `AddState`/`RemoveState`
- **Transition Validation**: Cache frequently used transition lookups
- **Memory Management**: Use `Clear()` to release all state instances when needed
- **Async Operations**: Use `Forget()` for fire-and-forget transitions in synchronous contexts
