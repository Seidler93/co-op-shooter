# Third Person Movement Refactor Prototype

> Status: reference prototype. Do not copy this folder into `Assets` as a complete
> replacement for the live player stack. The live project already owns input,
> Cinemachine setup, combat state, animation triggers, and network ownership.
>
> The prototype's acceleration/deceleration approach was integrated into
> `Assets/_Project/Scripts/Player/Controllers/PlayerMovement.cs` on September 27,
> 2026. Future movement work should continue in the live player stack.

This folder is a clean starting point for rebuilding character movement, camera, and animation logic.

It remains isolated from the Unity project so its duplicate input, camera, motor,
and animation types cannot interfere with the working player prefab.

## Suggested Player Setup

- Root player object:
  - `CharacterController`
  - `PlayerInputReader`
  - `PlayerMotor`
  - `GroundChecker`
  - `PlayerAnimatorController`
- Camera rig object:
  - `ThirdPersonCameraController`
- Project assets:
  - `PlayerMovementConfig`
  - `CameraConfig`

## Animator Parameters

- `MoveX` float
- `MoveY` float
- `Speed` float
- `VerticalVelocity` float
- `IsGrounded` bool
- `IsAiming` bool
- `IsSprinting` bool
