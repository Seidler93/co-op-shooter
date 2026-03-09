using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WeaponAmmoNetcode : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int reserveMax = 120;
    [SerializeField] private float reloadTime = 1.6f;

    [Header("Start Ammo")]
    [SerializeField] private int startMag = 30;
    [SerializeField] private int startReserve = 90;

    [Header("Optional")]
    [SerializeField] private PlayerState playerState;

    public NetworkVariable<int> MagAmmo = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> ReserveAmmo = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsReloading = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine reloadCo;

    public int MagSize => magSize;
    public float ReloadTime => reloadTime;
    public bool HasAmmoInMag => MagAmmo.Value > 0;
    public bool HasReserveAmmo => ReserveAmmo.Value > 0;
    public bool CanReload => !IsReloading.Value && MagAmmo.Value < magSize && ReserveAmmo.Value > 0;

    private void Awake()
    {
        if (!playerState)
            playerState = GetComponentInParent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        IsReloading.OnValueChanged += OnReloadingChanged;

        if (IsServer)
        {
            MagAmmo.Value = Mathf.Clamp(startMag, 0, magSize);
            ReserveAmmo.Value = Mathf.Clamp(startReserve, 0, reserveMax);
            IsReloading.Value = false;
        }

        playerState?.SetReloading(IsReloading.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsReloading.OnValueChanged -= OnReloadingChanged;
    }

    private void OnReloadingChanged(bool oldValue, bool newValue)
    {
        playerState?.SetReloading(newValue);
    }

    public bool ServerCanShoot()
    {
        return !IsReloading.Value && MagAmmo.Value > 0;
    }

    public void ServerConsumeOne()
    {
        if (!IsServer) return;
        if (IsReloading.Value) return;
        if (MagAmmo.Value <= 0) return;

        MagAmmo.Value -= 1;
    }

    public void ServerTryReloadIfEmpty()
    {
        if (!IsServer) return;

        if (MagAmmo.Value == 0)
            ServerTryReload();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestReloadServerRpc()
    {
        ServerTryReload();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestReloadIfEmptyServerRpc()
    {
        if (MagAmmo.Value == 0)
            ServerTryReload();
    }

    private void ServerTryReload()
    {
        if (!IsServer) return;
        if (IsReloading.Value) return;
        if (MagAmmo.Value >= magSize) return;
        if (ReserveAmmo.Value <= 0) return;

        if (reloadCo != null)
            StopCoroutine(reloadCo);

        reloadCo = StartCoroutine(ServerReloadRoutine());
    }

    private IEnumerator ServerReloadRoutine()
    {
        IsReloading.Value = true;

        yield return new WaitForSeconds(reloadTime);

        int need = magSize - MagAmmo.Value;
        int take = Mathf.Min(need, ReserveAmmo.Value);

        MagAmmo.Value += take;
        ReserveAmmo.Value -= take;

        IsReloading.Value = false;
        reloadCo = null;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void CancelReloadServerRpc()
    {
        if (!IsServer) return;

        if (reloadCo != null)
        {
            StopCoroutine(reloadCo);
            reloadCo = null;
        }

        IsReloading.Value = false;
    }
}