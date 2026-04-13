using Unity.Netcode;
using UnityEngine;

public class PlayerShopper : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private NetworkPlayer networkPlayer;
    [SerializeField] private Health health;
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private WeaponAmmoNetcode weaponAmmo;

    [Header("Health Shop")]
    [SerializeField] private int healAmount = 35;

    [Header("Ammo Shop")]
    [SerializeField] private int ammoAmount = 30;

    [Header("Upgrade Limits")]
    [SerializeField] private int maxDamageUpgradeLevel = 5;
    [SerializeField] private int maxFireRateUpgradeLevel = 5;

    public NetworkVariable<int> DamageUpgradeLevel = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> FireRateUpgradeLevel = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int CurrentDamageUpgradeLevel => DamageUpgradeLevel.Value;
    public int CurrentFireRateUpgradeLevel => FireRateUpgradeLevel.Value;

    private void Awake()
    {
        if (networkPlayer == null) networkPlayer = GetComponent<NetworkPlayer>();
        if (health == null) health = GetComponent<Health>();
        if (weaponShooter == null) weaponShooter = GetComponentInChildren<WeaponShooter>(true);
        if (weaponAmmo == null) weaponAmmo = GetComponentInChildren<WeaponAmmoNetcode>(true);
    }

    public override void OnNetworkSpawn()
    {
        DamageUpgradeLevel.OnValueChanged += HandleDamageUpgradeChanged;
        FireRateUpgradeLevel.OnValueChanged += HandleFireRateUpgradeChanged;
        ApplyUpgradeLevelsToWeapon();
    }

    public override void OnNetworkDespawn()
    {
        DamageUpgradeLevel.OnValueChanged -= HandleDamageUpgradeChanged;
        FireRateUpgradeLevel.OnValueChanged -= HandleFireRateUpgradeChanged;
    }

    [ServerRpc]
    public void BuyItemServerRpc(ShopItemType itemType, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (networkPlayer == null)
        {
            SendPurchaseResult(false, "Player score component not found.", rpcParams.Receive.SenderClientId);
            return;
        }

        int cost = GetCurrentCost(itemType);

        if (!networkPlayer.TrySpendScore(cost))
        {
            SendPurchaseResult(false, "Not enough points.", rpcParams.Receive.SenderClientId);
            return;
        }

        bool success = ApplyPurchase(itemType, out string message);

        if (!success)
        {
            networkPlayer.RefundScore(cost);
            SendPurchaseResult(false, message, rpcParams.Receive.SenderClientId);
            return;
        }

        SendPurchaseResult(true, message, rpcParams.Receive.SenderClientId);
    }

    private bool ApplyPurchase(ShopItemType itemType, out string message)
    {
        switch (itemType)
        {
            case ShopItemType.Ammo:
                if (weaponAmmo == null)
                {
                    message = "Ammo component not found.";
                    return false;
                }

                if (!weaponAmmo.Server_AddReserveAmmo(ammoAmount))
                {
                    message = "Ammo already full.";
                    return false;
                }

                message = $"+{ammoAmount} ammo";
                return true;

            case ShopItemType.Health:
                if (health == null)
                {
                    message = "Health component not found.";
                    return false;
                }

                if (!health.Server_Heal(healAmount))
                {
                    message = "Health already full.";
                    return false;
                }

                message = $"+{healAmount} health";
                return true;

            case ShopItemType.DamageUpgrade:
                if (weaponShooter == null)
                {
                    message = "Weapon shooter not found.";
                    return false;
                }

                if (DamageUpgradeLevel.Value >= maxDamageUpgradeLevel)
                {
                    message = "Damage already maxed.";
                    return false;
                }

                DamageUpgradeLevel.Value++;
                ApplyUpgradeLevelsToWeapon();

                message = $"Damage Lv {DamageUpgradeLevel.Value}";
                return true;

            case ShopItemType.FireRateUpgrade:
                if (weaponShooter == null)
                {
                    message = "Weapon shooter not found.";
                    return false;
                }

                if (FireRateUpgradeLevel.Value >= maxFireRateUpgradeLevel)
                {
                    message = "Fire rate already maxed.";
                    return false;
                }

                FireRateUpgradeLevel.Value++;
                ApplyUpgradeLevelsToWeapon();

                message = $"Fire Rate Lv {FireRateUpgradeLevel.Value}";
                return true;

            default:
                message = "Unknown item.";
                return false;
        }
    }

    private void SendPurchaseResult(bool success, string message, ulong targetClientId)
    {
        int remainingScore = networkPlayer != null ? networkPlayer.Score.Value : 0;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        };

        PurchaseResultClientRpc(
            success,
            message,
            remainingScore,
            DamageUpgradeLevel.Value,
            FireRateUpgradeLevel.Value,
            clientRpcParams
        );
    }

    [ClientRpc]
    private void PurchaseResultClientRpc(
        bool success,
        string message,
        int remainingScore,
        int damageLevel,
        int fireRateLevel,
        ClientRpcParams clientRpcParams = default
    )
    {
        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.HandlePurchaseResult(success, message, remainingScore, damageLevel, fireRateLevel);
        }
    }

    private void HandleDamageUpgradeChanged(int previousValue, int newValue)
    {
        ApplyUpgradeLevelsToWeapon();
    }

    private void HandleFireRateUpgradeChanged(int previousValue, int newValue)
    {
        ApplyUpgradeLevelsToWeapon();
    }

    private void ApplyUpgradeLevelsToWeapon()
    {
        if (weaponShooter == null)
            return;

        weaponShooter.ApplyShopUpgradeLevels(DamageUpgradeLevel.Value, FireRateUpgradeLevel.Value);
    }

    public int GetCurrentCost(ShopItemType itemType)
    {
        return itemType switch
        {
            ShopItemType.DamageUpgrade => ShopCatalog.GetCost(itemType, DamageUpgradeLevel.Value),
            ShopItemType.FireRateUpgrade => ShopCatalog.GetCost(itemType, FireRateUpgradeLevel.Value),
            _ => ShopCatalog.GetCost(itemType)
        };
    }
}
