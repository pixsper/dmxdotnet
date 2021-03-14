// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pixsper.DmxDotNet;

public interface IDmxProtocolService : IAsyncDisposable
{
	string ProtocolName { get; }

	IDmxProtocolServiceInfo Info { get; }

	bool IsListening { get; }

	ValueTask<bool> StartListeningAsync();
	ValueTask StopListeningAsync();

	IEnumerable<IDmxInputProtocolService> InputProtocolServices { get; }
	IEnumerable<IDmxOutputProtocolService> OutputProtocolServices { get; }

	IDmxInputProtocolService? AddInputProtocolService(IDmxInputProtocolServiceInfo info);
	bool RemoveInputProtocolService(Guid id);

	IDmxOutputProtocolService? AddOutputProtocolService(IDmxOutputProtocolServiceInfo info);
	bool RemoveOutputProtocolService(Guid id);

	Task SendOutputAsync();
	void SendOutput();
}