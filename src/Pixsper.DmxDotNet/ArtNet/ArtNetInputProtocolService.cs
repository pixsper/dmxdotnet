﻿// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Pixsper.DmxDotNet.Artnet;

internal class ArtNetInputProtocolService : IDmxInputProtocolService
{
	private readonly ILogger _log;
	private ArtNetProtocolService _parentService;
	private ServiceStatus _status = ServiceStatus.Idle();

	public ArtNetInputProtocolService(ILogger log, ArtNetProtocolService parentService,
		ArtNetInputProtocolServiceInfo info)
	{
		_log = log;
		_parentService = parentService;
		TypedInfo = info;
	}


	public event EventHandler<DmxDataReceivedEventArgs>? DataReceived;

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


	public ArtNetInputProtocolServiceInfo TypedInfo { get; }

       
	public IDmxInputProtocolServiceInfo Info => TypedInfo;
	public DateTime? LastDataReceived { get; private set; }


	public DmxUniverseAddress UniverseAddress => TypedInfo.UniverseAddress;
        
	public ReadOnlyMemory<byte>? Data { get; private set; }


	void IDmxInputProtocolService.RefreshStatus()
	{
		if (LastDataReceived is null || (DateTime.Now - LastDataReceived) > IDmxInputProtocolService.WarningMinimumPacketInterval)
			Status = ServiceStatus.Warning(IDmxInputProtocolService.NoDataStatusMessage);
		else
			Status = ServiceStatus.Ok();
	}

	void IDmxInputProtocolService.OnDataPacketReceived(IPAddress sourceAddress, IDmxDataPacket packet)
	{
		Data = packet.Data;
		LastDataReceived = DateTime.Now;
		Status = ServiceStatus.Ok();
		DataReceived?.Invoke(this, new DmxDataReceivedEventArgs(this, UniverseAddress, sourceAddress, Data.Value));
	}
}