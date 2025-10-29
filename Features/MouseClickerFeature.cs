using MouseClickerUI.Services;

namespace MouseClickerUI.Features;

/// <summary>
/// Feature for automated mouse clicking.
/// </summary>
public class MouseClickerFeature : IFeature
{
    private readonly InputSimulator _inputSimulator;

    public MouseClickerFeature(InputSimulator inputSimulator)
    {
        _inputSimulator = inputSimulator;
    }

    public void Execute()
    {
        _inputSimulator.SimulateMouseClick();
    }

    public void Reset()
    {
        // No state to reset for clicking
    }
}
