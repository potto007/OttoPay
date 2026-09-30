using OttoPay.Banking;

namespace OttoPay.Tests;

public class BalanceTests
{
    [Fact]
    public void A_player_with_no_entry_has_an_empty_balance()
    {
        Assert.Equal(0, Balance.Read(new Dictionary<string, string>()));
    }

    [Fact]
    public void The_balance_is_read_from_the_key_CurrencyPocket_used()
    {
        Dictionary<string, string> data = new() { ["CoinPocket_CoinCount"] = "1234" };

        Assert.Equal(1234, Balance.Read(data));
    }

    [Fact]
    public void Writing_a_negative_balance_stores_zero()
    {
        Dictionary<string, string> data = new();

        Balance.Write(data, -5);

        Assert.Equal("0", data["CoinPocket_CoinCount"]);
    }

    [Theory]
    [InlineData(100, 50, true, 150)]
    [InlineData(100, 0, true, 100)]
    [InlineData(100, -1, false, 100)]
    [InlineData(int.MaxValue - 10, 10, true, int.MaxValue)]
    [InlineData(int.MaxValue - 10, 11, false, int.MaxValue - 10)]
    public void A_deposit_credits_all_or_nothing(int balance, int amount, bool accepted, int expected)
    {
        Assert.Equal(accepted, Balance.TryDeposit(balance, amount, out int updated));
        Assert.Equal(expected, updated);
    }

    [Theory]
    [InlineData(100, 40, true, 60)]
    [InlineData(100, 100, true, 0)]
    [InlineData(100, 101, false, 100)]
    [InlineData(100, 0, true, 100)]
    [InlineData(0, -3, true, 0)]
    public void A_withdrawal_takes_all_or_nothing(int balance, int amount, bool accepted, int expected)
    {
        Assert.Equal(accepted, Balance.TryWithdraw(balance, amount, out int updated));
        Assert.Equal(expected, updated);
    }

    [Fact]
    public void Carried_coins_that_cover_the_price_leave_the_balance_alone()
    {
        Assert.Equal(500, Balance.AfterPurchase(500, 120, 120));
    }

    [Fact]
    public void The_balance_pays_only_what_carried_coins_did_not()
    {
        Assert.Equal(430, Balance.AfterPurchase(500, 120, 50));
    }

    [Fact]
    public void A_short_balance_is_emptied_not_overdrawn()
    {
        Assert.Equal(0, Balance.AfterPurchase(30, 120, 0));
    }
}
