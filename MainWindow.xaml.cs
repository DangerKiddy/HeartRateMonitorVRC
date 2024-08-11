using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        private static DeviceInformation hrDeviceInfo;
        private static BluetoothLEDevice hrDevice;
        private static GattCharacteristic hrGatt;
        private static OSC osc;

        private static int lastSentBpm = 0;
        private static int currentBpm = 0;

        private static DeviceInformationCollection pairedDevices;

        public MainWindow()
        {
            InitializeComponent();

            osc = new OSC();
            SelectDeviceButton.IsEnabled = false;

            ScanForDevicesAsync();
            EmulateBeatEffect();
        }

        private void DisplayStatus(string text)
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
                    BPMText.BeginAnimation(OpacityProperty, beatEffect);

                    osc.Send("/avatar/parameters/isHRBeat", true);
                    nextBeat -= 50;

                    await Task.Delay(50);

                    osc.Send("/avatar/parameters/isHRBeat", false);
                }

                await Task.Delay(nextBeat);
            }
        }

        private bool ShouldEmulateBeatEffect()
        {
            return osc != null && hrDevice != null && hrGatt != null;
        }

        private async void ScanForDevicesAsync()
        {
            DisplayStatus("Scanning");
            var deviceSelector = BluetoothLEDevice.GetDeviceSelector();
            pairedDevices = await DeviceInformation.FindAllAsync(deviceSelector);

            DisplayStatus("Scan complete, please select device");
            SelectDeviceButton.IsEnabled = true;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = args.CharacteristicValue.ToArray();
            int heartRate = ParseHeartRate(data);

            currentBpm = heartRate;
            BPMText.Dispatcher.Invoke(new Action(() =>
            {
                BPMText.Text = $"{currentBpm} BPM";
            }));

            SendBPMToVRChat(heartRate);
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

        private void SendBPMToVRChat(int bpm)
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
            }

            // Sending it all the time in case if avatar was changed and it should receive new data
            osc.Send("/avatar/parameters/isHRConnected", true);
            osc.Send("/avatar/parameters/isHRActive", true);
        }

        public static float Remap(float input, float inputStart, float inputEnd, float outputStart, float outputEnd)
        {
            return outputStart + ((outputEnd - outputStart) / (inputEnd - inputStart)) * (input - inputStart);
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            ElementFadeInOut(BPMText, Fade.Out);
            ElementFadeInOut(SelectDeviceButton, Fade.In);

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
                    ElementFadeInOut(BPMText, Fade.In);
                    ElementFadeInOut(SelectDeviceButton, Fade.Out);

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
                DisplayStatus("Failed connecting to device.");
                return null;
            }

            device.ConnectionStatusChanged += Device_ConnectionStatusChanged;

            DisplayStatus($"Connected to: {device.Name}");
            return device;
        }

        private async void Device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                DisplayStatus("Device disconnected. Reconnecting...");

                await TryPairWithDevice(hrDeviceInfo, true);
            }
        }

        private async Task<GattCharacteristic> GetHeartRateCharacteristicAsync(BluetoothLEDevice device, bool reconnecting = false)
        {
            var heartRateServiceUuid = GattServiceUuids.HeartRate;
            var heartRateMeasurementCharacteristicUuid = GattCharacteristicUuids.HeartRateMeasurement;

            var servicesResult = await device.GetGattServicesForUuidAsync(heartRateServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                DisplayStatus("Failed getting HR service.");
                return null;
            }

            var service = servicesResult.Services[0];

            var characteristicsResult = await service.GetCharacteristicsForUuidAsync(heartRateMeasurementCharacteristicUuid);
            if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
            {
                DisplayStatus("Unable to get characteristics.");

                if (reconnecting)
                {
                    await Task.Delay(1000);

                    await TryPairWithDevice(hrDeviceInfo);
                }

                return null;
            }

            return characteristicsResult.Characteristics[0];
        }

        private async Task<bool> SubscribeToHeartRateNotificationsAsync(GattCharacteristic characteristic)
        {
            characteristic.ValueChanged += Characteristic_ValueChanged;

            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (status == GattCommunicationStatus.Success)
            {
                DisplayStatus("Subscribed to HR notifications.");
            }
            else
            {
                DisplayStatus("Failed subscribing to HR notifications.");
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
    }
}
