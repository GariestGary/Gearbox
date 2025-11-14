# 🎮 Gearbox - Async State Machine

> [!WARNING]
> Some parts of this package was written by AI and need to be tested properly. Not recommended to use in production. Later this warning will gone :)

<div align="center">

**A powerful, flexible state machine system for Unity with async/await support**

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black.svg)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-4.7.1+-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

*No more graph editors - Pure code-driven state management with visual inspector interface*


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
- ✅ **Async/Await Support** - Fully asynchronous state transitions
- ✅ **Type-Safe** - Strong typing with compile-time checks
- ✅ **Inspector Integration** - Visual state configuration in Unity
- ✅ **Transition Management** - Named transitions with validation
- ✅ **Performance Optimized** - Cached type scanning and efficient updates

### 🎨 **Developer Experience**
- ✅ **Zero Boilerplate** - Simple state classes with automatic serialization
- ✅ **Unity Integration** - Direct access to `transform`, `GetComponent<T>`, etc.
- ✅ **Visual Feedback** - Color-coded states and real-time editing
- ✅ **Assembly Scanning** - Configurable type discovery for large projects
- ✅ **Comprehensive Testing** - Unit and integration tests included

### 🔧 **Architecture**
- ✅ **Component-Based** - Attach to GameObjects like any Unity component
- ✅ **Serializable States** - All state properties automatically saved
- ✅ **Runtime Flexibility** - Add/remove states dynamically
- ✅ **Memory Efficient** - Lazy instantiation and smart caching

---

## 🚀 Quick Start

### 1. Create a State Class

```csharp
using System.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

public class MyCustomState : StateDefinition
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private Color stateColor = Color.blue;

    public override async UniTask OnEnter()
    {
        Debug.Log("Entering custom state!");
        GetComponent<Renderer>().material.color = stateColor;
        await UniTask.CompletedTask;
    }

    public override async UniTask OnUpdate()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        await UniTask.CompletedTask;
    }

    public override async UniTask OnExit()
    {
        Debug.Log("Exiting custom state!");
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
   - Add **Transitions** (dropdown of other state names)

### 3. Runtime Usage

```csharp
// Get reference to state machine
var stateMachine = GetComponent<StateMachine>();

// Initialize (usually called automatically in Start)
await stateMachine.InitializeStateMachine();

// Transition by state name
await stateMachine.TransitionToState("Move");

// Transition by trigger index
await stateMachine.TriggerTransition(currentState, 0);

// Get available transitions
var transitions = stateMachine.GetAvailableTransitions(currentState);
```

---

## 📖 API Reference

### StateMachine Component

| Method/Property | Description |
|-----------------|-------------|
| `States` | List of configured state data |
| `CurrentState` | Currently active state instance |
| `InitializeStateMachine()` | Initialize state instances and set initial state |
| `TransitionToState(string)` | Transition to state by name |
| `TransitionToState(StateDefinition)` | Transition to specific state instance |
| `TriggerTransition(StateDefinition, int)` | Trigger transition by index from current state |
| `GetAvailableTransitions(StateDefinition)` | Get list of available transition names |

### StateDefinition Base Class

| Property/Method | Description |
|-----------------|-------------|
| `StateMachine` | Reference to owning StateMachine component |
| `transform` | Shortcut to StateMachine.transform |
| `gameObject` | Shortcut to StateMachine.gameObject |
| `GetComponent<T>()` | Get component from StateMachine GameObject |
| `OnEnter()` | Called when entering state (async) |
| `OnUpdate()` | Called every frame while active (async) |
| `OnExit()` | Called when exiting state (async) |

### StateData Structure

| Property | Description |
|----------|-------------|
| `name` | Human-readable state name |
| `stateTypeName` | Fully qualified type name |
| `transitionNames` | List of target state names |
| `instance` | Runtime state instance |

---

## 🎯 State Definition

### Basic Structure

All states inherit from `StateDefinition`:

```csharp
using VolumeBox.Gearbox.Core;

public class MyState : StateDefinition
{
    // Serializable fields are automatically saved/loaded
    [SerializeField] private float duration = 2.0f;
    [SerializeField] private Vector3 targetPosition;

    // Public fields also work
    public bool isActive = true;

    // Async lifecycle methods
    public override async UniTask OnEnter() { /* ... */ }
    public override async UniTask OnUpdate() { /* ... */ }
    public override async UniTask OnExit() { /* ... */ }
}
```

### Unity Integration

States have full access to Unity APIs:

```csharp
public class PhysicsState : StateDefinition
{
    public override async UniTask OnEnter()
    {
        // Direct access to transform
        transform.position = Vector3.zero;

        // Get components from the StateMachine's GameObject
        var rb = GetComponent<Rigidbody>();
        rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);

        // Access child/parent components
        var childRenderer = GetComponentInChildren<Renderer>();
        var parent = GetComponentInParent<Transform>();
    }
}
```

### State Communication

States can communicate through the StateMachine:

```csharp
public class AIState : StateDefinition
{
    public override async UniTask OnUpdate()
    {
        // Check conditions and trigger transitions
        if (ShouldAttack())
        {
            await StateMachine.TransitionToState("Attack");
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
public class IdleState : StateDefinition
{
    public override async UniTask OnEnter()
    {
        Debug.Log("AI is now idle");
        GetComponent<Renderer>().material.color = Color.gray;
    }
}

public class PatrolState : StateDefinition
{
    [SerializeField] private Transform[] waypoints;
    private int currentWaypoint;

    public override async UniTask OnEnter()
    {
        Debug.Log("Starting patrol");
        GetComponent<Renderer>().material.color = Color.blue;
    }

    public override async UniTask OnUpdate()
    {
        if (waypoints.Length == 0) return;

        var target = waypoints[currentWaypoint].position;
        transform.position = Vector3.MoveTowards(transform.position, target, 2f * Time.deltaTime);

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
await stateMachine.InitializeStateMachine(); // Starts with "Idle"
await stateMachine.TransitionToState("Patrol"); // Switch to patrol
```

### Event-Driven States

```csharp
public class WaitingForInputState : StateDefinition
{
    public override async UniTask OnEnter()
    {
        Debug.Log("Waiting for player input...");
    }

    public override async UniTask OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            await StateMachine.TransitionToState("Gameplay");
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

    public override async UniTask OnEnter()
    {
        startTime = Time.time;
        Debug.Log($"Starting timed state for {duration}s");
    }

    public override async UniTask OnUpdate()
    {
        if (Time.time - startTime >= duration)
        {
            await StateMachine.TransitionToState("NextState");
        }
    }
}
```

---

## 🧪 Testing

### Automated Tests

Run comprehensive tests via **Window → General → Test Runner**:

- **Unit Tests**: Core functionality validation
- **Integration Tests**: End-to-end state machine flows
- **Async Tests**: Proper async operation handling

### Manual Testing

Use the included `StateMachineManualTest` component:

```csharp
// Attach to GameObject with StateMachine
// Press Space: Cycle through transitions
// Press R: Random transition
// View debug logs and on-screen status
```

### Example Test Structure

```csharp
[Test]
public void StateMachine_BasicTransition()
{
    var stateMachine = new GameObject().AddComponent<StateMachine>();

    // Setup states
    var idleData = new StateData { name = "Idle", stateTypeName = typeof(IdleState).AssemblyQualifiedName };
    var moveData = new StateData { name = "Move", stateTypeName = typeof(MoveState).AssemblyQualifiedName };
    stateMachine.States.AddRange(new[] { idleData, moveData });

    // Initialize and test
    await stateMachine.InitializeStateMachine();
    await stateMachine.TransitionToState("Move");

    Assert.AreEqual(typeof(MoveState), stateMachine.CurrentState.GetType());
}
```

---

## 🔧 Advanced Usage

### Dynamic State Creation

```csharp
public class StateFactory : MonoBehaviour
{
    private StateMachine stateMachine;

    void Start()
    {
        stateMachine = GetComponent<StateMachine>();

        // Add states programmatically
        AddNewState("Jump", typeof(JumpState));
        AddNewState("Dash", typeof(DashState));

        // Configure transitions
        // (Would need to extend StateMachine API for runtime configuration)
    }

    private void AddNewState(string name, Type stateType)
    {
        var stateData = new StateData
        {
            name = name,
            stateTypeName = stateType.AssemblyQualifiedName
        };
        stateMachine.States.Add(stateData);
    }
}
```

### State Communication Patterns

```csharp
// Method 1: Direct state communication
public class CommunicationState : StateDefinition
{
    public static event Action<string> OnStateMessage;

    public override async UniTask OnEnter()
    {
        OnStateMessage?.Invoke("Entered " + GetType().Name);
    }
}

// Method 2: Component-based messaging
public class MessengerState : StateDefinition
{
    public override async UniTask OnEnter()
    {
        var messenger = GetComponent<StateMessenger>();
        messenger.SendMessage("StateChanged", GetType().Name);
    }
}
```

### Performance Considerations

- **Assembly Scanning**: Use preferences to limit scanned assemblies
- **State Updates**: Keep `OnUpdate` methods lightweight
- **Object Pooling**: Reuse state instances when possible
- **Transition Validation**: Cache transition lookups for frequently used states

---

## ❓ FAQ

### **Q: How is this different from Unity's Animator?**
**A:** Animator focuses on animation blending and Mecanim graphs. Gearbox is a code-first state machine for game logic, AI behaviors, and UI flows with full async support.

### **Q: Can I have multiple state machines on one GameObject?**
**A:** Yes! Each StateMachine component operates independently. Use different components for different behaviors (e.g., AI states, UI states).

### **Q: Are states singletons or instances?**
**A:** States are instantiated per StateMachine. Each StateMachine has its own instances with separate serialized data.

### **Q: How do transitions work?**
**A:** Transitions are defined per state as a list of target state names. Use `TriggerTransition(currentState, index)` to activate them by index.

### **Q: Can I modify states at runtime?**
**A:** Yes, but changes to the States list require reinitialization. Individual state properties can be modified directly.

### **Q: What's the performance impact?**
**A:** Minimal! Type scanning is cached, async operations are efficient, and only active states consume Update cycles.

### **Q: Can I use this for UI state management?**
**A:** Absolutely! Perfect for menu systems, dialog flows, and complex UI state machines.

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Made with ❤️ for Unity developers**

[⭐ Star on GitHub](https://github.com/your-repo) • [📖 Documentation](https://your-docs) • [🐛 Report Issues](https://github.com/your-repo/issues)

</div>
