

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using PowerThreadPool_Net20.Collections;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Represents an asynchronous queue of delegates.
    /// 表示一个异步委托队列。
    /// </summary>
    public partial class DelegateQueue : SynchronizationContext, IComponent, ISynchronizeInvoke
    {
        #region IDisposable Members

        /// <summary>
        /// Disposes of the DelegateQueue.
        /// 释放 DelegateQueue 的资源。
        /// </summary>
        public void Dispose()
        {
            #region Guards

            if (disposed) return;

            #endregion

            Dispose(true);

            OnDisposed(EventArgs.Empty);
        }

        #endregion

        #region DelegateQueue Members

        #region Fields

        /// <summary>
        /// The thread for processing delegates.
        /// 用于处理委托的线程。
        /// </summary>
        private Thread delegateThread;

        /// <summary>
        /// The deque for holding delegates.
        /// 用于保存委托的双端队列。
        /// </summary>
        private readonly Deque<DelegateQueueAsyncResult> delegateDeque = new Deque<DelegateQueueAsyncResult>();

        /// <summary>
        /// The object to use for locking.
        /// 用于锁定的对象。
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// The synchronization context in which this DelegateQueue was created.
        /// 创建此 DelegateQueue 的同步上下文。
        /// </summary>
        private readonly SynchronizationContext context;

        /// <summary>
        /// Indicates whether the delegate queue has been disposed.
        /// 指示委托队列是否已被释放。
        /// </summary>
        private volatile bool disposed;

        /// <summary>
        /// Thread ID counter for all DelegateQueues.
        /// 所有 DelegateQueue 的线程 ID 计数器。
        /// </summary>
        private static volatile uint threadID;

        /// <summary>
        /// Indicates whether to use a dedicated thread for processing delegates.
        /// 指示是否使用专用线程处理委托。
        /// </summary>
        private readonly bool useDedicatedThread;

        #endregion

        #region Events

        /// <summary>
        /// Occurs after a method has been invoked as a result of a call to
        /// BeginInvoke or BeginInvokePriority methods.
        /// 在调用 BeginInvoke 或 BeginInvokePriority 方法后发生。
        /// </summary>
        public event EventHandler<InvokeCompletedEventArgs> InvokeCompleted;

        /// <summary>
        /// Occurs after a method has been invoked as a result of a call to
        /// Post and PostPriority methods.
        /// 在调用 Post 或 PostPriority 方法后发生。
        /// </summary>
        public event EventHandler<PostCompletedEventArgs> PostCompleted;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class.
        /// 初始化 DelegateQueue 类的新实例。
        /// </summary>
        public DelegateQueue()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class with the specified thread mode.
        /// 使用指定的线程模式初始化 DelegateQueue 类的新实例。
        /// </summary>
        /// <param name="useDedicatedThread">
        /// true to use a dedicated thread for processing delegates; false to process delegates manually via DoActions().
        /// true 表示使用专用线程处理委托；false 表示通过 DoActions() 手动处理委托。
        /// </param>
        public DelegateQueue(bool useDedicatedThread)
        {
            this.useDedicatedThread = useDedicatedThread;

            InitializeDelegateQueue();

            if (Current == null)
                context = new SynchronizationContext();
            else
                context = Current;
        }

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class with the specified IContainer object.
        /// 使用指定的 IContainer 对象初始化 DelegateQueue 类的新实例。
        /// </summary>
        /// <param name="container">
        /// The IContainer to which the DelegateQueue will add itself.
        /// DelegateQueue 将添加到的 IContainer。
        /// </param>
        public DelegateQueue(IContainer container)
            : this(container, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DelegateQueue class with the specified IContainer object and thread mode.
        /// 使用指定的 IContainer 对象和线程模式初始化 DelegateQueue 类的新实例。
        /// </summary>
        /// <param name="container">
        /// The IContainer to which the DelegateQueue will add itself.
        /// DelegateQueue 将添加到的 IContainer。
        /// </param>
        /// <param name="useDedicatedThread">
        /// true to use a dedicated thread for processing delegates; false to process delegates manually via DoActions().
        /// true 表示使用专用线程处理委托；false 表示通过 DoActions() 手动处理委托。
        /// </param>
        public DelegateQueue(IContainer container, bool useDedicatedThread)
        {
            
            ///
            /// Required for Windows.Forms Class Composition Designer support
            /// Windows.Forms 类组合设计器支持所需
            ///
            container.Add(this);

            this.useDedicatedThread = useDedicatedThread;

            InitializeDelegateQueue();
        }

        /// <summary>
        /// Finalizer for DelegateQueue.
        /// DelegateQueue 的终结器。
        /// </summary>
        ~DelegateQueue()
        {
            Dispose(false);
        }

        // Initializes the DelegateQueue.
        private void InitializeDelegateQueue()
        {
            if (useDedicatedThread)
            {
                delegateThread = new Thread(DelegateProcedure);

                lock (lockObject)
                {
                    threadID++;

                    delegateThread.Name = "Delegate Queue Thread: " + threadID;

                    delegateThread.Start();

                    Debug.WriteLine(delegateThread.Name + " Started.");

                    Monitor.Wait(lockObject);
                }
            }
            else
            {
                SetSynchronizationContext(this);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Processes delegates in the queue manually. This method should be called periodically when useDedicatedThread is false.
        /// 手动处理队列中的委托。当 useDedicatedThread 为 false 时，应定期调用此方法。
        /// </summary>
        /// <param name="doLoop">
        /// Whether to loop continuously until disposed.
        /// 是否持续循环直到被释放。
        /// </param>
        public void DoActions(bool doLoop = false)
        {
            if (useDedicatedThread)
            {
                throw new InvalidOperationException("DoActions() can only be called when useDedicatedThread is false.");
            }

            do
            {
                ProcessPendingDelegates();
            } while (doLoop && !disposed);
        }

        /// <summary>
        /// Processes all pending delegates in the queue.
        /// 处理队列中所有待处理的委托。
        /// </summary>
        private void ProcessPendingDelegates()
        {
            lock (lockObject)
            {
                while (delegateDeque.Count > 0)
                {
                    DelegateQueueAsyncResult result = delegateDeque.PopFront();
                    Monitor.Exit(lockObject);

                    try
                    {
                        result.Invoke();

                        if (result.NotificationType == NotificationType.BeginInvokeCompleted)
                        {
                            var e = new InvokeCompletedEventArgs(
                                result.Method,
                                result.GetArgs(),
                                result.ReturnValue,
                                result.Error);

                            OnInvokeCompleted(e);
                        }
                        else if (result.NotificationType == NotificationType.PostCompleted)
                        {
                            var args = result.GetArgs();

                            Debug.Assert(args.Length == 1);
                            Debug.Assert(result.Method is SendOrPostCallback);

                            var e = new PostCompletedEventArgs(
                                (SendOrPostCallback)result.Method,
                                result.Error,
                                args[0]);

                            OnPostCompleted(e);
                        }
                    }
                    finally
                    {
                        Monitor.Enter(lockObject);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                lock (lockObject)
                {
                    disposed = true;

                    Monitor.Pulse(lockObject);

                    GC.SuppressFinalize(this);
                }
        }

        /// <summary>
        ///     Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="method">
        ///     A Delegate to a method that takes parameters of the same number and
        ///     type that are contained in args.
        /// </param>
        /// <param name="args">
        ///     An array of type Object to pass as arguments to the given method.
        /// </param>
        /// <returns>
        ///     An IAsyncResult interface that represents the asynchronous operation
        ///     started by calling this method.
        /// </returns>
        /// <remarks>
        ///     The delegate is placed at the beginning of the queue. Its invocation
        ///     takes priority over delegates already in the queue.
        /// </remarks>
        public IAsyncResult BeginInvokePriority(Delegate method, params object[] args)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (method == null) throw new ArgumentNullException();

            #endregion

            DelegateQueueAsyncResult result;

            // If BeginInvokePriority was called from a different thread than the one
            // in which the DelegateQueue is running.
            if (InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.BeginInvokeCompleted);

                lock (lockObject)
                {
                    // Put the method at the front of the queue.
                    delegateDeque.PushFront(result);

                    Monitor.Pulse(lockObject);
                }
            }
            // Else BeginInvokePriority was called from the same thread in which the 
            // DelegateQueue is running.
            else
            {
                result = new DelegateQueueAsyncResult(this, method, args, true, NotificationType.None);

                // The method is invoked here instead of placing it in the 
                // queue. The reason for this is that if EndInvoke is called 
                // from the same thread in which the DelegateQueue is running and
                // the method has not been invoked, deadlock will occur. 
                result.Invoke();
            }

            return result;
        }

        /// <summary>
        ///     Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="method">
        ///     A Delegate to a method that takes parameters of the same number and
        ///     type that are contained in args.
        /// </param>
        /// <param name="args">
        ///     An array of type Object to pass as arguments to the given method.
        /// </param>
        /// <returns>
        ///     An IAsyncResult interface that represents the asynchronous operation
        ///     started by calling this method.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The delegate is placed at the beginning of the queue. Its invocation
        ///         takes priority over delegates already in the queue.
        ///     </para>
        ///     <para>
        ///         Unlike BeginInvoke, this method operates synchronously, that is, it
        ///         waits until the process completes before returning. Exceptions raised
        ///         during the call are propagated back to the caller.
        ///     </para>
        /// </remarks>
        public object InvokePriority(Delegate method, params object[] args)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (method == null) throw new ArgumentNullException();

            #endregion

            object returnValue = null;

            // If InvokePriority was called from a different thread than the one
            // in which the DelegateQueue is running.
            if (InvokeRequired)
            {
                var result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                lock (lockObject)
                {
                    // Put the method at the back of the queue.
                    delegateDeque.PushFront(result);

                    Monitor.Pulse(lockObject);
                }

                // Wait for the result of the method invocation.
                returnValue = EndInvoke(result);
            }
            // Else InvokePriority was called from the same thread in which the 
            // DelegateQueue is running.
            else
            {
                // Invoke the method here rather than placing it in the queue.
                returnValue = method.DynamicInvoke(args);
            }

            return returnValue;
        }

        /// <summary>
        ///     Executes the delegate on the main thread that this object executes on.
        /// </summary>
        /// <param name="callback">
        ///     An optional asynchronous callback, to be called when the method is invoked.
        /// </param>
        /// <param name="state">
        ///     A user-provided object that distinguishes this particular asynchronous invoke request from other requests.
        /// </param>
        /// <param name="method">
        ///     A Delegate to a method that takes parameters of the same number and
        ///     type that are contained in args.
        /// </param>
        /// <param name="args">
        ///     An array of type Object to pass as arguments to the given method.
        /// </param>
        /// <returns>
        ///     An IAsyncResult interface that represents the asynchronous operation
        ///     started by calling this method.
        /// </returns>
        public IAsyncResult BeginInvoke(AsyncCallback callback, object state, Delegate method, params object[] args)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (method == null) throw new ArgumentNullException();

            #endregion

            DelegateQueueAsyncResult result;

            if (InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, callback, state, method, args, false,
                    NotificationType.BeginInvokeCompleted);

                lock (lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }
            }
            else
            {
                result = new DelegateQueueAsyncResult(this, callback, state, method, args, false,
                    NotificationType.None);

                result.Invoke();
            }

            return result;
        }

        /// <summary>
        ///     Dispatches an asynchronous message to this synchronization context.
        /// </summary>
        /// <param name="d">
        ///     The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        ///     The object passed to the delegate.
        /// </param>
        /// <remarks>
        ///     The Post method starts an asynchronous request to post a message.
        /// </remarks>
        public void PostPriority(SendOrPostCallback d, object state)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (d == null) throw new ArgumentNullException();

            #endregion

            lock (lockObject)
            {
                var result =
                    new DelegateQueueAsyncResult(this, d, new[] {state}, false, NotificationType.PostCompleted);

                // Put the method at the front of the queue.
                delegateDeque.PushFront(result);

                Monitor.Pulse(lockObject);
            }
        }

        /// <summary>
        ///     Dispatches an synchronous message to this synchronization context.
        /// </summary>
        /// <param name="d">
        ///     The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        ///     The object passed to the delegate.
        /// </param>
        public void SendPriority(SendOrPostCallback d, object state)
        {
            InvokePriority(d, state);
        }

        // Processes and invokes delegates.
        private void DelegateProcedure()
        {
            lock (lockObject)
            {
                Monitor.Pulse(lockObject);
            }

            SetSynchronizationContext(this);

            while (true)
            {
                lock (lockObject)
                {
                    if (disposed) break;

                    if (delegateDeque.Count == 0)
                    {
                        Monitor.Wait(lockObject);

                        if (disposed) break;
                    }
                }

                ProcessPendingDelegates();
            }

            Debug.WriteLine(delegateThread.Name + " Finished");
        }

        // Raises the InvokeCompleted event.
        protected virtual void OnInvokeCompleted(InvokeCompletedEventArgs e)
        {
            var handler = InvokeCompleted;

            if (handler != null)
                context.Post(delegate { handler(this, e); }, null);
        }

        // Raises the PostCompleted event.
        protected virtual void OnPostCompleted(PostCompletedEventArgs e)
        {
            var handler = PostCompleted;

            if (handler != null)
                context.Post(delegate { handler(this, e); }, null);
        }

        // Raises the Disposed event.
        protected virtual void OnDisposed(EventArgs e)
        {
            var handler = Disposed;

            if (handler != null)
                context.Post(delegate { handler(this, e); }, null);
        }

        #endregion

        #endregion

        #region SynchronizationContext Overrides

        /// <summary>
        ///     Dispatches a synchronous message to this synchronization context.
        /// </summary>
        /// <param name="d">
        ///     The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        ///     The object passed to the delegate.
        /// </param>
        /// <remarks>
        ///     The Send method starts an synchronous request to send a message.
        /// </remarks>
        public override void Send(SendOrPostCallback d, object state)
        {
            Invoke(d, state);
        }

        /// <summary>
        ///     Dispatches an asynchronous message to this synchronization context.
        /// </summary>
        /// <param name="d">
        ///     The SendOrPostCallback delegate to call.
        /// </param>
        /// <param name="state">
        ///     The object passed to the delegate.
        /// </param>
        /// <remarks>
        ///     The Post method starts an asynchronous request to post a message.
        /// </remarks>
        public override void Post(SendOrPostCallback d, object state)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (d == null) throw new ArgumentNullException();

            #endregion

            lock (lockObject)
            {
                delegateDeque.PushBack(new DelegateQueueAsyncResult(this, d, new[] {state}, false,
                    NotificationType.PostCompleted));

                Monitor.Pulse(lockObject);
            }
        }

        #endregion

        #region IComponent Members

        /// <summary>
        ///     Represents the method that handles the Disposed delegate of a DelegateQueue.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        ///     Gets or sets the ISite associated with the DelegateQueue.
        /// </summary>
        public ISite Site { get; set; } = null;

        #endregion

        #region ISynchronizeInvoke Members

        /// <summary>
        ///     Executes the delegate on the main thread that this DelegateQueue executes on.
        /// </summary>
        /// <param name="method">
        ///     A Delegate to a method that takes parameters of the same number and type that
        ///     are contained in args.
        /// </param>
        /// <param name="args">
        ///     An array of type Object to pass as arguments to the given method. This can be
        ///     a null reference (Nothing in Visual Basic) if no arguments are needed.
        /// </param>
        /// <returns>
        ///     An IAsyncResult interface that represents the asynchronous operation started
        ///     by calling this method.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The delegate is called asynchronously, and this method returns immediately.
        ///         You can call this method from any thread. If you need the return value from a process
        ///         started with this method, call EndInvoke to get the value.
        ///     </para>
        ///     <para>If you need to call the delegate synchronously, use the Invoke method instead.</para>
        /// </remarks>
        public IAsyncResult BeginInvoke(Delegate method, params object[] args)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (method == null) throw new ArgumentNullException();

            #endregion

            DelegateQueueAsyncResult result;

            if (InvokeRequired)
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.BeginInvokeCompleted);

                lock (lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }
            }
            else
            {
                result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                result.Invoke();
            }

            return result;
        }

        /// <summary>
        ///     Waits until the process started by calling BeginInvoke completes, and then returns
        ///     the value generated by the process.
        /// </summary>
        /// <param name="result">
        ///     An IAsyncResult interface that represents the asynchronous operation started
        ///     by calling BeginInvoke.
        /// </param>
        /// <returns>
        ///     An Object that represents the return value generated by the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///     This method gets the return value of the asynchronous operation represented by the
        ///     IAsyncResult passed by this interface. If the asynchronous operation has not completed, this method will wait until
        ///     the result is available.
        /// </remarks>
        public object EndInvoke(IAsyncResult result)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (!(result is DelegateQueueAsyncResult))
                throw new ArgumentException();
            if (((DelegateQueueAsyncResult) result).Owner != this) throw new ArgumentException();

            #endregion

            result.AsyncWaitHandle.WaitOne();

            var r = (DelegateQueueAsyncResult) result;

            if (r.Error != null) throw r.Error;

            return r.ReturnValue;
        }

        /// <summary>
        ///     Executes the delegate on the main thread that this DelegateQueue executes on.
        /// </summary>
        /// <param name="method">
        ///     A Delegate that contains a method to call, in the context of the thread for the DelegateQueue.
        /// </param>
        /// <param name="args">
        ///     An array of type Object that represents the arguments to pass to the given method.
        /// </param>
        /// <returns>
        ///     An Object that represents the return value from the delegate being invoked, or a
        ///     null reference (Nothing in Visual Basic) if the delegate has no return value.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Unlike BeginInvoke, this method operates synchronously, that is, it waits until
        ///         the process completes before returning. Exceptions raised during the call are propagated
        ///         back to the caller.
        ///     </para>
        ///     <para>
        ///         Use this method when calling a method from a different thread to marshal the call
        ///         to the proper thread.
        ///     </para>
        /// </remarks>
        public object Invoke(Delegate method, params object[] args)
        {
            #region Require

            if (disposed)
                throw new ObjectDisposedException("DelegateQueue");
            if (method == null) throw new ArgumentNullException();

            #endregion

            object returnValue = null;

            if (InvokeRequired)
            {
                var result = new DelegateQueueAsyncResult(this, method, args, false, NotificationType.None);

                lock (lockObject)
                {
                    delegateDeque.PushBack(result);

                    Monitor.Pulse(lockObject);
                }

                returnValue = EndInvoke(result);
            }
            else
            {
                // Invoke the method here rather than placing it in the queue.
                returnValue = method.DynamicInvoke(args);
            }

            return returnValue;
        }

        /// <summary>
        /// Gets a value indicating whether the caller must call Invoke.
        /// 获取一个值，指示调用者是否必须调用 Invoke。
        /// </summary>
        /// <value>
        /// <b>true</b> if the caller must call Invoke; otherwise, <b>false</b>.
        /// 如果调用者必须调用 Invoke，则为 <b>true</b>；否则为 <b>false</b>。
        /// </value>
        /// <remarks>
        /// This property determines whether the caller must call Invoke when making
        /// method calls to this DelegateQueue. If you are calling a method from a different
        /// thread, you must use the Invoke method to marshal the call to the proper thread.
        /// 此属性确定调用者在对此 DelegateQueue 进行方法调用时是否必须调用 Invoke。
        /// 如果您从不同的线程调用方法，则必须使用 Invoke 方法将调用封送到适当的线程。
        /// </remarks>
        public bool InvokeRequired => useDedicatedThread && Thread.CurrentThread.ManagedThreadId != delegateThread.ManagedThreadId;

        #endregion
    }
}