using System.Diagnostics;
using System.Runtime.InteropServices;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Services;

/// <summary>
/// Service for simulating mouse and keyboard input via Win32 SendInput API.
/// </summary>
public class InputSimulator
{
    private static readonly int InputSize = Marshal.SizeOf(typeof(INPUT));

    /// <summary>
    /// Simulates a left mouse click (down + up) as a single atomic SendInput call.
    /// </summary>
    public virtual void SimulateMouseClick()
    {
        INPUT[] inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = Constants.INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = Constants.MOUSEEVENTF_LEFTDOWN,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = Constants.INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = Constants.MOUSEEVENTF_LEFTUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        uint result = NativeMethods.SendInput(2, inputs, InputSize);
        if (result != 2)
        {
            uint errorCode = NativeMethods.GetLastError();
            Debug.WriteLine($"[SendInput] Failed to send mouse click. Expected 2 events, got {result}. Error code: {errorCode} (0x{errorCode:X8})");
        }
    }

    /// <summary>
    /// Simulates relative mouse movement via SendInput.
    /// </summary>
    /// <param name="dx">Horizontal movement in pixels</param>
    /// <param name="dy">Vertical movement in pixels</param>
    public virtual void SimulateMouseMovement(int dx, int dy)
    {
        INPUT[] inputs = new INPUT[1];

        inputs[0] = new INPUT
        {
            type = Constants.INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = 0,
                    dwFlags = Constants.MOUSEEVENTF_MOVE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        uint result = NativeMethods.SendInput(1, inputs, InputSize);
        if (result != 1)
        {
            uint errorCode = NativeMethods.GetLastError();
            Debug.WriteLine($"[SendInput] Failed to send mouse movement. Expected 1 event, got {result}. Error code: {errorCode} (0x{errorCode:X8})");
        }
    }

    /// <summary>
    /// Simulates a key press (down + up) for the specified virtual key code.
    /// Includes scan codes via MapVirtualKey for better compatibility with DirectInput apps.
    /// </summary>
    /// <param name="virtualKeyCode">Virtual key code to press</param>
    /// <returns>True if successful, false otherwise</returns>
    public virtual bool SimulateKeyPress(ushort virtualKeyCode)
    {
        ushort scanCode = (ushort)NativeMethods.MapVirtualKey(virtualKeyCode, Constants.MAPVK_VK_TO_VSC);

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
                    wScan = scanCode,
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
                    wScan = scanCode,
                    dwFlags = Constants.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Send both key down and key up events
        uint result = NativeMethods.SendInput(2, inputs, InputSize);

        if (result != 2)
        {
            uint errorCode = NativeMethods.GetLastError();
            Debug.WriteLine($"[SendInput] Failed to send key press. Expected 2 events, got {result}. Error code: {errorCode} (0x{errorCode:X8})");
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
