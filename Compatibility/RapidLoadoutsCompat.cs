using BepInEx.Bootstrap;

namespace OttoPay.Compatibility;

/// RapidLoadouts sells loadouts for coins through its own coin count, so it reads the balance
/// too. Its type only exists when it is installed, and plugins may load in any order, so the
/// patch goes on at Start, once every plugin has loaded, and only when it is there.
internal static class RapidLoadoutsCompat
{
    private const string CoinCountMethod = "RapidLoadouts.UI.PurchasableLoadoutGui:GetPlayerCoins";

    internal static void Init(Harmony harmony)
    {
        if (!Chainloader.PluginInfos.TryGetValue(RapidLoadoutsGuid, out PluginInfo? info) || info == null || info.Instance == null)
            return;

        System.Reflection.MethodInfo? target = AccessTools.Method(CoinCountMethod);
        if (target == null)
        {
            Log.Warning($"RapidLoadouts is installed but {CoinCountMethod} was not found, so its loadouts will not see the balance.");
            return;
        }

        harmony.Patch(target, postfix: new HarmonyMethod(typeof(RapidLoadoutsCompat), nameof(PurchasableLoadoutGuiGetPlayerCoinsPostfix)));
    }

    private static void PurchasableLoadoutGuiGetPlayerCoinsPostfix(ref int __result, ItemDrop ___m_coinPrefab)
    {
        if (___m_coinPrefab == null || ___m_coinPrefab.m_itemData.m_shared.m_name != CoinToken)
            return;
        if (Membership.LocalIsMember())
            __result += Balance.Local();
    }
}
