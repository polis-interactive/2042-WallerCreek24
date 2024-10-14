using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Polis.UArtnet.Packets;
using UnityEngine;
using UnityEngine.Events;

namespace Polis.UArtnet.Network
{
    public interface DmxReceiver
    {
        public void ReceiveDmxPacket(DmxPacket packet);
    }

    public class ArtNetReceiver
    {

        private UdpReceiver UdpReceiver { get; } = new(6454);
        private DmxReceiver receiver;

        public void InitializeReceiver(DmxReceiver receiver)
        {
            this.receiver = receiver;
            UdpReceiver.OnReceivedPacket = OnReceivedPacket;
            UdpReceiver.StartReceive();
        }

        private void OnReceivedPacket(byte[] receiveBuffer, int length, EndPoint remoteEp)
        {
            var packet = Packet.Create(receiveBuffer);
            if (packet == null) return;

            switch (packet.OpCode)
            {
                case OpCode.Dmx:
                    receiver.ReceiveDmxPacket(packet as DmxPacket);
                    break;
                case OpCode.PollReply:
                    // todo: handle pollReply
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ReceivedData<TPacket> ReceivedData<TPacket>(Packet netPacket, EndPoint endPoint)
            where TPacket : Packet
        {
            return new ReceivedData<TPacket>(netPacket as TPacket, endPoint);
        }
    }
}