// This file is part of DmxDotNet.
// 
// DmxDotNet is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// DmxDotNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with DmxDotNet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Pixsper.DmxDotNet.Serialization;

namespace Pixsper.DmxDotNet.ArtNet.Packets
{
	internal class ArtPollReplyPacket : ArtPacket
	{
		private const int ShortNameFieldLength = 18;
		private const int LongNameFieldLength = 64;
		private const int NodeReportFieldLength = 64;

		private const int MaxPorts = 4;



		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum IndicatorState : byte
		{
			Unknown = 0x00,
			Locate = 0x40,
			Mute = 0x80,
			Normal = 0xC0
		}


	
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum PortAddressProgrammingAuthority : byte
		{
			Unknown = 0x00,
			FromFrontPanelControls = 0x10,
			FromNetwork = 0x20
		}


		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum PortType : byte
		{
			Dmx = 0x00,
			Midi = 0x01,
			Avab = 0x02,
			ColortranCmx = 0x03,
			Adb62_5 = 0x04,
			ArtNet = 0x05
		}



		[Flags]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum GoodInputFlags : byte
		{
			None = 0x00,
			DataReceived = 0x80,
			ChannelIncludesDmx512TestPackets = 0x40,
			ChannelIncludesDmx512Sips = 0x20,
			ChannelIncludesDmx512TextPackets = 0x10,
			InputDisabled = 0x08,
			ReceiveErrorsDetected = 0x04
		}



		[Flags]
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		public enum GoodOutputFlags : byte
		{
			None = 0x00,
			DataTransmitted = 0x80,
			ChannelIncludesDmx512TestPackets = 0x40,
			ChannelIncludesDmx512Sips = 0x20,
			ChannelIncludesDmx512TextPackets = 0x10,
			OutputMergingArtNetData = 0x08,
			DmxOutputShortDetectedOnPowerup = 0x04,
			MergeModeIsLtp = 0x02
		}


        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum ArtNetStyleCode : byte
        {
            StNode = 0x00,
            StController = 0x01,
            StMedia = 0x02,
            StRoute = 0x03,
            StBackup = 0x04,
            StConfig = 0x05,
            StVisual = 0x06
        }



        public ArtPollReplyPacket(string shortName, string longName, ArtNetStyleCode styleCode,
			int net, int subNet, byte[] ipAddress, bool isDhcpConfigured, byte[] macAddress, 
			int firmwareRevisionMajor, int firmwareRevisionMinor, 
			int oemCode, int estaCodeLow, int estaCodeHigh, 
			IEnumerable<Port> ports,
			bool isBootedFromRom = false,
			NodeReport? report = null, IndicatorState indicator = IndicatorState.Normal,
			PortAddressProgrammingAuthority addressProgramming = PortAddressProgrammingAuthority.FromFrontPanelControls,
			int ubeaVersion = 0, bool isUbeaPresent = false,
			int udpPort = ArtNetNodeService.ArtNetUdpPort, byte[]? rootDeviceIpAddress = null, int bindIndex = 0,
			IEnumerable<bool>? macroSwitchStates = null, IEnumerable<bool>? remoteSwitchStates = null,
			bool isRdmCapable = false, bool isWebConfigurable = false, bool isDhcpCapable = true,
			bool is15BitPortAddressCapable = true, bool isVideoDisplayingEthernetData = false, int? numPorts = null)
		{
			if (shortName == null)
				throw new ArgumentNullException(nameof(shortName));
			
			ShortName = shortName;

			if (longName == null)
				throw new ArgumentNullException(nameof(longName));
			LongName = longName;

			StyleCode = styleCode;

			if (net < 0 || net > 127)
				throw new ArgumentOutOfRangeException(nameof(net), net, "Must be in range 0-127");
			Net = net;

			if (subNet < 0 || subNet > 15)
				throw new ArgumentOutOfRangeException(nameof(subNet), subNet, "Must be in range 0-15");
			SubNet = subNet;

			if (ipAddress == null)
				throw new ArgumentNullException(nameof(ipAddress));
			if (ipAddress.Length != 4)
				throw new ArgumentException("Must be a 4-byte array", nameof(ipAddress));
			IpAddress = ipAddress;

			IsDhcpConfigured = isDhcpConfigured;

			if (macAddress == null)
				throw new ArgumentNullException(nameof(macAddress));
			if (macAddress.Length != 6)
				throw new ArgumentException("Must be a 6-byte array", nameof(macAddress));
			MacAddress = macAddress;

			if (firmwareRevisionMajor < byte.MinValue || firmwareRevisionMajor > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(firmwareRevisionMajor), firmwareRevisionMajor, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			FirmwareRevisionMajor = firmwareRevisionMajor;

			if (firmwareRevisionMinor < byte.MinValue || firmwareRevisionMinor > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(firmwareRevisionMinor), firmwareRevisionMinor, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			FirmwareRevisionMinor = firmwareRevisionMinor;

			if (oemCode < ushort.MinValue || oemCode > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(oemCode), oemCode, $"Must be in range {ushort.MinValue}-{ushort.MaxValue}");
			OemCode = oemCode;

			if (estaCodeLow < byte.MinValue || estaCodeLow > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(estaCodeLow), estaCodeLow, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			EstaCodeLow = estaCodeLow;

			if (estaCodeHigh < byte.MinValue || estaCodeHigh > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(estaCodeHigh), estaCodeHigh, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			EstaCodeHigh = estaCodeHigh;

			if (ports == null)
				throw new ArgumentNullException(nameof(ports));
			Ports = ports.ToList();

			if (Ports.Select(p => p.InputAddress.ArtNetNet).Distinct().Count() > 1 || Ports.Select(p => p.InputAddress.ArtNetSubNet).Distinct().Count() > 1)
				throw new ArgumentException("Port list cannot contain ports with multiple input net or subnet values, only universe number can vary.");

			if (Ports.Select(p => p.OutputAddress.ArtNetNet).Distinct().Count() > 1 || Ports.Select(p => p.OutputAddress.ArtNetSubNet).Distinct().Count() > 1)
				throw new ArgumentException("Port list cannot contain ports with multiple output net or subnet values, only universe number can vary.");

			IsBootedFromRom = isBootedFromRom;
			Report = report;
			Indicator = indicator;
			AddressProgramming = addressProgramming;

			IsUbeaPresent = isUbeaPresent;
			UbeaVersion = ubeaVersion;

			if (udpPort < ushort.MinValue || udpPort > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(udpPort), udpPort, $"Must be in range {ushort.MinValue}-{ushort.MaxValue}");
			UdpPort = udpPort;

			if (rootDeviceIpAddress == null)
			{
				RootDeviceIpAddress = new byte[4];
			}
			else
			{
				if (ipAddress.Length != 4)
					throw new ArgumentException("Must be a 4-byte array", nameof(ipAddress));
				RootDeviceIpAddress = rootDeviceIpAddress;
			}

			if (bindIndex < byte.MinValue || bindIndex > byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(bindIndex), bindIndex, $"Must be in range {byte.MinValue}-{byte.MaxValue}");
			BindIndex = bindIndex;

			if (macroSwitchStates == null)
			{
				MacroSwitchStates = Enumerable.Repeat(false, 8).ToList();
			}
			else
			{
				MacroSwitchStates = macroSwitchStates.ToList();
				if (MacroSwitchStates.Count != 8)
					throw new ArgumentException("Must contain 8 switch states", nameof(macroSwitchStates));
			}


			if (remoteSwitchStates == null)
			{
				RemoteSwitchStates = Enumerable.Repeat(false, 8).ToList();
			}
			else
			{
				RemoteSwitchStates = remoteSwitchStates.ToList();
				if (RemoteSwitchStates.Count != 8)
					throw new ArgumentException("Must contain 8 switch states", nameof(remoteSwitchStates));
			}
			

			IsRdmCapable = isRdmCapable;
			IsWebConfigurable = isWebConfigurable;
			IsDhcpCapable = isDhcpCapable;
			Is15BitPortAddressCapable = is15BitPortAddressCapable;
			IsVideoDisplayingEthernetData = isVideoDisplayingEthernetData;

			NumPorts = numPorts ?? Ports.Count;

			if (NumPorts > MaxPorts)
				throw new ArgumentException($"Packet cannot contain more than {MaxPorts} ports", nameof(ports));
		}

		public override ArtNetOpCode OpCode => ArtNetOpCode.OpPollReply;
		internal override int MaxPacketLength => 239;

		public byte[] IpAddress { get; }
		public int UdpPort { get; }
		public byte[] MacAddress { get; }
		public byte[] RootDeviceIpAddress { get; }
		public int BindIndex { get; }
		public int FirmwareRevisionMajor { get; }
		public int FirmwareRevisionMinor { get; }

		public int Net { get; }
		public int SubNet { get; }

		public int OemCode { get; }
		public int UbeaVersion { get; }


		public IndicatorState Indicator { get; }
		public PortAddressProgrammingAuthority AddressProgramming { get; }
		public bool IsBootedFromRom { get; }
		public bool IsRdmCapable { get; }
		public bool IsUbeaPresent { get; }

		public int EstaCodeLow { get; }
		public int EstaCodeHigh { get; }

		public string ShortName { get; }
		public string LongName { get; }

		public NodeReport? Report { get; }

		public int NumPorts { get; }
		public IReadOnlyList<Port> Ports { get; }

		public bool IsVideoDisplayingEthernetData { get; }

		public IReadOnlyList<bool> MacroSwitchStates { get; }
		public IReadOnlyList<bool> RemoteSwitchStates { get; }

		public ArtNetStyleCode StyleCode { get; }

		public bool IsWebConfigurable { get; }
		public bool IsDhcpConfigured { get; }
		public bool IsDhcpCapable { get; }
		public bool Is15BitPortAddressCapable { get; }


		internal static ArtPollReplyPacket? Deserialize(ArtNetBinaryReader reader)
		{
			DeserializeHeader(reader, false);

			var ipAddress = reader.ReadBytes(4);
			int udpPort = reader.ReadUInt16();

			int firmwareRevisionHigh = reader.ReadByte();
			int firmwareRevisionLow = reader.ReadByte();

			int net = reader.ReadByte() & 0x7F;
			int subNet = reader.ReadByte() & 0x0F;

			int oemCodeHigh = reader.ReadByte();
			int oemCodeLow = reader.ReadByte();
			int oemCode = (oemCodeHigh << 8) + oemCodeLow;

			int ubeaVersion = reader.ReadByte();

			byte status1 = reader.ReadByte();

			var indicatorState = (IndicatorState)(status1 & 0xC0);
			var addressProgramming = (PortAddressProgrammingAuthority)(status1 & 0x30);
			bool isBootedFromRom = (status1 & 0x04) != 0;
			bool isRdmCapable = (status1 & 0x02) != 0;
			bool isUbeaPresent = (status1 & 0x01) != 0;

			int estaCodeLow = reader.ReadByte();
			int estaCodeHigh = reader.ReadByte();

			string shortName = Encoding.UTF8.GetString(reader.ReadBytes(ShortNameFieldLength), 0, ShortNameFieldLength);
			string longName = Encoding.UTF8.GetString(reader.ReadBytes(LongNameFieldLength), 0, LongNameFieldLength);
			string nodeReportString = Encoding.UTF8.GetString(reader.ReadBytes(NodeReportFieldLength), 0, NodeReportFieldLength);

			var nodeReport = NodeReport.Parse(nodeReportString);

			byte numPortsHigh = reader.ReadByte();
			byte numPortsLow = reader.ReadByte();
			int numPorts = (numPortsHigh << 8) + numPortsLow;

			var portTypesRaw = reader.ReadBytes(4);
			var goodInputFlagsRaw = reader.ReadBytes(4);
			var goodOutputFlagsRaw = reader.ReadBytes(4);
			var swIn = reader.ReadBytes(4);
			var swOut = reader.ReadBytes(4);

			var ports = portTypesRaw.Take(numPorts)
				.Select((b, i) => new Port(b, goodInputFlagsRaw[i], goodOutputFlagsRaw[i], net, subNet, swIn[i], swOut[i]))
				.ToList();

			bool isVideoDisplayingEthernetData = reader.ReadByte() == 1;

			byte macroSwitchStatesRaw = reader.ReadByte();
			var macroSwitchStates = new bool[8];
			for (int i = 0; i < 8; ++i)
				macroSwitchStates[i] = (macroSwitchStatesRaw & (1 << i)) != 0;

			byte remoteSwitchStatesRaw = reader.ReadByte();
			var remoteSwitchStates = new bool[8];
			for (int i = 0; i < 8; ++i)
				remoteSwitchStates[i] = (remoteSwitchStatesRaw & (1 << i)) != 0;

			// 3 empty bytes
			reader.Seek(3, SeekOrigin.Current);

			var styleCode = (ArtNetStyleCode)reader.ReadByte();

			var macAddress = reader.ReadBytes(6);
			var rootDeviceIp = reader.ReadBytes(4);
			byte bindIndex = reader.ReadByte();

			byte status2 = reader.ReadByte();

			bool isWebConfigurable = (status2 & 0x01) != 0;
			bool isDhcpConfigured = (status2 & 0x02) != 0;
			bool isDhcpCapable = (status2 & 0x04) != 0;
			bool is15BitPortAddressCapable = (status2 & 0x08) != 0;

			// 26 empty bytes
			reader.Seek(26, SeekOrigin.Current);

			return new ArtPollReplyPacket(shortName, longName, styleCode, net, subNet, ipAddress, isDhcpConfigured, macAddress, 
				firmwareRevisionHigh, firmwareRevisionLow, oemCode, estaCodeLow, estaCodeHigh, ports, 
				isBootedFromRom, nodeReport, indicatorState, addressProgramming, ubeaVersion, isUbeaPresent, udpPort, rootDeviceIp, bindIndex,
				macroSwitchStates, remoteSwitchStates, isRdmCapable, isWebConfigurable, isDhcpCapable, is15BitPortAddressCapable,
				isVideoDisplayingEthernetData, numPorts);
		}

	    protected override void Serialize(ArtNetBinaryWriter writer)
		{
			SerializeHeader(writer, false);

			writer.Write(IpAddress);
			writer.Write((ushort)UdpPort);

			writer.Write((byte)FirmwareRevisionMajor);
			writer.Write((byte)FirmwareRevisionMinor);

			writer.Write((byte)Net);
			writer.Write((byte)SubNet);

			writer.Write((ushort)OemCode);
			writer.Write((byte)UbeaVersion);

			writer.Write((byte)Indicator 
				| (byte)AddressProgramming 
				| (IsBootedFromRom ? 0x04 : 0x00)
				| (IsRdmCapable ? 0x02 : 0x00)
				| (IsUbeaPresent ? 0x01 : 0x00));

			writer.Write(EstaCodeLow);
			writer.Write(EstaCodeHigh);

			writer.Write(ShortName, ShortNameFieldLength);
			writer.Write(LongName, LongNameFieldLength);
			writer.Write(Report?.ToString() ?? string.Empty, NodeReportFieldLength);

			writer.Write((NumPorts & 0xFF00) >> 8);
			writer.Write(NumPorts & 0x00FF);

			var portTypes = new byte[4];
			var goodInput = new byte[4];
			var goodOutput = new byte[4];
			var swIn = new byte[4];
			var swOut = new byte[4];

			for (int i = 0; i < Ports.Count; ++i)
			{
				var port = Ports[i];
				portTypes[i] = (byte)((byte)port.PortType | (port.IsArtNetInput ? 0x20 : 0x00) | (port.IsArtNetOutput ? 0x40 : 0x00));
				goodInput[i] = (byte)port.InputFlags;
				goodOutput[i] = (byte)port.OutputFlags;
				swIn[i] = (byte)port.InputAddress.ArtNetUniverse;
				swOut[i] = (byte)port.OutputAddress.ArtNetUniverse;
			}

			writer.Write(portTypes);
			writer.Write(goodInput);
			writer.Write(goodOutput);
			writer.Write(swIn);
			writer.Write(swOut);

			writer.Write((byte)(IsVideoDisplayingEthernetData ? 1 : 0));

			byte swMacro = 0;
			for (int i = 0; i < 8; ++i)
				swMacro |= (byte)((MacroSwitchStates[i] ? 1 : 0) << i);

			writer.Write(swMacro);

			byte swRemote = 0;
			for (int i = 0; i < 8; ++i)
				swRemote |= (byte)((RemoteSwitchStates[i] ? 1 : 0) << i);

			writer.Write(swRemote);

			// 3 empty bytes
			writer.Write(Enumerable.Repeat((byte)0, 3).ToArray());

			writer.Write((byte)StyleCode);
			writer.Write(MacAddress);
			writer.Write(RootDeviceIpAddress);
			writer.Write((byte)BindIndex);
			writer.Write((byte)((IsWebConfigurable ? 0x01 : 0x00)
				| (IsDhcpConfigured ? 0x02 : 0x00)
				| (IsDhcpCapable ? 0x04 : 0x00)
				| (Is15BitPortAddressCapable ? 0x08 : 0x00)));

			// 26 empty bytes
			writer.Write(Enumerable.Repeat((byte)0, 26).ToArray());
		}


		public class Port
		{
			internal Port(byte portTypeRaw, byte inputFlags, byte outputFlags, int net, int subNet, byte swIn,
				byte swOut)
			{
				PortType = (PortType)(portTypeRaw & 0x1F);
				IsArtNetInput = (portTypeRaw & 0x20) != 0;
				IsArtNetOutput = (portTypeRaw & 0x40) != 0;

				InputFlags = (GoodInputFlags)inputFlags;
				OutputFlags = (GoodOutputFlags)outputFlags;

				InputAddress = DmxUniverseAddress.FromArtNetAddress(net, subNet, swIn & 0x0F);
				OutputAddress = DmxUniverseAddress.FromArtNetAddress(net, subNet, swOut & 0x0F);
			}

			public Port(PortType portType, bool isArtNetInput, bool isArtNetOutput,
				GoodInputFlags inputFlags, GoodOutputFlags outputFlags, DmxUniverseAddress inputAddress,
				DmxUniverseAddress outputAddress)
			{
				PortType = portType;
				IsArtNetInput = isArtNetInput;
				IsArtNetOutput = isArtNetOutput;
				InputFlags = inputFlags;
				OutputFlags = outputFlags;

				InputAddress = inputAddress;
				OutputAddress = outputAddress;
			}

			public PortType PortType { get; }
			public bool IsArtNetInput { get; }
			public bool IsArtNetOutput { get; }

			public DmxUniverseAddress InputAddress { get; }
			public DmxUniverseAddress OutputAddress { get; }

			public GoodInputFlags InputFlags { get; }
			public GoodOutputFlags OutputFlags { get; }
		}


		internal class NodeReport
		{
			[SuppressMessage("ReSharper", "UnusedMember.Global")]
			public enum ArtNetNodeReportCode : ushort
			{
				RcDebug = 0x0000,
				RcPowerOk = 0x0001,
				RcPowerFail = 0x0002,
				RcSocketWr1 = 0x0003,
				RcParseFail = 0x0004,
				RcUdpFail = 0x0005,
				RcShNameOk = 0x0006,
				RcLoNameOk = 0x0007,
				RcDmxError = 0x0008,
				RcDmxUdpFull = 0x0009,
				RcDmxRxFull = 0x000a,
				RcSwitchErr = 0x000b,
				RcConfigErr = 0x000c,
				RcDmxShort = 0x000d,
				RcFirmwareFail = 0x000e,
				RcUserFail = 0x000f
			}

			private static readonly Regex NodeReportRegex = new Regex(@"^#([a-zA-Z0-9]{4}) \[(\d{4})\] *(.*)$");

			public static NodeReport? Parse(string input)
			{
				var match = NodeReportRegex.Match(input);
				if (!match.Success)
					return null;

				var nodeReportCode = (ArtNetNodeReportCode)int.Parse(match.Captures[1].Value, System.Globalization.NumberStyles.HexNumber);
				int reportResponseCounter = int.Parse(match.Captures[2].Value);
				string reportMessage = match.Captures[3].Value;

				return new NodeReport(nodeReportCode, reportResponseCounter, reportMessage);
			}

			public NodeReport(ArtNetNodeReportCode reportCode, int reportResponseCounter, string reportMessage)
			{
				NodeReportCode = reportCode;
				ReportResponseCounter = reportResponseCounter;
				ReportMessage = reportMessage;
			}

			public ArtNetNodeReportCode NodeReportCode { get; }
			public int ReportResponseCounter { get; }
			public string ReportMessage { get; }

			public override string ToString()
			{
				return $"#{NodeReportCode:X4} [{ReportResponseCounter}] {ReportMessage}";
			}
		}
	}
}