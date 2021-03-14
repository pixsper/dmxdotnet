using System;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pixsper.DmxDotNet.Artnet;
using Pixsper.DmxDotNet.SAcn;

namespace Pixsper.DmxDotNet;

public static class DmxService
{
	public const double DmxMaxFramerate = 44;

	public static IDmxProtocolService Create(IDmxProtocolServiceInfo info) => Create(NullLogger.Instance, info);

	public static IDmxProtocolService Create(ILogger logger, IDmxProtocolServiceInfo info)
	{
		return info switch
		{
			ArtNetProtocolServiceInfo artNetInfo => new ArtNetProtocolService(logger, artNetInfo),
			SAcnProtocolServiceInfo sAcnInfo => new SAcnProtocolService(logger, sAcnInfo),
			_ => throw new ArgumentOutOfRangeException(nameof(info), "Unsupported DMX Protocol service kind")
		};
	}

	public static IDmxProtocolService Create(DmxProtocolKind protocol) => Create(protocol, NullLogger.Instance, IPAddress.Any);

	public static IDmxProtocolService Create(DmxProtocolKind protocol, IPAddress localIp) => Create(protocol, NullLogger.Instance, localIp);

	public static IDmxProtocolService Create(DmxProtocolKind protocol, ILogger logger) => Create(protocol, logger, IPAddress.Any);

	public static IDmxProtocolService Create(DmxProtocolKind protocol, ILogger logger, IPAddress localIp)
	{
		switch (protocol)
		{
			case DmxProtocolKind.ArtNet:
				return Create(logger, new ArtNetProtocolServiceInfo(DmxMaxFramerate, localIp));
			case DmxProtocolKind.SAcn:
				return Create(logger, new SAcnProtocolServiceInfo(DmxMaxFramerate, localIp));
			default:
				throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null);
		}
	}

	
}