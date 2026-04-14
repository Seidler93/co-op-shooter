using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyBossAI : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool useNavMeshAgent = true;
    [SerializeField] private float chaseMoveSpeed = 4.8f;
    [SerializeField] private float fallbackMoveSpeed = 4.8f;
    [SerializeField] private float retargetInterval = 0.25f;
    [SerializeField] private float rotationSharpness = 12f;

    [Header("Dash Attack")]
    [SerializeField] private float dashTriggerMinRange = 4.5f;
    [SerializeField] private float dashTriggerMaxRange = 10f;
    [SerializeField] private float dashDistance = 4.6f;
    [SerializeField] private float dashTellDuration = 0.55f;
    [SerializeField] private float dashTravelDuration = 0.22f;
    [SerializeField] private float dashRecoveryDuration = 1.85f;
    [SerializeField] private float dashCooldown = 5.2f;
    [SerializeField] private float dashHitRadius = 1.1f;
    [SerializeField] private int dashDamage = 32;

    [Header("Smash Attack")]
    [SerializeField] private float smashTriggerRange = 2.7f;
    [SerializeField] private float smashTellDuration = 0.6f;
    [SerializeField] private float smashStrikeDuration = 0.14f;
    [SerializeField] private float smashRecoveryDuration = 2.1f;
    [SerializeField] private float smashCooldown = 4.2f;
    [SerializeField] private float smashRadius = 2.4f;
    [SerializeField] private float smashForwardOffset = 1.2f;
    [SerializeField] private float smashArc = 150f;
    [SerializeField] private int smashDamage = 40;

    [Header("Visual Tell")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualResetSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private NavMeshAgent agent;
    private Health myHealth;

    private Health targetHealth;
    private PlayerHealth targetPlayerHealth;
    private Transform targetTransform;

    private float nextRetargetTime;
    private float nextDashReadyTime;
    private float nextSmashReadyTime;

    private Coroutine attackRoutine;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0);

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
            agent.acceleration = Mathf.Max(agent.acceleration, 12f);
            agent.angularSpeed = Mathf.Max(agent.angularSpeed, 240f);
            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (myHealth == null || !myHealth.IsAlive)
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

        if (CanUseSmash(distance))
        {
            BeginAttack(SmashAttackRoutine());
            return;
        }

        if (CanUseDash(distance))
        {
            BeginAttack(DashAttackRoutine());
            return;
        }

        ChaseTarget();
        RestoreVisualPose(Time.deltaTime);
    }

    private void AcquireClosestLivingPlayer()
    {
        Health closest = null;
        PlayerHealth closestPlayerHealth = null;
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
            closestPlayerHealth = playerHp;
        }

        if (closest != targetHealth && logStateChanges)
        {
            string nextName = closest != null ? closest.name : "NONE";
            Debug.Log($"[EnemyBossAI] Target changed -> {nextName}");
        }

        targetHealth = closest;
        targetPlayerHealth = closestPlayerHealth;
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

    private void StopMovement()
    {
        if (useNavMeshAgent && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool CanUseDash(float distance)
    {
        return Time.time >= nextDashReadyTime &&
               distance >= dashTriggerMinRange &&
               distance <= dashTriggerMaxRange;
    }

    private bool CanUseSmash(float distance)
    {
        return Time.time >= nextSmashReadyTime &&
               distance <= smashTriggerRange;
    }

    private void BeginAttack(IEnumerator routine)
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        StopMovement();
        attackRoutine = StartCoroutine(routine);
    }

    private IEnumerator DashAttackRoutine()
    {
        nextDashReadyTime = Time.time + dashCooldown;

        yield return AnimateVisualPose(
            visualBaseLocalPosition + new Vector3(0f, 0f, -0.45f),
            visualBaseLocalRotation * Quaternion.Euler(-30f, 0f, 0f),
            dashTellDuration,
            true
        );

        HashSet<PlayerHealth> hitPlayers = new HashSet<PlayerHealth>();
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * dashDistance;
        float elapsed = 0f;

        while (elapsed < dashTravelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashTravelDuration);
            Vector3 nextPosition = Vector3.Lerp(start, end, t);
            MoveBoss(nextPosition);
            DamagePlayersInRadius(transform.position, dashHitRadius, dashDamage, hitPlayers);
            yield return null;
        }

        MoveBoss(end);
        DamagePlayersInRadius(transform.position, dashHitRadius, dashDamage, hitPlayers);

        yield return AnimateVisualPose(
            visualBaseLocalPosition + new Vector3(0f, 0f, 0.14f),
            visualBaseLocalRotation * Quaternion.Euler(16f, 0f, 0f),
            0.08f,
            false
        );

        yield return AnimateVisualPose(
            visualBaseLocalPosition,
            visualBaseLocalRotation,
            dashRecoveryDuration,
            false
        );

        attackRoutine = null;
    }

    private IEnumerator SmashAttackRoutine()
    {
        nextSmashReadyTime = Time.time + smashCooldown;

        yield return AnimateVisualPose(
            visualBaseLocalPosition + new Vector3(0f, 0f, -0.25f),
            visualBaseLocalRotation * Quaternion.Euler(-36f, 0f, 0f),
            smashTellDuration,
            true
        );

        yield return AnimateVisualPose(
            visualBaseLocalPosition + new Vector3(0f, 0f, 0.16f),
            visualBaseLocalRotation * Quaternion.Euler(46f, 0f, 0f),
            smashStrikeDuration,
            true
        );

        DamagePlayersInSmash();

        yield return AnimateVisualPose(
            visualBaseLocalPosition,
            visualBaseLocalRotation,
            smashRecoveryDuration,
            false
        );

        attackRoutine = null;
    }

    private IEnumerator AnimateVisualPose(Vector3 targetPosition, Quaternion targetRotation, float duration, bool keepFacingTarget)
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

            if (keepFacingTarget && targetTransform != null)
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

    private void MoveBoss(Vector3 nextPosition)
    {
        if (useNavMeshAgent && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(nextPosition);
            return;
        }

        transform.position = nextPosition;
    }

    private void DamagePlayersInRadius(Vector3 center, float radius, int damage, HashSet<PlayerHealth> hitPlayers)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            PlayerHealth player = playerObject.GetComponent<PlayerHealth>();
            if (player == null || !player.IsAlive || player.IsDowned)
                continue;

            if (hitPlayers != null && hitPlayers.Contains(player))
                continue;

            Vector3 closestPoint = player.transform.position;
            closestPoint.y = center.y;
            if ((closestPoint - center).sqrMagnitude > radius * radius)
                continue;

            player.Health.ApplyDamage(damage, 0);
            hitPlayers?.Add(player);
        }
    }

    private void DamagePlayersInSmash()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        Vector3 hitCenter = transform.position + transform.forward * smashForwardOffset;
        float cosHalfArc = Mathf.Cos(smashArc * 0.5f * Mathf.Deg2Rad);

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            PlayerHealth player = playerObject.GetComponent<PlayerHealth>();
            if (player == null || !player.IsAlive || player.IsDowned)
                continue;

            Vector3 toPlayer = player.transform.position - hitCenter;
            Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flatDirection.sqrMagnitude > smashRadius * smashRadius)
                continue;

            Vector3 dirNormalized = flatDirection.normalized;
            if (Vector3.Dot(transform.forward, dirNormalized) < cosHalfArc)
                continue;

            player.Health.ApplyDamage(smashDamage, 0);
        }
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
