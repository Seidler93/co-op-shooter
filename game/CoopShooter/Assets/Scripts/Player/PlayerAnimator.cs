using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerState playerState;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int FireHash = Animator.StringToHash("Fire");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");
    private const float MoveBlendDampTime = 0.12f;
    private const float SpeedBlendDampTime = 0.1f;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!playerState) playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        if (!animator) return;

        float speed = playerController != null ? playerController.PlanarSpeed : 0f;
        Vector2 moveInput = playerMovement != null ? playerMovement.CurrentMoveInput : Vector2.zero;

        animator.SetFloat(MoveXHash, moveInput.x, MoveBlendDampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveInput.y, MoveBlendDampTime, Time.deltaTime);
        animator.SetFloat(SpeedHash, speed, SpeedBlendDampTime, Time.deltaTime);
        animator.SetBool(IsAimingHash, playerState != null && playerState.IsAiming);
        animator.SetBool(IsGroundedHash, playerState != null && playerState.IsGrounded);
        animator.SetBool(IsDeadHash, playerState != null && playerState.IsDead);
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
}
