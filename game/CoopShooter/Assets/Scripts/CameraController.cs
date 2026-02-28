using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform camPivot;
    [SerializeField] private PlayerController playerController;

    [Header("Look Settings")]
    [SerializeField] private float sensitivity = 0.08f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool invertY = false;

    private float pitch;

    private PlayerControls input;
    private InputAction lookAction;

    void Awake()
    {
        input = new PlayerControls();
        lookAction = input.Gameplay.Look;

        if (!playerController && player)
            playerController = player.GetComponent<PlayerController>();
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
    }

    void Update()
    {
        HandleLook();
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
}