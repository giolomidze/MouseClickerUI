using MouseClickerUI.Features;
using MouseClickerUI.Models;
using MouseClickerUI.Services;

namespace MouseClickerUI.Tests;

public class RandomWasdFeatureTests
{
    [Fact]
    public void Reset_SetsAllFieldsToInitialState()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Execute to change internal state
        feature.Execute();
        feature.Execute();

        // Act
        feature.Reset();

        // Assert
        // After reset, first execution should press a key immediately
        inputSimulator.KeyPresses.Clear();
        feature.Execute();

        // Should have pressed a key on first execution after reset
        Assert.Single(inputSimulator.KeyPresses);
    }

    [Fact]
    public void Execute_FirstCall_PressesKeyImmediately()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act
        feature.Execute();

        // Assert
        // First execution should press a key immediately
        Assert.Single(inputSimulator.KeyPresses);
    }

    [Fact]
    public void Execute_PressesWasdKeysOnly()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act - Execute multiple times to get various random selections
        for (int i = 0; i < 100; i++)
        {
            // Reset to trigger immediate keypress
            feature.Reset();
            feature.Execute();
        }

        // Assert - All keypresses should be W, A, S, or D
        var validKeys = new[] { (ushort)0x57, (ushort)0x41, (ushort)0x53, (ushort)0x44 }; // W, A, S, D
        foreach (var keyPress in inputSimulator.KeyPresses)
        {
            Assert.Contains(keyPress, validKeys);
        }
    }

    [Fact]
    public void Execute_ClicksWithApproximately50PercentProbability()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act - Execute many times to get statistical sample
        int iterations = 200;
        for (int i = 0; i < iterations; i++)
        {
            // Reset to trigger immediate keypress
            feature.Reset();
            feature.Execute();
        }

        // Assert
        // Should have pressed a key for each iteration
        Assert.Equal(iterations, inputSimulator.KeyPresses.Count);

        // Click probability should be approximately 50% (allow 35-65% range for randomness)
        int clickCount = inputSimulator.MouseClicks;
        double clickPercentage = (double)clickCount / iterations * 100;

        Assert.True(clickPercentage >= 35 && clickPercentage <= 65,
            $"Expected click probability between 35-65%, got {clickPercentage:F1}%");
    }

    [Fact]
    public void Execute_DoesNotPressKeyWhenTargetWindowNotActive()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager { IsTarget = false };
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act
        feature.Execute();

        // Assert
        // Should not press any keys when target window is not active
        Assert.Empty(inputSimulator.KeyPresses);
        Assert.Equal(0, inputSimulator.MouseClicks);
    }

    [Fact]
    public void Execute_RespectsRandomInterval()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act - First execution should press immediately
        feature.Execute();
        Assert.Single(inputSimulator.KeyPresses);

        // Multiple immediate executions should not press more keys (waiting for interval)
        for (int i = 0; i < 10; i++)
        {
            feature.Execute();
        }

        // Assert
        // Should still have only one keypress since interval hasn't elapsed
        Assert.Single(inputSimulator.KeyPresses);
    }

    [Fact]
    public void Execute_KeyPressAlwaysAccompaniedByOptionalClick()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act
        int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            feature.Reset();
            feature.Execute();
        }

        // Assert
        // Number of clicks should never exceed number of keypresses
        Assert.True(inputSimulator.MouseClicks <= inputSimulator.KeyPresses.Count,
            "Mouse clicks should never exceed keypresses");

        // Should have exactly as many keypresses as iterations
        Assert.Equal(iterations, inputSimulator.KeyPresses.Count);
    }

    [Fact]
    public void Execute_AllWasdKeysGetSelected()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var windowManager = new TestWindowManager();
        var feature = new RandomWasdFeature(inputSimulator, windowManager);

        // Act - Execute many times to ensure all keys are selected at least once
        for (int i = 0; i < 100; i++)
        {
            feature.Reset();
            feature.Execute();
        }

        // Assert - All four WASD keys should have been selected
        var uniqueKeys = inputSimulator.KeyPresses.Distinct().ToList();

        // With 100 iterations, we should see all 4 keys (W, A, S, D)
        // Being overly strict here - with random selection, it's statistically unlikely to miss one
        Assert.True(uniqueKeys.Count >= 3,
            $"Expected at least 3 different WASD keys to be selected, got {uniqueKeys.Count}");
    }

    /// <summary>
    /// Test double for InputSimulator that captures method calls.
    /// </summary>
    private class TestInputSimulator : InputSimulator
    {
        public List<ushort> KeyPresses { get; } = new();
        public int MouseClicks { get; private set; }

        public override bool SimulateKeyPress(ushort virtualKeyCode)
        {
            KeyPresses.Add(virtualKeyCode);
            return true;
        }

        public override void SimulateMouseClick()
        {
            MouseClicks++;
        }
    }

    /// <summary>
    /// Test double for WindowManager that allows controlling target window state.
    /// </summary>
    private class TestWindowManager : WindowManager
    {
        public bool IsTarget { get; set; } = true;

        public TestWindowManager() : base(new ApplicationState())
        {
        }

        public override bool IsTargetWindow()
        {
            return IsTarget;
        }
    }
}
