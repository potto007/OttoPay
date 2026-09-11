namespace OttoPay;

// The surface other mods use. OttoAura binds these by name at runtime, so renaming or
// changing a signature here breaks paid aura repairs without a compile error anywhere.
// Every member is safe to call before a player exists.
public static class OttoPayApi
{
    // Stored in the player's own data, so the choice follows the character, not the world.
    public const string AuraPayCustomData = "OttoPay_AuraPay";
    public const string BankMemberCustomData = "OttoPay_BankMember";

    // A player joins the Merchant Bank Network at a merchant. Coins already in the balance,
    // from CurrencyPocket or an earlier OttoPay, count as membership, so no coins are ever
    // hidden from a player who never joined.
    public static bool IsBankMember()
    {
        Player? player = Player.m_localPlayer;
        if (player == null) return false;
        if (player.m_customData.TryGetValue(BankMemberCustomData, out string value) && value == "1") return true;
        return MiscFunctions.GetPlayerCoinsFromCustomData() > 0;
    }

    public static void JoinBank()
    {
        Player? player = Player.m_localPlayer;
        if (player == null) return;
        player.m_customData[BankMemberCustomData] = "1";
        CurrencyPocket.UpdatePocketUI();
    }

    public static int GetPouchCoins() => MiscFunctions.GetPlayerCoinsFromCustomData();

    public static bool IsAuraPayEnabled()
    {
        Player? player = Player.m_localPlayer;
        return player != null && IsBankMember() && player.m_customData.TryGetValue(AuraPayCustomData, out string value) && value == "1";
    }

    public static void SetAuraPayEnabled(bool enabled)
    {
        Player? player = Player.m_localPlayer;
        if (player == null) return;
        player.m_customData[AuraPayCustomData] = enabled ? "1" : "0";
        CurrencyPocket.UpdateAuraPayToggle();
    }

    // Takes the whole amount or nothing, so a caller never pays for half a service.
    public static bool TryWithdraw(int amount)
    {
        if (amount <= 0) return true;
        if (!IsBankMember()) return false;
        int coins = MiscFunctions.GetPlayerCoinsFromCustomData();
        if (coins < amount) return false;
        MiscFunctions.UpdatePlayerCustomData(coins - amount);
        CurrencyPocket.UpdatePocketUI();
        return true;
    }
}
