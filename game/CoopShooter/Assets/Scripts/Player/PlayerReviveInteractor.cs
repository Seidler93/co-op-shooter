using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerReviveInteractor : NetworkBehaviour
{
    [Header("Revive")]
    [SerializeField] private float reviveRange = 3f;
    [SerializeField] private float reviveDuration = 2f;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerState playerState;

    private PlayerControls controls;
    private InputAction interactAction;
    private PlayerHealth currentTarget;
    private float currentProgress;

    private void Awake()
    {
        controls = new PlayerControls();
        interactAction = controls.Gameplay.Interact;

        if (!playerHealth)
            playerHealth = GetComponent<PlayerHealth>();

        if (!playerState)
            playerState = GetComponent<PlayerState>();
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
        ClearReviveTarget();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!CanAttemptRevive())
        {
            ClearReviveTarget();
            return;
        }

        PlayerHealth nextTarget = FindBestReviveTarget();
        if (nextTarget != currentTarget)
        {
            currentTarget = nextTarget;
            currentProgress = 0f;
        }

        if (currentTarget == null)
        {
            DownedStateUI.Instance?.HideRevivePrompt();
            return;
        }

        bool isHolding = interactAction != null && interactAction.IsPressed();
        if (isHolding)
        {
            currentProgress += Time.deltaTime;
        }
        else
        {
            currentProgress = 0f;
        }

        float normalizedProgress = reviveDuration > 0.001f
            ? Mathf.Clamp01(currentProgress / reviveDuration)
            : 1f;

        string reviveName = currentTarget.gameObject.name;
        DownedStateUI.Instance?.ShowRevivePrompt(
            $"Hold Interact to revive {reviveName}",
            normalizedProgress
        );

        if (normalizedProgress >= 1f)
        {
            AttemptReviveServerRpc(currentTarget.NetworkObjectId);
            currentProgress = 0f;
        }
    }

    private bool CanAttemptRevive()
    {
        if (!IsSpawned)
            return false;

        if (playerHealth == null || playerState == null)
            return false;

        if (!playerHealth.IsAlive || playerHealth.IsDowned)
            return false;

        if (playerState.IsDead || playerState.IsDowned || playerState.IsInputBlocked)
            return false;

        return true;
    }

    private PlayerHealth FindBestReviveTarget()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return null;

        PlayerHealth bestTarget = null;
        float bestSqrDistance = reviveRange * reviveRange;

        foreach (NetworkClient client in networkManager.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null || playerObject == NetworkObject)
                continue;

            PlayerHealth candidate = playerObject.GetComponent<PlayerHealth>();
            if (candidate == null || !candidate.IsDowned || !candidate.IsAlive)
                continue;

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    [ServerRpc]
    private void AttemptReviveServerRpc(ulong targetNetworkObjectId)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
            return;

        PlayerHealth target = targetObject.GetComponent<PlayerHealth>();
        if (target == null || !target.IsDowned || !target.IsAlive)
            return;

        PlayerHealth reviver = GetComponent<PlayerHealth>();
        if (reviver == null || !reviver.IsAlive || reviver.IsDowned)
            return;

        float sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
        if (sqrDistance > reviveRange * reviveRange)
            return;

        target.ReviveToFull();
    }

    private void ClearReviveTarget()
    {
        currentTarget = null;
        currentProgress = 0f;
        DownedStateUI.Instance?.HideRevivePrompt();
    }
}
