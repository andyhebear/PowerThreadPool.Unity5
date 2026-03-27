using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
namespace PowerThreadPool_Net20.Helpers
{
    /// <summary>
    /// 线程到主线程传递动作（实例单例模式）
    /// Thread to main thread action helper (instance singleton pattern)
    /// </summary>
    public sealed class ThreadInvokeHelper
    {
        private static readonly ThreadInvokeHelper _instance = new ThreadInvokeHelper();
        private static readonly object _instanceLock = new object();

        private SynchronizationContext _mainSyncContext;
        private int _initThreadId = -1; // 记录初始化线程的 ID / Record the ID of the initialization thread
        private int _invokeDepth = 0; // 检测嵌套同步调用 / Detect nested synchronous calls
        private readonly object _lockObj = new object();
        private bool _isInitialized = false;

        /// <summary>
        /// 获取单例实例
        /// Get singleton instance
        /// </summary>
        public static ThreadInvokeHelper Instance => _instance;

        /// <summary>
        /// 获取初始化线程的 ID
        /// Get the ID of the initialization thread
        /// </summary>
        public int InitThreadId => _initThreadId;

        /// <summary>
        /// 判断当前线程是否为初始化线程（即主线程）
        /// Determine if the current thread is the initialization thread (i.e., main thread)
        /// </summary>
        public bool IsInitThread => Thread.CurrentThread.ManagedThreadId == _initThreadId;

        /// <summary>
        /// 获取是否已初始化
        /// Get whether initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// Private constructor to prevent external instantiation
        /// </summary>
        internal ThreadInvokeHelper(bool autoInitialization=false)
        {
            if (autoInitialization) {
                Init();
            }
        }

        /// <summary>
        /// 初始化：必须在主线程调用，且只能调用一次
        /// Initialization: Must be called on the main thread and only once
        /// </summary>
        public void Init() {
            lock (_lockObj) {
                if (_isInitialized)
                    throw new InvalidOperationException("ThreadInvokeHelper 已初始化，禁止重复调用 / ThreadInvokeHelper is already initialized, cannot call again");

                // 记录初始化线程的 ID / Record the ID of the initialization thread
                _initThreadId = Thread.CurrentThread.ManagedThreadId;

                _mainSyncContext = SynchronizationContext.Current ?? new SynchronizationContext();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 校验初始化状态
        /// Validate initialization status
        /// </summary>
        private void CheckInit() {
            if (!_isInitialized)
                throw new InvalidOperationException("请先在主线程调用 ThreadInvokeHelper.Instance.Init() 初始化 / Please call ThreadInvokeHelper.Instance.Init() on the main thread first");
        }

        #region 无参 - 无返回值 / No parameters - No return value
        /// <summary>
        /// 在主线程同步执行操作（带超时和防死锁）
        /// Execute action synchronously on the main thread (with timeout and deadlock prevention)
        /// </summary>
        /// <param name="action">要执行的操作 / Action to execute</param>
        /// <param name="timeoutMs">超时时间（毫秒），-1 表示不超时 / Timeout in milliseconds, -1 means no timeout</param>
        public void InvokeOnMainThread(Action action,int timeoutMs = -1) {
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

                // 超时等待 / Timeout wait
                if (timeoutMs > 0 && !waitHandle.WaitOne(timeoutMs)) {
                    throw new TimeoutException($"主线程调用超时（{timeoutMs}ms） / Main thread invocation timeout ({timeoutMs}ms)");
                }
            }
            finally {
                Interlocked.Decrement(ref _invokeDepth);
                waitHandle.Close();
            }

            // 异常穿透 / Exception propagation
            if (ex != null)
                throw new Exception("主线程执行方法抛出异常 / Exception thrown in main thread execution",ex);
        }

        /// <summary>
        /// 在主线程异步执行操作（不阻塞调用线程）
        /// Execute action asynchronously on the main thread (does not block the calling thread)
        /// </summary>
        /// <param name="action">要执行的操作 / Action to execute</param>
        public void BeginInvokeOnMainThread(Action action) {
            CheckInit();
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (_mainSyncContext == SynchronizationContext.Current) {
                action();
                return;
            }

            // 异步调用不阻塞，无死锁风险 / Asynchronous invocation, no blocking, no deadlock risk
            _mainSyncContext.Post(_ =>
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"异步回调异常: {ex.Message} / Asynchronous callback exception: {ex.Message}"); }
            },null);
        }
        #endregion

        #region 带返回值 - 含超时 + 防死锁 / With return value - timeout + deadlock prevention
        /// <summary>
        /// 在主线程同步执行操作并返回结果（带超时和防死锁）
        /// Execute action synchronously on the main thread and return result (with timeout and deadlock prevention)
        /// </summary>
        /// <typeparam name="TResult">返回值类型 / Return type</typeparam>
        /// <param name="func">要执行的函数 / Function to execute</param>
        /// <param name="timeoutMs">超时时间（毫秒），-1 表示不超时 / Timeout in milliseconds, -1 means no timeout</param>
        /// <returns>执行结果 / Execution result</returns>
        public TResult InvokeOnMainThread<TResult>(Func<TResult> func,int timeoutMs = -1) {
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
                    throw new TimeoutException($"主线程调用超时（{timeoutMs}ms） / Main thread invocation timeout ({timeoutMs}ms)");
            }
            finally {
                Interlocked.Decrement(ref _invokeDepth);
                waitHandle.Close();
            }

            if (ex != null)
                throw new Exception("主线程执行方法抛出异常 / Exception thrown in main thread execution",ex);

            return result;
        }

        public TResult InvokeOnMainThread<T, TResult>(Func<T,TResult> func,T param,int timeoutMs = -1) {
            return InvokeOnMainThread(() => func(param),timeoutMs);
        }

        public TResult InvokeOnMainThread<T1, T2, TResult>(Func<T1,T2,TResult> func,T1 p1,T2 p2,int timeoutMs = -1) {
            return InvokeOnMainThread(() => func(p1,p2),timeoutMs);
        }
        #endregion

        /// <summary>
        /// 重置单例（仅用于测试）
        /// Reset singleton (for testing only)
        /// </summary>
        internal static void ResetForTesting() {
            lock (_instanceLock) {
                var field = typeof(ThreadInvokeHelper).GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (field != null) {
                    field.SetValue(null, new ThreadInvokeHelper());
                }
            }
        }
    }
}
