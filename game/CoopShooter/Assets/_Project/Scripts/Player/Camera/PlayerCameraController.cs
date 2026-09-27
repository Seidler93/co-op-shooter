using Unity.Cinemachine;
using UnityEngine;

public abstract class PlayerCameraController : MonoBehaviour
{
    public abstract bool IsAiming { get; }
    public abstract void SetCinemachine(CinemachineCamera cam);
    public abstract void SetAiming(bool aiming);
}
