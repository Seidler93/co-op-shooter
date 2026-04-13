using UnityEngine;

public class WeaponRecoil : MonoBehaviour
{
    [Header("Recoil (client-only feel)")]
    [SerializeField] private float recoilPitchPerShot = 2.5f;
    [SerializeField] private float recoilYawJitter = 0.4f;
    [SerializeField] private float recoilMaxPitch = 18f;
    [SerializeField] private float recoilMaxYaw = 8f;
    [SerializeField] private float recoilReturnWhileFiring = 3f;
    [SerializeField] private float recoilReturnWhenReleased = 10f;
    [SerializeField] private float recoilSnappiness = 18f;

    [SerializeField] private Transform recoilPivot;
    [SerializeField] private float recoilPitchSign = -1f;
    [SerializeField] private float recoilYawSign = 1f;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;

    public void SetRecoilPivot(Transform pivot)
    {
        recoilPivot = pivot;
    }

    public Vector2 AddShotRecoil()
    {
        float pitchKickMag = recoilPitchPerShot;
        float yawKickMag = Random.Range(-recoilYawJitter, recoilYawJitter);

        recoilTarget.x += pitchKickMag;
        recoilTarget.y += yawKickMag;

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, 0f, recoilMaxPitch);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, -recoilMaxYaw, recoilMaxYaw);

        float signedPitch = pitchKickMag * recoilPitchSign;
        float signedYaw = yawKickMag * recoilYawSign;

        return new Vector2(signedPitch, signedYaw);
    }

    public void TickRecoil(float dt, bool fireHeld)
    {
        if (recoilPivot == null) return;

        float returnSpeed = fireHeld ? recoilReturnWhileFiring : recoilReturnWhenReleased;

        recoilTarget.x = Mathf.MoveTowards(recoilTarget.x, 0f, returnSpeed * dt);
        recoilTarget.y = Mathf.MoveTowards(recoilTarget.y, 0f, returnSpeed * dt);

        float smoothT = 1f - Mathf.Exp(-recoilSnappiness * dt);
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, smoothT);

        float pitch = recoilCurrent.x * recoilPitchSign;
        float yaw = recoilCurrent.y * recoilYawSign;

        Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);
        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);

        recoilPivot.localRotation = yawRot * pitchRot;
    }

    public void ResetRecoil()
    {
        recoilTarget = Vector2.zero;
        recoilCurrent = Vector2.zero;

        if (recoilPivot != null)
            recoilPivot.localRotation = Quaternion.identity;
    }
}
