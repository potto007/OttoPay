namespace OttoPay.Patches;

/// Carries coins withdrawn from the balance from the split dialog to the slot they are dropped
/// on.
[HarmonyPatch]
internal static class WithdrawalPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSplitOk))]
    private static void InventoryGuiOnSplitOkPrefix(InventoryGui __instance)
    {
        Withdrawal.Confirm(__instance);
    }

    // With no container of its own open, vanilla cancels every drag that did not start in the
    // player's inventory. The withdrawn coins are dragged from an inventory of their own, so
    // that would drop them the frame after the split dialog closed.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    private static bool InventoryGuiUpdateContainerPrefix(InventoryGui __instance)
    {
        if (__instance.m_currentContainer != null && __instance.m_currentContainer.IsOwner())
            return true;
        return !Withdrawal.IsWithdrawalDrag(__instance.m_dragInventory);
    }

    // Only coins moved into a bank member's own inventory are placed by the mod.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
    private static bool InventoryMoveItemToThisPrefix(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y, ref bool __result)
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
            return true;
        if (item?.m_shared?.m_name != CoinToken || __instance != player.GetInventory() || !Membership.IsMember(player.m_customData))
            return true;

        __result = CoinPlacement.MoveInto(__instance, fromInventory, item, amount, x, y);
        return false;
    }
}
