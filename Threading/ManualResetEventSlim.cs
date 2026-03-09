using PowerThreadPool_Net20.Helpers;
using System;
using System.Threading;

namespace PowerThreadPool_Net20.Threading
{
	[System.Diagnostics.DebuggerDisplayAttribute ("Set = {IsSet}")]
	public class ManualResetEventSlim : IDisposable
	{
		readonly int spinCount;

		object handle;
        ManualResetEvent Handle
        {
            get { return (ManualResetEvent)handle; }
        }

		internal AtomicBooleanValue disposed;
		int used;
		int state;

		public ManualResetEventSlim ()
			: this (false, 10)
		{
		}

		public ManualResetEventSlim (bool initialState)
			: this (initialState, 10)
		{
		}

		public ManualResetEventSlim (bool initialState, int spinCount)
		{
			if (spinCount < 0 || spinCount > 2047)
				throw new ArgumentOutOfRangeException ("spinCount");

			this.state = initialState ? 1 : 0;
			this.spinCount = spinCount;
		}

		public bool IsSet {
			get {
				return (state & 1) == 1;
			}
		}

		public int SpinCount {
			get {
				return spinCount;
			}
		}

		public void Reset ()
		{
			ThrowIfDisposed ();

			var stamp = UpdateStateWithOp (false);
			if (handle != null)
				CommitChangeToHandle (stamp);
		}

		public void Set ()
		{
			var stamp = UpdateStateWithOp (true);
			if (handle != null)
				CommitChangeToHandle (stamp);
		}

		long UpdateStateWithOp (bool set)
		{
			int oldValue, newValue;
			do {
				oldValue = state;
				newValue = (int)(((oldValue >> 1) + 1) << 1) | (set ? 1 : 0);
			} while (Interlocked.CompareExchange (ref state, newValue, oldValue) != oldValue);
			return newValue;
		}

		void CommitChangeToHandle (long stamp)
		{
			Interlocked.Increment (ref used);
			var tmpHandle = Handle;
			if (tmpHandle != null) {
				// First in all case we carry the operation we were called for
 				if ((stamp & 1) == 1)
					tmpHandle.Set ();
				else
					tmpHandle.Reset ();

				/* Then what may happen is that the two suboperations (state change and handle change)
				 * overlapped with others. In our case it doesn't matter if the two suboperations aren't
				 * executed together at the same time, the only thing we have to make sure of is that both
				 * state and handle are synchronized on the last visible state change.
				 *
				 * For instance if S is state change and H is handle change, for 3 concurrent operations
				 * we may have the following serialized timeline: S1 S2 H2 S3 H3 H1
				 * Which is perfectly fine (all S were converted to H at some stage) but in that case
				 * we have a mismatch between S and H at the end because the last operations done were
				 * S3/H1. We thus need to repeat H3 to get to the desired final state.
				 */
				int currentState;
				do {
					currentState = state;
					if (currentState != stamp && (stamp & 1) != (currentState & 1)) {
						if ((currentState & 1) == 1)
							tmpHandle.Set ();
						else
							tmpHandle.Reset ();
					}
				} while (currentState != state);
			}
			Interlocked.Decrement (ref used);
		}

		public void Wait ()
		{
			Wait (CancellationToken.None);
		}

		public bool Wait (int millisecondsTimeout)
		{
			return Wait (millisecondsTimeout, CancellationToken.None);
		}

		public bool Wait (TimeSpan timeout)
		{
			return Wait (CheckTimeout (timeout), CancellationToken.None);
		}

		public void Wait (CancellationToken cancellationToken)
		{
			Wait (Timeout.Infinite, cancellationToken);
		}

		public bool Wait (int millisecondsTimeout, CancellationToken cancellationToken)
		{
			if (millisecondsTimeout < -1)
				throw new ArgumentOutOfRangeException ("millisecondsTimeout");

			ThrowIfDisposed ();

			if (!IsSet) {
				SpinWait wait = new SpinWait ();

				while (!IsSet) {
					if (wait.Count < spinCount) {
						wait.SpinOnce ();
						continue;
					}

					break;
				}

				cancellationToken.ThrowIfCancellationRequested ();

				if (IsSet)
					return true;

				WaitHandle handle = WaitHandle;

				if (cancellationToken.CanBeCanceled) {
					var result = WaitHandle.WaitAny (new[] { handle, cancellationToken.WaitHandle }, millisecondsTimeout, false);
					if (result == 1)
                        throw new OperationCanceledException (cancellationToken.ToString());
					if (result == WaitHandle.WaitTimeout)
						return false;
				} else {
					if (!handle.WaitOne (millisecondsTimeout, false))
						return false;
				}
			}

			return true;
		}

		public bool Wait (TimeSpan timeout, CancellationToken cancellationToken)
		{
			return Wait (CheckTimeout (timeout), cancellationToken);
		}

		public WaitHandle WaitHandle {
			get {
				ThrowIfDisposed ();

				if (handle != null)
					return Handle;

				var isSet = IsSet;
				var mre = new ManualResetEvent (IsSet);
				if (Interlocked.CompareExchange (ref handle, mre, null) == null) {
					//
					// Ensure the Set has not ran meantime
					//
					if (isSet != IsSet) {
						if (IsSet) {
							mre.Set ();
						} else {
							mre.Reset ();
						}
					}
				} else {
					//
					// Release the event when other thread was faster
					//
                    ((IDisposable)mre).Dispose ();
				}

				return Handle;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposed.TryRelaxedSet ())
				return;

			if (handle != null) {
				var tmpHandle = Interlocked.Exchange (ref handle, null);
				if (used > 0) {
					// A tiny wait (just a few cycles normally) before releasing
					SpinWait wait = new SpinWait ();
					while (used > 0)
						wait.SpinOnce ();
				}
                ((IDisposable)tmpHandle).Dispose ();
			}
		}

		void ThrowIfDisposed ()
		{
			if (disposed.Value)
				throw new ObjectDisposedException ("ManualResetEventSlim");
		}

		static int CheckTimeout (TimeSpan timeout)
		{
			try {
				return checked ((int)timeout.TotalMilliseconds);
			} catch (System.OverflowException) {
				throw new ArgumentOutOfRangeException ("timeout");
			}
		}
	}

	/*
	     /// <summary>
    /// .NET 2.0兼容的轻量级手动重置事件
    /// .NET 2.0 compatible lightweight manual reset event
    /// </summary>
    public class ManualResetEventSlim
    {
        private ManualResetEvent _event;
        private volatile bool _isSet;

        /// <summary>
        /// 构造函数（初始状态为未设置）
        /// Constructor (initial state is not set)
        /// </summary>
        public ManualResetEventSlim()
            : this(false)
        {
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="initialState">初始状态 / Initial state</param>
        public ManualResetEventSlim(bool initialState)
        {
            _event = new ManualResetEvent(initialState);
            _isSet = initialState;
        }

        /// <summary>
        /// 设置事件（变为有信号状态）
        /// Set the event (to signaled state)
        /// </summary>
        public void Set()
        {
            _isSet = true;
            _event.Set();
        }

        /// <summary>
        /// 重置事件（变为无信号状态）
        /// Reset the event (to non-signaled state)
        /// </summary>
        public void Reset()
        {
            _isSet = false;
            _event.Reset();
        }

        /// <summary>
        /// 等待事件变为有信号状态
        /// Wait for the event to become signaled
        /// </summary>
        public void Wait()
        {
            _event.WaitOne();
        }

        /// <summary>
        /// 等待事件变为有信号状态（带超时）
        /// Wait for the event to become signaled (with timeout)
        /// </summary>
        /// <param name="timeoutMilliseconds">超时毫秒数 / Timeout in milliseconds</param>
        /// <returns>是否在超时前收到信号 / Whether signal received before timeout</returns>
        public bool Wait(int timeoutMilliseconds)
        {
            return _event.WaitOne(timeoutMilliseconds, false);
        }

        /// <summary>
        /// 是否已设置
        /// Whether is set
        /// </summary>
        public bool IsSet
        {
            get { return _isSet; }
        }

        /// <summary>
        /// 获取底层的WaitHandle
        /// Get underlying WaitHandle
        /// </summary>
        public WaitHandle WaitHandle
        {
            get { return _event; }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_event != null)
            {
                _event.Close();
                _event = null;
            }
        }
    }
	 */
}
