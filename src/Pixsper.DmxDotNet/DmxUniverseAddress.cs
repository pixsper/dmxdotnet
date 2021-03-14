using System;

namespace Pixsper.DmxDotNet
{
    public readonly struct DmxUniverseAddress : IEquatable<DmxUniverseAddress>, IComparable<DmxUniverseAddress>
    {
	    public const int ArtNetAddressMin = 0;
	    public const int ArtNetAddressMax = 32767;
	    public const int SAcnAddressMin = 1;
	    public const int SAcnAddressMax = 63999;

		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="net"/> is not in range 0-127. --or--
		///		<paramref name="subNet"/> is not in range 0-15. --or--
		///		<paramref name="universe"/> is not in range 0-15.
		/// </exception>
		public static DmxUniverseAddress FromArtNetAddress(int universe, int subNet = 0, int net = 0)
	    {
			if (net < 0 || net > 127)
				throw new ArgumentOutOfRangeException(nameof(net), net, "Net must be in range 0-127");

			if (subNet < 0 || subNet > 15)
				throw new ArgumentOutOfRangeException(nameof(subNet), subNet, "SubNet must be in range 0-15");

			if (universe < 0 || universe > 15)
				throw new ArgumentOutOfRangeException(nameof(universe), universe, "Universe must be in range 0-15");

			return new DmxUniverseAddress((net << 8) + (subNet << 4) + universe);
		}

	    public static DmxUniverseAddress FromSAcnAddress(int universe)
	    {
			if (universe < SAcnAddressMin || universe > SAcnAddressMax)
				throw new ArgumentOutOfRangeException(nameof(universe), universe, $"Must be in range {SAcnAddressMin}-{SAcnAddressMax}");

		    return new DmxUniverseAddress(universe);
	    }

		public DmxUniverseAddress(int address)
        {
			if (address < 0 || address > SAcnAddressMax)
				throw new ArgumentOutOfRangeException(nameof(address), address, "Outside of any DMX protocol universe range");

            Address = address;
        }


        public int Address { get; }

        public int ArtNetNet => (Address & 0x7F00) >> 8;
        public int ArtNetSubNet => (Address & 0x00F0) >> 4;
        public int ArtNetUniverse => Address & 0x000F;


        public bool Equals(DmxUniverseAddress other)
        {
            return Address == other.Address;
        }

	    public int CompareTo(DmxUniverseAddress other)
	    {
		    return Address.CompareTo(other.Address);
	    }

	    public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is DmxUniverseAddress && Equals((DmxUniverseAddress)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ArtNetNet;
                hashCode = (hashCode * 397) ^ ArtNetSubNet;
                hashCode = (hashCode * 397) ^ ArtNetUniverse;
                return hashCode;
            }
        }

        public static bool operator ==(DmxUniverseAddress left, DmxUniverseAddress right) => left.Equals(right);

        public static bool operator !=(DmxUniverseAddress left, DmxUniverseAddress right) => !left.Equals(right);

        public static bool operator <(DmxUniverseAddress left, DmxUniverseAddress right) => left.Address < right.Address;

        public static bool operator >(DmxUniverseAddress left, DmxUniverseAddress right) => left.Address > right.Address;

        public static bool operator <=(DmxUniverseAddress left, DmxUniverseAddress right) => left.Address <= right.Address;

        public static bool operator >=(DmxUniverseAddress left, DmxUniverseAddress right) => left.Address >= right.Address;

        public static DmxUniverseAddress operator +(DmxUniverseAddress left, DmxUniverseAddress right) 
            => new DmxUniverseAddress(left.Address + right.Address);

        public static DmxUniverseAddress operator -(DmxUniverseAddress left, DmxUniverseAddress right) 
            => new DmxUniverseAddress(left.Address - right.Address);

        public static DmxUniverseAddress operator +(DmxUniverseAddress left, int right) 
            => new DmxUniverseAddress(left.Address + right);

        public static DmxUniverseAddress operator -(DmxUniverseAddress left, int right) 
            => new DmxUniverseAddress(left.Address - right);

        public static DmxUniverseAddress operator ++(DmxUniverseAddress a) => new DmxUniverseAddress(a.Address + 1);

        public static DmxUniverseAddress operator --(DmxUniverseAddress a) => new DmxUniverseAddress(a.Address - 1);

		public static implicit operator int(DmxUniverseAddress value) => value.Address;

	    public static implicit operator DmxUniverseAddress(int value) => new DmxUniverseAddress(value);

	    public override string ToString() => $"Net {ArtNetNet}, SubNet {ArtNetSubNet}, Universe {ArtNetUniverse}";
    }
}