using System;
using UnityEngine;

namespace Polis.UArtnet.Device
{
    public class Node : MonoBehaviour
    {
        public int channel;
        public int channels;
        public ArraySegment<byte> data;

        public void Setup(int channel, int channels)
        {
            this.channel = channel;
            this.channels = channels;
        }

        public void Initialize(ArraySegment<byte> data)
        {
            if (data.Count != channels)
            {
                throw new Exception("Polis.UArtnet.Device.Node.Initialize() arraysegment len does not match channels");
            }
            this.data = data;
        }
    }

}
