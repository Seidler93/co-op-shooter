public static class ShopCatalog
{
    public static int GetCost(ShopItemType item)
    {
        switch (item)
        {
            case ShopItemType.Ammo: return 100;
            case ShopItemType.Health: return 150;
            case ShopItemType.DamageUpgrade: return 300;
            case ShopItemType.FireRateUpgrade: return 300;
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