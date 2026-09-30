namespace OttoPay.Banking;

/// Puts coins dragged onto a slot of the player's inventory. Coins taken from the balance come
/// out of a temporary inventory, not a real one, so vanilla's stack rules are not used as they
/// stand: a coin stack in the slot takes as many as fit, whatever world level either came from.
internal static class CoinPlacement
{
    /// Returns whether the drag is done. Vanilla's own move answers the same way, and the
    /// inventory screen keeps whatever the answer leaves on the cursor.
    internal static bool MoveInto(Inventory target, Inventory? source, ItemDrop.ItemData item, int amount, int x, int y)
    {
        ItemDrop.ItemData? stack = target.GetItemAt(x, y);
        if (stack is { m_shared.m_name: CoinToken })
            return TopUp(target, source, item, amount, stack);

        int before = item.m_stack;
        bool placed = target.AddItem(item, amount, x, y);
        if (before - item.m_stack > 0)
        {
            if (item.m_stack <= 0 && source != null)
                source.RemoveItem(item);

            source?.Changed();
            target.Changed();
        }

        return placed;
    }

    private static bool TopUp(Inventory target, Inventory? source, ItemDrop.ItemData item, int amount, ItemDrop.ItemData stack)
    {
        int wanted = Math.Min(amount, item.m_stack);
        int room = Math.Max(0, stack.m_shared.m_maxStackSize - stack.m_stack);
        int moved = Math.Min(room, wanted);
        if (moved <= 0)
            return false;

        stack.m_stack += moved;
        item.m_stack -= moved;

        if (item.m_stack <= 0 && source != null)
            source.RemoveItem(item);

        target.Changed();
        source?.Changed();
        // Done only when the whole drag landed, as in vanilla. The inventory screen lets go of
        // a drag it is told is done, and coins left in a withdrawal would go with it.
        return moved == wanted;
    }
}
