// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.ComponentModel;

namespace Pixsper.DmxDotNet;

public record ServiceStatus
{
	public static ServiceStatus Idle() => new(Code.Idle);
	public static ServiceStatus Ok() => new(Code.Ok);
	public static ServiceStatus Warning(string message) => new(Code.Warning, message);
	public static ServiceStatus Error(string message) => new(Code.Error, message);

	public ServiceStatus(Code statusCode, string message = "")
	{
		StatusCode = statusCode;
		Message = message;
	}

	public Code StatusCode { get; }
	public string Message { get; }

	public enum Code
	{
		[Description("OK")] Ok,
		[Description("Idle")] Idle,
		[Description("Warning")] Warning,
		[Description("Error")] Error
	}
}