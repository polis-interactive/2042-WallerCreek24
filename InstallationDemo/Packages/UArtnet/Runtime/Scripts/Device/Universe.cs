using System;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Polis.UArtnet.Device
{
    public class Universe : MonoBehaviour
    {
        public string address; // value is just to show up in inspector
        public int universe;

        public IPAddress ipAddress
        {
            get
            {
                return IPAddress.Parse(address);
            }
        }

        [HideInInspector]
        public byte[] data = new byte[512];

        [HideInInspector]
        public int channels;

        public void Setup(IPAddress address, int universe) 
        {
            this.address = address.ToString();
            this.universe = universe;
        }

        public void Attach()
        {
            var nodes = GetComponentsInChildren<Node>();
            if (nodes.Length == 0)
            {
                throw new Exception("Polis.UArtnet.Device.Universe.Start() no nodes found");
            }
            var maxChannels = 0;
            foreach (var node in nodes)
            {
                var lastChannel = node.channel + node.channels;
                if (lastChannel >= 512)
                {
                    throw new Exception("Polis.UArtnet.Device.Universe.Start() universe overflow");
                }
                maxChannels = Math.Max(lastChannel, maxChannels);
                node.Initialize(new ArraySegment<byte>(data, node.channel, node.channels));
            }
            channels = maxChannels;
        }
    }

}
