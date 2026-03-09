using PowerThreadPool_Net20.Collections;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerThreadPool_Net20.Threading
{
    //[DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
    public struct CancellationToken
    {
        readonly CancellationTokenSource source;

        public CancellationToken(bool canceled)
            : this(canceled ? CancellationTokenSource.CanceledSource : null) {
        }

        internal CancellationToken(CancellationTokenSource source) {
            this.source = source;
        }

        public static CancellationToken None {
            get {
                // simply return new struct value, it's the fastest option
                // and we don't have to bother with reseting source
                return new CancellationToken();
            }
        }

        public CancellationTokenRegistration Register(Action callback) {
            return Register(callback,false);
        }

        public CancellationTokenRegistration Register(Action callback,bool useSynchronizationContext) {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return Source.Register(callback,useSynchronizationContext);
        }

        public CancellationTokenRegistration Register(Action<object> callback,object state) {
            return Register(callback,state,false);
        }

        public CancellationTokenRegistration Register(Action<object> callback,object state,bool useSynchronizationContext) {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return Register(() => callback(state),useSynchronizationContext);
        }

        public void ThrowIfCancellationRequested() {
            if (source != null && source.IsCancellationRequested)
                throw new OperationCanceledException("");
        }

        public bool Equals(CancellationToken other) {
            return this.Source == other.Source;
        }

        public override bool Equals(object other) {
            return (other is CancellationToken) ? Equals((CancellationToken)other) : false;
        }

        public override int GetHashCode() {
            return Source.GetHashCode();
        }

        public static bool operator ==(CancellationToken left,CancellationToken right) {
            return left.Equals(right);
        }

        public static bool operator !=(CancellationToken left,CancellationToken right) {
            return !left.Equals(right);
        }

        public bool CanBeCanceled {
            get {
                return source != null;
            }
        }

        public bool IsCancellationRequested {
            get {
                return Source.IsCancellationRequested;
            }
        }

        public WaitHandle WaitHandle {
            get {
                return Source.WaitHandle;
            }
        }

        CancellationTokenSource Source {
            get {
                return source ?? CancellationTokenSource.NoneSource;
            }
        }
    }
	public class CancellationTokenSource : IDisposable
	{
		const int StateValid = 0;
		const int StateCanceled = 1 << 1;
		const int StateDisposed = 1 << 2;

		int state;
		int currId = int.MinValue;
		ConcurrentDictionary<CancellationTokenRegistration,Action> callbacks;
		CancellationTokenRegistration[] linkedTokens {
			get { return (CancellationTokenRegistration[])_linkedTokens; }
		}
		object _linkedTokens;

		ManualResetEvent handle;

		internal static readonly CancellationTokenSource NoneSource = new CancellationTokenSource();
		internal static readonly CancellationTokenSource CanceledSource = new CancellationTokenSource();

//#if NET_4_5
		static readonly TimerCallback timer_callback;
		object timer;
        Timer Timer
        { 
            get { return (Timer)timer; }
        }
//#endif

        static CancellationTokenSource() {
			CanceledSource.state = StateCanceled;

            //#if NET_4_5
            timer_callback = token => {
                var cts = (CancellationTokenSource)token;
                cts.CancelSafe();
            };
            //#endif
        }

		public CancellationTokenSource() {
			callbacks = new ConcurrentDictionary<CancellationTokenRegistration,Action>(new PowerThreadPool_Net20.Collections.Comparer.GenericEqualityComparer<CancellationTokenRegistration>());
			handle = new ManualResetEvent(false);
		}

        //#if NET_4_5
        public CancellationTokenSource(int millisecondsDelay)
            : this() {
            if (millisecondsDelay < -1)
                throw new ArgumentOutOfRangeException("millisecondsDelay");

            if (millisecondsDelay != Timeout.Infinite)
                timer = new Timer(timer_callback,this,millisecondsDelay,Timeout.Infinite);
        }

        public CancellationTokenSource(TimeSpan delay)
            : this(CheckTimeout(delay)) {
        }
        //#endif

        public CancellationToken Token {
			get {
				CheckDisposed();
				return new CancellationToken(this);
			}
		}

		public bool IsCancellationRequested {
			get {
				return (state & StateCanceled) != 0;
			}
		}

		internal WaitHandle WaitHandle {
			get {
				CheckDisposed();
				return handle;
			}
		}

		public void Cancel() {
			Cancel(false);
		}

		// If parameter is true we throw exception as soon as they appear otherwise we aggregate them
		public void Cancel(bool throwOnFirstException) {
			CheckDisposed();
			//#if !NOUSE_System_Core_Net20
			//			CancelAfter(1);
			//#else
			//			Cancellation(throwOnFirstException);
			//#endif
			CancelAfter(1);
		}

		//
		// Don't throw ObjectDisposedException if the callback
		// is called concurrently with a Dispose
		//
		void CancelSafe() {
			if (state == StateValid)
				Cancellation(true);
		}

		void Cancellation(bool throwOnFirstException) {
			if (Interlocked.CompareExchange(ref state,StateCanceled,StateValid) != StateValid)
				return;

			handle.Set();

			if (linkedTokens != null)
				UnregisterLinkedTokens();

			var cbs = callbacks;
			if (cbs == null)
				return;

			List<Exception> exceptions = null;

			try {
				Action cb;
				for (int id = currId; id != int.MinValue; id--) {
					if (!cbs.TryRemove(new CancellationTokenRegistration(id,this),out cb))
						continue;
					if (cb == null)
						continue;

					if (throwOnFirstException) {
						cb();
					}
					else {
						try {
							cb();
						}
						catch (Exception e) {
							if (exceptions == null)
								exceptions = new List<Exception>();

							exceptions.Add(e);
						}
					}
				}
			}
			finally {
				cbs.Clear();
			}

			if (exceptions != null) {
				System.Text.StringBuilder finalMessage = new System.Text.StringBuilder(base.ToString());
				finalMessage.Append(exceptions.Count+ " errors occurred:");
				int currentIndex = -1;
				foreach (Exception e in exceptions) {
					finalMessage.Append(Environment.NewLine);
					finalMessage.Append(" --> (Inner exception ");
					finalMessage.Append(++currentIndex);
					finalMessage.Append(") ");
					finalMessage.Append(e.ToString());
					finalMessage.Append(Environment.NewLine);
				}
				// finalMessage.ToString();
				throw new OperationCanceledException(finalMessage.ToString());
			}
		}

//#if NET_4_5
		public void CancelAfter (TimeSpan delay)
		{
			CancelAfter (CheckTimeout (delay));
		}

		public void CancelAfter (int millisecondsDelay)
		{
			if (millisecondsDelay < -1)
				throw new ArgumentOutOfRangeException ("millisecondsDelay");

			CheckDisposed ();

			if (IsCancellationRequested || millisecondsDelay == Timeout.Infinite)
				return;

			if (timer == null) {
				// Have to be carefull not to create secondary background timer
				var t = new Timer (timer_callback, this, Timeout.Infinite, Timeout.Infinite);
				if (Interlocked.CompareExchange (ref timer, t, null) != null)
					t.Dispose ();
			}

			Timer.Change (millisecondsDelay, Timeout.Infinite);
		}
//#endif

		public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1,CancellationToken token2) {
			return CreateLinkedTokenSource(new[] { token1,token2 });
		}

		public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens) {
			if (tokens == null)
				throw new ArgumentNullException("tokens");

			if (tokens.Length == 0)
				throw new ArgumentException("Empty tokens array");

			CancellationTokenSource src = new CancellationTokenSource();
			Action action = src.CancelSafe;
			var registrations = new List<CancellationTokenRegistration>(tokens.Length);

			foreach (CancellationToken token in tokens) {
				if (token.CanBeCanceled)
					registrations.Add(token.Register(action));
			}
			src._linkedTokens = registrations.ToArray();

			return src;
		}

		static int CheckTimeout(TimeSpan delay) {
			try {
				return checked((int)delay.TotalMilliseconds);
			}
			catch (OverflowException) {
				throw new ArgumentOutOfRangeException("delay");
			}
		}

		void CheckDisposed() {
			if ((state & StateDisposed) != 0)
				throw new ObjectDisposedException(GetType().Name);
		}

		public void Dispose() {
			Dispose(true);
		}

//#if NET_4_5
		protected virtual
//#endif
		void Dispose(bool disposing) {
			if (disposing && (state & StateDisposed) == 0) {
				if (Interlocked.CompareExchange(ref state,StateDisposed,StateValid) == StateValid) {
					UnregisterLinkedTokens();
					callbacks = null;
				}
				else {
					if (handle != null)
						handle.WaitOne();

					state |= StateDisposed;
					Thread.MemoryBarrier();
				}
//#if NET_4_5
				if (timer != null)
					Timer.Dispose ();
//#endif

				((IDisposable)handle).Dispose();
				handle = null;
			}
		}

		void UnregisterLinkedTokens() {
			var registrations = Interlocked.Exchange(ref _linkedTokens,null);
			if (registrations == null)
				return;
			foreach (var linked in (CancellationTokenRegistration[])registrations)
				linked.Dispose();
		}

		internal CancellationTokenRegistration Register(Action callback,bool useSynchronizationContext) {
			CheckDisposed();

			var tokenReg = new CancellationTokenRegistration(Interlocked.Increment(ref currId),this);

			/* If the source is already canceled we execute the callback immediately
			 * if not, we try to add it to the queue and if it is currently being processed
			 * we try to execute it back ourselves to be sure the callback is ran
			 */
			if (IsCancellationRequested)
				callback();
			else {
				callbacks.TryAdd(tokenReg,callback);
				if (IsCancellationRequested && callbacks.TryRemove(tokenReg,out callback))
					callback();
			}

			return tokenReg;
		}

		internal void RemoveCallback(CancellationTokenRegistration reg) {
			// Ignore call if the source has been disposed
			if ((state & StateDisposed) != 0)
				return;
			Action dummy;
			var cbs = callbacks;
			if (cbs != null)
				cbs.TryRemove(reg,out dummy);
		}
	}

	public struct CancellationTokenRegistration : IDisposable, IEquatable<CancellationTokenRegistration>
	{
		readonly int id;
		readonly CancellationTokenSource source;

		internal CancellationTokenRegistration(int id,CancellationTokenSource source) {
			this.id = id;
			this.source = source;
		}

		#region IDisposable implementation
		public void Dispose() {
			if (source != null)
				source.RemoveCallback(this);
		}
		#endregion

		#region IEquatable<CancellationTokenRegistration> implementation
		public bool Equals(CancellationTokenRegistration other) {
			return id == other.id && source == other.source;
		}

		public static bool operator ==(CancellationTokenRegistration left,CancellationTokenRegistration right) {
			return left.Equals(right);
		}

		public static bool operator !=(CancellationTokenRegistration left,CancellationTokenRegistration right) {
			return !left.Equals(right);
		}
		#endregion

		public override int GetHashCode() {
			return id.GetHashCode() ^ (source == null ? 0 : source.GetHashCode());
		}

		public override bool Equals(object obj) {
			return (obj is CancellationTokenRegistration) && Equals((CancellationTokenRegistration)obj);
		}
	}
	/*
    /// <summary>
    /// .NET 2.0兼容的取消令牌
    /// .NET 2.0 compatible cancellation token
    /// </summary>
    public class CancellationToken
    {
        private bool _isCancelled = false;
        private readonly object _lock = new object();
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CancellationToken()
        {
        }
        
        /// <summary>
        /// 是否已取消
        /// Whether cancelled
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock (_lock)
                {
                    return _isCancelled;
                }
            }
        }
        
        /// <summary>
        /// 取消操作
        /// Cancel operation
        /// </summary>
        internal void Cancel()
        {
            lock (_lock)
            {
                _isCancelled = true;
            }
        }
        
        /// <summary>
        /// 重置令牌
        /// Reset token
        /// </summary>
        internal void Reset()
        {
            lock (_lock)
            {
                _isCancelled = false;
            }
        }
        
        /// <summary>
        /// 抛出取消异常（如果已取消）
        /// Throw cancellation exception if cancelled
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
    }
    
    /// <summary>
    /// 取消令牌源
    /// Cancellation token source
    /// </summary>
    public class CancellationTokenSource
    {
        private CancellationToken _token;
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CancellationTokenSource()
        {
            _token = new CancellationToken();
        }
        
        /// <summary>
        /// 获取令牌
        /// Get token
        /// </summary>
        public CancellationToken Token
        {
            get { return _token; }
        }
        
        /// <summary>
        /// 取消
        /// Cancel
        /// </summary>
        public void Cancel()
        {
            _token.Cancel();
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // .NET 2.0中没有复杂的资源需要释放
        }
    }

    */
}