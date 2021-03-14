// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pixsper.DmxDotNet.Artnet.Packets;

namespace Pixsper.DmxDotNet.Artnet;

internal class ArtNetOutputProtocolService : IDmxOutputProtocolService
{
	private readonly ILogger _log;
	private readonly ArtNetProtocolService _parentService;
	private ServiceStatus _status = ServiceStatus.Idle();

	private readonly byte[] _data = new byte[ArtDmxPacket.MaxChannels];
	private byte _sequence = 1;

	private readonly IPEndPoint _targetEndPoint;


	public ArtNetOutputProtocolService(ILogger log, ArtNetProtocolService parentService,
		ArtNetOutputProtocolServiceInfo info)
	{
		_log = log;
		_parentService = parentService;
		TypedInfo = info;

		if (TypedInfo.IsBroadcast)
		{
			_targetEndPoint = new IPEndPoint(TypedInfo.IsAltBroadcastAddress
				? ArtNetProtocolService.ArtNetAltBroadcastAddress
				: ArtNetProtocolService.ArtNetBroadcastAddress, ArtNetProtocolService.ArtNetPort);
		}
		else
		{
			_targetEndPoint = new IPEndPoint(TypedInfo.UnicastAddress, ArtNetProtocolService.ArtNetPort);
		}
	}

	public event EventHandler<ServiceStatus>? StatusChanged;
	public ServiceStatus Status
	{
		get => _status;
		set
		{
			if (_status != value)
			{
				_status = value;
				StatusChanged?.Invoke(this, _status);
			}
		}
	}


	public ArtNetOutputProtocolServiceInfo TypedInfo { get; }

	public IDmxOutputProtocolServiceInfo Info => TypedInfo;



	public DmxUniverseAddress UniverseAddress => TypedInfo.UniverseAddress;
        
	public Memory<byte> Data => _data;


	public void SetData(Span<byte> data)
	{
		if (data.Length > _data.Length)
			throw new ArgumentException("Data too long", nameof(data));

		data.CopyTo(_data);
	}

	public void SetData(Memory<byte> data)
	{
		if (data.Length > _data.Length)
			throw new ArgumentException("Data too long", nameof(data));

		data.CopyTo(_data);
	}

	public async ValueTask<bool> SendDataPacketAsync()
	{
		var packet = new ArtDmxPacket(Info.UniverseAddress, _data, _sequence);

		var packetBuffer = packet.ToMemory();

		int bytesSent = await _parentService.UdpSendService.SendAsync(packetBuffer, _targetEndPoint);

		if (bytesSent != packetBuffer.Length)
		{
			_log.LogDebug("Failed to send ArtNet packet to {TargetEndPoint}", _targetEndPoint);
			return false;
		}

		unchecked
		{
			++_sequence;
			if (_sequence == 0)
				++_sequence;
		}

		return true;
	}

	public bool SendDataPacket()
	{
		var packet = new ArtDmxPacket(Info.UniverseAddress, _data, _sequence);

		var packetBuffer = packet.ToSpan();

		int bytesSent = _parentService.UdpSendService.Send(packetBuffer, _targetEndPoint);

		if (bytesSent != packetBuffer.Length)
		{
			_log.LogDebug("Failed to send ArtNet packet to {TargetEndPoint}", _targetEndPoint);
			return false;
		}

		unchecked
		{
			++_sequence;
			if (_sequence == 0)
				++_sequence;
		}

		return true;
	}
}