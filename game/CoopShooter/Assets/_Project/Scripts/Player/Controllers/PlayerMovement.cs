using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMoveSpeed = 9f;
    [SerializeField] private float adsMoveSpeedMultiplier = 0.6f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 45f;
    [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.35f;
    [SerializeField] private float gravity = -18f;

    [Header("Optional")]
    [SerializeField] private WeaponIdleSway weaponSway;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerInputReader playerInputReader;

    private CharacterController cc;
    private Vector3 planarVelocity;
    private Vector3 verticalVel;
    private Vector2 currentMoveInput;
    private float currentPlanarMaxSpeed;
    private bool sprintHeld;

    public Vector2 CurrentMoveInput => currentMoveInput;
    public float CurrentPlanarMaxSpeed => currentPlanarMaxSpeed;
    public Vector2 CurrentLocomotionBlend
    {
        get
        {
            if (currentMoveInput.sqrMagnitude <= 0.0001f)
                return Vector2.zero;

            bool isAiming = playerState != null && playerState.IsAiming;
            bool isSprinting = sprintHeld && !isAiming && currentMoveInput.y > 0.05f;
            float gaitScale = isAiming ? 1f : (isSprinting ? 1f : 0.5f);

            Vector2 normalizedInput = currentMoveInput;
            if (normalizedInput.sqrMagnitude > 1f)
                normalizedInput.Normalize();

            return normalizedInput * gaitScale;
        }
    }

    public Vector2 LocalPlanarVelocityNormalized
    {
        get
        {
            if (cc == null || currentPlanarMaxSpeed <= 0.001f)
                return Vector2.zero;

            Vector3 planarVelocity = cc.velocity;
            planarVelocity.y = 0f;

            Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
            Vector2 normalized = new Vector2(localVelocity.x, localVelocity.z) / currentPlanarMaxSpeed;
            return Vector2.ClampMagnitude(normalized, 1f);
        }
    }

    public float PlanarSpeed => cc != null
        ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude
        : 0f;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (!playerState)
            playerState = GetComponent<PlayerState>();

        if (!playerInputReader)
            playerInputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (playerInputReader != null)
        {
            SetMoveInput(playerInputReader.MoveInput);
            SetSprintInput(playerInputReader.SprintHeld);
        }

        TickMovement(Time.deltaTime);
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        if (playerState != null && !playerState.CanMove)
        {
            currentMoveInput = Vector2.zero;

            if (weaponSway != null)
            {
                weaponSway.moveInput = Vector2.zero;
                weaponSway.isAiming = false;
            }

            return;
        }

        currentMoveInput = moveInput;

        bool isAiming = playerState != null && playerState.IsAiming;

        if (weaponSway != null)
        {
            weaponSway.moveInput = moveInput;
            weaponSway.isAiming = isAiming;
        }
    }

    public void SetSprintInput(bool isHeld)
    {
        if (playerState != null && !playerState.CanMove)
        {
            sprintHeld = false;
            return;
        }

        sprintHeld = isHeld;
    }

    private void TickMovement(float dt)
    {
        if (playerState != null && !playerState.CanMove)
        {
            currentMoveInput = Vector2.zero;
            sprintHeld = false;
            planarVelocity = Vector3.zero;
            verticalVel = Vector3.zero;
            currentPlanarMaxSpeed = 0f;

            if (playerState != null)
            {
                playerState.SetMoving(false);
                playerState.SetGrounded(cc.isGrounded);
            }

            return;
        }

        bool isAiming = playerState != null && playerState.IsAiming;
        bool isSprinting = sprintHeld && !isAiming && currentMoveInput.y > 0.05f;
        float baseSpeed = isSprinting ? sprintMoveSpeed : moveSpeed;
        float speed = isAiming
            ? baseSpeed * adsMoveSpeedMultiplier
            : baseSpeed;
        currentPlanarMaxSpeed = speed;

        Vector3 desiredDirection = transform.right * currentMoveInput.x + transform.forward * currentMoveInput.y;
        if (desiredDirection.sqrMagnitude > 1f)
            desiredDirection.Normalize();

        Vector3 targetPlanarVelocity = desiredDirection * speed;
        bool isAccelerating = targetPlanarVelocity.sqrMagnitude > planarVelocity.sqrMagnitude;
        float responsiveness = isAccelerating ? acceleration : deceleration;
        if (!cc.isGrounded)
            responsiveness *= airControlMultiplier;

        planarVelocity = Vector3.MoveTowards(
            planarVelocity,
            targetPlanarVelocity,
            Mathf.Max(0f, responsiveness) * dt);

        if (cc.isGrounded && verticalVel.y < 0f)
            verticalVel.y = -2f;

        verticalVel.y += gravity * dt;
        cc.Move((planarVelocity + verticalVel) * dt);

        if (playerState != null)
        {
            playerState.SetMoving(currentMoveInput.sqrMagnitude > 0.001f);
            playerState.SetGrounded(cc.isGrounded);
        }
    }
}
