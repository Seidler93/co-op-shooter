using UnityEngine;

public class PlayerInputRouter : PlayerInputReader
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private WeaponAmmoNetcode weaponAmmo;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private PlayerGrenadeController playerGrenadeController;

    protected override void Awake()
    {
        base.Awake();

        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!cameraController) cameraController = GetComponent<CameraController>();
        if (!playerState) playerState = GetComponent<PlayerState>();
        if (!weaponShooter) weaponShooter = GetComponentInChildren<WeaponShooter>();
        if (!weaponAmmo) weaponAmmo = GetComponentInChildren<WeaponAmmoNetcode>();
        if (!playerAnimator) playerAnimator = GetComponent<PlayerAnimator>();
        if (!playerGrenadeController) playerGrenadeController = GetComponent<PlayerGrenadeController>();
    }

    protected override void Update()
    {
        base.Update();

        if (playerState != null && !playerState.HasGameplayControl)
        {
            playerMovement?.SetMoveInput(Vector2.zero);
            playerMovement?.SetSprintInput(false);
            cameraController?.SetAiming(false);
            weaponShooter?.SetFireInput(false, false);
            return;
        }

        playerMovement?.SetMoveInput(MoveInput);
        playerMovement?.SetSprintInput(SprintHeld);

        bool isAiming = playerState == null || playerState.CanAim ? AimHeld : false;
        playerState?.SetAiming(isAiming);
        cameraController?.SetAiming(isAiming);

        bool fireHeld = playerState == null || playerState.CanUseWeapons ? FireHeld : false;
        bool firePressedThisFrame = playerState == null || playerState.CanUseWeapons ? FirePressedThisFrame : false;
        weaponShooter?.SetFireInput(fireHeld, firePressedThisFrame);

        if ((playerState == null || playerState.CanReload) &&
            ReloadPressedThisFrame)
        {
            weaponAmmo?.RequestReloadServerRpc();
        }

        if ((playerState == null || playerState.CanUseWeapons) &&
            GrenadePressedThisFrame)
        {
            playerGrenadeController?.TryThrowGrenade();
        }
    }
}
