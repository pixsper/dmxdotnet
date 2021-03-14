using System;
using System.Threading;

namespace Pixsper.DmxDotNet;

internal class HighResolutionTimer : IDisposable
{
	private readonly Thread _thread;
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public HighResolutionTimer(long intervalMicroseconds, long ignoreEventIfLateByMicroseconds = long.MaxValue)
	{
		IntervalMicroseconds = intervalMicroseconds;
		IgnoreEventIfLateByMicroseconds = ignoreEventIfLateByMicroseconds;

		_thread = new Thread(() => timerThreadProc(IntervalMicroseconds, IgnoreEventIfLateByMicroseconds, 
			_cancellationTokenSource.Token))
		{
			Priority = ThreadPriority.Highest
		};
		_thread.Start();
	}

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
		_thread.Join();

		_cancellationTokenSource.Dispose();
	}

	public event EventHandler<HighResolutionTimerElapsedEventArgs>? Elapsed;


	public long IntervalMicroseconds { get; }

	public long IgnoreEventIfLateByMicroseconds { get; }


        

	private void timerThreadProc(long timerInterval, long ignoreEventIfLateBy, CancellationToken cancellationToken)
	{
		var timerCount = 0;
		long nextNotification = 0;

		var microStopwatch = new MicroStopwatch();
		microStopwatch.Start();

		while (!cancellationToken.IsCancellationRequested)
		{
			long callbackFunctionExecutionTime = microStopwatch.ElapsedMicroseconds - nextNotification;
			nextNotification += timerInterval;
			++timerCount;

			long elapsedMicroseconds;

			while ((elapsedMicroseconds = microStopwatch.ElapsedMicroseconds) < nextNotification) { }

			var timerLateBy = elapsedMicroseconds - timerCount * timerInterval;

			if (timerLateBy >= ignoreEventIfLateBy)
				continue;

			Elapsed?.Invoke(this, new HighResolutionTimerElapsedEventArgs(timerCount, elapsedMicroseconds,
				timerLateBy, callbackFunctionExecutionTime));
		}

		microStopwatch.Stop();
	}

	internal class MicroStopwatch : System.Diagnostics.Stopwatch
	{
		public MicroStopwatch()
		{
			if (!IsHighResolution)
				throw new NotSupportedException("On this system the high-resolution performance counter is not available");
		}

		public double MicroSecPerTick => 1000000D / Frequency;

		public long ElapsedMicroseconds => (long)(ElapsedTicks * MicroSecPerTick);
	}
}

public class HighResolutionTimerElapsedEventArgs : EventArgs
{
	public HighResolutionTimerElapsedEventArgs(int timerCount, long elapsedMicroseconds,
		long timerLateBy, long callbackFunctionExecutionTime)
	{
		TimerCount = timerCount;
		ElapsedMicroseconds = elapsedMicroseconds;
		TimerLateBy = timerLateBy;
		CallbackFunctionExecutionTime = callbackFunctionExecutionTime;
	}

	/// <summary>
	/// Simple counter, number times timed event (callback function) executed
	/// </summary>
	public int TimerCount { get; }

	/// <summary>
	/// Time when timed event was called since timer started
	/// </summary>
	public long ElapsedMicroseconds { get; }

	/// <summary>
	/// How late the timer was compared to when it should have been called
	/// </summary>
	public long TimerLateBy { get; }

	/// <summary>
	/// The time it took to execute the previous call to the callback function (OnTimedEvent)
	/// </summary>
	public long CallbackFunctionExecutionTime { get; }
}