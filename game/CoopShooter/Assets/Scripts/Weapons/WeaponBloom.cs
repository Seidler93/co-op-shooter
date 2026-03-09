using UnityEngine;

public class WeaponBloom : MonoBehaviour
{
    [Header("Bloom / Spread")]
    [SerializeField] private float hipBloomDeg = 0.75f;
    [SerializeField] private float adsBloomDeg = 0.15f;
    [SerializeField] private float bloomGrowPerShot = 0.06f;
    [SerializeField] private float bloomMaxExtra = 1.5f;
    [SerializeField] private float bloomRecoverSpeed = 12f;

    [SerializeField] private PlayerState playerState;

    private float bloomExtra;

    public float CurrentBloomExtra => bloomExtra;

    private void Awake()
    {
        if (!playerState)
            playerState = GetComponentInParent<PlayerState>();
    }

    public void TickRecovery(float dt, bool fireHeld)
    {
        if (fireHeld) return;

        bool aiming = playerState != null && playerState.IsAiming;
        float recover = aiming ? bloomRecoverSpeed * 1.5f : bloomRecoverSpeed;

        bloomExtra = Mathf.MoveTowards(bloomExtra, 0f, recover * dt);
    }

    public void AddBloomOnShot()
    {
        bool aiming = playerState != null && playerState.IsAiming;
        float grow = aiming ? bloomGrowPerShot * 0.35f : bloomGrowPerShot;
        bloomExtra = Mathf.Min(bloomExtra + grow, bloomMaxExtra);
    }

    public float GetCurrentBloomDeg()
    {
        bool aiming = playerState != null && playerState.IsAiming;
        float baseBloom = aiming ? adsBloomDeg : hipBloomDeg;
        return baseBloom + bloomExtra;
    }

    public float GetBloom01()
    {
        bool aiming = playerState != null && playerState.IsAiming;
        float baseBloom = aiming ? adsBloomDeg : hipBloomDeg;
        float maxBloom = baseBloom + bloomMaxExtra;
        float currentBloom = baseBloom + bloomExtra;
        return Mathf.InverseLerp(baseBloom, maxBloom, currentBloom);
    }

    public Vector3 ApplyBloomCameraRelative(Vector3 direction, float maxAngleDeg, Camera ownerCam)
    {
        if (maxAngleDeg <= 0f) return direction;

        Vector2 r = Random.insideUnitCircle * maxAngleDeg;

        Vector3 up = ownerCam ? ownerCam.transform.up : Vector3.up;
        Vector3 right = ownerCam ? ownerCam.transform.right : Vector3.right;

        Quaternion spread = Quaternion.AngleAxis(r.x, up) * Quaternion.AngleAxis(-r.y, right);
        return (spread * direction).normalized;
    }
}