using OttoPay.Banking;

namespace OttoPay.Tests;

/// Coins dropped on an empty slot go through vanilla's own AddItem, which reads the Player
/// class and so cannot run here. Those stay with the in-game checks; stacking is the mod's own.
public class CoinPlacementTests
{
    [Fact]
    public void Coins_dropped_on_a_coin_stack_top_it_up()
    {
        Inventory player = Items.Grid(4, 2);
        ItemDrop.ItemData stack = Items.Place(player, Items.CoinStack(900), 1, 0);
        Inventory withdrawal = Items.Grid(1, 1, "withdrawal");
        ItemDrop.ItemData coins = Items.Place(withdrawal, Items.CoinStack(50), 0, 0);

        bool done = CoinPlacement.MoveInto(player, withdrawal, coins, 50, 1, 0);

        Assert.True(done);
        Assert.Equal(950, stack.m_stack);
        Assert.Single(player.GetAllItems());
        Assert.Empty(withdrawal.GetAllItems());
    }

    // The reason the mod places coins itself: vanilla will not stack coins from different
    // world levels, and withdrawn coins take the current one.
    [Fact]
    public void Coins_from_another_world_level_still_stack()
    {
        Inventory player = Items.Grid(4, 2);
        ItemDrop.ItemData stack = Items.Place(player, Items.CoinStack(100, worldLevel: 0), 0, 0);
        Inventory withdrawal = Items.Grid(1, 1, "withdrawal");
        ItemDrop.ItemData coins = Items.Place(withdrawal, Items.CoinStack(20, worldLevel: 2), 0, 0);

        Assert.True(CoinPlacement.MoveInto(player, withdrawal, coins, 20, 0, 0));
        Assert.Equal(120, stack.m_stack);
    }

    [Fact]
    public void A_full_coin_stack_takes_nothing()
    {
        Inventory player = Items.Grid(4, 2);
        ItemDrop.ItemData stack = Items.Place(player, Items.CoinStack(999), 0, 0);
        Inventory withdrawal = Items.Grid(1, 1, "withdrawal");
        ItemDrop.ItemData coins = Items.Place(withdrawal, Items.CoinStack(40), 0, 0);

        bool done = CoinPlacement.MoveInto(player, withdrawal, coins, 40, 0, 0);

        Assert.False(done);
        Assert.Equal(999, stack.m_stack);
        Assert.Equal(40, coins.m_stack);
        Assert.Contains(coins, withdrawal.GetAllItems());
    }

    [Fact]
    public void A_top_up_never_moves_more_than_the_drag_holds()
    {
        Inventory player = Items.Grid(4, 2);
        ItemDrop.ItemData stack = Items.Place(player, Items.CoinStack(10), 0, 0);
        Inventory withdrawal = Items.Grid(1, 1, "withdrawal");
        ItemDrop.ItemData coins = Items.Place(withdrawal, Items.CoinStack(30), 0, 0);

        CoinPlacement.MoveInto(player, withdrawal, coins, 500, 0, 0);

        Assert.Equal(40, stack.m_stack);
        Assert.Equal(0, coins.m_stack);
        Assert.Empty(withdrawal.GetAllItems());
    }
}
