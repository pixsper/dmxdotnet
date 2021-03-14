// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Buffers.Binary;
using System.Text;

namespace Pixsper.DmxDotNet.Serialization;

/// <summary>
/// Wrapper around Span for serializing binary data
/// </summary>
internal ref struct SpanWriter
{
	private readonly Span<byte> _data;
	private int _position;

	public SpanWriter(Span<byte> data, Encoding encoding, ByteOrder endianness)
	{
		_data = data;
		Encoding = encoding;
		Endianness = endianness;
		_position = 0;
	}


	public ByteOrder Endianness { get; }

	public Encoding Encoding { get; }


	public void Write(byte value)
	{
		_data[_position++] = value;
	}

	public void Write(ReadOnlyMemory<byte> values)
	{
		var slice = _data.Slice(_position, values.Length);
		values.Span.CopyTo(slice);
		_position += values.Length;
	}

	public void Skip(int count)
	{
		_position += count;
	}

	public void Write(short value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteInt16BigEndian(_data.Slice(_position, sizeof(short)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteInt16LittleEndian(_data.Slice(_position, sizeof(short)), value);
				break;
		}

		_position += sizeof(short);
	}

	public void Write(ushort value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteUInt16BigEndian(_data.Slice(_position, sizeof(ushort)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteUInt16LittleEndian(_data.Slice(_position, sizeof(ushort)), value);
				break;
		}

		_position += sizeof(ushort);
	}

	public void Write(int value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteInt32BigEndian(_data.Slice(_position, sizeof(int)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteInt32LittleEndian(_data.Slice(_position, sizeof(int)), value);
				break;
		}

		_position += sizeof(int);
	}

	public void Write(uint value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteUInt32BigEndian(_data.Slice(_position, sizeof(uint)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteUInt32LittleEndian(_data.Slice(_position, sizeof(uint)), value);
				break;
		}

		_position += sizeof(uint);
	}

	public void Write(long value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteInt64BigEndian(_data.Slice(_position, sizeof(long)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteInt64LittleEndian(_data.Slice(_position, sizeof(long)), value);
				break;
		}

		_position += sizeof(long);
	}

	public void Write(ulong value, ByteOrder? overrideEndianness = null)
	{
		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian:
			default:
				BinaryPrimitives.WriteUInt64BigEndian(_data.Slice(_position, sizeof(ulong)), value);
				break;
			case ByteOrder.LittleEndian:
				BinaryPrimitives.WriteUInt64LittleEndian(_data.Slice(_position, sizeof(ulong)), value);
				break;
		}

		_position += sizeof(ulong);
	}


	public void Write(string value)
	{
		var valueBytes = Encoding.GetBytes(value);

		valueBytes.CopyTo(_data.Slice(_position, valueBytes.Length));

		_data[_position + valueBytes.Length] = 0;
		_position += valueBytes.Length + 1;
	}

	public void Write(string value, int fieldLength)
	{
		string trimmedValue = value.TruncateToByteLength(fieldLength - 1, Encoding);

		Encoding.GetBytes(trimmedValue).CopyTo(_data.Slice(_position, fieldLength - 1));

		_data[_position + fieldLength] = 0;
		_position += fieldLength;
	}
}