using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform gunPitchPivot;
    [SerializeField] private PlayerState playerState;

    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.08f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY = false;

    private float pitch;

    private void Awake()
    {
        if (!playerRoot)
            playerRoot = transform;

        if (!camPivot)
        {
            Transform t = transform.Find("CamPivot");
            if (t) camPivot = t;
        }

        if (!gunPitchPivot)
        {
            Transform t = transform.Find("GunPitchPivot");
            if (t) gunPitchPivot = t;
        }

        if (!playerState)
            playerState = GetComponent<PlayerState>();

        if (camPivot)
        {
            pitch = camPivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
    }

    public void TickLook(Vector2 lookInput, float dt)
    {
        if (!playerRoot || !camPivot) return;

        if (playerState != null && (playerState.IsDead || playerState.IsDowned || playerState.IsInputBlocked))
            return;

        float mx = lookInput.x * sensitivity;
        float my = lookInput.y * sensitivity;

        if (invertY)
            my = -my;

        playerRoot.Rotate(0f, mx, 0f);

        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion pitchRot = Quaternion.Euler(pitch, 0f, 0f);
        camPivot.localRotation = pitchRot;

        if (gunPitchPivot)
            gunPitchPivot.localRotation = pitchRot;
    }
}
