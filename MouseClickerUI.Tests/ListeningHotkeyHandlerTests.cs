using MouseClickerUI.Models;
using MouseClickerUI.Services;

namespace MouseClickerUI.Tests;

public class ListeningHotkeyHandlerTests
{
    [Fact]
    public void Handle_EnablePressedInAutoDetectMode_ReturnsAutoDetectActions()
    {
        // Arrange
        var state = new ApplicationState();
        var handler = new ListeningHotkeyHandler(state, new FakeClock(new DateTime(2026, 1, 1, 12, 0, 0)));

        // Act
        var result = handler.Handle(
            isEnableListeningPressed: true,
            isDisableListeningPressed: false,
            isAutoDetectMode: true,
            isAutoDetectEnabled: true,
            targetProcessName: "notepad",
            autoDetectPaused: true);

        // Assert
        Assert.False(result.AutoDetectPaused);
        Assert.True(result.ShouldTryAutoDetect);
        Assert.False(result.StartListeningButtonEnabled);
        Assert.Equal("Auto-detect: waiting for notepad...", result.StatusMessage);
        Assert.True(state.PrevEnableListeningState);
        Assert.False(state.IsListening);
    }

    [Fact]
    public void Handle_EnableHeld_DoesNotRetriggerAutoDetectActions()
    {
        // Arrange
        var state = new ApplicationState { PrevEnableListeningState = true };
        var handler = new ListeningHotkeyHandler(state, new FakeClock(new DateTime(2026, 1, 1, 12, 0, 0)));

        // Act
        var result = handler.Handle(
            isEnableListeningPressed: true,
            isDisableListeningPressed: false,
            isAutoDetectMode: true,
            isAutoDetectEnabled: true,
            targetProcessName: "notepad",
            autoDetectPaused: true);

        // Assert
        Assert.True(result.AutoDetectPaused);
        Assert.False(result.ShouldTryAutoDetect);
        Assert.Null(result.StartListeningButtonEnabled);
        Assert.Null(result.StatusMessage);
    }

    [Fact]
    public void Handle_DisablePressedInAutoDetectMode_StopsAllAndEnablesStartButton()
    {
        // Arrange
        var state = new ApplicationState
        {
            IsListening = true,
            IsClicking = true,
            IsMouseMoving = true,
            IsRandomWasdEnabled = true
        };
        var handler = new ListeningHotkeyHandler(state, new FakeClock(new DateTime(2026, 1, 1, 12, 0, 0)));

        // Act
        var result = handler.Handle(
            isEnableListeningPressed: false,
            isDisableListeningPressed: true,
            isAutoDetectMode: true,
            isAutoDetectEnabled: true,
            targetProcessName: "notepad",
            autoDetectPaused: false);

        // Assert
        Assert.True(result.AutoDetectPaused);
        Assert.True(result.StartListeningButtonEnabled);
        Assert.Equal("Listening stopped - click Start Listening to resume auto-detect", result.StatusMessage);
        Assert.False(state.IsListening);
        Assert.False(state.IsClicking);
        Assert.False(state.IsMouseMoving);
        Assert.False(state.IsRandomWasdEnabled);
    }

    [Fact]
    public void Handle_EnablePressedInManualMode_StartsListening()
    {
        // Arrange
        var state = new ApplicationState();
        var time = new DateTime(2026, 1, 1, 12, 34, 56);
        var handler = new ListeningHotkeyHandler(state, new FakeClock(time));

        // Act
        var result = handler.Handle(
            isEnableListeningPressed: true,
            isDisableListeningPressed: false,
            isAutoDetectMode: false,
            isAutoDetectEnabled: true,
            targetProcessName: "notepad",
            autoDetectPaused: true);

        // Assert
        Assert.True(state.IsListening);
        Assert.Contains(time.ToString(), result.StatusMessage);
        Assert.False(result.ShouldTryAutoDetect);
        Assert.Null(result.StartListeningButtonEnabled);
    }

    [Fact]
    public void Handle_DisablePressedInManualMode_StopsAllAndSetsDisabledStatus()
    {
        // Arrange
        var state = new ApplicationState
        {
            IsListening = true,
            IsClicking = true
        };
        var time = new DateTime(2026, 1, 1, 13, 0, 0);
        var handler = new ListeningHotkeyHandler(state, new FakeClock(time));

        // Act
        var result = handler.Handle(
            isEnableListeningPressed: false,
            isDisableListeningPressed: true,
            isAutoDetectMode: false,
            isAutoDetectEnabled: false,
            targetProcessName: null,
            autoDetectPaused: false);

        // Assert
        Assert.True(result.AutoDetectPaused);
        Assert.Contains(time.ToString(), result.StatusMessage);
        Assert.False(state.IsListening);
        Assert.False(state.IsClicking);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; }
    }
}
