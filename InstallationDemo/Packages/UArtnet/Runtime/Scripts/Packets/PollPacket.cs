using System;

namespace Polis.UArtnet.Packets
{
    public class PollPacket : Packet
    {
        public PollPacket() : base(OpCode.Poll)
        {
        }

        public PollPacket(ReadOnlySpan<byte> buffer) : base(buffer, OpCode.Poll)
        {
        }

        public byte Flags { get; set; }
        public byte Priority { get; set; }


        protected override void Deserialize(Reader artNetReader)
        {
            ProtocolVersion = artNetReader.ReadNetworkUInt16();
            Flags = artNetReader.ReadByte();
            Priority = artNetReader.ReadByte();
        }

        protected override void Serialize(Writer artNetWriter)
        {
            base.Serialize(artNetWriter);
            artNetWriter.WriteNetwork(ProtocolVersion);
            artNetWriter.Write(Flags);
            artNetWriter.Write(Priority);
        }
    }
}