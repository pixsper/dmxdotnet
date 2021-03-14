// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Pixsper.DmxDotNet.SAcn;

public record SAcnProtocolServiceInfo : IDmxProtocolServiceInfo
{
	public SAcnProtocolServiceInfo()
	{
		AdapterIp = IPAddress.None;
	}

	public SAcnProtocolServiceInfo(Guid cId, double outputFramerate, IPAddress adapterIp)
	{
		CId = cId;
		OutputFramerate = outputFramerate;
		AdapterIp = adapterIp;
	}

	public SAcnProtocolServiceInfo(double outputFramerate, IPAddress adapterIp)
	{
		OutputFramerate = outputFramerate;
		AdapterIp = adapterIp;

		using var md5 = MD5.Create();
		CId = new Guid(md5.ComputeHash(Encoding.Default.GetBytes(Environment.MachineName)));
	}

	public Guid CId { get; init; }

	public double OutputFramerate { get; init; }
	public IPAddress AdapterIp { get; init; }
}