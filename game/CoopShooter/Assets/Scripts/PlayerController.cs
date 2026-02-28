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

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        input = new PlayerControls();
        moveAction = input.Gameplay.Move;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
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