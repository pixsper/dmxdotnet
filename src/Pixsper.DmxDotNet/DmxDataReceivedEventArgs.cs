// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;

namespace Pixsper.DmxDotNet;

public class DmxDataReceivedEventArgs : EventArgs
{
	public DmxDataReceivedEventArgs(IDmxInputProtocolService service, DmxUniverseAddress universeAddress, 
		IPAddress sourceAddress, ReadOnlyMemory<byte> data)
	{
		Service = service;
		UniverseAddress = universeAddress;
		SourceAddress = sourceAddress;
		Data = data;
	}

	public IDmxInputProtocolService Service { get; }

	public DmxUniverseAddress UniverseAddress { get; }

	public IPAddress SourceAddress { get; }

	public ReadOnlyMemory<byte> Data { get; }
}