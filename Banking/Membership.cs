namespace OttoPay.Banking;

/// Membership of the Merchant Bank Network and the AuraPay switch, both kept in the player's
/// own custom data. Until a player joins, coins behave exactly as they do in vanilla.
internal static class Membership
{
    /// Coins already in the balance, from CurrencyPocket or an earlier OttoPay, count as
    /// membership, so no coins are ever hidden from a player who never joined.
    internal static bool IsMember(Dictionary<string, string> customData)
    {
        if (customData.TryGetValue(OttoPayApi.BankMemberCustomData, out string value) && value == "1")
            return true;
        return Balance.Read(customData) > 0;
    }

    internal static void Join(Dictionary<string, string> customData)
    {
        customData[OttoPayApi.BankMemberCustomData] = "1";
    }

    /// AuraPay is off until the player turns it on, and a player outside the bank has no
    /// balance for it to charge.
    internal static bool IsAuraPayOn(Dictionary<string, string> customData)
    {
        return IsMember(customData) && customData.TryGetValue(OttoPayApi.AuraPayCustomData, out string value) && value == "1";
    }

    internal static void SetAuraPay(Dictionary<string, string> customData, bool on)
    {
        customData[OttoPayApi.AuraPayCustomData] = on ? "1" : "0";
    }

    internal static bool LocalIsMember()
    {
        Player? player = Player.m_localPlayer;
        return player != null && IsMember(player.m_customData);
    }
}
