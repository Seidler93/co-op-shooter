using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraTransform;

        private float yaw;
        private float pitch;
        private Vector3 currentPosition;

        public Quaternion CameraYawRotation => Quaternion.Euler(0f, yaw, 0f);

        private void Awake()
        {
            if (!cameraTransform && Camera.main)
                cameraTransform = Camera.main.transform;

            if (followTarget)
                currentPosition = followTarget.position;
        }

        private void LateUpdate()
        {
            if (!config || !followTarget || !cameraTransform || !input)
                return;

            Vector2 look = input.LookInput;
            yaw += look.x * config.mouseSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * config.mouseSensitivity, config.minPitch, config.maxPitch);

            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredOffset = input.AimHeld ? config.aimShoulderOffset : config.shoulderOffset;
            Vector3 targetPivot = followTarget.position;
            Vector3 desiredCameraPosition = targetPivot + targetRotation * desiredOffset;

            desiredCameraPosition = ResolveCollision(targetPivot, desiredCameraPosition);

            float followT = 1f - Mathf.Exp(-config.followSharpness * Time.deltaTime);
            float rotationT = 1f - Mathf.Exp(-config.rotationSharpness * Time.deltaTime);

            currentPosition = Vector3.Lerp(currentPosition, desiredCameraPosition, followT);
            cameraTransform.position = currentPosition;
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationT);
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 toCamera = desiredPosition - pivot;
            float distance = toCamera.magnitude;

            if (distance <= 0.001f)
                return desiredPosition;

            Vector3 direction = toCamera / distance;
            if (Physics.SphereCast(pivot, config.collisionRadius, direction, out RaycastHit hit, distance, config.collisionMask, QueryTriggerInteraction.Ignore))
                return pivot + direction * Mathf.Max(hit.distance - config.collisionRadius, 0f);

            return desiredPosition;
        }
    }
}
