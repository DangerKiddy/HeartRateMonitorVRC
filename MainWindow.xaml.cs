﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace HeartRateMonitorVRC
{
    public partial class MainWindow : Window
    {
        private enum Fade
        {
            In,
            Out
        }

        public static MainWindow Instance { private set; get; }

        private static DeviceInformation hrDeviceInfo;
        private static BluetoothLEDevice hrDevice;
        private static GattCharacteristic hrGatt;
        private static OSC osc;

        private static bool isUsingPhoneApp = false;

        private static int lastSentBpm = 0;
        private static int currentBpm = 0;

        private static int lowestBpm = int.MaxValue;
        private static int highestBpm = 0;

        private static DeviceInformationCollection pairedDevices;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            osc = new OSC();
            SelectDeviceButton.IsEnabled = false;

            ScanForDevicesAsync();
            EmulateBeatEffect();
        }

        public void SetDisplayStatus(string text)
        {
            Trace.WriteLine(text);

            StatusText.Dispatcher.Invoke(new Action(() =>
            {
                StatusText.Text = text;
            }));
        }

        private async void EmulateBeatEffect()
        {
            while (true)
            {
                var nextBeat = currentBpm == 0 ? 100 : (int)((1f / currentBpm) * 1000) * 60;

                if (ShouldEmulateBeatEffect())
                {
                    var beatEffect = new DoubleAnimation();
                    beatEffect.From = 1;
                    beatEffect.To = .25f;
                    beatEffect.Duration = TimeSpan.FromMilliseconds(nextBeat);
                    HeartIcon.BeginAnimation(OpacityProperty, beatEffect);

                    osc.Send("/avatar/parameters/isHRBeat", true);

                    var beatHalf = nextBeat / 3;
                    nextBeat = nextBeat - beatHalf;

                    await Task.Delay(beatHalf);

                    osc.Send("/avatar/parameters/isHRBeat", false);
                }

                await Task.Delay(nextBeat);
            }
        }

        private bool ShouldEmulateBeatEffect()
        {
            if (isUsingPhoneApp)
                return true;
            else
                return osc != null && hrDevice != null && hrGatt != null;
        }

        private async void ScanForDevicesAsync()
        {
            SetDisplayStatus("Scanning");
            var deviceSelector = BluetoothLEDevice.GetDeviceSelector();
            pairedDevices = await DeviceInformation.FindAllAsync(deviceSelector);

            SetDisplayStatus("Scan complete, please select device");
            SelectDeviceButton.IsEnabled = true;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = args.CharacteristicValue.ToArray();
            int heartRate = ParseHeartRate(data);

            ReceiveHeartRateValue(heartRate);
        }

        private int ParseHeartRate(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            // Flags byte
            var flags = data[0];

            // Heart Rate Measurement value (2 bytes)
            int heartRate = 0;
            if ((flags & 0x01) == 0) // Heart Rate Measurement value is 1 byte
            {
                heartRate = data[1];
            }
            else // Heart Rate Measurement value is 2 bytes
            {
                heartRate = BitConverter.ToUInt16(data, 1);
            }

            return heartRate;
        }

        public void ReceiveHeartRateValue(int heartRate)
        {
            currentBpm = heartRate;

            SetLowestAndHighesetBPM();
            SendBPMToVRChat(heartRate);

            SetHeartRateUIValue(currentBpm, BPMText);
            SetHeartRateUIValue(lowestBpm, BPMText_Lowest);
            SetHeartRateUIValue(highestBpm, BPMText_Highest);
        }

        public void SetLowestAndHighesetBPM()
        {
            if (currentBpm == 0)
                return;

            if (currentBpm < lowestBpm)
                lowestBpm = currentBpm;

            if (currentBpm > highestBpm)
                highestBpm = currentBpm;
        }

        public void SendBPMToVRChat(int bpm)
        {
            if (osc == null || bpm == lastSentBpm)
                return;

            lastSentBpm = bpm;

            float hr01 = Remap(bpm, 0, 255, 0, 1);
            osc.Send("/avatar/parameters/Heartrate2", hr01);
            osc.Send("/avatar/parameters/HRPercent", hr01);
            osc.Send("/avatar/parameters/FullHRPercent", Remap(bpm, 0, 255, -1, 1));
            osc.Send("/avatar/parameters/HR", bpm);

            string hrAsStr = bpm.ToString();
            if (bpm > 99)
            {
                osc.Send("/avatar/parameters/onesHR", int.Parse(hrAsStr[2].ToString()));
                osc.Send("/avatar/parameters/tensHR", int.Parse(hrAsStr[1].ToString()));
                osc.Send("/avatar/parameters/hundredsHR", int.Parse(hrAsStr[0].ToString()));
            }
            else
            {
                osc.Send("/avatar/parameters/onesHR", int.Parse(hrAsStr[1].ToString()));
                osc.Send("/avatar/parameters/tensHR", int.Parse(hrAsStr[0].ToString()));
                osc.Send("/avatar/parameters/hundredsHR", 0);
            }

            osc.Send("/avatar/parameters/HeartrateLowest", Remap(lowestBpm, 0, 255, 0, 1));
            osc.Send("/avatar/parameters/HeartrateHighest", Remap(highestBpm, 0, 255, 0, 1));

            // Sending it all the time in case if avatar was changed and it should receive new data
            osc.Send("/avatar/parameters/isHRConnected", true);
            osc.Send("/avatar/parameters/isHRActive", true);
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

        public static float Remap(float input, float inputStart, float inputEnd, float outputStart, float outputEnd)
        {
            return outputStart + ((outputEnd - outputStart) / (inputEnd - inputStart)) * (input - inputStart);
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            ScanForDevicesAsync();
        }

        private void SelectDevice_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            BuildDeviceSelectionMenu(contextMenu);

            contextMenu.IsOpen = true;
        }

        private void BuildDeviceSelectionMenu(ContextMenu contextMenu)
        {
            foreach (var device in pairedDevices)
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

                bool isSuccess = await TryPairWithDevice(device);
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

        private async Task<bool> TryPairWithDevice(DeviceInformation device, bool reconnecting = false)
        {
            hrDevice = await ConnectToDeviceAsync(device.Id);
            if (hrDevice == null)
                return false;

            hrGatt = await GetHeartRateCharacteristicAsync(hrDevice, reconnecting);
            if (hrGatt == null)
                return false;

            var success = await SubscribeToHeartRateNotificationsAsync(hrGatt);
            if (!success)
            {
                hrGatt = null;

                if (hrDevice != null)
                    hrDevice.Dispose();
            }

            hrDeviceInfo = device;
            return success;
        }

        private async Task<BluetoothLEDevice> ConnectToDeviceAsync(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (device == null)
            {
                SetDisplayStatus("Failed connecting to device.");
                return null;
            }

            device.ConnectionStatusChanged += Device_ConnectionStatusChanged;

            SetDisplayStatus($"Attempting get HR characteristics of {device.Name}");
            return device;
        }

        private async void Device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                SetDisplayStatus("Device disconnected. Reconnecting...");

                BPMText.Dispatcher.Invoke(new Action(() =>
                {
                    DisconnectDevice();

                    RequireReconnect();
                }));
            }
        }

        private async void RequireReconnect()
        {
            await Task.Delay(1000);
            await RetryConnecting();
        }

        private async Task RetryConnecting()
        {
            while (!await TryPairWithDevice(hrDeviceInfo, true))
            {
                SetDisplayStatus("Reconnection failed!. Trying again...");
                DisconnectDevice();
                await Task.Delay(1000);
            }

            SetDisplayStatus("Connection restored!");
        }

        private async Task<GattCharacteristic> GetHeartRateCharacteristicAsync(BluetoothLEDevice device, bool reconnecting = false)
        {
            var heartRateServiceUuid = GattServiceUuids.HeartRate;
            var heartRateMeasurementCharacteristicUuid = GattCharacteristicUuids.HeartRateMeasurement;

            var servicesResult = await device.GetGattServicesForUuidAsync(heartRateServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                SetDisplayStatus("Failed getting HR characteristics.");
                return null;
            }

            var service = servicesResult.Services[0];

            var characteristicsResult = await service.GetCharacteristicsForUuidAsync(heartRateMeasurementCharacteristicUuid);
            if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
            {
                SetDisplayStatus("Unable to get characteristics.");

                return null;
            }

            return characteristicsResult.Characteristics[0];
        }

        private async Task<bool> SubscribeToHeartRateNotificationsAsync(GattCharacteristic characteristic)
        {
            characteristic.ValueChanged += Characteristic_ValueChanged;

            var status = GattCommunicationStatus.ProtocolError;
            try
            {
                status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (status == GattCommunicationStatus.Success)
                {
                    SetDisplayStatus("Subscribed to HR notifications.");
                }
                else
                {
                    SetDisplayStatus("Failed subscribing to HR notifications.");
                }
            }
            catch (Exception)
            {
                SetDisplayStatus("Failed subscribing to HR notifications.");
            }

            return status == GattCommunicationStatus.Success;
        }

        private void ElementFadeInOut(UIElement element, Fade fade, int fadeTimeMilliseconds = 500)
        {
            var showAnim = new DoubleAnimation();
            showAnim.From = fade == Fade.In ? 0 : 1;
            showAnim.To = fade == Fade.In ? 1 : 0;
            showAnim.Duration = TimeSpan.FromMilliseconds(fadeTimeMilliseconds);
            element.BeginAnimation(OpacityProperty, showAnim);
        }

        private void DisconnectDevice()
        {
            if (hrDevice == null)
                return;

            hrDevice.Dispose();
            hrDevice = null;
            hrGatt = null;

            GC.Collect();
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

        public static void UsePhoneAppForBPM()
        {
            isUsingPhoneApp = true;
        }
    }
}
