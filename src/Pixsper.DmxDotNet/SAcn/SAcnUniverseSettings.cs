using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Pixsper.DmxDotNet.SAcn.Packets;

namespace Pixsper.DmxDotNet.SAcn
{
    /// <summary>
    ///     Class defining custom settings for data sent on an sACN universe
    /// </summary>
    public class SAcnUniverseSettings : IDmxUniverseSettings
    {
        /// <summary>
        ///     Instance of the default settings for an sACN universe.
        /// </summary>
        public static SAcnUniverseSettings Default { get; } = new SAcnUniverseSettings();

        public SAcnUniverseSettings(IEnumerable<IPAddress>? unicastIps, int? udpPort = null,
            Guid? cId = null, string? sourceName = null, int? synchronizationAddress = null,
            int priority = SAcnDataPacket.PriorityDefault, int? startCode = null, int? channelCount = null,
            bool isForceSynchronization = false, bool isStreamTerminated = false, bool isPreviewData = false)
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

            if (udpPort.HasValue && (udpPort.Value < ushort.MinValue || udpPort.Value > ushort.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(udpPort), udpPort,
                    $"Must be in range {ushort.MinValue}-{ushort.MaxValue}");
            UdpPort = udpPort;

            CId = cId;

            if (sourceName != null && sourceName.Length > SAcnDataPacket.MaxSourceNameLength - 1)
                throw new ArgumentException(
                    $"Source name cannot be longer than {SAcnDataPacket.MaxSourceNameLength - 1} characters",
                    nameof(sourceName));
            SourceName = sourceName;

            if (synchronizationAddress.HasValue)
            {
                if (synchronizationAddress.Value < DmxUniverseAddress.SAcnAddressMin
                    || synchronizationAddress.Value > DmxUniverseAddress.SAcnAddressMax)
                    throw new ArgumentOutOfRangeException(nameof(synchronizationAddress), synchronizationAddress,
                        $"Must be in range {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
            }
            SynchronizationAddress = synchronizationAddress;

            if (priority < SAcnDataPacket.PriorityMin || priority > SAcnDataPacket.PriorityMax)
                throw new ArgumentOutOfRangeException(nameof(priority), priority,
                    $"Must be in range {SAcnDataPacket.PriorityMin}-{SAcnDataPacket.PriorityMax}");
            Priority = priority;

            if (startCode.HasValue && (startCode < byte.MinValue || startCode > byte.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(startCode), startCode,
                    $"Must be in range {byte.MinValue}-{byte.MaxValue}");
            StartCode = startCode;

            if (channelCount.HasValue
                && (channelCount < SAcnDataPacket.DataLengthMin || channelCount > SAcnDataPacket.DataLengthMax))
                throw new ArgumentOutOfRangeException(nameof(channelCount), channelCount,
                    $"Must be in range {SAcnDataPacket.DataLengthMin}-{SAcnDataPacket.DataLengthMax}");
            ChannelCount = channelCount;

            IsForceSynchronization = isForceSynchronization;
            IsStreamTerminated = isStreamTerminated;
            IsPreviewData = isPreviewData;
        }

        public SAcnUniverseSettings(IPAddress? unicastIp = null, int? udpPort = null,
            Guid? cId = null, string? sourceName = null, int? synchronizationAddress = null,
            int priority = SAcnDataPacket.PriorityDefault, int? startCode = null, int? channelCount = null,
            bool isForceSynchronization = false, bool isStreamTerminated = false, bool isPreviewData = false)
            : this(unicastIp != null ? new[] { unicastIp } : null, udpPort, cId, sourceName, synchronizationAddress,
                priority, startCode, channelCount, isForceSynchronization, isStreamTerminated, isPreviewData) { }


        /// <summary>
        ///     Collection of IPs which data for this universe should be sent to, or null if data should be sent to the default
        ///     multicast IP.
        /// </summary>
        /// <remarks>
        ///     Guaranteed not to contain duplicates
        /// </remarks>
        public IEnumerable<IPAddress>? UnicastIps { get; }

        /// <summary>
        ///     Custom UDP port which data for this universe should be sent to, or null if data should be sent to the default sACN
        ///     port.
        /// </summary>
        public int? UdpPort { get; }

        /// <summary>
        ///     Priority at which data for this universe should be sent with, or null if data should be sent at the controller
        ///     service's default priority.
        /// </summary>
        public int? Priority { get; }

        /// <summary>
        ///     The IsPreviewData flag for this universe, or null if universe should be sent with the controller service's default setting.
        /// </summary>
        public bool? IsPreviewData { get; }
        /// <summary>
        ///     The IsStreamTerminated flag for this universe, or null if universe should be sent with the controller service's default setting.
        /// </summary>
        public bool? IsStreamTerminated { get; }
        /// <summary>
        ///     The IsForceSynchronization flag for this universe, or null if universe should be sent with the controller service's default setting.
        /// </summary>
        public bool? IsForceSynchronization { get; }
        /// <summary>
        ///     The SynchronizationAddress for this universe, or null if universe should be sent with the controller service's default address.
        /// </summary>
        public int? SynchronizationAddress { get; }

        /// <summary>
        ///     The CId for this universe, or null if universe should be sent with the controller service's default CId.
        /// </summary>
        public Guid? CId { get; }

        /// <summary>
        ///     The SourceName for this universe, or null if universe should be sent with the controller service's default source name.
        /// </summary>
        public string? SourceName { get; }

        /// <summary>
        ///     The start code for DMX packets sent for this universe, or null if the default start code should be used.
        /// </summary>
        public int? StartCode { get; }

        /// <summary>
        ///     The channel count for DMX packets sent for this universe, or null to send the maximum channel count
        /// </summary>
        public int? ChannelCount { get; }
    }
}