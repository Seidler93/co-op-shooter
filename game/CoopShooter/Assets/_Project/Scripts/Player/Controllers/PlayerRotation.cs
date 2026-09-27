using UnityEngine;

public abstract class PlayerRotation : MonoBehaviour
{
    public abstract float PitchDegrees { get; }
    public abstract float NormalizedPitch { get; }
    public abstract void TickLook(Vector2 lookInput, float dt);
}
