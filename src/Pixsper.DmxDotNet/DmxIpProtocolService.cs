// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pixsper.DmxDotNet.Net;

namespace Pixsper.DmxDotNet;

/// <summary>
///     Abstract generic base class for DMX over IP protocols implementing common functionality
/// </summary>
/// /// <typeparam name="TServiceInfo"></typeparam>
/// <typeparam name="TInputService"></typeparam>
/// <typeparam name="TInputServiceInfo"></typeparam>
/// <typeparam name="TOutputService"></typeparam>
/// <typeparam name="TOutputServiceInfo"></typeparam>
internal abstract class DmxIpProtocolService<TServiceInfo, TInputService, TInputServiceInfo, TOutputService, TOutputServiceInfo> : IDmxProtocolService
	where TServiceInfo : IDmxProtocolServiceInfo
	where TInputService : class, IDmxInputProtocolService
	where TInputServiceInfo : IDmxInputProtocolServiceInfo
	where TOutputService : class, IDmxOutputProtocolService
	where TOutputServiceInfo : IDmxOutputProtocolServiceInfo
{
	private readonly UdpClientService _udpClientService;

	private readonly Dictionary<DmxUniverseAddress, List<TInputService>> _inputProtocolServices = new();
	private readonly Dictionary<DmxUniverseAddress, List<TOutputService>> _outputProtocolServices = new();

	protected DmxIpProtocolService(ILogger log, TServiceInfo info, IPEndPoint udpClientEndPoint, double outputFramerate, IEnumerable<IPAddress>? multicastGroups = null)
	{
		Log = log;
		TypedInfo = info;
		OutputFramerate = outputFramerate;

		_udpClientService = new UdpClientService(Log, udpClientEndPoint, multicastGroups ?? ImmutableList<IPAddress>.Empty);
		_udpClientService.PacketReceived += onPacketReceived;
	}

	public ValueTask DisposeAsync()
	{
		return _udpClientService.DisposeAsync();
	}


	public TServiceInfo TypedInfo { get; }

	public IDmxProtocolServiceInfo Info => TypedInfo;

	protected ILogger Log { get; }

	public abstract string ProtocolName { get; }

	public double OutputFramerate { get; }

	public IPEndPoint LocalEndPoint => _udpClientService.LocalEndPoint;

	public bool IsBoundToEndpoint => _udpClientService.IsBoundToEndpoint;

	public bool IsListening => _udpClientService.IsListening;


	internal IUdpSendService UdpSendService => _udpClientService;


	public ValueTask<bool> StartListeningAsync()
	{
		if (_inputProtocolServices.Any() && !_udpClientService.IsListening)
			_udpClientService.StartListening();

		return ValueTask.FromResult(IsListening);
	}

	public async ValueTask StopListeningAsync()
	{
		if (_udpClientService.IsListening)
			await _udpClientService.StopListeningAsync().ConfigureAwait(false);
	}

	public IEnumerable<IDmxInputProtocolService> InputProtocolServices => 
		_inputProtocolServices.Values.SelectMany(l => l);

	public IEnumerable<IDmxOutputProtocolService> OutputProtocolServices =>
		_outputProtocolServices.Values.SelectMany(l => l);

	public IDmxInputProtocolService? AddInputProtocolService(IDmxInputProtocolServiceInfo info)
	{
		if (info is TInputServiceInfo dmxInfo)
		{
			var inputProtocolService = CreateInputProtocolService(dmxInfo);
			if (inputProtocolService == null)
				return null;

			if (!_inputProtocolServices.TryGetValue(dmxInfo.UniverseAddress, out var serviceList))
			{
				serviceList = new List<TInputService>();
				_inputProtocolServices.Add(dmxInfo.UniverseAddress, serviceList);
			}

			serviceList.Add(inputProtocolService);

			return inputProtocolService;
		}
		else
		{
			throw new ArgumentException($"Incorrect {nameof(IDmxInputProtocolServiceInfo)} type", nameof(info));
		}
	}

	public bool RemoveInputProtocolService(Guid id)
	{
		foreach (var serviceList in _inputProtocolServices.Values)
		{
			var service = serviceList.FirstOrDefault(s => s.Info.Id == id);
			if (service != null)
			{
				serviceList.Remove(service);
				return true;
			}
		}

		return false;
	}

	public IDmxOutputProtocolService? AddOutputProtocolService(IDmxOutputProtocolServiceInfo info)
	{
		if (info is TOutputServiceInfo dmxInfo)
		{
			var outputProtocolService = CreateOutputProtocolService(dmxInfo);
			if (outputProtocolService == null)
				return null;

			if (!_outputProtocolServices.TryGetValue(dmxInfo.UniverseAddress, out var serviceList))
			{
				serviceList = new List<TOutputService>();
				_outputProtocolServices.Add(dmxInfo.UniverseAddress, serviceList);
			}

			serviceList.Add(outputProtocolService);

			return outputProtocolService;
		}
		else
		{
			throw new ArgumentException($"Incorrect {nameof(IDmxOutputProtocolServiceInfo)} type", nameof(info));
		}
	}

	public bool RemoveOutputProtocolService(Guid id)
	{
		foreach (var serviceList in _outputProtocolServices.Values)
		{
			var service = serviceList.FirstOrDefault(s => s.Info.Id == id);
			if (service != null)
			{
				serviceList.Remove(service);
				return true;
			}
		}

		return false;
	}


	public async Task SendOutputAsync()
	{
		var tasks = new List<Task<bool>>();
            
		foreach (var pair in _outputProtocolServices)
		{
			foreach (var service in pair.Value)
				tasks.Add(service.SendDataPacketAsync().AsTask());
		}

		await Task.WhenAll(tasks);
	}

	public void SendOutput()
	{
		foreach (var pair in _outputProtocolServices)
		{
			foreach (var service in pair.Value)
				service.SendDataPacket();
		}
	}


	protected IEnumerable<TInputService> GetInputServicesForUniverse(DmxUniverseAddress universeAddress) 
		=> _inputProtocolServices.TryGetValue(universeAddress, out var serviceList) ? serviceList : Enumerable.Empty<TInputService>();

	protected IEnumerable<TOutputService> GetOutputServicesForUniverse(DmxUniverseAddress universeAddress) 
		=> _outputProtocolServices.TryGetValue(universeAddress, out var serviceList) ? serviceList : Enumerable.Empty<TOutputService>();


	protected abstract TInputService? CreateInputProtocolService(TInputServiceInfo info);
	protected abstract TOutputService? CreateOutputProtocolService(TOutputServiceInfo info);
	protected virtual void OnRemoveInputProtocolService(TInputService service) { }
	protected virtual void OnRemoveOutputProtocolService(TInputService service) { }

	protected abstract IDmxDataPacket? DeserializePacket(ReadOnlySpan<byte> data);


	protected bool JoinMulticastGroup(IPAddress multicastIp) => _udpClientService.JoinMulticastGroup(multicastIp);
	protected void DropMulticastGroup(IPAddress multicastIp) => _udpClientService.DropMulticastGroup(multicastIp);

	private void onPacketReceived(object? sender, UdpReceiveResult e)
	{
		var dataPacket = DeserializePacket(e.Buffer.AsSpan());
		if (dataPacket is null || dataPacket.StartCode != 0)
			return;

		if (!_inputProtocolServices.TryGetValue(dataPacket.UniverseAddress, out var serviceList))
			return;

		foreach (var s in serviceList)
			s.OnDataPacketReceived(e.RemoteEndPoint.Address, dataPacket);
	}
}