using Polis.UArtnet;
using Polis.UArtnet.Device;
using Polis.UArtnet.Network;
using Polis.UArtnet.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

[Serializable]
public class RValuesContainer
{
    public List<float> rValues;
}

[RequireComponent(typeof(InstallationEffects))]
public class InstallationController : MonoBehaviour, QueuedSender, ArtNetReceiverCallback
{
    private ArtnetStrategy artnetStrategy = ArtnetStrategy.Loopback;

    private List<Universe> universes;
    public List<FishFinal> fishes;
    public List<List<FishFinal>> tThenThetaSortedFishes;
    public List<float> rValues;

    private TSplineFinal spline;

    private ConcurrentQueue<Dictionary<int, DmxSpecifier>> dmxSendQueue;
    private ConcurrentDictionary<int, byte[]> dmxReceiveDict;
    private ArtNetSender sender;
    private ArtNetReceiver receiver;

    private InstallationEffects installationEffects;

    void Start()
    {
        var splines = GetComponentsInChildren<TSplineFinal>();
        if (splines.Length == 0)
        {
            throw new System.Exception("InstallationController.Start() Requires a child that implements TSplineFinal");
        }
        else if (splines.Length > 1)
        {
            throw new System.Exception("InstallationController.Start() Multiple children implementing TSplineFinal found");
        }
        spline = splines[0];
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
        var installationEffects = GetComponentsInChildren<InstallationEffects>();
        if (installationEffects.Length == 0)
        {
            throw new System.Exception("InstallationController.Start() Requires a child that implements InstallationEffects");
        }
        else if (installationEffects.Length > 1)
        {
            throw new System.Exception("InstallationController.Start() Multiple children implementing InstallationEffects found");
        }
        this.installationEffects = installationEffects[0];
        var config = GetComponent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception("InstallationController.Start() config not found");
        }
        config.RegisterForUpdates<ArtnetConfig>(OnArtnetConfigChange);
        config.RegisterForUpdates<ParameterConfig>(OnParameterConfigChange);
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
        if (noArtnet)
        {
            return;
        }
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

    public void OnParameterConfigChange(InstallationConfig config)
    {
        var tBucketCount = (int)(1.0f / config.parameterConfig.tBucketStep);
        tThenThetaSortedFishes = new List<List<FishFinal>>(tBucketCount);
        rValues = new();
        for (int i = 0; i < tBucketCount; i++)
        {
            tThenThetaSortedFishes.Add(new List<FishFinal>());
        }
        foreach (var fish in fishes)
        {
            fish.SetParameterValues(spline, ref config.parameterConfig);
            var tValue = fish.tValueInt;
            tThenThetaSortedFishes[tValue].Add(fish);
            rValues.Add(fish.rValue);
        }
        foreach (var tBucket in tThenThetaSortedFishes)
        {
            tBucket.Sort((a, b) => a.thetaValue.CompareTo(b.thetaValue));
        }
        rValues.Sort();
    }

    public void EnqueueDmxDict(Dictionary<int, DmxSpecifier> dmxDict)
    {
        if (noArtnet)
        {
            return;
        }
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

    bool noArtnet
    {
        get
        {
            return artnetStrategy == ArtnetStrategy.None;
        }
    }

    void Update()
    {
        installationEffects.RunEffects(this);
        foreach (var fish in fishes)
        {
            fish.WriteToArtnet(loopbackArtnet);
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
        if (noArtnet)
        {
            return;
        }
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
