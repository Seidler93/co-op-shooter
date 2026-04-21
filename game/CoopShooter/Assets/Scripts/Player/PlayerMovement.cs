using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float aimMoveSpeed = 4.5f;
    [SerializeField] private float gravity = -18f;

    [Header("Optional")]
    [SerializeField] private WeaponIdleSway weaponSway;
    [SerializeField] private PlayerState playerState;

    private CharacterController cc;
    private Vector3 verticalVel;
    private Vector2 currentMoveInput;

    public Vector2 CurrentMoveInput => currentMoveInput;

    public float PlanarSpeed => cc != null
        ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude
        : 0f;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (!playerState)
            playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        TickMovement(Time.deltaTime);
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        if (playerState != null && (playerState.IsDead || playerState.IsDowned || playerState.IsInputBlocked))
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

    private void TickMovement(float dt)
    {
        if (playerState != null && (playerState.IsDead || playerState.IsDowned || playerState.IsInputBlocked))
        {
            currentMoveInput = Vector2.zero;
            verticalVel = Vector3.zero;

            if (playerState != null)
            {
                playerState.SetMoving(false);
                playerState.SetGrounded(cc.isGrounded);
            }

            return;
        }

        bool isAiming = playerState != null && playerState.IsAiming;
        float speed = isAiming ? aimMoveSpeed : moveSpeed;

        Vector3 move = transform.right * currentMoveInput.x + transform.forward * currentMoveInput.y;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        cc.Move(move * speed * dt);

        if (cc.isGrounded && verticalVel.y < 0f)
            verticalVel.y = -2f;

        verticalVel.y += gravity * dt;
        cc.Move(verticalVel * dt);

        if (playerState != null)
        {
            playerState.SetMoving(currentMoveInput.sqrMagnitude > 0.001f);
            playerState.SetGrounded(cc.isGrounded);
        }
    }
}
