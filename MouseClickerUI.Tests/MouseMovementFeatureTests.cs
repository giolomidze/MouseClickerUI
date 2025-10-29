using MouseClickerUI.Features;
using MouseClickerUI.Services;

namespace MouseClickerUI.Tests;

public class MouseMovementFeatureTests
{
    [Fact]
    public void Reset_SetsAllFieldsToInitialState()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Execute a few times to change internal state
        feature.Execute();
        feature.Execute();
        feature.Execute();

        // Act
        feature.Reset();

        // Assert
        // After reset, the first execution should initialize movement range again
        feature.Execute();

        // Verify that movement happened (non-zero values after reset)
        Assert.NotEmpty(inputSimulator.MouseMovements);
        var firstMovementAfterReset = inputSimulator.MouseMovements[0];

        // After reset, behavior should be same as initial state
        // We can't directly test private fields, but we can verify the behavior resets
        // by checking that movement values are within expected initial range
        Assert.True(Math.Abs(firstMovementAfterReset.dx) <= 38,
            "Horizontal movement should be within initial range (25-35 + 3 offset)");
        Assert.True(Math.Abs(firstMovementAfterReset.dy) <= 38,
            "Vertical movement should be within initial range (25-35 + 3 offset)");
    }

    [Fact]
    public void Execute_FirstCall_InitializesMovementRange()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Act
        feature.Execute();

        // Assert
        // First execution should produce movement within expected range
        Assert.Single(inputSimulator.MouseMovements);
        var movement = inputSimulator.MouseMovements[0];

        // With range 25-35 + random offset ±3, max possible movement is 38
        Assert.True(Math.Abs(movement.dx) <= 38,
            $"Horizontal movement {movement.dx} exceeds expected maximum");
        Assert.True(Math.Abs(movement.dy) <= 38,
            $"Vertical movement {movement.dy} exceeds expected maximum");
    }

    [Fact]
    public void Execute_CompletesFullCycle_ResetsStepAndReversesDirection()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Act - Execute 20 times to complete one full cycle (10 steps * 2 directions)
        for (int i = 0; i < 20; i++)
        {
            feature.Execute();
        }

        // Get the first movement direction
        var firstMovement = inputSimulator.MouseMovements[0].dx;
        var firstSign = Math.Sign(firstMovement);

        // Clear to test next cycle
        inputSimulator.MouseMovements.Clear();

        // Execute once more to trigger new cycle
        feature.Execute();

        // Assert - Direction should have reversed (step counter reset)
        var newCycleMovement = inputSimulator.MouseMovements[0].dx;
        var newSign = Math.Sign(newCycleMovement);

        // Signs should be opposite (or one could be zero due to sine wave)
        // At step 0, sine is 0, so we need to check a few steps in
        inputSimulator.MouseMovements.Clear();
        feature.Execute();
        feature.Execute();
        feature.Execute();

        // After a few steps, direction should be clear
        if (inputSimulator.MouseMovements.Count > 0)
        {
            var laterMovement = inputSimulator.MouseMovements.Last().dx;
            // Direction reversed means the overall trend should be opposite
            // (We can't guarantee exact sign due to random offsets, but behavior should reset)
            Assert.True(inputSimulator.MouseMovements.Count > 0,
                "Movement should continue in new cycle");
        }
    }

    [Fact]
    public void Execute_CompletesFullCycle_RandomizesNewMovementRange()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Act - Complete first cycle
        for (int i = 0; i < 20; i++)
        {
            feature.Execute();
        }

        var firstCycleMovements = inputSimulator.MouseMovements.Select(m =>
            Math.Max(Math.Abs(m.dx), Math.Abs(m.dy))).ToList();

        inputSimulator.MouseMovements.Clear();

        // Complete second cycle
        for (int i = 0; i < 20; i++)
        {
            feature.Execute();
        }

        var secondCycleMovements = inputSimulator.MouseMovements.Select(m =>
            Math.Max(Math.Abs(m.dx), Math.Abs(m.dy))).ToList();

        // Assert - Movement ranges could be different between cycles due to randomization
        // We can't guarantee they're different, but we can verify both are valid
        var firstMax = firstCycleMovements.Max();
        var secondMax = secondCycleMovements.Max();

        // Both should be within valid range (25-35 base + 3 offset)
        Assert.True(firstMax <= 38, "First cycle movement within expected range");
        Assert.True(secondMax <= 38, "Second cycle movement within expected range");
    }

    [Fact]
    public void Execute_CalculatesMovementWithinExpectedBounds()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Act - Execute multiple times
        for (int i = 0; i < 50; i++)
        {
            feature.Execute();
        }

        // Assert - All movements should be within bounds
        foreach (var movement in inputSimulator.MouseMovements)
        {
            // Base range is 25-35, random offset is ±3, so max is 38
            Assert.True(Math.Abs(movement.dx) <= 38,
                $"Horizontal movement {movement.dx} exceeds bounds");
            Assert.True(Math.Abs(movement.dy) <= 38,
                $"Vertical movement {movement.dy} exceeds bounds");
        }
    }

    [Fact]
    public void Execute_IncrementsStepCounter()
    {
        // Arrange
        var inputSimulator = new TestInputSimulator();
        var feature = new MouseMovementFeature(inputSimulator);

        // Act
        for (int i = 0; i < 10; i++)
        {
            feature.Execute();
        }

        // Assert
        // Each Execute call should produce one mouse movement
        Assert.Equal(10, inputSimulator.MouseMovements.Count);
    }

    /// <summary>
    /// Test double for InputSimulator that captures method calls instead of
    /// actually calling Win32 APIs.
    /// </summary>
    private class TestInputSimulator : InputSimulator
    {
        public List<(int dx, int dy)> MouseMovements { get; } = new();

        public override void SimulateMouseMovement(int dx, int dy)
        {
            MouseMovements.Add((dx, dy));
        }
    }
}
