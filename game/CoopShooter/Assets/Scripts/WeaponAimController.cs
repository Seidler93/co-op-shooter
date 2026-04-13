using Unity.Netcode;
using UnityEngine;

public class WeaponAimController : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponMuzzle;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 200f;
    [SerializeField] private float minimumCameraHitDistance = 4f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Smoothing")]
    [Tooltip("0 = immediate. Try 18-30 for modern TPS.")]
    [SerializeField] private float aimSharpness = 25f;
    [SerializeField] private float remoteAimSharpness = 20f;

    [Tooltip("Prevents the gun from rolling sideways.")]
    [SerializeField] private bool lockRoll = true;

    [Header("Networking")]
    [SerializeField] private NetworkWeaponAim netAim;

    [Header("Remote Recoil Visuals")]
    [SerializeField] private float recoilReturnSpeed = 18f;
    [SerializeField] private float recoilSnappiness = 28f;
    [SerializeField] private float recoilMaxPitch = 18f;
    [SerializeField] private float recoilMaxYaw = 8f;
    [SerializeField] private int maxBufferedRemoteShots = 3;

    [Header("Aim Sync")]
    [SerializeField] private float forcedAimSyncInterval = 0.04f;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;
    private int lastRecoilSeqSeen;
    private Vector2 lastSentAimAngles;
    private float lastAimSyncTime;
    private Vector2 localAimCurrent;
    private Vector2 remoteAimCurrent;
    private bool localAimInitialized;
    private bool remoteAimInitialized;

    private void Awake()
    {
        if (!netAim)
            netAim = GetComponentInParent<NetworkWeaponAim>();
    }

    public override void OnNetworkSpawn()
    {
        if (!netAim)
            netAim = GetComponentInParent<NetworkWeaponAim>();

        if (netAim != null)
        {
            lastRecoilSeqSeen = netAim.RecoilSeq.Value;
            lastSentAimAngles = netAim.WeaponAimAngles.Value;
            remoteAimCurrent = lastSentAimAngles;
            remoteAimInitialized = true;
        }

        if (IsOwner && !mainCam)
            mainCam = Camera.main;

        if (weaponPivot != null)
        {
            localAimCurrent = GetCurrentLocalAimAngles();
            localAimInitialized = true;
        }
    }

    private void LateUpdate()
    {
        if (!weaponPivot || !netAim)
            return;

        Vector2 angles;

        if (IsOwner)
        {
            if (!mainCam)
                mainCam = Camera.main;
            if (!mainCam)
                return;

            Vector2 desiredAngles = ResolveDesiredLocalAimAngles();
            angles = SmoothAimAngles(ref localAimCurrent, ref localAimInitialized, desiredAngles, aimSharpness, Time.deltaTime);

            SyncAimAnglesIfNeeded(angles);
        }
        else
        {
            ConsumeRemoteRecoilEvents();
            angles = SmoothAimAngles(ref remoteAimCurrent, ref remoteAimInitialized, netAim.WeaponAimAngles.Value, remoteAimSharpness, Time.deltaTime);
            UpdateRemoteRecoil(Time.deltaTime);
        }

        Vector2 totalAngles = angles + recoilCurrent;
        ApplyLocalAimAngles(totalAngles);
    }

    public Vector3 ResolveAimPoint(Camera aimCamera, out RaycastHit hitInfo, out bool hasHit)
    {
        hitInfo = default;
        hasHit = false;

        if (aimCamera == null)
            return weaponPivot != null
                ? weaponPivot.position + weaponPivot.forward * maxAimDistance
                : Vector3.forward * maxAimDistance;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float resolvedDistance = maxAimDistance;

        if (Physics.Raycast(ray, out hitInfo, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            hasHit = true;
            resolvedDistance = Mathf.Max(hitInfo.distance, minimumCameraHitDistance);
        }

        return ray.origin + ray.direction * resolvedDistance;
    }

    public Vector3 ResolveShotDirection(Camera aimCamera, Transform shotOrigin)
    {
        Vector3 aimPoint = ResolveAimPoint(aimCamera, out _, out _);
        Vector3 origin = shotOrigin != null
            ? shotOrigin.position
            : (weaponMuzzle != null ? weaponMuzzle.position : weaponPivot.position);

        Vector3 direction = aimPoint - origin;
        if (direction.sqrMagnitude < 0.0001f)
            direction = shotOrigin != null ? shotOrigin.forward : weaponPivot.forward;

        return direction.normalized;
    }

    private Vector2 ResolveDesiredLocalAimAngles()
    {
        Vector3 aimPoint = ResolveAimPoint(mainCam, out _, out _);
        Vector3 origin = weaponMuzzle ? weaponMuzzle.position : weaponPivot.position;
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.0001f)
            dir = weaponPivot.forward;

        Quaternion desiredWorld = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (lockRoll)
        {
            Vector3 euler = desiredWorld.eulerAngles;
            desiredWorld = Quaternion.Euler(NormalizeAngle(euler.x), NormalizeAngle(euler.y), 0f);
        }

        Transform parent = weaponPivot.parent;
        if (parent == null)
            return Vector2.zero;

        Quaternion localRotation = Quaternion.Inverse(parent.rotation) * desiredWorld;
        Vector3 localEuler = localRotation.eulerAngles;

        return new Vector2(
            NormalizeAngle(localEuler.x),
            NormalizeAngle(localEuler.y));
    }

    private Vector2 GetCurrentLocalAimAngles()
    {
        Vector3 euler = weaponPivot.localEulerAngles;
        return new Vector2(NormalizeAngle(euler.x), NormalizeAngle(euler.y));
    }

    private Vector2 SmoothAimAngles(ref Vector2 current, ref bool initialized, Vector2 target, float sharpness, float dt)
    {
        if (!initialized || sharpness <= 0f)
        {
            current = target;
            initialized = true;
            return current;
        }

        float t = 1f - Mathf.Exp(-sharpness * dt);
        current.x = Mathf.LerpAngle(current.x, target.x, t);
        current.y = Mathf.LerpAngle(current.y, target.y, t);
        return current;
    }

    private void ApplyLocalAimAngles(Vector2 angles)
    {
        weaponPivot.localRotation = Quaternion.Euler(angles.x, angles.y, 0f);
    }

    private void ConsumeRemoteRecoilEvents()
    {
        int seq = netAim.RecoilSeq.Value;
        if (seq == lastRecoilSeqSeen)
            return;

        Vector2 kick = netAim.RecoilKick.Value;
        int recoilEvents = Mathf.Clamp(seq - lastRecoilSeqSeen, 1, maxBufferedRemoteShots);
        lastRecoilSeqSeen = seq;

        for (int i = 0; i < recoilEvents; i++)
        {
            recoilTarget.x += kick.x;
            recoilTarget.y += kick.y;
        }

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, -recoilMaxPitch, recoilMaxPitch);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, -recoilMaxYaw, recoilMaxYaw);
    }

    private void UpdateRemoteRecoil(float dt)
    {
        float returnT = 1f - Mathf.Exp(-recoilReturnSpeed * dt);
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, returnT);

        float smoothT = 1f - Mathf.Exp(-recoilSnappiness * dt);
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, smoothT);
    }

    private void SyncAimAnglesIfNeeded(Vector2 angles)
    {
        if (netAim == null)
            return;

        bool shouldForceSync = Time.time - lastAimSyncTime >= forcedAimSyncInterval;
        bool changedEnough = Vector2.SqrMagnitude(angles - lastSentAimAngles) > 0.0001f;

        if (!changedEnough && !shouldForceSync)
            return;

        netAim.OwnerSetAimAngles(angles);
        lastSentAimAngles = angles;
        lastAimSyncTime = Time.time;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
