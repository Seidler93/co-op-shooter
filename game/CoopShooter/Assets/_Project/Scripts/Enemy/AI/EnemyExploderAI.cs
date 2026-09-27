using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyExploderAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool useNavMeshAgent = true;
    [SerializeField] private float chaseMoveSpeed = 4.2f;
    [SerializeField] private float fallbackMoveSpeed = 4.2f;
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float rotationSharpness = 12f;

    [Header("Explosion Attack")]
    [SerializeField] private float explodeTriggerRange = 2.2f;
    [SerializeField] private float explodeTellDuration = 0.7f;
    [SerializeField] private float explodeRadius = 3.2f;
    [SerializeField] private int explodeDamage = 38;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualResetSpeed = 10f;
    [SerializeField] private Vector3 swellScaleMultiplier = new Vector3(1.3f, 1.15f, 1.3f);

    private NavMeshAgent agent;
    private Health myHealth;
    private Health targetHealth;
    private PlayerHealth targetPlayerHealth;
    private Transform targetTransform;
    private float nextRetargetTime;
    private Coroutine explodeRoutine;
    private Vector3 visualBaseLocalScale;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0);

        if (visualRoot != null)
            visualBaseLocalScale = visualRoot.localScale;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            if (agent != null)
                agent.enabled = false;

            enabled = false;
            return;
        }

        if (useNavMeshAgent && agent != null)
        {
            agent.enabled = true;
            agent.speed = chaseMoveSpeed;
            agent.acceleration = Mathf.Max(agent.acceleration, 12f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 240f);
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (!IsServer || myHealth == null || !myHealth.IsAlive)
            return;

        if (Time.time >= nextRetargetTime)
        {
            nextRetargetTime = Time.time + retargetInterval;
            AcquireClosestLivingPlayer();
        }

        if (!IsTargetValid(targetHealth, targetPlayerHealth) || targetTransform == null)
        {
            AcquireClosestLivingPlayer();

            if (!IsTargetValid(targetHealth, targetPlayerHealth) || targetTransform == null)
            {
                StopMovement();
                RestoreVisuals(Time.deltaTime);
                return;
            }
        }

        if (explodeRoutine != null)
        {
            RotateToward(targetTransform.position, Time.deltaTime);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= explodeTriggerRange)
        {
            StopMovement();
            explodeRoutine = StartCoroutine(ExplodeRoutine());
            return;
        }

        ChaseTarget();
        RestoreVisuals(Time.deltaTime);
    }

    private void AcquireClosestLivingPlayer()
    {
        Health closest = null;
        PlayerHealth closestPlayer = null;
        float bestSqr = float.MaxValue;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            Health hp = playerObject.GetComponentInChildren<Health>(true);
            PlayerHealth playerHp = playerObject.GetComponent<PlayerHealth>();
            if (!IsTargetValid(hp, playerHp))
                continue;

            float sqr = (hp.transform.position - transform.position).sqrMagnitude;
            if (sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            closest = hp;
            closestPlayer = playerHp;
        }

        targetHealth = closest;
        targetPlayerHealth = closestPlayer;
        targetTransform = closest != null ? closest.transform : null;
    }

    private void ChaseTarget()
    {
        if (targetTransform == null)
            return;

        if (useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseMoveSpeed;
            agent.SetDestination(targetTransform.position);
            return;
        }

        Vector3 direction = targetTransform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();
        transform.position += direction * (fallbackMoveSpeed * Time.deltaTime);
        RotateToward(transform.position + direction, Time.deltaTime);
    }

    private IEnumerator ExplodeRoutine()
    {
        if (visualRoot != null)
        {
            Vector3 startScale = visualRoot.localScale;
            Vector3 targetScale = Vector3.Scale(visualBaseLocalScale, swellScaleMultiplier);
            float elapsed = 0f;

            while (elapsed < explodeTellDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, explodeTellDuration));
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                visualRoot.localScale = Vector3.Lerp(startScale, targetScale, eased);

                if (targetTransform != null)
                    RotateToward(targetTransform.position, Time.deltaTime);

                yield return null;
            }

            visualRoot.localScale = targetScale;
        }
        else
        {
            yield return new WaitForSeconds(explodeTellDuration);
        }

        DamagePlayersInRadius();
        myHealth.ForceDeath();
        explodeRoutine = null;
    }

    private void DamagePlayersInRadius()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        float sqrRadius = explodeRadius * explodeRadius;

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            PlayerHealth player = playerObject.GetComponent<PlayerHealth>();
            if (player == null || !player.IsAlive || player.IsDowned)
                continue;

            Vector3 flatOffset = player.transform.position - transform.position;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude > sqrRadius)
                continue;

            player.Health.ApplyDamage(explodeDamage, 0);
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

    private void RestoreVisuals(float dt)
    {
        if (visualRoot == null)
            return;

        float sharpness = 1f - Mathf.Exp(-visualResetSpeed * dt);
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, visualBaseLocalScale, sharpness);
    }

    private void RotateToward(Vector3 worldPosition, float dt)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSharpness * dt);
    }

    private bool IsTargetValid(Health hp, PlayerHealth playerHp)
    {
        if (hp == null || !hp.IsAlive)
            return false;

        if (playerHp != null && (playerHp.IsDowned || !playerHp.IsAlive))
            return false;

        return true;
    }
}
