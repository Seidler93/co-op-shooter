using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyManager : NetworkBehaviour
{
    private const string EntrySeparator = "::";

    public static PartyManager Instance { get; private set; }

    [SerializeField] private string gameplaySceneName = "Gameplay";

    public NetworkList<FixedString64Bytes> Players;
    private string lastSubmittedDisplayName = string.Empty;

    private void Awake()
    {
        Instance = this;
        Players = new NetworkList<FixedString64Bytes>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsClient)
            return;

        string resolvedName = ResolveLocalDisplayName();
        if (string.IsNullOrWhiteSpace(resolvedName))
            return;

        if (string.Equals(resolvedName, lastSubmittedDisplayName, System.StringComparison.Ordinal))
            return;

        SubmitLocalPlayerIdentity();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            // Add host if list is empty when session starts
            if (Players.Count == 0)
            {
                AddPlayer(NetworkManager.LocalClientId);
            }
        }

        if (IsClient)
        {
            SubmitLocalPlayerIdentity();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (Instance == this)
        {
            Instance = null;
        }

        Players?.Dispose();
    }

    public void RequestStartGame()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("[PartyManager] Cannot start game before PartyManager is network-spawned.");
            return;
        }

        if (IsServer)
        {
            StartGameForParty();
        }
        else
        {
            RequestStartGameRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStartGameRpc()
    {
        Debug.Log("[PartyManager] Client requested game start.");
        StartGameForParty();
    }

    private void StartGameForParty()
    {
        if (!IsServer || NetworkManager == null)
        {
            return;
        }

        NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        AddPlayer(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (TryParseEntry(Players[i].ToString(), out ulong entryClientId, out _)
                && entryClientId == clientId)
            {
                Players.RemoveAt(i);
                break;
            }
        }
    }

    private void AddPlayer(ulong clientId)
    {
        string entry = BuildEntry(clientId, GetFallbackName(clientId));

        for (int i = 0; i < Players.Count; i++)
        {
            if (TryParseEntry(Players[i].ToString(), out ulong entryClientId, out _)
                && entryClientId == clientId)
                return;
        }

        Players.Add(entry);
    }

    private void SubmitLocalPlayerIdentity()
    {
        string displayName = ResolveLocalDisplayName();
        lastSubmittedDisplayName = displayName;

        if (IsServer)
        {
            UpdatePlayerEntry(NetworkManager.LocalClientId, displayName);
            return;
        }

        SubmitPlayerIdentityRpc(displayName);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitPlayerIdentityRpc(string displayName, RpcParams rpcParams = default)
    {
        UpdatePlayerEntry(rpcParams.Receive.SenderClientId, displayName);
    }

    private void UpdatePlayerEntry(ulong clientId, string displayName)
    {
        if (!IsServer)
            return;

        string sanitizedName = SanitizeDisplayName(displayName, clientId);
        string entry = BuildEntry(clientId, sanitizedName);

        for (int i = 0; i < Players.Count; i++)
        {
            if (TryParseEntry(Players[i].ToString(), out ulong entryClientId, out _)
                && entryClientId == clientId)
            {
                Players[i] = entry;
                return;
            }
        }

        Players.Add(entry);
    }

    public static string GetDisplayNameFromEntry(string entry)
    {
        return TryParseEntry(entry, out _, out string displayName)
            ? displayName
            : entry;
    }

    private static bool TryParseEntry(string entry, out ulong clientId, out string displayName)
    {
        clientId = 0;
        displayName = string.Empty;

        if (string.IsNullOrWhiteSpace(entry))
            return false;

        string[] parts = entry.Split(EntrySeparator, 2);
        if (parts.Length != 2 || !ulong.TryParse(parts[0], out clientId))
            return false;

        displayName = parts[1];
        return true;
    }

    private static string BuildEntry(ulong clientId, string displayName)
    {
        return $"{clientId}{EntrySeparator}{displayName}";
    }

    private static string SanitizeDisplayName(string displayName, ulong clientId)
    {
        string trimmed = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? GetFallbackName(clientId) : trimmed;
    }

    private static string GetFallbackName(ulong clientId)
    {
        return $"Player {clientId + 1}";
    }

    private static string ResolveLocalDisplayName()
    {
        FriendsService service = FriendsService.Instance;
        if (service != null)
        {
            if (!string.IsNullOrWhiteSpace(service.CurrentDisplayName))
                return service.CurrentDisplayName;

            if (!string.IsNullOrWhiteSpace(service.CurrentUsername))
                return service.CurrentUsername;
        }

        return "Player";
    }
}
