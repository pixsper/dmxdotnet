// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Text;

namespace Pixsper.DmxDotNet;

internal static class StringExtensions
{
	public static string ToStringOrEmpty(this object? obj)
	{
		return obj?.ToString() ?? String.Empty;
	}

	public static string TruncateToUtf8ByteLength(this string value, int maxLength)
	{
		if (Encoding.UTF8.GetByteCount(value) <= maxLength)
			return value;

		var a = Encoding.UTF8.GetBytes(value);

		if (maxLength > 0 && (a[maxLength] & 0xC0) == 0x80)
			while (--maxLength > 0 && (a[maxLength] & 0xC0) == 0x80) { }

		return Encoding.UTF8.GetString(a, 0, maxLength);
	}

	public static string TruncateToAsciiByteLength(this string value, int maxLength)
	{
		if (Encoding.ASCII.GetByteCount(value) <= maxLength)
			return value;

		return value.Substring(0, maxLength);
	}

	public static string TruncateToUtf32ByteLength(this string value, int maxLength)
	{
		if (Encoding.UTF32.GetByteCount(value) <= maxLength)
			return value;

		return value.Substring(0, maxLength / 4);
	}

	public static string TruncateToByteLength(this string value, int maxLength, Encoding encoding)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		if (ReferenceEquals(encoding, Encoding.UTF8))
			return TruncateToUtf8ByteLength(value, maxLength);

		if (ReferenceEquals(encoding, Encoding.ASCII))
			return TruncateToAsciiByteLength(value, maxLength);

		if (ReferenceEquals(encoding, Encoding.UTF32))
			return TruncateToUtf32ByteLength(value, maxLength);

		if (encoding.GetByteCount(value) <= maxLength)
			return value;

		var encoder = encoding.GetEncoder();
		byte[] buffer = new byte[maxLength];
		char[] valueChars = value.ToCharArray();
		encoder.Convert(valueChars, 0, valueChars.Length, buffer, 0, buffer.Length, false,
			out var charsUsed, out var bytesUsed, out var completed);

		return encoding.GetString(buffer, 0, bytesUsed);
	}
}