using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    [CreateAssetMenu(menuName = "Coop Shooter/Movement Refactor/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Speed")]
        public float walkSpeed = 3f;
        public float runSpeed = 6f;
        public float sprintSpeed = 9f;
        public float aimSpeed = 3.5f;

        [Header("Responsiveness")]
        public float acceleration = 35f;
        public float deceleration = 45f;
        public float rotationSpeed = 720f;

        [Header("Vertical Movement")]
        public float gravity = -24f;
        public float groundedStickVelocity = -2f;
        public float jumpHeight = 1.2f;
    }
}
