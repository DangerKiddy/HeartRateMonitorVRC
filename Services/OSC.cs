using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using SharpOSC;

namespace HeartRateMonitorVRC
{
    internal class OSC
    {
        private UdpClient _udp;
        private static UDPSender _oscSender;
        private static UDPListener _routerListener;
        private const int _routerPort = 28012;
        private const int _routerListenerPort = 28013;
        private bool _shouldStopListening = false;

        public OSC()
        {
            try
            {
                _udp = new UdpClient(_routerPort);
            }
            catch
            {
                MessageBox.Show($"Failed to create listen socket! (Another app listening on 127.0.0.1:{_routerPort} already?)", "UDP Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _oscSender = new UDPSender("127.0.0.1", 9000);
        }

        public void Send(string address, object value)
        {
            var message = new OscMessage(address, value);
            _oscSender.Send(message);
        }

        public void ActivatePhoneListener()
        {
            if (_routerListener != null)
                return;

            _shouldStopListening = false;

            ListenForPhoneApp();
        }

        public void DeactivatePhoneListener()
        {
            _shouldStopListening = true;

            _routerListener?.Dispose();
            _routerListener = null;
        }

        private async void ListenForPhoneApp()
        {
            while (!_shouldStopListening)
            {
                var incomingIP = new IPEndPoint(IPAddress.Any, 0);
                var data = await _udp.ReceiveAsync();
                incomingIP = data.RemoteEndPoint;

                if (data != null && data.Buffer.Length > 0)
                {
                    string dataAsStr = "";
                    foreach(var b in data.Buffer)
                    {
                        if (b != 0)
                            dataAsStr += (char)b;
                    }

                    Trace.WriteLine(dataAsStr);
                    if (dataAsStr.Contains("/netLocalIpAddress"))
                    {
                        var endpoint = new IPEndPoint(incomingIP.Address, _routerPort);
                        var sender = new UDPSender(incomingIP.Address.ToString(), _routerPort);

                        var message = new OscMessage("/confirmAddress", dataAsStr.Replace("/netLocalIpAddress,s", ""));
                        sender.Send(message);

                        sender.Close();

                        _shouldStopListening = true;

                        CreateListener();
                    }
                }
            }
        }

        private void CreateListener()
        {
            _routerListener = new UDPListener(_routerListenerPort, OnReceiveMessage);
            MainWindow.Instance.SetDisplayStatus("Using phone application.");
        }

        private void OnReceiveMessage(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            if (messageReceived == null)
                return;

            MainWindow.Instance.ReceiveHeartRateValue((int)messageReceived.Arguments[0]);
        }
    }
}
