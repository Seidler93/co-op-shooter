using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealth : NetworkBehaviour, ILethalDamageHandler
{
    [Header("Downed State")]
    [SerializeField] private bool enableDownedState = true;
    [SerializeField] private int downedHP = 1;
    [SerializeField] private float bleedoutDuration = 20f;

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
    public bool IsDowned => playerState != null && playerState.IsDowned;
    public int CurrentHP => health != null ? health.CurrentHP.Value : 0;
    public int MaxHP => health != null ? health.MaxHP : 0;
    public double DownedUntilServerTime => downedUntilServerTime.Value;

    public event Action<PlayerHealth> PlayerDowned;
    public event Action<PlayerHealth> PlayerDied;
    public event Action<int, int> PlayerHealthChanged;

    public static event Action<PlayerHealth> AnyPlayerDiedServer;
    public static event Action<PlayerHealth> AnyPlayerDownedServer;

    private readonly NetworkVariable<double> downedUntilServerTime = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine bleedoutRoutine;

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

        if (bleedoutRoutine != null)
        {
            StopCoroutine(bleedoutRoutine);
            bleedoutRoutine = null;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        PlayerHealthChanged?.Invoke(current, max);
    }

    private void HandleDied(Health deadHealth)
    {
        if (bleedoutRoutine != null)
        {
            StopCoroutine(bleedoutRoutine);
            bleedoutRoutine = null;
        }

        downedUntilServerTime.Value = 0d;

        if (logPlayerDeath)
        {
            Debug.Log($"[PlayerHealth] {name} died. OwnerClientId={OwnerClientId}, IsServer={IsServer}, IsOwner={IsOwner}");
        }

        PlayerDied?.Invoke(this);

        if (playerState != null)
        {
            playerState.SetDowned(false);
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
            playerState.SetDead(true);
        }

        ApplyLocalDeathState();

        if (IsServer)
            AnyPlayerDiedServer?.Invoke(this);
    }

    public bool TryHandleLethalDamage(Health targetHealth, int incomingDamage, ulong attackerClientId)
    {
        if (!IsServer)
            return false;

        if (!enableDownedState || targetHealth == null)
            return false;

        if (playerState != null && (playerState.IsDead || playerState.IsDowned))
            return false;

        EnterDownedStateServer();
        targetHealth.ResetToValue(Mathf.Max(1, downedHP));
        return true;
    }

    private void EnterDownedStateServer()
    {
        if (bleedoutRoutine != null)
            StopCoroutine(bleedoutRoutine);

        downedUntilServerTime.Value = GetServerTime() + bleedoutDuration;
        ApplyDownedStateLocally(true);
        SetDownedStateClientRpc(true);
        PlayerDowned?.Invoke(this);
        AnyPlayerDownedServer?.Invoke(this);
        bleedoutRoutine = StartCoroutine(BleedoutRoutine());
    }

    private System.Collections.IEnumerator BleedoutRoutine()
    {
        yield return new WaitForSeconds(bleedoutDuration);

        bleedoutRoutine = null;

        if (!IsServer || health == null || !health.IsAlive)
            yield break;

        if (playerState != null && !playerState.IsDowned)
            yield break;

        health.ForceDeath();
    }

    private void ApplyDownedStateLocally(bool downed)
    {
        if (playerState != null)
        {
            playerState.SetDowned(downed);
            playerState.SetDead(false);
            playerState.SetAiming(false);
            playerState.SetFiring(false);
            playerState.SetReloading(false);
            playerState.SetMoving(false);
        }

        if (playerController != null)
            playerController.SetGameplayInputBlocked(downed);
    }

    [ClientRpc]
    private void SetDownedStateClientRpc(bool downed)
    {
        ApplyDownedStateLocally(downed);
    }

    private void ApplyLocalDeathState()
    {
        if (playerController != null)
            playerController.SetGameplayInputBlocked(true);

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

        if (bleedoutRoutine != null)
        {
            StopCoroutine(bleedoutRoutine);
            bleedoutRoutine = null;
        }

        downedUntilServerTime.Value = 0d;
        health.ResetToFull();
        ReviveClientRpc();
    }

    [ClientRpc]
    private void ReviveClientRpc()
    {
        ApplyDownedStateLocally(false);

        if (playerState != null)
        {
            playerState.SetDead(false);
            playerState.SetDowned(false);
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
        health.ApplyDamage(amount, OwnerClientId);
    }

    public float GetDownedSecondsRemaining()
    {
        if (!IsDowned)
            return 0f;

        return Mathf.Max(0f, (float)(downedUntilServerTime.Value - GetServerTime()));
    }

    private double GetServerTime()
    {
        if (NetworkManager == null)
            return Time.unscaledTimeAsDouble;

        return NetworkManager.ServerTime.Time;
    }
}
