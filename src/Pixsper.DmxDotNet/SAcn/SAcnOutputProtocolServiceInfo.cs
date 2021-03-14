// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;

namespace Pixsper.DmxDotNet.SAcn;

public record SAcnOutputProtocolServiceInfo : IDmxOutputProtocolServiceInfo
{
	public SAcnOutputProtocolServiceInfo()
	{
		SourceName = string.Empty;
		UnicastAddress = IPAddress.None;
	}

	public SAcnOutputProtocolServiceInfo(Guid? id, DmxUniverseAddress universeAddress, 
		string sourceName, bool isUnicast, IPAddress? unicastAddress = null)
	{
		Id = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
		UniverseAddress = universeAddress;
		SourceName = sourceName;
		IsUnicast = isUnicast;
		UnicastAddress = unicastAddress ?? IPAddress.Loopback;
	}

	public SAcnOutputProtocolServiceInfo(Guid? id, DmxUniverseAddress universeAddress,
		string sourceName, IPAddress? unicastAddress = null)
		: this(id, universeAddress, sourceName, 
			unicastAddress is not null, unicastAddress)
	{
	        
	}


	public Guid Id { get; init; }
	public DmxUniverseAddress UniverseAddress { get; init; }

	public string SourceName { get; init; }

	public bool IsUnicast { get; init; }

	public IPAddress UnicastAddress { get; init; }

	public string ProtocolName => "sAcn";
}