using HeartRateMonitorVRC.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Windows.Devices.Enumeration;

namespace HeartRateMonitorVRC
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { private set; get; }

        private static OSC _osc;
        private BluetoothService _bluetoothService;
        private HeartRateProcessor _heartRateProcessor;
        private VRChatService _vrchat;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            _osc = new OSC();
            _heartRateProcessor = new HeartRateProcessor();
            _vrchat = new VRChatService(_osc);

            _heartRateProcessor.OnHeartBeatEmulationUpdate += (isBeat, millisecondsBeforeNextBeat) =>
            {
                _osc.Send("/avatar/parameters/isHRBeat", isBeat);

                if (isBeat)
                {
                    var beatEffect = new DoubleAnimation();
                    beatEffect.From = 1;
                    beatEffect.To = .25f;
                    beatEffect.Duration = TimeSpan.FromMilliseconds(millisecondsBeforeNextBeat);
                    HeartIcon.BeginAnimation(OpacityProperty, beatEffect);
                }
            };

            _heartRateProcessor.OnHeartRateProcessed += (hearRateData) =>
            {
                _vrchat.SendHeartRateInfoToVRChat(hearRateData);

                SetHeartRateUIValue(hearRateData.BPM, BPMText);
                SetHeartRateUIValue(hearRateData.LowestBPM, BPMText_Lowest);
                SetHeartRateUIValue(hearRateData.HighestBPM, BPMText_Highest);
            };

            InitializeWaitingForConnection();
        }

        public void InitializeWaitingForConnection()
        {
            SelectDeviceButton.IsEnabled = false;

            _osc.ActivatePhoneListener();
            _bluetoothService = new BluetoothService();

            _bluetoothService.OnStatusChanged += (describedStatus, status) =>
            {
                if (status == BluetoothStatus.SubscribedToNotification)
                    _osc.DeactivatePhoneListener();
                else if (status == BluetoothStatus.ScanComplete)
                    SelectDeviceButton.IsEnabled = true;

                SetDisplayStatus(describedStatus);
            };

            _bluetoothService.OnHeartRateUpdated += (bpm) =>
            {
                ReceiveHeartRateValue(bpm);
            };

            _bluetoothService.ScanDevicesAsync();
        }

        public void ReceiveHeartRateValue(int bpm)
        {
            _heartRateProcessor.UpdateBPM(bpm);
        }

        public void SetDisplayStatus(string text)
        {
            Trace.WriteLine(text);

            StatusText.Dispatcher.Invoke(new Action(() =>
            {
                StatusText.Text = text;
            }));
        }

        public void SetHeartRateUIValue(int heartrate, TextBlock textBlock)
        {
            textBlock.Dispatcher.Invoke(new Action(() =>
            {
                string zeros = "";
                for (int i = 0; i < 3 - heartrate.ToString().Length; i++)
                {
                    zeros += '0';
                }

                textBlock.Text = $"{zeros}{heartrate}";
            }));
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            _bluetoothService?.Dispose();
            InitializeWaitingForConnection();
            GC.Collect();

            _bluetoothService.ScanDevicesAsync();
        }

        private void SelectDevice_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            BuildDeviceSelectionMenu(contextMenu);

            contextMenu.IsOpen = true;
        }

        private void BuildDeviceSelectionMenu(ContextMenu contextMenu)
        {
            var devices = _bluetoothService.PairedDevices;
            foreach (var device in devices)
            {
                var menuItem = BuildDeviceOption(device);

                contextMenu.Items.Add(menuItem);
            }
        }

        private MenuItem BuildDeviceOption(DeviceInformation device)
        {
            var menuItem = new MenuItem
            {
                Header = device.Name
            };

            menuItem.Click += async (sender, e) =>
            {
                SelectDeviceButton.IsEnabled = false;
                ScanButton.IsEnabled = false;

                bool isSuccess = await _bluetoothService.TryPairWithDevice(device);
                if (isSuccess)
                {
                    SelectDeviceButton.IsEnabled = false;
                }
                else
                {
                    SelectDeviceButton.IsEnabled = true;
                }

                ScanButton.IsEnabled = true;
            };

            return menuItem;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
