using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Player refs")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraController cameraController;

    [Header("Camera targets (ON THIS PLAYER)")]
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform shoulderTarget;

    [Header("Scene camera (ONE in scene)")]
    [SerializeField] private CinemachineCamera sceneAimCam;

    private void Awake()
    {
        // Works even if your scripts live on children
        if (!playerController) playerController = GetComponentInChildren<PlayerController>(true);
        if (!cameraController) cameraController = GetComponentInChildren<CameraController>(true);

        // If you named it CamPivot, this grabs it automatically
        if (!camPivot)
        {
            var t = transform.Find("CamPivot");
            if (t) camPivot = t;
        }
    }

    public override void OnNetworkSpawn()
    {
        ApplyOwnership(IsOwner);
    }

    private void ApplyOwnership(bool isOwner)
    {
        if (playerController) playerController.enabled = isOwner;
        if (cameraController) cameraController.enabled = isOwner;

        if (!isOwner) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(ClaimSceneCameraWhenReady());
    }

    private IEnumerator ClaimSceneCameraWhenReady()
    {
        // Wait a couple frames so: scene loaded + Cinemachine exists + player fully spawned
        yield return null;
        yield return null;

        if (!sceneAimCam)
            sceneAimCam = FindFirstObjectByType<CinemachineCamera>();

        Debug.Log($"[NetworkPlayer] Claim camera. camPivot={(camPivot ? "OK" : "NULL")} sceneAimCam={(sceneAimCam ? "FOUND" : "NULL")}");

        if (!sceneAimCam || !camPivot)
            yield break;

        sceneAimCam.Follow = camPivot;
        sceneAimCam.LookAt = shoulderTarget ? shoulderTarget : camPivot;
    }
}