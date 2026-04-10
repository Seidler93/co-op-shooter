using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private MonoBehaviour[] disableOnDeathBehaviours;
    [SerializeField] private GameObject[] hideOnDeathObjects;

    [Header("Debug")]
    [SerializeField] private bool logPlayerDeath = true;

    public Health Health => health;
    public bool IsAlive => health != null && health.IsAlive;
    public int CurrentHP => health != null ? health.CurrentHP.Value : 0;
    public int MaxHP => health != null ? health.MaxHP : 0;

    public event Action<PlayerHealth> PlayerDied;
    public event Action<int, int> PlayerHealthChanged;

    public static event Action<PlayerHealth> AnyPlayerDiedServer;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerState) playerState = GetComponent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        if (health == null)
        {
            Debug.LogError($"[{name}] PlayerHealth requires a Health component.");
            return;
        }

        health.Died += HandleDied;
        health.HealthChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
            health.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        PlayerHealthChanged?.Invoke(current, max);
    }

    private void HandleDied(Health deadHealth)
    {
        if (logPlayerDeath)
        {
            Debug.Log($"[PlayerHealth] {name} died. OwnerClientId={OwnerClientId}, IsServer={IsServer}, IsOwner={IsOwner}");
        }

        PlayerDied?.Invoke(this);

        if (playerState != null)
        {
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
            playerState.SetDead(true);
        }

        ApplyLocalDeathState();

        if (IsServer)
            AnyPlayerDiedServer?.Invoke(this);
    }

    private void ApplyLocalDeathState()
    {
        if (disableOnDeathBehaviours != null)
        {
            for (int i = 0; i < disableOnDeathBehaviours.Length; i++)
            {
                if (disableOnDeathBehaviours[i] != null)
                    disableOnDeathBehaviours[i].enabled = false;
            }
        }

        if (hideOnDeathObjects != null)
        {
            for (int i = 0; i < hideOnDeathObjects.Length; i++)
            {
                if (hideOnDeathObjects[i] != null)
                    hideOnDeathObjects[i].SetActive(false);
            }
        }
    }

    public void ReviveToFull()
    {
        if (!IsServer) return;
        if (health == null) return;

        health.ResetToFull();
        ReviveClientRpc();
    }

    [ClientRpc]
    private void ReviveClientRpc()
    {
        if (playerState != null)
        {
            playerState.SetDead(false);
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
        }

        if (disableOnDeathBehaviours != null)
        {
            for (int i = 0; i < disableOnDeathBehaviours.Length; i++)
            {
                if (disableOnDeathBehaviours[i] != null)
                    disableOnDeathBehaviours[i].enabled = true;
            }
        }

        if (hideOnDeathObjects != null)
        {
            for (int i = 0; i < hideOnDeathObjects.Length; i++)
            {
                if (hideOnDeathObjects[i] != null)
                    hideOnDeathObjects[i].SetActive(true);
            }
        }
    }

    public void ApplyDamage(int amount)
    {
        if (health == null) return;
        health.ApplyDamage(amount, OwnerClientId);
    }

    [ServerRpc]
    public bool Server_Heal(int amount)
    {
        if (CurrentHP.Value >= MaxHP)
            return false;

        CurrentHP.Value = Mathf.Min(CurrentHP.Value + amount, MaxHP);
        HealthChanged?.Invoke(CurrentHP.Value, MaxHP);
        return true;
    }
}

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

    [Header("Shop Upgrades")]
    [SerializeField] private int baseDamage = 20;
    [SerializeField] private int damagePerUpgrade = 5;
    [SerializeField] private float fireRateMultiplierPerUpgrade = 0.9f;

    private int damageUpgradeLevel = 0;
    private int fireRateUpgradeLevel = 0;
    private float defaultFireCooldown;

    [Header("Networking (optional)")]
    [SerializeField] private NetworkWeaponAim netAim;

    [Header("Local Predicted World Impact (Owner Only)")]
    [Tooltip("Use a VISUAL-ONLY prefab here (no audio), so world impacts don't sound doubled.")]
    [SerializeField] private GameObject localWorldImpactPrefab;
    [SerializeField] private GameObject localEnemyImpactPrefab;

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

            if (hitEnemy)
                SpawnLocalPredictedImpact(predictedHit.point, predictedHit.normal, localEnemyImpactPrefab);
            else
                SpawnLocalPredictedImpact(predictedHit.point, predictedHit.normal, localWorldImpactPrefab);
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

    private void SpawnLocalPredictedImpact(Vector3 position, Vector3 normal, GameObject impactPrefab)
    {
        if (impactPrefab == null) return;

        Quaternion rot = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject fx = Instantiate(impactPrefab, position, rot);
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

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Camera targets (ON THIS PLAYER)")]
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform shoulderTarget;

    [Header("Scene camera (ONE in scene)")]
    [SerializeField] private CinemachineCamera sceneAimCam;

    [Header("Optional")]
    [SerializeField] private CameraController cameraController;

    [Header("Score")]
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (!cameraController)
            cameraController = GetComponentInChildren<CameraController>(true);

        if (!camPivot)
        {
            var t = transform.Find("CamPivot");
            if (t) camPivot = t;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
    
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    Debug.Log($"[Player Spawn] {name} | IsOwner={IsOwner} | IsServer={IsServer} | OwnerClientId={OwnerClientId}");

        StartCoroutine(ClaimSceneCameraWhenReady());
    }

    private IEnumerator ClaimSceneCameraWhenReady()
    {
        yield return null;
        yield return null;

        BindLocalHUD();

        if (!sceneAimCam)
            sceneAimCam = FindFirstObjectByType<CinemachineCamera>();

        Debug.Log($"[NetworkPlayer] Claim camera. camPivot={(camPivot ? "OK" : "NULL")} sceneAimCam={(sceneAimCam ? "FOUND" : "NULL")} cameraController={(cameraController ? "OK" : "NULL")}");


        if (!sceneAimCam || !camPivot)
            yield break;

        sceneAimCam.Follow = camPivot;
        sceneAimCam.LookAt = shoulderTarget ? shoulderTarget : camPivot;

        if (cameraController)
            cameraController.SetCinemachine(sceneAimCam);
    }

    private void BindLocalHUD()
    {
        if (!IsOwner) return;

        var ammo = GetComponentInChildren<WeaponAmmoNetcode>(true);
        var hud = FindFirstObjectByType<AmmoHUDNetcode>();

        if (ammo != null && hud != null)
        {
            hud.Bind(ammo);
        }
    }

    public void AddPoints(int amount)
    {
        if (!IsServer) return;

        Score.Value += amount;
        Debug.Log($"[SERVER] {name} gained {amount} points. New score = {Score.Value}");
    }

    public bool TrySpendScore(int amount)
    {
        if (!IsServer)
            return false;

        if (Score.Value < amount)
            return false;

        Score.Value -= amount;
        return true;
    }
}

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WeaponAmmoNetcode : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int reserveMax = 120;
    [SerializeField] private float reloadTime = 1.6f;

    [Header("Start Ammo")]
    [SerializeField] private int startMag = 30;
    [SerializeField] private int startReserve = 90;

    [Header("Optional")]
    [SerializeField] private PlayerState playerState;

    public NetworkVariable<int> MagAmmo = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> ReserveAmmo = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsReloading = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine reloadCo;

    public int MagSize => magSize;
    public float ReloadTime => reloadTime;
    public bool HasAmmoInMag => MagAmmo.Value > 0;
    public bool HasReserveAmmo => ReserveAmmo.Value > 0;
    public bool CanReload => !IsReloading.Value && MagAmmo.Value < magSize && ReserveAmmo.Value > 0;

    private void Awake()
    {
        if (!playerState)
            playerState = GetComponentInParent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        IsReloading.OnValueChanged += OnReloadingChanged;

        if (IsServer)
        {
            MagAmmo.Value = Mathf.Clamp(startMag, 0, magSize);
            ReserveAmmo.Value = Mathf.Clamp(startReserve, 0, reserveMax);
            IsReloading.Value = false;
        }

        playerState?.SetReloading(IsReloading.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsReloading.OnValueChanged -= OnReloadingChanged;
    }

    private void OnReloadingChanged(bool oldValue, bool newValue)
    {
        playerState?.SetReloading(newValue);
    }

    public bool ServerCanShoot()
    {
        return !IsReloading.Value && MagAmmo.Value > 0;
    }

    public void ServerConsumeOne()
    {
        if (!IsServer) return;
        if (IsReloading.Value) return;
        if (MagAmmo.Value <= 0) return;

        MagAmmo.Value -= 1;
    }

    public void ServerTryReloadIfEmpty()
    {
        if (!IsServer) return;

        if (MagAmmo.Value == 0)
            ServerTryReload();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestReloadServerRpc()
    {
        ServerTryReload();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestReloadIfEmptyServerRpc()
    {
        if (MagAmmo.Value == 0)
            ServerTryReload();
    }

    private void ServerTryReload()
    {
        if (!IsServer) return;
        if (IsReloading.Value) return;
        if (MagAmmo.Value >= magSize) return;
        if (ReserveAmmo.Value <= 0) return;

        if (reloadCo != null)
            StopCoroutine(reloadCo);

        reloadCo = StartCoroutine(ServerReloadRoutine());
    }

    private IEnumerator ServerReloadRoutine()
    {
        IsReloading.Value = true;

        yield return new WaitForSeconds(reloadTime);

        int need = magSize - MagAmmo.Value;
        int take = Mathf.Min(need, ReserveAmmo.Value);

        MagAmmo.Value += take;
        ReserveAmmo.Value -= take;

        IsReloading.Value = false;
        reloadCo = null;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void CancelReloadServerRpc()
    {
        if (!IsServer) return;

        if (reloadCo != null)
        {
            StopCoroutine(reloadCo);
            reloadCo = null;
        }

        IsReloading.Value = false;
    }

    [ServerRpc]
    public bool Server_AddReserveAmmo(int amount)
    {
        if (ReserveAmmo.Value >= maxReserveAmmo)
            return false;

        ReserveAmmo.Value = Mathf.Min(ReserveAmmo.Value + amount, maxReserveAmmo);
        return true;
    }
}