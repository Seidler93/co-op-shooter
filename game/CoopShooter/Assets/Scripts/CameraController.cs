using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;          // Player root (yaw rotates here)
    [SerializeField] private Transform camPivot;        // Pitch pivot (child of player)
    [SerializeField] private PlayerController playerController;

    [Header("Cinemachine (v3)")]
    [SerializeField] private CinemachineCamera cmCam;                
    [SerializeField] private CinemachineThirdPersonFollow thirdFollow;

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

    private void Awake()
    {
        input = new PlayerControls();
        lookAction = input.Gameplay.Look;
        aimAction = input.Gameplay.Aim;

        if (!playerController)
            playerController = GetComponentInParent<PlayerController>(); // IMPORTANT: parent, not children

        if (!playerController)
            playerController = GetComponentInChildren<PlayerController>(true); // fallback

        // Player root = the object that actually moves (CharacterController)
        if (!player && playerController)
            player = playerController.transform;

        // Pivot should usually be under the player root
        if (!camPivot && playerController)
        {
            var t = playerController.transform.Find("CamPivot");
            if (t) camPivot = t;
        }
    }

    private void OnEnable()
    {
        input.Enable();
        Debug.Log("[CameraController] Enabled");
    }

    private void OnDisable()
    {
        input.Disable();
        Debug.Log("[CameraController] Disabled");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (camPivot)
        {
            pitch = camPivot.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;
        }

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

    private void Update()
    {
        HandleLook();
        HandleAimZoom();
    }

    private void HandleLook()
    {
        if (!player || !camPivot)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        float mx = look.x * sensitivity;
        float my = look.y * sensitivity;

        if (invertY)
            my = -my;

        // Yaw on player root
        player.Rotate(0f, mx, 0f);

        // Pitch on pivot
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        camPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleAimZoom()
    {
        if (!cmCam || !thirdFollow)
            return;

        bool aiming = aimAction != null && aimAction.IsPressed();

        float targetFov = aiming ? aimFov : defaultFov;
        float targetDist = aiming ? aimDistance : defaultDistance;
        Vector3 targetShoulder = aiming ? aimShoulderOffset : defaultShoulderOffset;

        float t = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);

        // Smooth FOV
        var lens = cmCam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        cmCam.Lens = lens;

        // Smooth distance
        thirdFollow.CameraDistance =
            Mathf.Lerp(thirdFollow.CameraDistance, targetDist, t);

        if (adjustShoulderOffset)
        {
            thirdFollow.ShoulderOffset =
                Vector3.Lerp(thirdFollow.ShoulderOffset, targetShoulder, t);
        }
    }
}