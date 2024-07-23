using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace qoldau.suap.miniagent.Mitsubishi {
    public class MitsubishiQSeries {

        public string Ip { get; set; }
        public ushort Port { get; set; }
        public object AdditionalParameterBox { get; set; }

        public bool IsAvailable {
            get {
                Ping ping = new Ping();
                PingReply result = ping.Send(Ip);
                return result?.Status == IPStatus.Success;
            }
        }

        public bool IsDirectOrderOfBytes {
            get { return true; }
        }

        public bool IsConnected { get; private set; }

        public MitsubishiQSeries(string ip, ushort port) {
            Ip = ip;
            Port = port;
        }


        public byte[] ReadBytes() {
            var bReceive = new byte[2046];
            int receivedCount = 0;
            var socket = new TcpClient();
            try {
                socket.Connect(Ip, Port);
                socket.ReceiveTimeout = 1000;

                var stream = socket.GetStream();
                var buffer = new byte[] { 0x01 };
                stream.Write(buffer, 0, buffer.Length);

                receivedCount = stream.Read(bReceive, 0, bReceive.Length);
                stream.Close();

            } catch (Exception ex) {
                socket.Close();
                throw;
            }

            return bReceive.Take(receivedCount).ToArray();
        }
    }
}
