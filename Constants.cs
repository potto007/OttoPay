namespace OttoPay;

internal static class Constants
{
    /// ExtraSlots lists panels by this name in its own config, so it must not change.
    internal const string CoinPocketUIName = "CoinPocketUI";
    internal const string ExtractCoinsButtonName = "ExtractCoinsButton";
    internal const string AuraPayButtonName = "AuraPayToggleButton";
    internal const string ArmorName = "Armor";
    internal const string WeightName = "Weight";
    internal const string JewelcraftingSynergyName = "Jewelcrafting Synergy";
    internal const string TrashButtonName = "Trash";
    /// The balance's key in the player's custom data. CurrencyPocket used the same key, so a
    /// balance built up with it carries over.
    internal const string CoinCountCustomData = "CoinPocket_CoinCount";
    internal const string CoinToken = "$item_coins";
    internal const string CoinsPrefabName = "Coins";
    internal const string AcText = "ac_text";
    internal const string ArmorIconName = "armor_icon";

    internal const string QuickStackStoreGuid = "goldenrevolver.quick_stack_store";
    internal const string JewelcraftingGuid = "org.bepinex.plugins.jewelcrafting";
    internal const string RapidLoadoutsGuid = "Azumatt.RapidLoadouts";
    internal const string OttoAuraGuid = "potto007.OttoAura";
    internal const string ExtraSlotsGuid = "shudnal.ExtraSlots";
}
