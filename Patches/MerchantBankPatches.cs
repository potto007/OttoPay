namespace OttoPay.Patches;

/// Offers the Merchant Bank Network in every merchant's store window.
[HarmonyPatch]
internal static class MerchantBankPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    private static void StoreGuiShowPostfix(StoreGui __instance)
    {
        JoinButton.Refresh(__instance);
    }
}
