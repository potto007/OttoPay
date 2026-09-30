namespace OttoPay.Banking;

/// Taking coins out of the balance. The coins are put in a one slot inventory of their own and
/// handed to vanilla's split dialog, so the player picks the amount with the slider they already
/// know and then drops the coins wherever they like. The mod keeps one shared withdrawal; tests
/// build their own.
internal sealed class Withdrawal
{
    internal static readonly Withdrawal Shared = new();

    private Inventory? _coins;
    private bool _confirmed;

    /// Holds the coins the split dialog offers. Nothing leaves the balance until the dialog is
    /// confirmed.
    internal void Open(Inventory coins)
    {
        _coins = coins;
        _confirmed = false;
    }

    /// Takes the chosen amount off the balance. The withdrawal was filled with the whole balance
    /// so the slider could reach it, and only the chosen amount left the balance, so the stack is
    /// cut to that amount: only that much may reach the inventory, however many drops it takes.
    internal bool Confirm(Dictionary<string, string> playerData, int amount)
    {
        ItemDrop.ItemData? stack = _coins?.GetAllItems().FirstOrDefault();
        if (stack == null)
            return false;

        Balance.Write(playerData, Balance.Read(playerData) - amount);
        stack.m_stack = amount;
        _confirmed = true;
        return true;
    }

    /// Puts back on the balance whatever coins the drag did not place, and closes the
    /// withdrawal. Returns how many went back.
    internal int DragEnded(Dictionary<string, string> playerData)
    {
        if (_coins == null || !_confirmed)
            return 0;

        int left = 0;
        foreach (ItemDrop.ItemData stack in _coins.GetAllItems())
            left += stack.m_stack;
        _coins.RemoveAll();
        _coins = null;
        _confirmed = false;

        Balance.Write(playerData, Balance.Read(playerData) + left);
        return left;
    }

    /// Withdrawn coins may land on an empty slot or on coins, never on another item. Vanilla
    /// swaps a whole stack dropped on a different item, which would move that item into the
    /// withdrawal, and nothing ever takes an item out of it.
    internal static bool MayLandOn(ItemDrop.ItemData? occupant)
    {
        return occupant == null || occupant.m_shared.m_name == CoinToken;
    }

    internal bool Owns(Inventory? inventory)
    {
        return inventory != null && inventory == _coins;
    }

    internal static void Start()
    {
        Player? player = Player.m_localPlayer;
        int balance = Balance.Local();
        if (player == null || balance <= 0)
            return;

        // A withdrawal still open from an earlier click is settled first. Settling empties it,
        // so a drag still showing its coins can no longer place them.
        if (Shared.DragEnded(player.m_customData) > 0)
            balance = Balance.Local();

        GameObject? prefab = ObjectDB.instance.GetItemPrefab(CoinsPrefabName);
        Inventory coins = new(CoinCountCustomData, prefab.GetComponent<ItemDrop>().m_itemData.GetIcon(), 1, 1);
        coins.AddItem(prefab, balance);
        Shared.Open(coins);
        InventoryGui.instance.ShowSplitDialog(coins.GetAllItems().FirstOrDefault(), coins);

        // ShowSplitDialog resets the slider, so Ctrl for everything has to come after it.
        if (ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl))
        {
            SplitDialog dialog = InventoryGui.instance.m_splitDialog;
            dialog.SliderValue = dialog.m_splitSlider.maxValue;
        }
    }

    /// Confirms the shared withdrawal as the split dialog closes on its coins.
    internal static void ConfirmSplit(InventoryGui gui)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || !Shared.Holds(gui.m_splitItem))
            return;

        // The dialog sometimes swaps its inventory for the player's own, so the coins would be
        // dragged out of the inventory instead of the withdrawal. Setting it back keeps the drag
        // on the withdrawn coins.
        gui.m_splitInventory = Shared._coins;
        if (Shared.Confirm(player.m_customData, (int)gui.m_splitDialog.SliderValue))
            BalancePanel.Refresh();
    }

    private bool Holds(ItemDrop.ItemData? item)
    {
        return item != null && _coins != null && _coins.ContainsItem(item);
    }

    internal static bool IsWithdrawalDrag(Inventory? dragInventory)
    {
        return dragInventory is { m_name: CoinCountCustomData };
    }
}
