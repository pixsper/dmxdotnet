// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets;

internal class SAcnDataPacket : SAcnPacket, IEquatable<SAcnDataPacket>, IDmxDataPacket
{
	private const byte DmpVector = 0x02;
	private const byte AddressAndDataType = 0xA1;
	private const ushort FirstPropertyAddress = 0x0000;
	private const ushort AddressIncrement = 0x0001;

	public const int MaxSourceNameLength = 64;

	private const int MinPacketLength = 126;
	private const int HeaderLength = 126;

	public const int PriorityMin = 0;
	public const int PriorityMax = 200;
	public const int PriorityDefault = 100;

	public const int DataLengthMin = 0;
	public const int DataLengthMax = 512;

	public const int StartCodeDefault = 0;

	public SAcnDataPacket(Guid cId, string sourceName, int sequence, DmxUniverseAddress universeAddress, ReadOnlyMemory<byte> data,
		DmxUniverseAddress? synchronizationAddress = null, int priority = PriorityDefault,
		int startCode = StartCodeDefault,
		bool isForceSynchronization = false, bool isStreamTerminated = false, bool isPreviewData = false)
		: base(cId)
	{
		if (sourceName == null)
			throw new ArgumentNullException(nameof(sourceName));
		if (sourceName.Length > MaxSourceNameLength - 1)
			throw new ArgumentException($"Source name cannot be longer than {MaxSourceNameLength - 1} characters",
				nameof(sourceName));
		SourceName = sourceName;

		if (sequence < byte.MinValue || sequence > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(sequence), sequence,
				$"Must be in range {byte.MinValue}-{byte.MaxValue}");
		Sequence = sequence;

		if (universeAddress < DmxUniverseAddress.SAcnUniverseMin || universeAddress > DmxUniverseAddress.SAcnUniverseMax)
			throw new ArgumentOutOfRangeException(nameof(universeAddress), universeAddress,
				$"Must be in range {DmxUniverseAddress.SAcnUniverseMin}-{DmxUniverseAddress.SAcnUniverseMax}");
		UniverseAddress = universeAddress;

		if (data.Length > DataLengthMax)
			throw new ArgumentException($"Data array length must be in range {DataLengthMin}-{DataLengthMax}", nameof(data));
		Data = data;

		if (synchronizationAddress.HasValue)
			if (synchronizationAddress.Value < DmxUniverseAddress.SAcnUniverseMin ||
			    universeAddress > DmxUniverseAddress.SAcnUniverseMax)
				throw new ArgumentOutOfRangeException(nameof(universeAddress), universeAddress,
					$"Must be in range {DmxUniverseAddress.SAcnUniverseMin}-{DmxUniverseAddress.SAcnUniverseMax}");
		SynchronizationAddress = synchronizationAddress;


		if (priority < PriorityMin || priority > PriorityMax)
			throw new ArgumentOutOfRangeException(nameof(priority), priority,
				$"Must be in range {PriorityMin}-{PriorityMax}");
		Priority = priority;

		if (startCode < byte.MinValue || startCode > byte.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(startCode), startCode,
				$"Must be in range {byte.MinValue}-{byte.MaxValue}");
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

	public DmxUniverseAddress UniverseAddress { get; }
	public DmxUniverseAddress? SynchronizationAddress { get; }

	public int StartCode { get; }

	public ReadOnlyMemory<byte> Data { get; }

	protected override int MaxPacketLength => HeaderLength + DataLengthMax;
	protected override int PacketLength => HeaderLength + Data.Length;

	public bool Equals(SAcnDataPacket? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return SourceName == other.SourceName && Priority == other.Priority && Sequence == other.Sequence &&
		       IsPreviewData == other.IsPreviewData && IsStreamTerminated == other.IsStreamTerminated &&
		       IsForceSynchronization == other.IsForceSynchronization && UniverseAddress.Equals(other.UniverseAddress) &&
		       Nullable.Equals(SynchronizationAddress, other.SynchronizationAddress) &&
		       StartCode == other.StartCode && Data.Span.SequenceEqual(other.Data.Span);
	}

	internal static SAcnDataPacket? Deserialize(ref SpanReader reader)
	{
		if (!DeserializeRootLayer(ref reader, false, out Guid cId))
			return null;

		reader.Skip(6);

		string sourceName = reader.ReadNullTerminatedString(MaxSourceNameLength);
		int priority = reader.ReadByte();

		DmxUniverseAddress? synchronizationAddress = reader.ReadUShort();
		if (synchronizationAddress == 0)
			synchronizationAddress = null;

		int sequence = reader.ReadByte();

		var options = reader.ReadByte();
		var isPreviewData = (options & 0x80) != 0;
		var isStreamTerminated = (options & 0x40) != 0;

		int universeNumber = reader.ReadUShort();

		reader.Skip(8);

		int propertyValueCount = reader.ReadUShort();
		int startCode = reader.ReadByte();

		var dmxData = reader.ReadBytes(propertyValueCount - 1).ToArray();

		return new SAcnDataPacket(cId, sourceName, sequence, universeNumber, dmxData,
			synchronizationAddress, priority, startCode, isPreviewData, isStreamTerminated);
	}

	protected override void Serialize(ref SpanWriter writer)
	{
		var packetSize = MinPacketLength + Data.Length;

		SerializeRootLayer(ref writer, false, packetSize);

		// E1.31 framing layer
		writer.Write((ushort) (((packetSize - 38) & 0x0FFF) | (ProtocolFlags << 12)));
		writer.Write((uint) E131Vector.DataPacket);

		writer.Write(SourceName, MaxSourceNameLength);

		writer.Write((byte) Priority);
		writer.Write((ushort) (SynchronizationAddress ?? 0));
		writer.Write((byte) Sequence);
		writer.Write((byte) ((IsPreviewData ? 0x80 : 0x00)
		                     | (IsStreamTerminated ? 0x40 : 0x00)
		                     | (IsForceSynchronization ? 0x20 : 0x00)));
		writer.Write((ushort) UniverseAddress);

		// DMP layer
		writer.Write((ushort) (((packetSize - 115) & 0x0FFF) | (ProtocolFlags << 12)));
		writer.Write(DmpVector);
		writer.Write(AddressAndDataType);
		writer.Write(FirstPropertyAddress);
		writer.Write(AddressIncrement);
		writer.Write((ushort) (Data.Length + 1));
		writer.Write((byte) StartCode);
		writer.Write(Data);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == GetType() && Equals((SAcnDataPacket) obj);
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(SourceName);
		hashCode.Add(Priority);
		hashCode.Add(Sequence);
		hashCode.Add(IsPreviewData);
		hashCode.Add(IsStreamTerminated);
		hashCode.Add(IsForceSynchronization);
		hashCode.Add(UniverseAddress);
		hashCode.Add(SynchronizationAddress);
		hashCode.Add(StartCode);
		hashCode.Add(Data);
		return hashCode.ToHashCode();
	}
}