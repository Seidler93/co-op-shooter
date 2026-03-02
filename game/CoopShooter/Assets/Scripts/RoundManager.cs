using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundManager : NetworkBehaviour
{
    public enum MatchState : int
    {
        Waiting = 0,
        Playing = 1,
        Ended = 2
    }

    [Header("Scenes")]
    [SerializeField] private string startMenuSceneName = "StartMenu";

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
    private Coroutine roundRoutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        SubscribeAllPlayers();

        roundRoutine = StartCoroutine(ServerStartRoutine());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private IEnumerator ServerStartRoutine()
    {
        SetStateServer(MatchState.Waiting);
        RoundNumber.Value = 0;

        yield return new WaitForSeconds(initialStartDelay);

        if (ending) yield break;

        StartNextRoundServer();
    }

    private void SetStateServer(MatchState s)
    {
        State.Value = (int)s;
        if (logState) Debug.Log($"[RoundManager] State -> {s}");
    }

    private void OnClientConnected(ulong clientId)
    {
        // Ensure new player's death ends run
        SubscribeAllPlayers();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Optional: You could treat disconnect as fail later.
        SubscribeAllPlayers();
    }

    private void SubscribeAllPlayers()
    {
        if (NetworkManager == null) return;

        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var hp = playerObj.GetComponentInChildren<Health>(true);
            if (hp == null) continue;

            // Avoid double subscriptions
            hp.Died -= OnAnyPlayerDied;
            hp.Died += OnAnyPlayerDied;
        }
    }

    private void OnAnyPlayerDied(Health playerHealth)
    {
        if (!IsServer) return;
        if (ending) return;

        Debug.Log("[RoundManager] A player died -> ending run for everyone.");
        EndRunServer("PlayerDied");
    }

    private void StartNextRoundServer()
    {
        if (!IsServer) return;
        if (ending) return;

        SetStateServer(MatchState.Playing);
        RoundNumber.Value = Mathf.Max(1, RoundNumber.Value + 1);

        int enemyCount = baseEnemies + (RoundNumber.Value - 1) * enemiesPerRound;
        Debug.Log($"[RoundManager] Round {RoundNumber.Value} starting. Spawning {enemyCount} enemies.");

        SpawnEnemiesServer(enemyCount);
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

        if (logState) Debug.Log($"[RoundManager] Enemy died. Remaining: {aliveEnemyIds.Count}");

        if (aliveEnemyIds.Count == 0 && (MatchState)State.Value == MatchState.Playing)
        {
            StartCoroutine(NextRoundAfterDelay());
        }
    }

    private IEnumerator NextRoundAfterDelay()
    {
        yield return new WaitForSeconds(nextRoundDelay);

        if (ending) yield break;

        StartNextRoundServer();
    }

    private void EndRunServer(string reason)
    {
        if (!IsServer) return;
        if (ending) return;

        ending = true;
        SetStateServer(MatchState.Ended);

        Debug.Log($"[RoundManager] Ending run: {reason}");

        // Preferred: NGO scene management (synced load)
        bool ngoSceneManagement =
            NetworkManager != null &&
            NetworkManager.NetworkConfig != null &&
            NetworkManager.NetworkConfig.EnableSceneManagement &&
            NetworkManager.SceneManager != null;

        if (ngoSceneManagement)
        {
            NetworkManager.SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[RoundManager] EnableSceneManagement is OFF. Falling back to local SceneManager load via ClientRpc.");
            LoadStartMenuClientRpc();
            SceneManager.LoadScene(startMenuSceneName);
        }
    }

    [ClientRpc]
    private void LoadStartMenuClientRpc()
    {
        SceneManager.LoadScene(startMenuSceneName);
    }
}