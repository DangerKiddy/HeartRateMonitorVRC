using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Runtime.InteropServices.WindowsRuntime;

namespace HeartRateMonitorVRC.Services
{
    internal class BluetoothService : IDisposable
    {
        public event Action<int> OnHeartRateUpdated;
        public event Action<string, BluetoothStatus> OnStatusChanged;
        public event Action<DeviceInformationCollection> OnDevicesScanned;

        private DeviceInformation _deviceInfo { get; set; }
        private BluetoothLEDevice _device { get; set; }
        private GattCharacteristic _characteristic { get; set; }

        private DeviceInformationCollection _pairedDevices;
        public DeviceInformationCollection PairedDevices { get => _pairedDevices; }

        private static readonly Guid HeartRateServiceUuid = GattServiceUuids.HeartRate;
        private static readonly Guid HeartRateMeasurementUuid = GattCharacteristicUuids.HeartRateMeasurement;

        public bool IsDeviceConnected() => _device != null && _characteristic != null;

        public async void ScanDevicesAsync()
        {
            OnStatusChanged?.Invoke("Scanning", BluetoothStatus.Scanning);
            var deviceSelector = BluetoothLEDevice.GetDeviceSelector();
            _pairedDevices = await DeviceInformation.FindAllAsync(deviceSelector);

            OnDevicesScanned?.Invoke(_pairedDevices);
            OnStatusChanged?.Invoke("Scan complete, please select device", BluetoothStatus.ScanComplete);
        }

        public async Task<bool> TryPairWithDevice(DeviceInformation device, bool isReconnecting = false)
        {
            bool isSuccess = true;
            try
            {
                isSuccess = await ConnectToDeviceAndSubscribe(device, isReconnecting);
            }
            catch (Exception)
            {
                return false;
            }

            _deviceInfo = device;
            return isSuccess;
        }

        private async Task<bool> ConnectToDeviceAndSubscribe(DeviceInformation device, bool reconnecting = false)
        {
            _device = await ConnectToDeviceAsync(device.Id);
            if (_device == null)
                return false;

            _characteristic = await GetHeartRateCharacteristicAsync(_device, reconnecting);
            if (_characteristic == null)
                return false;

            var isSuccess = await SubscribeToHeartRateNotificationsAsync(_characteristic);
            return isSuccess;
        }

        private async Task<BluetoothLEDevice> ConnectToDeviceAsync(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (device == null)
            {
                OnStatusChanged?.Invoke("Failed connecting to device.", BluetoothStatus.FailedConnection);
                return null;
            }

            device.ConnectionStatusChanged += Device_ConnectionStatusChanged;

            OnStatusChanged?.Invoke($"Attempting get HR characteristics of {device.Name}", BluetoothStatus.GettingCharacteristics);
            return device;
        }

        private void Device_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                OnStatusChanged?.Invoke("Device disconnected. Reconnecting...", BluetoothStatus.Reconnecting);

                DisconnectDevice();
                RequireReconnect();
            }
        }

        private async void RequireReconnect()
        {
            await Task.Delay(1000);
            await RetryConnecting();
        }

        private async Task RetryConnecting()
        {
            while (!await TryPairWithDevice(_deviceInfo, true))
            {
                OnStatusChanged?.Invoke("Reconnection failed!. Trying again...", BluetoothStatus.Reconnecting);
                DisconnectDevice();
                await Task.Delay(1000);
            }

            OnStatusChanged?.Invoke("Connection restored!", BluetoothStatus.ConnectionRestored);
        }

        private async Task<GattCharacteristic> GetHeartRateCharacteristicAsync(BluetoothLEDevice device, bool reconnecting = false)
        {
            GattDeviceServicesResult servicesResult = await device.GetGattServicesForUuidAsync(HeartRateServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                OnStatusChanged?.Invoke("Failed getting HR characteristics.", BluetoothStatus.FailedGettingCharacteristics);
                return null;
            }

            var service = servicesResult.Services[0];

            var characteristicsResult = await service.GetCharacteristicsForUuidAsync(HeartRateMeasurementUuid);
            if (characteristicsResult.Status != GattCommunicationStatus.Success || characteristicsResult.Characteristics.Count == 0)
            {
                OnStatusChanged?.Invoke("Unable to get characteristics.", BluetoothStatus.FailedGettingCharacteristics);

                return null;
            }

            return characteristicsResult.Characteristics[0];
        }

        private async Task<bool> SubscribeToHeartRateNotificationsAsync(GattCharacteristic characteristic)
        {
            characteristic.ValueChanged += Characteristic_ValueChanged;

            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status == GattCommunicationStatus.Success)
                OnStatusChanged?.Invoke("Subscribed to HR notifications.", BluetoothStatus.SubscribedToNotification);
            else
                OnStatusChanged?.Invoke("Failed subscribing to HR notifications.", BluetoothStatus.FailedSubscribingToNotification);

            return status == GattCommunicationStatus.Success;
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = args.CharacteristicValue.ToArray();
            int heartRate = ParseHeartRate(data);

            OnHeartRateUpdated?.Invoke(heartRate);
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

        private void DisconnectDevice()
        {
            _device?.Dispose();
            _device = null;
            _characteristic = null;

            GC.Collect();
        }

        public void Dispose()
        {
            DisconnectDevice();

            OnStatusChanged?.Invoke("Destroying BluetoothService", BluetoothStatus.DestroyingBluetooth);
        }
    }
}
