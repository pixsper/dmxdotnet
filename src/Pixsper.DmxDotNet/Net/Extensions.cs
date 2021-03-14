// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Pixsper.DmxDotNet.Net;

internal static class Extensions
{
	public static bool IsLoopback(this IPAddress value)
	{
		return value.Equals(IPAddress.Loopback);
	}

	public static bool IsAny(this IPAddress value)
	{
		return value.Equals(IPAddress.Any);
	}

	public static bool IsMulticast(this IPAddress value)
	{
		if (value.AddressFamily != AddressFamily.InterNetwork)
			return false;

		var bytes = value.GetAddressBytes();

		return bytes[0] >= 224 && bytes[0] <= 239;
	}

	public static string ToFormattedString(this PhysicalAddress value)
	{
		return string.Join (":", value.GetAddressBytes().Select(b => b.ToString("X2")));
	}
}