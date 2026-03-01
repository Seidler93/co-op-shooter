using UnityEngine;

public class WeaponAimController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform weaponPitchPivot;
    [SerializeField] private Transform weaponMuzzle;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 200f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Smoothing")]
    [Tooltip("0 = immediate. Try 0.04–0.08 for modern TPS.")]
    [SerializeField] private float pitchSmoothTime = 0.06f;

    private float currentPitch;
    private float pitchVel;

    void Start()
    {
        // Initialize currentPitch from the pivot so there’s no first-frame pop
        float p = weaponPitchPivot.localEulerAngles.x;
        if (p > 180f) p -= 360f;
        currentPitch = p;
    }

    void LateUpdate()
    {
        if (!mainCam || !weaponPitchPivot) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = ray.origin + ray.direction * maxAimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 origin = weaponMuzzle ? weaponMuzzle.position : weaponPitchPivot.position;
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.0001f) return;

        Transform parent = weaponPitchPivot.parent;
        Vector3 localDir = parent ? parent.InverseTransformDirection(dir.normalized) : dir.normalized;

        float targetPitch = -Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;

        if (pitchSmoothTime <= 0f)
        {
            currentPitch = targetPitch; // immediate
        }
        else
        {
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVel, pitchSmoothTime);
        }

        weaponPitchPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }
}