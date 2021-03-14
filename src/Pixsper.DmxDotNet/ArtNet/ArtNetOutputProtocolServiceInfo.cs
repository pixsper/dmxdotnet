// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;

namespace Pixsper.DmxDotNet.Artnet;

public record ArtNetOutputProtocolServiceInfo : IDmxOutputProtocolServiceInfo
{
	public ArtNetOutputProtocolServiceInfo()
	{
		UnicastAddress = IPAddress.None;
	}

	public ArtNetOutputProtocolServiceInfo(Guid? id, DmxUniverseAddress universeAddress, bool isBroadcast, bool isAltBroadcastAddress, 
		IPAddress? unicastAddress = null)
	{
		Id = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
		UniverseAddress = universeAddress;
		IsBroadcast = isBroadcast;
		IsAltBroadcastAddress = isAltBroadcastAddress;
		UnicastAddress = unicastAddress ?? IPAddress.Loopback;
	}

	public ArtNetOutputProtocolServiceInfo(Guid? id, DmxUniverseAddress universeAddress, IPAddress? unicastAddress = null)
		: this(id, universeAddress, unicastAddress is null, false, unicastAddress)
	{

	}


	public Guid Id { get; init; }
	public DmxUniverseAddress UniverseAddress { get; init; }

	public bool IsBroadcast { get; init; }
	public bool IsAltBroadcastAddress { get; init; }

	public IPAddress UnicastAddress { get; init; }

	public string ProtocolName => "Art-Net";
}