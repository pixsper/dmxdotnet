// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Pixsper.DmxDotNet;

public interface IDmxOutputWriter
{
	DmxUniverseAddress UniverseAddress { get; }

	Memory<byte> Data { get; }
        
	void WriteToAddress(int channel, long value,
		DmxParameterResolution resolution = DmxParameterResolution._8Bit)
	{
		var address = new DmxAddress(UniverseAddress, channel);

		if (!address.CanFitWithinUniverse(resolution.Width()))
			return;

		int p = address.Channel - 1;

		var buffer = Data.Span;

		switch (resolution)
		{
			case DmxParameterResolution._8Bit:
				buffer[p] = (byte)(value & 0xFF);
				break;

			case DmxParameterResolution._16Bit:
				buffer[p++] = (byte)((value >> 8) & 0xFF);
				buffer[p] = (byte)(value & 0xFF);
				break;

			case DmxParameterResolution._24Bit:
				buffer[p++] = (byte)((value >> 16) & 0xFF);
				buffer[p++] = (byte)((value >> 8) & 0xFF);
				buffer[p] = (byte)(value & 0xFF);
				break;

			case DmxParameterResolution._32Bit:
				buffer[p++] = (byte)((value >> 24) & 0xFF);
				buffer[p++] = (byte)((value >> 16) & 0xFF);
				buffer[p++] = (byte)((value >> 8) & 0xFF);
				buffer[p] = (byte)(value & 0xFF);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
		}
	}

	public void WriteNormalizedToAddress(int channel, double value,
		DmxParameterResolution resolution = DmxParameterResolution._8Bit)
	{
		long intValue = (long)Math.Round(value * resolution.MaxValue(), MidpointRounding.AwayFromZero);
		WriteToAddress(channel, intValue, resolution);
	}
}