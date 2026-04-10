using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PartyManager : NetworkBehaviour
{
    public static PartyManager Instance { get; private set; }

    public NetworkList<FixedString64Bytes> Players;

    private void Awake()
    {
        Instance = this;
        Players = new NetworkList<FixedString64Bytes>();
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
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        AddPlayer(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        string entryToRemove = $"Player {clientId}";
        for (int i = Players.Count - 1; i >= 0; i--)
        {
            if (Players[i].ToString() == entryToRemove)
            {
                Players.RemoveAt(i);
                break;
            }
        }
    }

    private void AddPlayer(ulong clientId)
    {
        string entry = $"Player {clientId}";

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ToString() == entry)
                return;
        }

        Players.Add(entry);
    }
}