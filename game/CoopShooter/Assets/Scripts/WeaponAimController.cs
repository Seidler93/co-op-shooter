using Unity.Netcode;
using UnityEngine;

public class WeaponAimController : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCam;              // owner only
    [SerializeField] private Transform playerRoot;        // yaw basis (player root)
    [SerializeField] private Transform weaponPivot;       // your handle pivot (single pivot)
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

    // Remote recoil spring (visual only)
    [Header("Remote Recoil Visuals")]
    [SerializeField] private float recoilReturnSpeed = 18f;
    [SerializeField] private float recoilSnappiness = 28f;
    [SerializeField] private float recoilMaxPitch = 18f;
    [SerializeField] private float recoilMaxYaw = 8f;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;
    private int lastRecoilSeqSeen;

    private void Awake()
    {
        if (!netAim) netAim = GetComponentInParent<NetworkWeaponAim>();

        if (!playerRoot)
            playerRoot = transform.root; // fallback; best to assign to player root explicitly

        if (!mainCam && IsOwner)
            mainCam = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        if (!netAim) netAim = GetComponentInParent<NetworkWeaponAim>();
        if (netAim != null) lastRecoilSeqSeen = netAim.RecoilSeq.Value;

        // Owner uses camera; remote does not need Camera.main
        if (IsOwner && !mainCam) mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (!weaponPivot || !playerRoot || !netAim) return;

        // Owner: compute aim angles from camera center and replicate
        if (IsOwner)
        {
            if (!mainCam) mainCam = Camera.main;
            if (!mainCam) return;

            Quaternion desiredWorld = ComputeDesiredWorldRotationFromCameraRay();
            Vector2 angles = WorldToLocalAimAngles(desiredWorld);

            // Apply locally
            Vector2 total = angles + recoilCurrent; // recoilCurrent is signed now
            ApplyLocalAimAngles(total);

            // Replicate to others
            netAim.OwnerSetAimAngles(angles);
        }
        // Remote: apply replicated aim angles (and replicated recoil events)
        else
        {
            ConsumeRemoteRecoilEvents();

            Vector2 angles = netAim.WeaponAimAngles.Value;
            ApplyLocalAimAngles(angles);

            UpdateRemoteRecoil(Time.deltaTime);

            // Add recoil on top (pitch up + yaw jitter)
            Vector2 total = angles + recoilCurrent;
            ApplyLocalAimAngles(total);
        }
    }

    private Quaternion ComputeDesiredWorldRotationFromCameraRay()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = ray.origin + ray.direction * maxAimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 origin = weaponMuzzle ? weaponMuzzle.position : weaponPivot.position;
        Vector3 dir = (aimPoint - origin);
        if (dir.sqrMagnitude < 0.0001f)
            dir = weaponPivot.forward;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (lockRoll)
        {
            Vector3 e = desired.eulerAngles;
            desired = Quaternion.Euler(NormalizeAngle(e.x), NormalizeAngle(e.y), 0f);
        }

        // Smooth in world space for owner feel
        float t = 1f - Mathf.Exp(-aimSharpness * Time.deltaTime);
        return Quaternion.Slerp(weaponPivot.rotation, desired, t);
    }

    // Convert world rotation to (pitch,yaw) relative to playerRoot
    private Vector2 WorldToLocalAimAngles(Quaternion desiredWorld)
    {
        Quaternion local = Quaternion.Inverse(playerRoot.rotation) * desiredWorld;
        Vector3 e = local.eulerAngles;

        float pitch = NormalizeAngle(e.x);
        float yaw = NormalizeAngle(e.y);

        return new Vector2(pitch, yaw);
    }

    // Apply angles relative to playerRoot (no roll)
    private void ApplyLocalAimAngles(Vector2 angles)
    {
        Quaternion localRot = Quaternion.Euler(angles.x, angles.y, 0f);
        weaponPivot.rotation = playerRoot.rotation * localRot;
    }

    private void ConsumeRemoteRecoilEvents()
    {
        int seq = netAim.RecoilSeq.Value;
        if (seq == lastRecoilSeqSeen) return;

        lastRecoilSeqSeen = seq;

        Vector2 kick = netAim.RecoilKick.Value;

        recoilTarget.x += kick.x;
        recoilTarget.y += kick.y;

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, -recoilMaxPitch, recoilMaxPitch);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, -recoilMaxYaw, recoilMaxYaw);
    }

    private void UpdateRemoteRecoil(float dt)
    {
        // Return target to 0
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, recoilReturnSpeed * dt);
        // Smooth current toward target
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, recoilSnappiness * dt);
    }

    private float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}