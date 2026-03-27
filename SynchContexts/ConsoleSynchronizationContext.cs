using PowerThreadPool_Net20.Collections;
using PowerThreadPool_Net20.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// 当前线程（包括主线程）同步上下文,如果当前线程使用该同步上下文，必须在该线程的循环中执行DoActions();
    /// Current thread (including main thread) synchronization context. If the current thread uses this synchronization context, DoActions() must be executed in the thread's loop.
    /// </summary>
    public class ConsoleSynchronizationContext : SynchronizationContext
    {
        /// <summary>
        /// The queue for storing actions to be executed.
        /// 用于存储要执行的操作的队列。
        /// </summary>
        private readonly ConcurrentQueue<Action> _queue
           = new ConcurrentQueue<Action>();
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
            Action action = () => {
                SynchronizationContext.SetSynchronizationContext(this);

                d?.Invoke(state);
            };

            _queue.Enqueue(action);
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
            Exception ex = null;
            using (var waiter = new ManualResetEventSlim(false)) {
                Post(_ => {
                    try {
                        d.Invoke(state);
                    }
                    catch (Exception e) {
                        ex = e;
                    }
                    finally {
                        waiter.Set();
                    }
                },null);
                waiter.Wait();
            }

            if (ex != null)
                throw ex;
        }
        /// <summary>
        /// External call, executes on the current thread.
        /// 外部调用，在当前线程执行。
        /// </summary>
        /// <param name="doLoopMSGEvents">
        /// Whether to loop continuously.
        /// 是否持续循环。
        /// </param>
        public void DoActions(bool doLoopMSGEvents = false) {
            do {
                Action action = null;
                while (_queue.Count > 0) {
                    if (_queue.TryDequeue(out action)) {
                        action();
                    }
                }
                threadSpinOnce();
                //Thread.Sleep(10);
            } while (doLoopMSGEvents);
        }
        private void threadSpinOnce() {
            //System.Threading.Thread.Sleep(0);
            SpinWait spinWait = new SpinWait();
            Thread.MemoryBarrier();
            spinWait.SpinOnce();
        }
    }
}
