using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using SharpOSC;

namespace HeartRateMonitorVRC.Services
{
    internal class Osc
    {
        private readonly UdpClient _udp;
        private static UDPSender _oscSender;
        private static UDPListener _routerListener;
        private const int RouterPort = 28012;
        private const int RouterListenerPort = 28013;
        private bool _shouldStopListening;

        public Osc()
        {
            try
            {
                _udp = new UdpClient(RouterPort);
            }
            catch
            {
                MessageBox.Show($"Failed to create listen socket! (Another app listening on 127.0.0.1:{RouterPort} already?)", "UDP Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                UdpReceiveResult data = await _udp.ReceiveAsync();
                IPEndPoint incomingIp = data.RemoteEndPoint;

                if (data.Buffer.Length <= 0) continue;
                
                var dataAsString = data.Buffer.Where(b => b != 0)
                    .Aggregate("", (current, b) => current + (char)b);

                Trace.WriteLine(dataAsString);

                if (!dataAsString.Contains("/netLocalIpAddress")) continue;
                
                var endpoint = new IPEndPoint(incomingIp.Address, RouterPort);
                var sender = new UDPSender(incomingIp.Address.ToString(), RouterPort);

                var content = dataAsString.Replace("/netLocalIpAddress,s", "");
                var message = new OscMessage("/confirmAddress", content);
                sender.Send(message);
                sender.Close();
                _shouldStopListening = true;
                CreateListener();
            }
        }

        private void CreateListener()
        {
            _routerListener = new UDPListener(RouterListenerPort, OnReceiveMessage);
            MainWindow.Instance.SetDisplayStatus("Using phone application.");
        }

        private void OnReceiveMessage(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            if (messageReceived == null) return;
            MainWindow.Instance.ReceiveHeartRateValue((int)messageReceived.Arguments[0]);
        }
    }
}
