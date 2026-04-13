using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine (v3) - injected by NetworkPlayer")]
    [SerializeField] private CinemachineCamera cmCam;
    [SerializeField] private CinemachineThirdPersonFollow thirdFollow;

    [Header("Aim Zoom")]
    [SerializeField] private float defaultFov = 65f;
    [SerializeField] private float aimFov = 50f;
    [SerializeField] private float defaultDistance = 4.5f;
    [SerializeField] private float aimDistance = 3.2f;
    [SerializeField] private float zoomSharpness = 12f;

    [Header("Optional Shoulder Offset")]
    [SerializeField] private bool adjustShoulderOffset = true;
    [SerializeField] private Vector3 defaultShoulderOffset = new Vector3(0.55f, 0.10f, 0f);
    [SerializeField] private Vector3 aimShoulderOffset = new Vector3(0.35f, 0.10f, 0f);

    [Header("Obstruction")]
    [SerializeField] private bool avoidCameraObstruction = true;
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private float obstructionRadius = 0.2f;
    [SerializeField] private float obstructionBuffer = 0.12f;
    [SerializeField] private float minObstructedDistance = 0.75f;
    [SerializeField] private float obstructionSharpness = 18f;

    public bool IsAiming { get; private set; }

    private Camera runtimeCamera;

    public void SetCinemachine(CinemachineCamera cam)
    {
        cmCam = cam;
        thirdFollow = cmCam ? cmCam.GetComponent<CinemachineThirdPersonFollow>() : null;
        ApplyDefaultInstant();
    }

    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ApplyDefaultInstant();
    }

    private void Update()
    {
        if (!cmCam || !thirdFollow) return;

        if (runtimeCamera == null)
            runtimeCamera = Camera.main;

        float targetFov = IsAiming ? aimFov : defaultFov;
        float baseTargetDistance = IsAiming ? aimDistance : defaultDistance;
        Vector3 targetShoulder = IsAiming ? aimShoulderOffset : defaultShoulderOffset;

        float t = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);

        var lens = cmCam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        cmCam.Lens = lens;

        float targetDist = ResolveCameraDistance(baseTargetDistance);
        thirdFollow.CameraDistance = Mathf.Lerp(thirdFollow.CameraDistance, targetDist, t);

        if (adjustShoulderOffset)
            thirdFollow.ShoulderOffset = Vector3.Lerp(thirdFollow.ShoulderOffset, targetShoulder, t);
    }

    private void ApplyDefaultInstant()
    {
        if (cmCam)
        {
            var lens = cmCam.Lens;
            lens.FieldOfView = defaultFov;
            cmCam.Lens = lens;
        }

        if (thirdFollow)
        {
            thirdFollow.CameraDistance = defaultDistance;

            if (adjustShoulderOffset)
                thirdFollow.ShoulderOffset = defaultShoulderOffset;
        }
    }

    private float ResolveCameraDistance(float desiredDistance)
    {
        if (!avoidCameraObstruction || runtimeCamera == null || cmCam == null || cmCam.Follow == null)
            return desiredDistance;

        Vector3 pivot = cmCam.Follow.position;
        Vector3 currentCameraPosition = runtimeCamera.transform.position;
        Vector3 toCamera = currentCameraPosition - pivot;

        if (toCamera.sqrMagnitude < 0.0001f)
            return desiredDistance;

        Vector3 direction = toCamera.normalized;
        float castDistance = Mathf.Max(desiredDistance, minObstructedDistance);

        if (Physics.SphereCast(
            pivot,
            obstructionRadius,
            direction,
            out RaycastHit hit,
            castDistance,
            obstructionMask,
            QueryTriggerInteraction.Ignore))
        {
            float clampedDistance = Mathf.Clamp(hit.distance - obstructionBuffer, minObstructedDistance, desiredDistance);
            float obstructionT = 1f - Mathf.Exp(-obstructionSharpness * Time.deltaTime);
            return Mathf.Lerp(thirdFollow.CameraDistance, clampedDistance, obstructionT);
        }

        return desiredDistance;
    }
}
