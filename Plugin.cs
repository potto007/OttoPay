using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using OttoPay.Compatibility;
using ServerSync;

namespace OttoPay;

[BepInPlugin(ModGUID, ModName, ModVersion)]
// Both keep coins under the same player data key, so only one may load.
[BepInIncompatibility("Azumatt.CurrencyPocket")]
public class OttoPayPlugin : BaseUnityPlugin
{
    internal const string ModName = "OttoPay";
    internal const string ModVersion = "1.6.5";
    internal const string Author = "potto007";
    private const string ModGUID = $"{Author}.{ModName}";
    private const string ConfigFileName = $"{ModGUID}.cfg";
    // One second, in ticks.
    private const long ReloadDelay = 10000000;
    private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    // Nothing tested may read a static of this class: this initializer needs ServerSync, which
    // only runs inside the game. Shared state lives in the feature classes instead.
    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = false };
    private readonly Harmony _harmony = new(ModGUID);
    private readonly object _reloadLock = new();
    private FileSystemWatcher? _watcher;
    private DateTime _lastConfigReloadTime;

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        bool saveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;

        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();

        BalanceDropTarget.DepositSprite = EmbeddedSprites.Load("download.png");

        Config.Save();
        if (saveOnSet)
        {
            Config.SaveOnConfigSet = saveOnSet;
        }
    }

    public void Start()
    {
        RapidLoadoutsCompat.Init(_harmony);
        ExtraSlotsCompat.Init();
    }

    private void OnDestroy()
    {
        SaveWithRespectToConfigSet();
        _watcher?.Dispose();
    }

    private void SetupWatcher()
    {
        _watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        _watcher.Changed += ReadConfigValues;
        _watcher.Created += ReadConfigValues;
        _watcher.Renamed += ReadConfigValues;
        _watcher.IncludeSubdirectories = true;
        _watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        DateTime now = DateTime.Now;
        long time = now.Ticks - _lastConfigReloadTime.Ticks;
        if (time < ReloadDelay)
        {
            return;
        }

        lock (_reloadLock)
        {
            if (!File.Exists(ConfigFileFullPath))
            {
                Log.Warning("Config file does not exist. Skipping reload.");
                return;
            }

            try
            {
                Log.Debug("Reloading configuration...");
                SaveWithRespectToConfigSet(true);
                Log.Info("Configuration reload complete.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error reloading configuration: {ex.Message}");
            }
        }

        _lastConfigReloadTime = now;
    }

    private void SaveWithRespectToConfigSet(bool reload = false)
    {
        bool originalSaveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;
        if (reload)
            Config.Reload();
        Config.Save();
        if (originalSaveOnSet)
        {
            Config.SaveOnConfigSet = originalSaveOnSet;
        }
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }
}
