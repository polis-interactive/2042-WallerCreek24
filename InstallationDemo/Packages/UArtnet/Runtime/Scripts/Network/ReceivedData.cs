using System;
using System.Net;
using Polis.UArtnet.Packets;

namespace Polis.UArtnet.Network
{
    public class ReceivedData<TPacket> where TPacket : Packet
    {
        private ReceivedData()
        {
            ReceivedAt = DateTime.Now;
        }

        public ReceivedData(TPacket packet, EndPoint remoteEndPoint) : this() =>
            (Packet, RemoteEp) = (packet, remoteEndPoint);
        public TPacket Packet { get; }
        public EndPoint RemoteEp { get; }
        public DateTime ReceivedAt { get; }
    }
}