// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Pixsper.DmxDotNet.SAcn;

public record SAcnInputProtocolServiceInfo : IDmxInputProtocolServiceInfo
{
	public SAcnInputProtocolServiceInfo()
	{

	}

	public SAcnInputProtocolServiceInfo(Guid? id, DmxUniverseAddress universeAddress)
	{
		Id = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
		UniverseAddress = universeAddress;
	}


	public Guid Id { get; init; }
	public DmxUniverseAddress UniverseAddress { get; init; }


	public DmxProtocolKind Protocol => DmxProtocolKind.SAcn;
}