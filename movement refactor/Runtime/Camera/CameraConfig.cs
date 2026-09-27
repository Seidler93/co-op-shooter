using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    [CreateAssetMenu(menuName = "Coop Shooter/Movement Refactor/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Look")]
        public float mouseSensitivity = 0.12f;
        public float gamepadSensitivity = 120f;
        public float minPitch = -35f;
        public float maxPitch = 70f;

        [Header("Follow")]
        public Vector3 shoulderOffset = new Vector3(0.65f, 1.65f, -3.5f);
        public Vector3 aimShoulderOffset = new Vector3(0.45f, 1.6f, -2.2f);
        public float followSharpness = 18f;
        public float rotationSharpness = 24f;

        [Header("Collision")]
        public float collisionRadius = 0.25f;
        public LayerMask collisionMask = ~0;
    }
}
