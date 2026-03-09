using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
namespace PowerThreadPool_Net20.Helpers
{
    /// <summary>
    /// 线程到主线程传递动作
    /// </summary>
    public static class ThreadInvokeHelper
    {
        private static SynchronizationContext _mainSyncContext;
        private static int _invokeDepth = 0; // 检测嵌套同步调用
        private static readonly object _lockObj = new object();

        /// <summary>
        /// 初始化：必须在主线程调用，且只能调用一次
        /// </summary>
        public static void Init() {
            lock (_lockObj) {
                if (_mainSyncContext != null)
                    throw new InvalidOperationException("ThreadInvokeHelper 已初始化，禁止重复调用");

                _mainSyncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            }
        }

        /// <summary>
        /// 校验初始化状态
        /// </summary>
        private static void CheckInit() {
            if (_mainSyncContext == null)
                throw new InvalidOperationException("请先在主线程调用 ThreadInvokeHelper.Init() 初始化");
        }

        #region 无参 - 无返回值
        public static void InvokeOnMainThread(Action action,int timeoutMs = -1) {
            CheckInit();
            if (action == null) throw new ArgumentNullException(nameof(action));

            // 1. 主线程/嵌套调用 直接执行，防死锁
            if (_mainSyncContext == SynchronizationContext.Current || _invokeDepth > 0) {
                action();
                return;
            }

            // 2. 带超时的同步调用
            var waitHandle = new ManualResetEvent(false);
            Exception ex = null;

            try {
                Interlocked.Increment(ref _invokeDepth);
                _mainSyncContext.Send(_ =>
                {
                    try { action(); }
                    catch (Exception e) { ex = e; }
                    finally { waitHandle.Set(); }
                },null);

                // 超时等待
                if (timeoutMs > 0 && !waitHandle.WaitOne(timeoutMs)) {
                    throw new TimeoutException($"主线程调用超时（{timeoutMs}ms）");
                }
            }
            finally {
                Interlocked.Decrement(ref _invokeDepth);
                waitHandle.Close();
            }

            // 异常穿透
            if (ex != null)
                throw new Exception("主线程执行方法抛出异常",ex);
        }

        public static void BeginInvokeOnMainThread(Action action) {
            CheckInit();
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (_mainSyncContext == SynchronizationContext.Current) {
                action();
                return;
            }

            // 异步调用不阻塞，无死锁风险
            _mainSyncContext.Post(_ =>
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"异步回调异常: {ex.Message}"); }
            },null);
        }
        #endregion

        #region 带返回值 - 含超时 + 防死锁
        public static TResult InvokeOnMainThread<TResult>(Func<TResult> func,int timeoutMs = -1) {
            CheckInit();
            if (func == null) throw new ArgumentNullException(nameof(func));

            if (_mainSyncContext == SynchronizationContext.Current || _invokeDepth > 0)
                return func();

            var waitHandle = new ManualResetEvent(false);
            Exception ex = null;
            TResult result = default;

            try {
                Interlocked.Increment(ref _invokeDepth);
                _mainSyncContext.Send(_ =>
                {
                    try { result = func(); }
                    catch (Exception e) { ex = e; }
                    finally { waitHandle.Set(); }
                },null);

                if (timeoutMs > 0 && !waitHandle.WaitOne(timeoutMs))
                    throw new TimeoutException($"主线程调用超时（{timeoutMs}ms）");
            }
            finally {
                Interlocked.Decrement(ref _invokeDepth);
                waitHandle.Close();
            }

            if (ex != null)
                throw new Exception("主线程执行方法抛出异常",ex);

            return result;
        }

        public static TResult InvokeOnMainThread<T, TResult>(Func<T,TResult> func,T param,int timeoutMs = -1) {
            return InvokeOnMainThread(() => func(param),timeoutMs);
        }

        public static TResult InvokeOnMainThread<T1, T2, TResult>(Func<T1,T2,TResult> func,T1 p1,T2 p2,int timeoutMs = -1) {
            return InvokeOnMainThread(() => func(p1,p2),timeoutMs);
        }
        #endregion
    }
}
