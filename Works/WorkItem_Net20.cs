using System;
using System.Reflection;
using System.Threading;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Threading;

namespace PowerThreadPool_Net20.Works
{
    /// <summary>
    /// 工作项执行完成回调委托
    /// Work item execution completion callback delegate
    /// </summary>
    internal delegate void WorkItemCompletedCallback(WorkItem workItem,object result,Exception exception);

    /// <summary>
    /// 工作项类
    /// Work item class
    /// </summary>
    internal class WorkItem
    {
        private readonly WorkID _id;
        private readonly Delegate _method;
        private readonly WorkOption _option;
        private readonly DateTime _createTime;
        private readonly PowerPool _pool; // 添加PowerPool引用用于超时线程注册
        internal Thread ExecuteThread;//执行线程
        private DateTime? _queueTime;
        private DateTime? _startTime;

        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID ID => _id;

        /// <summary>
        /// 工作名称（从WorkOption获取）
        /// Work name (from WorkOption)
        /// </summary>
        public string Name => _option.Name;

        /// <summary>
        /// 工作分组（从WorkOption获取）
        /// Work group (from WorkOption)
        /// </summary>
        public string Group => _option.Group;

        /// <summary>
        /// 选项
        /// Option
        /// </summary>
        public WorkOption Option => _option;

        /// <summary>
        /// 创建时间
        /// Create time
        /// </summary>
        public DateTime CreateTime => _createTime;

        /// <summary>
        /// 入队时间
        /// Queue time
        /// </summary>
        public DateTime QueueTime => _queueTime ?? DateTime.MinValue;

        /// <summary>
        /// 开始时间
        /// Start time
        /// </summary>
        public DateTime StartTime => _startTime ?? DateTime.MinValue;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkItem(WorkID id,Delegate method,WorkOption option)
            : this(id,method,option,null) {
        }

        /// <summary>
        /// 构造函数（内部使用，支持PowerPool引用）
        /// Internal constructor with PowerPool reference
        /// </summary>
        public WorkItem(WorkID id,Delegate method,WorkOption option,PowerPool pool) {
            _id = id;
            _method = method;
            _option = option ?? new WorkOption();
            _createTime = DateTime.Now;
            _queueTime = DateTime.Now; // 设置入队时间
            _pool = pool;
        }
        volatile bool wasExeCallback = false;
        /// <summary>
        /// 异步执行工作（由WorkerThread调用）
        /// Execute work asynchronously (called by WorkerThread)
        /// </summary>
        public void ExecuteAsync(WorkItemCompletedCallback callback) {
            // 设置开始时间
            _startTime = DateTime.Now;

            // 检查是否需要开启新线程执行
            bool needsSeparateThread = _option.CancellationToken != null ||
                                    (_option.Timeout.TotalMilliseconds < int.MaxValue &&
                                    _option.Timeout.TotalMilliseconds > 0);

            if (!needsSeparateThread)
            {
                // 没有设置超时或取消令牌，直接在当前线程执行，节省线程启动开销
                ExecuteSynchronously(callback);
            }
            else
            {
                // 有超时或取消令牌，需要在新线程中执行以便控制
                ExecuteInSeparateThread(callback);
            }
        }

        /// <summary>
        /// 同步执行工作（在当前线程直接执行，无额外线程开销）
        /// Execute work synchronously (execute directly in current thread, no extra thread overhead)
        /// </summary>
        private void ExecuteSynchronously(WorkItemCompletedCallback callback)
        {
            object result = null;
            Exception exception = null;

            try
            {
                // 直接执行工作方法
                result = _method.DynamicInvoke();
            }
            catch (TargetInvocationException tie)
            {
                exception = tie.InnerException ?? tie;
            }
            catch (OperationCanceledException)
            {
                exception = new OperationCanceledException("Work item execution was cancelled");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // 调用回调通知完成
            callback(this, result, exception);
        }

        /// <summary>
        /// 在独立线程中执行工作（支持超时和取消）
        /// Execute work in separate thread (supports timeout and cancellation)
        /// </summary>
        private void ExecuteInSeparateThread(WorkItemCompletedCallback callback)
        {
            // 创建线程同步信号
            ManualResetEvent executionCompletedEvent = new ManualResetEvent(false);
            bool executionCompleted = false;
            bool wasCancelled = false;
            bool wasTimeout = false;
            wasExeCallback = false;

            // 创建执行线程
            this.ExecuteThread = new Thread(() => {
                object result = null;
                Exception exception = null;

                try {
                    // 检查取消状态
                    if (_option.CancellationToken != null && _option.CancellationToken.IsCancellationRequested) {
                        wasCancelled = true;
                        throw new OperationCanceledException();
                    }

                    // 执行工作方法
                    result = _method.DynamicInvoke();
                }
                catch (ThreadInterruptedException) {
                    // 线程被中断，表示取消操作
                    wasCancelled = true;
                    exception = new OperationCanceledException("Work item execution was interrupted by cancellation token");
                }
                catch (TargetInvocationException tie) {
                    exception = tie.InnerException ?? tie;
                }
                catch (OperationCanceledException) {
                    wasCancelled = true;
                    exception = new OperationCanceledException("Work item execution was cancelled");
                }
                catch (Exception ex) {
                    exception = ex;
                }

                // 调用回调通知完成
                if (!wasExeCallback) {
                    wasExeCallback = true;
                    callback(this,result,exception);
                }
                // 设置执行完成标志
                executionCompleted = true;
                executionCompletedEvent.Set();
            });
            this.ExecuteThread.Name = $"{this._id}_{this.Name}";
            // 启动执行线程
            this.ExecuteThread.Start();

            // 等待执行线程完成（使用超时机制避免无限等待）
            // 同时处理取消令牌中断
            bool waitCompleted = false;
            bool isCancellationTokenCompleted = false;
            DateTime startWaitTime = DateTime.Now;
            bool isTimeoutCompleted = false;

            while (!waitCompleted) {
                // 检查取消令牌
                if (_option.CancellationToken != null && _option.CancellationToken.IsCancellationRequested) {
                    // 取消令牌被触发，标记为取消状态
                    wasCancelled = true;
                    isCancellationTokenCompleted = true;
                    try {
                        if (this.ExecuteThread != null && this.ExecuteThread.IsAlive) {
                            this.ExecuteThread.Abort();
                        }
                    }
                    catch {
                    }
                    // 设置事件以退出等待
                    executionCompletedEvent.Set();
                    break;
                }

                // 检查是否超时
                if (_option.Timeout.TotalMilliseconds < int.MaxValue && _option.Timeout.TotalMilliseconds > 0)
                {
                    TimeSpan elapsed = DateTime.Now - startWaitTime;
                    if (elapsed >= _option.Timeout)
                    {
                        // 超时，标记为超时状态
                        wasTimeout = true;
                        isTimeoutCompleted = true;
                        try {
                            if (this.ExecuteThread != null && this.ExecuteThread.IsAlive) {
                                this.ExecuteThread.Abort();
                            }
                        }
                        catch {
                        }
                        // 设置事件以退出等待
                        executionCompletedEvent.Set();
                        break;
                    }
                }

                // 等待执行完成或短时间超时
                waitCompleted = executionCompletedEvent.WaitOne(TimeSpan.FromMilliseconds(100));
            }

            // 如果超时或被取消，检查线程是否仍在运行
            if (!executionCompleted && this.ExecuteThread != null && this.ExecuteThread.IsAlive) {
                if (wasCancelled) {
                    Console.WriteLine($"WorkItem {_id} execution was cancelled, but thread is still running");
                }
                else if (wasTimeout) {
                    Console.WriteLine($"WorkItem {_id} execution timeout detected, but thread is still running");
                }
            }
            // 释放同步资源
            //(executionCompletedEvent as IDisposable).Dispose();
            executionCompletedEvent.Close();
            // 确保回调必须执行
            if (!wasExeCallback)
            {
                wasExeCallback = true;
                if (isCancellationTokenCompleted)
                {
                    // 取消令牌导致的退出
                    callback(this, null, new OperationCanceledException($"WorkItem {_id} execution was cancelled"));
                }
                else if (isTimeoutCompleted)
                {
                    // 超时导致的退出
                    callback(this, null, new TimeoutException($"WorkItem {_id} execution timeout after {_option.Timeout.TotalMilliseconds}ms"));
                }
                else
                {
                    // 其他未知原因导致的退出
                    callback(this, null, new Exception($"WorkItem {_id} execution was terminated unexpectedly"));
                }
            }
        }


    }
}