// Copyright (c) 2022 Pixsper Ltd. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pixsper.DmxDotNet;

internal static class LinqExtensions
{
	public static string JoinAsString<T>(this IEnumerable<T> values, char separator)
	{
		return string.Join(separator, values);
	}

	public static string JoinAsString<TSource, TResult>(this IEnumerable<TSource> values, char separator, Func<TSource, TResult> selector)
	{
		return string.Join(separator, values.Select(selector));
	}
}