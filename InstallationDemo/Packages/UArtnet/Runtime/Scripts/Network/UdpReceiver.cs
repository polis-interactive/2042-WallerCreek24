
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Polis.UArtnet.Network
{
    public sealed class UdpReceiver
    {
        private Socket _socket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _task;
        private byte[] _receiveBuffer = new byte[1500];

        public int Port { get; }
        public bool IsRunning => _task is { IsCanceled: false, IsCompleted: false };

        private bool ignoreSelf;

        public ReceivedPacketEventHandler OnReceivedPacket = (_, _, _) => { };
        public ErrorOccuredEventHandler OnUdpStartFailed = _ => { };
        public ErrorOccuredEventHandler OnUdpReceiveFailed = _ => { };
        public ErrorOccuredEventHandler OnUdpReceiveRaiseException = _ => { };

        public delegate void ReceivedPacketEventHandler(byte[] receiveBuffer, int length, EndPoint remoteEp);
        public delegate void ErrorOccuredEventHandler(Exception e);
        public UdpReceiver(int port, bool ignoreSelf = true)
        {
            Port = port;
            this.ignoreSelf = ignoreSelf;
        }

        ~UdpReceiver()
        {
            StopReceive();
        }

        public void StartReceive()
        {
            StopReceive();
            if (IsRunning) return;

            if (Port == 0) Debug.LogWarning("[UdpReceiver] Port is not set.");

            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                _socket.EnableBroadcast = true;

                _cancellationTokenSource = new CancellationTokenSource();
                _task = Task.Run(() => UdpTaskAsync(_cancellationTokenSource.Token));
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat($"[UdpReceiver] Udp start failed. {e.GetType()} : {e.Message}");
                OnUdpStartFailed?.Invoke(e);
            }
        }

        private async Task UdpTaskAsync(CancellationToken token)
        {
            Debug.Log($"[UdpReceiver] Udp Receive task start. port: {Port}");

            var localAddresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Select(ip => ip.Address)
                .ToHashSet();

            while (!token.IsCancellationRequested && _socket != null)
            {
                try
                {
                    EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                    var result = await _socket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, remoteEp);
                    if (result.ReceivedBytes != 0)
                    {
                        var remoteIpAddress = ((IPEndPoint)result.RemoteEndPoint).Address;
                        if (!ignoreSelf || !localAddresses.Contains(remoteIpAddress))
                        {
                            OnReceivedPacket?.Invoke(_receiveBuffer, result.ReceivedBytes, result.RemoteEndPoint);
                        }
                    }
                }
                catch (Exception e) when (e is SocketException or ObjectDisposedException)
                {
                    Debug.Log($"[UdpReceiver] Udp Receive task failed. {e.Message} : {e.GetType()}");
                    OnUdpReceiveFailed?.Invoke(e);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat($"[UdpReceiver] Udp receive failed. {e.Message} : {e.GetType()}");
                    OnUdpReceiveRaiseException?.Invoke(e);
                }
            }
        }

        public void StopReceive()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _task = null;
            }

            if (_socket == null) return;
            _socket.Close();
            _socket = null;
        }
    }
}