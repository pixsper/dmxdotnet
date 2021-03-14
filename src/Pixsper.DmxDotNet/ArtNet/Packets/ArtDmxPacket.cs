// This file is part of DmxDotNet.
// 
// DmxDotNet is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// DmxDotNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with DmxDotNet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
	internal class ArtDmxPacket : ArtPacket, IEquatable<ArtDmxPacket>
	{
		public const int MaxChannels = 512;
		public const int MinChannels = 2;

		/// <exception cref="ArgumentOutOfRangeException">
		///     <paramref name="sequence" /> is outside of range 0-255. --or--
		///     <paramref name="physical" /> is outside of range 0-255.
		/// </exception>
		/// <exception cref="ArgumentException">Length of <paramref name="data" /> is outside of range 2-512. </exception>
		public ArtDmxPacket(DmxUniverseAddress address, byte[] data, int sequence = 0, int physical = 0)
		{
			Address = address;

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

		public override ArtNetOpCode OpCode { get; } = ArtNetOpCode.OpDmx;
		internal override int MaxPacketLength => 530;

		public int Sequence { get; }
		public int Physical { get; }
		
		public DmxUniverseAddress Address { get; }

		public byte[] Data { get; }

		public bool Equals(ArtDmxPacket? other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return OpCode == other.OpCode && MaxPacketLength == other.MaxPacketLength && Sequence == other.Sequence && Physical == other.Physical && Address.Equals(other.Address) && Data.SequenceEqual(other.Data);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == this.GetType() && Equals((ArtDmxPacket)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)OpCode;
				hashCode = (hashCode * 397) ^ MaxPacketLength;
				hashCode = (hashCode * 397) ^ Sequence;
				hashCode = (hashCode * 397) ^ Physical;
				hashCode = (hashCode * 397) ^ Address.GetHashCode();
				hashCode = (hashCode * 397) ^ Data.GetHashCode();
				return hashCode;
			}
		}

		internal static ArtDmxPacket? Deserialize(ArtNetBinaryReader reader)
		{
			if (!DeserializeHeader(reader))
				return null;

			int sequence = reader.ReadByte();
			int physical = reader.ReadByte();
			int subUni = reader.ReadByte();
			int net = reader.ReadByte();
			int dataLength = reader.ReadUInt16();
			var data = reader.ReadBytes(dataLength);

			if (data.Length != dataLength)
				return null;

			try
			{
				return new ArtDmxPacket(DmxUniverseAddress.FromArtNetAddress(subUni & 0x0F, (subUni & 0xF0) >> 4, net), data, sequence, physical);
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}
		}

	    protected override void Serialize(ArtNetBinaryWriter writer)
		{
			SerializeHeader(writer);
			writer.Write((byte)Sequence);
			writer.Write((byte)Physical);
			writer.Write((byte)((Address.ArtNetSubNet << 4) + Address.ArtNetUniverse));
			writer.Write((byte)Address.ArtNetNet);
			writer.Write((byte)((Data.Length & 0xFF00) >> 8));
			writer.Write((byte)(Data.Length & 0x00FF));
			writer.Write(Data);
		}
	}
}