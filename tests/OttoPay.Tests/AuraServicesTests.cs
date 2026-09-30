using OttoPay.Banking;

namespace OttoPay.Tests;

public class AuraServicesTests
{
    [Fact]
    public void Services_are_listed_in_the_order_they_first_registered()
    {
        AuraServices services = new();
        services.Register("AuraBoost", () => "boost");
        services.Register("AuraMove", () => "move");
        services.Register("AuraBoost", () => "boost again");

        Assert.Equal(new[] { "boost again", "move" }, services.Lines());
    }

    [Fact]
    public void An_unregistered_service_drops_out()
    {
        AuraServices services = new();
        services.Register("AuraBoost", () => "boost");
        services.Register("AuraMove", () => "move");

        services.Unregister("AuraBoost");
        services.Unregister("NeverRegistered");

        Assert.Equal(new[] { "move" }, services.Lines());
    }

    [Fact]
    public void Blank_answers_are_left_out_and_the_rest_trimmed()
    {
        AuraServices services = new();
        services.Register("Empty", () => "");
        services.Register("Spaces", () => "   ");
        services.Register("Null", () => null!);
        services.Register("Padded", () => "  padded \n");

        Assert.Equal(new[] { "padded" }, services.Lines());
    }

    [Fact]
    public void A_service_that_throws_is_left_out_every_time()
    {
        AuraServices services = new();
        services.Register("Broken", () => throw new InvalidOperationException("boom"));
        services.Register("Fine", () => "fine");

        Assert.Equal(new[] { "fine" }, services.Lines());
        Assert.Equal(new[] { "fine" }, services.Lines());
    }
}
