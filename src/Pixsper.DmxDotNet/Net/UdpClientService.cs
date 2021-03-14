// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pixsper.DmxDotNet.Net;

internal sealed class UdpClientService : IUdpClientService
{
	private const int SioUdpConnReset = -1744830452;

	private readonly ILogger _log;

	private readonly UdpClient _client;

	private Task _receiveTask = Task.CompletedTask;
	private CancellationTokenSource _cancellationTokenSource = new();
        

	public UdpClientService(ILogger? log, IPEndPoint localEndPoint, IEnumerable<IPAddress>? multicastGroups = null)
	{
		_log = log ?? NullLogger.Instance;

		LocalEndPoint = localEndPoint;
		MulticastGroups = multicastGroups?.ToImmutableHashSet() ?? ImmutableHashSet<IPAddress>.Empty;

		_client = new UdpClient
		{
			ExclusiveAddressUse = false, 
			EnableBroadcast = true
		};

		// This is required to prevent exceptions from ICMP messages
		_client.Client.IOControl(
			(IOControlCode)SioUdpConnReset, 
			new byte[] { 0, 0, 0, 0 }, 
			null
		);
	}

	public UdpClientService(ILogger? parentLogger, IPEndPoint localEndPoint, IPAddress multicastGroup)
		: this(parentLogger, localEndPoint, new [] { multicastGroup })
	{

	}

	public async ValueTask DisposeAsync()
	{
		if (IsListening)
			await StopListeningAsync();

		_client.Dispose();
	}

	public IPEndPoint LocalEndPoint { get; }
	public ImmutableHashSet<IPAddress> MulticastGroups { get; private set; }

	public bool IsListening { get; private set; }

	public bool IsBoundToEndpoint { get; private set; }

	public event EventHandler<UdpReceiveResult>? PacketReceived; 

	public bool StartListening()
	{
		if (!IsBoundToEndpoint)
		{
			try
			{
				_client.Client.Bind(LocalEndPoint);
				IsBoundToEndpoint = true;
			}
			catch (SocketException ex)
			{
				_log.LogWarning(ex, "Failed to bind to local endpoint {EndPoint}", LocalEndPoint);

				IsBoundToEndpoint = false;
				return false;
			}
		}

		foreach (var multicastIp in MulticastGroups)
			JoinMulticastGroup(multicastIp);

		if (!IsBoundToEndpoint)
			return false;

		if (IsListening)
			throw new InvalidOperationException("Service is already listening");

		_receiveTask = listenAsync(_cancellationTokenSource.Token);

		IsListening = true;
		_log.LogInformation("Listening on UDP endpoint {LocalEndPoint}", LocalEndPoint);

		return true;
	}

	public async Task StopListeningAsync()
	{
		if (!IsListening)
			throw new InvalidOperationException("Service is not currently listening");

		_cancellationTokenSource.Cancel();

		await _receiveTask.ConfigureAwait(false);
		_receiveTask = Task.CompletedTask;
			
		_cancellationTokenSource.Dispose();
		_cancellationTokenSource = new CancellationTokenSource();

		foreach (var multicastIp in MulticastGroups)
			DropMulticastGroup(multicastIp);

		if (IsBoundToEndpoint)
		{
			_client.Client.Close();
			IsBoundToEndpoint = false;
		}

		IsListening = false;
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> data, IPEndPoint targetEndPoint, CancellationToken cancellationToken = default)
	{
		return _client.SendAsync(data, targetEndPoint, cancellationToken);
	}

	public int Send(ReadOnlySpan<byte> data, IPEndPoint targetEndPoint)
	{
		return _client.Send(data, targetEndPoint);
	}

	public bool JoinMulticastGroup(IPAddress multicastIp)
	{
		if (MulticastGroups.Contains(multicastIp))
			return true;

		try
		{
			if (LocalEndPoint.Address.IsAny())
				_client.JoinMulticastGroup(multicastIp);
			else
				_client.JoinMulticastGroup(multicastIp, LocalEndPoint.Address);
		}
		catch (SocketException)
		{
			return false;
		}

		MulticastGroups = MulticastGroups.Add(multicastIp);
		return true;
	}

	public void DropMulticastGroup(IPAddress multicastIp)
	{
		if (!MulticastGroups.Contains(multicastIp))
			throw new ArgumentException("Not a member of this multicast group", nameof(multicastIp));

		MulticastGroups = MulticastGroups.Remove(multicastIp);

		_client.DropMulticastGroup(multicastIp);
	}

	private async Task listenAsync(CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				var result = await _client.ReceiveAsync(token);
				PacketReceived?.Invoke(this, result);
			}
		}
		catch (AggregateException ex)
		{
			foreach (var innerException in ex.InnerExceptions)
			{
				if (innerException is SocketException socketException)
				{
					_log.LogWarning(socketException, "Socket exception in {nameof(UdpClientService)}", this);
				}
				else
				{
					Debug.Assert(false);
					_log.LogError(innerException, "Exception in {nameof(UdpClientService)}", this);
				}
			}
		}
		catch (SocketException ex)
		{
			Debug.Assert(false);
			_log.LogError(ex, "Socket exception in {nameof(UdpClientService)}", this);
		}
		catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
		{

		}
	}

	public override string ToString()
	{
		return $" IsListening: {IsListening}, LocalEndPoint: {LocalEndPoint}, MulticastGroups: {MulticastGroups.JoinAsString(',')},";
	}
}