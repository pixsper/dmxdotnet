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
using System.Diagnostics.CodeAnalysis;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum ArtNetTimeCodeFormat : byte
	{
		F24 = 0,
		F25 = 1,
		F30DF = 2,
		F30ND = 3
	}



	internal class ArtTimeCodePacket : ArtPacket, IEquatable<ArtTimeCodePacket>
	{
		/// <exception cref="ArgumentOutOfRangeException">
		///     <paramref name="hours" /> is not in range 0-23. --or--
		///     <paramref name="minutes" /> is not in range 0-59. --or--
		///     <paramref name="seconds" /> is not in range 0-59. --or--
		///     <paramref name="frames" /> is not in range 0-MaxFrameIndex.
		/// </exception>
		public ArtTimeCodePacket(ArtNetTimeCodeFormat format, int hours, int minutes, int seconds, int frames)
		{
			Format = format;

			if (hours < 0 || hours > 23)
				throw new ArgumentOutOfRangeException(nameof(hours), hours, "Must be in range 0 to 23");

			Hours = hours;

			if (minutes < 0 || minutes > 59)
				throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Must be in range 0 to 59");

			Minutes = minutes;

			if (seconds < 0 || seconds > 59)
				throw new ArgumentOutOfRangeException(nameof(hours), hours, "Must be in range 0 to 59");

			Seconds = seconds;

			if (frames < 0 || frames > MaxFrameIndex)
				throw new ArgumentOutOfRangeException(nameof(hours), hours, $"Must be in range 0 to {MaxFrameIndex}");

			Frames = frames;
		}

		public override ArtNetOpCode OpCode => ArtNetOpCode.OpTimeCode;
		internal override int MaxPacketLength => 19;


		public ArtNetTimeCodeFormat Format { get; }

		public int Hours { get; }
		public int Minutes { get; }
		public int Seconds { get; }
		public int Frames { get; }

		/// <exception cref="ArgumentOutOfRangeException" accessor="get">Format is not an enumeration value</exception>
		public int MaxFrameIndex
		{
			get
			{
				switch (Format)
				{
					case ArtNetTimeCodeFormat.F24:
						return 23;
					case ArtNetTimeCodeFormat.F25:
						return 24;
					case ArtNetTimeCodeFormat.F30DF:
						return 29;
					case ArtNetTimeCodeFormat.F30ND:
						return 29;
					default:
						throw new ArgumentOutOfRangeException(nameof(Format), Format, "Must be an enumeration value");
				}
			}
		}

		
		public static ArtTimeCodePacket? Deserialize(ArtNetBinaryReader reader)
		{
			DeserializeHeader(reader);

			reader.ReadBytes(2);

			int frames = reader.ReadByte();
			int seconds = reader.ReadByte();
			int minutes = reader.ReadByte();
			int hours = reader.ReadByte();

			ArtNetTimeCodeFormat format;

			switch (reader.ReadByte())
			{
				case 0:
					format = ArtNetTimeCodeFormat.F24;
					break;
				case 1:
					format = ArtNetTimeCodeFormat.F25;
					break;
				case 2:
					format = ArtNetTimeCodeFormat.F30DF;
					break;
				case 3:
					format = ArtNetTimeCodeFormat.F30ND;
					break;
				default:
					return null;
			}

			try
			{
				return new ArtTimeCodePacket(format, hours, minutes, seconds, frames);
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}
		}

	    protected override void Serialize(ArtNetBinaryWriter writer)
		{
			SerializeHeader(writer);

			writer.Write(new byte[] {0, 0});
			writer.Write((byte)Frames);
			writer.Write((byte)Seconds);
			writer.Write((byte)Minutes);
			writer.Write((byte)Hours);
			writer.Write((byte)Format);
		}


		public bool Equals(ArtTimeCodePacket? other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return Format == other.Format && Hours == other.Hours && Minutes == other.Minutes && Seconds == other.Seconds
			       && Frames == other.Frames;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == GetType() && Equals((ArtTimeCodePacket)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (int)Format;
				hashCode = (hashCode * 397) ^ Hours;
				hashCode = (hashCode * 397) ^ Minutes;
				hashCode = (hashCode * 397) ^ Seconds;
				hashCode = (hashCode * 397) ^ Frames;
				return hashCode;
			}
		}
	}
}