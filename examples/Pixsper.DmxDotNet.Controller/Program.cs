using System;
using System.Net;
using System.Threading.Tasks;
using Pixsper.DmxDotNet.Artnet;
using Pixsper.DmxDotNet.SAcn;

namespace Pixsper.DmxDotNet.Controller;

public class Program
{
	public static async Task Main(string[] args)
	{
		IDmxProtocolService dmxService;
		IDmxOutputProtocolServiceInfo outputServiceInfo;

		if (args.Length > 0 && args[0] == "artnet")
		{
			dmxService = DmxService.Create(DmxProtocolKind.ArtNet);
			outputServiceInfo = new ArtNetOutputProtocolServiceInfo(null,
				new DmxUniverseAddress(1), IPAddress.Loopback);
		}
		else
		{
			dmxService = DmxService.Create(DmxProtocolKind.SAcn);
			outputServiceInfo = new SAcnOutputProtocolServiceInfo(null, new DmxUniverseAddress(1),
				Environment.MachineName);
		}
		
		dmxService.AddOutputProtocolService(outputServiceInfo);


		while (!Console.KeyAvailable)
		{
			await dmxService.SendOutputAsync();
			await Task.Delay(1000 / 44);
		}

		await dmxService.DisposeAsync();
	}
}