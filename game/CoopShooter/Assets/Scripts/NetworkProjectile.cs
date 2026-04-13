using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum ImpactKind : byte { World = 0, Enemy = 1 }

public class NetworkProjectile : NetworkBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 2.5f;

    [Header("Damage")]
    [SerializeField] private int damage;
    [SerializeField] private bool canDamagePlayers = false;

    [Header("Motion")]
    [Tooltip("Units per second (you said 200).")]
    [SerializeField] private float speed = 200f;

    [Header("Hit Detection (Deterministic Sweep)")]
    [Tooltip("Should be >= projectile collider radius (0.05). Use slightly bigger for reliability.")]
    [SerializeField] private float sweepRadius = 0.08f;

    [Tooltip("Layers the projectile can hit (Default + Enemy typically).")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("VFX (NOT NetworkObjects)")]
    [Tooltip("World impact prefab (particles/decal). Should NOT have a NetworkObject.")]
    [SerializeField] private GameObject worldImpactPrefab;

    [Tooltip("Enemy hit sparks prefab. Should NOT have a NetworkObject.")]
    [SerializeField] private GameObject enemyImpactPrefab;

    [Tooltip("Seconds to keep enemy sparks alive when parented to enemy.")]
    [SerializeField] private float enemyFxLifetime = 1.5f;

    [Tooltip("Seconds to keep world impact alive.")]
    [SerializeField] private float worldFxLifetime = 3f;
    
    // Set by Initialize() on server before Spawn()
    private Vector3 dir;
    private ulong shooterClientId;
    private bool hasShooter;
    private float spawnTime;

    private Vector3 lastPos;
    private bool hasLastPos;

    private bool hasHit;

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;

        if (!IsServer)
        {
            enabled = false; // clients do not simulate
            return;
        }

        hasHit = false;
        hasLastPos = false; // first FixedUpdate will initialize lastPos
    }

    /// <summary>
    /// Called on server BEFORE Spawn(). Sets deterministic trajectory.
    /// </summary>
    public void Initialize(Vector3 directionNormalized, float projectileSpeed, ulong? shooterId, int damageAmount)
    {
        dir = directionNormalized.sqrMagnitude > 0.0001f ? directionNormalized.normalized : Vector3.forward;
        speed = projectileSpeed;

        if (shooterId.HasValue)
        {
            hasShooter = true;
            shooterClientId = shooterId.Value;
        }
        else
        {
            hasShooter = false;
            shooterClientId = 0;
        }

        damage = damageAmount;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        if (!NetworkObject || !NetworkObject.IsSpawned) return;
        if (hasHit) return;

        if (Time.time - spawnTime > lifetime)
        {
            SafeDespawn();
            return;
        }

        Vector3 curPos = transform.position;

        if (!hasLastPos)
        {
            lastPos = curPos;
            hasLastPos = true;
            return;
        }

        float stepDist = speed * Time.fixedDeltaTime;
        Vector3 nextPos = curPos + dir * stepDist;

        // Sweep from current -> next to guarantee hit detection (no tunneling)
        Vector3 delta = nextPos - lastPos;
        float dist = delta.magnitude;

        if (dist > 0.0001f)
        {
            Vector3 sweepDir = delta / dist;

            if (TryGetFirstValidHit(lastPos, sweepDir, dist, out RaycastHit hit))
            {
                hasHit = true;      // set first to prevent double-trigger
                HandleImpact(hit);  // VFX + damage (server authoritative)
                SafeDespawn();
                return;
            }
        }

        // No hit: move forward deterministically
        transform.position = nextPos;
        lastPos = nextPos;
    }

    public bool ResolveImmediateImpact(float distance)
    {
        if (!IsServer) return false;
        if (hasHit) return false;
        if (distance <= 0.0001f) return false;

        if (!TryGetFirstValidHit(transform.position, dir, distance, out RaycastHit hit))
            return false;

        hasHit = true;
        HandleImpact(hit);
        SafeDespawn();
        return true;
    }

    private bool TryGetFirstValidHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            sweepRadius,
            direction,
            distance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits.OrderBy(entry => entry.distance))
        {
            if (hit.collider == null)
                continue;

            if (ShouldIgnoreHit(hit.collider))
                continue;

            validHit = hit;
            return true;
        }

        validHit = default;
        return false;
    }

    private bool ShouldIgnoreHit(Collider col)
    {
        if (!hasShooter) return false;

        // Ignore hitting the shooter (by clientId)
        // Works if shooter PlayerObject contains the collider we hit
        var no = col.GetComponentInParent<NetworkObject>();
        if (no == null) return false;

        var nm = NetworkManager.Singleton;
        if (nm == null) return false;

        if (nm.ConnectedClients.TryGetValue(shooterClientId, out var client))
        {
            if (client.PlayerObject != null && no == client.PlayerObject)
                return true;
        }

        return false;
    }

    private void HandleImpact(RaycastHit hit)
    {
        Collider col = hit.collider;

        // Enemy classification: use EnemyAI so player Health doesn't count as "enemy"
        bool hitEnemy = col.GetComponentInParent<EnemyAI>() != null;

        // Optional parenting: only if the hit object has a NetworkObject
        ulong hitNetId = 0;
        var hitNo = col.GetComponentInParent<NetworkObject>();
        if (hitNo != null) hitNetId = hitNo.NetworkObjectId;

        // Tell observing clients to spawn the impact VFX once.
        // The shooter already gets an immediate predicted local impact from WeaponShooter.
        SpawnImpactClientRpc(
            hitEnemy ? ImpactKind.Enemy : ImpactKind.World,
            hit.point,
            hit.normal,
            hitEnemy ? hitNetId : 0,
            BuildImpactClientRpcParams()
        );

        if (ShouldBlockPlayerDamage(col))
            return;

        // Damage (server only)
        var hp = col.GetComponentInParent<Health>();
        if (hp != null && hp.IsAlive)
        {
            if (hasShooter)
            {
                hp.ApplyDamage(damage, shooterClientId);
                Debug.Log($"{shooterClientId}");
            }
            else
                hp.ApplyDamage(damage, 0);

            Debug.Log($"[SERVER] Kinematic projectile hit {hp.name} for {damage}. HP now {hp.CurrentHP.Value}/{hp.MaxHP}");
        }
    }

    private bool ShouldBlockPlayerDamage(Collider col)
    {
        if (canDamagePlayers)
            return false;

        PlayerHealth playerHealth = col.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            return false;

        if (hasShooter && playerHealth.OwnerClientId == shooterClientId)
            return true;

        Debug.Log($"[SERVER] Friendly fire blocked on {playerHealth.name}.");
        return true;
    }

    private ClientRpcParams BuildImpactClientRpcParams()
    {
        if (!hasShooter || NetworkManager.Singleton == null)
            return default;

        var targetClientIds = new List<ulong>();
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId != shooterClientId)
                targetClientIds.Add(clientId);
        }

        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targetClientIds
            }
        };
    }

    [ClientRpc]
    private void SpawnImpactClientRpc(ImpactKind kind, Vector3 pos, Vector3 normal, ulong hitNetId, ClientRpcParams rpcParams = default)
    {
        GameObject prefab = (kind == ImpactKind.Enemy) ? enemyImpactPrefab : worldImpactPrefab;
        if (!prefab) return;

        Quaternion rot = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        if (kind == ImpactKind.Enemy && hitNetId != 0 &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(hitNetId, out var netObj) &&
            netObj != null)
        {
            // Spawn sparks at hit point, parent to enemy so it follows movement slightly
            var fx = Instantiate(prefab, pos, rot, netObj.transform);
            Destroy(fx, Mathf.Max(0.1f, enemyFxLifetime));
        }
        else
        {
            var fx = Instantiate(prefab, pos, rot);
            Destroy(fx, Mathf.Max(0.1f, worldFxLifetime));
        }
    }

    private void SafeDespawn()
    {
        if (!IsServer) return;
        if (NetworkObject == null) return;
        if (!NetworkObject.IsSpawned) return;

        NetworkObject.Despawn(true);
    }
}
