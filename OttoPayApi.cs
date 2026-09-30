namespace OttoPay;

/// The surface other mods use. OttoAura binds these members by name via reflection, so renaming
/// one or changing a signature breaks paid aura repairs and AuraTrade without a compile error
/// anywhere. Every member is safe to call before a player exists.
public static class OttoPayApi
{
    /// Stored in the player's own data, so the choice follows the character, not the world.
    public const string AuraPayCustomData = "OttoPay_AuraPay";
    public const string BankMemberCustomData = "OttoPay_BankMember";

    /// Lists a service in the AuraPay toggle's tooltip. The describe callback is asked each time
    /// the tooltip is built. Registering a name again replaces its callback in place.
    public static void RegisterAuraService(string name, Func<string> describe)
    {
        AuraServices.Shared.Register(name, describe);
        BalancePanel.RefreshAuraPayToggle();
    }

    /// Safe to call for a name that is not registered.
    public static void UnregisterAuraService(string name)
    {
        AuraServices.Shared.Unregister(name);
        BalancePanel.RefreshAuraPayToggle();
    }

    /// Lets the balance take dragged items other than coins. OttoAura's AuraTrade sells
    /// valuables this way. Each callback gets the drag's inventory, item and amount, and
    /// deposit does the whole sale itself, removing the items included; OttoPay removes
    /// nothing. Registering a name again replaces that handler in place, keeping its order.
    public static void RegisterDepositHandler(
        string name,
        Func<Inventory, ItemDrop.ItemData, int, bool> canDeposit,
        Func<Inventory, ItemDrop.ItemData, int, bool> deposit,
        Func<Inventory, ItemDrop.ItemData, int, string> describe)
    {
        DepositHandlers.Shared.Register(name, canDeposit, deposit, describe);
    }

    public static void UnregisterDepositHandler(string name)
    {
        DepositHandlers.Shared.Unregister(name);
    }

    public static bool IsBankMember()
    {
        return Membership.LocalIsMember();
    }

    public static void JoinBank()
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
            return;
        Membership.Join(player.m_customData);
        BalancePanel.Refresh();
    }

    public static int GetPouchCoins()
    {
        return Balance.Local();
    }

    public static bool IsAuraPayEnabled()
    {
        Player? player = Player.m_localPlayer;
        return player != null && Membership.IsAuraPayOn(player.m_customData);
    }

    public static void SetAuraPayEnabled(bool enabled)
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
            return;
        Membership.SetAuraPay(player.m_customData, enabled);
        BalancePanel.RefreshAuraPayToggle();
    }

    /// Credits a Merchant Bank member's balance. OttoAura's AuraTrade pays the net value of
    /// traded valuables through this; the transaction fee is never credited. The whole amount
    /// or nothing: a deposit that would overflow the balance credits nothing.
    public static bool TryDeposit(int amount)
    {
        if (amount < 0 || !IsBankMember())
            return false;
        if (!Balance.TryDeposit(Balance.Local(), amount, out int updated))
            return false;
        if (amount == 0)
            return true;
        Balance.SetLocal(updated);
        BalancePanel.Refresh();
        return true;
    }

    /// Takes the whole amount or nothing, so a caller never pays for half a service.
    public static bool TryWithdraw(int amount)
    {
        if (amount <= 0)
            return true;
        if (!IsBankMember())
            return false;
        if (!Balance.TryWithdraw(Balance.Local(), amount, out int updated))
            return false;
        Balance.SetLocal(updated);
        BalancePanel.Refresh();
        return true;
    }
}
