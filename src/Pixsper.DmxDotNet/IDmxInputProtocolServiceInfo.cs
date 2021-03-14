// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;

namespace Pixsper.DmxDotNet;

public interface IDmxInputProtocolServiceInfo
{
	Guid Id { get; }

	DmxUniverseAddress UniverseAddress { get; }

	DmxProtocolKind Protocol { get; }
}