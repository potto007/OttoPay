namespace OttoPay.Banking;

/// Taking coins out of the balance. The coins are put in a one slot inventory of their own and
/// handed to vanilla's split dialog, so the player picks the amount with the slider they already
/// know and then drops the coins wherever they like.
internal static class Withdrawal
{
    private static bool _inProgress;

    /// The inventory the withdrawn coins are dragged from until they land somewhere.
    internal static Inventory? Pending;

    internal static void Start()
    {
        int balance = Balance.Local();
        if (Player.m_localPlayer == null || balance <= 0)
            return;

        GameObject? coins = ObjectDB.instance.GetItemPrefab(CoinsPrefabName);
        Pending = new Inventory(CoinCountCustomData, coins.GetComponent<ItemDrop>().m_itemData.GetIcon(), 1, 1);
        Pending.AddItem(coins, balance);
        InventoryGui.instance.ShowSplitDialog(Pending.m_inventory.FirstOrDefault(), Pending);

        // ShowSplitDialog resets the slider, so Ctrl for everything has to come after it.
        if (ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl))
        {
            SplitDialog dialog = InventoryGui.instance.m_splitDialog;
            dialog.SliderValue = dialog.m_splitSlider.maxValue;
        }

        _inProgress = true;
    }

    /// Takes the chosen amount off the balance as the split dialog is confirmed.
    internal static void Confirm(InventoryGui gui)
    {
        if (!_inProgress || gui.m_splitItem?.m_shared.m_name != CoinToken)
            return;

        // The dialog sometimes swaps its inventory for the player's own, so the coins would be
        // dragged out of the inventory instead of the withdrawal. Setting it back keeps the drag
        // on the withdrawn coins.
        gui.m_splitInventory = Pending;
        int amount = (int)gui.m_splitDialog.SliderValue;
        Balance.SetLocal(Balance.Local() - amount);
        // The withdrawal was filled with the whole balance so the slider could reach it. Only
        // the chosen amount left the balance, so only that much may reach the inventory, however
        // many drops it takes.
        gui.m_splitItem!.m_stack = amount;
        BalancePanel.Refresh();
        _inProgress = false;
    }

    internal static bool IsWithdrawalDrag(Inventory? dragInventory)
    {
        return dragInventory is { m_name: CoinCountCustomData };
    }
}
