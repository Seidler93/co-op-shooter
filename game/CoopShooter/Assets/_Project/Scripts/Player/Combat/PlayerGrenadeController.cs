using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerGrenadeController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private WeaponAimController weaponAimController;
    [SerializeField] private Transform throwOrigin;

    [Header("Grenade")]
    [SerializeField] private NetworkObject grenadePrefab;
    [SerializeField] private float throwDelay = 0.35f;
    [SerializeField] private float throwCooldown = 1.25f;
    [SerializeField] private float throwSpeed = 14f;
    [SerializeField] private float upwardBias = 0.18f;
    [SerializeField] private float spawnForwardOffset = 0.35f;

    private Camera ownerCam;
    private float nextThrowTime;
    private Coroutine throwRoutine;

    private void Awake()
    {
        if (!playerAnimator) playerAnimator = GetComponent<PlayerAnimator>();
        if (!playerState) playerState = GetComponent<PlayerState>();
        if (!weaponAimController) weaponAimController = GetComponentInChildren<WeaponAimController>();
        if (!throwOrigin) throwOrigin = transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            ownerCam = Camera.main;
    }

    public bool TryThrowGrenade()
    {
        if (!IsOwner)
            return false;

        if (grenadePrefab == null || throwOrigin == null)
            return false;

        if (Time.time < nextThrowTime)
            return false;

        if (playerState != null && !playerState.CanUseWeapons)
            return false;

        nextThrowTime = Time.time + throwCooldown;

        playerAnimator?.TriggerGrenade();

        if (throwRoutine != null)
            StopCoroutine(throwRoutine);

        throwRoutine = StartCoroutine(ThrowGrenadeRoutine());
        return true;
    }

    private IEnumerator ThrowGrenadeRoutine()
    {
        yield return new WaitForSeconds(throwDelay);

        if (ownerCam == null)
            ownerCam = Camera.main;

        Vector3 spawnPosition = throwOrigin.position + throwOrigin.forward * spawnForwardOffset;
        Vector3 aimPoint = weaponAimController != null
            ? weaponAimController.ResolveAimPoint(ownerCam, out _, out _)
            : spawnPosition + (ownerCam != null ? ownerCam.transform.forward : transform.forward) * 20f;

        Vector3 flatDirection = (aimPoint - spawnPosition).normalized;
        if (flatDirection.sqrMagnitude < 0.0001f)
            flatDirection = transform.forward;

        Vector3 launchVelocity = (flatDirection + Vector3.up * upwardBias).normalized * throwSpeed;
        RequestSpawnGrenadeServerRpc(spawnPosition, launchVelocity);
        throwRoutine = null;
    }

    [ServerRpc]
    private void RequestSpawnGrenadeServerRpc(Vector3 spawnPosition, Vector3 launchVelocity)
    {
        if (grenadePrefab == null)
            return;

        NetworkObject grenadeObject = Instantiate(grenadePrefab, spawnPosition, Quaternion.identity);
        GrenadeProjectile grenadeProjectile = grenadeObject.GetComponent<GrenadeProjectile>();
        if (grenadeProjectile != null)
            grenadeProjectile.Initialize(launchVelocity, OwnerClientId);

        grenadeObject.Spawn(true);
    }
}
