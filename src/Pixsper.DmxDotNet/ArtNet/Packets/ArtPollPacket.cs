using System;
using System.Diagnostics.CodeAnalysis;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
	internal class ArtPollPacket : ArtPacket, IEquatable<ArtPollPacket>
	{
		[Flags]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum TalkToMeFlags : byte
		{
			None = 0x00,
			IsNodesSendUpdates = 0x02,
			IsNodesSendDiagnostics = 0x04,
			IsNodesSendDiagnosticsUnicast = 0x08,
            IsVlcTransmissionEnabled = 0x10
		}

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum ArtNetDiagnosticsPriorityCode : byte
        {
            DpLow = 0x10,
            DpMed = 0x40,
            DpHigh = 0x80,
            DpCritical = 0xe0,
            DpVolatile = 0xf0,
        }



        public ArtPollPacket(TalkToMeFlags talkToMeSettings = TalkToMeFlags.None,
			ArtNetDiagnosticsPriorityCode diagnosticsPriority = ArtNetDiagnosticsPriorityCode.DpCritical)
		{
			TalkToMeSettings = talkToMeSettings;
			DiagnosticsPriority = diagnosticsPriority;
		}


		public override ArtNetOpCode OpCode => ArtNetOpCode.OpPoll;
		internal override int MaxPacketLength => 14;

		public TalkToMeFlags TalkToMeSettings { get; }
		public ArtNetDiagnosticsPriorityCode DiagnosticsPriority { get; }

		public bool Equals(ArtPollPacket? other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return TalkToMeSettings == other.TalkToMeSettings && DiagnosticsPriority == other.DiagnosticsPriority;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			return obj.GetType() == GetType() && Equals((ArtPollPacket)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int)TalkToMeSettings * 397) ^ (int)DiagnosticsPriority;
			}
		}

		internal static ArtPollPacket? Deserialize(ArtNetBinaryReader reader)
		{
			if (!DeserializeHeader(reader))
				return null;

			var talkToMeSettings = (TalkToMeFlags)reader.ReadByte();
			var diagnosticsPriority = (ArtNetDiagnosticsPriorityCode)reader.ReadByte();

			return new ArtPollPacket(talkToMeSettings, diagnosticsPriority);
		}

	    protected override void Serialize(ArtNetBinaryWriter writer)
		{
			SerializeHeader(writer);
			writer.Write((byte)TalkToMeSettings);
			writer.Write((byte)DiagnosticsPriority);
		}
	}
}