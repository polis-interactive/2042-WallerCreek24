using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Polis.UArtnet.Packets;
using UnityEngine;
using UnityEngine.Events;

namespace Polis.UArtnet.Network
{
    public interface ArtNetReceiverCallback
    {
        public void ReceiveDmxPacket(DmxPacket packet);
    }

    public class ArtNetReceiver
    {

        private UdpReceiver udpReceiver;
        private ArtNetReceiverCallback receiver;
        private bool ignoreSelf;

        public ArtNetReceiver(bool ignoreSelf = true)
        {
            udpReceiver = new(6454, ignoreSelf);
        }

        public void InitializeReceiver(ArtNetReceiverCallback receiver)
        {
            this.receiver = receiver;
            udpReceiver.OnReceivedPacket = OnReceivedPacket;
            udpReceiver.StartReceive();
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