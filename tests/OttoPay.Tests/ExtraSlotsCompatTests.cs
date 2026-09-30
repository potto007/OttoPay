using BepInEx.Configuration;
using OttoPay.Compatibility;

namespace OttoPay.Tests;

public class ExtraSlotsCompatTests
{
    private const string Section = "Mods compatibility - Reduced inventory size";

    [Theory]
    [InlineData("Armor,Weight", 2, "Armor,Weight")]
    [InlineData("Armor,CoinPocketUI", 2, "Armor,CoinPocketUI")]
    [InlineData("CoinPocketUI", 1, "CoinPocketUI")]
    [InlineData("Armor,CoinPocketUI,Weight", 2, "Armor,Weight")]
    [InlineData("Armor, coinpocketui ,Weight", 2, "Armor,Weight")]
    [InlineData("Armor,Weight,Trash", 2, "Armor,Weight,Trash")]
    [InlineData("CoinPocketUI,CoinPocketUI", 1, "")]
    [InlineData("   ", 3, "")]
    [InlineData("Armor,,CoinPocketUI", 1, "Armor,")]
    public void Only_an_overfilled_list_loses_the_balance_panel(string csv, int maxPanels, string expected)
    {
        Assert.Equal(expected, ExtraSlotsCompat.WithoutBalancePanel(csv, maxPanels));
    }

    [Fact]
    public void An_overfilled_entry_is_cleaned_now_and_after_every_change()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ottopay-extraslots-{Guid.NewGuid():N}.cfg");
        try
        {
            ConfigFile config = new(path, true);
            ConfigEntry<string> entry = config.Bind(Section, "Panels to show with 2 rows", "Armor,CoinPocketUI,Weight");

            Assert.True(ExtraSlotsCompat.Watch(config, "Panels to show with 2 rows", 2));
            Assert.Equal("Armor,Weight", entry.Value);

            entry.Value = "Trash,CoinPocketUI,Armor";
            Assert.Equal("Trash,Armor", entry.Value);

            entry.Value = "CoinPocketUI,Armor";
            Assert.Equal("CoinPocketUI,Armor", entry.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_missing_entry_is_left_alone()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ottopay-extraslots-{Guid.NewGuid():N}.cfg");
        try
        {
            Assert.False(ExtraSlotsCompat.Watch(new ConfigFile(path, true), "Panels to show with 1 row", 1));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
