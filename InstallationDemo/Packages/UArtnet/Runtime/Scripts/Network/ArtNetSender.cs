

using Polis.UArtnet;
using Polis.UArtnet.Network;
using Polis.UArtnet.Packets;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;


namespace Polis.UArtnet.Network
{
    public interface QueuedSender
    {
        public void EnqueueDmxDict(Dictionary<int, DmxSpecifier> dmxDict);
    }

    public class ArtNetSender
    {
        private Dictionary<int, DmxPacket> dmxPackets = new();

        private CancellationTokenSource cancellationTokenSource;
        private Task sendingTask;
        public bool IsRunning => sendingTask is { IsCanceled: false, IsCompleted: false };
        private UdpSender sender = new UdpSender(6454);
        private ConcurrentQueue<(QueuedSender, Dictionary<int, DmxSpecifier>)> sendQueue = new();
        readonly object syncPrimitive = new object();

        public void IntializeSender(int universes)
        {
            for (int i = 0; i < universes; i++)
            {
                dmxPackets[i] = new DmxPacket() { Universe = (ushort)i, Sequence = 0, Dmx = new byte[512] };
            }
            cancellationTokenSource = new CancellationTokenSource();
            sendingTask = Task.Run(() => SendLoop());
        }

        public void SendDmx(QueuedSender dmxRet, Dictionary<int, DmxSpecifier> dmxDict)
        {
            lock (syncPrimitive)
            {
                sendQueue.Enqueue((dmxRet, dmxDict));
                Monitor.Pulse(syncPrimitive);
            }
        }

        private void SendLoop()
        {
            var token = cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                (QueuedSender, Dictionary<int, DmxSpecifier>) item;
                lock (syncPrimitive)
                {
                    while (sendQueue.IsEmpty && !token.IsCancellationRequested)
                    {
                        // Wait until Monitor.Pulse is called
                        Monitor.Wait(syncPrimitive);
                    }
                    if (!sendQueue.TryDequeue(out item))
                    {
                        continue;
                    }
                }
                // todo: need to support sending polls
                var (dmxRet, dmxDict) = item;
                DoSendDmx(dmxDict);
                dmxRet.EnqueueDmxDict(dmxDict);
            }
        }

        private void DoSendDmx(Dictionary<int, DmxSpecifier> dmxDict)
        {
            foreach (var dmxItem in dmxDict)
            {
                if (dmxPackets.TryGetValue(dmxItem.Key, out var packet))
                {
                    packet.Sequence = (byte)(packet.Sequence + 1 % 255);
                    Array.Copy(dmxItem.Value.data, packet.Dmx, 512);
                    var data = packet.ToByteArray();
                    sender.Send(data, dmxItem.Value.ipAddress);
                }
                else
                {
                    throw new System.Exception($"ArtNetSender.DoSendDmx() no packet prepared for universe {dmxItem.Key}");
                }
            }
        }
    }

}
