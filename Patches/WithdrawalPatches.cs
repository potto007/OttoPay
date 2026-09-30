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
        Withdrawal.ConfirmSplit(__instance);
    }

    // Every way a drag ends passes through here: a drop that lands, a deposit, closing the
    // inventory. Coins the withdrawal still holds then go back to the balance, because they
    // left it when the split dialog was confirmed.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
    private static void InventoryGuiSetupDragItemPrefix(InventoryGui __instance, Inventory inventory)
    {
        if (!Withdrawal.Shared.Owns(__instance.m_dragInventory) || Withdrawal.Shared.Owns(inventory))
            return;
        Player? player = Player.m_localPlayer;
        if (player != null && Withdrawal.Shared.DragEnded(player.m_customData) > 0)
            BalancePanel.Refresh();
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

    // Withdrawn coins dropped on another item, in the player's grid or a chest's, stay on the
    // cursor, as they do over a full coin stack. Vanilla would swap the two.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
    private static bool InventoryGridDropItemPrefix(InventoryGrid __instance, Inventory fromInventory, Vector2i pos, ref bool __result)
    {
        if (!Withdrawal.Shared.Owns(fromInventory) || Withdrawal.MayLandOn(__instance.m_inventory.GetItemAt(pos.x, pos.y)))
            return true;

        __result = false;
        return false;
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
