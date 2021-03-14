// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.Artnet.Packets;

internal class ArtDmxPacket : ArtPacket, IEquatable<ArtDmxPacket>, IDmxDataPacket
{
	public const int MaxChannels = 512;
	public const int MinChannels = 2;

	public const int HeaderLength = 18;

	/// <exception cref="ArgumentOutOfRangeException">
	///     <paramref name="sequence" /> is outside of range 0-255. --or--
	///     <paramref name="physical" /> is outside of range 0-255.
	/// </exception>
	/// <exception cref="ArgumentException">Length of <paramref name="data" /> is outside of range 2-512. </exception>
	public ArtDmxPacket(DmxUniverseAddress universeAddress, ReadOnlyMemory<byte> data, int sequence = 0, int physical = 0)
	{
		UniverseAddress = universeAddress;

		if (data.Length < 2 || data.Length > 512)
			throw new ArgumentException("Data must contain between 2 and 512 values", nameof(data));

		Data = data;

		if (sequence < 0 || sequence > 255)
			throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be a value between 0 and 255");

		Sequence = sequence;

		if (physical < 0 || physical > 255)
			throw new ArgumentOutOfRangeException(nameof(physical), "Physical must be a value between 0 and 255");

		Physical = physical;
	}

	public override ArtNetOpCode OpCode => ArtNetOpCode.OpDmx;
	internal override int MaxPacketLength => HeaderLength + MaxChannels;
	internal override int PacketLength => HeaderLength + Data.Length;

	public int Sequence { get; }
	public int Physical { get; }
		
	public DmxUniverseAddress UniverseAddress { get; }
	public int StartCode => 0;

	public ReadOnlyMemory<byte> Data { get; }

	public bool Equals(ArtDmxPacket? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Sequence == other.Sequence && Physical == other.Physical && UniverseAddress.Equals(other.UniverseAddress) 
		       && Data.Span.SequenceEqual(other.Data.Span);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		return obj.GetType() == this.GetType() && Equals((ArtDmxPacket) obj);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Sequence, Physical, UniverseAddress, Data);
	}

	internal static ArtDmxPacket? Deserialize(ref SpanReader reader)
	{
		if (!DeserializeHeader(ref reader))
			return null;

		int sequence = reader.ReadByte();
		int physical = reader.ReadByte();
		int subUni = reader.ReadByte();
		int net = reader.ReadByte();
		int dataLength = reader.ReadUShort();
		var data = reader.ReadBytes(dataLength);

		try
		{
			return new ArtDmxPacket(new DmxUniverseAddress(net, (subUni & 0xF0) >> 4, subUni & 0x0F), data.ToArray(), sequence, physical);
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}

	protected override void Serialize(ref SpanWriter writer)
	{
		SerializeHeader(ref writer);
		writer.Write((byte)Sequence);
		writer.Write((byte)Physical);
		writer.Write((byte)((UniverseAddress.ArtNetSubNet << 4) + UniverseAddress.ArtNetUniverse));
		writer.Write((byte)UniverseAddress.ArtNetNet);
		writer.Write((short)Data.Length);
		writer.Write(Data);
	}
}