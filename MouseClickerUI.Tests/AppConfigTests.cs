using MouseClickerUI.Models;

namespace MouseClickerUI.Tests;

public class AppConfigTests
{
    [Fact]
    public void IsAutoDetectEnabled_NullTargetProcessName_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfig { TargetProcessName = null };

        // Act & Assert
        Assert.False(config.IsAutoDetectEnabled);
    }

    [Fact]
    public void IsAutoDetectEnabled_EmptyTargetProcessName_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfig { TargetProcessName = "" };

        // Act & Assert
        Assert.False(config.IsAutoDetectEnabled);
    }

    [Fact]
    public void IsAutoDetectEnabled_WhitespaceTargetProcessName_ReturnsFalse()
    {
        // Arrange
        var config = new AppConfig { TargetProcessName = "   " };

        // Act & Assert
        Assert.False(config.IsAutoDetectEnabled);
    }

    [Fact]
    public void IsAutoDetectEnabled_ValidTargetProcessName_ReturnsTrue()
    {
        // Arrange
        var config = new AppConfig { TargetProcessName = "notepad" };

        // Act & Assert
        Assert.True(config.IsAutoDetectEnabled);
    }

    [Fact]
    public void Constructor_DefaultTargetProcessName_IsNull()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.Null(config.TargetProcessName);
        Assert.False(config.IsAutoDetectEnabled);
    }
}
