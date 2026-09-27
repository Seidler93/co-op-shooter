using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class GrenadeProjectile : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float fuseTime = 1.75f;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private int damage = 80;
    [SerializeField] private LayerMask damageMask = ~0;
    [SerializeField] private bool canDamagePlayers = false;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField] private float explosionVfxLifetime = 3f;

    private Rigidbody rb;
    private ulong shooterClientId;
    private bool hasShooter;
    private Vector3 initialVelocity;
    private bool initialized;
    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 launchVelocity, ulong ownerClientId)
    {
        initialVelocity = launchVelocity;
        shooterClientId = ownerClientId;
        hasShooter = true;
        initialized = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        if (!initialized)
            initialVelocity = transform.forward * 10f;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        IgnoreShooterCollision();
        rb.linearVelocity = initialVelocity;
        StartCoroutine(FuseRoutine());
    }

    private System.Collections.IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Bounce-only first pass. Explosion comes from fuse time.
    }

    private void Explode()
    {
        if (!IsServer || exploded)
            return;

        exploded = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            damageMask,
            QueryTriggerInteraction.Ignore);

        HashSet<Health> damagedHealth = new HashSet<Health>();

        foreach (Collider hit in hits)
        {
            if (ShouldIgnoreHit(hit))
                continue;

            Health health = hit.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive || damagedHealth.Contains(health))
                continue;

            damagedHealth.Add(health);
            health.ApplyDamage(damage, hasShooter ? shooterClientId : 0);
        }

        SpawnExplosionClientRpc(transform.position);

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    private bool ShouldIgnoreHit(Collider col)
    {
        if (col == null)
            return true;

        if (!canDamagePlayers)
        {
            PlayerHealth playerHealth = col.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (hasShooter && playerHealth.OwnerClientId == shooterClientId)
                    return true;
            }
        }

        return false;
    }

    private void IgnoreShooterCollision()
    {
        if (!hasShooter || NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var client))
            return;

        if (client.PlayerObject == null)
            return;

        Collider[] shooterColliders = client.PlayerObject.GetComponentsInChildren<Collider>(true);
        Collider[] grenadeColliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider grenadeCollider in grenadeColliders)
        {
            if (grenadeCollider == null) continue;

            foreach (Collider shooterCollider in shooterColliders)
            {
                if (shooterCollider == null) continue;
                Physics.IgnoreCollision(grenadeCollider, shooterCollider, true);
            }
        }
    }

    [ClientRpc]
    private void SpawnExplosionClientRpc(Vector3 position)
    {
        if (explosionVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(explosionVfxPrefab, position, Quaternion.identity);
        if (explosionVfxLifetime > 0f)
            Destroy(vfx, explosionVfxLifetime);
    }
}
