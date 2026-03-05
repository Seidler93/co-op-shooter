using UnityEngine;

public class WeaponIdleSway : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. If assigned, sway reacts slightly to camera look delta.")]
    public Transform cameraTransform;

    [Header("Idle Sway")]
    public float idlePosAmount = 0.01f;      // meters
    public float idleRotAmount = 0.6f;       // degrees
    public float idleSpeed = 1.2f;           // Hz-ish

    [Header("Look Sway")]
    public float lookPosAmount = 0.0025f;    // meters
    public float lookRotAmount = 0.8f;       // degrees
    public float lookSmooth = 12f;           // higher = snappier

    [Header("Movement Influence")]
    public float moveMultiplier = 1.5f;      // sway gets stronger while moving
    public float aimMultiplier = 0.35f;      // sway reduced while aiming

    [Header("Smoothing")]
    public float posLerp = 14f;
    public float rotLerp = 14f;

    // Call these from your player/weapon code
    [HideInInspector] public Vector2 moveInput; // set to your WASD input (-1..1)
    [HideInInspector] public bool isAiming;

    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    Vector2 lookDeltaSmoothed;
    Vector3 posVelocity; // for SmoothDamp (optional)

    Vector3 lastCamForward;
    Vector3 lastCamRight;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;

        if (cameraTransform != null)
        {
            lastCamForward = cameraTransform.forward;
            lastCamRight = cameraTransform.right;
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        float stateMult = 1f;
        if (moveInput.sqrMagnitude > 0.01f) stateMult *= moveMultiplier;
        if (isAiming) stateMult *= aimMultiplier;

        // 1) Idle “breathing” sway (sin/cos)
        float t = Time.time * idleSpeed * 2f * Mathf.PI;
        float idleX = Mathf.Sin(t) * idlePosAmount;
        float idleY = Mathf.Cos(t * 0.9f) * idlePosAmount * 0.6f;

        Vector3 idlePos = new Vector3(idleX, idleY, 0f) * stateMult;
        Vector3 idleRot = new Vector3(
            Mathf.Cos(t) * idleRotAmount,
            Mathf.Sin(t * 0.8f) * idleRotAmount,
            Mathf.Sin(t * 0.6f) * idleRotAmount * 0.5f
        ) * stateMult;

        // 2) Look-reactive sway (based on camera direction change)
        Vector2 lookDelta = Vector2.zero;
        if (cameraTransform != null)
        {
            // Approx “mouse delta” by tracking camera basis change frame to frame
            Vector3 fwd = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            float yawLike = Vector3.SignedAngle(lastCamForward, fwd, Vector3.up);
            float pitchLike = Vector3.SignedAngle(lastCamRight, right, cameraTransform.forward);

            // Scale down into a reasonable range
            lookDelta = new Vector2(yawLike, -pitchLike) * 0.2f;

            lastCamForward = fwd;
            lastCamRight = right;
        }

        lookDeltaSmoothed = Vector2.Lerp(lookDeltaSmoothed, lookDelta, 1f - Mathf.Exp(-lookSmooth * dt));

        Vector3 lookPos = new Vector3(
            -lookDeltaSmoothed.x * lookPosAmount,
            -lookDeltaSmoothed.y * lookPosAmount,
            0f
        ) * stateMult;

        Vector3 lookRot = new Vector3(
            -lookDeltaSmoothed.y * lookRotAmount,
            lookDeltaSmoothed.x * lookRotAmount,
            lookDeltaSmoothed.x * lookRotAmount * 0.35f
        ) * stateMult;

        // Combine targets
        Vector3 targetPos = baseLocalPos + idlePos + lookPos;
        Quaternion targetRot = baseLocalRot * Quaternion.Euler(idleRot + lookRot);

        // Smooth apply
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, 1f - Mathf.Exp(-posLerp * dt));
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, 1f - Mathf.Exp(-rotLerp * dt));
    }
}