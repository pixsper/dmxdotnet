using System;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.SAcn.Packets
{
	internal class SAcnUniverseDiscoveryPacket : SAcnPacket
	{
        internal static SAcnUniverseDiscoveryPacket? Deserialize(SAcnBinaryReader reader)
        {
            return null;
        }

        public SAcnUniverseDiscoveryPacket(Guid cId)
			: base(cId)
		{
			
		}

		protected override int MaxPacketLength => 1144;

	    protected override void Serialize(SAcnBinaryWriter writer)
		{
			
		}
	}
}
