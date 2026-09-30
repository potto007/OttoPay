namespace OttoPay.Patches;

/// Coins a bank member picks up go straight to the balance instead of the inventory.
[HarmonyPatch]
internal static class CoinPickupPatches
{
    private static bool _autoPickingUp;

    [HarmonyPrefix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    private static bool HumanoidPickupPrefix(Humanoid __instance, GameObject go, bool autoPickupDelay, bool __runOriginal, ref bool __result)
    {
        if (!__runOriginal || __instance is not Player player || player.IsTeleporting())
            return true;
        ItemDrop? itemDrop = go.GetComponent<ItemDrop>();
        if (itemDrop == null || !itemDrop.CanPickup(autoPickupDelay) || itemDrop.m_nview.GetZDO() == null)
            return true;

        if (itemDrop.m_itemData.m_dropPrefab == null)
            itemDrop.m_itemData.m_dropPrefab = ObjectDB.instance.GetItemPrefab(Utils.GetPrefabName(itemDrop.gameObject));

        _autoPickingUp = false;

        if (itemDrop.m_itemData.m_shared.m_name != CoinToken || !Membership.LocalIsMember())
            return true;

        int amount = itemDrop.m_itemData.m_stack;
        Balance.SetLocal(Balance.Local() + amount);
        BalancePanel.Refresh();
        ZNetScene.instance.Destroy(go);
        player.m_pickupEffects.Create(player.transform.position, Quaternion.identity);
        player.ShowPickupMessage(itemDrop.m_itemData, amount);
        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    private static void PlayerAutoPickupPrefix()
    {
        _autoPickingUp = true;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    private static void PlayerAutoPickupFinalizer()
    {
        _autoPickingUp = false;
    }

    // Auto pickup skips an item the inventory has no room for. Coins take no room once they
    // go to the balance, so a full inventory still pulls them in.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
    private static void InventoryCanAddItemPostfix(ItemDrop.ItemData item, ref bool __result)
    {
        if (__result || !_autoPickingUp)
            return;
        if (item?.m_shared?.m_name == CoinToken && Membership.LocalIsMember())
            __result = true;
    }
}
