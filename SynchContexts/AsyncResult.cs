

using System;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Provides basic implementation of the IAsyncResult interface.
    /// 提供 IAsyncResult 接口的基本实现。
    /// </summary>
    public class AsyncResult : IAsyncResult, IDisposable
    {
        #region AsyncResult Members

        #region Fields

        /// <summary>
        /// The owner of this AsyncResult object.
        /// 此 AsyncResult 对象的所有者。
        /// </summary>
        private readonly object owner;

        /// <summary>
        /// The callback to be invoked when the operation completes.
        /// 操作完成时要调用的回调。
        /// </summary>
        private readonly AsyncCallback callback;

        /// <summary>
        /// For signaling when the operation has completed.
        /// 用于在操作完成时发出信号。
        /// </summary>
        private readonly ManualResetEvent waitHandle = new ManualResetEvent(false);

        /// <summary>
        /// A value indicating whether the operation completed synchronously.
        /// 指示操作是否同步完成的值。
        /// </summary>
        private bool completedSynchronously;

        /// <summary>
        /// A value indicating whether the operation has completed.
        /// 指示操作是否已完成的值。
        /// </summary>
        private bool isCompleted;

        /// <summary>
        /// The ID of the thread this AsyncResult object originated on.
        /// 此 AsyncResult 对象起源的线程 ID。
        /// </summary>
        private readonly int threadId;

        /// <summary>
        /// Indicates whether this object has been disposed.
        /// 指示此对象是否已被释放。
        /// </summary>
        private bool disposed;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the AsyncResult object with the
        /// specified owner of the AsyncResult object, the optional callback
        /// delegate, and optional state object.
        /// 使用指定的 AsyncResult 对象所有者、可选的回调解托和可选的状态对象初始化 AsyncResult 对象的新实例。
        /// </summary>
        /// <param name="owner">
        /// The owner of the AsyncResult object.
        /// AsyncResult 对象的所有者。
        /// </param>
        /// <param name="callback">
        /// An optional asynchronous callback, to be called when the
        /// operation is complete.
        /// 可选的异步回调，在操作完成时调用。
        /// </param>
        /// <param name="state">
        /// A user-provided object that distinguishes this particular
        /// asynchronous request from other requests.
        /// 用户提供的对象，用于区分此特定异步请求与其他请求。
        /// </param>
        public AsyncResult(object owner,AsyncCallback callback,object state) {
            Owner = owner;
            this.callback = callback;
            AsyncState = state;

            // Get the current thread ID. This will be used later to determine
            // if the operation completed synchronously.
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Signals that the operation has completed.
        /// 信号通知操作已完成。
        /// </summary>
        public void Signal() {
            IsCompleted = true;

            CompletedSynchronously = threadId == Thread.CurrentThread.ManagedThreadId;

            waitHandle.Set();

            if (callback != null) callback(this);
        }

        /// <summary>
        /// Releases all resources used by the AsyncResult.
        /// 释放 AsyncResult 使用的所有资源。
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the AsyncResult and optionally releases the managed resources.
        /// 释放 AsyncResult 使用的非托管资源，并可选地释放托管资源。
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources; false to release only unmanaged resources.
        /// true 表示释放托管和非托管资源；false 表示仅释放非托管资源。
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;

            if (disposing) {
                if (waitHandle != null) {
                    waitHandle.Close();
                }
            }

            disposed = true;
        }

        /// <summary>
        /// Finalizer for AsyncResult.
        /// AsyncResult 的终结器。
        /// </summary>
        ~AsyncResult() {
            Dispose(false);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the owner of this AsyncResult object.
        /// </summary>
        public object Owner { get; }

        #endregion

        #endregion

        #region IAsyncResult Members

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle => waitHandle;

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted { get; private set; }

        #endregion
    }
}