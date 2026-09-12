using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using OttoPay.Compatibility;
using OttoPay.Pathwalk;
using JetBrains.Annotations;
using ServerSync;

namespace OttoPay;

[BepInPlugin(ModGUID, ModName, ModVersion)]
// Both keep coins under the same player data key, so only one may load.
[BepInIncompatibility("Azumatt.CurrencyPocket")]
public class OttoPayPlugin : BaseUnityPlugin
{
    internal const string ModName = "OttoPay";
    internal const string ModVersion = "1.4.0";
    internal const string Author = "potto007";
    private const string ModGUID = $"{Author}.{ModName}";
    private static string ConfigFileName = $"{ModGUID}.cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal readonly Harmony _harmony = new(ModGUID);
    public static readonly ManualLogSource OttoPayLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    internal static Sprite DownloadSprite = null!;
    public static OttoPayPlugin instance = null!;
    private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = false};
    private FileSystemWatcher _watcher;
    private readonly object _reloadLock = new();
    private DateTime _lastConfigReloadTime;
    private const long RELOAD_DELAY = 10000000; // One second


    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    internal static ConfigEntry<bool> AllowValuableItems = null!;
    internal static ConfigEntry<string> AllowedValuablePrefabs = null!;
    internal static ConfigEntry<bool> PathwalkEnabled = null!;
    internal static ConfigEntry<float> PathwalkStaminaTrail = null!;
    internal static ConfigEntry<float> PathwalkStaminaRoad = null!;
    internal static ConfigEntry<bool> PathwalkShowStatusIcon = null!;
    internal static Sprite? PathwalkSprite;

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        bool saveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;

        instance = this;

        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        AllowValuableItems = Config.Bind("1 - General", "AllowValuableItems", true, "If enabled, items with a value greater than 0 can be converted to coins when dropped into the pocket.");
        AllowedValuablePrefabs = Config.Bind("1 - General", "AllowedValuablePrefabs", "", "Comma-separated list of prefab names that are allowed to be converted to coins (e.g., 'Ruby,Amber,AmberPearl'). If empty, all valuable items are allowed when AllowValuableItems is enabled.");

        PathwalkEnabled = config("2 - AuraPay Pathwalk", "Enabled", true, "If on, Merchant Bank members with AuraPay on drain less stamina while running on roads and trails. Also shows the AuraPay toggle when OttoAura is not installed.");
        PathwalkStaminaTrail = config("2 - AuraPay Pathwalk", "StaminaUsageTrail", 0.5f, new ConfigDescription("Run stamina drain on dirt paths, wood and metal, as a fraction of vanilla. 1 is vanilla, 0 is no drain.", new AcceptableValueRange<float>(0f, 1f)));
        PathwalkStaminaRoad = config("2 - AuraPay Pathwalk", "StaminaUsageRoad", 0f, new ConfigDescription("Run stamina drain on paved roads and stone, as a fraction of vanilla. 1 is vanilla, 0 is no drain.", new AcceptableValueRange<float>(0f, 1f)));
        PathwalkShowStatusIcon = config("2 - AuraPay Pathwalk", "ShowStatusIcon", true, "If on, the Pathwalk icon shows in the status bar while the effect is active.");
        PathwalkEnabled.SettingChanged += (_, _) => CurrencyPocket.UpdateAuraPayToggle();

        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();

        DownloadSprite = loadSprite("download.png");
        PathwalkSprite = loadSprite("pathwalk_icon.png");

        Config.Save();
        if (saveOnSet)
        {
            Config.SaveOnConfigSet = saveOnSet;
        }
    }

    public void Start()
    {
        RapidLoadoutsCompat.Init();
        ExtraSlotsCompat.FuckOff();
        PathwalkEffect.Init();
    }

    private void Update()
    {
        PathwalkEffect.Tick();
    }

    private void OnDestroy()
    {
        PathwalkEffect.Shutdown();
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
        if (time < RELOAD_DELAY)
        {
            return;
        }

        lock (_reloadLock)
        {
            if (!File.Exists(ConfigFileFullPath))
            {
                OttoPayLogger.LogWarning("Config file does not exist. Skipping reload.");
                return;
            }

            try
            {
                OttoPayLogger.LogDebug("Reloading configuration...");
                SaveWithRespectToConfigSet(true);
                OttoPayLogger.LogInfo("Configuration reload complete.");
            }
            catch (Exception ex)
            {
                OttoPayLogger.LogError($"Error reloading configuration: {ex.Message}");
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


    private static byte[] ReadEmbeddedFileBytes(string name)
    {
        using MemoryStream stream = new();
        Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + "." + name)!.CopyTo(stream);
        return stream.ToArray();
    }

    // UnityEngine.ImageConversionModule cannot be referenced from net48: its metadata
    // names ReadOnlySpan<byte>, which lives in the game's Mono mscorlib and not in the
    // net48 reference assemblies. The byte[] overload of LoadImage still exists, so it is
    // bound once at startup instead.
    private static readonly MethodInfo? LoadImageMethod = AccessTools.Method(
        "UnityEngine.ImageConversion:LoadImage", [typeof(Texture2D), typeof(byte[])]);

    private static Texture2D loadTexture(string name)
    {
        Texture2D texture = new(0, 0);
        if (LoadImageMethod == null)
        {
            OttoPayLogger.LogError("UnityEngine.ImageConversion.LoadImage was not found. Textures will not load.");
            return texture;
        }

        LoadImageMethod.Invoke(null, [texture, ReadEmbeddedFileBytes("assets." + name)]);
        return texture;
    }

    internal static Sprite loadSprite(string name)
    {
        Texture2D texture = loadTexture(name);
        return texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero) : null!;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }
}

public struct Constants
{
    public const string CoinPocketUIName = "CoinPocketUI";
    public const string ExtractCoinsButtonName = "ExtractCoinsButton";
    public const string AuraPayButtonName = "AuraPayToggleButton";
    public const string ArmorName = "Armor";
    public const string WeightName = "Weight";
    public const string JewelcraftingSynergyName = "Jewelcrafting Synergy";
    public const string TrashButtonName = "Trash";
    public const string FavoritingToggleButton = "favoritingTogglingButton";
    public const string QuickStackAreaButton = "quickStackAreaButton";
    public const string RestockAreaButton = "restockAreaButton";
    public const string SortInventoryButton = "sortInventoryButton";
    internal const string CoinCountCustomData = "CoinPocket_CoinCount";
    internal const string CoinIconName = "CoinIcon";
    internal const string CoinToken = "$item_coins";
    internal const string CoinsPrefabName = "Coins";
    internal const string AcText = "ac_text";
    internal const string ArmorIconName = "armor_icon";

    // GUIDS
    internal const string RandyQuickslots = "randyknapp.mods.equipmentandquickslots";
    internal const string AzuEPIGUID = "Azumatt.AzuExtendedPlayerInventory";
    internal const string QuickStackStoreGUID = "goldenrevolver.quick_stack_store";
    internal const string JewelcraftingGUID = "org.bepinex.plugins.jewelcrafting";
    internal const string RapidLoadoutsGUID = "Azumatt.RapidLoadouts";
    internal const string OttoAuraGUID = "potto007.OttoAura";
    internal const string ExtraSlotsGuid = "shudnal.ExtraSlots";
    internal const string EsSectionName = "Mods compatibility - Reduced inventory size";
}