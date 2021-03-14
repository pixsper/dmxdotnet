// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Text;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets;

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

	protected abstract int PacketLength { get; }

	public static SAcnPacket? Deserialize(ReadOnlySpan<byte> data)
	{
		if (data.Length < MinPacketLength)
			return null;

		var packetIdSlice = data.Slice(4, AcnPacketId.Length);

		for (int i = 0; i < AcnPacketId.Length; i++)
		{
			if (packetIdSlice[i] != AcnPacketId[i])
				return null;
		}

		var reader = new SpanReader(data, Encoding.UTF8, ByteOrder.BigEndian);

		var rootVector = (RootVector) ((data[18] << 24) + (data[19] << 16) + (data[20] << 8) + data[21]);

		return rootVector switch
		{
			RootVector.E131Data => SAcnDataPacket.Deserialize(ref reader),
			_ => null
		};
	}

	public ReadOnlySpan<byte> ToSpan()
	{
		byte[] data = new byte[PacketLength];
		var writer = new SpanWriter(data, Encoding.UTF8, ByteOrder.BigEndian);

		Serialize(ref writer);

		return data;
	}

	public ReadOnlyMemory<byte> ToMemory()
	{
		byte[] data = new byte[PacketLength];
		var writer = new SpanWriter(data, Encoding.UTF8, ByteOrder.BigEndian);

		Serialize(ref writer);

		return data;
	}

	protected abstract void Serialize(ref SpanWriter writer);

	internal void SerializeRootLayer(ref SpanWriter writer, bool isExtended, int packetSize)
	{
		writer.Write(PreambleSize);
		writer.Write(PostambleSize);
		writer.Write(AcnPacketId);
		writer.Write((ushort)(((packetSize - 16) & 0x0FFF) | (ProtocolFlags << 12)));
		writer.Write((uint)(isExtended ? RootVector.E131Extended : RootVector.E131Data));
		writer.Write(CId.ToByteArray());
	}

	internal static bool DeserializeRootLayer(ref SpanReader reader, bool isExtended, out Guid cId)
	{
		cId = Guid.Empty;

		// Skip preamble size, postamble size, packet size and protocol flags
		// TODO: Do some better error checking on the above
		reader.Skip(sizeof(ushort) + sizeof(ushort) + AcnPacketId.Length + sizeof(ushort));

		var rootVector = (RootVector)reader.ReadUInt();
		switch (rootVector)
		{
			case RootVector.E131Data:
				if (isExtended)
					return false;
				break;
			case RootVector.E131Extended:
				if (!isExtended)
					return false;
				break;
			default:
				return false;
		}

		cId = new Guid(reader.ReadBytes(16));

		return true;
	}
}