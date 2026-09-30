namespace OttoPay.Tests;

internal static class Items
{
    internal const string Coins = "$item_coins";

    internal static ItemDrop.ItemData Stack(string name, int stack, int maxStack = 999, int worldLevel = 0)
    {
        return new ItemDrop.ItemData
        {
            m_shared = new ItemDrop.ItemData.SharedData { m_name = name, m_maxStackSize = maxStack },
            m_stack = stack,
            m_worldLevel = (byte)worldLevel,
        };
    }

    internal static ItemDrop.ItemData CoinStack(int stack, int worldLevel = 0)
    {
        return Stack(Coins, stack, 999, worldLevel);
    }

    internal static ItemDrop.ItemData Place(Inventory inventory, ItemDrop.ItemData item, int x, int y)
    {
        Assert.True(inventory.AddItem(item, new Vector2i(x, y)));
        return item;
    }

    internal static Inventory Grid(int width, int height, string name = "player")
    {
        return new Inventory(name, null, width, height);
    }
}
