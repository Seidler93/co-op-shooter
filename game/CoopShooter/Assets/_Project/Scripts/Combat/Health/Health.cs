using System;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    private ILethalDamageHandler[] lethalDamageHandlers;

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
        lethalDamageHandlers = GetComponents<ILethalDamageHandler>();

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

        if (next <= 0 && TryHandleLethalDamage(amount, attackerClientId))
        {
            return;
        }

        CurrentHP.Value = next;

        if (next <= 0)
        {
            HandleDeathServer();
        }
    }

    private bool TryHandleLethalDamage(int amount, ulong attackerClientId)
    {
        if (lethalDamageHandlers == null || lethalDamageHandlers.Length == 0)
            return false;

        for (int i = 0; i < lethalDamageHandlers.Length; i++)
        {
            ILethalDamageHandler handler = lethalDamageHandlers[i];
            if (handler == null)
                continue;

            if (handler.TryHandleLethalDamage(this, amount, attackerClientId))
                return true;
        }

        return false;
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
                        player.AddKill();
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

    public void ResetToValue(int hpValue)
    {
        if (!IsServer) return;

        CurrentHP.Value = Mathf.Clamp(hpValue, 1, maxHP);
        hasLastAttacker = false;
        lastAttackerClientId = 0;

        if (disableOnDeath)
        {
            EnableObjectClientRpc();
        }
    }

    public void ForceDeath(ulong attackerClientId = 0, bool attackerProvided = false)
    {
        if (!IsServer) return;
        if (!IsAlive) return;

        hasLastAttacker = attackerProvided;
        lastAttackerClientId = attackerClientId;
        CurrentHP.Value = 0;
        HandleDeathServer();
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
