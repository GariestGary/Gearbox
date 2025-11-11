using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Examples
{
    [System.Serializable]
    public class IdleState : StateDefinition
    {
        [SerializeField] private float idleTime = 2.0f;
        [SerializeField] private Color idleColor = Color.blue;

        public override async UniTask OnEnter()
        {
            Debug.Log($"Entering Idle state for {idleTime} seconds");
            GetComponent<Renderer>().material.color = idleColor;
            await Task.Delay((int)(idleTime * 1000));
        }

        public override async UniTask OnUpdate()
        {
            // Idle state just waits
            await Task.CompletedTask;
        }

        public override async UniTask OnExit()
        {
            Debug.Log("Exiting Idle state");
            await Task.CompletedTask;
        }
    }

    [System.Serializable]
    public class MoveState : StateDefinition
    {
        [SerializeField] private Vector3 targetPosition = new Vector3(5, 0, 5);
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private Color moveColor = Color.green;

        private Vector3 startPosition;
        private float journeyLength;
        private float startTime;

        public override async UniTask OnEnter()
        {
            Debug.Log($"Moving to position {targetPosition}");
            GetComponent<Renderer>().material.color = moveColor;

            startPosition = transform.position;
            journeyLength = Vector3.Distance(startPosition, targetPosition);
            startTime = Time.time;

            await Task.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            float distCovered = (Time.time - startTime) * moveSpeed;
            float fractionOfJourney = distCovered / journeyLength;
            transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

            await Task.CompletedTask;
        }

        public override async UniTask OnExit()
        {
            Debug.Log("Finished moving");
            await Task.CompletedTask;
        }
    }

    [System.Serializable]
    public class AttackState : StateDefinition
    {
        [SerializeField] private float attackDamage = 10.0f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private Color attackColor = Color.red;

        private float lastAttackTime;

        public override async UniTask OnEnter()
        {
            Debug.Log($"Starting attack with {attackDamage} damage");
            GetComponent<Renderer>().material.color = attackColor;
            lastAttackTime = Time.time - attackCooldown; // Allow immediate attack
            await Task.CompletedTask;
        }

        public override async UniTask OnUpdate()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Debug.Log($"Attacking for {attackDamage} damage!");
                lastAttackTime = Time.time;
                // Attack logic would go here
            }

            await Task.CompletedTask;
        }

        public override async UniTask OnExit()
        {
            Debug.Log("Stopping attack");
            await Task.CompletedTask;
        }
    }

    [System.Serializable]
    public class PatrolState : StateDefinition
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointThreshold = 0.1f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private Color patrolColor = Color.yellow;

        private int currentWaypointIndex = 0;

        public override async UniTask OnEnter()
        {
            Debug.Log("Starting patrol");
            GetComponent<Renderer>().material.color = patrolColor;
            if (waypoints.Length == 0)
            {
                Debug.LogWarning("No waypoints assigned to patrol state!");
            }
            await Task.CompletedTask;
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

            await Task.CompletedTask;
        }

        public override async UniTask OnExit()
        {
            Debug.Log("Stopping patrol");
            await Task.CompletedTask;
        }
    }

    [System.Serializable]
    public class FleeState : StateDefinition
    {
        [SerializeField] private Transform fleeTarget;
        [SerializeField] private float fleeDistance = 10.0f;
        [SerializeField] private float fleeSpeed = 3.0f;
        [SerializeField] private Color fleeColor = Color.magenta;

        public override async UniTask OnEnter()
        {
            Debug.Log("Starting to flee!");
            GetComponent<Renderer>().material.color = fleeColor;
            await Task.CompletedTask;
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

            await Task.CompletedTask;
        }

        public override async UniTask OnExit()
        {
            Debug.Log("Stopped fleeing");
            await Task.CompletedTask;
        }
    }
}