using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MouseClickerWPF
{
    public partial class MainWindow : Window
    {
        private static bool clicking = false;
        private static bool listening = false;
        private static bool prevEnableListeningState = false;
        private static bool prevDisableListeningState = false;
        private static bool prevEnableClickingState = false;
        private static string targetWindowTitle = string.Empty;
        private static int clickDelay = 100; // Default delay in milliseconds
        private DispatcherTimer timer;

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        private const uint MouseeventfLeftdown = 0x02;
        private const uint MouseeventfLeftup = 0x04;

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
            if (string.IsNullOrEmpty(targetWindowTitle))
            {
                return false;
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            StringBuilder windowText = new StringBuilder(256);
            if (GetWindowText(foregroundWindow, windowText, windowText.Capacity) > 0)
            {
                return windowText.ToString().Contains(targetWindowTitle);
            }
            return false;
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadProcesses();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Timer_Tick;

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
                MessageBox.Show("Please select an application first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            textBlockValidationMessage.Visibility = Visibility.Collapsed;
            targetWindowTitle = ((Process)comboBoxProcesses.SelectedItem).MainWindowTitle;
            listening = true;
            labelStatus.Content = $"Listening enabled for {targetWindowTitle}";
            timer.Start();
        }

        private void buttonStopListening_Click(object sender, RoutedEventArgs e)
        {
            listening = false;
            clicking = false; // Stop clicking when stop listening
            labelStatus.Content = "Listening disabled";
            timer.Stop();
        }

        private void SliderDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (textBoxDelay != null)
            {
                clickDelay = (int)e.NewValue;
                textBoxDelay.Text = clickDelay.ToString();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateMouseClickingState();
        }

        public void UpdateMouseClickingState()
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
            if (isKey1Pressed && !prevEnableListeningState)
            {
                listening = true;
                labelStatus.Content = $"Listening enabled at {DateTime.Now}";
            }
            prevEnableListeningState = isKey1Pressed;

            // Check if the number 0 key is pressed to disable listening
            if (isKey0Pressed && !prevDisableListeningState)
            {
                listening = false;
                clicking = false; // Stop clicking when 0 key is pressed
                labelStatus.Content = $"Listening disabled at {DateTime.Now}";
            }
            prevDisableListeningState = isKey0Pressed;

            // Check if the mouse clicking should be enabled
            if (listening && isKey8Pressed && !prevEnableClickingState)
            {
                clicking = true;
                labelStatus.Content = $"Mouse clicking enabled at {DateTime.Now}";
            }
            prevEnableClickingState = isKey8Pressed;

            // Check if the mouse clicking should be disabled
            if (listening && isKey9Pressed && clicking)
            {
                clicking = false;
                labelStatus.Content = $"Mouse clicking disabled at {DateTime.Now}";
            }

            // Check if the mouse clicking is currently enabled
            if (clicking)
            {
                // Simulate a mouse click
                SimulateMouseClick();
            }
        }

        private async void SimulateMouseClick()
        {
            // Simulate a left mouse click
            mouse_event(MouseeventfLeftdown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseeventfLeftup, 0, 0, 0, UIntPtr.Zero);
            await Task.Delay(clickDelay);
        }
    }
}
