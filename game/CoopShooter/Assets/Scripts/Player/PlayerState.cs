using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("State")]
    public bool IsAiming { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsFiring { get; private set; }
    public bool IsReloading { get; private set; }

    // NEW
    public bool IsInputBlocked { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

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
        LogStateChange(nameof(IsReloading), value);
    }

    // NEW
    public void SetInputBlocked(bool value)
    {
        if (IsInputBlocked == value) return;
        IsInputBlocked = value;
        LogStateChange(nameof(IsInputBlocked), value);
    }

    private void LogStateChange(string stateName, bool value)
    {
        if (!logStateChanges) return;
        Debug.Log($"[{name}] {stateName} -> {value}");
    }
}