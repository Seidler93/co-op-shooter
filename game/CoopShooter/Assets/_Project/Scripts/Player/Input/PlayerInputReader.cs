using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input State")]
    [SerializeField] protected PlayerState playerState;

    protected PlayerControls input;
    protected InputAction moveAction;
    protected InputAction lookAction;
    protected InputAction aimAction;
    protected InputAction fireAction;
    protected InputAction reloadAction;
    protected InputAction interactAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool AimHeld { get; private set; }
    public bool FireHeld { get; private set; }
    public bool FirePressedThisFrame { get; private set; }
    public bool ReloadPressedThisFrame { get; private set; }
    public bool GrenadePressedThisFrame { get; private set; }
    public bool InteractHeld { get; private set; }
    public bool InteractPressedThisFrame { get; private set; }

    protected virtual void Awake()
    {
        input = new PlayerControls();
        moveAction = input.Gameplay.Move;
        lookAction = input.Gameplay.Look;
        aimAction = input.Gameplay.Aim;
        fireAction = input.Gameplay.Fire;
        reloadAction = input.Gameplay.Reload;
        interactAction = input.Gameplay.Interact;

        if (!playerState)
            playerState = GetComponent<PlayerState>();
    }

    protected virtual void OnEnable()
    {
        input?.Enable();
    }

    protected virtual void OnDisable()
    {
        input?.Disable();
        ClearFrameState();
    }

    protected virtual void Update()
    {
        RefreshIntent();
    }

    protected void RefreshIntent()
    {
        if (playerState != null && !playerState.HasGameplayControl)
        {
            ClearFrameState();
            return;
        }

        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        LookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        AimHeld = aimAction != null && aimAction.IsPressed();
        FireHeld = fireAction != null && fireAction.IsPressed();
        FirePressedThisFrame = fireAction != null && fireAction.WasPressedThisFrame();
        ReloadPressedThisFrame = reloadAction != null && reloadAction.WasPressedThisFrame();
        GrenadePressedThisFrame = Keyboard.current?.gKey?.wasPressedThisFrame ?? false;
        InteractHeld = interactAction != null && interactAction.IsPressed();
        InteractPressedThisFrame = interactAction != null && interactAction.WasPressedThisFrame();

        SprintHeld =
            Keyboard.current != null &&
            ((Keyboard.current.leftShiftKey?.isPressed ?? false) ||
             (Keyboard.current.rightShiftKey?.isPressed ?? false));
    }

    protected void ClearFrameState()
    {
        MoveInput = Vector2.zero;
        LookInput = Vector2.zero;
        SprintHeld = false;
        AimHeld = false;
        FireHeld = false;
        FirePressedThisFrame = false;
        ReloadPressedThisFrame = false;
        GrenadePressedThisFrame = false;
        InteractHeld = false;
        InteractPressedThisFrame = false;
    }
}
