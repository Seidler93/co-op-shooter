public static class ShopCatalog
{
    private const int UpgradeBaseCost = 300;
    private const int UpgradeCostStep = 100;

    public static int GetCost(ShopItemType item, int currentLevel = 0)
    {
        switch (item)
        {
            case ShopItemType.Ammo: return 100;
            case ShopItemType.Health: return 150;
            case ShopItemType.DamageUpgrade: return UpgradeBaseCost + (UpgradeCostStep * currentLevel);
            case ShopItemType.FireRateUpgrade: return UpgradeBaseCost + (UpgradeCostStep * currentLevel);
            default: return 9999;
        }
    }

    public static string GetName(ShopItemType item)
    {
        switch (item)
        {
            case ShopItemType.Ammo: return "Ammo";
            case ShopItemType.Health: return "Health";
            case ShopItemType.DamageUpgrade: return "Damage Upgrade";
            case ShopItemType.FireRateUpgrade: return "Fire Rate Upgrade";
            default: return "Unknown";
        }
    }
}
