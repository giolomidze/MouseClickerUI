using MouseClickerUI.Win32;

namespace MouseClickerUI.Tests;

public class NumpadHotkeyBindingsTests
{
    [Fact]
    public void NumpadHotkeys_UseExpectedVirtualKeyCodes()
    {
        Assert.Equal((ushort)0x60, Constants.VK_NUMPAD0);
        Assert.Equal((ushort)0x61, Constants.VK_NUMPAD1);
        Assert.Equal((ushort)0x66, Constants.VK_NUMPAD6);
        Assert.Equal((ushort)0x67, Constants.VK_NUMPAD7);
        Assert.Equal((ushort)0x68, Constants.VK_NUMPAD8);
        Assert.Equal((ushort)0x69, Constants.VK_NUMPAD9);
    }

    [Fact]
    public void NumpadHotkeys_DoNotUseTopRowDigitVirtualKeyCodes()
    {
        Assert.NotEqual((ushort)0x30, Constants.VK_NUMPAD0); // Top-row '0'
        Assert.NotEqual((ushort)0x31, Constants.VK_NUMPAD1); // Top-row '1'
        Assert.NotEqual((ushort)0x36, Constants.VK_NUMPAD6); // Top-row '6'
        Assert.NotEqual((ushort)0x37, Constants.VK_NUMPAD7); // Top-row '7'
        Assert.NotEqual((ushort)0x38, Constants.VK_NUMPAD8); // Top-row '8'
        Assert.NotEqual((ushort)0x39, Constants.VK_NUMPAD9); // Top-row '9'
    }
}
