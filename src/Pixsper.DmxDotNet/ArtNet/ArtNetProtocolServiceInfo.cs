// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Net;

namespace Pixsper.DmxDotNet.Artnet;

public record ArtNetProtocolServiceInfo : IDmxProtocolServiceInfo
{
	public ArtNetProtocolServiceInfo()
	{
		AdapterIp = IPAddress.None;
	}

	public ArtNetProtocolServiceInfo(double outputFramerate, IPAddress adapterIp)
	{
		OutputFramerate = outputFramerate;
		AdapterIp = adapterIp;
	}

        
	public double OutputFramerate { get; init; }
	public IPAddress AdapterIp { get; init; }
}