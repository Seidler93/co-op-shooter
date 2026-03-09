using UnityEngine;

public class PlayerRefs : MonoBehaviour
{
    [Header("Core Components")]
    public CharacterController characterController;
    public PlayerController playerController;
    public PlayerMovement playerMovement;
    public PlayerLook playerLook;
    public PlayerInputRouter playerInputRouter;
    public PlayerHealth playerHealth;
    public PlayerAnimator playerAnimator;
    public PlayerNetwork playerNetwork;
    public PlayerState playerState;

    [Header("Transforms")]
    public Transform playerRoot;
    public Transform camPivot;
    public Transform gunPitchPivot;

    [Header("Optional")]
    public CameraController cameraController;
    public WeaponIdleSway weaponIdleSway;

    private void Awake()
    {
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!playerLook) playerLook = GetComponent<PlayerLook>();
        if (!playerInputRouter) playerInputRouter = GetComponent<PlayerInputRouter>();
        if (!playerHealth) playerHealth = GetComponent<PlayerHealth>();
        if (!playerAnimator) playerAnimator = GetComponent<PlayerAnimator>();
        if (!playerNetwork) playerNetwork = GetComponent<PlayerNetwork>();
        if (!playerState) playerState = GetComponent<PlayerState>();

        if (!cameraController) cameraController = GetComponent<CameraController>();
        if (!weaponIdleSway) weaponIdleSway = GetComponentInChildren<WeaponIdleSway>();

        if (!playerRoot) playerRoot = transform;

        if (!camPivot)
        {
            Transform t = transform.Find("CamPivot");
            if (t) camPivot = t;
        }

        if (!gunPitchPivot)
        {
            Transform t = transform.Find("GunPitchPivot");
            if (t) gunPitchPivot = t;
        }
    }
}