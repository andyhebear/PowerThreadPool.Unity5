using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool_Net20.Collections;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Works;
using PowerThreadPool_Net20.Exceptions;
using PowerThreadPool_Net20.Helpers;


namespace PowerThreadPool_Net20
{
    /// <summary>
    /// 为Unity5.6和.NET 2.0设计的简化版高效线程池
    /// A simplified efficient thread pool designed for Unity5.6 and .NET 2.0
    /// </summary>
    public class PowerPool : IDisposable
    {
        private bool _disposed = false;
        private bool _disposing = false;
        private bool _isStarted = false;

        // 线程同步对象
        private readonly object _lockObject = new object();
        private readonly ManualResetEvent _waitAllSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _pauseSignal = new ManualResetEvent(true);
        
        // 监控线程
        private Thread _monitorThread;
        private volatile bool _monitorThreadRunning = false;

        // 线程和队列管理
        private List<WorkerThread> _workerThreads = new List<WorkerThread>();
        private PriorityQueue _workQueue = new PriorityQueue();
        private PriorityQueue _suspendedWorkQueue = new PriorityQueue();

        // 结果缓存（用于ExecuteResult落地）
        private Dictionary<WorkID,ExecuteResult> _resultCache = new Dictionary<WorkID,ExecuteResult>();
        private readonly object _resultCacheLock = new object();
        
        // 超时线程跟踪（用于资源管理）
        private Dictionary<WorkID,Thread> _timeoutThreads = new Dictionary<WorkID,Thread>();
        private readonly object _timeoutThreadsLock = new object();

        // 取消检查管理（用于执行线程的取消检查）
        //private Dictionary<WorkID, WorkItem> _cancelCheckThreads = new Dictionary<WorkID,WorkItem>();
        //private readonly object _cancelCheckThreadsLock = new object();

        // 统计信息
        private int _totalWorkItems = 0;
        private int _completedWorkItems = 0;
        private int _failedWorkItems = 0;
        private long _totalQueueTime = 0;
        private long _totalExecuteTime = 0;
        private DateTime _startTime;

        // 配置选项
        private PowerPoolOption _options;
        public PowerPoolOption Options
        {
            get { return _options; }
            set
            {
                _options = value;
                UpdateWorkerCount();
            }
        }

        /// <summary>
        /// 线程池是否正在运行
        /// Whether the thread pool is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 空闲工作线程数量
        /// Number of idle worker threads
        /// </summary>
        public int IdleWorkerCount
        {
            get
            {
                lock (_lockObject)
                {
                    int count = 0;
                    foreach (WorkerThread worker in _workerThreads)
                    {
                        if (worker.IsIdle)
                            count++;
                    }
                    return count;
                }
            }
        }

        /// <summary>
        /// 等待执行的工作项数量
        /// Number of work items waiting to be executed
        /// </summary>
        public int WaitingWorkCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _workQueue.Count;
                }
            }
        }

        /// <summary>
        /// 失败的工作项数量
        /// Number of failed work items
        /// </summary>
        public int FailedWorkCount => _failedWorkItems;

        /// <summary>
        /// 总执行时间（毫秒）
        /// Total execution time in milliseconds
        /// </summary>
        public long TotalExecuteTime => _totalExecuteTime;

        /// <summary>
        /// 平均执行时间（毫秒）
        /// Average execution time in milliseconds
        /// </summary>
        public long AverageExecuteTime
        {
            get
            {
                return _completedWorkItems > 0 ? _totalExecuteTime / _completedWorkItems : 0;
            }
        }

        /// <summary>
        /// 线程池运行时间
        /// Thread pool running duration
        /// </summary>
        public TimeSpan RunningDuration
        {
            get
            {
                return IsRunning ? DateTime.Now - _startTime : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 活跃工作线程数量
        /// Number of active worker threads
        /// </summary>
        public int ActiveWorkerThreads
        {
            get
            {
                lock (_lockObject)
                {
                    return _workerThreads.Count;
                }
            }
        }

        /// <summary>
        /// 缓存结果数量
        /// Number of cached results
        /// </summary>
        public int CachedResultCount
        {
            get
            {
                lock (_resultCacheLock)
                {
                    return _resultCache.Count;
                }
            }
        }

        /// <summary>
        /// 检查工作结果是否已缓存
        /// Check if work result is cached
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否已缓存 / Whether cached</returns>
        private bool IsWorkResultCached(WorkID workId)
        {
            lock (_resultCacheLock)
            {
                return _resultCache.ContainsKey(workId);
            }
        }
        
        /// <summary>
        /// 检查工作项是否已完成（包括在队列中、执行中或已完成）
        /// Check if work item is completed (including queued, executing, or finished)
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否已完成 / Whether completed</returns>
        private bool IsWorkCompleted(WorkID workId)
        {
            // 如果结果已缓存，说明已完成
            if (IsWorkResultCached(workId))
                return true;
                
            lock (_lockObject)
            {
                // 如果还在队列中，说明未完成
                if (IsWorkQueued(workId))
                    return false;
                    
                // 检查是否有线程正在执行
                foreach (WorkerThread worker in _workerThreads)
                {
                    if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workId))
                        return false;
                }
                
                // 不在队列中，也不在执行，可能已完成但结果被清理，或从未执行
                return true;
            }
        }

        /// <summary>
        /// 检查工作项是否还在队列中
        /// Check if work item is still in queue
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否在队列中 / Whether in queue</returns>
        private bool IsWorkQueued(WorkID workId)
        {
            lock (_lockObject)
            {
                // 检查主队列
                foreach (WorkItem work in _workQueue)
                {
                    if (work.ID.Equals(workId))
                        return true;
                }

                // 检查挂起队列
                foreach (WorkItem work in _suspendedWorkQueue)
                {
                    if (work.ID.Equals(workId))
                        return true;
                }

                return false;
            }
        }

        // 事件委托
        public event EventHandler<WorkCompletedEventArgs> WorkCompleted;
        public event EventHandler<WorkFailedEventArgs> WorkFailed;
        //public event EventHandler<WorkCanceledEventArgs> WorkCanceled;
        public event EventHandler<PoolStartedEventArgs> PoolStarted;
        public event EventHandler<PoolStoppedEventArgs> PoolStopped;

        /// <summary>
        /// 默认构造函数
        /// Default constructor
        /// </summary>
        public PowerPool()
        {
            _options = new PowerPoolOption();
        }

        /// <summary>
        /// 使用指定选项的构造函数
        /// Constructor with specified options
        /// </summary>
        public PowerPool(PowerPoolOption options)
        {
            _options = options ?? new PowerPoolOption();
            UpdateWorkerCount();
        }

        /// <summary>
        /// [核心]创建完PowerPool后启动线程池才能工作
        /// Start the thread pool
        /// </summary>
        public void Start()
        {
            CheckDisposed();

            if (IsRunning)
                return;

            lock (_lockObject)
            {
                if (IsRunning)
                    return;

                IsRunning = true;
                _startTime = DateTime.Now;

                // 重置统计信息
                _totalWorkItems = 0;
                _completedWorkItems = 0;
                _failedWorkItems = 0;
                _totalQueueTime = 0;
                _totalExecuteTime = 0;

                // 创建工作线程
                CreateWorkerThreads();

                // 启动监控线程
                StartMonitorThread();

                // 启动挂起的工作
                StartSuspendedWork();

                // 检查初始空闲状态
                CheckPoolIdle();

                // 触发启动事件
                if (PoolStarted != null)
                {
                    PoolStarted(this,new PoolStartedEventArgs(DateTime.Now));
                }
            }
        }

        /// <summary>
        /// 停止线程池
        /// Stop the thread pool
        /// </summary>
        public void Stop()
        {
            // 在Dispose过程中调用时，允许Stop()执行
            if (!_disposing)
            {
                CheckDisposed();
            }

            InternalStop();
        }

        /// <summary>
        /// 暂停线程池
        /// Pause the thread pool
        /// </summary>
        public void Pause()
        {
            CheckDisposed();
            _pauseSignal.Reset();
        }

        /// <summary>
        /// 恢复线程池
        /// Resume the thread pool
        /// </summary>
        public void Resume()
        {
            CheckDisposed();
            _pauseSignal.Set();
        }

        /// <summary>
        /// 队列工作项
        /// Queue a work item
        /// </summary>
        public WorkID QueueWorkItem(Action action,WorkOption option = null)
        {
            return QueueWorkItem<object>(() =>
            {
                action();
                return null;
            },option);
        }

        /// <summary>
        /// 队列工作项（带返回值）
        /// Queue a work item with return value
        /// </summary>
        public WorkID QueueWorkItem<T>(Func<T> func,WorkOption option = null)
        {
            CheckDisposed();

            option = option ?? new WorkOption();
            WorkID workID = new WorkID(true);
            WorkItem workItem = new WorkItem(workID,func,option,this);

            lock (_lockObject)
            {
                // 检查队列限制
                if (!_options.StartSuspended && _workQueue.Count >= _options.ThreadQueueLimit)
                {
                    throw new InvalidOperationException($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
                }

                if (_options.StartSuspended)
                {
                    _suspendedWorkQueue.Enqueue(workItem);
                }
                else
                {
                    _workQueue.Enqueue(workItem);
                    _totalWorkItems++;

                    // 重置WaitAll信号，因为有新工作入队
                    _waitAllSignal.Reset();

                    // 通知空闲线程
                    Monitor.PulseAll(_lockObject);
                }
            }

            return workID;
        }

        /// <summary>
        /// 内部队列工作项方法（不加锁，供批量操作使用）
        /// Internal queue work item method (without lock, for batch operations)
        /// </summary>
        private WorkID QueueWorkItemInternal(Action action,WorkOption option)
        {
            option = option ?? new WorkOption();
            WorkID workID = new WorkID(true);
            WorkItem workItem = new WorkItem(workID,action,option,this);

            // 检查队列限制
            if (!_options.StartSuspended && _workQueue.Count >= _options.ThreadQueueLimit)
            {
                throw new InvalidOperationException($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
            }

            if (_options.StartSuspended)
            {
                _suspendedWorkQueue.Enqueue(workItem);
            }
            else
            {
                _workQueue.Enqueue(workItem);
                _totalWorkItems++;

                // 重置WaitAll信号，因为有新工作入队
                _waitAllSignal.Reset();

                // 通知空闲线程
                Monitor.PulseAll(_lockObject);
            }

            return workID;
        }

        /// <summary>
        /// 并行循环 - 执行从start到end的循环
        /// Parallel for loop - Execute loop from start to end
        /// </summary>
        /// <param name="start">起始索引 / Start index</param>
        /// <param name="end">结束索引 / End index</param>
        /// <param name="body">循环体 / Loop body</param>
        /// <param name="step">步长 / Step size</param>
        /// <returns>所有工作ID / All work IDs</returns>
        public WorkID[] ParallelFor(int start,int end,Action<int> body,int step = 1)
        {
            CheckDisposed();

            if (start >= end)
                return new WorkID[0];

            if (step <= 0)
                throw new ArgumentException("Step must be greater than 0",nameof(step));

            if (body == null)
                throw new ArgumentNullException(nameof(body));

            // 计算批次大小，基于可用线程数
            int totalIterations = (end - start + step - 1) / step;
            int batchSize = Math.Max(1,totalIterations / Math.Max(1,_options.MaxThreads));

            var workIds = new List<WorkID>();

            lock (_lockObject)
            {
                for (int i = start; i < end; i += batchSize * step)
                {
                    int batchStart = i;
                    int batchEnd = Math.Min(i + batchSize * step,end);

                    // 创建批处理工作项
                    WorkID workId = QueueWorkItemInternal(() =>
                    {
                        for (int j = batchStart; j < batchEnd; j += step)
                        {
                            body(j);
                        }
                    },null);

                    workIds.Add(workId);
                }
            }

            return workIds.ToArray();
        }

        /// <summary>
        /// 获取指定工作项的执行结果
        /// Get execution result of specified work item
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>执行结果 / Execution result</returns>
        public ExecuteResult GetResult(WorkID workId)
        {
            CheckDisposed();

            // 首先检查结果缓存
            lock (_resultCacheLock)
            {
                ExecuteResult result;
                if (_resultCache.TryGetValue(workId,out result))
                {
                    return result;
                }
            }

            // 检查工作是否还在队列中
            if (IsWorkQueued(workId))
            {
                // 还在队列中
                throw new InvalidOperationException($"Work {workId} is still queued");
            }
            
            // 检查是否有工作线程正在执行该工作项
            bool isExecuting = false;
            lock (_lockObject)
            {
                foreach (WorkerThread worker in _workerThreads)
                {
                    if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workId))
                    {
                        isExecuting = true;
                        break;
                    }
                }
            }
            
            if (isExecuting)
            {
                // 正在执行中
                throw new InvalidOperationException($"Work {workId} is still executing");
            }
            else
            {
                // 工作已完成但结果不可用（可能是清理了或从未执行）
                throw new InvalidOperationException($"Work {workId} result is no longer available");
            }
        }

        /// <summary>
        /// 获取多个工作项的执行结果
        /// Get execution results of multiple work items
        /// </summary>
        /// <param name="workIds">工作ID数组 / Work ID array</param>
        /// <returns>执行结果数组 / Execution result array</returns>
        public ExecuteResult[] GetResults(params WorkID[] workIds)
        {
            CheckDisposed();

            if (workIds == null)
                throw new ArgumentNullException(nameof(workIds));

            var results = new ExecuteResult[workIds.Length];
            for (int i = 0; i < workIds.Length; i++)
            {
                results[i] = GetResult(workIds[i]);
            }
            return results;
        }

        /// <summary>
        /// 等待工作完成并返回结果
        /// Wait for work completion and return result
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <param name="timeoutMs">超时毫秒数 / Timeout in milliseconds</param>
        /// <returns>执行结果 / Execution result</returns>
        public ExecuteResult GetResultAndWait(WorkID workId,int timeoutMs = 30000)
        {
            CheckDisposed();

            // 等待工作完成
            WaitWork(workId,timeoutMs);

            // 获取结果
            return GetResult(workId);
        }

        /// <summary>
        /// 等待多个工作完成并返回结果（批量等待优化）
        /// Wait for multiple work completion and return results (optimized batch waiting)
        /// </summary>
        /// <param name="workIds">工作ID数组 / Work ID array</param>
        /// <param name="timeoutMs">总超时毫秒数 / Total timeout in milliseconds</param>
        /// <returns>执行结果数组 / Execution result array</returns>
        public ExecuteResult[] GetResultsAndWait(WorkID[] workIds,int timeoutMs = 30000)
        {
            CheckDisposed();

            if (workIds == null)
                throw new ArgumentNullException(nameof(workIds));

            // 批量等待优化：计算每个工作的平均超时时间
            DateTime startTime = DateTime.Now;

            // 等待所有工作完成
            foreach (var workId in workIds)
            {
                // 动态计算剩余超时时间
                long elapsed = (DateTime.Now - startTime).Ticks / TimeSpan.TicksPerMillisecond;
                int remainingTimeout = Math.Max(0,timeoutMs - (int)elapsed);

                if (remainingTimeout <= 0)
                    break; // 总超时时间已用完

                WaitWork(workId,remainingTimeout);
            }

            // 获取所有结果
            return GetResults(workIds);
        }

        /// <summary>
        /// 清除指定的执行结果缓存
        /// Clear specified execution result cache
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否成功清除 / Whether successfully cleared</returns>
        public bool ClearResult(WorkID workId)
        {
            CheckDisposed();

            lock (_resultCacheLock)
            {
                return _resultCache.Remove(workId);
            }
        }

        /// <summary>
        /// 批量清除执行结果缓存
        /// Batch clear execution result cache
        /// </summary>
        /// <param name="workIds">工作ID数组 / Work ID array</param>
        /// <returns>成功清除的数量 / Number of successfully cleared items</returns>
        public int ClearResults(params WorkID[] workIds)
        {
            CheckDisposed();

            if (workIds == null)
                return 0;

            int clearedCount = 0;
            lock (_resultCacheLock)
            {
                foreach (WorkID workId in workIds)
                {
                    if (_resultCache.Remove(workId))
                        clearedCount++;
                }
            }
            return clearedCount;
        }

        /// <summary>
        /// 清除过期结果缓存（基于时间）
        /// Clear expired result cache based on time
        /// </summary>
        /// <param name="maxAgeMs">最大保留时间（毫秒）/ Maximum retention time in milliseconds</param>
        /// <returns>清除的项目数量 / Number of cleared items</returns>
        public int ClearExpiredResults(int maxAgeMs)
        {
            CheckDisposed();

            if (maxAgeMs <= 0)
                return 0;

            DateTime cutoffTime = DateTime.Now.AddMilliseconds(-maxAgeMs);
            List<WorkID> expiredIds = new List<WorkID>();

            lock (_resultCacheLock)
            {
                foreach (var kvp in _resultCache)
                {
                    if (kvp.Value.EndTime < cutoffTime)
                        expiredIds.Add(kvp.Key);
                }

                foreach (WorkID id in expiredIds)
                {
                    _resultCache.Remove(id);
                }
            }

            return expiredIds.Count;
        }

        /// <summary>
        /// 清除所有执行结果缓存
        /// Clear all execution result cache
        /// </summary>
        public void ClearAllResults()
        {
            CheckDisposed();

            lock (_resultCacheLock)
            {
                _resultCache.Clear();
            }
        }



        /// <summary>
        /// 获取工作执行状态摘要
        /// Get work execution status summary
        /// </summary>
        /// <returns>状态摘要信息 / Status summary information</returns>
        public WorkStatusSummary GetWorkStatusSummary()
        {
            CheckDisposed();

            lock (_lockObject)
            {
                return new WorkStatusSummary
                {
                    TotalQueued = WaitingWorkCount,
                    TotalExecuting = ActiveWorkerThreads - IdleWorkerCount,
                    TotalCompleted = _completedWorkItems,
                    TotalFailed = _failedWorkItems,
                    CachedResults = CachedResultCount,
                    SuccessRate = _totalWorkItems > 0 ? (double)_completedWorkItems / _totalWorkItems : 0.0
                };
            }
        }

        /// <summary>
        /// 并行循环 - 对集合中的每个元素执行操作
        /// Parallel foreach - Execute action for each element in collection
        /// </summary>
        /// <typeparam name="T">元素类型 / Element type</typeparam>
        /// <param name="source">源集合 / Source collection</param>
        /// <param name="body">操作体 / Action body</param>
        /// <returns>所有工作ID / All work IDs</returns>
        public WorkID[] ParallelForEach<T>(IEnumerable<T> source,Action<T> body)
        {
            CheckDisposed();

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (body == null)
                throw new ArgumentNullException(nameof(body));

            var items = source as T[] ?? ToArray(source);
            if (items.Length == 0)
                return new WorkID[0];

            // 计算批次大小
            int batchSize = Math.Max(1,items.Length / Math.Max(1,this._options.MaxThreads));
            var workIds = new List<WorkID>();

            lock (_lockObject)
            {
                for (int i = 0; i < items.Length; i += batchSize)
                {
                    int batchStart = i;
                    int batchEnd = Math.Min(i + batchSize,items.Length);
                    var batch = new T[batchEnd - batchStart];
                    Array.Copy(items,batchStart,batch,0,batch.Length);

                    // 创建批处理工作项
                    WorkID workId = QueueWorkItemInternal(() =>
                    {
                        foreach (var item in batch)
                        {
                            body(item);
                        }
                    },null);

                    workIds.Add(workId);
                }
            }

            return workIds.ToArray();
        }

        private T[] ToArray<T>(IEnumerable<T> source)
        {
            List<T> result = new List<T>();
            foreach (var item in source)
            {
                result.Add(item);
            }
            return result.ToArray();
        }


        /// <summary>
        /// 并行执行 - 并行执行多个操作
        /// Parallel invoke - Execute multiple actions in parallel
        /// </summary>
        /// <param name="actions">要执行的操作 / Actions to execute</param>
        /// <returns>所有工作ID / All work IDs</returns>
        public WorkID[] ParallelInvoke(params Action[] actions)
        {
            CheckDisposed();

            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            if (actions.Length == 0)
                return new WorkID[0];

            var workIds = new WorkID[actions.Length];

            lock (_lockObject)
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    int index = i; // 避免闭包问题
                    if (actions[index] != null)
                    {
                        workIds[index] = QueueWorkItemInternal(actions[index],null);
                    }
                }
            }

            return workIds;
        }

        /// <summary>
        /// 等待所有工作完成
        /// Wait for all work to complete
        /// </summary>
        public void WaitAll()
        {
            CheckDisposed();
            
            // 首先检查当前是否已经空闲
            CheckPoolIdle();
            
            // 然后等待信号，增加异常处理
            try
            {
                _waitAllSignal.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                // 如果信号已被释放，检查是否真的完成了所有工作
                if (WaitingWorkCount == 0 && IdleWorkerCount == ActiveWorkerThreads)
                {
                    return; // 已经完成，直接返回
                }
                throw;
            }
        }

        /// <summary>
        /// 等待指定的工作完成
        /// Wait for specified work to complete
        /// </summary>
        public void WaitWork(WorkID workID,int timeoutMs = 30000)
        {
            CheckDisposed();

            DateTime startTime = DateTime.Now;
            while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeoutMs))
            {
                // 首先检查结果缓存 - 如果有结果说明已完成
                lock (_resultCacheLock)
                {
                    if (_resultCache.ContainsKey(workID))
                    {
                        return; // 工作已完成，结果已缓存
                    }
                }

                bool isCompleted = true;
                lock (_lockObject)
                {
                    // 检查队列中是否有该工作项
                    foreach (WorkItem work in _workQueue)
                    {
                        if (work.ID.Equals(workID))
                        {
                            isCompleted = false;
                            break;
                        }
                    }

                    // 检查是否有工作线程正在执行该工作项
                    if (isCompleted)
                    {
                        foreach (WorkerThread worker in _workerThreads)
                        {
                            if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workID))
                            {
                                isCompleted = false;
                                break;
                            }
                        }
                    }
                }

                if (isCompleted)
                {
                    // 工作项已完成，但结果可能还未缓存
                    // 给OnWorkCompleted一些时间来缓存结果
                    Thread.Sleep(10);
                    
                    // 再次检查结果缓存
                    lock (_resultCacheLock)
                    {
                        if (_resultCache.ContainsKey(workID))
                        {
                            return;
                        }
                    }
                }

                Thread.Sleep(50); // 避免忙等待
            }

            throw new TimeoutException($"Work {workID} did not complete within {timeoutMs}ms timeout period.");
        

        //    DateTime startTime = DateTime.Now;
        //    while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeoutMs))
        //    {
        //        bool isCompleted = true;
                
        //        // 首先检查结果缓存 - 如果有结果说明已完成
        //        lock (_resultCacheLock)
        //        {
        //            if (_resultCache.ContainsKey(workID))
        //            {
        //                return; // 工作已完成，结果已缓存
        //            }
        //        }
                
        //        lock (_lockObject)
        //        {
        //            // 检查队列中是否有该工作项
        //            foreach (WorkItem work in _workQueue)
        //            {
        //                if (work.ID.Equals(workID))
        //                {
        //                    isCompleted = false;
        //                    break;
        //                }
        //            }

        //            // 检查是否有工作线程正在执行该工作项
        //            if (isCompleted)
        //            {
        //                foreach (WorkerThread worker in _workerThreads)
        //                {
        //                    if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workID))
        //                    {
        //                        isCompleted = false;
        //                        break;
        //                    }
        //                }
        //            }
                    
        //            // 如果队列中没有且没有线程在执行，但结果缓存中也没有，
        //            // 说明工作可能还没开始或者已经完成但结果还没缓存
        //            // 再检查一下是否在任何地方能找到这个工作
        //            if (isCompleted)
        //            {
        //                // 最后检查：如果队列和线程中都没有，那可能工作已经完成但结果还没来得及缓存
        //                // 或者工作根本不存在，这种情况下我们应该等待一小段时间再检查
        //                // 这里我们简单地继续等待，让后续的逻辑处理
        //            }
        //        }

        //        if (isCompleted)
        //        {
        //            // 再次确认结果缓存
        //            lock (_resultCacheLock)
        //            {
        //                if (_resultCache.ContainsKey(workID))
        //                {
        //                    return;
        //                }
        //            }
        //        }

        //        Thread.Sleep(50); // 避免忙等待
        //    }

        //    throw new TimeoutException($"Work {workID} did not complete within {timeoutMs}ms timeout period.");
        }

        /// <summary>
        /// 清空队列
        /// Clear the queue
        /// </summary>
        public void ClearQueue()
        {
            CheckDisposed();
            lock (_lockObject)
            {
                _workQueue.Clear();
                CheckPoolIdle();
            }
        }

        /// <summary>
        /// 检查线程池是否空闲
        /// Check if the thread pool is idle
        /// </summary>
        private void CheckPoolIdle()
        {
            bool isIdle = false;
            bool shouldSetSignal = false;
            
            lock (_lockObject)
            {
                // 在锁内再次检查状态，避免竞态条件
                if (_disposed || _disposing)
                    return;

                // 检查工作队列是否为空
                bool queueEmpty = _workQueue.Count == 0;
                
                // 检查所有活跃工作线程是否都空闲
                bool allThreadsIdle = true;
                foreach (WorkerThread worker in _workerThreads)
                {
                    if (!worker.IsIdle)
                    {
                        allThreadsIdle = false;
                        break;
                    }
                }
                
                isIdle = queueEmpty && allThreadsIdle;
                shouldSetSignal = isIdle;
            }

            // 在锁外设置信号，避免死锁
            try
            {
                if (shouldSetSignal)
                {
                    _waitAllSignal.Set();
                }
                else
                {
                    _waitAllSignal.Reset();
                }
            }
            catch (ObjectDisposedException)
            {
                // 忽略对象已释放异常，正常情况
            }
        }

        /// <summary>
        /// 启动监控线程
        /// Start monitor thread
        /// </summary>
        private void StartMonitorThread()
        {
            if (_monitorThread != null && _monitorThread.IsAlive)
                return;

            _monitorThreadRunning = true;
            _monitorThread = new Thread(MonitorThreadProc)
            {
                Name = "PowerPool-Monitor",
                IsBackground = true
            };
            _monitorThread.Start();
        }

        /// <summary>
        /// 停止监控线程
        /// Stop monitor thread
        /// </summary>
        private void StopMonitorThread()
        {
            if (_monitorThread == null)
                return;

            _monitorThreadRunning = false;
            
            // 等待监控线程结束
            if (_monitorThread.IsAlive)
            {
                _monitorThread.Join(1000); // 最多等待1秒
                if (_monitorThread.IsAlive)
                {
                    // 如果线程还在运行，强制中断（仅在.NET Core/NET 5+中可用）
                    try
                    {
#if !NET20
                        _monitorThread.Interrupt();
#endif
                    }
                    catch
                    {
                        // 忽略中断异常
                    }
                }
            }
            _monitorThread = null;
        }
       
     

        /// <summary>
        /// 监控线程主循环
        /// Monitor thread main procedure
        /// </summary>
        private void MonitorThreadProc()
        {
            DateTime lastThreadCleanupTime = DateTime.Now;
            
            while (_monitorThreadRunning && IsRunning)
            {
                try
                {
                    // 检查线程池空闲状态
                    CheckPoolIdle();
                                       
                    // 每30秒执行一次线程回收检查
                    if (DateTime.Now - lastThreadCleanupTime >= TimeSpan.FromSeconds(30))
                    {
                        CleanupIdleThreads();
                        lastThreadCleanupTime = DateTime.Now;
                    }
                    
                    // 每50ms检查一次
                    Thread.Sleep(50);
                }
                catch (ThreadAbortException)
                {
                    // 线程被中止，正常退出
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    // 线程被中断，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    // 记录异常但继续运行
#if UNITY
                    UnityEngine.Debug.LogWarning($"PowerPool monitor thread error: {ex.Message}");
#else
                    Console.WriteLine($"PowerPool monitor thread error: {ex.Message}");
#endif
                }
            }
        }

        /// <summary>
        /// 清理空闲线程
        /// Clean up idle threads
        /// </summary>
        private void CleanupIdleThreads()
        {
            lock (_lockObject)
            {
                // 确保不低于最小线程数
                if (_workerThreads.Count <= _options.MinThreads)
                    return;
                
                List<WorkerThread> threadsToRemove = new List<WorkerThread>();
                DateTime now = DateTime.Now;
                
                // 查找超过空闲超时时间的线程
                foreach (WorkerThread worker in _workerThreads)
                {
                    if (worker.IsIdle && now - worker.IdleStartTime >= _options.IdleThreadTimeout)
                    {
                        threadsToRemove.Add(worker);
                        
                        // 确保不低于最小线程数
                        if (_workerThreads.Count - threadsToRemove.Count <= _options.MinThreads)
                            break;
                    }
                }
                
                // 停止并移除这些线程
                foreach (WorkerThread worker in threadsToRemove)
                {
                    worker.Stop();
                    _workerThreads.Remove(worker);
                }
                
                // 通知所有等待的线程
                if (threadsToRemove.Count > 0)
                {
                    Monitor.PulseAll(_lockObject);
                    
                    // 在锁外等待线程真正停止
                    foreach (var worker in threadsToRemove)
                    {
                        try
                        {
                            worker.Thread.Join(100); // 最多等待100ms
                        }
                        catch (Exception ex)
                        {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#else
                            Console.WriteLine($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建工作线程
        /// Create worker threads
        /// </summary>
        private void CreateWorkerThreads()
        {
            int threadCount = _options.MaxThreads;

            for (int i = 0; i < threadCount; i++)
            {
                WorkerThread worker = new WorkerThread(this,i);
                _workerThreads.Add(worker);
                worker.Start();
            }
        }

        /// <summary>
        /// 更新工作线程数量
        /// Update worker thread count
        /// </summary>
        private void UpdateWorkerCount()
        {
            if (!IsRunning)
                return;

            lock (_lockObject)
            {
                int currentCount = _workerThreads.Count;
                int targetCount = _options.MaxThreads;

                if (targetCount > currentCount)
                {
                    // 添加新线程
                    for (int i = currentCount; i < targetCount; i++)
                    {
                        WorkerThread worker = new WorkerThread(this,i);
                        _workerThreads.Add(worker);
                        worker.Start();
                    }
                }
                else if (targetCount < currentCount)
                {
                    // 确保不低于最小线程数
                    int minAllowed = _options.MinThreads;
                    if (currentCount <= minAllowed)
                        return;
                        
                    // 减少线程（标记要停止的线程并等待完成）
                    int targetAfterReduction = Math.Max(targetCount, minAllowed);
                    int toStop = currentCount - targetAfterReduction;
                    var threadsToStop = new List<WorkerThread>();
                    
                    // 从末尾开始标记要停止的线程
                    for (int i = 0; i < toStop && _workerThreads.Count > minAllowed; i++)
                    {
                        int index = _workerThreads.Count - 1;
                        WorkerThread worker = _workerThreads[index];
                        worker.MarkForStop();
                        threadsToStop.Add(worker);
                        _workerThreads.RemoveAt(index); // 立即从列表中移除
                    }
                    
                    // 通知所有等待的线程
                    Monitor.PulseAll(_lockObject);
                    
                    // 在锁外等待线程真正停止
                    foreach (var worker in threadsToStop)
                    {
                        worker.Join();
                    }
                }
            }
        }

        /// <summary>
        /// 启动挂起的工作
        /// Start suspended work
        /// </summary>
        private void StartSuspendedWork()
        {
            while (_suspendedWorkQueue.Count > 0)
            {
                WorkItem workItem = _suspendedWorkQueue.Dequeue();
                _workQueue.Enqueue(workItem);
                _totalWorkItems++;
            }

            Monitor.PulseAll(_lockObject);
        }

        /// <summary>
        /// 工作线程获取工作项
        /// Worker thread gets work item
        /// </summary>
        internal WorkItem GetWorkItem()
        {
            while (IsRunning && !_disposed)
            {
                lock (_lockObject)
                {
                    // 双重检查，防止在获取锁的过程中状态发生变化
                    if (!IsRunning || _disposed)
                        break;
                        
                    if (_workQueue.Count > 0)
                    {
                        WorkItem workItem = _workQueue.Dequeue();
                        return workItem;
                    }

                    // 等待工作项，使用更短的超时以更快响应停止信号
                    Monitor.Wait(_lockObject, 50);
                }
            }

            return null;
        }

        /// <summary>
        /// 工作完成回调
        /// Work completed callback
        /// </summary>
        internal void OnWorkCompleted(WorkItem workItem,object result,Exception exception)
        {
            DateTime completionTime = DateTime.Now;
            DateTime startTime = workItem.CreateTime;
            ExecuteResult executeResult = null;

            // 创建ExecuteResult并缓存
            if (exception != null)
            {
                Interlocked.Increment(ref _failedWorkItems);

                // 判断异常类型
                if (exception is TimeoutException)
                {
                    executeResult = new ExecuteResult(workItem.ID,startTime,completionTime,exception);
                }
                else if (exception is OperationCanceledException)
                {
                    executeResult = new ExecuteResult(workItem.ID,startTime,completionTime);
                }
                else
                {
                    executeResult = new ExecuteResult(workItem.ID,exception,startTime,completionTime);
                }

                if (WorkFailed != null)
                {
                    WorkFailedEventArgs args = new WorkFailedEventArgs(workItem.ID,exception,completionTime);
                    WorkFailed(this,args);
                }
            }
            else
            {
                Interlocked.Increment(ref _completedWorkItems);
                executeResult = new ExecuteResult(workItem.ID,result,startTime,completionTime);

                if (WorkCompleted != null)
                {
                    WorkCompletedEventArgs args = new WorkCompletedEventArgs(workItem.ID,result,completionTime);
                    WorkCompleted(this,args);
                }
            }

            // 缓存结果
            CacheExecuteResult(workItem.ID,executeResult);

            CheckPoolIdle();
        }

        /// <summary>
        /// 缓存执行结果
        /// Cache execution result
        /// </summary>
        private void CacheExecuteResult(WorkID workId,ExecuteResult result)
        {
            lock (_resultCacheLock)
            {
                _resultCache[workId] = result;
            }
        }
        
        /// <summary>
        /// 检查是否已释放
        /// Check if disposed
        /// </summary>
        private void CheckDisposed()
        {
            if (_disposed || _disposing)
                throw new ObjectDisposedException("PowerPool");
        }

        /// <summary>
        /// 内部停止方法（Dispose调用，不检查Disposed状态）
        /// Internal stop method (called by Dispose, without disposed check)
        /// </summary>
        private void InternalStop()
        {
            if (!IsRunning)
                return;

            lock (_lockObject)
            {
                if (!IsRunning)
                    return;

                IsRunning = false;

                try
                {
                    // 停止监控线程
                    StopMonitorThread();

                    // 停止所有工作线程
                    foreach (WorkerThread worker in _workerThreads)
                    {
                        try
                        {
                            worker.Stop();
                        }
                        catch (Exception ex)
                        {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error stopping worker thread {worker.ThreadId}: {ex.Message}");
#else
                            Console.WriteLine($"Error stopping worker thread {worker.ThreadId}: {ex.Message}");
#endif
                        }
                    }

                    // 等待所有线程完成，设置更长的超时时间
                    foreach (WorkerThread worker in _workerThreads)
                    {
                        try
                        {
                            worker.Join();

                            // 检查线程是否真的停止了
                            if (worker.Thread != null && worker.Thread.IsAlive)
                            {
#if UNITY
                                Debug.LogWarning($"Worker thread {worker.ThreadId} did not stop gracefully after timeout");
#else
                                Console.WriteLine($"Worker thread {worker.ThreadId} did not stop gracefully after timeout");
#endif
                            }
                        }
                        catch (Exception ex)
                        {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#else
                            Console.WriteLine($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#endif
                        }
                    }

                    _workerThreads.Clear();

                    // 触发停止事件
                    if (PoolStopped != null)
                    {
                        try
                        {
                            PoolStopped(this,new PoolStoppedEventArgs(DateTime.Now,_completedWorkItems,_failedWorkItems));
                        }
                        catch (Exception ex)
                        {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error triggering PoolStopped event: {ex.Message}");
#else
                            Console.WriteLine($"Error triggering PoolStopped event: {ex.Message}");
#endif
                        }
                    }
                }
                catch (Exception ex)
                {
#if UNITY
                    UnityEngine.Debug.LogWarning($"Error during InternalStop(): {ex.Message}");
#else
                    Console.WriteLine($"Error during InternalStop(): {ex.Message}");
#endif
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 安全释放WaitHandle
        /// Safely dispose WaitHandle
        /// </summary>
        private void SafeDisposeWaitHandle(WaitHandle handle)
        {
            if (handle != null)
            {
                try
                {
                    // 直接调用Close()方法，这是WaitHandle的标准释放方式
                    handle.Close();
                }
                catch (ObjectDisposedException)
                {
                    // 忽略已经被释放的情况
                }
                catch (NotSupportedException)
                {
                    // 某些WaitHandle实现可能不支持Close
                    try
                    {
                        var disposable = handle as IDisposable;
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // 忽略已经被释放的情况
                    }
                    catch (Exception)
                    {
                        // 忽略其他释放异常，确保Dispose过程不会中断
                    }
                }
                catch (Exception)
                {
                    // 忽略其他释放异常，确保Dispose过程不会中断
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposing = true;

                    try
                    {
                        // 先停止线程池，确保所有线程都正确停止
                        InternalStop();

                        // 清理结果缓存
                        lock (_resultCacheLock)
                        {
                            _resultCache.Clear();
                        }
                        
                        // 清理超时线程引用
                        lock (_timeoutThreadsLock)
                        {
                            _timeoutThreads.Clear();
                        }

                        // 安全地释放WaitHandle，避免ObjectDisposedException
                        SafeDisposeWaitHandle(_waitAllSignal);
                        SafeDisposeWaitHandle(_pauseSignal);
                    }
                    catch (Exception ex)
                    {
                        // 记录Dispose过程中的异常，但不抛出
#if UNITY
                        UnityEngine.Debug.LogWarning($"PowerPool dispose error: {ex.Message}");
#else
                        Console.WriteLine($"PowerPool dispose error: {ex.Message}");
#endif
                    }
                    finally
                    {
                        // 确保状态始终被正确设置
                        _disposed = true;
                        _disposing = false;
                    }
                }
                else
                {
                    // 从析构函数调用，不管理其他对象
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// 等待暂停信号的公共接口 / Public interface for waiting pause signal
        /// </summary>
        /// <returns>是否成功等待 / Whether wait was successful</returns>
        internal bool WaitForPauseSignal()
        {
            if (_disposed)
                return false;
            
            try
            {
                return _pauseSignal.WaitOne(0); // 非阻塞检查
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// 注册超时线程
        /// Register timeout thread
        /// </summary>
        internal void RegisterTimeoutThread(WorkID workId, Thread thread)
        {
            lock (_timeoutThreadsLock)
            {
                if (!_timeoutThreads.ContainsKey(workId))
                {
                    _timeoutThreads[workId] = thread;
                }
            }
        }

        /// <summary>
        /// 清理死亡的超时线程
        /// Cleanup dead timeout threads
        /// </summary>
        private void CleanupTimeoutThreads()
        {
            lock (_timeoutThreadsLock)
            {
                var deadThreads = new List<WorkID>();
                foreach (var kvp in _timeoutThreads)
                {
                    if (kvp.Value == null || !kvp.Value.IsAlive)
                    {
                        deadThreads.Add(kvp.Key);
                    }
                }
                
                foreach (var id in deadThreads)
                {
                    _timeoutThreads.Remove(id);
                }
            }
        }

        /// <summary>
        /// 添加执行时间统计 / Add execution time statistics
        /// </summary>
        /// <param name="executeTime">执行时间 / Execution time</param>
        internal void AddExecuteTime(long executeTime)
        {
            InterlockedHelper.Add(ref _totalExecuteTime,executeTime);
            
            // 定期清理超时线程（每100次执行清理一次）
            if (_completedWorkItems % 100 == 0)
            {
                CleanupTimeoutThreads();
            }
        }

        /// <summary>
        /// 析构函数
        /// Destructor
        /// </summary>
        ~PowerPool()
        {
            Dispose(false);
        }
    }
}