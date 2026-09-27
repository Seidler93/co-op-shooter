using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    public sealed class PlayerAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerInputReader input;

        [Header("Damping")]
        [SerializeField] private float moveDampTime = 0.12f;
        [SerializeField] private float speedDampTime = 0.1f;

        private void Awake()
        {
            if (!animator)
                animator = GetComponentInChildren<Animator>();

            if (!motor)
                motor = GetComponent<PlayerMotor>();

            if (!input)
                input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (!animator || !motor)
                return;

            Vector2 localMove = motor.LocalPlanarVelocityNormalized;
            float normalizedSpeed = motor.NormalizedPlanarSpeed;

            animator.SetFloat(AnimatorHashes.MoveX, localMove.x, moveDampTime, Time.deltaTime);
            animator.SetFloat(AnimatorHashes.MoveY, localMove.y, moveDampTime, Time.deltaTime);
            animator.SetFloat(AnimatorHashes.Speed, normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetFloat(AnimatorHashes.VerticalVelocity, motor.VerticalVelocity, speedDampTime, Time.deltaTime);
            animator.SetBool(AnimatorHashes.IsGrounded, motor.IsGrounded);
            animator.SetBool(AnimatorHashes.IsAiming, input != null && input.AimHeld);
            animator.SetBool(AnimatorHashes.IsSprinting, motor.IsSprinting);
        }
    }
}
