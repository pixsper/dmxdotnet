using System;
using System.IO;
using System.Linq;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets
{
	internal abstract class SAcnPacket
	{
		public enum RootVector : uint
		{
			E131Data = 0x00000004,
			E131Extended = 0x00000008
		}

		public enum E131Vector : uint
		{
			DataPacket = 0x00000002
		}

		public enum E131ExtendedVector : uint
		{
			Synchronization = 0x00000001,
			Discovery = 0x00000002
		}

		public enum UniverseDiscoveryVector : uint
		{
			UniverseList = 0x00000001
		}

		private const int MinPacketLength = 49;

		private static readonly byte[] AcnPacketId = { 0x41, 0x53, 0x43, 0x2d, 0x45, 0x31, 0x2e, 0x31, 0x37, 0x00, 0x00, 0x00 };
		private const ushort PreambleSize = 0x0010;
		private const ushort PostambleSize = 0x0000;

		protected const int ProtocolFlags = 0x7;

		protected SAcnPacket(Guid cId)
		{
			CId = cId;
		}

		

		public Guid CId { get; }

		protected abstract int MaxPacketLength { get; }
		

		public static SAcnPacket? FromByteArray(byte[] data)
		{
			if (data.Length < MinPacketLength)
				return null;

			if (AcnPacketId.Where((t, i) => data[i + 4] != t).Any())
				return null;

			using (var ms = new MemoryStream(data))
			using (var reader = new SAcnBinaryReader(ms))
			{
				var rootVector = (RootVector)((data[18] << 24) + (data[19] << 16) + (data[20] << 8) + data[21]);

				switch (rootVector)
				{
				    case RootVector.E131Data:
				        return SAcnDataPacket.Deserialize(reader);

				    case RootVector.E131Extended:
				    {
				        var extendedVector = (E131ExtendedVector)((data[40] << 24) + (data[41] << 16) + (data[42] << 8) + data[43]);
				        switch (extendedVector)
				        {
				            case E131ExtendedVector.Synchronization:
				                return SAcnSyncPacket.Deserialize(reader);
				            case E131ExtendedVector.Discovery:
                                return SAcnUniverseDiscoveryPacket.Deserialize(reader);
				            default:
				                return null;
				        }
				    }

				    default:
				        return null;
				}
			}
		}

	    public byte[] ToByteArray()
	    {
	        using (var ms = new MemoryStream(MaxPacketLength))
	        using (var writer = new SAcnBinaryWriter(ms))
	        {
	            Serialize(writer);
	            return ms.ToArray();
	        }
	    }

	    protected abstract void Serialize(SAcnBinaryWriter writer);

	    internal void SerializeRootLayer(SAcnBinaryWriter writer, bool isExtended, int packetSize)
	    {
	        writer.Write(PreambleSize);
	        writer.Write(PostambleSize);
	        writer.Write(AcnPacketId);
	        writer.Write((ushort)(((packetSize - 16) & 0x0FFF) | (ProtocolFlags << 12)));
	        writer.Write((uint)(isExtended ? RootVector.E131Extended : RootVector.E131Data));
	        writer.Write(CId.ToByteArray());
	    }

	    internal static bool DeserializeRootLayer(SAcnBinaryReader reader, out Guid cId)
	    {
			cId = new Guid();
	        return false;
	    }
	}
}
