// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pixsper.DmxDotNet.SAcn.Packets;

namespace Pixsper.DmxDotNet.SAcn;

internal class SAcnOutputProtocolService : IDmxOutputProtocolService
{
	private readonly ILogger _log;
	private readonly SAcnProtocolService _parentService;
	private ServiceStatus _status = ServiceStatus.Idle();

	private readonly byte[] _data = new byte[SAcnDataPacket.DataLengthMax];

	private byte _sequence;

	private readonly IPEndPoint _targetEndPoint;

	public SAcnOutputProtocolService(ILogger log, SAcnProtocolService parentService, SAcnOutputProtocolServiceInfo info)
	{
		_log = log;
		_parentService = parentService;
		TypedInfo = info;

		_targetEndPoint = TypedInfo.IsUnicast 
			? new IPEndPoint(TypedInfo.UnicastAddress, SAcnProtocolService.SAcnPort) 
			: new IPEndPoint(TypedInfo.UniverseAddress.ToSAcnMulticastIp(), SAcnProtocolService.SAcnPort);
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


	public SAcnOutputProtocolServiceInfo TypedInfo { get; }

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
		var packet = new SAcnDataPacket(_parentService.TypedInfo.CId, TypedInfo.SourceName, _sequence, TypedInfo.UniverseAddress, _data);

		var packetBuffer = packet.ToMemory();

		int bytesSent = await _parentService.UdpSendService.SendAsync(packetBuffer, _targetEndPoint);

		if (bytesSent != packetBuffer.Length)
		{
			_log.LogDebug("Failed to send SAcn packet to {TargetEndPoint}", _targetEndPoint);
			return false;
		}

		unchecked
		{
			++_sequence;
		}

		return true;
	}

	public bool SendDataPacket()
	{
		var packet = new SAcnDataPacket(_parentService.TypedInfo.CId, TypedInfo.SourceName, _sequence, TypedInfo.UniverseAddress, _data);

		var packetBuffer = packet.ToSpan();

		int bytesSent = _parentService.UdpSendService.Send(packetBuffer, _targetEndPoint);

		if (bytesSent != packetBuffer.Length)
		{
			_log.LogDebug("Failed to send SAcn packet to {TargetEndPoint}", _targetEndPoint);
			return false;
		}

		unchecked
		{
			++_sequence;
		}

		return true;
	}
}