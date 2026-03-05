using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float aimMoveSpeed = 4.5f;
    public float gravity = -18f;

    private CharacterController cc;
    private Vector3 verticalVel;

    // Input
    private PlayerControls input;
    private InputAction moveAction;

    public bool IsAiming { get; set; }

    // Used by WeaponShooter to drive movement crosshair spread
    public float PlanarSpeed => cc != null
        ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude
        : 0f;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        input = new PlayerControls();
        moveAction = input.Gameplay.Move;
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float speed = IsAiming ? aimMoveSpeed : moveSpeed;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float x = moveInput.x;
        float z = moveInput.y;

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f) move.Normalize();

        cc.Move(move * speed * Time.deltaTime);

        if (cc.isGrounded && verticalVel.y < 0)
            verticalVel.y = -2f;

        verticalVel.y += gravity * Time.deltaTime;
        cc.Move(verticalVel * Time.deltaTime);
    }
}