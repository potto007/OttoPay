namespace OttoPay.Banking;

/// The Merchant Bank balance, a coin count kept in the player's own custom data so it follows
/// the character from world to world. The rules take plain values, so they can be checked
/// without a player.
internal static class Balance
{
    internal static int Read(Dictionary<string, string> customData)
    {
        return customData.TryGetValue(CoinCountCustomData, out string coins) ? int.Parse(coins) : 0;
    }

    /// A balance never goes below zero, whatever a caller asks for.
    internal static void Write(Dictionary<string, string> customData, int coins)
    {
        customData[CoinCountCustomData] = Math.Max(0, coins).ToString();
    }

    internal static int Local()
    {
        Player? player = Player.m_localPlayer;
        return player == null ? 0 : Read(player.m_customData);
    }

    internal static void SetLocal(int coins)
    {
        Player? player = Player.m_localPlayer;
        if (player != null)
            Write(player.m_customData, coins);
    }

    /// The whole amount or nothing. A deposit that would overflow the balance credits nothing,
    /// so a caller never loses the part that did not fit.
    internal static bool TryDeposit(int balance, int amount, out int updated)
    {
        updated = balance;
        if (amount < 0 || balance > int.MaxValue - amount)
            return false;
        updated = balance + amount;
        return true;
    }

    /// The whole amount or nothing, so a caller never pays for half a service.
    internal static bool TryWithdraw(int balance, int amount, out int updated)
    {
        updated = balance;
        if (amount <= 0)
            return true;
        if (balance < amount)
            return false;
        updated = balance - amount;
        return true;
    }

    /// A purchase takes its price from the coins the player carries first, and the balance pays
    /// only the part those coins did not cover. Charging the balance the full price on top made
    /// a player who carried coins pay twice.
    internal static int AfterPurchase(int balance, int price, int paidFromCarried)
    {
        int unpaid = price - paidFromCarried;
        if (unpaid <= 0)
            return balance;
        return balance - Math.Min(balance, unpaid);
    }
}
