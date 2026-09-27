using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("State")]
    public bool IsAiming { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsDowned { get; private set; }
    public bool IsFiring { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsThrowingGrenade { get; private set; }

    // NEW
    public bool IsInputBlocked { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    public bool HasGameplayControl => !IsDead && !IsDowned && !IsInputBlocked;
    public bool CanMove => HasGameplayControl;
    public bool CanLook => HasGameplayControl;
    public bool CanAim => HasGameplayControl && !IsThrowingGrenade;
    public bool CanUseWeapons => HasGameplayControl && !IsThrowingGrenade;
    public bool CanFire => CanUseWeapons && !IsReloading;
    public bool CanReload => CanUseWeapons && !IsReloading;
    public bool CanInteract => HasGameplayControl;

    public void SetAiming(bool value)
    {
        if (IsAiming == value) return;
        IsAiming = value;
        LogStateChange(nameof(IsAiming), value);
    }

    public void SetMoving(bool value)
    {
        if (IsMoving == value) return;
        IsMoving = value;
        LogStateChange(nameof(IsMoving), value);
    }

    public void SetGrounded(bool value)
    {
        if (IsGrounded == value) return;
        IsGrounded = value;
        LogStateChange(nameof(IsGrounded), value);
    }

    public void SetDead(bool value)
    {
        if (IsDead == value) return;
        IsDead = value;
        LogStateChange(nameof(IsDead), value);
    }

    public void SetDowned(bool value)
    {
        if (IsDowned == value) return;
        IsDowned = value;
        LogStateChange(nameof(IsDowned), value);
    }

    public void SetFiring(bool value)
    {
        if (IsFiring == value) return;
        IsFiring = value;
        LogStateChange(nameof(IsFiring), value);
    }

    public void SetReloading(bool value)
    {
        if (IsReloading == value) return;
        IsReloading = value;

        if (value)
            SetFiring(false);

        LogStateChange(nameof(IsReloading), value);
    }

    public void SetThrowingGrenade(bool value)
    {
        if (IsThrowingGrenade == value) return;
        IsThrowingGrenade = value;

        if (value)
        {
            SetAiming(false);
            SetFiring(false);
        }

        LogStateChange(nameof(IsThrowingGrenade), value);
    }

    public void SetInputBlocked(bool value)
    {
        if (IsInputBlocked == value) return;
        IsInputBlocked = value;

        if (value)
            ClearGameplayActions(includeMovement: true, includeReloading: false);

        LogStateChange(nameof(IsInputBlocked), value);
    }

    public void ClearGameplayActions(bool includeMovement, bool includeReloading)
    {
        if (includeMovement)
            SetMoving(false);

        SetAiming(false);
        SetFiring(false);
        SetThrowingGrenade(false);

        if (includeReloading)
            SetReloading(false);
    }

    private void LogStateChange(string stateName, bool value)
    {
        if (!logStateChanges) return;
        Debug.Log($"[{name}] {stateName} -> {value}");
    }
}
