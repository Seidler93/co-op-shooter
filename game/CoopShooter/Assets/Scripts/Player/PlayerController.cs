using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Core References")]
    public PlayerMovement Movement { get; private set; }
    public PlayerLook Look { get; private set; }
    public PlayerHealth Health { get; private set; }
    public PlayerState State { get; private set; }
    public PlayerRefs Refs { get; private set; }

    public float PlanarSpeed => Movement != null ? Movement.PlanarSpeed : 0f;

    [Header("Collision")]
    [SerializeField] private bool disableChildVisualColliders = true;

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Look = GetComponent<PlayerLook>();
        Health = GetComponent<PlayerHealth>();
        State = GetComponent<PlayerState>();
        Refs = GetComponent<PlayerRefs>();

        if (disableChildVisualColliders)
            DisableChildVisualColliders();
    }

    public void SetGameplayInputBlocked(bool blocked)
    {
        if (State == null) return;

        State.SetInputBlocked(blocked);

        if (blocked)
        {
            State.SetMoving(false);
            State.SetAiming(false);
            State.SetFiring(false);
            State.SetReloading(false);
        }
    }

    private void DisableChildVisualColliders()
    {
        Collider[] childColliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < childColliders.Length; i++)
        {
            Collider collider = childColliders[i];
            if (collider == null)
                continue;

            if (collider.transform == transform)
                continue;

            collider.enabled = false;
        }
    }
}
