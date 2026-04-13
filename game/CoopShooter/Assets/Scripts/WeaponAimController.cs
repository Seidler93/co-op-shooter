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
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Smoothing")]
    [Tooltip("0 = immediate. Try 18–30 for modern TPS.")]
    [SerializeField] private float aimSharpness = 25f;

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
    [SerializeField] private float forcedAimSyncInterval = 0.08f;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;
    private int lastRecoilSeqSeen;
    private Vector2 lastSentAimAngles;
    private float lastAimSyncTime;

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
            lastRecoilSeqSeen = netAim.RecoilSeq.Value;

        if (IsOwner && !mainCam)
            mainCam = Camera.main;

        if (netAim != null)
            lastSentAimAngles = netAim.WeaponAimAngles.Value;
    }

    private void LateUpdate()
    {
        if (!weaponPivot || !netAim) return;

        Vector2 angles;

        if (IsOwner)
        {
            if (!mainCam)
                mainCam = Camera.main;
            if (!mainCam)
                return;

            Quaternion desiredWorld = ComputeDesiredWorldRotationFromCameraRay();
            angles = WorldToLocalAimAngles(desiredWorld);

            SyncAimAnglesIfNeeded(angles);
        }
        else
        {
            ConsumeRemoteRecoilEvents();
            angles = netAim.WeaponAimAngles.Value;
            UpdateRemoteRecoil(Time.deltaTime);
        }

        Vector2 total = angles + recoilCurrent;
        ApplyLocalAimAngles(total);
    }

    private Quaternion ComputeDesiredWorldRotationFromCameraRay()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = ray.origin + ray.direction * maxAimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 origin = weaponMuzzle ? weaponMuzzle.position : weaponPivot.position;
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.0001f)
            dir = weaponPivot.forward;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (lockRoll)
        {
            Vector3 e = desired.eulerAngles;
            desired = Quaternion.Euler(NormalizeAngle(e.x), NormalizeAngle(e.y), 0f);
        }

        if (aimSharpness <= 0f)
            return desired;

        float t = 1f - Mathf.Exp(-aimSharpness * Time.deltaTime);
        return Quaternion.Slerp(weaponPivot.rotation, desired, t);
    }

    private Vector2 WorldToLocalAimAngles(Quaternion desiredWorld)
    {
        Transform parent = weaponPivot.parent;
        if (parent == null)
            return Vector2.zero;

        Quaternion local = Quaternion.Inverse(parent.rotation) * desiredWorld;
        Vector3 e = local.eulerAngles;

        float pitch = NormalizeAngle(e.x);
        float yaw = NormalizeAngle(e.y);

        return new Vector2(pitch, yaw);
    }

    private void ApplyLocalAimAngles(Vector2 angles)
    {
        weaponPivot.localRotation = Quaternion.Euler(angles.x, angles.y, 0f);
    }

    private void ConsumeRemoteRecoilEvents()
    {
        int seq = netAim.RecoilSeq.Value;
        if (seq == lastRecoilSeqSeen) return;

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
        recoilTarget.x = Mathf.MoveTowards(recoilTarget.x, 0f, recoilReturnSpeed * dt);
        recoilTarget.y = Mathf.MoveTowards(recoilTarget.y, 0f, recoilReturnSpeed * dt);

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

    private float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}
