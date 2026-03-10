using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string startMenuSceneName = "StartMenu";

    [Header("Player Spawning")]
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private float playerSpawnHeightOffset = 1.0f;

    [Header("Enemy Spawning")]
    [SerializeField] private NetworkObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Round Scaling")]
    [SerializeField] private int baseEnemies = 3;
    [SerializeField] private int enemiesPerRound = 2;

    [Header("Timing")]
    [SerializeField] private float initialStartDelay = 1.0f;
    [SerializeField] private float nextRoundDelay = 2.0f;

    [Header("Debug")]
    [SerializeField] private bool logState = true;

    public enum MatchState : int
    {
        Waiting = 0,
        Playing = 1,
        Ended = 2
    }

    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> State = new NetworkVariable<int>(
        (int)MatchState.Waiting,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly HashSet<ulong> aliveEnemyIds = new HashSet<ulong>();
    private bool ending;
    private Coroutine roundFlowRoutine;

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!IsServer) return;

        PlayerHealth.AnyPlayerDiedServer += OnAnyPlayerDiedServer;

        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompletedServer;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;

        if (!IsServer) return;

        PlayerHealth.AnyPlayerDiedServer -= OnAnyPlayerDiedServer;

        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompletedServer;

        if (roundFlowRoutine != null)
        {
            StopCoroutine(roundFlowRoutine);
            roundFlowRoutine = null;
        }
    }

    private void StartRoundFlow(IEnumerator routine)
    {
        if (roundFlowRoutine != null)
            StopCoroutine(roundFlowRoutine);

        roundFlowRoutine = StartCoroutine(routine);
    }

    private void SetStateServer(MatchState s)
    {
        State.Value = (int)s;

        if (logState)
            Debug.Log($"[RoundManager] State -> {s}");
    }

    private void OnAnyPlayerDiedServer(PlayerHealth deadPlayer)
    {
        if (!IsServer) return;
        if (ending) return;

        Debug.Log($"[RoundManager] Player died ({deadPlayer.name}) -> ending run for everyone.");
        EndRunServer("PlayerDied");
    }

    private void EndRunServer(string reason)
    {
        if (!IsServer) return;
        if (ending) return;

        ending = true;
        SetStateServer(MatchState.Ended);

        Debug.Log($"[RoundManager] Ending run: {reason}");
        ShowGameOverClientRpc();
    }

    [ClientRpc]
    private void ShowGameOverClientRpc()
    {
        GameOverUI ui = FindFirstObjectByType<GameOverUI>();
        if (ui != null)
            ui.Show();
    }

    [Rpc(SendTo.Server)]
    public void RequestRestartRpc()
    {
        if (!IsServer) return;
        if ((MatchState)State.Value != MatchState.Ended) return;

        if (NetworkManager != null &&
            NetworkManager.NetworkConfig != null &&
            NetworkManager.NetworkConfig.EnableSceneManagement &&
            NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestReturnToMenuRpc()
    {
        if (!IsServer) return;

        ReturnToMenuClientRpc();
    }

    [ClientRpc]
    private void ReturnToMenuClientRpc()
    {
        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        yield return null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Single);
    }

    private void OnSceneLoadCompletedServer(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (sceneName == gameplaySceneName)
        {
            Debug.Log("[RoundManager] Gameplay scene load complete. Spawning players and starting round flow.");
            StartRoundFlow(RespawnPlayersAndStartRoutine());
        }
        else if (sceneName == startMenuSceneName)
        {
            Debug.Log("[RoundManager] Start menu loaded.");
            ending = false;
            aliveEnemyIds.Clear();
            SetStateServer(MatchState.Waiting);
            RoundNumber.Value = 0;
        }
    }

    private IEnumerator RespawnPlayersAndStartRoutine()
    {
        yield return null;
        yield return null;

        RespawnAllPlayersServer();

        ending = false;
        aliveEnemyIds.Clear();

        SetStateServer(MatchState.Waiting);
        RoundNumber.Value = 0;

        yield return new WaitForSeconds(initialStartDelay);

        if (ending) yield break;

        StartNextRoundServer();
        roundFlowRoutine = null;
    }

    private void RespawnAllPlayersServer()
    {
        if (!IsServer) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[RoundManager] playerPrefab not assigned.");
            return;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogError("[RoundManager] No playerSpawnPoints assigned.");
            return;
        }

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            var client = NetworkManager.ConnectedClients[clientId];

            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
                client.PlayerObject.Despawn(true);

            Transform sp = playerSpawnPoints[(int)(clientId % (ulong)playerSpawnPoints.Length)];
            Vector3 spawnPos = sp.position + Vector3.up * playerSpawnHeightOffset;

            Debug.Log($"[RoundManager] Spawning player for client {clientId} at {spawnPos}");

            NetworkObject player = Instantiate(playerPrefab, spawnPos, sp.rotation);
            player.SpawnAsPlayerObject(clientId, true);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                cc.enabled = true;
            }
        }
    }

    private void StartNextRoundServer()
    {
        if (!IsServer) return;
        if (ending) return;

        SetStateServer(MatchState.Playing);

        RoundNumber.Value = Mathf.Max(1, RoundNumber.Value + 1);

        ShowRoundStartClientRpc(RoundNumber.Value);

        int enemyCount = baseEnemies + (RoundNumber.Value - 1) * enemiesPerRound;
        Debug.Log($"[RoundManager] Round {RoundNumber.Value} starting. Spawning {enemyCount} enemies.");

        SpawnEnemiesServer(enemyCount);
    }

    [ClientRpc]
    private void ShowRoundStartClientRpc(int roundNumber)
    {
        if (RoundUI.Instance == null) return;

        RoundUI.Instance.SetRound(roundNumber);
        RoundUI.Instance.ShowRoundStart(roundNumber);
    }

    private void SpawnEnemiesServer(int count)
    {
        if (!IsServer) return;

        if (enemyPrefab == null)
        {
            Debug.LogError("[RoundManager] enemyPrefab not assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[RoundManager] No spawnPoints assigned.");
            return;
        }

        aliveEnemyIds.Clear();

        for (int i = 0; i < count; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            NetworkObject enemy = Instantiate(enemyPrefab, sp.position + Vector3.up * 1.0f, sp.rotation);

            var hp = enemy.GetComponentInChildren<Health>(true);
            if (hp != null)
            {
                hp.Died -= OnEnemyDied;
                hp.Died += OnEnemyDied;
            }

            enemy.Spawn(true);
            aliveEnemyIds.Add(enemy.NetworkObjectId);
        }
    }

    private void OnEnemyDied(Health enemyHealth)
    {
        if (!IsServer) return;
        if (ending) return;

        var no = enemyHealth.GetComponentInParent<NetworkObject>();
        if (no != null)
            aliveEnemyIds.Remove(no.NetworkObjectId);

        if (aliveEnemyIds.Count == 0 && (MatchState)State.Value == MatchState.Playing)
            StartCoroutine(NextRoundAfterDelay());
    }

    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(nextRoundDelay);

        if (ending) yield break;

        StartNextRoundServer();
    }
}