// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;

namespace Pixsper.DmxDotNet;

public interface IDmxInputProtocolService : IServiceStatusProvider
{
	internal static readonly TimeSpan WarningMinimumPacketInterval = TimeSpan.FromSeconds(0.5);

	internal const string NoDataStatusMessage = "Not receiving data packets";


	event EventHandler<DmxDataReceivedEventArgs>? DataReceived;


	IDmxInputProtocolServiceInfo Info { get; }

	DmxUniverseAddress UniverseAddress { get; }
        
	ReadOnlyMemory<byte>? Data { get; }

	DateTime? LastDataReceived { get; }


	long? ReadFromAddress(DmxAddress address, DmxParameterResolution resolution = DmxParameterResolution._8Bit)
	{
		if (!Data.HasValue)
			return null;
            
		if (UniverseAddress != address.Universe)
			return null;
            
		if (!address.CanFitWithinUniverse(resolution.Width()))
			return null;

		var buffer = Data.Value.Span;

		long value;
            
		int p = address.Channel - 1;
            
		switch (resolution)
		{
			case DmxParameterResolution._8Bit:
				value = buffer[p];
				break;
			case DmxParameterResolution._16Bit:
				value = buffer[p++] << 8;
				value += buffer[p];
				break;
			case DmxParameterResolution._24Bit:
				value = buffer[p++] << 16;
				value += buffer[p++] << 8;
				value += buffer[p];
				break;
			case DmxParameterResolution._32Bit:
				value = buffer[p++] << 24;
				value += buffer[p++] << 16;
				value += buffer[p++] << 8;
				value += buffer[p];
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
		}

		return value;
	}

	internal void RefreshStatus();

	internal void OnDataPacketReceived(IPAddress sourceAddress, IDmxDataPacket packet);
}