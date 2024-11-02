using System.Net;
using System.Net.Sockets;


namespace Polis.UArtnet.Network
{
    public class UdpSender
    {
        private Socket _socket;

        public UdpSender(int port, IPAddress? localAddress = null)
        {
            Port = port;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.EnableBroadcast = true;
            if (localAddress != null)
            {
                _socket.Bind(new IPEndPoint(localAddress, 0));
            }
        }
        public int Port { get; }

        public void Send(byte[] data, IPAddress ip)
        {
            _socket.SendTo(data, new IPEndPoint(ip, Port));
        }
    }
}