using System;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 100;

    [SerializeField] private bool despawnOnDeath = false;
    [SerializeField] private bool disableOnDeath = false;

    [Header("Scoring")]
    [SerializeField] private bool awardPointsOnDeath = true;
    [SerializeField] private int killPoints = 100;

    [Header("Debug")]
    [SerializeField] private bool logHealthChanges = true;

    public int MaxHP => maxHP;

    public NetworkVariable<int> CurrentHP = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsAlive => CurrentHP.Value > 0;

    public event Action<Health> Died;
    public event Action<int, int> HealthChanged;
    public event Action<int, int> HealthValueChanged;

    private ulong lastAttackerClientId;
    private bool hasLastAttacker;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (CurrentHP.Value <= 0)
                CurrentHP.Value = maxHP;

            hasLastAttacker = false;
            lastAttackerClientId = 0;
        }

        CurrentHP.OnValueChanged += OnHpChanged;

        if (logHealthChanges)
        {
            Debug.Log($"[{name}] Spawned with HP: {CurrentHP.Value}/{maxHP} | IsServer={IsServer}");
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentHP.OnValueChanged -= OnHpChanged;
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        HealthValueChanged?.Invoke(oldValue, newValue);
        HealthChanged?.Invoke(newValue, maxHP);

        if (logHealthChanges)
        {
            string side = IsServer ? "SERVER" : "CLIENT";
            Debug.Log($"[{side}] {name} HP: {oldValue} -> {newValue}");
        }

        if (oldValue > 0 && newValue <= 0)
        {
            if (logHealthChanges)
            {
                Debug.Log($"[{name}] DIED | IsServer={IsServer}");
            }

            Died?.Invoke(this);

            if (disableOnDeath)
                gameObject.SetActive(false);
        }
    }

    public void ApplyDamage(int amount, ulong attackerClientId)
    {
        if (!IsServer) return;
        if (amount <= 0) return;
        if (!IsAlive) return;

        hasLastAttacker = true;
        lastAttackerClientId = attackerClientId;

        int next = Mathf.Max(0, CurrentHP.Value - amount);

        if (logHealthChanges)
        {
            Debug.Log($"[SERVER] Applying {amount} damage to {name} from client {attackerClientId}");
        }

        CurrentHP.Value = next;

        if (next <= 0)
        {
            HandleDeathServer();
        }
    }

    private void HandleDeathServer()
    {
        if (awardPointsOnDeath && hasLastAttacker && NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerClientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    NetworkPlayer player = client.PlayerObject.GetComponent<NetworkPlayer>();

                    if (player != null)
                    {
                        player.AddPoints(killPoints);
                    }
                }
            }
        }

        if (despawnOnDeath && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else if (disableOnDeath)
        {
            DisableObjectClientRpc();
        }
    }

    [ClientRpc]
    private void DisableObjectClientRpc()
    {
        gameObject.SetActive(false);
    }

    public void ResetToFull()
    {
        if (!IsServer) return;

        CurrentHP.Value = maxHP;
        hasLastAttacker = false;
        lastAttackerClientId = 0;

        if (disableOnDeath)
        {
            EnableObjectClientRpc();
        }
    }

    [ClientRpc]
    private void EnableObjectClientRpc()
    {
        gameObject.SetActive(true);
    }

    public bool Server_Heal(int amount)
    {
        if (!IsServer)
            return false;

        if (CurrentHP.Value >= MaxHP)
            return false;

        CurrentHP.Value = Mathf.Min(CurrentHP.Value + amount, MaxHP);
        HealthChanged?.Invoke(CurrentHP.Value, MaxHP);
        return true;
    }
}
