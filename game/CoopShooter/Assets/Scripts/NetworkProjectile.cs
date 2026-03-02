using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkProjectile : NetworkBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 2.5f;

    [Header("Damage")]
    [SerializeField] private int damage = 25;

    [Header("Deterministic Hit (Server Sweep)")]
    [Tooltip("Should be >= projectile collider radius. Use slightly larger for reliability.")]
    [SerializeField] private float sweepRadius = 0.08f;

    [Tooltip("Layers the projectile can hit. Exclude 'Projectile' and usually 'Player' if no friendly fire.")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Spawn Safety")]
    [SerializeField] private float armDelaySeconds = 0.02f;

    private Rigidbody rb;
    private float spawnTime;

    private bool armed;
    private bool hasLastPos;
    private Vector3 lastPos;

    private bool hasHit; // deterministic: only process one hit

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;

        if (!IsServer)
        {
            rb.isKinematic = true;
            return;
        }

        // Server authoritative motion (Rigidbody), deterministic hit detection (sweep)
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // fine since we don't rely on collisions
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // IMPORTANT: We do NOT want physics collisions/bounce to affect behavior
        // We'll either disable the collider or set it to trigger.
        DisablePhysicsCollisions();

        armed = false;
        hasHit = false;
        hasLastPos = false;

        StartCoroutine(ArmAfterDelay());
    }

    private IEnumerator ArmAfterDelay()
    {
        if (armDelaySeconds > 0f)
            yield return new WaitForSeconds(armDelaySeconds);
        else
            yield return null;

        armed = true;

        // Initialize sweep baseline once armed
        lastPos = rb.position;
        hasLastPos = true;
    }

    /// <summary>
    /// Called on server BEFORE Spawn().
    /// </summary>
    public void Initialize(Vector3 initialVelocity, ulong? shooterClientId)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.linearVelocity = initialVelocity;
        rb.angularVelocity = Vector3.zero;

        // Ignore collision with shooter (still useful if you keep any colliders enabled)
        if (!shooterClientId.HasValue) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId.Value, out var client))
            return;

        var shooterObj = client.PlayerObject;
        if (shooterObj == null) return;

        var shooterColliders = shooterObj.GetComponentsInChildren<Collider>(true);
        var projColliders = GetComponentsInChildren<Collider>(true);

        foreach (var pc in projColliders)
        {
            if (!pc) continue;
            foreach (var sc in shooterColliders)
            {
                if (!sc) continue;
                Physics.IgnoreCollision(pc, sc, true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (!NetworkObject.IsSpawned) return;

        if (Time.time - spawnTime > lifetime)
        {
            SafeDespawn();
            return;
        }

        if (!armed || hasHit) return;

        Vector3 curPos = rb.position;

        if (!hasLastPos)
        {
            lastPos = curPos;
            hasLastPos = true;
            return;
        }

        Vector3 delta = curPos - lastPos;
        float dist = delta.magnitude;

        if (dist > 0.0001f)
        {
            Vector3 dir = delta / dist;

            // Deterministic sweep along the travelled path this fixed step
            if (Physics.SphereCast(lastPos, sweepRadius, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
            {
                // Optional: avoid hitting other projectiles or triggers, etc.
                // If you need, you can filter by tag/layer here.

                ApplyHit(hit.collider);
                hasHit = true;
                SafeDespawn();
                return;
            }
        }

        lastPos = curPos;
    }

    private void ApplyHit(Collider col)
    {
        // Apply damage server-side
        var hp = col.GetComponentInParent<Health>();
        if (hp != null && hp.IsAlive)
        {
            hp.ApplyDamage(damage);
            Debug.Log($"[SERVER] Projectile hit {hp.name} for {damage}. HP now {hp.CurrentHP.Value}/{hp.MaxHP}");
        }
        else
        {
            // Useful to confirm we hit world geometry too
            Debug.Log($"[SERVER] Projectile hit {col.name} (no Health)");
        }
    }

    private void DisablePhysicsCollisions()
    {
        // We keep colliders for visuals / queries, but stop physics bounce/collision response.
        // Option A: set all colliders to triggers so they don't bounce.
        // Option B: disable colliders entirely.
        //
        // We'll do Option A by default (safer for some setups).
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (!c) continue;
            c.isTrigger = true;
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