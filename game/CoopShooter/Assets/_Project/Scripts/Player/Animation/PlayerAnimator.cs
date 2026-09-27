using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerRotation playerRotation;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private WeaponAimController weaponAimController;

    [Header("Grenade Throw")]
    [SerializeField] private Rig upperBodyRig;
    [SerializeField] private float grenadeRigDisableDuration = 0.9f;
    [SerializeField] private float grenadeActionDuration = 0.9f;
    [SerializeField] private float grenadeRigRestoreWeight = 1f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");
    private static readonly int AimYawHash = Animator.StringToHash("AimYaw");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int FireHash = Animator.StringToHash("Fire");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");
    private static readonly int GrenadeHash = Animator.StringToHash("Grenade");
    private const float MoveBlendDampTime = 0.12f;
    private const float SpeedBlendDampTime = 0.1f;
    private const float MoveBlendDeadzone = 0.05f;
    private bool wasReloading;
    private Coroutine grenadeRigRoutine;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!playerRotation) playerRotation = GetComponent<PlayerRotation>();
        if (!playerState) playerState = GetComponent<PlayerState>();
        if (!weaponAimController) weaponAimController = GetComponentInChildren<WeaponAimController>();
        if (!upperBodyRig) upperBodyRig = GetComponentInChildren<Rig>(true);
    }

    private void Update()
    {
        if (!animator) return;

        Vector2 moveBlend = playerMovement != null ? playerMovement.CurrentLocomotionBlend : Vector2.zero;
        if (moveBlend.sqrMagnitude < MoveBlendDeadzone * MoveBlendDeadzone)
            moveBlend = Vector2.zero;

        float speed = playerMovement != null && playerMovement.CurrentPlanarMaxSpeed > 0.001f
            ? Mathf.Clamp01(playerController.PlanarSpeed / playerMovement.CurrentPlanarMaxSpeed)
            : 0f;

        bool isReloading = playerState != null && playerState.IsReloading;

        animator.SetFloat(MoveXHash, moveBlend.x, MoveBlendDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveBlend.y, MoveBlendDampTime, Time.deltaTime);
        animator.SetFloat(SpeedHash, speed, SpeedBlendDampTime, Time.deltaTime);
        // Mixamo aim-offset clips in this setup expect the opposite pitch sign from the camera pitch.
        animator.SetFloat(AimPitchHash, playerRotation != null ? -playerRotation.NormalizedPitch : 0f, MoveBlendDampTime, Time.deltaTime);
        animator.SetFloat(AimYawHash, weaponAimController != null ? weaponAimController.NormalizedYaw : 0f, MoveBlendDampTime, Time.deltaTime);
        animator.SetBool(IsAimingHash, playerState != null && playerState.IsAiming);
        animator.SetBool(IsGroundedHash, playerState != null && playerState.IsGrounded);
        animator.SetBool(IsDeadHash, playerState != null && playerState.IsDead);

        if (isReloading && !wasReloading)
            animator.SetTrigger(ReloadHash);

        wasReloading = isReloading;
    }

    public void TriggerFire()
    {
        if (!animator) return;
        animator.SetTrigger(FireHash);
    }

    public void TriggerReload()
    {
        if (!animator) return;
        animator.SetTrigger(ReloadHash);
    }

    public void TriggerGrenade()
    {
        if (!animator) return;
        animator.SetTrigger(GrenadeHash);
        playerState?.SetThrowingGrenade(true);

        if (grenadeRigRoutine != null)
            StopCoroutine(grenadeRigRoutine);

        if (upperBodyRig != null && grenadeRigDisableDuration > 0f)
            grenadeRigRoutine = StartCoroutine(DisableUpperBodyRigForGrenade());
        else if (grenadeActionDuration > 0f)
            StartCoroutine(EndGrenadeActionAfterDelay());
    }

    private System.Collections.IEnumerator DisableUpperBodyRigForGrenade()
    {
        if (upperBodyRig == null)
            yield break;

        float previousWeight = upperBodyRig.weight;
        upperBodyRig.weight = 0f;

        yield return new WaitForSeconds(grenadeRigDisableDuration);

        if (upperBodyRig != null)
            upperBodyRig.weight = Mathf.Clamp01(grenadeRigRestoreWeight > 0f ? grenadeRigRestoreWeight : previousWeight);

        playerState?.SetThrowingGrenade(false);
        grenadeRigRoutine = null;
    }

    private System.Collections.IEnumerator EndGrenadeActionAfterDelay()
    {
        yield return new WaitForSeconds(grenadeActionDuration);
        playerState?.SetThrowingGrenade(false);
    }
}
