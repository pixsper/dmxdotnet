// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Pixsper.DmxDotNet;

public readonly struct DmxAddress : IEquatable<DmxAddress>, IComparable<DmxAddress>, IComparable
{
	public const int DmxChannelMin = 1;
	public const int DmxChannelMax = 512;

	public static bool TryParse(string str, out DmxAddress value)
	{
		value = default;

		var values = str.Split('.', ',', ' ', ':', '/', '\\', '-', '_');

		try
		{
			switch (values.Length)
			{
				case 1:
				{
					if (!int.TryParse(values[0], out int iValue))
						return false;

					value = new DmxAddress(iValue);
					return true;
				}

				case 2:
				{
					if (!int.TryParse(values[0], out int iValueUniverse) || !int.TryParse(values[1], out int iValueChannel))
						return false;

					value = new DmxAddress(iValueUniverse, iValueChannel);
					return true;
				}

				default:
					return false;
			}
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
	}


	public DmxAddress(DmxUniverseAddress universe, int channel)
	{
		Universe = universe;

		if (channel < DmxChannelMin || channel > DmxChannelMax)
			throw new ArgumentOutOfRangeException(nameof(channel));

		Channel = channel;
	}

	public DmxAddress(int universe, int channel)
		: this(new DmxUniverseAddress(universe), channel)
	{
	}


	public DmxAddress(int globalChannel)
		: this(globalChannel / 512, (globalChannel % 512) + 1)
	{
	}

	public DmxAddress(int artNetNet, int artNetSubNet, int artNetUniverse, int channel)
		: this(new DmxUniverseAddress(artNetNet, artNetSubNet, artNetUniverse), channel)
	{
          
	}


	public DmxUniverseAddress Universe { get; }
	public int Channel { get; }

	public int GlobalChannel => Universe.GlobalChannel + (Channel - 1);

	public int ArtNetUniverse => Universe.ArtNetUniverse % 16;
	public int ArtNetSubNet => Universe.ArtNetSubNet / 16 % 16;
	public int ArtNetNet => Universe.ArtNetNet / 256;


	public bool CanFitWithinUniverse(int numChannels)
	{
		return (Channel - 1) + numChannels <= 512;
	}

	public DmxAddress With(DmxUniverseAddress? universe = null, int? channel = null)
	{
		return new(universe ?? Universe, channel ?? Channel);
	}


	public bool Equals(DmxAddress other)
	{
		return Universe == other.Universe && Channel == other.Channel;
	}

	public override bool Equals(object? obj)
	{
		return obj is DmxAddress other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Universe, Channel);
	}

	public static bool operator ==(DmxAddress left, DmxAddress right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DmxAddress left, DmxAddress right)
	{
		return !left.Equals(right);
	}

	public int CompareTo(DmxAddress other)
	{
		return GlobalChannel.CompareTo(other.GlobalChannel);
	}

	public int CompareTo(object? obj)
	{
		if (ReferenceEquals(null, obj)) return 1;
		return obj is DmxAddress other
			? CompareTo(other)
			: throw new ArgumentException($"Object must be of type {nameof(DmxAddress)}");
	}

	public static bool operator <(DmxAddress left, DmxAddress right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator >(DmxAddress left, DmxAddress right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator <=(DmxAddress left, DmxAddress right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >=(DmxAddress left, DmxAddress right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static implicit operator int(DmxAddress value) => value.GlobalChannel;

	public static implicit operator DmxAddress(int value) => new(value);

	public override string ToString() => $"{Universe}.{Channel}";
}