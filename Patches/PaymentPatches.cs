namespace OttoPay.Patches;

/// Merchants read the balance as coins the player has, and a purchase draws on it for whatever
/// the carried coins do not cover.
[HarmonyPatch]
internal static class PaymentPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.GetPlayerCoins))]
    private static void StoreGuiGetPlayerCoinsPostfix(ref int __result)
    {
        if (Membership.LocalIsMember())
            __result += Balance.Local();
    }

    // Counts the carried coins before vanilla removes the price, so the postfix knows how much
    // of it they covered.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
    private static void InventoryRemoveItemPrefix(Inventory __instance, string name, int itemQuality, bool worldLevelBased, out int __state)
    {
        __state = name == CoinToken ? __instance.CountItems(name, itemQuality, worldLevelBased) : 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
    private static void InventoryRemoveItemPostfix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased, int __state)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || __instance != player.GetInventory())
            return;
        if (name != CoinToken || !Membership.IsMember(player.m_customData))
            return;

        int paidFromCarried = __state - __instance.CountItems(name, itemQuality, worldLevelBased);
        int balance = Balance.Local();
        int updated = Balance.AfterPurchase(balance, amount, paidFromCarried);
        if (updated == balance)
            return;

        Balance.SetLocal(updated);
        BalancePanel.Refresh();
    }
}
