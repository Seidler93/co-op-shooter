using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 2.5f;

    [Header("Damage")]
    [SerializeField] private int damage = 25;

    [Header("Motion")]
    [Tooltip("Units per second (you said 200).")]
    [SerializeField] private float speed = 200f;

    [Header("Hit Detection (Deterministic Sweep)")]
    [Tooltip("Should be >= projectile collider radius (0.05). Use slightly bigger for reliability.")]
    [SerializeField] private float sweepRadius = 0.08f;

    [Tooltip("Layers the projectile can hit (Default + Enemy typically).")]
    [SerializeField] private LayerMask hitMask = ~0;

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
    public void Initialize(Vector3 directionNormalized, float projectileSpeed, ulong? shooterId)
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

            if (Physics.SphereCast(lastPos, sweepRadius, sweepDir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (ShouldIgnoreHit(hit.collider))
                {
                    // If we ignore (ex: shooter), keep moving this step
                    transform.position = nextPos;
                    lastPos = nextPos;
                    return;
                }

                ApplyHit(hit.collider);
                hasHit = true;
                SafeDespawn();
                return;
            }
        }

        // No hit: move forward deterministically
        transform.position = nextPos;
        lastPos = nextPos;
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

    private void ApplyHit(Collider col)
    {
        var hp = col.GetComponentInParent<Health>();
        if (hp != null && hp.IsAlive)
        {
            hp.ApplyDamage(damage);
            Debug.Log($"[SERVER] Kinematic projectile hit {hp.name} for {damage}. HP now {hp.CurrentHP.Value}/{hp.MaxHP}");
        }
        else
        {
            Debug.Log($"[SERVER] Kinematic projectile hit {col.name} (no Health)");
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