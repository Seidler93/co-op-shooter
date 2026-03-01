using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform camPivot;
    [SerializeField] private PlayerController playerController;

    [Header("Cinemachine (v3)")]
    [SerializeField] private CinemachineCamera cmCam;                // CM_AimCam
    [SerializeField] private CinemachineThirdPersonFollow thirdFollow; // Position Control: Third Person Follow

    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.08f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY = false;

    [Header("Aim Zoom")]
    [SerializeField] private float defaultFov = 65f;
    [SerializeField] private float aimFov = 50f;

    [SerializeField] private float defaultDistance = 4.5f;
    [SerializeField] private float aimDistance = 3.2f;

    [Tooltip("Higher = snappier (still smooth). 10–16 is a good range.")]
    [SerializeField] private float zoomSharpness = 12f;

    [Header("Optional Shoulder Offset")]
    [SerializeField] private bool adjustShoulderOffset = true;
    [SerializeField] private Vector3 defaultShoulderOffset = new Vector3(0.55f, 0.10f, 0f);
    [SerializeField] private Vector3 aimShoulderOffset = new Vector3(0.35f, 0.10f, 0f);

    private float pitch;

    private PlayerControls input;
    private InputAction lookAction;
    private InputAction aimAction;

    void Awake()
    {
        input = new PlayerControls();
        lookAction = input.Gameplay.Look;
        aimAction = input.Gameplay.Aim; // <-- bind RMB to this action in your Input Actions

        if (!playerController && player)
            playerController = player.GetComponent<PlayerController>();

        // Auto-wire Cinemachine if not assigned
        if (!cmCam) cmCam = FindFirstObjectByType<CinemachineCamera>();
        if (cmCam && !thirdFollow) thirdFollow = cmCam.GetComponent<CinemachineThirdPersonFollow>();
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (camPivot)
        {
            pitch = camPivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }

        // Initialize Cinemachine values to defaults (avoids first-frame pop)
        if (cmCam)
        {
            var lens = cmCam.Lens;
            lens.FieldOfView = defaultFov;
            cmCam.Lens = lens;
        }

        if (thirdFollow)
        {
            thirdFollow.CameraDistance = defaultDistance;
            if (adjustShoulderOffset)
                thirdFollow.ShoulderOffset = defaultShoulderOffset;
        }
    }

    void Update()
    {
        HandleLook();
        HandleAimZoom();
    }

    void HandleLook()
    {
        if (!player || !camPivot) return;

        Vector2 look = lookAction.ReadValue<Vector2>();
        float mx = look.x * sensitivity;
        float my = look.y * sensitivity;

        if (invertY) my = -my;

        // Yaw on player
        player.Rotate(0f, mx, 0f);

        // Pitch on pivot
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        camPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleAimZoom()
    {
        if (!cmCam || !thirdFollow) return;

        bool aiming = aimAction != null && aimAction.IsPressed();

        float targetFov = aiming ? aimFov : defaultFov;
        float targetDist = aiming ? aimDistance : defaultDistance;
        Vector3 targetShoulder = aiming ? aimShoulderOffset : defaultShoulderOffset;

        // Exponential smoothing (better feel than raw Lerp w/ speed)
        float t = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);

        // Smooth FOV
        var lens = cmCam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        cmCam.Lens = lens;

        // Smooth distance
        thirdFollow.CameraDistance = Mathf.Lerp(thirdFollow.CameraDistance, targetDist, t);

        // Optional shoulder offset
        if (adjustShoulderOffset)
        {
            thirdFollow.ShoulderOffset = Vector3.Lerp(thirdFollow.ShoulderOffset, targetShoulder, t);
        }
    }
}