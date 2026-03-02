using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShooter : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private NetworkObject projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float projectileSpeed = 35f;
    [SerializeField] private float maxAimDistance = 250f;
    [SerializeField] private float fireCooldown = 0.12f;
    [SerializeField] private bool fullAuto = true;

    [Header("Aim Raycast")]
    [Tooltip("Layers that can be aimed at. Exclude Player layer to avoid hitting yourself.")]
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Collision")]
    [SerializeField] private bool ignoreShooterCollision = true;

    [Header("Audio")]
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private float gunshotVolume = 1f;
    [SerializeField] private float gunshotPitchMin = 0.95f;
    [SerializeField] private float gunshotPitchMax = 1.05f;

    private PlayerControls input;
    private InputAction fireAction;

    private float nextFireTime;
    private Camera ownerCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        input = new PlayerControls();
        input.Enable();

        fireAction = input.Gameplay.Fire;
        ownerCam = Camera.main;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (input != null)
        {
            input.Disable();
            input.Dispose();
            input = null;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (muzzle == null || projectilePrefab == null) return;
        if (Time.time < nextFireTime) return;
        if (fireAction == null) return;

        bool wantsFire = fullAuto
            ? fireAction.IsPressed()
            : fireAction.WasPressedThisFrame();

        if (!wantsFire) return;

        nextFireTime = Time.time + fireCooldown;

        if (ownerCam == null) ownerCam = Camera.main;
        if (ownerCam == null) return;

        Ray ray = ownerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;
        else
            aimPoint = ray.origin + ray.direction * maxAimDistance;

        Vector3 dir = (aimPoint - muzzle.position);
        if (dir.sqrMagnitude < 0.0001f)
            dir = muzzle.forward;
        dir.Normalize();

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 initialVel = dir * projectileSpeed;

        PlayLocalGunshot(muzzle.position);

        FireServerRpc(muzzle.position, rot, initialVel);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Quaternion spawnRot, Vector3 initialVelocity, ServerRpcParams rpcParams = default)
    {
        // Spawn slightly forward so we don't immediately start inside a wall/enemy capsule
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

        // 🔊 Tell all OTHER clients to play the gunshot
        PlayGunshotClientRpc(spawnPos, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = GetOtherClientIds(rpcParams.Receive.SenderClientId)
            }
        });
    }

    // Plays instantly for shooter only
    private void PlayLocalGunshot(Vector3 position)
    {
        if (!gunshotClip) return;

        AudioSource.PlayClipAtPoint(gunshotClip, position, gunshotVolume);
    }

    // Plays for everyone except the shooter
    [ClientRpc]
    private void PlayGunshotClientRpc(Vector3 position, ClientRpcParams rpcParams = default)
    {
        if (!gunshotClip) return;

        GameObject temp = new GameObject("GunshotAudio");
        temp.transform.position = position;

        var audio = temp.AddComponent<AudioSource>();
        audio.clip = gunshotClip;
        audio.spatialBlend = 1f; // 3D
        audio.volume = gunshotVolume;
        audio.pitch = Random.Range(gunshotPitchMin, gunshotPitchMax);
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        audio.minDistance = 5f;
        audio.maxDistance = 60f;
        audio.Play();

        Destroy(temp, gunshotClip.length + 0.1f);
    }

    private ulong[] GetOtherClientIds(ulong shooterId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return new ulong[0];

        var list = nm.ConnectedClientsIds;
        var result = new System.Collections.Generic.List<ulong>();

        foreach (var id in list)
        {
            if (id != shooterId)
                result.Add(id);
        }

        return result.ToArray();
    }
}