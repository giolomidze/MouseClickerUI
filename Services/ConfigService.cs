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

            // Strip .exe extension if user included it for convenience
            if (config.TargetProcessName != null &&
                config.TargetProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                config.TargetProcessName = config.TargetProcessName[..^4];
            }

            return config;
        }
        catch
        {
            return new AppConfig();
        }
    }

    /// <summary>
    /// Saves the target process name to the config file.
    /// </summary>
    public void SaveTargetProcessName(string processName, string? configPath = null)
    {
        configPath ??= GetConfigPath();
        var config = new AppConfig { TargetProcessName = processName };
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    /// <summary>
    /// Gets the full path to the config file next to the executable.
    /// Uses AppContext.BaseDirectory which works correctly with single-file publish.
    /// </summary>
    public static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, ConfigFileName);
    }
}
