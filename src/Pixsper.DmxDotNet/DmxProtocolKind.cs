// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Pixsper.DmxDotNet;

public enum DmxProtocolKind
{
	[Description("Art-Net")]
	ArtNet,
	[Description("sACN")]
	SAcn
}

public static class DmxProtocolKindHelpers
{
	public static DmxProtocolKind Parse(string str)
	{
		if (TryParse(str, out var value))
			return value;
		else
			throw new FormatException($"Invalid {nameof(DmxProtocolKind)} value");
	}

	public static bool TryParse(string str, out DmxProtocolKind value)
	{
		value = default;

		switch (str.ToLower(CultureInfo.InvariantCulture))
		{
			case "artnet":
			case "art-net":
				value = DmxProtocolKind.ArtNet;
				return true;

			case "sacn":
			case "acn":
			case "streaming acn":
			case "e1.31":
				value = DmxProtocolKind.SAcn;
				return true;

			default:
				return false;
		}

	}
}