// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;

namespace Pixsper.DmxDotNet;

public readonly struct DmxUniverseAddress : IEquatable<DmxUniverseAddress>, IComparable<DmxUniverseAddress>, IComparable
{
	public const int DmxUniverseLength = 512;
	public const int DmxUniverseMin = 0;
	public const int DmxUniverseMax = 63999;

	public const int ArtNetNetMin = 0;
	public const int ArtNetNetMax = 128;
	public const int ArtNetSubNetMin = 0;
	public const int ArtNetSubNetMax = 15;
	public const int ArtNetUniverseMin = 0;
	public const int ArtNetUniverseMax = 15;

	public const int SAcnUniverseMin = 1;
	public const int SAcnUniverseMax = 63999;


	public static DmxUniverseAddress Parse(string str)
	{
		var values = str.Split('.', ',', ' ', ':', '/', '\\', '-', '_');

		try
		{
			switch (values.Length)
			{
				case 1:
					return new DmxUniverseAddress(int.Parse(str));

				case 2:
					return new DmxUniverseAddress(0, int.Parse(values[0]), int.Parse(values[1]));

				case 3:
					return new DmxUniverseAddress(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]));
			}
		}
		catch (FormatException ex)
		{
			throw new FormatException($"Invalid {nameof(DmxUniverseAddress)} format", ex);
		}

		throw new FormatException($"Invalid {nameof(DmxUniverseAddress)} format");
	}

	public static bool TryParse(string str, out DmxUniverseAddress value)
	{
		value = default;

		try
		{
			value = Parse(str);
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
	}
	public static DmxUniverseAddress? TryParse(string str)
	{
		DmxUniverseAddress? value = null;

		try
		{
			value = Parse(str);
		}
		catch (FormatException) { }

		return value;
	}


	public DmxUniverseAddress(int universe)
	{
		if (universe < DmxUniverseMin || universe > DmxUniverseMax)
			throw new ArgumentOutOfRangeException(nameof(universe));

		Universe = universe;
	}

	public DmxUniverseAddress(int artNetNet, int artNetSubNet, int artNetUniverse)
	{
		if (artNetNet < ArtNetNetMin || artNetNet > ArtNetNetMax)
			throw new ArgumentOutOfRangeException(nameof(artNetNet));

		if (artNetSubNet < ArtNetSubNetMin || artNetSubNet > ArtNetSubNetMax)
			throw new ArgumentOutOfRangeException(nameof(artNetSubNet));

		if (artNetUniverse < ArtNetUniverseMin || artNetUniverse > ArtNetUniverseMax)
			throw new ArgumentOutOfRangeException(nameof(artNetUniverse));

		Universe = (artNetNet << 8) | (artNetSubNet << 4) | artNetUniverse;
	}


	public int Universe { get; }

	public int GlobalChannel => Universe * DmxUniverseLength;

	public int ArtNetUniverse => Universe % 16;
	public int ArtNetSubNet => Universe / 16 % 16;
	public int ArtNetNet => Universe / 256;


	public DmxAddress With(int? universe = null)
	{
		return new(universe ?? Universe);
	}

	public bool Equals(DmxUniverseAddress other)
	{
		return Universe == other.Universe;
	}

	public override bool Equals(object? obj)
	{
		return obj is DmxAddress other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Universe);
	}

	public static bool operator ==(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return !left.Equals(right);
	}

	public int CompareTo(DmxUniverseAddress other)
	{
		return GlobalChannel.CompareTo(other.GlobalChannel);
	}

	public int CompareTo(object? obj)
	{
		if (ReferenceEquals(null, obj)) return 1;
		return obj is DmxUniverseAddress other
			? CompareTo(other)
			: throw new ArgumentException($"Object must be of type {nameof(DmxUniverseAddress)}");
	}

	public static bool operator <(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator >(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator <=(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >=(DmxUniverseAddress left, DmxUniverseAddress right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static implicit operator int(DmxUniverseAddress value) => value.Universe;

	public static implicit operator DmxUniverseAddress(int value) => new(value);

	public IPAddress ToSAcnMulticastIp()
	{
		return new(new byte[]{ 239, 255, (byte)((Universe & 0xFF00) >> 8), (byte)(Universe & 0x00FF) });
	}


	public override string ToString()
	{
		return $"{Universe}";
	}
}