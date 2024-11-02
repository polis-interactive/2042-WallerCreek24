using Polis.UArtnet;
using Polis.UArtnet.Device;
using Polis.UArtnet.Network;
using Polis.UArtnet.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public class InstallationController : MonoBehaviour, QueuedSender, ArtNetReceiverCallback
{
    private ArtnetStrategy artnetStrategy = ArtnetStrategy.Loopback;

    private List<Universe> universes;
    private List<FishFinal> fishes;

    private ConcurrentQueue<Dictionary<int, DmxSpecifier>> dmxSendQueue;
    private ConcurrentDictionary<int, byte[]> dmxReceiveDict;
    private ArtNetSender sender;
    private ArtNetReceiver receiver;

    void Start()
    {
        universes = GetComponentsInChildren<Universe>().ToList();
        if (universes.Count == 0)
        {
            throw new System.Exception("InstallationController.Start() no universe found");
        }
        foreach(var universe in universes)
        {
            universe.Attach();
        }
        fishes = GetComponentsInChildren<FishFinal>().ToList();
        if (fishes.Count == 0)
        {
            throw new System.Exception("InstallationController.Start() no fishes found");
        }
        var config = GetComponent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception("InstallationController.Start() config not found");
        }
        config.RegisterForUpdates<ArtnetConfig>(OnArtnetConfigChange);
    }

    IPAddress tryGetLocalIp()
    {
        var interfaces = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up)
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);

        foreach (var ipInfo in interfaces)
        {
            if (ipInfo.Address.ToString().StartsWith("10."))
            {
                return ipInfo.Address;
            }
        }
        Debug.Log("InstallationController.tryGetLocalIp() couldn't find ethernet; you're going to have a bad time");
        return null;
    }

    public void OnArtnetConfigChange(InstallationConfig config)
    {
        artnetStrategy = config.artnetConfig.artnetStrategy;
        dmxSendQueue = new ConcurrentQueue<Dictionary<int, DmxSpecifier>>();
        dmxReceiveDict = new ConcurrentDictionary<int, byte[]>();
        var dmxDict = new Dictionary<int, DmxSpecifier>();
        foreach(var universe in universes)
        {
            var ipAddress = artnetStrategy == ArtnetStrategy.Loopback
                ? new IPAddress(new byte[] { 127, 0, 0, 1 })
                : artnetStrategy == ArtnetStrategy.Broadcast
                ? new IPAddress(new byte[] { 255, 255, 255, 255 })
                : universe.ipAddress
            ;
            var dmxEntry = new DmxSpecifier()
            {
                ipAddress = ipAddress,
                channels = universe.channels,
                data = new byte[512]
            };
            dmxDict.Add(universe.universe, dmxEntry);
            dmxReceiveDict[universe.universe] = new byte[512];
        }
        var dmxDictCopy = new Dictionary<int, DmxSpecifier>(dmxDict);
        EnqueueDmxDict(dmxDictCopy);
        EnqueueDmxDict(dmxDict);
        IPAddress localAddr = null;
        if (!loopbackArtnet)
        {
            localAddr = tryGetLocalIp();
        }
        sender = new(localAddr);
        sender.IntializeSender(universes.Count);
        // ignoreSelf if !loopbackArtnet
        receiver = new(!loopbackArtnet);
        receiver.InitializeReceiver(this);
        // start polling task if not loopback artnet
    }

    public void EnqueueDmxDict(Dictionary<int, DmxSpecifier> dmxDict)
    {
        var isLoopback = IPAddress.IsLoopback(dmxDict[0].ipAddress);
        var isBroadcast = IPAddress.Broadcast.Equals(dmxDict[0].ipAddress);
        bool doEnqueue;
        switch(artnetStrategy)
        {
            case ArtnetStrategy.Broadcast:
                doEnqueue = isBroadcast;
                break;
            case ArtnetStrategy.Loopback:
                doEnqueue = isLoopback;
                break;
            default:
                doEnqueue = !isBroadcast && !isLoopback;
                break;
        }
        if (doEnqueue)
        {
            dmxSendQueue.Enqueue(dmxDict);
        }
    }

    public void ReceiveDmxPacket(DmxPacket packet)
    {
        if (dmxReceiveDict.TryGetValue(packet.Universe, out var data))
        {
            Array.Copy(packet.Dmx, data, packet.Length);
        } else
        {
            Debug.LogWarning($"InstallationController.ReceiveDmxPacket() dmxReceiveDict doesn't have universe {packet.Universe}");
        }
    }

    bool loopbackArtnet
    {
        get
        {
            return artnetStrategy == ArtnetStrategy.Loopback;
        }
    }

    void Update()
    {
        // need to parallelize these calls probably
        foreach(var fish in fishes)
        {
            fish.RunUpdate(!loopbackArtnet);
        }
        TrySendArtnet();
        if (!loopbackArtnet)
        {
            foreach (var fish in fishes)
            {
                fish.RunDisplay();
            }
        } else
        {
            foreach (var universe in universes)
            {
                if (dmxReceiveDict.TryGetValue(universe.universe, out var dmxValues)) {
                    Array.Copy(dmxValues, universe.data, 512);
                } else
                {
                    throw new System.Exception("InstallationController.Update() couldn't find universe in dmxReceiveDict");
                }
            }
            foreach (var fish in fishes)
            {
                fish.SetFromArtnet();
                fish.RunDisplay();
            }
        }
    }

    void TrySendArtnet()
    {
        if (dmxSendQueue.TryDequeue(out var dmxDict))
        {
            foreach (var universe in universes)
            {
                if (dmxDict.TryGetValue(universe.universe, out var dmxSpecifier))
                {
                    Array.Copy(universe.data, dmxSpecifier.data, 512);
                } else
                {
                    throw new System.Exception("InstallationController.TrySendArtnet() couldn't find universe in dmxDict");
                }
            }
            sender.SendDmx(this, dmxDict);
        } else
        {
            Debug.LogWarning("InstallationController.TrySendArtnet() no buffers to dequeue; skipping frame");
        }
    }
}
