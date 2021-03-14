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
using System.Collections.Generic;
using System.Linq;

namespace Pixsper.DmxDotNet
{
	public class DmxUniverse : IEquatable<DmxUniverse>
    {
        public const int MaxChannelCount = 512;

        private readonly byte[] _data;

        /// <exception cref="ArgumentNullException"><paramref name="src" /> is null. </exception>
        /// <exception cref="ArgumentException">
        ///     The number of bytes in <paramref name="src" /> is less than
        ///     <paramref name="srcOffset" /> plus <paramref name="count" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="srcOffset" /> or <paramref name="count" /> is less than
        ///     0.
        /// </exception>
        public static DmxUniverse FromSourceArray(int universeNumber, byte[] src, int srcOffset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(src, srcOffset, data, 0, count);

            return new DmxUniverse(universeNumber, data);
        }

        /// <exception cref="ArgumentNullException"><paramref name="data" /> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="universeNumber" /> is less than 0. </exception>
        /// <exception cref="ArgumentException"><paramref name="data" /> is too long. </exception>
        public DmxUniverse(int universeNumber, byte[] data)
        {
            if (universeNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(universeNumber), universeNumber, "Must be greater than 0");

            UniverseNumber = universeNumber;

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length > MaxChannelCount)
                throw new ArgumentException($"data cannot be longer than {MaxChannelCount} bytes");

            _data = data;
        }

        /// <exception cref="ArgumentNullException"><paramref name="data" /> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="universeNumber" /> is less than 0. </exception>
        /// <exception cref="ArgumentException"><paramref name="data" /> is too long. </exception>
        public DmxUniverse(int universeNumber, IEnumerable<byte> data)
            :this(universeNumber, data.ToArray())
        {
            
        }

        public int ChannelCount => _data.Length;

        public int UniverseNumber { get; }

        internal byte[] Data => _data;


        public byte this[int index] => _data[index];


        public DmxUniverse MergeHtp(DmxUniverse other)
        {
            var data = new byte[Math.Max(ChannelCount, other.ChannelCount)];

            DmxUniverse smallerUniverse;
            DmxUniverse largerUniverse;

            if (ChannelCount < other.ChannelCount)
            {
                smallerUniverse = this;
                largerUniverse = other;
            }
            else
            {
                smallerUniverse = other;
                largerUniverse = this;
            }


            int i;
            for (i = 0; i < smallerUniverse.ChannelCount; ++i)
                data[i] = Math.Max(smallerUniverse[i], largerUniverse[i]);

            while (i < largerUniverse.ChannelCount)
            {
                data[i] = largerUniverse[i];
                ++i;
            }

            return new DmxUniverse(other.UniverseNumber, data);
        }


        public bool Equals(DmxUniverse? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Data.SequenceEqual(other.Data) && UniverseNumber == other.UniverseNumber;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((DmxUniverse)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Data?.GetHashCode() ?? 0) * 397) ^ UniverseNumber;
            }
        }

        public static bool operator ==(DmxUniverse left, DmxUniverse right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DmxUniverse left, DmxUniverse right)
        {
            return !Equals(left, right);
        }
    }
}