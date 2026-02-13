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
}
