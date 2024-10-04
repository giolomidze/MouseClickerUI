using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MouseClickerUI
{
    public partial class MainWindow : Window
    {
        private static bool _clicking;
        private static bool _listening;
        private static bool _prevEnableListeningState;
        private static bool _prevDisableListeningState;
        private static bool _prevEnableClickingState;
        private static bool _prevDisableClickingState;
        private static bool _debugEnabled;
        private static string _targetWindowTitle = string.Empty;
        private static int _clickDelay = 100;
        private static int _clickXCoordinate = 2511;
        private static int _clickYCoordinate = 1117;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _pollingTimer;
        private List<string> _cachedProcessNames = new List<string>();

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private const uint MouseEventLeftDown = 0x02;
        private const uint MouseEventLeftUp = 0x04;
        private const uint MouseEventRightDown = 0x08;
        private const uint MouseEventRightUp = 0x10;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private static bool IsKeyPressed(int keyCode)
        {
            return (GetKeyState(keyCode) & 0x8000) != 0;
        }

        private static bool IsTargetWindow()
        {
            if (string.IsNullOrEmpty(_targetWindowTitle))
            {
                return false;
            }

            var foregroundWindow = GetForegroundWindow();
            var windowText = new StringBuilder(256);
            return GetWindowText(foregroundWindow, windowText, windowText.Capacity) > 0 &&
                   windowText.ToString().Contains(_targetWindowTitle);
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadProcesses();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += PollingTimer_Tick;
            _pollingTimer.Start();

            ComboBoxProcesses.GotFocus += ComboBoxProcesses_GotFocus;
        }

        private void ComboBoxProcesses_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBlockValidationMessage.Visibility = Visibility.Collapsed;
        }

        private void LoadProcesses(string selectedProcessName = null)
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .OrderBy(p => p.ProcessName)
                .ToList();

            var processNames = processes.Select(p => p.ProcessName).ToList();

            if (_cachedProcessNames.SequenceEqual(processNames)) return;
            _cachedProcessNames = processNames;
            ComboBoxProcesses.ItemsSource = processes;
            ComboBoxProcesses.DisplayMemberPath = "MainWindowTitle";
            ComboBoxProcesses.SelectedValuePath = "ProcessName";

            if (!string.IsNullOrEmpty(selectedProcessName))
            {
                ComboBoxProcesses.SelectedValue = selectedProcessName;
            }
        }

        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            var selectedProcess = ComboBoxProcesses.SelectedItem as Process;
            var selectedProcessName = selectedProcess?.ProcessName;
            LoadProcesses(selectedProcessName);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateMouseClickingState();
            CheckMouseSideButtonClick();
        }

        private void UpdateMouseClickingState()
        {
            if (!IsTargetWindow())
            {
                return;
            }

            var isKey1Pressed = IsKeyPressed(0x31); // Key '1'
            var isKey0Pressed = IsKeyPressed(0x30); // Key '0'
            var isKey8Pressed = IsKeyPressed(0x38); // Key '8'
            var isKey9Pressed = IsKeyPressed(0x39); // Key '9'

            if (isKey1Pressed && !_prevEnableListeningState)
            {
                _listening = true;
                LabelStatus.Content = $"Listening enabled at {DateTime.Now}";
            }

            _prevEnableListeningState = isKey1Pressed;

            if (isKey0Pressed && !_prevDisableListeningState)
            {
                _listening = false;
                _clicking = false;
                LabelStatus.Content = $"Listening disabled at {DateTime.Now}";
            }

            _prevDisableListeningState = isKey0Pressed;

            if (_listening && isKey8Pressed && !_prevEnableClickingState)
            {
                _clicking = true;
                LabelStatus.Content = $"Mouse clicking enabled at {DateTime.Now}";
            }

            _prevEnableClickingState = isKey8Pressed;

            if (_listening && isKey9Pressed && !_prevDisableClickingState)
            {
                _clicking = false;
                LabelStatus.Content = $"Mouse clicking disabled at {DateTime.Now}";
            }

            _prevDisableClickingState = isKey9Pressed;

            if (_clicking)
            {
                SimulateMouseClick();
            }
        }

        private static async void SimulateMouseClick()
        {
            mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(_clickDelay);
        }

        private void CheckMouseSideButtonClick()
        {
            if (!_listening)
            {
                return;
            }

            // Virtual Key Codes for Side Buttons (XButton1 and XButton2)
            const int VK_XBUTTON1 = 0x05;
            const int VK_XBUTTON2 = 0x06;

            if (IsKeyPressed(VK_XBUTTON1))
            {
                TriggerMouseActions("XButton1");
            }
            else if (IsKeyPressed(VK_XBUTTON2))
            {
                TriggerMouseActions("XButton2");
            }
        }

        private async void TriggerMouseActions(string buttonName)
        {
            if (!_listening || !IsTargetWindow())
            {
                return;
            }

            if (_debugEnabled)
            {
                LabelStatus.Content = $"{buttonName} clicked at {DateTime.Now}";
                Debug.WriteLine($"{buttonName} clicked at {DateTime.Now}");
            }

            // Trigger right-click
            mouse_event(MouseEventRightDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventRightUp, 0, 0, 0, UIntPtr.Zero);

            // Add delay to allow window to appear
            await Task.Delay(10);

            // Move mouse to specified coordinates
            SetCursorPos(_clickXCoordinate, _clickYCoordinate);

            // Trigger left-click 5 times
            for (int i = 0; i < 5; i++)
            {
                mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
            }
        }

        private void SliderDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBoxDelay != null)
            {
                _clickDelay = (int)e.NewValue;
                TextBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(TextBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)SliderDelay.Minimum)
                {
                    newDelay = (int)SliderDelay.Minimum;
                }
                else if (newDelay > (int)SliderDelay.Maximum)
                {
                    newDelay = (int)SliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                SliderDelay.Value = newDelay;
            }
            else
            {
                TextBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TextBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)SliderDelay.Minimum)
                {
                    newDelay = (int)SliderDelay.Minimum;
                }
                else if (newDelay > (int)SliderDelay.Maximum)
                {
                    newDelay = (int)SliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                SliderDelay.Value = newDelay;
            }
            else
            {
                TextBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxXCoordinate_KeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(TextBoxXCoordinate.Text, out int newXCoordinate))
            {
                _clickXCoordinate = newXCoordinate;
            }
            else
            {
                TextBoxXCoordinate.Text = _clickXCoordinate.ToString();
            }
        }

        private void TextBoxXCoordinate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TextBoxXCoordinate.Text, out int newXCoordinate))
            {
                _clickXCoordinate = newXCoordinate;
            }
            else
            {
                TextBoxXCoordinate.Text = _clickXCoordinate.ToString();
            }
        }

        private void TextBoxYCoordinate_KeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(TextBoxYCoordinate.Text, out int newYCoordinate))
            {
                _clickYCoordinate = newYCoordinate;
            }
            else
            {
                TextBoxYCoordinate.Text = _clickYCoordinate.ToString();
            }
        }

        private void TextBoxYCoordinate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TextBoxYCoordinate.Text, out int newYCoordinate))
            {
                _clickYCoordinate = newYCoordinate;
            }
            else
            {
                TextBoxYCoordinate.Text = _clickYCoordinate.ToString();
            }
        }

        private void buttonStartListening_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxProcesses.SelectedItem == null)
            {
                TextBlockValidationMessage.Text = "Please select an application first.";
                TextBlockValidationMessage.Visibility = Visibility.Visible;
                ComboBoxProcesses.Focus();
                MessageBox.Show("Please select an application first.", "Validation", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            TextBlockValidationMessage.Visibility = Visibility.Collapsed;
            _targetWindowTitle = ((Process)ComboBoxProcesses.SelectedItem).MainWindowTitle;
            _listening = true;
            LabelStatus.Content = $"Listening enabled for {_targetWindowTitle}";
            _timer.Start();
        }

        private void buttonStopListening_Click(object sender, RoutedEventArgs e)
        {
            _listening = false;
            _clicking = false;
            LabelStatus.Content = "Listening disabled";
            _timer.Stop();
        }

        private void buttonToggleDebug_Click(object sender, RoutedEventArgs e)
        {
            _debugEnabled = !_debugEnabled;
            LabelStatus.Content = _debugEnabled ? "Debug mode enabled" : "Debug mode disabled";
        }
    }
}