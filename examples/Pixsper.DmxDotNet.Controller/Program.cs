using System;
using System.Linq;
using System.Net;
using Pixsper.DmxDotNet.ArtNet;

namespace Pixsper.DmxDotNet.Controller
{
	class Program
	{
		static void Main(string[] args)
		{
		    const int universeCount = 1;

			var service = new ArtNetControllerService("Test Source", isSendSyncPacket: false);

			service.AddUniverse(DmxUniverseAddress.FromArtNetAddress(0), universeCount, new ArtNetUniverseSettings(IPAddress.Loopback));

			service.WriteData(DmxUniverseAddress.FromArtNetAddress(0), Enumerable.Repeat((byte)127, 512 * universeCount).ToArray());

			service.StartSending();

		    while (!Console.KeyAvailable) { }

		    service.Dispose();
		}
	}
}
