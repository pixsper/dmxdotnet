// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pixsper.DmxDotNet.Net;

internal interface IUdpSendService
{
	ValueTask<int> SendAsync(ReadOnlyMemory<byte> data, IPEndPoint targetEndPoint, CancellationToken cancellationToken = default);
	int Send(ReadOnlySpan<byte> data, IPEndPoint targetEndPoint);
}

internal interface IUdpClientService : IUdpSendService, IAsyncDisposable
{
	IPEndPoint LocalEndPoint { get; }
	ImmutableHashSet<IPAddress> MulticastGroups { get; }
	bool IsListening { get; }

	bool IsBoundToEndpoint { get; }

	event EventHandler<UdpReceiveResult>? PacketReceived;

	bool StartListening();
	Task StopListeningAsync();

	bool JoinMulticastGroup(IPAddress multicastIp);
	void DropMulticastGroup(IPAddress multicastIp);
}