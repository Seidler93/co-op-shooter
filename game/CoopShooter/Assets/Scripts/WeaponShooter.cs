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

    [Header("Fire Mode")]
    [SerializeField] private bool fullAuto = true;

    [Header("Aim Raycast")]
    [Tooltip("Layers that can be aimed at. Exclude Player layer to avoid hitting yourself.")]
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Collision")]
    [SerializeField] private bool ignoreShooterCollision = true;

     [Header("Recoil (client-only feel)")]
    [Tooltip("Degrees of pitch added per shot (magnitude; direction controlled by recoilPitchSign).")]
    [SerializeField] private float recoilPitchPerShot = 1.2f;

    [Tooltip("Random yaw added per shot (left/right).")]
    [SerializeField] private float recoilYawJitter = 0.6f;

    [SerializeField] private float recoilMaxPitch = 18f;
    [SerializeField] private float recoilMaxYaw = 8f;

    [Tooltip("How quickly recoil target returns to 0 while holding fire (LOWER = more climb during spray).")]
    [SerializeField] private float recoilReturnWhileFiring = 2f;

    [Tooltip("How quickly recoil target returns to 0 after releasing fire.")]
    [SerializeField] private float recoilReturnWhenReleased = 18f;

    [Tooltip("How fast current recoil follows the target.")]
    [SerializeField] private float recoilSnappiness = 28f;

    [Tooltip("Assign the transform you want to visually recoil (gun pitch pivot or cam pivot).")]
    [SerializeField] private Transform recoilPivot;

    [Tooltip("Flip if recoil goes the wrong direction on your rig. Start with -1 for kick-up in many setups.")]
    [SerializeField] private float recoilPitchSign = -1f;

    [Tooltip("Flip if yaw jitter goes the wrong direction.")]
    [SerializeField] private float recoilYawSign = 1f;

    // Recoil state
    private Vector2 recoilTarget;  // x=pitch, y=yaw
    private Vector2 recoilCurrent;
    private Quaternion recoilBaseLocalRot;
    private bool fireHeld;

    private PlayerControls input;
    private InputAction fireAction;

    private float nextFireTime;
    private Camera ownerCam;

    [Header("Audio")]
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private float gunshotVolume = 1f;
    [SerializeField] private float gunshotPitchMin = 0.95f;
    [SerializeField] private float gunshotPitchMax = 1.05f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        input = new PlayerControls();
        input.Enable();

        fireAction = input.Gameplay.Fire;
        ownerCam = Camera.main;
    }

    private void OnDestroy()
    {
        if (IsOwner && input != null)
        {
            input.Disable();
            input.Dispose();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (muzzle == null || projectilePrefab == null) return;

        if (fireAction == null) return;

        // Held state used by recoil behavior
        fireHeld = fireAction.IsPressed();

        // Cache the "base" local rotation AFTER your aim/weapon logic has done its work this frame.
        // Recoil will be applied additively in LateUpdate so it won't get stomped while spraying.
        if (recoilPivot != null)
            recoilBaseLocalRot = recoilPivot.localRotation;

        // Fire input (semi vs auto)
        bool wantsFire = fullAuto
            ? fireAction.IsPressed()
            : fireAction.WasPressedThisFrame();

        if (!wantsFire) return;

        // Fire rate gate
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        if (ownerCam == null) ownerCam = Camera.main;
        if (ownerCam == null) return;

        // Ray from screen center
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

        // Instant local audio for shooter (zero latency feel)
        PlayLocalGunshot(muzzle.position);

        // Recoil accumulates per shot (visual gets applied in LateUpdate)
        ApplyRecoil();

        FireServerRpc(muzzle.position, rot, initialVel);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        UpdateRecoil(Time.deltaTime);
    }

    private void ApplyRecoil()
    {
        recoilTarget.x += recoilPitchPerShot;
        recoilTarget.y += Random.Range(-recoilYawJitter, recoilYawJitter);

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, 0f, recoilMaxPitch);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, -recoilMaxYaw, recoilMaxYaw);
    }

    private void UpdateRecoil(float dt)
    {
        if (recoilPivot == null) return;

        float returnSpeed = fireHeld ? recoilReturnWhileFiring : recoilReturnWhenReleased;

        // Return target to zero (use different return speed while firing vs released)
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, returnSpeed * dt);

        // Smooth current toward target
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, recoilSnappiness * dt);

        float pitch = recoilCurrent.x * recoilPitchSign;
        float yaw = recoilCurrent.y * recoilYawSign;

        // Offset around explicit local axes (robust across rigs)
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);
        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion recoilOffset = yawRot * pitchRot;

        // Apply additively ON TOP of cached base rotation (prevents "no recoil while firing" stomp)
        recoilPivot.localRotation = recoilBaseLocalRot * recoilOffset;
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