using System;
using System.IO;
using System.Text;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets
{
	internal class SAcnDataPacket : SAcnPacket
	{
		private const byte DmpVector = 0x02;
		private const byte AddressAndDataType = 0xA1;
		private const ushort FirstPropertyAddress = 0x0000;
		private const ushort AddressIncrement = 0x0001;

		public const int MaxSourceNameLength = 64;

		private const int MinPacketLength = 126;

		public const int PriorityMin = 0;
		public const int PriorityMax = 200;
		public const int PriorityDefault = 100;

		public const int DataLengthMin = 1;
		public const int DataLengthMax = 512;

		public const int StartCodeDefault = 0;
		

		internal static SAcnDataPacket? Deserialize(SAcnBinaryReader reader)
		{
			Guid cId;
			if (!DeserializeRootLayer(reader, out cId))
				return null;

			reader.Seek(6, SeekOrigin.Current);

			string sourceName = Encoding.UTF8.GetString(reader.ReadBytes(64), 0, 64);
			int priority = reader.ReadByte();
			int synchronizationAddress = reader.ReadUInt16();
			int sequence = reader.ReadByte();

			byte options = reader.ReadByte();
			bool isPreviewData = (options & 0x80) != 0;
			bool isStreamTerminated = (options & 0x40) != 0;

			int universeNumber = reader.ReadUInt16();

			reader.Seek(8, SeekOrigin.Current);

			int propertyValueCount = reader.ReadUInt16();
			int startCode = reader.ReadByte();

			var dmxData = reader.ReadBytes(propertyValueCount - 1);

			return new SAcnDataPacket(cId, sourceName, sequence, universeNumber, dmxData,
				synchronizationAddress != 0 ? (int?)synchronizationAddress : null,
				priority, startCode, isPreviewData, isStreamTerminated);
		}

		public SAcnDataPacket(Guid cId, string sourceName, int sequence, int universe, byte[] data,
			int? synchronizationAddress = null, int priority = PriorityDefault, int startCode = StartCodeDefault,
			bool isForceSynchronization = false, bool isStreamTerminated = false, bool isPreviewData = false)
			: base(cId)
		{
			if (sourceName == null)
				throw new ArgumentNullException(nameof(sourceName));
			if (sourceName.Length > MaxSourceNameLength - 1)
				throw new ArgumentException($"Source name cannot be longer than {MaxSourceNameLength - 1} characters", nameof(sourceName));
			SourceName = sourceName;

			if (sequence < byte.MinValue || sequence > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(sequence), sequence, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			Sequence = sequence;

			if (universe < DmxUniverseAddress.SAcnAddressMin || universe > DmxUniverseAddress.SAcnAddressMax)
				throw new ArgumentOutOfRangeException(nameof(universe), universe, $"Must be in range {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			Universe = universe;

			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (data.Length < DataLengthMin || data.Length > DataLengthMax)
				throw new ArgumentException($"Data array length must be in range {DataLengthMin}-{DataLengthMax}", nameof(data));
			Data = data;

			if (synchronizationAddress.HasValue)
			{
				if (synchronizationAddress.Value < DmxUniverseAddress.SAcnAddressMin || universe > DmxUniverseAddress.SAcnAddressMax)
					throw new ArgumentOutOfRangeException(nameof(universe), universe, $"Must be in range {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			}
			SynchronizationAddress = synchronizationAddress;



			if (priority < PriorityMin || priority > PriorityMax)
				throw new ArgumentOutOfRangeException(nameof(priority), priority, $"Must be in range {PriorityMin}-{PriorityMax}");
			Priority = priority;

			if (startCode < byte.MinValue || startCode > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(startCode), startCode, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			StartCode = startCode;

			IsForceSynchronization = isForceSynchronization;
			IsStreamTerminated = isStreamTerminated;
			IsPreviewData = isPreviewData;
		}

		public string SourceName { get; }

		public int Priority { get; }

		public int Sequence { get; }

		public bool IsPreviewData { get; }
		public bool IsStreamTerminated { get; }
		public bool IsForceSynchronization { get; }

		public int Universe { get; }
		public int? SynchronizationAddress { get; }

		public int StartCode { get; }

		public byte[] Data { get; }

		protected override int MaxPacketLength => 638;

	    protected override void Serialize(SAcnBinaryWriter writer)
		{
			int packetSize = MinPacketLength + Data.Length;

			SerializeRootLayer(writer, false, packetSize);

			// E1.31 framing layer
			writer.Write((ushort)(((packetSize - 38) & 0x0FFF) | (ProtocolFlags << 12)));
			writer.Write((uint)E131Vector.DataPacket);

			writer.Write(SourceName, MaxSourceNameLength);

			writer.Write((byte)Priority);
			writer.Write((ushort)(SynchronizationAddress ?? 0));
			writer.Write((byte)Sequence);
			writer.Write((byte)((IsPreviewData ? 0x80 : 0x00)
			                    | (IsStreamTerminated ? 0x40 : 0x00)
			                    | (IsForceSynchronization ? 0x20 : 0x00)));
			writer.Write((ushort)Universe);

			// DMP layer
			writer.Write((ushort)(((packetSize - 115) & 0x0FFF) | (ProtocolFlags << 12)));
			writer.Write(DmpVector);
			writer.Write(AddressAndDataType);
			writer.Write(FirstPropertyAddress);
			writer.Write(AddressIncrement);
			writer.Write((ushort)(Data.Length + 1));
			writer.Write((byte)StartCode);
			writer.Write(Data);
		}
	}
}
