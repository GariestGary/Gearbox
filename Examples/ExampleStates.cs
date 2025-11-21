using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Examples
{
    [StateCategory("Basic/Movement")]
    [System.Serializable]
    public class IdleState : StateDefinition
    {
        [SerializeField] private float idleTime = 2.0f;
        [SerializeField] private Color idleColor = Color.blue;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Entering Idle state from {fromState.GetType().Name} for {idleTime} seconds");
            }
            else
            {
                Debug.Log($"Entering Idle state (initial) for {idleTime} seconds");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = idleColor;
            }
            await UniTask.Delay((int)(idleTime * 1000));
        }

        public override async UniTask OnUpdate()
        {
            // Idle state just waits
            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Exiting Idle state, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Exiting Idle state");
            }
            await UniTask.CompletedTask;
        }
    }

    [StateCategory("Basic/Movement")]
    [System.Serializable]
    public class MoveState : StateDefinition
    {
        [SerializeField] private Vector3 targetPosition = new Vector3(5, 0, 5);
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private Color moveColor = Color.green;

        private Vector3 startPosition;
        private float journeyLength;
        private float startTime;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            // Check if custom target position was passed via data
            if (data is Vector3 customTarget)
            {
                targetPosition = customTarget;
                Debug.Log($"Moving to custom position {targetPosition} (from data)");
            }
            else if (data != null)
            {
                Debug.Log($"Received data: {data}");
            }
            
            if (fromState != null)
            {
                Debug.Log($"Moving to position {targetPosition} from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log($"Moving to position {targetPosition}");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = moveColor;
            }

            startPosition = transform.position;
            journeyLength = Vector3.Distance(startPosition, targetPosition);
            startTime = Time.time;

            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distCovered / journeyLength;
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Finished moving, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Finished moving");
            }
            await UniTask.CompletedTask;
        }
    }

    [StateCategory("Combat")]
    [System.Serializable]
    public class AttackState : StateDefinition
    {
        [SerializeField] private float attackDamage = 10.0f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private Color attackColor = Color.red;

        private float lastAttackTime;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            // Check if damage was modified via data
            if (data is float customDamage && customDamage > 0)
            {
                attackDamage = customDamage;
                Debug.Log($"Starting attack with custom damage {attackDamage} (from data)");
            }
            else if (fromState != null)
            {
                Debug.Log($"Starting attack with {attackDamage} damage from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log($"Starting attack with {attackDamage} damage");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = attackColor;
            }
            lastAttackTime = Time.time - attackCooldown; // Allow immediate attack
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Debug.Log($"Attacking for {attackDamage} damage!");
                lastAttackTime = Time.time;
                // Attack logic would go here
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Stopping attack, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Stopping attack");
            }
            await UniTask.CompletedTask;
        }
    }

    [StateCategory("AI/Patrol")]
    [System.Serializable]
    public class PatrolState : StateDefinition
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointThreshold = 0.1f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private Color patrolColor = Color.yellow;

        private int currentWaypointIndex = 0;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Starting patrol from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log("Starting patrol");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = patrolColor;
            }
            if (waypoints.Length == 0)
            {
                Debug.LogWarning("No waypoints assigned to patrol state!");
            }
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (waypoints.Length == 0) return;

            Vector3 target = waypoints[currentWaypointIndex].position;
            Vector3 currentPos = transform.position;

            // Move towards current waypoint
            Vector3 direction = (target - currentPos).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Check if reached waypoint
            if (Vector3.Distance(currentPos, target) < waypointThreshold)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                Debug.Log($"Reached waypoint {currentWaypointIndex}, moving to next");
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Stopping patrol, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Stopping patrol");
            }
            await UniTask.CompletedTask;
        }
    }

    [StateCategory("AI/Flee")]
    [System.Serializable]
    public class FleeState : StateDefinition
    {
        [SerializeField] private Transform fleeTarget;
        [SerializeField] private float fleeDistance = 10.0f;
        [SerializeField] private float fleeSpeed = 3.0f;
        [SerializeField] private Color fleeColor = Color.magenta;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Starting to flee from {fromState.GetType().Name}!");
            }
            else
            {
                Debug.Log("Starting to flee!");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = fleeColor;
            }
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (fleeTarget != null)
            {
                Vector3 fleeDirection = (transform.position - fleeTarget.position).normalized;
                transform.position += fleeDirection * fleeSpeed * Time.deltaTime;

                // Check if far enough away
                if (Vector3.Distance(transform.position, fleeTarget.position) >= fleeDistance)
                {
                    Debug.Log("Fled far enough, can stop fleeing");
                }
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Stopped fleeing, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Stopped fleeing");
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class RotateState : StateDefinition
    {
        [SerializeField] private float rotationSpeed = 90.0f; // degrees per second
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private Color rotateColor = Color.cyan;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Starting rotation from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log("Starting rotation");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = rotateColor;
            }
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Stopping rotation, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Stopping rotation");
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class JumpState : StateDefinition
    {
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float jumpCooldown = 2.0f;
        [SerializeField] private Color jumpColor = Color.white;

        private float lastJumpTime;
        private Rigidbody rb;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Preparing to jump from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log("Preparing to jump");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = jumpColor;
            }

            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("JumpState requires a Rigidbody component!");
            }

            lastJumpTime = Time.time - jumpCooldown; // Allow immediate jump
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (rb != null && Time.time - lastJumpTime >= jumpCooldown)
            {
                Debug.Log("Jumping!");
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                lastJumpTime = Time.time;
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Finished jumping, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Finished jumping");
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class ChaseState : StateDefinition
    {
        [SerializeField] private Transform chaseTarget;
        [SerializeField] private float chaseSpeed = 3.0f;
        [SerializeField] private float stopDistance = 1.0f;
        [SerializeField] private Color chaseColor = Color.red;

        private Vector3 lastKnownPosition;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            // Check if target was passed via data
            if (data is Transform targetTransform)
            {
                chaseTarget = targetTransform;
                Debug.Log($"Starting chase with target from data");
            }
            else if (fromState != null)
            {
                Debug.Log($"Starting chase from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log("Starting chase");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = chaseColor;
            }

            if (chaseTarget != null)
            {
                lastKnownPosition = chaseTarget.position;
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (chaseTarget != null)
            {
                lastKnownPosition = chaseTarget.position;
                Vector3 direction = (lastKnownPosition - transform.position);

                if (direction.magnitude > stopDistance)
                {
                    direction.Normalize();
                    transform.position += direction * chaseSpeed * Time.deltaTime;

                    // Look at target
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
                else
                {
                    Debug.Log("Reached chase target");
                }
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Stopping chase, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Stopping chase");
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class WaitForInputState : StateDefinition
    {
        [SerializeField] private KeyCode activationKey = KeyCode.Space;
        [SerializeField] private Color waitingColor = Color.gray;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Waiting for key press: {activationKey} (from {fromState.GetType().Name})");
            }
            else
            {
                Debug.Log($"Waiting for key press: {activationKey}");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = waitingColor;
            }
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (Input.GetKeyDown(activationKey))
            {
                Debug.Log($"Key {activationKey} pressed - activating!");
                // The state machine will handle the transition
            }

            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Input received, exiting wait state, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Input received, exiting wait state");
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class ScaleState : StateDefinition
    {
        [SerializeField] private Vector3 targetScale = new Vector3(2, 2, 2);
        [SerializeField] private float scaleSpeed = 1.0f;
        [SerializeField] private Color scaleColor = Color.green;

        private Vector3 originalScale;

        public override async UniTask OnEnter(StateDefinition fromState, object data)
        {
            if (fromState != null)
            {
                Debug.Log($"Starting scale animation from {fromState.GetType().Name}");
            }
            else
            {
                Debug.Log("Starting scale animation");
            }
            
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = scaleColor;
            }

            originalScale = transform.localScale;
            await UniTask.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
            await UniTask.CompletedTask;
        }

        public override async UniTask OnExit(StateDefinition toState)
        {
            if (toState != null)
            {
                Debug.Log($"Finished scaling, transitioning to {toState.GetType().Name}");
            }
            else
            {
                Debug.Log("Finished scaling");
            }
            // Optionally reset scale: transform.localScale = originalScale;
            await UniTask.CompletedTask;
        }
    }
}

