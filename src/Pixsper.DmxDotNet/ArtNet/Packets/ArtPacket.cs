// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Text;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.Artnet.Packets;

internal abstract class ArtPacket
{
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
	internal abstract int PacketLength { get; }

	public static ArtPacket? Deserialize(ReadOnlySpan<byte> data)
	{
		if (data.Length < MinPacketLength)
			return null;

		var packetIdSlice = data.Slice(0, ArtNetPacketId.Length);

		for (int i = 0; i < ArtNetPacketId.Length; i++)
		{
			if (packetIdSlice[i] != ArtNetPacketId[i])
				return null;
		}

		var opcode = (ArtNetOpCode)(data[8] + (data[9] << 8));

		var reader = new SpanReader(data, Encoding.ASCII, ByteOrder.BigEndian);

		return opcode switch
		{
			ArtNetOpCode.OpDmx => ArtDmxPacket.Deserialize(ref reader),
			_ => null
		};
	}

	internal static bool DeserializeHeader(ref SpanReader reader, bool isReadProtocolVersion = true)
	{
		// Skip the packet ID and opcode
		reader.Skip(ArtNetPacketId.Length + 2);

		if (isReadProtocolVersion)
		{
			ushort protocolVersion = reader.ReadUShort();

			if (protocolVersion != ArtNetProtocolVersion)
				return false;
		}

		return true;
	}

	public ReadOnlySpan<byte> ToSpan()
	{
		byte[] data = new byte[PacketLength];
		var writer = new SpanWriter(data, Encoding.ASCII, ByteOrder.BigEndian);

		Serialize(ref writer);

		return data;
	}

	public ReadOnlyMemory<byte> ToMemory()
	{
		byte[] data = new byte[PacketLength];
		var writer = new SpanWriter(data, Encoding.ASCII, ByteOrder.BigEndian);

		Serialize(ref writer);

		return data;
	}


	protected abstract void Serialize(ref SpanWriter writer);

	internal void SerializeHeader(ref SpanWriter writer, bool isWriteProtocolVersion = true)
	{
		writer.Write(ArtNetPacketId);
		writer.Write((short)OpCode, ByteOrder.LittleEndian);

		if (isWriteProtocolVersion)
			writer.Write(ArtNetProtocolVersion);
	}
}