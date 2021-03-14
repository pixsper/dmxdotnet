using System;
using System.Collections.Generic;
using System.Net;

namespace Pixsper.DmxDotNet
{
	public interface IDmxControllerService : IDisposable
	{
		IPEndPoint LocalEndPoint { get; }
		int UdpPort { get; }
		float TargetOutputRateHz { get; }
        float ActualOutputRateHz { get; }
        bool IsSending { get; }
		bool IsSendSyncPacket { get; }

        

		IReadOnlyDictionary<DmxUniverseAddress, IDmxUniverseSettings> RegisteredUniverses { get; }

		void StartSending();
		void StopSending();
		void AddUniverse(DmxUniverseAddress universe, IDmxUniverseSettings? customSettings = null);
		void AddUniverse(IDictionary<DmxUniverseAddress, IDmxUniverseSettings> universes);
		void AddUniverse(DmxUniverseAddress startUniverse, int count, IDmxUniverseSettings? customSettings = null);
		void RemoveUniverse(DmxUniverseAddress universe);
		void RemoveUniverse(IEnumerable<DmxUniverseAddress> universes);
		void RemoveUniverse(DmxUniverseAddress startUniverse, int count);
		void ClearUniverses();
		void WriteData(DmxUniverseAddress startUniverse, byte[] data, int? count = null);
	}
}