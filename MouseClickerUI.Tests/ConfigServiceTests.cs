using MouseClickerUI.Models;
using MouseClickerUI.Services;

namespace MouseClickerUI.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void LoadConfig_FileDoesNotExist_ReturnsDefaultConfig()
    {
        // Arrange
        var service = new ConfigService();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");

        // Act
        var config = service.LoadConfig(nonExistentPath);

        // Assert
        Assert.NotNull(config);
        Assert.False(config.IsAutoDetectEnabled);
        Assert.Null(config.TargetProcessName);
    }

    [Fact]
    public void LoadConfig_ValidJson_ParsesTargetProcessName()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "targetProcessName": "notepad" }""");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.True(config.IsAutoDetectEnabled);
            Assert.Equal("notepad", config.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfig_TargetProcessNameWithExe_StripsExtension()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "targetProcessName": "notepad.exe" }""");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal("notepad", config.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfig_CaseInsensitivePropertyName_Parses()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "TargetProcessName": "notepad" }""");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal("notepad", config.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfig_InvalidJson_ReturnsDefaultConfig()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "not valid json {{{");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.NotNull(config);
            Assert.False(config.IsAutoDetectEnabled);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfig_EmptyTargetProcessName_ReturnsDisabledAutoDetect()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "targetProcessName": "" }""");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.False(config.IsAutoDetectEnabled);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadConfig_ExeExtensionCaseInsensitive_StripsExtension()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "targetProcessName": "MyApp.EXE" }""");

        try
        {
            // Act
            var config = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal("MyApp", config.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveTargetProcessName_WritesConfigFile()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            service.SaveTargetProcessName("notepad", tempFile);

            // Assert
            var config = service.LoadConfig(tempFile);
            Assert.Equal("notepad", config.TargetProcessName);
            Assert.True(config.IsAutoDetectEnabled);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveTargetProcessName_OverwritesExistingConfig()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, """{ "targetProcessName": "oldapp" }""");

        try
        {
            // Act
            service.SaveTargetProcessName("newapp", tempFile);

            // Assert
            var config = service.LoadConfig(tempFile);
            Assert.Equal("newapp", config.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_WithDetectionHistory_RoundTrips()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            TargetProcessName = "notepad",
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "Untitled - Notepad", LastSelectedAtUtc = DateTime.UtcNow },
                new() { ProcessName = "chrome", WindowTitle = "Google Chrome", LastSelectedAtUtc = DateTime.UtcNow.AddMinutes(-5) }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal(2, loaded.DetectionHistory.Count);
            Assert.Equal("notepad", loaded.DetectionHistory[0].ProcessName);
            Assert.Equal("Untitled - Notepad", loaded.DetectionHistory[0].WindowTitle);
            Assert.Equal("chrome", loaded.DetectionHistory[1].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_DeduplicatesByProcessName()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var now = DateTime.UtcNow;
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "Old Title", LastSelectedAtUtc = now.AddMinutes(-10) },
                new() { ProcessName = "Notepad", WindowTitle = "New Title", LastSelectedAtUtc = now }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Single(loaded.DetectionHistory);
            Assert.Equal("New Title", loaded.DetectionHistory[0].WindowTitle);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_OrdersByLastSelectedDescending()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var now = DateTime.UtcNow;
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "older", WindowTitle = "Older App", LastSelectedAtUtc = now.AddHours(-1) },
                new() { ProcessName = "newest", WindowTitle = "Newest App", LastSelectedAtUtc = now },
                new() { ProcessName = "middle", WindowTitle = "Middle App", LastSelectedAtUtc = now.AddMinutes(-30) }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal(3, loaded.DetectionHistory.Count);
            Assert.Equal("newest", loaded.DetectionHistory[0].ProcessName);
            Assert.Equal("middle", loaded.DetectionHistory[1].ProcessName);
            Assert.Equal("older", loaded.DetectionHistory[2].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_LimitsTo50Entries()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var now = DateTime.UtcNow;
        var config = new AppConfig
        {
            DetectionHistory = Enumerable.Range(0, 60)
                .Select(i => new DetectionHistoryEntry
                {
                    ProcessName = $"process{i}",
                    WindowTitle = $"Window {i}",
                    LastSelectedAtUtc = now.AddMinutes(-i)
                })
                .ToList()
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal(50, loaded.DetectionHistory.Count);
            Assert.Equal("process0", loaded.DetectionHistory[0].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_StripsExeFromProcessName()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad.exe", WindowTitle = "Notepad", LastSelectedAtUtc = DateTime.UtcNow }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Single(loaded.DetectionHistory);
            Assert.Equal("notepad", loaded.DetectionHistory[0].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_FiltersEmptyProcessNames()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "", WindowTitle = "Empty", LastSelectedAtUtc = DateTime.UtcNow },
                new() { ProcessName = "  ", WindowTitle = "Whitespace", LastSelectedAtUtc = DateTime.UtcNow },
                new() { ProcessName = "valid", WindowTitle = "Valid App", LastSelectedAtUtc = DateTime.UtcNow }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Single(loaded.DetectionHistory);
            Assert.Equal("valid", loaded.DetectionHistory[0].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveTargetProcessName_PreservesExistingDetectionHistory()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            TargetProcessName = "oldapp",
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "chrome", WindowTitle = "Google Chrome", LastSelectedAtUtc = DateTime.UtcNow },
                new() { ProcessName = "firefox", WindowTitle = "Mozilla Firefox", LastSelectedAtUtc = DateTime.UtcNow.AddMinutes(-5) }
            }
        };

        try
        {
            service.SaveConfig(config, tempFile);

            // Act
            service.SaveTargetProcessName("newapp", tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal("newapp", loaded.TargetProcessName);
            Assert.Equal(2, loaded.DetectionHistory.Count);
            Assert.Equal("chrome", loaded.DetectionHistory[0].ProcessName);
            Assert.Equal("firefox", loaded.DetectionHistory[1].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_DefaultDateTime_GetsReplacedWithUtcNow()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "Notepad", LastSelectedAtUtc = default }
            }
        };

        try
        {
            // Act
            var before = DateTime.UtcNow;
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Single(loaded.DetectionHistory);
            Assert.True(loaded.DetectionHistory[0].LastSelectedAtUtc >= before.AddSeconds(-1));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_DetectionHistory_TrimsWhitespaceFromWindowTitle()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "  My App  ", LastSelectedAtUtc = DateTime.UtcNow }
            }
        };

        try
        {
            // Act
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Single(loaded.DetectionHistory);
            Assert.Equal("My App", loaded.DetectionHistory[0].WindowTitle);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_AfterRemovingEntry_PersistsWithoutRemovedEntry()
    {
        // Arrange — simulate the UI flow: save first (NormalizeConfig rebuilds list),
        // then remove from the normalized list, then save again
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var now = DateTime.UtcNow;
        var config = new AppConfig
        {
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "Notepad", LastSelectedAtUtc = now },
                new() { ProcessName = "chrome", WindowTitle = "Google Chrome", LastSelectedAtUtc = now.AddMinutes(-5) },
                new() { ProcessName = "firefox", WindowTitle = "Firefox", LastSelectedAtUtc = now.AddMinutes(-10) }
            }
        };

        try
        {
            service.SaveConfig(config, tempFile);

            // Act — remove from the normalized list (as the UI would via SelectedItem)
            var entryToRemove = config.DetectionHistory.First(e => e.ProcessName == "chrome");
            config.DetectionHistory.Remove(entryToRemove);
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Equal(2, loaded.DetectionHistory.Count);
            Assert.Equal("notepad", loaded.DetectionHistory[0].ProcessName);
            Assert.Equal("firefox", loaded.DetectionHistory[1].ProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveConfig_AfterRemovingAllEntries_PersistsEmptyHistory()
    {
        // Arrange
        var service = new ConfigService();
        var tempFile = Path.GetTempFileName();
        var config = new AppConfig
        {
            TargetProcessName = "notepad",
            DetectionHistory = new List<DetectionHistoryEntry>
            {
                new() { ProcessName = "notepad", WindowTitle = "Notepad", LastSelectedAtUtc = DateTime.UtcNow }
            }
        };

        try
        {
            service.SaveConfig(config, tempFile);

            // Act — remove from normalized list, then save
            var entry = config.DetectionHistory.Single();
            config.DetectionHistory.Remove(entry);
            service.SaveConfig(config, tempFile);
            var loaded = service.LoadConfig(tempFile);

            // Assert
            Assert.Empty(loaded.DetectionHistory);
            Assert.Equal("notepad", loaded.TargetProcessName);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
