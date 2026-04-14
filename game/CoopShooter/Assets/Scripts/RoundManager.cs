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
    [SerializeField] private NetworkObject enemySmallPrefab;
    [SerializeField] private NetworkObject enemySpitterPrefab;
    [SerializeField] private NetworkObject enemyExploderPrefab;
    [SerializeField] private NetworkObject enemyBossPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private EnemySpawnLane[] spawnLanes;
    [SerializeField] private float enemySpawnHeightOffset = 1.0f;

    [Header("Round Scaling")]
    [SerializeField] private int baseEnemies = 3;
    [SerializeField] private int enemiesPerRound = 2;

    [Header("Small Enemy Waves")]
    [SerializeField] private bool enableSmallEnemies = true;
    [SerializeField] private int smallEnemyStartRound = 5;
    [SerializeField] private int baseSmallEnemies = 2;
    [SerializeField] private int smallEnemiesPerRoundStep = 1;
    [SerializeField] private int roundsPerSmallEnemyStep = 2;
    [SerializeField] private int smallEnemyRandomBonusMax = 2;

    [Header("Spitter Waves")]
    [SerializeField] private bool enableSpitters = true;
    [SerializeField] private int spitterStartRound = 4;
    [SerializeField] private int baseSpitters = 1;
    [SerializeField] private int spittersPerRoundStep = 1;
    [SerializeField] private int roundsPerSpitterStep = 3;
    [SerializeField] private int spitterRandomBonusMax = 1;

    [Header("Exploder Waves")]
    [SerializeField] private bool enableExploders = true;
    [SerializeField] private int exploderStartRound = 6;
    [SerializeField] private int baseExploders = 1;
    [SerializeField] private int explodersPerRoundStep = 1;
    [SerializeField] private int roundsPerExploderStep = 4;
    [SerializeField] private int exploderRandomBonusMax = 1;

    [Header("Boss Waves")]
    [SerializeField] private bool enableBossWaves = true;
    [SerializeField] private int bossRoundGapMin = 2;
    [SerializeField] private int bossRoundGapMax = 3;
    [SerializeField] private int baseBossCount = 1;
    [SerializeField] private int roundsPerExtraBoss = 3;

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
    private readonly List<EnemySpawnLane> validSpawnLanes = new List<EnemySpawnLane>();
    private bool ending;
    private Coroutine roundFlowRoutine;
    private int nextBossRound = -1;

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
            nextBossRound = -1;
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
        nextBossRound = GetInitialBossRound();

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
        int smallEnemyCount = GetSmallEnemyCountForRound(RoundNumber.Value);
        int spitterCount = GetSpitterCountForRound(RoundNumber.Value);
        int exploderCount = GetExploderCountForRound(RoundNumber.Value);
        int bossCount = ShouldSpawnBossesThisRound(RoundNumber.Value)
            ? GetBossCountForRound(RoundNumber.Value)
            : 0;

        Debug.Log($"[RoundManager] Round {RoundNumber.Value} starting. Spawning {enemyCount} enemies, {smallEnemyCount} small enemies, {spitterCount} spitters, {exploderCount} exploders, and {bossCount} bosses.");

        SpawnWaveServer(enemyCount, smallEnemyCount, spitterCount, exploderCount, bossCount);

        if (bossCount > 0)
        {
            nextBossRound = RoundNumber.Value + Random.Range(
                Mathf.Max(1, bossRoundGapMin),
                Mathf.Max(bossRoundGapMin, bossRoundGapMax) + 1
            );
        }
    }

    [ClientRpc]
    private void ShowRoundStartClientRpc(int roundNumber)
    {
        if (RoundUI.Instance == null) return;

        RoundUI.Instance.SetRound(roundNumber);
        RoundUI.Instance.ShowRoundStart(roundNumber);
    }

    private void SpawnWaveServer(int enemyCount, int smallEnemyCount, int spitterCount, int exploderCount, int bossCount)
    {
        if (!IsServer) return;

        if (enemyPrefab == null)
        {
            Debug.LogError("[RoundManager] enemyPrefab not assigned.");
            return;
        }

        bool hasSpawnLanes = RefreshValidSpawnLanes();

        if (!hasSpawnLanes && (spawnPoints == null || spawnPoints.Length == 0))
        {
            Debug.LogError("[RoundManager] No enemy spawn lanes or fallback spawnPoints assigned.");
            return;
        }

        aliveEnemyIds.Clear();

        Dictionary<EnemySpawnLane, int> laneUsage = new Dictionary<EnemySpawnLane, int>();

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnSingleEnemyServer(enemyPrefab, "enemy", i + 1, enemyCount, hasSpawnLanes, laneUsage);
        }

        for (int i = 0; i < smallEnemyCount; i++)
        {
            SpawnSingleEnemyServer(enemySmallPrefab, "small enemy", i + 1, smallEnemyCount, hasSpawnLanes, laneUsage);
        }

        for (int i = 0; i < spitterCount; i++)
        {
            SpawnSingleEnemyServer(enemySpitterPrefab, "spitter", i + 1, spitterCount, hasSpawnLanes, laneUsage);
        }

        for (int i = 0; i < exploderCount; i++)
        {
            SpawnSingleEnemyServer(enemyExploderPrefab, "exploder", i + 1, exploderCount, hasSpawnLanes, laneUsage);
        }

        for (int i = 0; i < bossCount; i++)
        {
            SpawnSingleEnemyServer(enemyBossPrefab, "boss", i + 1, bossCount, hasSpawnLanes, laneUsage);
        }
    }

    private void SpawnSingleEnemyServer(
        NetworkObject prefab,
        string label,
        int index,
        int totalCount,
        bool hasSpawnLanes,
        Dictionary<EnemySpawnLane, int> laneUsage)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[RoundManager] Tried to spawn {label}, but no prefab is assigned.");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        string spawnLabel;

        if (hasSpawnLanes && TryGetLaneSpawn(out EnemySpawnLane lane, laneUsage, out Transform laneSpawnPoint))
        {
            spawnPosition = laneSpawnPoint.position + Vector3.up * enemySpawnHeightOffset;
            spawnRotation = lane.GetSpawnRotation(spawnPosition);
            spawnLabel = lane.LaneName;
        }
        else
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPosition = sp.position + Vector3.up * enemySpawnHeightOffset;
            spawnRotation = sp.rotation;
            spawnLabel = sp.name;
        }

        if (logState)
        {
            Debug.Log($"[RoundManager] Spawning {label} {index}/{totalCount} from {spawnLabel}.");
        }

        NetworkObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);

        Health hp = enemy.GetComponentInChildren<Health>(true);
        if (hp != null)
        {
            hp.Died -= OnEnemyDied;
            hp.Died += OnEnemyDied;
        }

        enemy.Spawn(true);
        aliveEnemyIds.Add(enemy.NetworkObjectId);
    }

    private bool RefreshValidSpawnLanes()
    {
        validSpawnLanes.Clear();

        if (spawnLanes == null || spawnLanes.Length == 0)
            return false;

        for (int i = 0; i < spawnLanes.Length; i++)
        {
            EnemySpawnLane lane = spawnLanes[i];
            if (lane == null || !lane.HasValidSpawnPoint)
                continue;

            validSpawnLanes.Add(lane);
        }

        return validSpawnLanes.Count > 0;
    }

    private bool TryGetLaneSpawn(
        out EnemySpawnLane selectedLane,
        Dictionary<EnemySpawnLane, int> laneUsage,
        out Transform spawnPoint)
    {
        selectedLane = null;
        spawnPoint = null;

        if (validSpawnLanes.Count == 0)
            return false;

        List<EnemySpawnLane> eligibleLanes = new List<EnemySpawnLane>();

        for (int i = 0; i < validSpawnLanes.Count; i++)
        {
            EnemySpawnLane lane = validSpawnLanes[i];
            laneUsage.TryGetValue(lane, out int usedCount);

            if (usedCount < lane.MaxSpawnPerWave)
                eligibleLanes.Add(lane);
        }

        if (eligibleLanes.Count == 0)
        {
            eligibleLanes.AddRange(validSpawnLanes);
        }

        selectedLane = eligibleLanes[Random.Range(0, eligibleLanes.Count)];
        spawnPoint = selectedLane.GetRandomSpawnPoint();

        laneUsage.TryGetValue(selectedLane, out int currentCount);
        laneUsage[selectedLane] = currentCount + 1;

        return spawnPoint != null;
    }

    private bool ShouldSpawnBossesThisRound(int roundNumber)
    {
        if (!enableBossWaves)
            return false;

        if (enemyBossPrefab == null)
            return false;

        if (roundNumber <= 0)
            return false;

        if (nextBossRound < 0)
            nextBossRound = GetInitialBossRound();

        return roundNumber >= nextBossRound;
    }

    private int GetInitialBossRound()
    {
        int minGap = Mathf.Max(2, bossRoundGapMin);
        int maxGap = Mathf.Max(minGap, bossRoundGapMax);
        return Random.Range(minGap, maxGap + 1);
    }

    private int GetBossCountForRound(int roundNumber)
    {
        int scaledExtra = roundsPerExtraBoss > 0
            ? Mathf.Max(0, (roundNumber - 1) / roundsPerExtraBoss)
            : 0;

        return Mathf.Max(1, baseBossCount + scaledExtra);
    }

    private int GetSmallEnemyCountForRound(int roundNumber)
    {
        if (!enableSmallEnemies)
            return 0;

        if (enemySmallPrefab == null)
            return 0;

        if (roundNumber < smallEnemyStartRound)
            return 0;

        int roundsSinceStart = roundNumber - smallEnemyStartRound;
        int scaledExtra = roundsPerSmallEnemyStep > 0
            ? Mathf.Max(0, roundsSinceStart / roundsPerSmallEnemyStep) * Mathf.Max(0, smallEnemiesPerRoundStep)
            : 0;
        int randomBonus = smallEnemyRandomBonusMax > 0
            ? Random.Range(0, smallEnemyRandomBonusMax + 1)
            : 0;

        return Mathf.Max(0, baseSmallEnemies + scaledExtra + randomBonus);
    }

    private int GetSpitterCountForRound(int roundNumber)
    {
        if (!enableSpitters)
            return 0;

        if (enemySpitterPrefab == null)
            return 0;

        if (roundNumber < spitterStartRound)
            return 0;

        int roundsSinceStart = roundNumber - spitterStartRound;
        int scaledExtra = roundsPerSpitterStep > 0
            ? Mathf.Max(0, roundsSinceStart / roundsPerSpitterStep) * Mathf.Max(0, spittersPerRoundStep)
            : 0;
        int randomBonus = spitterRandomBonusMax > 0
            ? Random.Range(0, spitterRandomBonusMax + 1)
            : 0;

        return Mathf.Max(0, baseSpitters + scaledExtra + randomBonus);
    }

    private int GetExploderCountForRound(int roundNumber)
    {
        if (!enableExploders)
            return 0;

        if (enemyExploderPrefab == null)
            return 0;

        if (roundNumber < exploderStartRound)
            return 0;

        int roundsSinceStart = roundNumber - exploderStartRound;
        int scaledExtra = roundsPerExploderStep > 0
            ? Mathf.Max(0, roundsSinceStart / roundsPerExploderStep) * Mathf.Max(0, explodersPerRoundStep)
            : 0;
        int randomBonus = exploderRandomBonusMax > 0
            ? Random.Range(0, exploderRandomBonusMax + 1)
            : 0;

        return Mathf.Max(0, baseExploders + scaledExtra + randomBonus);
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
