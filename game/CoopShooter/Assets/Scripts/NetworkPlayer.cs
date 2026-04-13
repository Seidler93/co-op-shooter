using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Camera targets (ON THIS PLAYER)")]
    [SerializeField] private Transform camPivot;
    [SerializeField] private Transform shoulderTarget;

    [Header("Scene camera (ONE in scene)")]
    [SerializeField] private CinemachineCamera sceneAimCam;

    [Header("Optional")]
    [SerializeField] private CameraController cameraController;

    [Header("Score")]
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> Kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (!cameraController)
            cameraController = GetComponentInChildren<CameraController>(true);

        if (!camPivot)
        {
            var t = transform.Find("CamPivot");
            if (t) camPivot = t;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
    
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    Debug.Log($"[Player Spawn] {name} | IsOwner={IsOwner} | IsServer={IsServer} | OwnerClientId={OwnerClientId}");

        StartCoroutine(ClaimSceneCameraWhenReady());
    }

    private IEnumerator ClaimSceneCameraWhenReady()
    {
        yield return null;
        yield return null;

        BindLocalHUD();

        if (!sceneAimCam)
            sceneAimCam = FindFirstObjectByType<CinemachineCamera>();

        Debug.Log($"[NetworkPlayer] Claim camera. camPivot={(camPivot ? "OK" : "NULL")} sceneAimCam={(sceneAimCam ? "FOUND" : "NULL")} cameraController={(cameraController ? "OK" : "NULL")}");


        if (!sceneAimCam || !camPivot)
            yield break;

        sceneAimCam.Follow = camPivot;
        sceneAimCam.LookAt = shoulderTarget ? shoulderTarget : camPivot;

        if (cameraController)
            cameraController.SetCinemachine(sceneAimCam);
    }

    private void BindLocalHUD()
    {
        if (!IsOwner) return;

        var ammo = GetComponentInChildren<WeaponAmmoNetcode>(true);
        var hud = FindFirstObjectByType<AmmoHUDNetcode>();

        if (ammo != null && hud != null)
        {
            hud.Bind(ammo);
        }
    }

    public void AddPoints(int amount)
    {
        if (!IsServer) return;

        Score.Value += amount;
        Debug.Log($"[SERVER] {name} gained {amount} points. New score = {Score.Value}");
    }

    public void AddKill()
    {
        if (!IsServer)
            return;

        Kills.Value += 1;
    }

    public bool TrySpendScore(int amount)
    {
        if (!IsServer)
            return false;

        if (Score.Value < amount)
            return false;

        Score.Value -= amount;
        return true;
    }

    public void RefundScore(int amount)
    {
        if (!IsServer)
            return;

        Score.Value += amount;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            Score.Value = 0;
            Kills.Value = 0;
        }
    }
}
