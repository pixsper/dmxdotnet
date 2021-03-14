using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Pixsper.DmxDotNet.SAcn.Packets;

namespace Pixsper.DmxDotNet.SAcn
{
    public class SAcnControllerService : IDmxControllerService
	{
		public static IPAddress GetMulticastAddressForUniverse(int universe)
		{
			if (universe < DmxUniverseAddress.SAcnAddressMin || universe > DmxUniverseAddress.SAcnAddressMax)
				throw new ArgumentOutOfRangeException(nameof(universe), universe, $"Must be in range {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");

			return new IPAddress(new byte[] {239, 255, (byte)((universe & 0xFF00) >> 8), (byte)(universe & 0x00FF)});
		}

		public const int DefaultUdpPort = 5568;
		public const float DefaultOutputRateHz = 44;
		public const int DefaultSynchronizationAddress = 1;

		private bool _isDisposed;

		private readonly UdpClient _udpClient;
		private readonly int _outputIntervalMs;
	    private float _actualOutputRateHz;
        private CancellationTokenSource? _cancellationTokenSource;

		private readonly ConcurrentDictionary<DmxUniverseAddress, SAcnUniverseSettings> _registeredUniverses 
			= new ConcurrentDictionary<DmxUniverseAddress, SAcnUniverseSettings>();

		private byte[] _buffer = new byte[0];
		private DmxUniverseAddress _startUniverse = DmxUniverseAddress.FromArtNetAddress(0);
		private DmxUniverseAddress _endUniverse = DmxUniverseAddress.FromArtNetAddress(0);

		private byte _sequence;
		
		public SAcnControllerService(string sourceName, IPAddress? localIp = null, Guid? cId = null, 
			int udpPort = DefaultUdpPort, float targetOutputRateHz = DefaultOutputRateHz, bool isSendSyncPacket = true,
			int synchronizationAddress = DefaultSynchronizationAddress, bool isForceSynchronization = false,
			int priority = SAcnDataPacket.PriorityDefault, bool isPreviewData = false)
		{
			if (sourceName == null)
				throw new ArgumentNullException(nameof(sourceName));
			if (sourceName.Length > SAcnDataPacket.MaxSourceNameLength - 1)
				throw new ArgumentException($"Source name cannot be longer than {SAcnDataPacket.MaxSourceNameLength - 1} characters", nameof(sourceName));
			SourceName = sourceName;

			CId = cId ?? Guid.NewGuid();

			if (udpPort < ushort.MinValue || udpPort > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(udpPort), udpPort, $"Must be in range {ushort.MinValue}-{ushort.MaxValue}");
			UdpPort = udpPort;

			if (targetOutputRateHz < double.Epsilon)
				throw new ArgumentOutOfRangeException(nameof(targetOutputRateHz), targetOutputRateHz, "Must be greater than 0");
			TargetOutputRateHz = targetOutputRateHz;
			_outputIntervalMs = (int)Math.Floor(1000d / TargetOutputRateHz);

			IsSendSyncPacket = isSendSyncPacket;

			if (synchronizationAddress < DmxUniverseAddress.SAcnAddressMin || synchronizationAddress > DmxUniverseAddress.SAcnAddressMax)
				throw new ArgumentOutOfRangeException(nameof(synchronizationAddress), synchronizationAddress,
					$"Must be in range {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			SynchronizationAddress = synchronizationAddress;

			IsForceSynchronization = isForceSynchronization;

			if (priority < SAcnDataPacket.PriorityMin || priority > SAcnDataPacket.PriorityMax)
				throw new ArgumentOutOfRangeException(nameof(priority), priority, $"Must be in range {SAcnDataPacket.PriorityMin}-{SAcnDataPacket.PriorityMax}");
			Priority = priority;

			IsPreviewData = isPreviewData;

			LocalEndPoint = new IPEndPoint(localIp ?? IPAddress.Any, 0);
			_udpClient = new UdpClient(LocalEndPoint) { EnableBroadcast = true };
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			if (IsSending)
				StopSending();

			_isDisposed = true;
		}

		public string SourceName { get; }
		public Guid CId { get; }

		public IPEndPoint LocalEndPoint { get; }
		public int UdpPort { get; }

		public float TargetOutputRateHz { get; }
	    public float ActualOutputRateHz => _actualOutputRateHz;
	    public bool IsSendSyncPacket { get; }
		public int SynchronizationAddress { get; }
		public int Priority { get; }
		public bool IsForceSynchronization { get; }
		public bool IsPreviewData { get; }

		public bool IsSending { get; private set; }

		public IReadOnlyDictionary<DmxUniverseAddress, IDmxUniverseSettings> RegisteredUniverses => 
			_registeredUniverses.ToDictionary(p => p.Key, p => (IDmxUniverseSettings)p.Value);


		public void StartSending()
		{
			if (_isDisposed)
				throw new ObjectDisposedException("SAcnControllerService");

			if (IsSending)
				throw new InvalidOperationException("Cannot start sending, service is already sending");

			_cancellationTokenSource = new CancellationTokenSource();
			Task.Factory.StartNew(() => sendTask(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning);

			IsSending = true;
		}

		public void StopSending()
		{
			if (_isDisposed)
				throw new ObjectDisposedException("SAcnControllerService");

			if (!IsSending)
				throw new InvalidOperationException("Cannot stop sending, service is not currently sending");

			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;

			sendData(true);

			IsSending = false;
		}

		public void AddUniverse(DmxUniverseAddress universe, IDmxUniverseSettings? customSettings = null)
		{
			if (customSettings == null)
				customSettings = SAcnUniverseSettings.Default;

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			if (customSettings != null && !(customSettings is SAcnUniverseSettings))
				throw new ArgumentException("Custom settings objects must be of type SAcnUniverseSettings", nameof(customSettings));

			addUniverse(universe, (SAcnUniverseSettings)customSettings!);
			recalculateBuffer();
		}

		public void AddUniverse(IDictionary<DmxUniverseAddress, IDmxUniverseSettings> universes)
		{
			if (universes == null)
				throw new ArgumentNullException(nameof(universes));

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (var pair in universes)
			{
				if (!(pair.Value is SAcnUniverseSettings))
					throw new ArgumentException("Custom settings objects must be of type SAcnUniverseSettings", nameof(universes));

				addUniverse(pair.Key, (SAcnUniverseSettings)pair.Value ?? SAcnUniverseSettings.Default);
			}

			recalculateBuffer();
		}

		public void AddUniverse(DmxUniverseAddress startUniverse, int count, IDmxUniverseSettings? customSettings = null)
		{
			if (customSettings != null && !(customSettings is SAcnUniverseSettings))
				throw new ArgumentException("Custom settings objects must be of type SAcnUniverseSettings", nameof(customSettings));

			if (customSettings == null)
				customSettings = SAcnUniverseSettings.Default;

			if (startUniverse < DmxUniverseAddress.SAcnAddressMin || startUniverse > DmxUniverseAddress.SAcnAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(startUniverse), startUniverse,
					$"Outside of sACN universe range, {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			}

			var range = Enumerable.Range(startUniverse, count).ToList();

			if (count < 1 || range.Max() > DmxUniverseAddress.SAcnAddressMin)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count,
					$"Must be in range 1-{DmxUniverseAddress.SAcnAddressMax - (startUniverse - 1)}");
			}

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (int universe in range)
				addUniverse(universe, (SAcnUniverseSettings)customSettings);

			recalculateBuffer();
		}

		public void RemoveUniverse(DmxUniverseAddress universe)
		{
			if (universe < DmxUniverseAddress.SAcnAddressMin || universe > DmxUniverseAddress.SAcnAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(universe), universe,
					$"Outside of sACN universe range, {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			}

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			removeUniverse(universe);
			recalculateBuffer();
		}

		public void RemoveUniverse(IEnumerable<DmxUniverseAddress> universes)
		{
			if (universes == null)
				throw new ArgumentNullException(nameof(universes));

			var universesList = universes as IList<DmxUniverseAddress> ?? universes.ToList();

			if (universesList.Any(u => u < DmxUniverseAddress.SAcnAddressMin || universesList.Min() > DmxUniverseAddress.SAcnAddressMax))
			{
				throw new ArgumentOutOfRangeException(nameof(universes), universes,
					$"All universes must be in sACN universe range, {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			}

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (int universe in universesList)
				removeUniverse(universe);

			recalculateBuffer();
		}

		public void RemoveUniverse(DmxUniverseAddress startUniverse, int count)
		{
			if (startUniverse < DmxUniverseAddress.SAcnAddressMin || startUniverse > DmxUniverseAddress.SAcnAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(startUniverse), startUniverse,
					$"Outside of sACN universe range, {DmxUniverseAddress.SAcnAddressMin}-{DmxUniverseAddress.SAcnAddressMax}");
			}

			var range = Enumerable.Range(startUniverse, count).ToList();

			if (count < 1 || range.Max() > DmxUniverseAddress.SAcnAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count,
					$"Must be in range 1-{DmxUniverseAddress.SAcnAddressMax - (startUniverse - 1)}");
			}

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (int universe in range)
				removeUniverse(universe);

			recalculateBuffer();
		}

		public void ClearUniverses()
		{
			_registeredUniverses.Clear();
			recalculateBuffer();
		}

		public void WriteData(DmxUniverseAddress startUniverse, byte[] data, int? count = null)
		{
			if (startUniverse < _startUniverse || startUniverse > _endUniverse)
				throw new ArgumentOutOfRangeException(nameof(startUniverse), startUniverse, "Outside of registered universe range");

			int startOffset = (startUniverse - 1) * SAcnDataPacket.DataLengthMax;

			if ((count ?? data.Length) > _buffer.Length - startOffset)
				throw new IndexOutOfRangeException("Data length/count is greater than the buffer length for registered universes");

			Buffer.BlockCopy(data, 0, _buffer, startOffset, count ?? data.Length);
		}



		private void addUniverse(int universe, SAcnUniverseSettings settings)
		{
			_registeredUniverses.AddOrUpdate(universe, settings, (key, oldValue) => settings);
		}

		private void removeUniverse(int universe)
		{
			SAcnUniverseSettings settings;
			bool result = _registeredUniverses.TryRemove(universe, out settings);

			if (!result)
				throw new InvalidOperationException($"Universe '{universe}' not registered");
		}

		private void sendTask(CancellationToken token)
		{
            var stopwatch = new Stopwatch();
            var waitEvent = new ManualResetEventSlim(false);

            while (!token.IsCancellationRequested)
            {
                long frameTime = stopwatch.ElapsedMilliseconds;
                if (frameTime != 0)
					_actualOutputRateHz = 1000f / frameTime;
				
                stopwatch.Restart();
				sendData();

                waitEvent.Wait(Math.Max(0, _outputIntervalMs - (int)stopwatch.ElapsedMilliseconds), token);
            }

			_actualOutputRateHz = 0;

            waitEvent.Dispose();
		}

		private void sendData(bool isEndStream = false)
		{
			foreach (var pair in _registeredUniverses)
			{
				var settings = pair.Value;
				int channelCount = settings.ChannelCount ?? SAcnDataPacket.DataLengthMax;
				var dmxData = new byte[channelCount];

				Buffer.BlockCopy(_buffer, (pair.Key - _startUniverse) * SAcnDataPacket.DataLengthMax, dmxData, 0, channelCount);

				var packet = new SAcnDataPacket(settings.CId ?? CId, settings.SourceName ?? SourceName,
					_sequence, pair.Key, dmxData, settings.SynchronizationAddress ?? SynchronizationAddress,
					settings.Priority ?? Priority, settings.StartCode ?? SAcnDataPacket.StartCodeDefault,
					settings.IsForceSynchronization ?? IsForceSynchronization, isEndStream, settings.IsPreviewData ?? IsPreviewData);

				var data = packet.ToByteArray();

				if (settings.UnicastIps != null)
				{
					foreach (var ip in settings.UnicastIps)
					{
						var endPoint = new IPEndPoint(ip, settings.UdpPort ?? DefaultUdpPort);
						_udpClient.SendAsync(data, data.Length, endPoint).Wait();
					}
				}
				else
				{
					var endPoint = new IPEndPoint(GetMulticastAddressForUniverse(pair.Key), settings.UdpPort ?? DefaultUdpPort);
					_udpClient.SendAsync(data, data.Length, endPoint).Wait();
				}
			}

			if (IsSendSyncPacket)
			{
				var syncPacket = new SAcnSyncPacket(CId);
				var syncData = syncPacket.ToByteArray();
				var syncEndPoint = new IPEndPoint(GetMulticastAddressForUniverse(SynchronizationAddress), UdpPort);

			    _udpClient.SendAsync(syncData, syncData.Length, syncEndPoint).Wait();
			}

			unchecked
			{
				++_sequence;
			}
		}




		private void recalculateBuffer()
		{
			var universeNumbers = _registeredUniverses.Keys;

			if (universeNumbers.Count == 0)
			{
				_startUniverse = 0;
				_endUniverse = 0;
				_buffer = new byte[0];
				return;
			}

			_startUniverse = universeNumbers.Min();
			_endUniverse = universeNumbers.Max();

			var buffer = new byte[(_endUniverse + 1) * 512 - _startUniverse * 512];

			_buffer = buffer;
		}
	}
}
