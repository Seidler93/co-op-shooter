using UnityEngine;

public class PlayerLook : PlayerRotation
{
    [Header("References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform gunPitchPivot;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerInputReader playerInputReader;

    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.08f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY = false;

    private float pitch;

    public override float PitchDegrees => pitch;
    public override float NormalizedPitch
    {
        get
        {
            if (pitch < 0f)
                return Mathf.Clamp(pitch / Mathf.Abs(minPitch), -1f, 0f);

            return Mathf.Clamp(pitch / Mathf.Max(maxPitch, 0.001f), 0f, 1f);
        }
    }

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

        if (!playerInputReader)
            playerInputReader = GetComponent<PlayerInputReader>();

        if (camPivot)
        {
            pitch = camPivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
    }

    private void Update()
    {
        if (playerInputReader == null)
            return;

        TickLook(playerInputReader.LookInput, Time.deltaTime);
    }

    public override void TickLook(Vector2 lookInput, float dt)
    {
        if (!playerRoot || !camPivot) return;

        if (playerState != null && !playerState.CanLook)
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
