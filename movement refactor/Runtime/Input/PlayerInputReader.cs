using UnityEngine;
using UnityEngine.InputSystem;

namespace CoopShooter.MovementRefactor
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference aimAction;
        [SerializeField] private InputActionReference jumpAction;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AimHeld { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }

        private void OnEnable()
        {
            Enable(moveAction);
            Enable(lookAction);
            Enable(sprintAction);
            Enable(aimAction);
            Enable(jumpAction);
        }

        private void OnDisable()
        {
            Disable(moveAction);
            Disable(lookAction);
            Disable(sprintAction);
            Disable(aimAction);
            Disable(jumpAction);
            Clear();
        }

        private void Update()
        {
            MoveInput = ReadVector2(moveAction);
            LookInput = ReadVector2(lookAction);
            SprintHeld = IsPressed(sprintAction);
            AimHeld = IsPressed(aimAction);
            JumpPressedThisFrame = jumpAction != null && jumpAction.action != null && jumpAction.action.WasPressedThisFrame();
        }

        private void Clear()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            SprintHeld = false;
            AimHeld = false;
            JumpPressedThisFrame = false;
        }

        private static Vector2 ReadVector2(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null
                ? actionReference.action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private static bool IsPressed(InputActionReference actionReference)
        {
            return actionReference != null && actionReference.action != null && actionReference.action.IsPressed();
        }

        private static void Enable(InputActionReference actionReference)
        {
            actionReference?.action?.Enable();
        }

        private static void Disable(InputActionReference actionReference)
        {
            actionReference?.action?.Disable();
        }
    }
}
