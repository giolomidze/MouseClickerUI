namespace MouseClickerUI.Features;

/// <summary>
/// Interface for automation features.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// Executes the feature's action.
    /// </summary>
    void Execute();

    /// <summary>
    /// Resets the feature's state.
    /// </summary>
    void Reset();
}
