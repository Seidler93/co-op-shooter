using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    public static class AnimatorHashes
    {
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveY = Animator.StringToHash("MoveY");
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int IsAiming = Animator.StringToHash("IsAiming");
        public static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    }
}
