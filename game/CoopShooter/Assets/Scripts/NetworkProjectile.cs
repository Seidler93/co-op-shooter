using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] private float lifetime = 2.5f;

    private Rigidbody rb;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        spawnTime = Time.time;

        // Server authoritative physics:
        // - Server simulates rigidbody
        // - Clients are kinematic and just follow NetworkTransform replication
        if (!IsServer)
        {
            rb.isKinematic = true;
        }
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

        // Ignore collision with shooter (server side)
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

        if (Time.time - spawnTime > lifetime)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // Minimal: despawn on any impact.
        NetworkObject.Despawn(true);
    }
}