using BepInEx.Logging;

namespace OttoPay;

/// The mod's log. It lives apart from the plugin class, whose static initializer builds the
/// ServerSync config and cannot run outside the game, so code under test can log without
/// pulling that in.
internal static class Log
{
    private static readonly ManualLogSource Source = BepInEx.Logging.Logger.CreateLogSource(OttoPayPlugin.ModName);

    internal static void Debug(string message)
    {
        Source.LogDebug(message);
    }

    internal static void Info(string message)
    {
        Source.LogInfo(message);
    }

    internal static void Warning(string message)
    {
        Source.LogWarning(message);
    }

    internal static void Error(string message)
    {
        Source.LogError(message);
    }
}
