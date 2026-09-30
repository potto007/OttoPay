namespace OttoPay.Banking;

/// The aura services other mods advertise in the AuraPay toggle's tooltip, in the order they
/// first registered. The mod keeps one shared registry; tests build their own.
internal sealed class AuraServices
{
    internal static readonly AuraServices Shared = new();

    private readonly List<(string Name, Func<string> Describe)> _services = new();
    // A describe callback that throws is logged once, not on every tooltip build.
    private readonly HashSet<string> _warnedNames = new();

    /// Registering a name that already exists replaces its callback in place, so a mod that
    /// reloads keeps its spot in the list.
    internal void Register(string name, Func<string> describe)
    {
        for (int i = 0; i < _services.Count; i++)
        {
            if (_services[i].Name != name)
                continue;
            _services[i] = (name, describe);
            return;
        }

        _services.Add((name, describe));
    }

    internal void Unregister(string name)
    {
        _services.RemoveAll(service => service.Name == name);
    }

    /// Each callback is asked afresh, because a service's state can change between tooltips.
    /// One that throws, or answers with nothing, is left out.
    internal IReadOnlyList<string> Lines()
    {
        List<string> lines = new();
        foreach ((string name, Func<string> describe) in _services)
        {
            string? line;
            try
            {
                line = describe();
            }
            catch (Exception ex)
            {
                if (_warnedNames.Add(name))
                    Log.Warning($"OttoPayApi: aura service '{name}' describe() threw: {ex.Message}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;
            lines.Add(line!.Trim());
        }

        return lines;
    }
}
