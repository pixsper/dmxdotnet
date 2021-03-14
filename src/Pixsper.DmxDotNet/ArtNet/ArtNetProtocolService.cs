// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Pixsper.DmxDotNet.Artnet.Packets;

namespace Pixsper.DmxDotNet.Artnet;

internal class ArtNetProtocolService 
	: DmxIpProtocolService<ArtNetProtocolServiceInfo, ArtNetInputProtocolService, ArtNetInputProtocolServiceInfo, ArtNetOutputProtocolService, ArtNetOutputProtocolServiceInfo>
{
	internal const int ArtNetPort = 0x1936;

	internal static readonly DmxUniverseAddress ArtNetUniverseMin = 0;
	internal static readonly DmxUniverseAddress ArtNetUniverseMax = 32767;

	internal static readonly IPAddress ArtNetBroadcastAddress = IPAddress.Parse("2.255.255.255");
	internal static readonly IPAddress ArtNetAltBroadcastAddress = IPAddress.Parse("10.255.255.255");
        
	public ArtNetProtocolService(ILogger log, ArtNetProtocolServiceInfo info) 
		: base(log, info, new IPEndPoint(info.AdapterIp, ArtNetPort), info.OutputFramerate)
	{

	}

	public override string ProtocolName => "Art-Net";


	protected override ArtNetInputProtocolService? CreateInputProtocolService(ArtNetInputProtocolServiceInfo info)
	{
		if (info.UniverseAddress.Universe < ArtNetUniverseMin || info.UniverseAddress > ArtNetUniverseMax)
			return null;

		return new ArtNetInputProtocolService(Log, this, info);
	}

	protected override ArtNetOutputProtocolService? CreateOutputProtocolService(ArtNetOutputProtocolServiceInfo info)
	{
		if (info.UniverseAddress.Universe < ArtNetUniverseMin || info.UniverseAddress > ArtNetUniverseMax)
			return null;

		return new ArtNetOutputProtocolService(Log, this, info);
	}

	protected override IDmxDataPacket? DeserializePacket(ReadOnlySpan<byte> data)
	{
		var packet = ArtPacket.Deserialize(data);
		if (packet == null)
			return null;

		if (packet.OpCode != ArtPacket.ArtNetOpCode.OpDmx)
			return null;

		return (ArtDmxPacket)packet;
	}
}