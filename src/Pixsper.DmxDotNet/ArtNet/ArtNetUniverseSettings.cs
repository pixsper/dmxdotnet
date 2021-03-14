using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Pixsper.DmxDotNet.ArtNet
{
    /// <summary>
    ///     Class defining custom settings for data sent on an Art-Net universe
    /// </summary>
	public class ArtNetUniverseSettings : IDmxUniverseSettings
	{
		public static ArtNetUniverseSettings Default => new ArtNetUniverseSettings();

		public ArtNetUniverseSettings(IEnumerable<IPAddress>? unicastIps = null)
		{
			if (unicastIps != null)
			{
				var unicastIpsList = unicastIps as IList<IPAddress> ?? unicastIps.ToList();
				if (!unicastIpsList.Any())
					throw new ArgumentException("Collection cannot be empty", nameof(unicastIps));
				if (unicastIpsList.Distinct().Count() != unicastIpsList.Count)
					throw new ArgumentException("Collection cannot contain duplicates", nameof(unicastIps));
				UnicastIps = new HashSet<IPAddress>(unicastIpsList);
			}
			else
			{
				UnicastIps = null;
			}
		}

		public ArtNetUniverseSettings(IPAddress unicastIp)
			: this(new[] { unicastIp })
		{
			
		}

		public IEnumerable<IPAddress>? UnicastIps { get; }

		public int? UdpPort { get; }

		public int? ChannelCount { get; }
	}
}
