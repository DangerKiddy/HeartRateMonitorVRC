using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using SharpOSC;

namespace HeartRateMonitorVRC
{
    internal class OSC
    {
        private UdpClient udp;
        private static UDPSender oscSender;
        private static UDPListener routerListener;
        private const int routerPort = 28012;
        private const int routerListenerPort = 28013;
        private bool shouldStopListening = false;

        public OSC()
        {
            udp = new UdpClient(routerPort);

            try
            {
                if (udp == null)
                    udp = new UdpClient(routerPort);

                ListenForPhoneApp();
            }
            catch
            {
                MessageBox.Show($"Failed to create listen socket! (Another app listening on 127.0.0.1:{routerPort} already?)", "UDP Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            oscSender = new UDPSender("127.0.0.1", 9000);

        }
        public void Send(string address, object value)
        {
            var message = new OscMessage(address, value);
            oscSender.Send(message);
        }

        private async void ListenForPhoneApp()
        {
            while (!shouldStopListening)
            {
                var incomingIP = new IPEndPoint(IPAddress.Any, 0);
                var data = await udp.ReceiveAsync();
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
                        var endpoint = new IPEndPoint(incomingIP.Address, routerPort);

                        var sender = new UDPSender(incomingIP.Address.ToString(), routerPort);

                        var message = new OscMessage("/confirmAddress", dataAsStr.Replace("/netLocalIpAddress,s", ""));
                        sender.Send(message);

                        sender.Close();

                        shouldStopListening = true;

                        CreateListener();
                    }
                }
            }
        }

        private void CreateListener()
        {
            routerListener = new UDPListener(routerListenerPort, ReceivePhoneAppOSC);
        }

        private void ReceivePhoneAppOSC(OscPacket packet)
        {
            var messageReceived = (OscMessage)packet;
            if (messageReceived == null)
                return;

            MainWindow.UsePhoneAppForBPM();
            MainWindow.Instance.ReceiveHeartRateValue((int)messageReceived.Arguments[0]);
        }
    }
}
