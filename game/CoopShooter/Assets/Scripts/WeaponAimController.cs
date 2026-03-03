using UnityEngine;

public class WeaponAimController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform weaponPivot;      // your handle pivot (single pivot)
    [SerializeField] private Transform weaponMuzzle;

    [Header("Aim")]
    [SerializeField] private float maxAimDistance = 200f;
    [SerializeField] private LayerMask aimMask = ~0;

    [Header("Smoothing")]
    [Tooltip("0 = immediate. Try 18–30 for modern TPS.")]
    [SerializeField] private float aimSharpness = 25f;

    [Tooltip("Prevents the gun from rolling sideways.")]
    [SerializeField] private bool lockRoll = true;

    private void LateUpdate()
    {
        if (!mainCam || !weaponPivot) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = ray.origin + ray.direction * maxAimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
            aimPoint = hit.point;

        Vector3 origin = weaponMuzzle ? weaponMuzzle.position : weaponPivot.position;
        Vector3 dir = aimPoint - origin;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (lockRoll)
        {
            Vector3 e = desired.eulerAngles;
            desired = Quaternion.Euler(NormalizeAngle(e.x), NormalizeAngle(e.y), 0f);
        }

        float t = 1f - Mathf.Exp(-aimSharpness * Time.deltaTime);
        weaponPivot.rotation = Quaternion.Slerp(weaponPivot.rotation, desired, t);
    }

    private float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}