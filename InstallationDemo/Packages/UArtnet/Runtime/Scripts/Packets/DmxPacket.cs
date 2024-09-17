using System;

namespace Polis.UArtnet.Packets
{
    public class DmxPacket : Packet
    {
        public DmxPacket() : base(OpCode.Dmx)
        {
        }

        public DmxPacket(ReadOnlySpan<byte> buffer) : base(buffer, OpCode.Dmx)
        {
        }

        public byte Sequence { get; set; }
        public byte Physical { get; set; }
        public ushort Universe { get; set; }

        public ushort Length => Dmx == null ? (ushort)0 : (ushort)Dmx.Length;

        public byte[] Dmx { get; set; }

        protected override void Deserialize(Reader reader)
        {
            ProtocolVersion = reader.ReadNetworkUInt16();
            Sequence = reader.ReadByte();
            Physical = reader.ReadByte();
            Universe = reader.ReadUInt16();
            int length = reader.ReadNetworkUInt16();
            Dmx = reader.ReadBytes(length);
        }

        protected override void Serialize(Writer writer)
        {
            base.Serialize(writer);
            writer.WriteNetwork(ProtocolVersion);
            writer.Write(Sequence);
            writer.Write(Physical);
            writer.Write(Universe);
            writer.WriteNetwork(Length);
            writer.Write(Dmx);
        }
    }
}