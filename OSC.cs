using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HeartRateMonitorVRC.Properties;

namespace HeartRateMonitorVRC
{
    internal class OSC
    {
        private UdpClient udp;

        public OSC()
        {
            udp = new UdpClient();
        }

        public void SendFloat(string address, float value)
        {
            var sendBack = address + '\0';
            AlignStringBytes(ref sendBack);

            sendBack += ",f";
            AlignStringBytes(ref sendBack);

            var buffer = Encoding.ASCII.GetBytes(sendBack);
            var finalBuffer = new byte[buffer.Length + 4];
            Array.Copy(buffer, finalBuffer, buffer.Length);

            var bytes = BitConverter.GetBytes(value);
            finalBuffer[buffer.Length + 0] = bytes[3];
            finalBuffer[buffer.Length + 1] = bytes[2];
            finalBuffer[buffer.Length + 2] = bytes[1];
            finalBuffer[buffer.Length + 3] = bytes[0];

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            udp.Send(finalBuffer, finalBuffer.Length, endPoint);
        }

        public void SendInt(string address, int value)
        {
            var sendBack = address + '\0';
            AlignStringBytes(ref sendBack);

            sendBack += ",i";
            AlignStringBytes(ref sendBack);

            var buffer = Encoding.ASCII.GetBytes(sendBack);
            var finalBuffer = new byte[buffer.Length + 4];
            Array.Copy(buffer, finalBuffer, buffer.Length);

            var bytes = BitConverter.GetBytes(value);
            finalBuffer[buffer.Length + 0] = bytes[3];
            finalBuffer[buffer.Length + 1] = bytes[2];
            finalBuffer[buffer.Length + 2] = bytes[1];
            finalBuffer[buffer.Length + 3] = bytes[0];

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            udp.Send(finalBuffer, finalBuffer.Length, endPoint);
        }

        // TODO: Fix byte alignment, since with specific addresses it aligns bytes wrong. Most likely happens to SendFloat and SendInt as well
        public void SendBool(string address, bool value)
        {
            var sendBack = address + '\0';
            AlignStringBytes(ref sendBack);

            sendBack += ",";
            sendBack += value ? "T" : "F";
            AlignStringBytes(ref sendBack);

            var buffer = Encoding.ASCII.GetBytes(sendBack);

            Trace.WriteLine($"{address}, {buffer.Length}");

            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
            udp.Send(buffer, buffer.Length, endPoint);
        }

        private static void AlignStringBytes(ref string str)
        {
            int strLen = str.Length;
            if (strLen % 4 != 0)
            {
                strLen += 4 - (strLen % 4);
            }
            else
            {
                strLen += 4;
            }

            for (int i = str.Length; i < strLen; i++)
            {
                str += '\0';
            }
        }
    }
}
