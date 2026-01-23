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
    internal delegate void WorkItemCompletedCallback(WorkItem workItem,object result,Exception exception,int retryCount = 0);

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
        private volatile bool _callbackInvoked = false;

        /// <summary>
        /// 是否为延迟工作
        /// Whether this is delayed work
        /// </summary>
        public bool IsDelayedWork { get; set; }

        /// <summary>
        /// 计划执行时间
        /// Scheduled execution time
        /// </summary>
        public DateTime ExecuteTime { get; set; }

        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID ID => _id;

        /// <summary>
        /// 工作名称（从WorkOption获取）
        /// Work name (from WorkOption)
        /// </summary>
        public string Name => _option.Name+"_"+_id;

        ///// <summary>
        ///// 工作分组（从WorkOption获取）
        ///// Work group (from WorkOption)
        ///// </summary>
        //public string Group => _option.Group;

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

        /// <summary>
        /// 异步执行工作（由WorkerThread调用，支持重试）
        /// Execute work asynchronously (called by WorkerThread, supports retry)
        /// </summary>
        public void Execute(WorkItemCompletedCallback callback) {
            ExecuteWithRetry(callback,this._option.MaxRetries);
        }

        /// <summary>
        /// 异步执行工作（带重试支持）
        /// Execute work asynchronously with retry support
        /// </summary>
        private void ExecuteWithRetry(WorkItemCompletedCallback callback,int currentRetryCount) {
            // 设置开始时间（仅在第一次执行时设置）
            if (currentRetryCount == 0)
                _startTime = DateTime.Now;

            // 检查是否需要开启新线程执行
            bool needsSeparateThread = _option.CancellationToken != null ||
                                    (_option.Timeout.TotalMilliseconds < int.MaxValue &&
                                    _option.Timeout.TotalMilliseconds > 0);

            if (!needsSeparateThread) {
                // 没有设置超时或取消令牌，直接在当前线程执行，节省线程启动开销
                ExecuteSynchronously(callback,currentRetryCount);
            }
            else {
                // 有超时或取消令牌，需要在新线程中执行以便控制
                ExecuteInSeparateThread(callback,currentRetryCount);
            }
        }

        /// <summary>
        /// 判断异常是否应该重试
        /// Determine if exception should be retried
        /// </summary>
        private bool ShouldRetry(Exception exception) {
            // 超时不重试
            if (exception is TimeoutException)
                return false;

            // 取消不重试
            if (exception is OperationCanceledException)
                return false;

            // 检查是否达到最大重试次数
            if (_option.MaxRetries <= 0)
                return false;

            // 如果设置了重试条件，使用自定义条件
            if (_option.RetryCondition != null)
                return (bool)_option.RetryCondition.DynamicInvoke(exception);

            // 默认对所有异常重试（除超时和取消）
            return true;
        }

        /// <summary>
        /// 同步执行工作（在当前线程直接执行，无额外线程开销）
        /// Execute work synchronously (execute directly in current thread, no extra thread overhead)
        /// </summary>
        private void ExecuteSynchronously(WorkItemCompletedCallback callback,int currentRetryCount) {
            object result = null;
            Exception exception = null;

            try {
                // 直接执行工作方法
                result = _method.DynamicInvoke();
            }
            catch (TargetInvocationException tie) {
                exception = tie.InnerException ?? tie;
            }
            catch (OperationCanceledException) {
                exception = new OperationCanceledException("Work item execution was cancelled");
            }
            catch (Exception ex) {
                exception = ex;
            }

            // 判断是否需要重试
            if (exception != null && ShouldRetry(exception) && currentRetryCount < _option.MaxRetries) {
                // 等待重试间隔
                int waitTime = (int)_option.RetryInterval.TotalMilliseconds;
                if (waitTime > 0)
                    Thread.Sleep(waitTime);

                // 递归重试
                ExecuteWithRetry(callback, currentRetryCount + 1);
            }
            else {
                // 确保回调只执行一次
                if (!_callbackInvoked) {
                    _callbackInvoked = true;
                    callback(this, result, exception, currentRetryCount);
                }
            }
        }

        /// <summary>
        /// 在独立线程中执行工作（支持超时和取消）
        /// Execute work in separate thread (supports timeout and cancellation)
        /// </summary>
        private void ExecuteInSeparateThread(WorkItemCompletedCallback callback,int currentRetryCount) {
            // 创建线程同步信号
            ManualResetEvent executionCompletedEvent = new ManualResetEvent(false);
            bool executionCompleted = false;
            object executionResult = null;
            Exception executionException = null;
            bool threadAborted = false; // 标记线程是否被主线程中止

            // 创建执行线程
            this.ExecuteThread = new Thread(() => {
                try {
                    // 检查取消状态
                    if (_option.CancellationToken != null && _option.CancellationToken.IsCancellationRequested) {
                        throw new OperationCanceledException();
                    }

                    // 执行工作方法
                    executionResult = _method.DynamicInvoke();
                }
                catch (ThreadAbortException) {
                    // 线程被主线程中止（Thread.Abort），不设置异常，让主线程处理超时/取消
                    // Thread was aborted by main thread (Thread.Abort), don't set exception, let main thread handle timeout/cancel
                    executionException = null;
                    threadAborted = true;
                }
                catch (ThreadInterruptedException) {
                    // 线程被中断（Thread.Interrupt），表示取消操作
					//executionException = new OperationCanceledException("Work item execution was interrupted");
                    // Thread was interrupted (Thread.Interrupt), indicates cancellation
                    executionException = null;
                    threadAborted = true;
                }
                catch (TargetInvocationException tie) {
                    executionException = tie.InnerException ?? tie;
                }
                catch (OperationCanceledException) {
                    executionException = new OperationCanceledException("Work item execution was cancelled");
                }
                catch (Exception ex) {
                    executionException = ex;
                }
                finally {
                    // 只有在非中止情况下才标记为完成
                    // Only mark as completed if not aborted
                    if (!threadAborted) {
                        executionCompleted = true;
                    }
                    try {
                        executionCompletedEvent.Set();
                    }
                    catch (ObjectDisposedException) {
                        // 事件已被主线程关闭（超时或取消），忽略此异常
                        // Event has been closed by main thread (timeout or cancel), ignore this exception
                    }
                }
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

            // 计算超时截止时间（只计算一次）
            DateTime? timeoutDeadline = null;
            if (_option.Timeout.TotalMilliseconds > 0) {
                timeoutDeadline = startWaitTime + _option.Timeout;
            }

            while (!waitCompleted) {
                // 检查取消令牌
                if (_option.CancellationToken != null && _option.CancellationToken.IsCancellationRequested) {
                    // 取消令牌被触发，标记为取消状态
                    isCancellationTokenCompleted = true;
                    AbortThreadSafely();
                    // 设置事件以退出等待
                    executionCompletedEvent.Set();
                    break;
                }

                // 检查是否超时
                if (timeoutDeadline.HasValue && DateTime.Now >= timeoutDeadline.Value) {
                    // 超时，标记为超时状态
                    isTimeoutCompleted = true;
                    AbortThreadSafely();
                    // 设置事件以退出等待
                    executionCompletedEvent.Set();
                    break;
                }

                // 等待执行完成或短时间超时
                waitCompleted = executionCompletedEvent.WaitOne(TimeSpan.FromMilliseconds(100));
            }

            // 释放同步资源
            executionCompletedEvent.Close();

            // 如果超时或被取消，检查线程是否仍在运行
            if (!executionCompleted && this.ExecuteThread != null && this.ExecuteThread.IsAlive) {
                if (isCancellationTokenCompleted) {
                    Console.WriteLine($"WorkItem {_id} execution was cancelled, but thread is still running");
                }
                else if (isTimeoutCompleted) {
                    Console.WriteLine($"WorkItem {_id} execution timeout detected, but thread is still running");
                }
            }

            // 判断是否需要重试（仅在正常执行完成但失败的情况下）
            // 只有在非超时/取消、线程正常完成且有异常的情况下才重试
            if (!isCancellationTokenCompleted && !isTimeoutCompleted &&
                executionCompleted && executionException != null &&
                ShouldRetry(executionException) && currentRetryCount < _option.MaxRetries) {
                // 等待重试间隔
                int waitTime = (int)_option.RetryInterval.TotalMilliseconds;
                if (waitTime > 0)
                    Thread.Sleep(waitTime);

                // 递归重试
                ExecuteWithRetry(callback, currentRetryCount + 1);
                return;
            }

            // 确保回调只执行一次
            if (!_callbackInvoked) {
                _callbackInvoked = true;
                if (isCancellationTokenCompleted) {
                    // 取消令牌导致的退出
                    callback(this, null, new OperationCanceledException($"WorkItem {_id} execution was cancelled"), currentRetryCount);
                }
                else if (isTimeoutCompleted) {
                    // 超时导致的退出
                    callback(this, null, new TimeoutException($"WorkItem {_id} execution timeout after {_option.Timeout.TotalMilliseconds}ms"), currentRetryCount);
                }
                else {
                    // 正常完成或执行异常
                    callback(this, executionResult, executionException, currentRetryCount);
                }
            }
        }

        /// <summary>
        /// 安全地中止执行线程
        /// Safely abort the execution thread
        /// </summary>
        private void AbortThreadSafely() {
            try {
                if (this.ExecuteThread != null && this.ExecuteThread.IsAlive) {
                    // 注意：Thread.Abort 在 .NET Core/.NET 5+ 中已过时
                    // 这里为了兼容性保留，建议使用 CancellationToken 替代
                    // Note: Thread.Abort is obsolete in .NET Core/.NET 5+
                    // Kept here for compatibility, recommend using CancellationToken instead
                    this.ExecuteThread.Abort();
                }
            }
            catch {
                // 忽略中止失败
                // Ignore abort failure
            }
        }
    }
}