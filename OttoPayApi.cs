namespace OttoPay;

// The surface other mods use. OttoAura binds these by name at runtime, so renaming or
// changing a signature here breaks paid aura repairs without a compile error anywhere.
// Every member is safe to call before a player exists.
// OttoAura binds RegisterAuraService and UnregisterAuraService by name via reflection;
// their signatures are a contract - do not rename or change parameters.
public static class OttoPayApi
{
    // Registry of aura services other mods advertise in the AuraPay toggle tooltip.
    // Stored as a list of (name, describe) pairs so first-registration order is preserved.
    private static readonly List<(string Name, Func<string> Describe)> _auraServices = new();
    // Tracks names that have already logged a warning to avoid log spam.
    private static readonly HashSet<string> _auraServiceWarnedNames = new();

    // Registers or replaces an aura service by name. The describe callback is called each
    // time the AuraPay tooltip is built. Registering a name that already exists replaces
    // the describe callback in place, preserving the original registration order.
    // After storing, refreshes an open inventory so the tooltip updates immediately.
    public static void RegisterAuraService(string name, Func<string> describe)
    {
        for (int i = 0; i < _auraServices.Count; i++)
        {
            if (_auraServices[i].Name == name)
            {
                _auraServices[i] = (name, describe);
                CurrencyPocket.UpdateAuraPayToggle();
                return;
            }
        }
        _auraServices.Add((name, describe));
        CurrencyPocket.UpdateAuraPayToggle();
    }

    // Removes a previously registered aura service. Safe to call if the name is not registered.
    public static void UnregisterAuraService(string name)
    {
        _auraServices.RemoveAll(s => s.Name == name);
        CurrencyPocket.UpdateAuraPayToggle();
    }

    // Returns the tooltip lines for all registered aura services. Each describe() is called
    // inside a try/catch; on exception a warning is logged once per name and the entry is skipped.
    // Null, empty, or whitespace-only results are also skipped.
    internal static IReadOnlyList<string> AuraServiceLines()
    {
        var lines = new List<string>();
        foreach (var (name, describe) in _auraServices)
        {
            string? line;
            try
            {
                line = describe();
            }
            catch (Exception ex)
            {
                if (_auraServiceWarnedNames.Add(name))
                    OttoPayPlugin.OttoPayLogger.LogWarning($"OttoPayApi: aura service '{name}' describe() threw: {ex.Message}");
                continue;
            }
            if (string.IsNullOrWhiteSpace(line)) continue;
            lines.Add(line.Trim());
        }
        return lines;
    }


    // Deposit handlers let other mods take dragged items other than coins on the balance.
    // OttoAura's AuraTrade sells valuables this way. OttoAura binds RegisterDepositHandler and
    // UnregisterDepositHandler by name via reflection; their signatures are a contract - do not
    // rename or change parameters. Each callback gets the drag's inventory, item and amount.
    private static readonly List<DepositHandler> _depositHandlers = new();
    private static readonly HashSet<string> _depositHandlerWarnedNames = new();

    // Registering a name that already exists replaces that handler in place, keeping its order.
    // deposit does the whole sale itself, removing the items included; OttoPay removes nothing.
    public static void RegisterDepositHandler(
        string name,
        Func<Inventory, ItemDrop.ItemData, int, bool> canDeposit,
        Func<Inventory, ItemDrop.ItemData, int, bool> deposit,
        Func<Inventory, ItemDrop.ItemData, int, string> describe)
    {
        DepositHandler handler = new(name, canDeposit, deposit, describe);
        int index = _depositHandlers.FindIndex(h => h.Name == name);
        if (index >= 0) _depositHandlers[index] = handler;
        else _depositHandlers.Add(handler);
    }

    public static void UnregisterDepositHandler(string name)
    {
        _depositHandlers.RemoveAll(h => h.Name == name);
    }

    // The first handler that takes the drag, asked afresh on every call: a handler's answer can
    // change from frame to frame, when the player walks out of a ward for one. The copy lets a
    // callback register or unregister without breaking the loop.
    internal static DepositHandler? AcceptingDepositHandler(Inventory inventory, ItemDrop.ItemData item, int amount)
    {
        foreach (DepositHandler handler in _depositHandlers.ToArray())
        {
            if (handler.CanDeposit(inventory, item, amount)) return handler;
        }

        return null;
    }

    // A callback that throws logs a warning once per name and counts as a refusal.
    internal sealed class DepositHandler(
        string name,
        Func<Inventory, ItemDrop.ItemData, int, bool> canDeposit,
        Func<Inventory, ItemDrop.ItemData, int, bool> deposit,
        Func<Inventory, ItemDrop.ItemData, int, string> describe)
    {
        internal string Name { get; } = name;

        internal bool CanDeposit(Inventory inventory, ItemDrop.ItemData item, int amount) =>
            Call(() => canDeposit(inventory, item, amount), false);

        internal bool Deposit(Inventory inventory, ItemDrop.ItemData item, int amount) =>
            Call(() => deposit(inventory, item, amount), false);

        internal string Describe(Inventory inventory, ItemDrop.ItemData item, int amount) =>
            Call(() => describe(inventory, item, amount), "") ?? "";

        private T Call<T>(Func<T> callback, T fallback)
        {
            try
            {
                return callback();
            }
            catch (Exception ex)
            {
                if (_depositHandlerWarnedNames.Add(Name))
                    OttoPayPlugin.OttoPayLogger.LogWarning($"OttoPayApi: deposit handler '{Name}' threw: {ex.Message}");
                return fallback;
            }
        }
    }

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

    // Credits a Merchant Bank member's balance. OttoAura's AuraTrade pays the net value of
    // traded valuables through this; the transaction fee is never credited. The whole amount
    // or nothing: a deposit that would overflow the balance credits nothing.
    public static bool TryDeposit(int amount)
    {
        if (amount < 0 || !IsBankMember()) return false;
        if (amount == 0) return true;
        int coins = MiscFunctions.GetPlayerCoinsFromCustomData();
        if (coins > int.MaxValue - amount) return false;
        MiscFunctions.UpdatePlayerCustomData(coins + amount);
        CurrencyPocket.UpdatePocketUI();
        return true;
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
