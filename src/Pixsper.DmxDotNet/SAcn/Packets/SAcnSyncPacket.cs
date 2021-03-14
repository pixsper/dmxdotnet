using System;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets
{
	internal class SAcnSyncPacket : SAcnPacket
	{
	    internal static SAcnSyncPacket? Deserialize(SAcnBinaryReader reader)
	    {
	        return null;
	    }

		public SAcnSyncPacket(Guid cId)
			: base(cId)
		{
			
		}



		protected override int MaxPacketLength => 48;

	    protected override void Serialize(SAcnBinaryWriter writer)
		{
			
		}
	}
}
