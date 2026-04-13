using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayLeaderboardUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Leaderboard")]
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private LeaderboardRowUI leaderboardRowPrefab;
    [SerializeField] private TMP_Text emptyLeaderboardText;
    [SerializeField] private float refreshInterval = 0.25f;

    [Header("Input")]
    [SerializeField] private bool showWhileHoldingTab = true;

    private readonly List<LeaderboardRowUI> spawnedRows = new();
    private readonly List<LeaderboardEntry> cachedEntries = new();

    private float nextRefreshTime;

    private struct LeaderboardEntry
    {
        public ulong ClientId;
        public string DisplayName;
        public int Score;
        public int Kills;
        public bool IsLocalPlayer;
    }

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldShowLeaderboard();

        if (root != null && root.activeSelf != shouldShow)
            root.SetActive(shouldShow);

        if (!shouldShow || Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshLeaderboard();
    }

    private bool ShouldShowLeaderboard()
    {
        if (!showWhileHoldingTab)
            return false;

        if (Keyboard.current == null || !Keyboard.current.tabKey.isPressed)
            return false;

        GameplayMenuUI gameplayMenu = FindFirstObjectByType<GameplayMenuUI>();
        if (gameplayMenu != null && gameplayMenu.IsOpen)
            return false;

        GameOverUI gameOverUI = FindFirstObjectByType<GameOverUI>();
        if (gameOverUI != null && gameOverUI.IsVisible)
            return false;

        return true;
    }

    private void RefreshLeaderboard()
    {
        BuildEntries();
        SyncRows();
    }

    private void BuildEntries()
    {
        cachedEntries.Clear();

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        PartyManager partyManager = PartyManager.Instance;

        foreach (var client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null)
                continue;

            NetworkPlayer networkPlayer = playerObject.GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
                continue;

            string displayName = partyManager != null
                ? partyManager.GetDisplayNameForClient(client.ClientId)
                : $"Player {client.ClientId + 1}";

            cachedEntries.Add(new LeaderboardEntry
            {
                ClientId = client.ClientId,
                DisplayName = displayName,
                Score = networkPlayer.Score.Value,
                Kills = networkPlayer.Kills.Value,
                IsLocalPlayer = playerObject.IsOwner
            });
        }

        cachedEntries.Sort((left, right) =>
        {
            int scoreCompare = right.Score.CompareTo(left.Score);
            if (scoreCompare != 0)
                return scoreCompare;

            int killCompare = right.Kills.CompareTo(left.Kills);
            if (killCompare != 0)
                return killCompare;

            return left.ClientId.CompareTo(right.ClientId);
        });
    }

    private void SyncRows()
    {
        if (leaderboardContent == null || leaderboardRowPrefab == null)
            return;

        while (spawnedRows.Count < cachedEntries.Count)
        {
            LeaderboardRowUI row = Instantiate(leaderboardRowPrefab, leaderboardContent);
            spawnedRows.Add(row);
        }

        for (int i = 0; i < spawnedRows.Count; i++)
        {
            bool shouldShow = i < cachedEntries.Count;
            spawnedRows[i].gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            LeaderboardEntry entry = cachedEntries[i];
            spawnedRows[i].Bind(i + 1, entry.DisplayName, entry.Score, entry.Kills, entry.IsLocalPlayer);
        }

        if (emptyLeaderboardText != null)
            emptyLeaderboardText.gameObject.SetActive(cachedEntries.Count == 0);
    }
}
