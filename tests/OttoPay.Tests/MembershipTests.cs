using OttoPay.Banking;

namespace OttoPay.Tests;

public class MembershipTests
{
    [Fact]
    public void A_new_character_is_not_a_member()
    {
        Assert.False(Membership.IsMember(new Dictionary<string, string>()));
    }

    [Fact]
    public void Joining_makes_a_member()
    {
        Dictionary<string, string> data = new();

        Membership.Join(data);

        Assert.True(Membership.IsMember(data));
    }

    [Fact]
    public void Coins_already_in_the_balance_count_as_membership()
    {
        Dictionary<string, string> data = new() { ["CoinPocket_CoinCount"] = "7" };

        Assert.True(Membership.IsMember(data));
    }

    [Fact]
    public void An_empty_carried_over_balance_is_not_membership()
    {
        Dictionary<string, string> data = new() { ["CoinPocket_CoinCount"] = "0" };

        Assert.False(Membership.IsMember(data));
    }

    [Fact]
    public void AuraPay_is_off_until_turned_on()
    {
        Dictionary<string, string> data = new();
        Membership.Join(data);

        Assert.False(Membership.IsAuraPayOn(data));
        Membership.SetAuraPay(data, true);
        Assert.True(Membership.IsAuraPayOn(data));
        Membership.SetAuraPay(data, false);
        Assert.False(Membership.IsAuraPayOn(data));
    }

    [Fact]
    public void AuraPay_stays_off_outside_the_bank()
    {
        Dictionary<string, string> data = new();

        Membership.SetAuraPay(data, true);

        Assert.False(Membership.IsAuraPayOn(data));
    }
}
