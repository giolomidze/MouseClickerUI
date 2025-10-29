using MouseClickerUI.Models;

namespace MouseClickerUI.Tests;

public class ApplicationStateTests
{
    [Fact]
    public void ResetFeatures_ResetsAllFeatureFlags()
    {
        // Arrange
        var state = new ApplicationState
        {
            IsListening = true,
            IsClicking = true,
            IsMouseMoving = true,
            IsRandomWasdEnabled = true
        };

        // Act
        state.ResetFeatures();

        // Assert
        Assert.True(state.IsListening, "IsListening should not be affected by ResetFeatures");
        Assert.False(state.IsClicking);
        Assert.False(state.IsMouseMoving);
        Assert.False(state.IsRandomWasdEnabled);
    }

    [Fact]
    public void StopAll_ResetsListeningAndAllFeatures()
    {
        // Arrange
        var state = new ApplicationState
        {
            IsListening = true,
            IsClicking = true,
            IsMouseMoving = true,
            IsRandomWasdEnabled = true
        };

        // Act
        state.StopAll();

        // Assert
        Assert.False(state.IsListening);
        Assert.False(state.IsClicking);
        Assert.False(state.IsMouseMoving);
        Assert.False(state.IsRandomWasdEnabled);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var state = new ApplicationState();

        // Assert
        Assert.False(state.IsListening);
        Assert.False(state.IsClicking);
        Assert.False(state.IsMouseMoving);
        Assert.False(state.IsRandomWasdEnabled);
        Assert.Equal(0, state.TargetProcessId);
        Assert.Equal(IntPtr.Zero, state.TargetWindowHandle);
        Assert.Equal(string.Empty, state.TargetProcessName);
        Assert.Equal(string.Empty, state.TargetWindowTitle);
        Assert.Equal(1, state.ClickDelay);
    }
}
