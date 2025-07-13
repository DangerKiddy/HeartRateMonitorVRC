using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Windows.Devices.Enumeration;
using HeartRateMonitorVRC.Services;

namespace HeartRateMonitorVRC
{
    public partial class MainWindow
    {
        private static Osc _osc;
        private readonly HeartRateProcessor _heartRateProcessor;
        private BluetoothService _bluetoothService;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            _osc = new Osc();
            _heartRateProcessor = new HeartRateProcessor();
            var vrChat = new VrChatService(_osc);

            _heartRateProcessor.OnHeartBeatEmulationUpdate += (isBeat, millisecondsBeforeNextBeat) =>
            {
                _osc.Send("/avatar/parameters/isHRBeat", isBeat);

                if (isBeat) BeatAnimation(millisecondsBeforeNextBeat);
            };

            _heartRateProcessor.OnHeartRateProcessed += hearRateData =>
            {
                vrChat.SendHeartRateInfoToVrChat(hearRateData);

                SetHeartRateUiValue(hearRateData.Bpm, BpmText);
                SetHeartRateUiValue(hearRateData.LowestBpm, BpmTextLowest);
                SetHeartRateUiValue(hearRateData.HighestBpm, BpmTextHighest);
            };

            InitializeWaitingForConnection();
        }

        public static MainWindow Instance { private set; get; }

        private void InitializeWaitingForConnection()
        {
            SelectDeviceButton.IsEnabled = false;

            _osc.ActivatePhoneListener();
            _bluetoothService = new BluetoothService();

            _bluetoothService.OnStatusChanged += (describedStatus, status) =>
            {
                switch (status)
                {
                    case BluetoothStatus.SubscribedToNotification:
                        _osc.DeactivatePhoneListener();
                        break;
                    case BluetoothStatus.ScanComplete:
                        SelectDeviceButton.IsEnabled = true;
                        break;
                    case BluetoothStatus.Scanning:
                    case BluetoothStatus.GettingCharacteristics:
                    case BluetoothStatus.Reconnecting:
                    case BluetoothStatus.ConnectionRestored:
                    case BluetoothStatus.FailedConnection:
                    case BluetoothStatus.FailedGettingCharacteristics:
                    case BluetoothStatus.FailedSubscribingToNotification:
                    case BluetoothStatus.DestroyingBluetooth:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(status), status, null);
                }

                SetDisplayStatus(describedStatus);
            };

            _bluetoothService.OnHeartRateUpdated += ReceiveHeartRateValue;
            _bluetoothService.ScanDevicesAsync();
        }

        public void ReceiveHeartRateValue(int bpm)
        {
            _heartRateProcessor.UpdateBpm(bpm);
        }

        public void SetDisplayStatus(string text)
        {
            Trace.WriteLine(text);

            StatusText.Dispatcher.Invoke(() => { StatusText.Text = text; });
        }

        private void BeatAnimation(float duration)
        {
            var beatEffect = new DoubleAnimation
            {
                From = 1,
                To = .25f,
                Duration = TimeSpan.FromMilliseconds(duration)
            };
            HeartIcon.BeginAnimation(OpacityProperty, beatEffect);
        }

        private void SetHeartRateUiValue(int heartRate, TextBlock textBlock)
        {
            textBlock.Dispatcher.Invoke(() =>
            {
                var zeros = "";
                for (var i = 0; i < 3 - heartRate.ToString().Length; i++) zeros += '0';

                textBlock.Text = $"{zeros}{heartRate}";
            });
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            _bluetoothService?.Dispose();
            InitializeWaitingForConnection();
            GC.Collect();

            _bluetoothService?.ScanDevicesAsync();
        }

        private void SelectDevice_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            BuildDeviceSelectionMenu(contextMenu);

            contextMenu.IsOpen = true;
        }

        private void BuildDeviceSelectionMenu(ContextMenu contextMenu)
        {
            DeviceInformationCollection devices = _bluetoothService.PairedDevices;
            foreach (DeviceInformation device in devices)
            {
                MenuItem menuItem = BuildDeviceOption(device);

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

                var isSuccess = await _bluetoothService.TryPairWithDevice(device);
                SelectDeviceButton.IsEnabled = !isSuccess;

                ScanButton.IsEnabled = true;
            };

            return menuItem;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}