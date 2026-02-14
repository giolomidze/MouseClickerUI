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
        configPath ??= GetConfigPath();

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
    /// Gets the full path to the config file next to the executable.
    /// Uses AppContext.BaseDirectory which works correctly with single-file publish.
    /// </summary>
    public static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, ConfigFileName);
    }

    private static void NormalizeConfig(AppConfig config)
    {
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
