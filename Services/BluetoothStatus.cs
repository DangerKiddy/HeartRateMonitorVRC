namespace HeartRateMonitorVRC.Services
{
    internal enum BluetoothStatus
    {
        Scanning,
        ScanComplete,

        GettingCharacteristics,
        Reconnecting,
        ConnectionRestored,
        SubscribedToNotification,

        FailedConnection,
        FailedGettingCharacteristics,
        FailedSubscribingToNotification,

        DestroyingBluetooth,
    }
}
