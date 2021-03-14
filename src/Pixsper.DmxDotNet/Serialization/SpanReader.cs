// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Buffers.Binary;
using System.Text;

namespace Pixsper.DmxDotNet.Serialization;

/// <summary>
/// Wrapper around ReadOnlySpan for deserializing binary data
/// </summary>
internal ref struct SpanReader
{
	private readonly ReadOnlySpan<byte> _data;
	private int _position;

	public SpanReader(ReadOnlySpan<byte> data, Encoding encoding, ByteOrder endianness)
	{
		_data = data;
		Encoding = encoding;
		Endianness = endianness;
		_position = 0;
	}


	public ByteOrder Endianness { get; }

	public Encoding Encoding { get; }


	public byte ReadByte()
	{
		return _data[_position++];
	}

	public ReadOnlySpan<byte> ReadBytes(int count)
	{
		var slice = _data.Slice(_position, count);
		_position += count;
		return slice;
	}

	public void Skip(int count)
	{
		_position += count;
	}

	public short ReadShort(ByteOrder? overrideEndianness = null)
	{
		short value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadInt16BigEndian(_data.Slice(_position, sizeof(short)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadInt16LittleEndian(_data.Slice(_position, sizeof(short)));
				break;
		}

		_position += sizeof(short);

		return value;
	}

	public ushort ReadUShort(ByteOrder? overrideEndianness = null)
	{
		ushort value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(_position, sizeof(ushort)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position, sizeof(ushort)));
				break;
		}

		_position += sizeof(ushort);

		return value;
	}

	public int ReadInt(ByteOrder? overrideEndianness = null)
	{
		int value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadInt32BigEndian(_data.Slice(_position, sizeof(int)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(_position, sizeof(int)));
				break;
		}

		_position += sizeof(int);

		return value;
	}

	public uint ReadUInt(ByteOrder? overrideEndianness = null)
	{
		uint value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(_position, sizeof(uint)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(_position, sizeof(uint)));
				break;
		}

		_position += sizeof(uint);

		return value;
	}

	public long ReadLong(ByteOrder? overrideEndianness = null)
	{
		long value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadInt64BigEndian(_data.Slice(_position, sizeof(long)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadInt64LittleEndian(_data.Slice(_position, sizeof(long)));
				break;
		}

		_position += sizeof(long);

		return value;
	}

	public ulong ReadULong(ByteOrder? overrideEndianness = null)
	{
		ulong value;

		switch (overrideEndianness ?? Endianness)
		{
			case ByteOrder.BigEndian: 
			default:
				value = BinaryPrimitives.ReadUInt64BigEndian(_data.Slice(_position, sizeof(ulong)));
				break;
			case ByteOrder.LittleEndian:
				value = BinaryPrimitives.ReadUInt64BigEndian(_data.Slice(_position, sizeof(ulong)));
				break;
		}

		_position += sizeof(ulong);

		return value;
	}

	public string ReadNullTerminatedString()
	{
		int end = _position;

		while(_data[end] != '\0')
			++end;

		var stringSlice = _data.Slice(_position, end - _position);

		_position = end + 1;

		return Encoding.UTF8.GetString(stringSlice);
	}

	public string ReadNullTerminatedString(int fieldLength)
	{
		int end = _position;

		for (int i = 0; i < fieldLength; ++i, ++end)
		{
			if (_data[end] == '\0')
				break;
		}

		var stringSlice = _data.Slice(_position, end - _position);

		_position += fieldLength;

		return Encoding.UTF8.GetString(stringSlice);
	}
}