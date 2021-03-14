// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Threading.Tasks;

namespace Pixsper.DmxDotNet;

public interface IDmxOutputProtocolService : IDmxOutputWriter, IServiceStatusProvider
{
	IDmxOutputProtocolServiceInfo Info { get; }

	void SetData(Span<byte> data);

	void SetData(Memory<byte> data);

	ValueTask<bool> SendDataPacketAsync();

	bool SendDataPacket();
}