using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace OttoPay.Compatibility;

/// ExtraSlots keeps lists of the inventory panels to show for each row count, and a list that
/// holds more panels than its rows allow breaks its layout. The balance panel is the one this
/// mod owns, so it is the one taken out of a list that is too long, now and whenever the
/// setting changes.
internal static class ExtraSlotsCompat
{
    private const string SectionName = "Mods compatibility - Reduced inventory size";

    // Writing the cleaned value back raises SettingChanged again.
    private static bool _handlingChange;

    internal static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue(ExtraSlotsGuid, out PluginInfo? info))
            return;

        ConfigFile config = info.Instance.Config;
        bool changed = false;
        changed |= Watch(config, "Panels to show with 1 row", 1);
        changed |= Watch(config, "Panels to show with 2 rows", 2);
        changed |= Watch(config, "Panels to show with 3 rows", 3);

        if (changed)
            config.Save();
    }

    /// Cleans the entry now and again on every change. Returns whether it changed now.
    internal static bool Watch(ConfigFile config, string key, int maxPanels)
    {
        if (!config.TryGetEntry(SectionName, key, out ConfigEntry<string>? entry))
            return false;

        bool changed = Sanitize(entry, maxPanels);
        entry.SettingChanged += (_, _) =>
        {
            if (_handlingChange)
                return;

            try
            {
                _handlingChange = true;
                Sanitize(entry, maxPanels);
            }
            finally
            {
                _handlingChange = false;
            }
        };

        return changed;
    }

    private static bool Sanitize(ConfigEntry<string> entry, int maxPanels)
    {
        string current = entry.Value ?? string.Empty;
        string cleaned = WithoutBalancePanel(current, maxPanels);
        if (cleaned == current)
            return false;

        entry.Value = cleaned;
        return true;
    }

    /// The panel list with the balance panel taken out, when the list names more panels than
    /// maxPanels. A list within its limit is the player's choice and is left as it is.
    internal static string WithoutBalancePanel(string csv, int maxPanels)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return string.Empty;

        string[] tokens = csv.Split(',');
        int panels = tokens.Count(token => !string.IsNullOrWhiteSpace(token));
        if (panels <= maxPanels)
            return csv;

        List<string> kept = new(tokens.Length);
        bool removed = false;
        foreach (string token in tokens)
        {
            if (token.Trim().Equals(CoinPocketUIName, StringComparison.OrdinalIgnoreCase))
            {
                removed = true;
                continue;
            }

            kept.Add(token);
        }

        if (!removed)
            return csv;
        return kept.Count == 0 ? string.Empty : string.Join(",", kept);
    }
}
