using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputRouter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private WeaponAmmoNetcode weaponAmmo;

    private PlayerControls input;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction aimAction;
    private InputAction fireAction;
    private InputAction reloadAction;

    private void Awake()
    {
        input = new PlayerControls();

        moveAction = input.Gameplay.Move;
        lookAction = input.Gameplay.Look;
        aimAction = input.Gameplay.Aim;
        fireAction = input.Gameplay.Fire;
        reloadAction = input.Gameplay.Reload;

        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!playerLook) playerLook = GetComponent<PlayerLook>();
        if (!cameraController) cameraController = GetComponent<CameraController>();
        if (!playerState) playerState = GetComponent<PlayerState>();
        if (!weaponShooter) weaponShooter = GetComponentInChildren<WeaponShooter>();
        if (!weaponAmmo) weaponAmmo = GetComponentInChildren<WeaponAmmoNetcode>();
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        playerMovement?.SetMoveInput(moveInput);

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        playerLook?.TickLook(lookInput, Time.deltaTime);

        bool isAiming = aimAction != null && aimAction.IsPressed();
        playerState?.SetAiming(isAiming);
        cameraController?.SetAiming(isAiming);

        bool fireHeld = fireAction != null && fireAction.IsPressed();
        bool firePressedThisFrame = fireAction != null && fireAction.WasPressedThisFrame();
        weaponShooter?.SetFireInput(fireHeld, firePressedThisFrame);

        if (reloadAction != null && reloadAction.WasPressedThisFrame())
        {
            weaponAmmo?.RequestReloadServerRpc();
        }
    }
}