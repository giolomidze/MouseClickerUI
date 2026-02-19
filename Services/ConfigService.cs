using System.IO;
using System.Text.Json;
using MouseClickerUI.Models;

namespace MouseClickerUI.Services;

/// <summary>
/// Service for loading application configuration from external JSON file.
/// </summary>
public class ConfigService
{
    private const string ConfigFileName = "config.json";
    private const int MaxDetectionHistoryEntries = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads configuration from the config file.
    /// Returns default config if the file doesn't exist or can't be parsed.
    /// </summary>
    public AppConfig LoadConfig(string? configPath = null)
    {
        var isDefaultPath = configPath == null;
        configPath ??= GetConfigPath();

        // Migration: if using the default path and no config exists yet,
        // try to copy from the legacy location (next to the exe)
        if (isDefaultPath && !File.Exists(configPath))
        {
            TryMigrateLegacyConfig(configPath);
        }

        if (!File.Exists(configPath))
        {
            return new AppConfig();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            if (config == null)
            {
                return new AppConfig();
            }

            NormalizeConfig(config);
            return config;
        }
        catch
        {
            return new AppConfig();
        }
    }

    /// <summary>
    /// Saves the full configuration to the config file.
    /// </summary>
    public void SaveConfig(AppConfig config, string? configPath = null)
    {
        configPath ??= GetConfigPath();

        NormalizeConfig(config);

        var directory = Path.GetDirectoryName(configPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Saves the target process name to the config file.
    /// </summary>
    public void SaveTargetProcessName(string processName, string? configPath = null)
    {
        configPath ??= GetConfigPath();

        var config = LoadConfig(configPath);
        config.TargetProcessName = processName;
        SaveConfig(config, configPath);
    }

    /// <summary>
    /// Gets the full path to the config file in the user's local app data folder.
    /// Uses %LOCALAPPDATA%\MouseClickerUI\ which is writable by standard users,
    /// unlike Program Files where the installer places the executable.
    /// </summary>
    public static string GetConfigPath()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MouseClickerUI");
        return Path.Combine(appDataDir, ConfigFileName);
    }

    /// <summary>
    /// Gets the legacy config path next to the executable (used for migration).
    /// </summary>
    private static string GetLegacyConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, ConfigFileName);
    }

    /// <summary>
    /// Attempts to migrate config from the legacy path (next to exe) to the new path.
    /// Silently does nothing if the legacy file doesn't exist or the copy fails.
    /// </summary>
    private static void TryMigrateLegacyConfig(string newConfigPath)
    {
        try
        {
            var legacyPath = GetLegacyConfigPath();

            if (string.Equals(Path.GetFullPath(legacyPath), Path.GetFullPath(newConfigPath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!File.Exists(legacyPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(newConfigPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(legacyPath, newConfigPath, overwrite: false);
        }
        catch
        {
            // Migration is best-effort — if it fails, the app starts with fresh defaults
        }
    }

    private static void NormalizeConfig(AppConfig config)
    {
        config.HotkeyInputSource = HotkeyInputSources.Normalize(config.HotkeyInputSource);
        config.TargetProcessName = NormalizeProcessName(config.TargetProcessName);

        var history = config.DetectionHistory ?? new List<DetectionHistoryEntry>();

        config.DetectionHistory = history
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ProcessName))
            .Select(entry => new DetectionHistoryEntry
            {
                ProcessName = NormalizeProcessName(entry.ProcessName) ?? string.Empty,
                WindowTitle = entry.WindowTitle?.Trim() ?? string.Empty,
                LastSelectedAtUtc = entry.LastSelectedAtUtc == default ? DateTime.UtcNow : entry.LastSelectedAtUtc
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ProcessName))
            .OrderByDescending(entry => entry.LastSelectedAtUtc)
            .GroupBy(entry => entry.ProcessName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(MaxDetectionHistoryEntries)
            .ToList();
    }

    private static string? NormalizeProcessName(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return processName;
        }

        var normalized = processName.Trim();

        // Strip .exe extension if user included it for convenience
        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^4];
        }

        return normalized;
    }
}
