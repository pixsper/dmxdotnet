using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
    internal abstract class ArtPacket
    {
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum ArtNetOpCode : ushort
		{
			OpPoll = 0x2000,
			OpPollReply = 0x2100,
			OpDiagData = 0x2300,
			OpCommand = 0x2400,
			OpDmx = 0x5000,
			OpNzs = 0x5100,
			OpSync = 0x5200,
			OpAddress = 0x6000,
			OpInput = 0x7000,
			OpTodRequest = 0x8000,
			OpTodData = 0x8100,
			OpTodControl = 0x8200,
			OpRdm = 0x8300,
			OpRdmSub = 0x8400,
			OpVideoSetup = 0xa010,
			OpVideoPalette = 0xa020,
			OpVideoData = 0xa040,
			OpMacMaster = 0xf000,
			OpMacSlave = 0xf100,
			OpFirmwareMaster = 0xf200,
			OpFirmwareReply = 0xf300,
			OpFileTnMaster = 0xf400,
			OpFileFnMaster = 0xf500,
			OpFileFnReply = 0xf600,
			OpIpProg = 0xf800,
			OpIpProgReply = 0xf900,
			OpMedia = 0x9000,
			OpMediaPatch = 0x9100,
			OpMediaControl = 0x9200,
			OpMediaControlReply = 0x9300,
			OpTimeCode = 0x9700,
			OpTimeSync = 0x9800,
			OpTrigger = 0x9900,
			OpDirectory = 0x9a00,
			OpDirectoryReply = 0x9b00
		}

		private const ushort ArtNetProtocolVersion = 14;
        private static readonly byte[] ArtNetPacketId = Encoding.UTF8.GetBytes("Art-Net\0");

        private const int MinPacketLength = 10;

        public abstract ArtNetOpCode OpCode { get; }
        internal abstract int MaxPacketLength { get; }


        public static ArtPacket? FromByteArray(byte[] data)
        {
            if (data.Length < MinPacketLength)
                return null;

            if (ArtNetPacketId.Where((t, i) => data[i] != t).Any())
                return null;

			using (var ms = new MemoryStream(data))
			using (var reader = new ArtNetBinaryReader(ms))
			{
				var opcode = (ArtNetOpCode)(data[8] + (data[9] << 8));

				switch (opcode)
				{
					case ArtNetOpCode.OpDmx:
						return ArtDmxPacket.Deserialize(reader);

					case ArtNetOpCode.OpPoll:
						return ArtPollPacket.Deserialize(reader);

					case ArtNetOpCode.OpPollReply:
						return ArtPollReplyPacket.Deserialize(reader);

					case ArtNetOpCode.OpSync:
						return ArtSyncPacket.Deserialize(reader);

					case ArtNetOpCode.OpTimeCode:
						return ArtTimeCodePacket.Deserialize(reader);

					default:
						return null;
				}
			}
        }

        internal static bool DeserializeHeader(ArtNetBinaryReader reader, bool isReadProtocolVersion = true)
        {
			// Skip the packet ID and opcode
            reader.Seek(ArtNetPacketId.Length + 2, SeekOrigin.Current);

            if (isReadProtocolVersion)
            {
                ushort protocolVersion = reader.ReadUInt16();

                if (protocolVersion != ArtNetProtocolVersion)
                    return false;
            }

            return true;
        }

        public byte[] ToByteArray()
        {
			using (var ms = new MemoryStream(MaxPacketLength))
			using (var writer = new ArtNetBinaryWriter(ms))
			{
				Serialize(writer);
				return ms.ToArray();
			}
        }


        protected abstract void Serialize(ArtNetBinaryWriter writer);

        internal void SerializeHeader(ArtNetBinaryWriter writer, bool isWriteProtocolVersion = true)
        {
            writer.Write(ArtNetPacketId);
            writer.Write((byte)((short)OpCode & 0x00FF));
            writer.Write((byte)(((short)OpCode & 0xFF00) >> 8));

            if (isWriteProtocolVersion)
                writer.Write(ArtNetProtocolVersion);
        }
    }
}