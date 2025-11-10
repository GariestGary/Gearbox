using Cysharp.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Examples
{
    // ============================================
    // BASIC STATE EXAMPLE
    // ============================================
    
    /// <summary>
    /// Simple state with basic variables
    /// </summary>
    public class IdleState : StateDefinition
    {
        [StateVariable]
        public float waitTime = 2.0f;
        
        [StateVariable]
        public string stateName = "Idle";
        
        [StateVariable]
        public int priority = 1;

        [SerializeField, StateVariable] private GameObject obj;
        [StateVariable] public Rigidbody rb;
        
        public override async UniTask OnEnter()
        {
            Debug.Log($"Entering {stateName} state, will wait for {waitTime} seconds");
            await UniTask.Delay((int)(waitTime * 1000));
        }
        
        public override async UniTask OnUpdate()
        {
            // Update logic here
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnExit()
        {
            Debug.Log($"Exiting {stateName} state");
            await UniTask.CompletedTask;
        }
    }
    
    // ============================================
    // STATE WITH UNITY OBJECT REFERENCES
    // ============================================
    
    /// <summary>
    /// State that references Unity objects (GameObjects, Components, etc.)
    /// </summary>
    public class ChaseState : StateDefinition
    {
        [StateVariable]
        public GameObject target;
        
        [StateVariable]
        public Transform chasePoint;
        
        [StateVariable]
        public float chaseSpeed = 5.0f;
        
        [StateVariable]
        public float maxDistance = 10.0f;
        
        public override async UniTask OnEnter()
        {
            if (target != null)
            {
                Debug.Log($"Starting to chase {target.name} at speed {chaseSpeed}");
            }
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnUpdate()
        {
            if (target != null && chasePoint != null)
            {
                // Chase logic here
                var direction = (target.transform.position - chasePoint.position).normalized;
                // Move towards target...
            }
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnExit()
        {
            Debug.Log("Stopped chasing");
            await UniTask.CompletedTask;
        }
    }
    
    // ============================================
    // STATE WITH CUSTOM TYPES
    // ============================================
    
    /// <summary>
    /// Custom data structure
    /// </summary>
    [System.Serializable]
    public class StateData
    {
        public string name;
        public int value;
    }
    
    /// <summary>
    /// State using custom serializable types
    /// </summary>
    public class CustomDataState : StateDefinition
    {
        [StateVariable]
        public StateData myData;
        
        [StateVariable]
        public Vector3 position;
        
        [StateVariable]
        public Color stateColor = Color.red;
        
        public override async UniTask OnEnter()
        {
            if (myData != null)
            {
                Debug.Log($"State data: {myData.name} = {myData.value}");
            }
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnUpdate()
        {
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnExit()
        {
            await UniTask.CompletedTask;
        }
    }
    
    // ============================================
    // STATE WITH ENUMS
    // ============================================
    
    public enum StateType
    {
        Passive,
        Aggressive,
        Defensive
    }
    
    public class EnumState : StateDefinition
    {
        [StateVariable]
        public StateType behaviorType = StateType.Passive;
        
        [StateVariable]
        public bool isActive = true;
        
        public override async UniTask OnEnter()
        {
            Debug.Log($"Entering {behaviorType} state");
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnUpdate()
        {
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnExit()
        {
            await UniTask.CompletedTask;
        }
    }
    
    // ============================================
    // COMPLEX STATE EXAMPLE
    // ============================================
    
    public class PatrolState : StateDefinition
    {
        [StateVariable]
        public Transform[] waypoints;
        
        [StateVariable]
        public float moveSpeed = 3.0f;
        
        [StateVariable]
        public float waitAtWaypoint = 1.0f;
        
        [StateVariable]
        public bool loopPatrol = true;
        
        [StateVariable]
        public GameObject patrolTarget; // Can be null
        
        // Fields WITHOUT [StateVariable] won't show in inspector
        private int currentWaypointIndex = 0;
        
        public override async UniTask OnEnter()
        {
            currentWaypointIndex = 0;
            Debug.Log($"Starting patrol with {waypoints?.Length ?? 0} waypoints");
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnUpdate()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                await UniTask.CompletedTask;
                return;
            }
            
            // Patrol logic here
            // Move to current waypoint, then wait, then move to next
            
            await UniTask.CompletedTask;
        }
        
        public override async UniTask OnExit()
        {
            Debug.Log("Stopped patrolling");
            await UniTask.CompletedTask;
        }
    }
}

