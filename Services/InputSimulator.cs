using System.Diagnostics;
using System.Runtime.InteropServices;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Services;

/// <summary>
/// Service for simulating mouse and keyboard input via Win32 API.
/// </summary>
public class InputSimulator
{
    /// <summary>
    /// Simulates a left mouse click.
    /// </summary>
    public void SimulateMouseClick()
    {
        NativeMethods.mouse_event(Constants.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        NativeMethods.mouse_event(Constants.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates relative mouse movement.
    /// </summary>
    /// <param name="dx">Horizontal movement in pixels</param>
    /// <param name="dy">Vertical movement in pixels</param>
    public void SimulateMouseMovement(int dx, int dy)
    {
        NativeMethods.mouse_event(Constants.MOUSEEVENTF_MOVE, dx, dy, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates a key press (down + up) for the specified virtual key code.
    /// </summary>
    /// <param name="virtualKeyCode">Virtual key code to press</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool SimulateKeyPress(ushort virtualKeyCode)
    {
        INPUT[] inputs = new INPUT[2];

        // Key down event
        inputs[0] = new INPUT
        {
            type = Constants.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up event
        inputs[1] = new INPUT
        {
            type = Constants.INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    wScan = 0,
                    dwFlags = Constants.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Send both key down and key up events
        // Marshal.SizeOf correctly calculates size including padding (40 bytes on 64-bit)
        uint result = NativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));

        // SendInput returns the number of events successfully sent
        // Should be 2 (key down + key up). If not, the send failed.
        if (result != 2)
        {
            // SendInput failed - check the error code
            uint errorCode = NativeMethods.GetLastError();
            Debug.WriteLine($"[SendInput] Failed to send key press. Expected 2 events, got {result}. Error code: {errorCode} (0x{errorCode:X8})");
            // Common error codes:
            // 5 = ACCESS_DENIED (requires admin or proper permissions)
            // 87 = ERROR_INVALID_PARAMETER (invalid INPUT structure)
            // 578 = ERROR_HOOK_TYPE_INCOMPATIBLE (UIPI blocking)
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a specific key is currently pressed.
    /// </summary>
    /// <param name="keyCode">Virtual key code to check</param>
    /// <returns>True if the key is pressed, false otherwise</returns>
    public bool IsKeyPressed(int keyCode)
    {
        return (NativeMethods.GetKeyState(keyCode) & 0x8000) != 0;
    }
}
