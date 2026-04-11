using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PartyPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text partyStatusText;
    [SerializeField] private Transform partyMemberListRoot;
    [SerializeField] private PartyMemberCardUI partyMemberCardPrefab;
    [SerializeField] private string fallbackPlayerName = "You";
    [SerializeField] private int playerLevel = 1;

    private bool isHookedToParty;
    private string localMemberStatus = string.Empty;

    private void OnEnable()
    {
        RegisterNetworkCallbacks();
        TryHookParty();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnhookParty();
        UnregisterNetworkCallbacks();
    }

    private void Update()
    {
        if (!isHookedToParty && PartyManager.Instance != null)
        {
            TryHookParty();
            RefreshUI();
        }
    }

    private void RegisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted += RefreshUI;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientChanged;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= RefreshUI;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientChanged;
    }

    private void TryHookParty()
    {
        if (PartyManager.Instance == null || PartyManager.Instance.Players == null)
            return;

        PartyManager.Instance.Players.OnListChanged -= HandlePartyListChanged;
        PartyManager.Instance.Players.OnListChanged += HandlePartyListChanged;
        isHookedToParty = true;
    }

    private void UnhookParty()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.Players != null)
        {
            PartyManager.Instance.Players.OnListChanged -= HandlePartyListChanged;
        }

        isHookedToParty = false;
    }

    private void HandlePartyListChanged(NetworkListEvent<Unity.Collections.FixedString64Bytes> changeEvent)
    {
        RefreshUI();
    }

    private void HandleClientChanged(ulong clientId)
    {
        TryHookParty();
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshPartyCards();

        if (partyStatusText != null)
            partyStatusText.text = BuildPartyStatus();
    }

    private void RefreshPartyCards()
    {
        if (partyMemberListRoot == null || partyMemberCardPrefab == null)
            return;

        for (int i = partyMemberListRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(partyMemberListRoot.GetChild(i).gameObject);
        }

        if (PartyManager.Instance == null ||
            PartyManager.Instance.Players == null ||
            PartyManager.Instance.Players.Count == 0)
        {
            CreatePartyCard(fallbackPlayerName, playerLevel, localMemberStatus);
            return;
        }

        for (int i = 0; i < PartyManager.Instance.Players.Count; i++)
        {
            string playerName = PartyManager.Instance.Players[i].ToString();
            CreatePartyCard(playerName, playerLevel, string.Empty);
        }
    }

    private void CreatePartyCard(string playerName, int level, string status)
    {
        PartyMemberCardUI card = Instantiate(partyMemberCardPrefab, partyMemberListRoot);
        card.Bind(playerName, level, status);
    }

    public void SetLocalMemberStatus(string status)
    {
        localMemberStatus = status ?? string.Empty;
        RefreshUI();
    }

    private string BuildPartyStatus()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return "Solo";

        int count = PartyManager.Instance != null && PartyManager.Instance.Players != null
            ? Mathf.Max(PartyManager.Instance.Players.Count, 1)
            : 1;

        return count == 1 ? "Party open" : $"{count} players in party";
    }
}
