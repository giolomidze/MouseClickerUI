using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MouseClickerUI
{
    public partial class MainWindow
    {
        private static bool _clicking;
        private static bool _listening;
        private static bool _prevEnableListeningState;
        private static bool _prevDisableListeningState;
        private static bool _prevEnableClickingState;
        private static string _targetWindowTitle = string.Empty;
        private static int _clickDelay = 100; // Default delay in milliseconds
        private readonly DispatcherTimer _timer;

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MouseEventLetdown = 0x02;
        private const uint MouseEventLeftUp = 0x04;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
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

            // Add focus event handler for ComboBox
            comboBoxProcesses.GotFocus += ComboBoxProcesses_GotFocus;
        }

        private void ComboBoxProcesses_GotFocus(object sender, RoutedEventArgs e)
        {
            textBlockValidationMessage.Visibility = Visibility.Collapsed;
        }

        private void LoadProcesses()
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .OrderBy(p => p.ProcessName)
                .ToList();

            comboBoxProcesses.ItemsSource = processes;
            comboBoxProcesses.DisplayMemberPath = "MainWindowTitle";
            comboBoxProcesses.SelectedValuePath = "ProcessName";
        }

        private void buttonStartListening_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxProcesses.SelectedItem == null)
            {
                textBlockValidationMessage.Text = "Please select an application first.";
                textBlockValidationMessage.Visibility = Visibility.Visible;
                comboBoxProcesses.Focus(); // Set focus back to the ComboBox
                MessageBox.Show("Please select an application first.", "Validation", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            textBlockValidationMessage.Visibility = Visibility.Collapsed;
            _targetWindowTitle = ((Process)comboBoxProcesses.SelectedItem).MainWindowTitle;
            _listening = true;
            labelStatus.Content = $"Listening enabled for {_targetWindowTitle}";
            _timer.Start();
        }

        private void buttonStopListening_Click(object sender, RoutedEventArgs e)
        {
            _listening = false;
            _clicking = false; // Stop clicking when stop listening
            labelStatus.Content = "Listening disabled";
            _timer.Stop();
        }

        private void SliderDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (textBoxDelay != null)
            {
                _clickDelay = (int)e.NewValue;
                textBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(textBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)sliderDelay.Minimum)
                {
                    newDelay = (int)sliderDelay.Minimum;
                }
                else if (newDelay > (int)sliderDelay.Maximum)
                {
                    newDelay = (int)sliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                sliderDelay.Value = newDelay;
            }
            else
            {
                textBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(textBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)sliderDelay.Minimum)
                {
                    newDelay = (int)sliderDelay.Minimum;
                }
                else if (newDelay > (int)sliderDelay.Maximum)
                {
                    newDelay = (int)sliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                sliderDelay.Value = newDelay;
            }
            else
            {
                textBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateMouseClickingState();
        }

        private void UpdateMouseClickingState()
        {
            if (!IsTargetWindow())
            {
                return;
            }

            bool isKey1Pressed = IsKeyPressed(0x31); // Key '1'
            bool isKey0Pressed = IsKeyPressed(0x30); // Key '0'
            bool isKey8Pressed = IsKeyPressed(0x38); // Key '8'
            bool isKey9Pressed = IsKeyPressed(0x39); // Key '9'

            // Check if the number 1 key is pressed to enable listening
            if (isKey1Pressed && !_prevEnableListeningState)
            {
                _listening = true;
                labelStatus.Content = $"Listening enabled at {DateTime.Now}";
            }

            _prevEnableListeningState = isKey1Pressed;

            // Check if the number 0 key is pressed to disable listening
            if (isKey0Pressed && !_prevDisableListeningState)
            {
                _listening = false;
                _clicking = false; // Stop clicking when 0 key is pressed
                labelStatus.Content = $"Listening disabled at {DateTime.Now}";
            }

            _prevDisableListeningState = isKey0Pressed;

            // Check if the mouse clicking should be enabled
            if (_listening && isKey8Pressed && !_prevEnableClickingState)
            {
                _clicking = true;
                labelStatus.Content = $"Mouse clicking enabled at {DateTime.Now}";
            }

            _prevEnableClickingState = isKey8Pressed;

            // Check if the mouse clicking should be disabled
            if (_listening && isKey9Pressed && _clicking)
            {
                _clicking = false;
                labelStatus.Content = $"Mouse clicking disabled at {DateTime.Now}";
            }

            // Check if the mouse clicking is currently enabled
            if (_clicking)
            {
                // Simulate a mouse click
                SimulateMouseClick();
            }
        }

        private async void SimulateMouseClick()
        {
            // Simulate a left mouse click
            mouse_event(MouseEventLetdown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(_clickDelay);
        }
    }
}