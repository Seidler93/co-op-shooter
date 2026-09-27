using UnityEngine;

namespace CoopShooter.MovementRefactor
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GroundChecker groundChecker;
        [SerializeField] private Transform cameraRoot;

        private CharacterController characterController;
        private Vector3 planarVelocity;
        private float verticalVelocity;

        public PlayerLocomotionState LocomotionState { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public float PlanarSpeed => new Vector3(characterController.velocity.x, 0f, characterController.velocity.z).magnitude;
        public float NormalizedPlanarSpeed => config != null && config.sprintSpeed > 0f ? Mathf.Clamp01(PlanarSpeed / config.sprintSpeed) : 0f;

        public Vector2 LocalPlanarVelocityNormalized
        {
            get
            {
                if (config == null || config.sprintSpeed <= 0f)
                    return Vector2.zero;

                Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
                return Vector2.ClampMagnitude(new Vector2(localVelocity.x, localVelocity.z) / config.sprintSpeed, 1f);
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (!input)
                input = GetComponent<PlayerInputReader>();

            if (!groundChecker)
                groundChecker = GetComponent<GroundChecker>();
        }

        private void Update()
        {
            if (!config || !input)
                return;

            float dt = Time.deltaTime;
            IsGrounded = groundChecker != null ? groundChecker.CheckGround() : characterController.isGrounded;

            TickHorizontalMovement(dt);
            TickVerticalMovement(dt);
            TickRotation(dt);
            UpdateState();

            Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * dt);
        }

        private void TickHorizontalMovement(float dt)
        {
            Vector2 moveInput = Vector2.ClampMagnitude(input.MoveInput, 1f);
            Vector3 cameraForward = cameraRoot ? Vector3.ProjectOnPlane(cameraRoot.forward, Vector3.up).normalized : transform.forward;
            Vector3 cameraRight = cameraRoot ? Vector3.ProjectOnPlane(cameraRoot.right, Vector3.up).normalized : transform.right;
            Vector3 desiredDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

            if (desiredDirection.sqrMagnitude > 1f)
                desiredDirection.Normalize();

            IsSprinting = input.SprintHeld && !input.AimHeld && moveInput.y > 0.05f;
            float targetSpeed = input.AimHeld ? config.aimSpeed : IsSprinting ? config.sprintSpeed : config.runSpeed;
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? config.acceleration : config.deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * dt);
        }

        private void TickVerticalMovement(float dt)
        {
            if (IsGrounded && verticalVelocity < 0f)
                verticalVelocity = config.groundedStickVelocity;

            if (IsGrounded && input.JumpPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(config.jumpHeight * -2f * config.gravity);

            verticalVelocity += config.gravity * dt;
        }

        private void TickRotation(float dt)
        {
            if (planarVelocity.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, config.rotationSpeed * dt);
        }

        private void UpdateState()
        {
            if (!IsGrounded)
            {
                LocomotionState = PlayerLocomotionState.Airborne;
                return;
            }

            if (input.AimHeld)
                LocomotionState = PlayerLocomotionState.Aiming;
            else if (IsSprinting)
                LocomotionState = PlayerLocomotionState.Sprint;
            else if (PlanarSpeed > config.runSpeed * 0.65f)
                LocomotionState = PlayerLocomotionState.Run;
            else if (PlanarSpeed > 0.05f)
                LocomotionState = PlayerLocomotionState.Walk;
            else
                LocomotionState = PlayerLocomotionState.Idle;
        }
    }
}
