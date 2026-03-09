using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponShooter : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform visualMuzzle;
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private WeaponMuzzleFlash muzzleFlash;

    [Header("Core Components")]
    [SerializeField] private WeaponBloom weaponBloom;
    [SerializeField] private WeaponRecoil weaponRecoil;
    [SerializeField] private WeaponCrosshairDriver crosshairDriver;
    [SerializeField] private WeaponAudio weaponAudio;
    [SerializeField] private WeaponTracer weaponTracer;
    [SerializeField] private WeaponAmmoNetcode weaponAmmo;
    [SerializeField] private PlayerState playerState;

    [Header("Tuning")]
    [SerializeField] private float projectileSpeed = 35f;
    [SerializeField] private float maxAimDistance = 250f;
    [SerializeField] private float fireCooldown = 0.12f;

    [Header("Fire Mode")]
    [SerializeField] private bool fullAuto = true;

    [Header("Aim Raycast")]
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Collision")]
    [SerializeField] private bool ignoreShooterCollision = true;

    [Header("Networking (optional)")]
    [SerializeField] private NetworkWeaponAim netAim;

    [Header("Local Predicted World Impact (Owner Only)")]
    [Tooltip("Use a VISUAL-ONLY prefab here (no audio), so world impacts don't sound doubled.")]
    [SerializeField] private GameObject localWorldImpactPrefab;

    [SerializeField] private float localWorldImpactLifetime = 2f;

    private float nextFireTime;
    private Camera ownerCam;
    private bool fireHeld;
    private bool firePressedThisFrame;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (!weaponBloom) weaponBloom = GetComponent<WeaponBloom>();
        if (!weaponRecoil) weaponRecoil = GetComponent<WeaponRecoil>();
        if (!crosshairDriver) crosshairDriver = GetComponent<WeaponCrosshairDriver>();
        if (!weaponAudio) weaponAudio = GetComponent<WeaponAudio>();
        if (!weaponTracer) weaponTracer = GetComponent<WeaponTracer>();
        if (!weaponAmmo) weaponAmmo = GetComponent<WeaponAmmoNetcode>();
        if (!playerState) playerState = GetComponentInParent<PlayerState>();
        if (!netAim) netAim = GetComponentInParent<NetworkWeaponAim>();

        ownerCam = Camera.main;

        if (!visualMuzzle && muzzle)
            visualMuzzle = muzzle;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (muzzle == null || projectilePrefab == null) return;

        if (playerState != null && (playerState.IsDead || playerState.IsInputBlocked))
        {
            playerState.SetFiring(false);
            return;
        }

        bool wantsFire = fullAuto ? fireHeld : firePressedThisFrame;
        bool isReloading = playerState != null && playerState.IsReloading;
        bool canCurrentlyFire = !isReloading;

        playerState?.SetFiring(canCurrentlyFire && fireHeld);

        weaponBloom?.TickRecovery(Time.deltaTime, fireHeld);
        crosshairDriver?.TickCrosshair();

        if (!wantsFire) return;
        if (isReloading) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireCooldown;

        if (ownerCam == null)
            ownerCam = Camera.main;
        if (ownerCam == null)
            return;

        if (weaponAmmo != null && !weaponAmmo.HasAmmoInMag)
        {
            weaponAmmo.RequestReloadIfEmptyServerRpc();
            return;
        }

        FireShot();
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        if (playerState != null && (playerState.IsDead || playerState.IsInputBlocked))
            return;

        weaponRecoil?.TickRecoil(Time.deltaTime, fireHeld);
    }

    public void SetFireInput(bool held, bool pressedThisFrame)
    {
        if (playerState != null && (playerState.IsDead || playerState.IsInputBlocked))
        {
            fireHeld = false;
            firePressedThisFrame = false;
            return;
        }

        fireHeld = held;
        firePressedThisFrame = pressedThisFrame;
    }

    private void FireShot()
    {
        Ray camRay = ownerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint;
        if (Physics.Raycast(camRay, out RaycastHit camHit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = camHit.point;
        else
            aimPoint = camRay.origin + camRay.direction * maxAimDistance;

        Vector3 dir = aimPoint - muzzle.position;
        if (dir.sqrMagnitude < 0.0001f)
            dir = muzzle.forward;
        dir.Normalize();

        weaponBloom?.AddBloomOnShot();

        float bloomDeg = weaponBloom != null ? weaponBloom.GetCurrentBloomDeg() : 0f;
        if (weaponBloom != null)
            dir = weaponBloom.ApplyBloomCameraRelative(dir, bloomDeg, ownerCam);

        RaycastHit predictedHit;
        bool hasPredictedHit = Physics.Raycast(
            muzzle.position,
            dir,
            out predictedHit,
            maxAimDistance,
            aimMask,
            QueryTriggerInteraction.Ignore
        );

        if (weaponTracer != null)
        {
            Vector3 tracerStart = visualMuzzle != null ? visualMuzzle.position : muzzle.position;
            Vector3 muzzleLineEnd = hasPredictedHit
                ? predictedHit.point
                : (muzzle.position + dir * maxAimDistance);

            float dist = Vector3.Distance(muzzle.position, muzzleLineEnd);
            Vector3 tracerEnd = tracerStart + dir * dist;

            weaponTracer.SpawnTracer(tracerStart, tracerEnd);
        }

        if (hasPredictedHit)
        {
            bool hitEnemy = predictedHit.collider.GetComponentInParent<EnemyAI>() != null;
            if (!hitEnemy)
            {
                SpawnLocalPredictedWorldImpact(predictedHit.point, predictedHit.normal);
            }
        }

        Vector3 shotAudioPos = visualMuzzle != null ? visualMuzzle.position : muzzle.position;
        weaponAudio?.PlayLocalGunshot(shotAudioPos);
        crosshairDriver?.AddFireKick();

        Vector2 kick = weaponRecoil != null ? weaponRecoil.AddShotRecoil() : Vector2.zero;
        if (netAim != null)
            netAim.OwnerTriggerRecoil(kick);

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 initialVel = dir * projectileSpeed;

        if (muzzleFlash != null)
            muzzleFlash.PlayFlash();

        FireServerRpc(muzzle.position, rot, initialVel);
    }

    private void SpawnLocalPredictedWorldImpact(Vector3 position, Vector3 normal)
    {
        if (!localWorldImpactPrefab) return;

        Quaternion rot = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject fx = Instantiate(localWorldImpactPrefab, position, rot);
        Destroy(fx, localWorldImpactLifetime);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Quaternion spawnRot, Vector3 initialVelocity, ServerRpcParams rpcParams = default)
    {
        if (weaponAmmo == null)
            weaponAmmo = GetComponent<WeaponAmmoNetcode>();

        if (weaponAmmo == null)
        {
            Debug.LogWarning("[WeaponShooter] Missing WeaponAmmoNetcode on server.");
            return;
        }

        if (!weaponAmmo.ServerCanShoot())
        {
            weaponAmmo.ServerTryReloadIfEmpty();
            return;
        }

        weaponAmmo.ServerConsumeOne();

        spawnPos += spawnRot * Vector3.forward * 0.25f;

        NetworkObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        Vector3 dir = initialVelocity.sqrMagnitude > 0.0001f
            ? initialVelocity.normalized
            : spawnRot * Vector3.forward;

        var projectile = proj.GetComponent<NetworkProjectile>();
        if (projectile != null)
        {
            ulong? shooterId = ignoreShooterCollision
                ? rpcParams.Receive.SenderClientId
                : (ulong?)null;

            projectile.Initialize(dir, projectileSpeed, shooterId);
        }

        proj.Spawn(true);

        weaponAmmo.ServerTryReloadIfEmpty();

        if (weaponAudio != null)
        {
            weaponAudio.PlayGunshotClientRpc(spawnPos, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = GetOtherClientIds(rpcParams.Receive.SenderClientId)
                }
            });
        }
    }

    private ulong[] GetOtherClientIds(ulong shooterId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return new ulong[0];

        var result = new List<ulong>();
        foreach (var id in nm.ConnectedClientsIds)
        {
            if (id != shooterId)
                result.Add(id);
        }

        return result.ToArray();
    }
}