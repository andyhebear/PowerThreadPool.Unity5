using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// STA (Single Threaded Apartment) synchronization context.
    /// STA（单线程单元）同步上下文。
    /// </summary>
    public class StaSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>
        /// The queue for storing callback items.
        /// 用于存储回调项的队列。
        /// </summary>
        private BlockingQueue<SendOrPostCallbackItem> mQueue;
        
        /// <summary>
        /// The STA thread for executing callbacks.
        /// 用于执行回调的 STA 线程。
        /// </summary>
        private StaThread mStaThread;
        
        /// <summary>
        /// The old synchronization context before this one was set.
        /// 设置此同步上下文之前的旧同步上下文。
        /// </summary>
        private SynchronizationContext oldSync;

        /// <summary>
        /// Initializes a new instance of the StaSynchronizationContext class.
        /// 初始化 StaSynchronizationContext 类的新实例。
        /// </summary>
        public StaSynchronizationContext()
            : base() {
            mQueue = new BlockingQueue<SendOrPostCallbackItem>();
            mStaThread = new StaThread(mQueue,this);
            mStaThread.Start();
            oldSync = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
        }

        /// <summary>
        /// Dispatches a synchronous message to this synchronization context.
        /// 向此同步上下文调度同步消息。
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// 要调用的 SendOrPostCallback 委托。
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// 传递给委托的对象。
        /// </param>
        public override void Send(SendOrPostCallback d,object state) {
            // create an item for execution
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d,state,
                                                                     ExecutionType.Send);
            // queue the item
            mQueue.Enqueue(item);
            // wait for the item execution to end
            item.ExecutionCompleteWaitHandle.WaitOne();

            // if there was an exception, throw it on the caller thread, not the
            // sta thread.
            if (item.ExecutedWithException)
                throw item.Exception;
        }

        /// <summary>
        /// Dispatches an asynchronous message to this synchronization context.
        /// 向此同步上下文调度异步消息。
        /// </summary>
        /// <param name="d">
        /// The SendOrPostCallback delegate to call.
        /// 要调用的 SendOrPostCallback 委托。
        /// </param>
        /// <param name="state">
        /// The object passed to the delegate.
        /// 传递给委托的对象。
        /// </param>
        public override void Post(SendOrPostCallback d,object state) {
            // queue the item and don't wait for its execution. This is risky because
            // an unhandled exception will terminate the STA thread. Use with caution.
            SendOrPostCallbackItem item = new SendOrPostCallbackItem(d,state,
                                                                     ExecutionType.Post);
            mQueue.Enqueue(item);
        }

        /// <summary>
        /// Releases all resources used by the StaSynchronizationContext.
        /// 释放 StaSynchronizationContext 使用的所有资源。
        /// </summary>
        public void Dispose() {
            if (mStaThread != null) {
                mStaThread.Stop();
            }
            SynchronizationContext.SetSynchronizationContext(oldSync);
        }

        public override SynchronizationContext CreateCopy() {
            return this;
        }

        /// <summary>
        /// STA thread for executing callbacks in a single-threaded apartment.
        /// 用于在单线程单元中执行回调的 STA 线程。
        /// </summary>
        internal class StaThread
        {
            /// <summary>
            /// The STA thread instance.
            /// STA 线程实例。
            /// </summary>
            private Thread mStaThread;
            
            /// <summary>
            /// The queue consumer for dequeuing callback items.
            /// 用于出队回调项的队列消费者。
            /// </summary>
            private IQueueReader<SendOrPostCallbackItem> mQueueConsumer;
            
            /// <summary>
            /// The synchronization context for this STA thread.
            /// 此 STA 线程的同步上下文。
            /// </summary>
            private readonly SynchronizationContext syncContext;

            /// <summary>
            /// Event to signal the thread to stop.
            /// 用于通知线程停止的事件。
            /// </summary>
            private ManualResetEvent mStopEvent = new ManualResetEvent(false);


            internal StaThread(IQueueReader<SendOrPostCallbackItem> reader,SynchronizationContext syncContext) {
                mQueueConsumer = reader;
                this.syncContext = syncContext;
                mStaThread = new Thread(Run);
                mStaThread.Name = "STA Worker Thread";
                mStaThread.SetApartmentState(ApartmentState.STA);
            }

            internal void Start() {
                mStaThread.Start();
            }


            internal void Join() {
                mStaThread.Join();
            }

            private void Run() {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                while (true) {
                    bool stop = mStopEvent.WaitOne(0);
                    if (stop) {
                        mQueueConsumer.Dispose();
                        break;
                    }

                    SendOrPostCallbackItem workItem = mQueueConsumer.Dequeue();
                    if (workItem != null)
                        workItem.Execute();
                }
            }

            internal void Stop() {
                mStopEvent.Set();
                mQueueConsumer.ReleaseReader();
            }
        }
        internal enum ExecutionType
        {
            Post,
            Send
        }

        internal class SendOrPostCallbackItem
        {
            object mState;
            private ExecutionType mExeType;
            SendOrPostCallback mMethod;
            ManualResetEvent mAsyncWaitHandle = new ManualResetEvent(false);
            Exception mException = null;

            internal SendOrPostCallbackItem(SendOrPostCallback callback,
               object state,ExecutionType type) {
                mMethod = callback;
                mState = state;
                mExeType = type;
            }

            internal Exception Exception {
                get { return mException; }
            }

            internal bool ExecutedWithException {
                get { return mException != null; }
            }

            // this code must run ont the STA thread
            internal void Execute() {
                if (mExeType == ExecutionType.Send)
                    Send();
                else
                    Post();
            }

            // calling thread will block until mAsyncWaitHandle is set
            internal void Send() {
                try {
                    // call the thread
                    mMethod(mState);
                }
                catch (Exception e) {
                    mException = e;
                }
                finally {
                    mAsyncWaitHandle.Set();
                }
            }

            /// <summary />
            /// Unhandled exceptions will terminate the STA thread
            /// </summary />
            internal void Post() {
                mMethod(mState);
            }

            internal WaitHandle ExecutionCompleteWaitHandle {
                get { return mAsyncWaitHandle; }
            }
        }
        internal interface IQueueReader<T> : IDisposable
        {
            T Dequeue();
            void ReleaseReader();
        }

        internal interface IQueueWriter<T> : IDisposable
        {
            void Enqueue(T data);
        }


        internal class BlockingQueue<T> : IQueueReader<T>,
                                             IQueueWriter<T>, IDisposable
        {
            // use a .NET queue to store the data
            private Queue<T> mQueue = new Queue<T>();
            // create a semaphore that contains the items in the queue as resources.
            // initialize the semaphore to zero available resources (empty queue).
            private Semaphore mSemaphore = new Semaphore(0,int.MaxValue);
            // a event that gets triggered when the reader thread is exiting
            private ManualResetEvent mKillThread = new ManualResetEvent(false);
            // wait handles that are used to unblock a Dequeue operation.
            // Either when there is an item in the queue
            // or when the reader thread is exiting.
            private WaitHandle[] mWaitHandles;

            public BlockingQueue() {
                mWaitHandles = new WaitHandle[2] { mSemaphore,mKillThread };
            }
            public void Enqueue(T data) {
                lock (mQueue) mQueue.Enqueue(data);
                // add an available resource to the semaphore,
                // because we just put an item
                // into the queue.
                mSemaphore.Release();
            }

            public T Dequeue() {
                // wait until there is an item in the queue
                WaitHandle.WaitAny(mWaitHandles);
                lock (mQueue) {
                    if (mQueue.Count > 0)
                        return mQueue.Dequeue();
                }
                return default(T);
            }

            public void ReleaseReader() {
                mKillThread.Set();
            }


            void IDisposable.Dispose() {
                if (mSemaphore != null) {
                    mSemaphore.Close();
                    mSemaphore = null;
                }
                if (mKillThread != null) {
                    mKillThread.Close();
                    mKillThread = null;
                }
                if (mQueue != null) {
                    mQueue.Clear();
                }
            }
        }
    }
}
