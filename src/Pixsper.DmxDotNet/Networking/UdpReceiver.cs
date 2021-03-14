using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pixsper.DmxDotNet.Networking
{
    /// <summary>
    ///     Helper class wrapping <see cref="UdpClient"/> to provide event-based UDP input service
    /// </summary>
	internal class UdpReceiver : IDisposable
	{
		private bool _isDisposed;

		private readonly UdpClient _udpClient;
		private CancellationTokenSource? _cancellationTokenSource;

		public UdpReceiver(IPEndPoint localEndPoint)
		{
			_udpClient = new UdpClient(localEndPoint)
			{
				ExclusiveAddressUse = false
			};
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			if (IsListening)
				StopListening();

			_udpClient.Dispose();

			_isDisposed = true;
		}

		public event EventHandler<UdpReceiveResult>? MessageReceived;


		public bool IsListening { get; private set; }

		public void StartListening()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			if (IsListening)
				throw new InvalidOperationException("Cannot start listening, UdpReceiver is already listening");

			_cancellationTokenSource = new CancellationTokenSource();
			Task.Run(() => receiveMessages(_cancellationTokenSource.Token));

			IsListening = true;
		}

		public void StopListening()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);

			if (!IsListening)
				throw new InvalidOperationException("Cannot stop listening, UdpReceiver is not currently listening");

			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;

			IsListening = false;
		}

		private async void receiveMessages(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				bool didReceive = false;
				UdpReceiveResult message;

				try
				{
					message = await _udpClient.ReceiveAsync().ConfigureAwait(false);
					didReceive = true;
				}
				catch
				{
					// Exception may be thrown by cancellation
					if (!cancellationToken.IsCancellationRequested)
						throw;
				}

				// If nothing received, must have been cancelled
				if (!didReceive)
					return;

				MessageReceived?.Invoke(this, message);
			}
		}
	}
}
