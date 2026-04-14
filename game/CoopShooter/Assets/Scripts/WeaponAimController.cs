using Unity.Netcode;
using UnityEngine;

public class WeaponAimController : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponMuzzle;

    [Header("Crosshair Aim")]
    [SerializeField] private float maxAimDistance = 200f;
    [SerializeField] private float minimumCameraHitDistance = 4f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Visual Aim Limits")]
    [SerializeField] private float minLocalPitch = -35f;
    [SerializeField] private float maxLocalPitch = 55f;
    [SerializeField] private float maxLocalYaw = 70f;
    [SerializeField] private bool lockRoll = true;

    [Header("Visual Smoothing")]
    [Tooltip("0 = immediate. Try 18-30 for modern TPS.")]
    [SerializeField] private float aimSharpness = 25f;
    [SerializeField] private float remoteAimSharpness = 20f;

    [Header("Shot Solve")]
    [SerializeField] private float minimumShotDistance = 0.35f;
    [SerializeField] private float closeRangeBlendEndDistance = 1.25f;
    [SerializeField] private float closeRangeBlendStartDistance = 3f;
    [SerializeField] private float maxShotAngleFromCamera = 14f;

    [Header("Networking")]
    [SerializeField] private NetworkWeaponAim netAim;
    [SerializeField] private float forcedAimSyncInterval = 0.04f;

    [Header("Remote Recoil Visuals")]
    [SerializeField] private float recoilReturnSpeed = 18f;
    [SerializeField] private float recoilSnappiness = 28f;
    [SerializeField] private float recoilMaxPitch = 18f;
    [SerializeField] private float recoilMaxYaw = 8f;
    [SerializeField] private int maxBufferedRemoteShots = 3;

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

        Vector2 visualAngles;

        if (IsOwner)
        {
            if (!mainCam)
                mainCam = Camera.main;
            if (!mainCam)
                return;

            Vector2 desiredAngles = ResolveVisualAimAnglesFromCamera();
            visualAngles = SmoothAimAngles(ref localAimCurrent, ref localAimInitialized, desiredAngles, aimSharpness, Time.deltaTime);

            SyncAimAnglesIfNeeded(visualAngles);
        }
        else
        {
            ConsumeRemoteRecoilEvents();
            visualAngles = SmoothAimAngles(ref remoteAimCurrent, ref remoteAimInitialized, netAim.WeaponAimAngles.Value, remoteAimSharpness, Time.deltaTime);
            UpdateRemoteRecoil(Time.deltaTime);
        }

        ApplyLocalAimAngles(visualAngles + recoilCurrent);
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
        Vector3 fallbackForward = shotOrigin != null
            ? shotOrigin.forward
            : (weaponPivot != null ? weaponPivot.forward : Vector3.forward);

        Vector3 cameraForward = GetCameraForwardDirection(aimCamera, fallbackForward);
        Vector3 aimPoint = ResolveAimPoint(aimCamera, out _, out _);

        Vector3 origin = shotOrigin != null
            ? shotOrigin.position
            : (weaponMuzzle != null ? weaponMuzzle.position : weaponPivot.position);

        Vector3 directionToAimPoint = aimPoint - origin;
        if (directionToAimPoint.sqrMagnitude < (minimumShotDistance * minimumShotDistance))
            return cameraForward;

        Vector3 shotDirection = directionToAimPoint.normalized;
        float targetDistance = directionToAimPoint.magnitude;
        float closeBlend = Mathf.InverseLerp(closeRangeBlendEndDistance, closeRangeBlendStartDistance, targetDistance);
        shotDirection = Vector3.Slerp(cameraForward, shotDirection, closeBlend);

        float angleFromCamera = Vector3.Angle(cameraForward, shotDirection);
        if (angleFromCamera > maxShotAngleFromCamera && angleFromCamera > 0.001f)
        {
            float clampT = maxShotAngleFromCamera / angleFromCamera;
            shotDirection = Vector3.Slerp(cameraForward, shotDirection, clampT);
        }

        return shotDirection.normalized;
    }

    private Vector2 ResolveVisualAimAnglesFromCamera()
    {
        Vector3 cameraForward = GetCameraForwardDirection(mainCam, weaponPivot != null ? weaponPivot.forward : Vector3.forward);
        Quaternion desiredWorld = Quaternion.LookRotation(cameraForward, Vector3.up);

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

        float pitch = Mathf.Clamp(NormalizeAngle(localEuler.x), minLocalPitch, maxLocalPitch);
        float yaw = Mathf.Clamp(NormalizeAngle(localEuler.y), -maxLocalYaw, maxLocalYaw);
        return new Vector2(pitch, yaw);
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

    private Vector3 GetCameraForwardDirection(Camera aimCamera, Vector3 fallbackDirection)
    {
        if (aimCamera == null)
            return fallbackDirection.normalized;

        return aimCamera.transform.forward.normalized;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
