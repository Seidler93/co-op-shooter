using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSpawnManager : MonoBehaviour
{
    private List<Transform> spawnPoints = new();
    private int nextIndex = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Gameplay")
        {
            CacheSpawnPoints();
        }
    }

    private void CacheSpawnPoints()
    {
        spawnPoints.Clear();

        var gos = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (var go in gos)
            spawnPoints.Add(go.transform);

        Debug.Log($"Found {spawnPoints.Count} spawn points.");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                               NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;

        if (spawnPoints.Count > 0)
        {
            var sp = spawnPoints[nextIndex % spawnPoints.Count];
            nextIndex++;

            response.Position = sp.position;
            response.Rotation = sp.rotation;
        }

        response.Pending = false;
    }
}