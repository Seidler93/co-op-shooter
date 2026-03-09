using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private MonoBehaviour[] disableOnDeathBehaviours;
    [SerializeField] private GameObject[] hideOnDeathObjects;

    [Header("Debug")]
    [SerializeField] private bool logPlayerDeath = true;

    public Health Health => health;
    public bool IsAlive => health != null && health.IsAlive;
    public int CurrentHP => health != null ? health.CurrentHP.Value : 0;
    public int MaxHP => health != null ? health.MaxHP : 0;

    public event Action<PlayerHealth> PlayerDied;
    public event Action<int, int> PlayerHealthChanged;

    public static event Action<PlayerHealth> AnyPlayerDiedServer;

    private void Awake()
    {
        if (!health) health = GetComponent<Health>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!playerState) playerState = GetComponent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        if (health == null)
        {
            Debug.LogError($"[{name}] PlayerHealth requires a Health component.");
            return;
        }

        health.Died += HandleDied;
        health.HealthChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
            health.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        PlayerHealthChanged?.Invoke(current, max);
    }

    private void HandleDied(Health deadHealth)
    {
        if (logPlayerDeath)
        {
            Debug.Log($"[PlayerHealth] {name} died. OwnerClientId={OwnerClientId}, IsServer={IsServer}, IsOwner={IsOwner}");
        }

        PlayerDied?.Invoke(this);

        if (playerState != null)
        {
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
            playerState.SetDead(true);
        }

        ApplyLocalDeathState();

        if (IsServer)
            AnyPlayerDiedServer?.Invoke(this);
    }

    private void ApplyLocalDeathState()
    {
        if (disableOnDeathBehaviours != null)
        {
            for (int i = 0; i < disableOnDeathBehaviours.Length; i++)
            {
                if (disableOnDeathBehaviours[i] != null)
                    disableOnDeathBehaviours[i].enabled = false;
            }
        }

        if (hideOnDeathObjects != null)
        {
            for (int i = 0; i < hideOnDeathObjects.Length; i++)
            {
                if (hideOnDeathObjects[i] != null)
                    hideOnDeathObjects[i].SetActive(false);
            }
        }
    }

    public void ReviveToFull()
    {
        if (!IsServer) return;
        if (health == null) return;

        health.ResetToFull();
        ReviveClientRpc();
    }

    [ClientRpc]
    private void ReviveClientRpc()
    {
        if (playerState != null)
        {
            playerState.SetDead(false);
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
        }

        if (disableOnDeathBehaviours != null)
        {
            for (int i = 0; i < disableOnDeathBehaviours.Length; i++)
            {
                if (disableOnDeathBehaviours[i] != null)
                    disableOnDeathBehaviours[i].enabled = true;
            }
        }

        if (hideOnDeathObjects != null)
        {
            for (int i = 0; i < hideOnDeathObjects.Length; i++)
            {
                if (hideOnDeathObjects[i] != null)
                    hideOnDeathObjects[i].SetActive(true);
            }
        }
    }

    public void ApplyDamage(int amount)
    {
        if (health == null) return;
        health.ApplyDamage(amount);
    }
}