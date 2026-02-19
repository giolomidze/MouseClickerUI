using MouseClickerUI.Models;
using MouseClickerUI.Services;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Tests;

public class HotkeyMappingTests
{
    [Fact]
    public void GetKeys_NumPad_ReturnsNumPadVirtualKeys()
    {
        var keys = HotkeyMapping.GetKeys(HotkeyInputSources.NumPad);

        Assert.Equal(Constants.VK_NUMPAD1, keys.EnableListening);
        Assert.Equal(Constants.VK_NUMPAD0, keys.DisableListening);
        Assert.Equal(Constants.VK_NUMPAD8, keys.EnableClicking);
        Assert.Equal(Constants.VK_NUMPAD9, keys.DisableClicking);
        Assert.Equal(Constants.VK_NUMPAD7, keys.ToggleMouseMovement);
        Assert.Equal(Constants.VK_NUMPAD6, keys.ToggleRandomWasd);
    }

    [Fact]
    public void GetKeys_NumberRow_ReturnsTopRowVirtualKeys()
    {
        var keys = HotkeyMapping.GetKeys(HotkeyInputSources.NumberRow);

        Assert.Equal(Constants.VK_1, keys.EnableListening);
        Assert.Equal(Constants.VK_0, keys.DisableListening);
        Assert.Equal(Constants.VK_8, keys.EnableClicking);
        Assert.Equal(Constants.VK_9, keys.DisableClicking);
        Assert.Equal(Constants.VK_7, keys.ToggleMouseMovement);
        Assert.Equal(Constants.VK_6, keys.ToggleRandomWasd);
    }

    [Fact]
    public void GetKeys_UnknownSource_DefaultsToNumPad()
    {
        var keys = HotkeyMapping.GetKeys("unsupported");

        Assert.Equal(Constants.VK_NUMPAD1, keys.EnableListening);
        Assert.Equal(Constants.VK_NUMPAD0, keys.DisableListening);
        Assert.Equal(Constants.VK_NUMPAD8, keys.EnableClicking);
        Assert.Equal(Constants.VK_NUMPAD9, keys.DisableClicking);
        Assert.Equal(Constants.VK_NUMPAD7, keys.ToggleMouseMovement);
        Assert.Equal(Constants.VK_NUMPAD6, keys.ToggleRandomWasd);
    }
}
