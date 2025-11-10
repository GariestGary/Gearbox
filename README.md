# Gearbox - Async State Machine

A Unity state machine system with visual graph editing and async state support.

## Quick Start

### 1. Create a State Class

Create a new class that inherits from `StateDefinition`:

```csharp
using System.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

public class MyState : StateDefinition
{
    [StateVariable]
    public float myFloat = 10.0f;
    
    [StateVariable]
    public GameObject myObject;
    
    public override async Task OnEnter()
    {
        Debug.Log("Entering state");
        await Task.CompletedTask;
    }
    
    public override async Task OnUpdate()
    {
        // Update logic
        await Task.CompletedTask;
    }
    
    public override async Task OnExit()
    {
        Debug.Log("Exiting state");
        await Task.CompletedTask;
    }
}
```

### 2. Add StateMachine Component

1. Add a `StateMachine` component to a GameObject
2. Click "Open State Machine Graph" in the inspector
3. Right-click in the graph window to add states
4. Select your state type from the dropdown in each state node

### 3. Configure State Variables

1. In the graph window, select a state type for each node
2. In the inspector, expand the state to see variables marked with `[StateVariable]`
3. Drag & drop references or enter values manually

## StateVariable Attribute

The `[StateVariable]` attribute marks fields that should be visible and editable in the inspector.

### Supported Types

✅ **Basic Types:**
- `int`, `float`, `double`, `bool`, `string`
- `Vector2`, `Vector3`, `Vector4`
- `Color`, `Rect`, `Bounds`
- `Quaternion`

✅ **Unity Object References:**
- `GameObject`
- `Component` types (Transform, Rigidbody, etc.)
- `ScriptableObject`
- Any type inheriting from `UnityEngine.Object`

✅ **Custom Serializable Types:**
- Classes marked with `[System.Serializable]`
- Structs marked with `[System.Serializable]`
- Arrays and Lists of serializable types

✅ **Enums:**
- Any enum type

### Important Rules

1. **Fields must be public** - Private fields won't be serialized properly
2. **Use [System.Serializable] for custom types** - Custom classes/structs must be serializable
3. **Only mark fields you want to edit** - Fields without `[StateVariable]` won't appear in inspector
4. **Unity Object references can be null** - Make sure to check for null before using

### Example with All Types

```csharp
public class CompleteExampleState : StateDefinition
{
    // Basic types
    [StateVariable]
    public int health = 100;
    
    [StateVariable]
    public float speed = 5.0f;
    
    [StateVariable]
    public string stateName = "Example";
    
    [StateVariable]
    public bool isActive = true;
    
    // Unity types
    [StateVariable]
    public Vector3 position;
    
    [StateVariable]
    public Color stateColor = Color.red;
    
    // Unity Object references
    [StateVariable]
    public GameObject target;
    
    [StateVariable]
    public Transform spawnPoint;
    
    [StateVariable]
    public Rigidbody rb;
    
    // Arrays
    [StateVariable]
    public Transform[] waypoints;
    
    [StateVariable]
    public int[] scores;
    
    // Custom serializable type
    [StateVariable]
    public MyCustomData data;
    
    // Enum
    [StateVariable]
    public StateType type;
    
    // This field won't show in inspector (no attribute)
    private int internalCounter = 0;
    
    public override async Task OnEnter() { await Task.CompletedTask; }
    public override async Task OnUpdate() { await Task.CompletedTask; }
    public override async Task OnExit() { await Task.CompletedTask; }
}

[System.Serializable]
public class MyCustomData
{
    public string name;
    public int value;
}

public enum StateType
{
    TypeA,
    TypeB,
    TypeC
}
```

## Common Mistakes

### ❌ Wrong: Private Field
```csharp
[StateVariable]
private float myValue; // Won't work properly
```

### ✅ Correct: Public Field
```csharp
[StateVariable]
public float myValue; // Works!
```

### ❌ Wrong: Non-Serializable Custom Type
```csharp
[StateVariable]
public MyClass myClass; // Won't serialize
```

### ✅ Correct: Serializable Custom Type
```csharp
[System.Serializable]
public class MyClass
{
    public int value;
}

[StateVariable]
public MyClass myClass; // Works!
```

### ❌ Wrong: Property Instead of Field
```csharp
[StateVariable]
public float MyValue { get; set; } // Won't work
```

### ✅ Correct: Field
```csharp
[StateVariable]
public float myValue; // Works!
```

## Async Methods

All state methods are async and return `Task`:

```csharp
public override async Task OnEnter()
{
    // Wait for 2 seconds
    await Task.Delay(2000);
    
    // Or do async operations
    await SomeAsyncMethod();
    
    // Or return immediately
    await Task.CompletedTask;
}
```

## Settings

Configure which assemblies to search for state types:

1. Go to `Tools > Gearbox > Settings`
2. Add assembly definition files (.asmdef) to the list
3. The system will only search these assemblies (plus Assembly-CSharp) for state types

This significantly improves performance when searching for state types.

## Examples

See `Assets/Scripts/Gearbox/Examples/ExampleStates.cs` for complete working examples.
