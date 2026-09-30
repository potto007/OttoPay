using OttoPay.Banking;

namespace OttoPay.Tests;

public class WithdrawalTests
{
    private readonly Dictionary<string, string> _player = new() { ["CoinPocket_CoinCount"] = "1000" };
    private readonly Inventory _coins = Items.Grid(1, 1, "CoinPocket_CoinCount");
    private readonly ItemDrop.ItemData _stack;
    private readonly Withdrawal _withdrawal = new();

    /// The withdrawal is filled with as much of the balance as a stack holds, so the split
    /// dialog's slider can reach it.
    public WithdrawalTests()
    {
        _stack = Items.Place(_coins, Items.CoinStack(999), 0, 0);
        _withdrawal.Open(_coins);
    }

    private int BalanceNow => int.Parse(_player["CoinPocket_CoinCount"]);

    [Fact]
    public void Confirming_takes_the_chosen_amount_off_the_balance()
    {
        Assert.True(_withdrawal.Confirm(_player, 50));

        Assert.Equal(950, BalanceNow);
        Assert.Equal(50, _stack.m_stack);
    }

    // Closing the inventory with withdrawn coins still on the cursor ends the drag. The coins
    // had already left the balance and nothing put them back.
    [Fact]
    public void Coins_still_held_when_the_drag_ends_go_back_to_the_balance()
    {
        _withdrawal.Confirm(_player, 50);

        Assert.Equal(50, _withdrawal.DragEnded(_player));

        Assert.Equal(1000, BalanceNow);
        Assert.Empty(_coins.GetAllItems());
    }

    // The withdrawal holds the whole balance while the dialog is open, and none of it has left
    // the balance yet, so a cancelled dialog must not pay it out a second time.
    [Fact]
    public void A_withdrawal_that_was_never_confirmed_changes_nothing()
    {
        Assert.Equal(0, _withdrawal.DragEnded(_player));

        Assert.Equal(1000, BalanceNow);
    }

    [Fact]
    public void Only_the_coins_that_landed_stay_off_the_balance()
    {
        Inventory player = Items.Grid(4, 2);
        ItemDrop.ItemData nearlyFull = Items.Place(player, Items.CoinStack(990), 0, 0);
        _withdrawal.Confirm(_player, 50);

        CoinPlacement.MoveInto(player, _coins, _stack, 50, 0, 0);
        _withdrawal.DragEnded(_player);

        Assert.Equal(999, nearlyFull.m_stack);
        Assert.Equal(991, BalanceNow);
    }
}
