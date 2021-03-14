// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Pixsper.DmxDotNet.SAcn.Packets;

namespace Pixsper.DmxDotNet.SAcn;

internal class SAcnProtocolService 
	: DmxIpProtocolService<SAcnProtocolServiceInfo, SAcnInputProtocolService, SAcnInputProtocolServiceInfo, SAcnOutputProtocolService, SAcnOutputProtocolServiceInfo>
{
	internal const int SAcnPort = 5568;

	internal static readonly DmxUniverseAddress SAcnUniverseMin = 0;
	internal static readonly DmxUniverseAddress SAcnUniverseMax = 63999;

	public SAcnProtocolService(ILogger log, SAcnProtocolServiceInfo info)
		: base(log, info, new IPEndPoint(info.AdapterIp, SAcnPort), info.OutputFramerate)
	{

	}

	public override string ProtocolName => "sACN";

	protected override SAcnInputProtocolService? CreateInputProtocolService(SAcnInputProtocolServiceInfo info)
	{
		if (info.UniverseAddress.Universe < SAcnUniverseMin || info.UniverseAddress > SAcnUniverseMax)
			return null;

		if (!GetInputServicesForUniverse(info.UniverseAddress).Any())
		{
			bool isSuccess = JoinMulticastGroup(info.UniverseAddress.ToSAcnMulticastIp());
			if (!isSuccess)
			{
				Log.LogWarning("Failed to join sACN multicast group for universe {UniverseAddress}", info.UniverseAddress);
				return null;
			}
		}

		return new SAcnInputProtocolService(Log, this, info);
	}

	protected override SAcnOutputProtocolService? CreateOutputProtocolService(SAcnOutputProtocolServiceInfo info)
	{
		if (info.UniverseAddress.Universe < SAcnUniverseMin || info.UniverseAddress > SAcnUniverseMax)
			return null;

		return new SAcnOutputProtocolService(Log, this, info);
	}

	protected override void OnRemoveInputProtocolService(SAcnInputProtocolService service)
	{
		if (!GetInputServicesForUniverse(service.Info.UniverseAddress).Any())
			DropMulticastGroup(service.Info.UniverseAddress.ToSAcnMulticastIp());
	}

	protected override IDmxDataPacket? DeserializePacket(ReadOnlySpan<byte> data)
	{
		var packet = SAcnPacket.Deserialize(data);

		return packet switch
		{
			SAcnDataPacket dataPacket => dataPacket,
			_ => null
		};
	}
}