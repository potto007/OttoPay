namespace OttoPay.Banking;

/// Handlers that take dragged items other than coins on the balance. OttoAura's AuraTrade
/// sells valuables this way. The mod keeps one shared registry; tests build their own.
internal sealed class DepositHandlers
{
    internal static readonly DepositHandlers Shared = new();

    private readonly List<DepositHandler> _handlers = new();
    // Shared by every handler here, so a callback that throws is logged once per name.
    private readonly HashSet<string> _warnedNames = new();

    /// Registering a name that already exists replaces that handler in place, keeping its order.
    internal void Register(
        string name,
        Func<Inventory, ItemDrop.ItemData, int, bool> canDeposit,
        Func<Inventory, ItemDrop.ItemData, int, bool> deposit,
        Func<Inventory, ItemDrop.ItemData, int, string> describe)
    {
        DepositHandler handler = new(name, canDeposit, deposit, describe, _warnedNames);
        int index = _handlers.FindIndex(existing => existing.Name == name);
        if (index >= 0)
            _handlers[index] = handler;
        else
            _handlers.Add(handler);
    }

    internal void Unregister(string name)
    {
        _handlers.RemoveAll(handler => handler.Name == name);
    }

    /// The first handler that takes the drag, asked afresh on every call: a handler's answer can
    /// change from frame to frame, when the player walks out of a ward for one. The copy lets a
    /// callback register or unregister without breaking the loop.
    internal DepositHandler? Accepting(Inventory inventory, ItemDrop.ItemData item, int amount)
    {
        foreach (DepositHandler handler in _handlers.ToArray())
        {
            if (handler.CanDeposit(inventory, item, amount))
                return handler;
        }

        return null;
    }
}

/// One registered handler. A callback that throws logs a warning once per name and counts as a
/// refusal, so a broken handler cannot take a player's items.
internal sealed class DepositHandler
{
    private readonly Func<Inventory, ItemDrop.ItemData, int, bool> _canDeposit;
    private readonly Func<Inventory, ItemDrop.ItemData, int, bool> _deposit;
    private readonly Func<Inventory, ItemDrop.ItemData, int, string> _describe;
    private readonly HashSet<string> _warnedNames;

    internal DepositHandler(
        string name,
        Func<Inventory, ItemDrop.ItemData, int, bool> canDeposit,
        Func<Inventory, ItemDrop.ItemData, int, bool> deposit,
        Func<Inventory, ItemDrop.ItemData, int, string> describe,
        HashSet<string> warnedNames)
    {
        Name = name;
        _canDeposit = canDeposit;
        _deposit = deposit;
        _describe = describe;
        _warnedNames = warnedNames;
    }

    internal string Name { get; }

    internal bool CanDeposit(Inventory inventory, ItemDrop.ItemData item, int amount)
    {
        return Call(() => _canDeposit(inventory, item, amount), false);
    }

    /// The handler does the whole sale itself, removing the items included; OttoPay removes
    /// nothing.
    internal bool Deposit(Inventory inventory, ItemDrop.ItemData item, int amount)
    {
        return Call(() => _deposit(inventory, item, amount), false);
    }

    internal string Describe(Inventory inventory, ItemDrop.ItemData item, int amount)
    {
        return Call(() => _describe(inventory, item, amount), "") ?? "";
    }

    private T Call<T>(Func<T> callback, T fallback)
    {
        try
        {
            return callback();
        }
        catch (Exception ex)
        {
            if (_warnedNames.Add(Name))
                Log.Warning($"OttoPayApi: deposit handler '{Name}' threw: {ex.Message}");
            return fallback;
        }
    }
}
