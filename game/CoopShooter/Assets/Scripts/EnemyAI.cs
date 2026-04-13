using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool useNavMeshAgent = true;
    [SerializeField] private float fallbackMoveSpeed = 3.5f;
    [SerializeField] private float retargetInterval = 0.35f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private int attackDamage = 10;

    [Header("Debug")]
    [SerializeField] private bool logTargetChanges = false;

    private NavMeshAgent agent;
    private Health myHealth;

    private Health targetHealth;
    private Transform targetTransform;

    private float nextRetargetTime;
    private float nextAttackTime;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        // Clients should never simulate enemy AI.
        if (!IsServer)
        {
            if (agent != null) agent.enabled = false;
            enabled = false;
            return;
        }

        // Server-only: enable navigation if desired.
        if (useNavMeshAgent && agent != null)
        {
            agent.enabled = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!myHealth || !myHealth.IsAlive) return;

        // Periodically reacquire target (handles joins, deaths, distance changes).
        if (Time.time >= nextRetargetTime)
        {
            nextRetargetTime = Time.time + retargetInterval;
            AcquireClosestLivingPlayer();
        }

        if (targetHealth == null || !targetHealth.IsAlive || targetTransform == null)
        {
            StopMovement();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetTransform.position);

        if (dist > attackRange)
        {
            ChaseTarget();
        }
        else
        {
            StopMovement();
            TryAttack();
        }
    }

    private void AcquireClosestLivingPlayer()
    {
        Health closest = null;
        float bestSqr = float.MaxValue;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Scan connected players. Uses their Health component to determine alive.
        foreach (var client in nm.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var hp = playerObj.GetComponentInChildren<Health>(true);
            if (hp == null || !hp.IsAlive) continue;

            float sqr = (hp.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = hp;
            }
        }

        if (closest != targetHealth)
        {
            targetHealth = closest;
            targetTransform = closest != null ? closest.transform : null;

            if (logTargetChanges)
            {
                string name = targetTransform ? targetTransform.name : "NONE";
                Debug.Log($"[EnemyAI] Target changed -> {name}");
            }
        }
    }

    private void ChaseTarget()
    {
        if (useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetTransform.position);
        }
        else
        {
            // Fallback movement (no NavMesh / agent not active)
            Vector3 dir = (targetTransform.position - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f) return;

            dir.Normalize();
            transform.position += dir * (fallbackMoveSpeed * Time.deltaTime);

            // rotate toward target
            var look = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 12f * Time.deltaTime);
        }
    }

    private void StopMovement()
    {
        if (useNavMeshAgent && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackInterval;

        if (targetHealth != null && targetHealth.IsAlive)
        {
            targetHealth.ApplyDamage(attackDamage, 0);
        }
    }
}
