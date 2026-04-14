using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemySpitterAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool useNavMeshAgent = true;
    [SerializeField] private float chaseMoveSpeed = 4.2f;
    [SerializeField] private float fallbackMoveSpeed = 4.2f;
    [SerializeField] private float retreatMoveSpeed = 3.6f;
    [SerializeField] private float preferredRange = 8.5f;
    [SerializeField] private float retreatRange = 4.5f;
    [SerializeField] private float retargetInterval = 0.3f;
    [SerializeField] private float rotationSharpness = 10f;

    [Header("Spit Attack")]
    [SerializeField] private NetworkObject spitProjectilePrefab;
    [SerializeField] private Transform spitOrigin;
    [SerializeField] private float attackRange = 14f;
    [SerializeField] private float attackCooldown = 2.6f;
    [SerializeField] private float attackTellDuration = 0.55f;
    [SerializeField] private float spitProjectileSpeed = 20f;
    [SerializeField] private int spitDamage = 14;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualResetSpeed = 10f;

    private NavMeshAgent agent;
    private Health myHealth;
    private Health targetHealth;
    private PlayerHealth targetPlayerHealth;
    private Transform targetTransform;
    private float nextRetargetTime;
    private float nextAttackReadyTime;
    private Coroutine attackRoutine;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0);

        if (spitOrigin == null)
            spitOrigin = visualRoot != null ? visualRoot : transform;

        if (visualRoot != null)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseLocalRotation = visualRoot.localRotation;
        }
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
            agent.acceleration = Mathf.Max(agent.acceleration, 10f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 220f);
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
                RestoreVisualPose(Time.deltaTime);
                return;
            }
        }

        if (attackRoutine != null)
        {
            RotateToward(targetTransform.position, Time.deltaTime);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= retreatRange)
        {
            RetreatFromTarget();
            RestoreVisualPose(Time.deltaTime);
            return;
        }

        if (distance <= attackRange && Time.time >= nextAttackReadyTime)
        {
            BeginAttack();
            return;
        }

        if (distance > preferredRange)
        {
            ChaseTarget();
        }
        else
        {
            StopMovement();
            RotateToward(targetTransform.position, Time.deltaTime);
        }

        RestoreVisualPose(Time.deltaTime);
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

        MoveFallback((targetTransform.position - transform.position).normalized, fallbackMoveSpeed);
    }

    private void RetreatFromTarget()
    {
        if (targetTransform == null)
            return;

        Vector3 away = transform.position - targetTransform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = -transform.forward;

        away.Normalize();

        if (useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = retreatMoveSpeed;
            agent.SetDestination(transform.position + away * 3f);
            RotateToward(targetTransform.position, Time.deltaTime);
            return;
        }

        MoveFallback(away, retreatMoveSpeed);
        RotateToward(targetTransform.position, Time.deltaTime);
    }

    private void MoveFallback(Vector3 direction, float speed)
    {
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        if (flat.sqrMagnitude < 0.001f)
            return;

        flat.Normalize();
        transform.position += flat * (speed * Time.deltaTime);
        RotateToward(transform.position + flat, Time.deltaTime);
    }

    private void StopMovement()
    {
        if (useNavMeshAgent && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void BeginAttack()
    {
        nextAttackReadyTime = Time.time + attackCooldown;
        StopMovement();
        attackRoutine = StartCoroutine(SpitRoutine());
    }

    private IEnumerator SpitRoutine()
    {
        yield return AnimateVisualPose(
            visualBaseLocalPosition + new Vector3(0f, 0f, -0.28f),
            visualBaseLocalRotation * Quaternion.Euler(-22f, 0f, 0f),
            attackTellDuration
        );

        FireSpitProjectile();

        yield return AnimateVisualPose(
            visualBaseLocalPosition,
            visualBaseLocalRotation,
            0.22f
        );

        attackRoutine = null;
    }

    private void FireSpitProjectile()
    {
        if (spitProjectilePrefab == null || targetTransform == null)
            return;

        Vector3 origin = spitOrigin != null ? spitOrigin.position : transform.position + Vector3.up * 1.4f;
        Vector3 targetPoint = targetTransform.position + Vector3.up * 1.0f;
        Vector3 direction = (targetPoint - origin).normalized;
        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        NetworkObject projectile = Instantiate(spitProjectilePrefab, origin, rotation);

        NetworkProjectile networkProjectile = projectile.GetComponent<NetworkProjectile>();
        if (networkProjectile != null)
            networkProjectile.Initialize(direction, spitProjectileSpeed, null, spitDamage);

        projectile.Spawn(true);
    }

    private IEnumerator AnimateVisualPose(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (visualRoot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Vector3 startPosition = visualRoot.localPosition;
        Quaternion startRotation = visualRoot.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            visualRoot.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
            visualRoot.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);

            if (targetTransform != null)
                RotateToward(targetTransform.position, Time.deltaTime);

            yield return null;
        }

        visualRoot.localPosition = targetPosition;
        visualRoot.localRotation = targetRotation;
    }

    private void RestoreVisualPose(float dt)
    {
        if (visualRoot == null)
            return;

        float sharpness = 1f - Mathf.Exp(-visualResetSpeed * dt);
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPosition, sharpness);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualBaseLocalRotation, sharpness);
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
