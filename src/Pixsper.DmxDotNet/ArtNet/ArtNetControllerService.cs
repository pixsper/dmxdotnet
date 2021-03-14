using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Pixsper.DmxDotNet.ArtNet.Packets;
using Pixsper.DmxDotNet.SAcn.Packets;

namespace Pixsper.DmxDotNet.ArtNet
{
	public class ArtNetControllerService : IDmxControllerService
	{
		public const int DefaultUdpPort = 0x1936;
		public const float DefaultOutputRateHz = 44;

		public static readonly IPAddress DefaultPrimaryBroadcastAddress = IPAddress.Parse("2.255.255.255");
		public static readonly IPAddress DefaultSecondaryBroadcastAddress = IPAddress.Parse("10.255.255.255");

		private bool _isDisposed;

		private readonly UdpClient _udpClient;
		private readonly int _outputIntervalMs;
	    private float _actualOutputRateHz;
		private CancellationTokenSource? _cancellationTokenSource;

		private readonly ConcurrentDictionary<DmxUniverseAddress, ArtNetUniverseSettings> _registeredUniverses 
			= new ConcurrentDictionary<DmxUniverseAddress, ArtNetUniverseSettings>();

		private byte[] _buffer = new byte[0];
		private DmxUniverseAddress _startUniverse = DmxUniverseAddress.FromArtNetAddress(0);
		private DmxUniverseAddress _endUniverse = DmxUniverseAddress.FromArtNetAddress(0);

		private byte _sequence;

		public ArtNetControllerService(string sourceName, 
			bool isUseSecondaryBroadcastIp = false,  IPAddress? localIp = null,
			int udpPort = DefaultUdpPort, float targetOutputRateHz = DefaultOutputRateHz, bool isSendSyncPacket = true)
		{
			if (sourceName == null)
				throw new ArgumentNullException(nameof(sourceName));
			if (sourceName.Length > SAcnDataPacket.MaxSourceNameLength - 1)
				throw new ArgumentException($"Source name cannot be longer than {SAcnDataPacket.MaxSourceNameLength - 1} characters", nameof(sourceName));
			SourceName = sourceName;

			if (udpPort < ushort.MinValue || udpPort > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(udpPort), udpPort, $"Must be in range {ushort.MinValue}-{ushort.MaxValue}");
			UdpPort = udpPort;

			if (targetOutputRateHz < double.Epsilon)
				throw new ArgumentOutOfRangeException(nameof(targetOutputRateHz), targetOutputRateHz, "Must be greater than 0");
			TargetOutputRateHz = targetOutputRateHz;
			_outputIntervalMs = (int)Math.Round(1000d / TargetOutputRateHz, MidpointRounding.AwayFromZero);

			IsSendSyncPacket = isSendSyncPacket;

			BroadcastIp = isUseSecondaryBroadcastIp ? DefaultSecondaryBroadcastAddress : DefaultPrimaryBroadcastAddress;

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

		public IPEndPoint LocalEndPoint { get; }
		public int UdpPort { get; }
		public IPAddress BroadcastIp { get; }

		public float TargetOutputRateHz { get; }
	    public float ActualOutputRateHz => _actualOutputRateHz;

	    public bool IsSendSyncPacket { get; }

		public bool IsSending { get; private set; }

		public IReadOnlyDictionary<DmxUniverseAddress, IDmxUniverseSettings> RegisteredUniverses => 
			_registeredUniverses.ToDictionary(p => p.Key, p => (IDmxUniverseSettings)p.Value);


		public void StartSending()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			if (IsSending)
				throw new InvalidOperationException("Cannot start sending, service is already sending");

			_cancellationTokenSource = new CancellationTokenSource();
			Task.Factory.StartNew(() => sendTask(_cancellationTokenSource.Token), TaskCreationOptions.LongRunning);

			IsSending = true;
		}

		public void StopSending()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

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
				customSettings = ArtNetUniverseSettings.Default;

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			if (customSettings != null && !(customSettings is ArtNetUniverseSettings))
				throw new ArgumentException("Custom settings objects must be of type ArtNetUniverseSettings", nameof(customSettings));

			addUniverse(universe, (ArtNetUniverseSettings)customSettings!);
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
				if (!(pair.Value is ArtNetUniverseSettings))
					throw new ArgumentException("Custom settings objects must be of type ArtNetUniverseSettings", nameof(universes));

				addUniverse(pair.Key, (ArtNetUniverseSettings)pair.Value ?? ArtNetUniverseSettings.Default);
			}

			recalculateBuffer();
		}

		public void AddUniverse(DmxUniverseAddress startUniverse, int count, IDmxUniverseSettings? customSettings = null)
		{
			if (customSettings != null && !(customSettings is ArtNetUniverseSettings))
				throw new ArgumentException("Custom settings objects must be of type ArtNetUniverseSettings", nameof(customSettings));

			if (customSettings == null)
				customSettings = ArtNetUniverseSettings.Default;

			var range = Enumerable.Range(startUniverse, count).ToList();

			if (count < 1 || range.Max() > DmxUniverseAddress.ArtNetAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count,
					$"Must be in range 1-{DmxUniverseAddress.ArtNetAddressMax - (startUniverse - 1)}");
			}

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (int universe in range)
				addUniverse(universe, (ArtNetUniverseSettings)customSettings);

			recalculateBuffer();
		}

		public void RemoveUniverse(DmxUniverseAddress universe)
		{
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

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			foreach (var universe in universesList)
				removeUniverse(universe);

			recalculateBuffer();
		}

		public void RemoveUniverse(DmxUniverseAddress startUniverse, int count)
		{
			var range = Enumerable.Range(startUniverse, count).ToList();

			if (count < 1 || range.Max() > DmxUniverseAddress.ArtNetAddressMax)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count,
					$"Must be in range 1-{DmxUniverseAddress.ArtNetAddressMax - (startUniverse - 1)}");
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

			int startOffset = startUniverse.Address * SAcnDataPacket.DataLengthMax;

			if ((count ?? data.Length) > _buffer.Length - startOffset)
				throw new IndexOutOfRangeException("Data length/count is greater than the buffer length for registered universes");

			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			Buffer.BlockCopy(data, 0, _buffer, startOffset, count ?? data.Length);
		}



		private void addUniverse(DmxUniverseAddress universe, ArtNetUniverseSettings settings)
		{
			_registeredUniverses.AddOrUpdate(universe, settings, (key, oldValue) => settings);
		}

		private void removeUniverse(DmxUniverseAddress universe)
		{
			ArtNetUniverseSettings settings;
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
				int channelCount = settings.ChannelCount ?? ArtDmxPacket.MaxChannels;
				var dmxData = new byte[channelCount];

				Buffer.BlockCopy(_buffer, (pair.Key - _startUniverse).Address * SAcnDataPacket.DataLengthMax, dmxData, 0, channelCount);

				var packet = new ArtDmxPacket(pair.Key, dmxData, _sequence);

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
					var endPoint = new IPEndPoint(DefaultPrimaryBroadcastAddress, settings.UdpPort ?? DefaultUdpPort);
					_udpClient.SendAsync(data, data.Length, endPoint).Wait();
				}
			}

			if (IsSendSyncPacket)
			{
				var syncPacket = new ArtSyncPacket();
				var syncData = syncPacket.ToByteArray();
				var syncEndPoint = new IPEndPoint(DefaultPrimaryBroadcastAddress, UdpPort);
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
				_startUniverse = DmxUniverseAddress.FromArtNetAddress(0);
				_endUniverse = DmxUniverseAddress.FromArtNetAddress(0);
				_buffer = new byte[0];
				return;
			}

			_startUniverse = universeNumbers.Min();
			_endUniverse = universeNumbers.Max();

			var buffer = new byte[(_endUniverse.Address + 1) * 512 - _startUniverse.Address * 512];

			_buffer = buffer;
		}
	}
}
