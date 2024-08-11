using System;
using System.Net.Sockets;
using SharpOSC;

namespace HeartRateMonitorVRC
{
    internal class OSC
    {
        private UdpClient udp;
        private static UDPSender oscSender;

        public OSC()
        {
            udp = new UdpClient();
            oscSender = new UDPSender("127.0.0.1", 9000);
        }
        public void Send(string address, object value)
        {
            var message = new OscMessage(address, value);
            oscSender.Send(message);
        }
    }
}
