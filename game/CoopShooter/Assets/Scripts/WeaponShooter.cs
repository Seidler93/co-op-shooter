using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShooter : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;                 // ballistic muzzle (used for direction + server spawn input)
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

    [Tooltip("Assign the transform you want to visually recoil (gun pivot).")]
    [SerializeField] private Transform recoilPivot;

    [Tooltip("Flip if recoil goes the wrong direction on your rig. Start with -1 for kick-up in many setups.")]
    [SerializeField] private float recoilPitchSign = -1f;

    [Tooltip("Flip if yaw jitter goes the wrong direction.")]
    [SerializeField] private float recoilYawSign = 1f;

    [Header("Networking (optional)")]
    [Tooltip("Optional: replicate weapon recoil to other clients using NetworkWeaponAim on the Player root.")]
    [SerializeField] private NetworkWeaponAim netAim;

    [Header("Audio")]
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private float gunshotVolume = 1f;
    [SerializeField] private float gunshotPitchMin = 0.95f;
    [SerializeField] private float gunshotPitchMax = 1.05f;

    [Header("Local Tracer Travel (Owner Only)")]
    [SerializeField] private bool useLocalTracer = true;

    [Tooltip("The muzzle used for VFX/tracer (usually affected by recoil). If null, falls back to muzzle.")]
    [SerializeField] private Transform visualMuzzle;

    [SerializeField] private float tracerWidth = 0.02f;

    [Tooltip("How fast the tracer head travels (units/sec). Try matching projectileSpeed or higher.")]
    [SerializeField] private float tracerSpeed = 120f;

    [Tooltip("Maximum lifetime (seconds) so tracers don't linger at long range.")]
    [SerializeField] private float tracerMaxLifetime = 0.20f;

    [Tooltip("Tail length behind the tracer head (units).")]
    [SerializeField] private float tracerTailLength = 1.5f;

    [Tooltip("Material for the tracer. Recommended to assign in inspector. If null, uses Sprites/Default.")]
    [SerializeField] private Material tracerMaterial;

    // Recoil state
    private Vector2 recoilTarget;  // x=pitch (magnitude), y=yaw (magnitude)
    private Vector2 recoilCurrent;
    private Quaternion recoilBaseLocalRot;
    private bool fireHeld;

    private PlayerControls input;
    private InputAction fireAction;

    private float nextFireTime;
    private Camera ownerCam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (!netAim)
            netAim = GetComponentInParent<NetworkWeaponAim>();

        input = new PlayerControls();
        input.Enable();

        fireAction = input.Gameplay.Fire;

        ownerCam = Camera.main;

        // Sensible defaults
        if (!recoilPivot && muzzle)
            recoilPivot = muzzle.parent;

        if (!visualMuzzle && muzzle)
            visualMuzzle = muzzle;

        // Initialize base rot so first shot doesn't pop
        if (recoilPivot)
            recoilBaseLocalRot = recoilPivot.localRotation;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && input != null)
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
        if (fireAction == null) return;

        // Held state used by recoil behavior
        fireHeld = fireAction.IsPressed();

        // Cache base rotation AFTER aim controller has run (we apply recoil additively in LateUpdate)
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
        Ray camRay = ownerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint;
        if (Physics.Raycast(camRay, out RaycastHit camHit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = camHit.point;
        else
            aimPoint = camRay.origin + camRay.direction * maxAimDistance;

        // Shot direction is from muzzle to aimPoint (your current behavior)
        Vector3 dir = (aimPoint - muzzle.position);
        if (dir.sqrMagnitude < 0.0001f)
            dir = muzzle.forward;
        dir.Normalize();

        // LOCAL TRACER: use the actual muzzle-line hit (so strafing/cover matches shot path)
        if (useLocalTracer)
        {
            Vector3 tracerStart = (visualMuzzle != null) ? visualMuzzle.position : muzzle.position;

            Vector3 muzzleLineEnd = muzzle.position + dir * maxAimDistance;
            if (Physics.Raycast(muzzle.position, dir, out RaycastHit tHit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
                muzzleLineEnd = tHit.point;

            float dist = Vector3.Distance(muzzle.position, muzzleLineEnd);
            Vector3 tracerEnd = tracerStart + dir * dist;

            SpawnTravelingTracer(tracerStart, tracerEnd);
        }

        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 initialVel = dir * projectileSpeed;

        // Instant local audio for shooter (zero latency feel)
        Vector3 shotAudioPos = (visualMuzzle != null) ? visualMuzzle.position : muzzle.position;
        PlayLocalGunshot(shotAudioPos);

        // Apply local recoil (visual only, local feel)
        Vector2 kick = ApplyRecoilAndReturnKick();

        // Optional: replicate recoil as a visual event for other clients (does not affect their camera)
        if (netAim != null)
            netAim.OwnerTriggerRecoil(kick);

        // NOTE: still using client-provided spawnPos for prototype
        FireServerRpc(muzzle.position, rot, initialVel);
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        UpdateRecoil(Time.deltaTime);
    }

    // Returns the exact kick we applied for replication (SIGNED space)
    private Vector2 ApplyRecoilAndReturnKick()
    {
        float pitchKickMag = recoilPitchPerShot;
        float yawKickMag = Random.Range(-recoilYawJitter, recoilYawJitter);

        // Local accumulation stays in magnitude space
        recoilTarget.x += pitchKickMag;
        recoilTarget.y += yawKickMag;

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, 0f, recoilMaxPitch);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, -recoilMaxYaw, recoilMaxYaw);

        // Replicate in applied sign space so remotes match direction
        float signedPitch = pitchKickMag * recoilPitchSign; // often negative means kick-up on many rigs
        float signedYaw = yawKickMag * recoilYawSign;

        return new Vector2(signedPitch, signedYaw);
    }

    private void UpdateRecoil(float dt)
    {
        if (recoilPivot == null) return;

        float returnSpeed = fireHeld ? recoilReturnWhileFiring : recoilReturnWhenReleased;

        // Return target to zero
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, returnSpeed * dt);

        // Smooth current toward target
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, recoilSnappiness * dt);

        float pitch = recoilCurrent.x * recoilPitchSign;
        float yaw = recoilCurrent.y * recoilYawSign;

        // Offset around explicit local axes
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);
        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);
        Quaternion recoilOffset = yawRot * pitchRot;

        // Apply additively on top of cached base rotation
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

    // -----------------------------
    // Traveling Local Tracer (owner)
    // -----------------------------

    private void SpawnTravelingTracer(Vector3 start, Vector3 end)
    {
        GameObject go = new GameObject("LocalTracer");
        go.transform.position = start;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;

        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth * 0.6f;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Material (avoid creating a new material per shot if you assign one in inspector)
        lr.material = tracerMaterial != null
            ? tracerMaterial
            : new Material(Shader.Find("Sprites/Default"));

        // Start as 0-length at muzzle
        lr.SetPosition(0, start);
        lr.SetPosition(1, start);

        StartCoroutine(TravelTracerRoutine(lr, start, end));
    }

    private IEnumerator TravelTracerRoutine(LineRenderer lr, Vector3 start, Vector3 end)
    {
        float totalDist = Vector3.Distance(start, end);
        if (totalDist < 0.001f)
        {
            if (lr != null) Destroy(lr.gameObject);
            yield break;
        }

        float speed = Mathf.Max(1f, tracerSpeed);
        float travelTime = totalDist / speed;

        // Cap lifetime
        travelTime = Mathf.Min(travelTime, tracerMaxLifetime);

        float t = 0f;
        Vector3 dir = (end - start).normalized;

        while (t < 1f && lr != null)
        {
            t += Time.deltaTime / travelTime;

            Vector3 head = Vector3.Lerp(start, end, t);

            float tailDist = Mathf.Min(tracerTailLength, Vector3.Distance(start, head));
            Vector3 tail = head - dir * tailDist;

            lr.SetPosition(0, tail);
            lr.SetPosition(1, head);

            yield return null;
        }

        if (lr != null)
            Destroy(lr.gameObject);
    }
}