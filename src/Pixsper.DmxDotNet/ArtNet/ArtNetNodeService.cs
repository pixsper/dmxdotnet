using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Pixsper.DmxDotNet.ArtNet.Packets;
using Pixsper.DmxDotNet.Networking;

namespace Pixsper.DmxDotNet.ArtNet
{
	public sealed class ArtNetNodeService : IDisposable
    {
        public const int ArtNetUdpPort = 0x1936;
		public const string ArtNetPrimaryAddress = "2.255.255.255";
		public const string ArtNetSecondaryAddress = "10.255.255.255";

		private readonly UdpReceiver _udpReceiver;

		private readonly ConcurrentDictionary<DmxUniverseAddress, ArtDmxPacket?> _registeredUniverses 
			= new ConcurrentDictionary<DmxUniverseAddress, ArtDmxPacket?>();  

		private bool _isDisposed = false;

		public ArtNetNodeService(string shortName, string longName, IPAddress? localIp = null)
		{
			if (localIp == null)
				throw new ArgumentNullException(nameof(localIp));

            ShortName = shortName;
            LongName = longName;

			_udpReceiver = new UdpReceiver(new IPEndPoint(localIp, ArtNetUdpPort));
			_udpReceiver.MessageReceived += messageReceived;
        }


		public bool IsListening => _udpReceiver.IsListening;

		public ICollection<DmxUniverseAddress> RegisteredUniverses => _registeredUniverses.Keys; 

		public string ShortName { get; }
		public string LongName { get; }

        public event EventHandler<DmxUniverse>? DmxReceived;
		public event EventHandler<IReadOnlyDictionary<DmxUniverseAddress, DmxUniverse>>? SyncedDmxReceived;

		public event EventHandler<byte[]>? InvalidPacketReceived;

		public void RegisterUniverse(DmxUniverseAddress address)
		{
			_registeredUniverses.TryAdd(address, null);
		}

		public void UnregisterUniverse(DmxUniverseAddress address)
		{
			_registeredUniverses.TryRemove(address, out _);
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			_udpReceiver.Dispose();
			_isDisposed = true;
		}

        public void StartListening()
        {
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(_udpReceiver));

			_udpReceiver.StartListening();
        }

		public void StopListening()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(_udpReceiver));

			_udpReceiver.StopListening();
		}

        private async void messageReceived(object sender, UdpReceiveResult message)
        {
            var packet = ArtPacket.FromByteArray(message.Buffer);
	        if (packet == null)
	        {
		        InvalidPacketReceived?.Invoke(this, message.Buffer);
		        return;
	        }
		        

	        switch (packet.OpCode)
	        {
		        case ArtPacket.ArtNetOpCode.OpDmx:
			        

			        break;

				case ArtPacket.ArtNetOpCode.OpSync:
					//SyncedDmxReceived?.Invoke(this, (ArtSyncPacket)packet);
			        break;

				case ArtPacket.ArtNetOpCode.OpPoll:
					await sendPollReplyAsync((ArtPollPacket)packet).ConfigureAwait(false);
			        break;

	        }
        }

		private void onDmxPacketReceived(ArtDmxPacket packet)
		{

			if (!_registeredUniverses.TryGetValue(packet.Address, out ArtDmxPacket? lastPacket))
				return;

            if (lastPacket != null)
            {
                int sequenceDifference = packet.Sequence - lastPacket.Sequence;
                if (sequenceDifference <= 0 && sequenceDifference > -20)
                    return;
            }

			

			//DmxPacketReceived?.Invoke(this, packet);
		}

		private async Task sendPollReplyAsync(ArtPollPacket packet)
		{
			//var replyPacket = new ArtPollReplyPacket(ShortName, LongName, ArtNetStyleCode.StNode, 0, 0, );

			//await _udpReceiver.SendToAsync(replyPacket.ToByteArray(), ArtNetPrimaryAddress, ArtNetUdpPort).ConfigureAwait(false);
			//await _udpReceiver.SendToAsync(replyPacket.ToByteArray(), ArtNetSecondaryAddress, ArtNetUdpPort).ConfigureAwait(false);
		}
    }
}