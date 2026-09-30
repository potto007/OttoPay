namespace OttoPay.Patches;

/// Builds the balance readout into the inventory screen and keeps it current each time the
/// screen opens.
[HarmonyPatch]
internal static class BalancePanelPatches
{
    // Jewelcrafting adds its synergy panel in the same place, so the balance goes in after it.
    [HarmonyPostfix]
    [HarmonyAfter(JewelcraftingGuid)]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    private static void InventoryGuiAwakePostfix(InventoryGui __instance)
    {
        BalancePanel.Build(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static void InventoryGuiShowPrefix()
    {
        BalancePanel.Refresh();
    }

    // Last, so the panels are moved from wherever other mods have put them.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static void InventoryGuiShowPostfix(InventoryGui __instance)
    {
        BalancePanel.MakeRoom(__instance);
    }
}
