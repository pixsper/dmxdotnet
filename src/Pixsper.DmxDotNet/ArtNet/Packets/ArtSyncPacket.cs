using System;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
	internal class ArtSyncPacket : ArtPacket, IEquatable<ArtSyncPacket>
	{
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="aux1"/> is not in range 0-255. --or--
		///		<paramref name="aux2"/> is not in range 0-255. 
		/// </exception>
		public ArtSyncPacket(int aux1 = 0, int aux2 = 0)
		{
			if (aux1 < 0 || aux1 > 255)
				throw new ArgumentOutOfRangeException(nameof(aux1), aux1, "Must be in range 0-255");

			Aux1 = aux1;

			if (aux2 < 0 || aux2 > 255)
				throw new ArgumentOutOfRangeException(nameof(aux2), aux2, "Must be in range 0-255");

			Aux2 = aux2;
		}

		public override ArtNetOpCode OpCode => ArtNetOpCode.OpSync;
		internal override int MaxPacketLength => 14;

		public int Aux1 { get; }
		public int Aux2 { get; }

		public bool Equals(ArtSyncPacket? other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Aux1 == other.Aux1 && Aux2 == other.Aux2;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == GetType() && Equals((ArtSyncPacket)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Aux1 * 397) ^ Aux2;
			}
		}

		internal static ArtSyncPacket? Deserialize(ArtNetBinaryReader reader)
		{
			DeserializeHeader(reader);

			int aux1 = reader.ReadByte();
			int aux2 = reader.ReadByte();

			return new ArtSyncPacket(aux1, aux2);
		}

	    protected override void Serialize(ArtNetBinaryWriter writer)
		{
			SerializeHeader(writer);
			writer.Write((byte)Aux1);
			writer.Write((byte)Aux2);
		}
	}
}