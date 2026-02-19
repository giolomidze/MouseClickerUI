using MouseClickerUI.Models;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Services;

public readonly record struct HotkeyKeySet(
    int EnableListening,
    int DisableListening,
    int EnableClicking,
    int DisableClicking,
    int ToggleMouseMovement,
    int ToggleRandomWasd);

public static class HotkeyMapping
{
    public static HotkeyKeySet GetKeys(string? hotkeyInputSource)
    {
        var normalizedSource = HotkeyInputSources.Normalize(hotkeyInputSource);

        if (string.Equals(normalizedSource, HotkeyInputSources.NumberRow, StringComparison.Ordinal))
        {
            return new HotkeyKeySet(
                Constants.VK_1,
                Constants.VK_0,
                Constants.VK_8,
                Constants.VK_9,
                Constants.VK_7,
                Constants.VK_6);
        }

        return new HotkeyKeySet(
            Constants.VK_NUMPAD1,
            Constants.VK_NUMPAD0,
            Constants.VK_NUMPAD8,
            Constants.VK_NUMPAD9,
            Constants.VK_NUMPAD7,
            Constants.VK_NUMPAD6);
    }
}
