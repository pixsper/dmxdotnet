// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;

namespace Pixsper.DmxDotNet;

public enum DmxParameterResolution
{
	[Description("8-Bit")]
	_8Bit = 8,
	[Description("16-Bit")]
	_16Bit = 16,
	[Description("24-Bit")]
	_24Bit = 24,
	[Description("32-Bit")]
	_32Bit = 32
}

public static class FixtureParameterResolutionExtensions
{
	public static long MaxValue(this DmxParameterResolution value)
	{
		return value switch
		{
			DmxParameterResolution._8Bit => byte.MaxValue,
			DmxParameterResolution._16Bit => ushort.MaxValue,
			DmxParameterResolution._24Bit => (byte.MaxValue << 16) | (byte.MaxValue << 8) | byte.MaxValue,
			DmxParameterResolution._32Bit => uint.MaxValue,
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
		};
	}

	public static int Width(this DmxParameterResolution value)
	{
		return value switch
		{
			DmxParameterResolution._8Bit => 1,
			DmxParameterResolution._16Bit => 2,
			DmxParameterResolution._24Bit => 3,
			DmxParameterResolution._32Bit => 4,
			_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
		};
	}
}