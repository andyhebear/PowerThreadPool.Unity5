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
using PowerThreadPool_Net20.Logging;
using PowerThreadPool_Net20.Constants;


namespace PowerThreadPool_Net20
{
    /// <summary>
    /// 为Unity5.6和.NET 2.0设计的简化版高效线程池
    /// A simplified efficient thread pool designed for Unity5.6 and .NET 2.0
    /// </summary>
    public partial class PowerPool : IDisposable
    {
        private readonly AtomicFlag _disposed = new AtomicFlag();
        private readonly AtomicFlag _disposing = new AtomicFlag();
        private readonly AtomicFlag _isStarted = new AtomicFlag();
        private readonly InterlockedEnumFlag<PoolStates> _poolState = PoolStates.NotRunning;

        // 日志记录器
        private ILogger _logger;

        // 线程同步对象
        private readonly object _lockObject = new object();
        private readonly ManualResetEvent _waitAllSignal = new ManualResetEvent(false);
        private readonly ManualResetEvent _pauseSignal = new ManualResetEvent(true);

        // 监控线程
        private Thread _monitorThread;
        private readonly AtomicFlag _monitorThreadRunning = new AtomicFlag();

        // 线程和队列管理（使用无锁队列）
        private List<WorkerThread> _workerThreads = new List<WorkerThread>();
        private LockFreePriorityQueue<WorkItem> _workQueue = new LockFreePriorityQueue<WorkItem>(4);
        private LockFreePriorityQueue<WorkItem> _suspendedWorkQueue = new LockFreePriorityQueue<WorkItem>(4);
      

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
        private long _totalExecuteTime = 0;
        private DateTime _startTime;

        // 配置选项
        private PowerPoolOption _options;
        public PowerPoolOption Options {
            get { return _options; }
            set {
                _options = value;
                UpdateWorkerCount();
            }
        }

        /// <summary>
        /// 线程池是否正在运行
        /// Whether the thread pool is running
        /// </summary>
        public bool IsRunning => _poolState.Value == PoolStates.Running;

        /// <summary>
        /// 空闲工作线程数量
        /// Number of idle worker threads
        /// </summary>
        public int IdleWorkerCount {
            get {
                lock (_lockObject) {
                    int count = 0;
                    foreach (WorkerThread worker in _workerThreads) {
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
        public int WaitingWorkCount {
            get {
                lock (_lockObject) {
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
        public long AverageExecuteTime {
            get {
                return _completedWorkItems > 0 ? _totalExecuteTime / _completedWorkItems : 0;
            }
        }

        /// <summary>
        /// 线程池运行时间
        /// Thread pool running duration
        /// </summary>
        public TimeSpan RunningDuration {
            get {
                return IsRunning ? DateTime.Now - _startTime : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 活跃工作线程数量
        /// Number of active worker threads
        /// </summary>
        public int ActiveWorkerThreads {
            get {
                lock (_lockObject) {
                    return _workerThreads.Count;
                }
            }
        }

        /// <summary>
        /// 缓存结果数量
        /// Number of cached results
        /// </summary>
        public int CachedResultCount {
            get {
                lock (_resultCacheLock) {
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
        private bool IsWorkResultCached(WorkID workId) {
            lock (_resultCacheLock) {
                return _resultCache.ContainsKey(workId);
            }
        }

        /// <summary>
        /// 检查工作项是否已完成（包括在队列中、执行中或已完成）
        /// Check if work item is completed (including queued, executing, or finished)
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否已完成 / Whether completed</returns>
        private bool IsWorkCompleted(WorkID workId) {
            // 如果结果已缓存，说明已完成
            if (IsWorkResultCached(workId))
                return true;

            lock (_lockObject) {
                // 如果还在队列中，说明未完成
                if (IsWorkQueued(workId))
                    return false;

                // 检查是否有线程正在执行
                foreach (WorkerThread worker in _workerThreads) {
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
        private bool IsWorkQueued(WorkID workId) {
            lock (_lockObject) {
                // 检查主队列（使用枚举器）
                foreach (WorkItem work in _workQueue) {
                    if (work != null && work.ID.Equals(workId))
                        return true;
                }

                // 检查挂起队列（使用枚举器）
                foreach (WorkItem work in _suspendedWorkQueue) {
                    if (work != null && work.ID.Equals(workId))
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
        public PowerPool() : this(null,null) {
        }

        /// <summary>
        /// 使用指定选项的构造函数
        /// Constructor with specified options
        /// </summary>
        public PowerPool(PowerPoolOption options) : this(options,null) {
        }

        /// <summary>
        /// 使用指定选项和日志记录器的构造函数
        /// Constructor with specified options and logger
        /// </summary>
        public PowerPool(PowerPoolOption options,ILogger logger) {
            _options = options ?? new PowerPoolOption();
            _logger = logger ?? LoggerFactory.CreateDefaultLogger();
            _logger.Info($"PowerPool created with MaxThreads: {_options.MaxThreads}, MinThreads: {_options.MinThreads}");

        }

        /// <summary>
        /// [核心]创建完PowerPool后启动线程池才能工作
        /// Start the thread pool
        /// </summary>
        public void Start() {
            CheckDisposed();

            if (IsRunning)
                return;

            lock (_lockObject) {
                if (IsRunning)
                    return;

                _poolState.InterlockedValue = (PoolStates.Running);
                _startTime = DateTime.Now;

                // 重置统计信息
                _totalWorkItems = 0;
                _completedWorkItems = 0;
                _failedWorkItems = 0;               
                _totalExecuteTime = 0;

                // 创建工作线程
                CreateWorkerThreads();

                // 启动监控线程
                StartMonitorThread();

                // 启动挂起的工作
                StartSuspendedWork();

                // 检查初始空闲状态
                CheckPoolIdle();

                // 记录启动日志
                _logger.Info($"PowerPool started with {_workerThreads.Count} worker threads");

                // 触发启动事件
                if (PoolStarted != null) {
                    PoolStarted(this,new PoolStartedEventArgs(DateTime.Now));
                }
            }
        }

        /// <summary>
        /// 停止线程池
        /// Stop the thread pool
        /// </summary>
        public void Stop() {
            // 在Dispose过程中调用时，允许Stop()执行
            if (!_disposing.Value) {
                CheckDisposed();
            }

            InternalStop();
        }

        /// <summary>
        /// 暂停线程池
        /// Pause the thread pool
        /// </summary>
        public void Pause() {
            CheckDisposed();
            _pauseSignal.Reset();
        }

        /// <summary>
        /// 恢复线程池
        /// Resume the thread pool
        /// </summary>
        public void Resume() {
            CheckDisposed();
            _pauseSignal.Set();
        }

        /// <summary>
        /// 队列工作项
        /// Queue a work item
        /// </summary>
        public WorkID QueueWorkItem(Action action,WorkOption option = null) {
            return QueueWorkItem<object>(() => {
                action();
                return null;
            },option);
        }

        /// <summary>
        /// 队列工作项（带返回值）
        /// Queue a work item with return value
        /// </summary>
        public WorkID QueueWorkItem<T>(Func<T> func,WorkOption option = null) {
            CheckDisposed();

            option = option ?? new WorkOption();
            WorkID workID = new WorkID(true);
            WorkItem workItem = new WorkItem(workID,func,option,this);

            // 转换优先级：WorkPriority (3=最高) -> 队列索引 (0=最高)
            int queuePriority = ConvertPriority(option.Priority);

            lock (_lockObject) {
                // 检查队列限制
                if (!_options.StartSuspended && _workQueue.Count >= _options.ThreadQueueLimit) {
                    _logger.Error($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
                    throw new InvalidOperationException($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
                }

                if (_options.StartSuspended) {
                    _suspendedWorkQueue.Enqueue(workItem,queuePriority);
                    _logger.Debug($"WorkItem {workID} queued to suspended queue with priority {option.Priority} (index: {queuePriority})");
                }
                else {
                    _workQueue.Enqueue(workItem,queuePriority);
                    _totalWorkItems++;

                    // 弹性扩容：如果队列积压较多且线程未达上限，动态增加工作线程
                    TryExpandThreadsElastic();

                    // 重置WaitAll信号，因为有新工作入队
                    _waitAllSignal.Reset();

                    // 通知空闲线程
                    Monitor.PulseAll(_lockObject);

                    _logger.Debug($"WorkItem {workID} queued with priority {option.Priority} (index: {queuePriority}), total in queue: {_workQueue.Count}");
                }
            }

            return workID;
        }

        /// <summary>
        /// 弹性扩容：根据队列负载动态增加工作线程
        /// Elastic expansion: dynamically increase worker threads based on queue load
        /// </summary>
        private void TryExpandThreadsElastic() {
            // 获取当前工作线程数
            int currentThreadCount = _workerThreads.Count;
            int maxThreads = _options.MaxThreads;

            // 如果已经达到最大线程数，无需扩容
            if (currentThreadCount >= maxThreads)
                return;

            // 计算空闲线程数
            int idleCount = 0;
            foreach (WorkerThread worker in _workerThreads) {
                if (worker.IsIdle)
                    idleCount++;
            }

            // 计算队列积压程度
            int queueSize = _workQueue.Count;

            // 扩容条件：
            // 1. 没有空闲线程（所有线程都在工作）
            // 2. 队列中有足够多的积压工作项（超过线程数的50%）
            // 3. 还可以增加线程（未达MaxThreads上限）
            if (idleCount == 0 && queueSize > currentThreadCount / 2) {
                // 计算需要增加的线程数（每次最多增加1个，避免过度扩容）
                int threadsToAdd = 1;

                // 确保不超过最大线程数
                if (currentThreadCount + threadsToAdd > maxThreads)
                    threadsToAdd = maxThreads - currentThreadCount;

                if (threadsToAdd > 0) {
                    // 动态创建新线程
                    for (int i = 0; i < threadsToAdd; i++) {
                        WorkerThread worker = new WorkerThread(this,currentThreadCount + i);
                        _workerThreads.Add(worker);
                        worker.Start();
                        _logger.Info($"Elastic expansion: Added worker thread {worker.ThreadId}. Total threads: {_workerThreads.Count}");
                    }
                }
            }
        }

        /// <summary>
        /// 内部队列工作项方法（不加锁，供批量操作使用）
        /// Internal queue work item method (without lock, for batch operations)
        /// </summary>
        private WorkID QueueWorkItemInternal(Action action,WorkOption option) {
            option = option ?? new WorkOption();
            WorkID workID = new WorkID(true);
            WorkItem workItem = new WorkItem(workID,action,option,this);

            // 转换优先级
            int queuePriority = ConvertPriority(option.Priority);

            // 检查队列限制
            if (!_options.StartSuspended && _workQueue.Count >= _options.ThreadQueueLimit) {
                _logger.Error($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
                throw new InvalidOperationException($"Thread queue limit ({_options.ThreadQueueLimit}) exceeded. Current queue size: {_workQueue.Count}");
            }

            if (_options.StartSuspended) {
                _suspendedWorkQueue.Enqueue(workItem,queuePriority);
                _logger.Debug($"Internal WorkItem {workID} queued to suspended queue with priority {option.Priority} (index: {queuePriority})");
            }
            else {
                _workQueue.Enqueue(workItem,queuePriority);
                _totalWorkItems++;

                // 弹性扩容：如果队列积压较多且线程未达上限，动态增加工作线程
                TryExpandThreadsElastic();

                // 重置WaitAll信号，因为有新工作入队
                _waitAllSignal.Reset();

                // 通知空闲线程
                Monitor.PulseAll(_lockObject);

                _logger.Debug($"Internal WorkItem {workID} queued with priority {option.Priority} (index: {queuePriority}), total in queue: {_workQueue.Count}");
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
        public WorkID[] ParallelFor(int start,int end,Action<int> body,int step = 1) {
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
            // 确保批次大小不会导致空批次
            if (batchSize * step > (end - start))
                batchSize = Math.Max(1,(end - start + step - 1) / step);

            var workIds = new List<WorkID>();
            //开启最大线程数
            Update2MaxWorkersCount();
            lock (_lockObject) {
                for (int i = start; i < end; i += batchSize * step) {
                    int batchStart = i;
                    int batchEnd = Math.Min(i + batchSize * step,end);

                    // 创建批处理工作项
                    WorkID workId = QueueWorkItemInternal(() => {
                        for (int j = batchStart; j < batchEnd; j += step) {
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
        public ExecuteResult GetResult(WorkID workId) {
            CheckDisposed();

            // 首先检查结果缓存
            lock (_resultCacheLock) {
                ExecuteResult result;
                if (_resultCache.TryGetValue(workId,out result)) {
                    return result;
                }
            }

            // 检查工作是否还在队列中
            if (IsWorkQueued(workId)) {
                // 还在队列中
                throw new InvalidOperationException($"Work {workId} is still queued");
            }

            // 检查是否有工作线程正在执行该工作项
            bool isExecuting = false;
            lock (_lockObject) {
                foreach (WorkerThread worker in _workerThreads) {
                    if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workId)) {
                        isExecuting = true;
                        break;
                    }
                }
            }

            if (isExecuting) {
                // 正在执行中
                throw new InvalidOperationException($"Work {workId} is still executing");
            }
            else {
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
        public ExecuteResult[] GetResults(params WorkID[] workIds) {
            CheckDisposed();

            if (workIds == null)
                throw new ArgumentNullException(nameof(workIds));

            var results = new ExecuteResult[workIds.Length];
            for (int i = 0; i < workIds.Length; i++) {
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
        public ExecuteResult GetResultAndWait(WorkID workId,int timeoutMs = 30000) {
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
        public ExecuteResult[] GetResultsAndWait(WorkID[] workIds,int timeoutMs = 30000) {
            CheckDisposed();

            // 等待工作完成
            WaitWorks(workIds,timeoutMs);

            // 获取所有结果
            return GetResults(workIds);
        }


        /// <summary>
        /// 清除指定的执行结果缓存
        /// Clear specified execution result cache
        /// </summary>
        /// <param name="workId">工作ID / Work ID</param>
        /// <returns>是否成功清除 / Whether successfully cleared</returns>
        public bool ClearResult(WorkID workId) {
            CheckDisposed();

            lock (_resultCacheLock) {
                return _resultCache.Remove(workId);
            }
        }

        /// <summary>
        /// 批量清除执行结果缓存
        /// Batch clear execution result cache
        /// </summary>
        /// <param name="workIds">工作ID数组 / Work ID array</param>
        /// <returns>成功清除的数量 / Number of successfully cleared items</returns>
        public int ClearResults(params WorkID[] workIds) {
            CheckDisposed();

            if (workIds == null)
                return 0;

            int clearedCount = 0;
            lock (_resultCacheLock) {
                foreach (WorkID workId in workIds) {
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
        public int ClearExpiredResults(int maxAgeMs) {
            CheckDisposed();

            if (maxAgeMs <= 0)
                return 0;

            DateTime cutoffTime = DateTime.Now.AddMilliseconds(-maxAgeMs);
            List<WorkID> expiredIds = new List<WorkID>();

            lock (_resultCacheLock) {
                foreach (var kvp in _resultCache) {
                    if (kvp.Value.EndTime < cutoffTime)
                        expiredIds.Add(kvp.Key);
                }

                foreach (WorkID id in expiredIds) {
                    _resultCache.Remove(id);
                }
            }

            return expiredIds.Count;
        }

        /// <summary>
        /// 清除所有执行结果缓存
        /// Clear all execution result cache
        /// </summary>
        public void ClearAllResults() {
            CheckDisposed();

            lock (_resultCacheLock) {
                _resultCache.Clear();
            }
        }



        /// <summary>
        /// 获取工作执行状态摘要
        /// Get work execution status summary
        /// </summary>
        /// <returns>状态摘要信息 / Status summary information</returns>
        public WorkStatusSummary GetWorkStatusSummary() {
            CheckDisposed();

            lock (_lockObject) {
                return new WorkStatusSummary {
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
        public WorkID[] ParallelForEach<T>(IEnumerable<T> source,Action<T> body) {
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
            // 确保至少有一个批次
            if (batchSize > items.Length)
                batchSize = items.Length;

            var workIds = new List<WorkID>();
            //开启最大线程数
            Update2MaxWorkersCount();
            lock (_lockObject) {
                for (int i = 0; i < items.Length; i += batchSize) {
                    int batchStart = i;
                    int batchEnd = Math.Min(i + batchSize,items.Length);
                    var batch = new T[batchEnd - batchStart];
                    Array.Copy(items,batchStart,batch,0,batch.Length);

                    // 创建批处理工作项
                    WorkID workId = QueueWorkItemInternal(() => {
                        foreach (var item in batch) {
                            body(item);
                        }
                    },null);

                    workIds.Add(workId);
                }
            }

            return workIds.ToArray();
        }

        private T[] ToArray<T>(IEnumerable<T> source) {
            List<T> result = new List<T>();
            foreach (var item in source) {
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
        public WorkID[] ParallelInvoke(params Action[] actions) {
            CheckDisposed();

            if (actions == null)
                throw new ArgumentNullException(nameof(actions));

            if (actions.Length == 0)
                return new WorkID[0];

            var workIds = new WorkID[actions.Length];

            lock (_lockObject) {
                for (int i = 0; i < actions.Length; i++) {
                    int index = i; // 避免闭包问题
                    if (actions[index] != null) {
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
        public void WaitAll() {
            CheckDisposed();

            // 首先检查当前是否已经空闲
            CheckPoolIdle();

            // 然后等待信号，增加异常处理
            try {
                _waitAllSignal.WaitOne();
            }
            catch (ObjectDisposedException) {
                // 如果信号已被释放，检查是否真的完成了所有工作
                if (WaitingWorkCount == 0 && IdleWorkerCount == ActiveWorkerThreads) {
                    return; // 已经完成，直接返回
                }
                throw;
            }
        }

        /// <summary>
        /// 等待指定的工作完成
        /// Wait for specified work to complete
        /// </summary>
        public void WaitWork(WorkID workID,int timeoutMs = 30000) {
            CheckDisposed();

            DateTime startTime = DateTime.Now;
            while (DateTime.Now - startTime < TimeSpan.FromMilliseconds(timeoutMs)) {
                // 首先检查结果缓存 - 如果有结果说明已完成
                lock (_resultCacheLock) {
                    if (_resultCache.ContainsKey(workID)) {
                        return; // 工作已完成，结果已缓存
                    }
                }

                bool isCompleted = true;
                lock (_lockObject) {
                    // 检查指定的工作项是否在队列中
                    if (IsWorkQueued(workID)) {
                        isCompleted = false;
                    }

                    // 检查是否有工作线程正在执行该工作项
                    if (isCompleted) {
                        foreach (WorkerThread worker in _workerThreads) {
                            if (worker.CurrentWorkID != WorkID.Empty && worker.CurrentWorkID.Equals(workID)) {
                                isCompleted = false;
                                break;
                            }
                        }
                    }
                }

                if (isCompleted) {
                    // 工作项已完成，但结果可能还未缓存
                    // 给OnWorkCompleted一些时间来缓存结果
                    Thread.Sleep(10);

                    // 再次检查结果缓存
                    lock (_resultCacheLock) {
                        if (_resultCache.ContainsKey(workID)) {
                            return;
                        }
                    }
                }

                Thread.Sleep(50); // 避免忙等待
            }
            throw new TimeoutException($"Work {workID} did not complete within {timeoutMs}ms timeout period.");
        }
        
        public void WaitWorks(WorkID[] workIds,int timeoutMs = 30000) {
            CheckDisposed();
            if (workIds == null)
                throw new ArgumentNullException(nameof(workIds));

            if (workIds.Length == 0)
                return;

            // 批量等待优化：使用总超时时间等待所有工作
            DateTime startTime = DateTime.Now;
            int remainingTimeout = timeoutMs;
            bool allCompleted = false;
            int currentCompletedCount = 0;
            // 等待所有工作完成（循环直到全部完成或超时）
            while (remainingTimeout > 0) {
                // 检查是否所有工作都已完成
                allCompleted = true;
                currentCompletedCount = 0;

                lock (_resultCacheLock) {
                    foreach (var workId in workIds) {
                        if (_resultCache.ContainsKey(workId)) {
                            currentCompletedCount++;
                        }
                        else {
                            allCompleted = false;
                        }
                    }
                }

                // 如果所有工作都已完成，退出循环
                if (allCompleted) {
                    return;
                }

                // 计算剩余超时时间
                long elapsed = (DateTime.Now - startTime).Ticks / TimeSpan.TicksPerMillisecond;
                remainingTimeout = Math.Max(0,timeoutMs - (int)elapsed);

                if (remainingTimeout <= 0) {
                    break;
                }

                // 短暂等待后重试
                int waitTime = Math.Min(50,remainingTimeout);
                Thread.Sleep(waitTime);
            }

            throw new TimeoutException($"{currentCompletedCount} out of {workIds.Length} works completed within {timeoutMs}ms timeout period.");
        }
        /// <summary>
        /// 等待多个工作完成
        /// Wait for multiple works to complete
        /// </summary>
        /// <param name="workIds">工作ID数组 / Work ID array</param>
        /// <param name="timeoutMs">总超时毫秒数 / Total timeout in milliseconds</param>
        /// <exception cref="ObjectDisposedException">对象已释放</exception>
        /// <exception cref="ArgumentNullException">workIds为null</exception>
        /// <exception cref="TimeoutException">工作在超时时间内未完成</exception>
        public void WaitWorks(ICollection<WorkID> workIds,int timeoutMs = 30000) {
            CheckDisposed();

            if (workIds == null)
                throw new ArgumentNullException(nameof(workIds));

            if (workIds.Count == 0)
                return;
            WorkID[] workIdArray = new WorkID[workIds.Count];
            workIds.CopyTo(workIdArray,0);
            WaitWorks(workIdArray,timeoutMs);
            //// 批量等待优化：使用总超时时间等待所有工作
            //DateTime startTime = DateTime.Now;
            //int remainingTimeout = timeoutMs;
            //bool allCompleted = false;
            //int currentCompletedCount = 0;
            //// 等待所有工作完成（循环直到全部完成或超时）
            //while (remainingTimeout > 0) {
            //    // 检查是否所有工作都已完成
            //    allCompleted = true;
            //    currentCompletedCount = 0;

            //    lock (_resultCacheLock) {
            //        foreach (var workId in workIds) {
            //            if (_resultCache.ContainsKey(workId)) {
            //                currentCompletedCount++;
            //            }
            //            else {
            //                allCompleted = false;
            //            }
            //        }
            //    }

            //    // 如果所有工作都已完成，退出循环
            //    if (allCompleted) {
            //        return;
            //    }

            //    // 计算剩余超时时间
            //    long elapsed = (DateTime.Now - startTime).Ticks / TimeSpan.TicksPerMillisecond;
            //    remainingTimeout = Math.Max(0,timeoutMs - (int)elapsed);

            //    if (remainingTimeout <= 0) {
            //        break;
            //    }

            //    // 短暂等待后重试
            //    int waitTime = Math.Min(50,remainingTimeout);
            //    Thread.Sleep(waitTime);
            //}

            //throw new TimeoutException($"{currentCompletedCount} out of {workIds.Count} works completed within {timeoutMs}ms timeout period.");
        }

        /// <summary>
        /// 清空队列
        /// Clear the queue
        /// </summary>
        public void ClearQueue() {
            CheckDisposed();
            lock (_lockObject) {
                _workQueue.Clear();
                CheckPoolIdle();
            }
        }

        /// <summary>
        /// 检查线程池是否空闲
        /// Check if the thread pool is idle
        /// </summary>
        private void CheckPoolIdle() {
            bool isIdle = false;
            bool shouldSetSignal = false;

            lock (_lockObject) {
                // 在锁内再次检查状态，避免竞态条件
                if (_disposed.Value || _disposing.Value)
                    return;

                // 检查工作队列是否为空
                bool queueEmpty = _workQueue.Count == 0;

                // 检查所有活跃工作线程是否都空闲
                bool allThreadsIdle = true;
                foreach (WorkerThread worker in _workerThreads) {
                    if (!worker.IsIdle) {
                        allThreadsIdle = false;
                        break;
                    }
                }

                isIdle = queueEmpty && allThreadsIdle;
                shouldSetSignal = isIdle;
            }

            // 在锁外设置信号，避免死锁
            try {
                if (shouldSetSignal) {
                    _waitAllSignal.Set();
                }
                else {
                    _waitAllSignal.Reset();
                }
            }
            catch (ObjectDisposedException) {
                // 忽略对象已释放异常，正常情况
            }
        }

        /// <summary>
        /// 启动监控线程
        /// Start monitor thread
        /// </summary>
        private void StartMonitorThread() {
            if (_monitorThread != null && _monitorThread.IsAlive)
                return;

            _monitorThreadRunning.SetTrue();
            _monitorThread = new Thread(MonitorThreadProc) {
                Name = "PowerPool-Monitor",
                IsBackground = true
            };
            _monitorThread.Start();
        }

        /// <summary>
        /// 停止监控线程
        /// Stop monitor thread
        /// </summary>
        private void StopMonitorThread() {
            if (_monitorThread == null)
                return;

            _monitorThreadRunning.SetFalse();

            // 等待监控线程结束
            if (_monitorThread.IsAlive) {
                _monitorThread.Join(1000); // 最多等待1秒
                if (_monitorThread.IsAlive) {
                    // 如果线程还在运行，强制中断
                    try {
#if !NET20
                        _monitorThread.Abort();
#endif
                    }
                    catch {
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
        private void MonitorThreadProc() {
            DateTime lastThreadCleanupTime = DateTime.Now;
            DateTime lastCacheCleanupTime = DateTime.Now;
            DateTime lastDelayedWorkCheckTime = DateTime.Now;

            // 自适应延迟检查间隔（毫秒）
            // Adaptive delayed work check interval (milliseconds)
            int adaptiveCheckInterval = 10;  // 默认 10ms
            //const int MIN_CHECK_INTERVAL = 10;    // 最小 10ms
            //const int MAX_CHECK_INTERVAL = 1000;  // 最大 1000ms

            while (_monitorThreadRunning.Value && IsRunning) {
                try {
                    // 检查线程池空闲状态
                    CheckPoolIdle();

                    // 每30秒执行一次线程回收检查
                    if (DateTime.Now - lastThreadCleanupTime >= TimeSpan.FromSeconds(30)) {
                        CleanupIdleThreads();
                        lastThreadCleanupTime = DateTime.Now;
                    }

                    // 每60秒执行一次结果缓存清理
                    if (DateTime.Now - lastCacheCleanupTime >= TimeSpan.FromSeconds(60)) {
                        CleanupExpiredResults();
                        lastCacheCleanupTime = DateTime.Now;
                    }

                    // 自适应检查延迟工作
                    // Adaptive check for delayed work
                    DateTime now = DateTime.Now;
                    if (now - lastDelayedWorkCheckTime >= TimeSpan.FromMilliseconds(adaptiveCheckInterval)) {
                        CheckAndResumeDelayedWorks();
                        // 计算下一个检查间隔（自适应）
                        // Calculate next check interval (adaptive)
                        //adaptiveCheckInterval = CalculateAdaptiveCheckInterval(now, MIN_CHECK_INTERVAL, MAX_CHECK_INTERVAL);
                        lastDelayedWorkCheckTime = now;
                    }

                    // 使用自适应等待机制
                    // Use adaptive wait mechanism
                    Monitor.Wait(_lockObject, Math.Min(adaptiveCheckInterval, 50));
                }
                catch (ThreadAbortException) {
                    // 线程被中止，正常退出
                    break;
                }
                catch (ThreadInterruptedException) {
                    // 线程被中断，正常退出
                    break;
                }
                catch (SynchronizationLockException) {
                    // 如果没有获得锁，使用Thread.Sleep作为备选
                    Thread.Sleep(50);
                }
                catch (Exception ex) {
                    // 记录详细的异常日志
#if UNITY
                    UnityEngine.Debug.LogWarning($"PowerPool monitor thread error: {ex.Message}\nStack Trace: {ex.StackTrace}");
#else
                    Console.WriteLine($"PowerPool monitor thread error: {ex.Message}\nStack Trace: {ex.StackTrace}");
#endif
                }
            }
        }
             

        /// <summary>
        /// 清理过期的结果缓存
        /// Cleanup expired result cache
        /// </summary>
        private void CleanupExpiredResults() {
            try {
                if (!_options.EnableResultCacheExpiration || _options.ResultCacheExpiration <= TimeSpan.Zero)
                    return;

                lock (_resultCacheLock) {
                    DateTime now = DateTime.Now;
                    List<WorkID> expiredKeys = new List<WorkID>();

                    // 查找所有过期的结果
                    foreach (var kvp in _resultCache) {
                        TimeSpan age = now - kvp.Value.EndTime;
                        if (age >= _options.ResultCacheExpiration) {
                            expiredKeys.Add(kvp.Key);
                        }
                    }

                    // 清理过期结果
                    foreach (var key in expiredKeys) {
                        _resultCache.Remove(key);
                    }

                    // 记录清理信息（可选）
#if DEBUG
                    if (expiredKeys.Count > 0) {
#if UNITY
                        UnityEngine.Debug.Log($"PowerPool cleaned up {expiredKeys.Count} expired results from cache");
#else
                        Console.WriteLine($"PowerPool cleaned up {expiredKeys.Count} expired results from cache");
#endif
                    }
#endif
                }
            }
            catch (Exception ex) {
                // 记录缓存清理异常
#if UNITY
                UnityEngine.Debug.LogWarning($"PowerPool cache cleanup error: {ex.Message}\nStack Trace: {ex.StackTrace}");
#else
                Console.WriteLine($"PowerPool cache cleanup error: {ex.Message}\nStack Trace: {ex.StackTrace}");
#endif
            }
        }

        /// <summary>
        /// 清理空闲线程
        /// Clean up idle threads
        /// </summary>
        private void CleanupIdleThreads() {
            lock (_lockObject) {
                // 确保不低于最小线程数
                if (_workerThreads.Count <= _options.MinThreads)
                    return;

                List<WorkerThread> threadsToRemove = new List<WorkerThread>();
                DateTime now = DateTime.Now;

                // 查找超过空闲超时时间的线程
                foreach (WorkerThread worker in _workerThreads) {
                    if (worker.IsIdle && now - worker.IdleStartTime >= _options.IdleThreadTimeout) {
                        threadsToRemove.Add(worker);

                        // 确保不低于最小线程数
                        if (_workerThreads.Count - threadsToRemove.Count <= _options.MinThreads)
                            break;
                    }
                }

                // 停止并移除这些线程
                foreach (WorkerThread worker in threadsToRemove) {
                    worker.Stop();
                    _workerThreads.Remove(worker);
                }

                // 通知所有等待的线程
                if (threadsToRemove.Count > 0) {
                    Monitor.PulseAll(_lockObject);

                    // 在锁外等待线程真正停止
                    foreach (var worker in threadsToRemove) {
                        try {
                            worker.Thread.Join(100); // 最多等待100ms
                        }
                        catch (Exception ex) {
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
        private void CreateWorkerThreads() {
            int threadCount = _options.MinThreads;

            for (int i = 0; i < threadCount; i++) {
                WorkerThread worker = new WorkerThread(this,i);
                _workerThreads.Add(worker);
                worker.Start();
            }
        }

        /// <summary>
        /// 更新工作线程数量
        /// Update worker thread count
        /// </summary>
        private void UpdateWorkerCount() {
            if (!IsRunning)
                return;

            lock (_lockObject) {
                int currentCount = _workerThreads.Count;
                int maxCount = _options.MaxThreads;
                int minCount = _options.MinThreads;
                if (minCount > currentCount) {
                    // 确保不低于最小线程数
                    // 添加新线程
                    for (int i = currentCount; i < minCount; i++) {
                        WorkerThread worker = new WorkerThread(this,i);
                        _workerThreads.Add(worker);
                        worker.Start();
                    }
                }
                else if (maxCount < currentCount) {
                    int minAllowed = _options.MinThreads;
                    if (currentCount <= minAllowed)
                        return;

                    // 减少线程（标记要停止的线程并等待完成）
                    int targetAfterReduction = Math.Max(maxCount,minAllowed);
                    int toStop = currentCount - targetAfterReduction;
                    var threadsToStop = new List<WorkerThread>();

                    // 从末尾开始标记要停止的线程
                    for (int i = 0; i < toStop && _workerThreads.Count > minAllowed; i++) {
                        int index = _workerThreads.Count - 1;
                        WorkerThread worker = _workerThreads[index];
                        worker.MarkForStop();
                        threadsToStop.Add(worker);
                        _workerThreads.RemoveAt(index); // 立即从列表中移除
                    }

                    // 通知所有等待的线程
                    Monitor.PulseAll(_lockObject);

                    // 在锁外等待线程真正停止
                    foreach (var worker in threadsToStop) {
                        worker.Join();
                    }
                }
            }
        }
        /// <summary>
        /// 使用最大线程数（避免频繁创建线程，只在必要时扩充）
        /// Ensure worker threads reach max count (avoid frequent thread creation, only expand when necessary)
        /// </summary>
        private void Update2MaxWorkersCount() {
            if (!IsRunning)
                return;

            lock (_lockObject) {
                int currentCount = _workerThreads.Count;
                int maxCount = _options.MaxThreads;

                // 限制单次扩充的最大线程数，避免过度创建
                int maxThreadsToAdd = Math.Min(3,maxCount - currentCount);

                if (maxThreadsToAdd > 0) {
                    // 添加新线程（分批添加，避免一次性创建过多线程）
                    for (int i = currentCount; i < currentCount + maxThreadsToAdd && i < maxCount; i++) {
                        WorkerThread worker = new WorkerThread(this,i);
                        _workerThreads.Add(worker);
                        worker.Start();
                    }
                    _logger.Info($"Expanded worker threads for parallel operation: from {currentCount} to {_workerThreads.Count}");
                }
            }
        }
        /// <summary>
        /// 启动挂起的工作
        /// Start suspended work
        /// </summary>
        private void StartSuspendedWork() {
            List<WorkItem> suspendedItems = new List<WorkItem>();

            // 从无锁队列中提取所有挂起的工作项
            while (_suspendedWorkQueue.TryDequeue(out WorkItem workItem)) {
                suspendedItems.Add(workItem);
            }

            if (suspendedItems.Count > 0) {
                lock (_lockObject) {
                    // 将挂起的工作项移到主队列
                    foreach (var item in suspendedItems) {
                        _workQueue.Enqueue(item,(int)item.Option.Priority);
                        _totalWorkItems++;
                    }

                    Monitor.PulseAll(_lockObject);
                    _logger.Info($"Resumed {suspendedItems.Count} suspended work items");
                }
            }
        }

        /// <summary>
        /// 工作线程获取工作项
        /// Worker thread gets work item
        /// </summary>
        internal WorkItem GetWorkItem() {
            while (IsRunning && !_disposed.Value) {
                // 首先尝试从无锁队列获取工作项
                if (_workQueue.TryDequeue(out WorkItem workItem)) {
                    // 添加 null 检查，防止返回 null 值
                    //if (workItem != null) {
                        _logger.Debug($"WorkItem {workItem.ID} dequeued for execution");
                        return workItem;
                    //}
                    //else {
                    //    // 如果返回了 null，记录警告并继续尝试
                    //    _logger.Warning("TryDequeue returned null WorkItem, retrying...");
                    //    continue;
                    //}
                }

                // 如果队列为空，等待一段时间后重试
                lock (_lockObject) {
                    // 双重检查，防止在获取锁的过程中状态发生变化
                    if (!IsRunning || _disposed.Value)
                        break;

                    // 再次检查，可能在等待期间有新工作项加入
                    if (_workQueue.TryDequeue(out workItem)) {
                        // 添加 null 检查
                        if (workItem != null) {
                            _logger.Debug($"WorkItem {workItem.ID} dequeued after wait");
                            return workItem;
                        }
                        else {
                            _logger.Warning("TryDequeue returned null WorkItem after wait, retrying...");
                            continue;
                        }
                    }

                    // 等待新工作项的通知
                    Monitor.Wait(_lockObject,50);
                }
            }

            return null;
        }

        /// <summary>
        /// 工作完成回调
        /// Work completed callback
        /// </summary>
        internal void OnWorkCompleted(WorkItem workItem,object result,Exception exception,int retryCount = 0) {
            DateTime completionTime = DateTime.Now;
            DateTime startTime = workItem.CreateTime;
            ExecuteResult executeResult = null;

            // 创建ExecuteResult并缓存
            if (exception != null) {
                InterlockedHelper.Add(ref _failedWorkItems,1);

                // 判断异常类型
                if (exception is TimeoutException) {
                    executeResult = new ExecuteResult(workItem.ID,startTime,completionTime,exception);
                }
                else if (exception is OperationCanceledException) {
                    executeResult = new ExecuteResult(workItem.ID,startTime,completionTime);
                }
                else {
                    executeResult = new ExecuteResult(workItem.ID,exception,startTime,completionTime,retryCount);
                }

                if (WorkFailed != null) {
                    // 判断是否为取消导致的失败
                    bool isCanceled = (exception is OperationCanceledException);
                    bool isTimeout = (exception is TimeoutException);
                    WorkFailedEventArgs args = new WorkFailedEventArgs(workItem.ID,exception,completionTime,isCanceled,isTimeout);
                    WorkFailed(this,args);
                }
            }
            else {
                InterlockedHelper.Add(ref _completedWorkItems,1);
                executeResult = new ExecuteResult(workItem.ID,result,startTime,completionTime,retryCount);

                if (WorkCompleted != null) {
                    WorkCompletedEventArgs args = new WorkCompletedEventArgs(workItem.ID,result,completionTime);
                    WorkCompleted(this,args);
                }
            }

            // 缓存结果
            CacheExecuteResult(workItem.ID,executeResult);

            // 记录完成日志
            if (exception != null) {
                string retryMsg = retryCount > 0 ? $" after {retryCount} retries" : "";
                _logger.Warning($"WorkItem {workItem.ID} failed{retryMsg} with exception: {exception.Message}");
            }
            else {
                string retryMsg = retryCount > 0 ? $" after {retryCount} retries" : "";
                _logger.Debug($"WorkItem {workItem.ID} completed successfully{retryMsg}");
            }

            CheckPoolIdle();
        }

        /// <summary>
        /// 缓存执行结果
        /// Cache execution result
        /// </summary>
        private void CacheExecuteResult(WorkID workId,ExecuteResult result) {
            lock (_resultCacheLock) {
                _resultCache[workId] = result;
            }
        }

        /// <summary>
        /// 检查是否已释放
        /// Check if disposed
        /// </summary>
        private void CheckDisposed() {
            if (_disposed.Value || _disposing.Value)
                throw new ObjectDisposedException("PowerPool");
        }

        /// <summary>
        /// 将WorkPriority转换为LockFreePriorityQueue的优先级索引
        /// Convert WorkPriority to LockFreePriorityQueue priority index
        /// 注意：WorkPriority中Critical=3(最高)，Low=0(最低)
        /// 但LockFreePriorityQueue中0=最高，priorityCount-1=最低
        /// 需要反向转换
        /// </summary>
        /// <param name="priority">工作优先级</param>
        /// <returns>队列优先级索引</returns>
        private int ConvertPriority(WorkPriority priority) {
            // WorkPriority: Low=0, Normal=1, High=2, Critical=3
            // LockFreePriorityQueue: 0=最高(Critical), 1=高, 2=普通, 3=低
            // 转换公式: 3 - WorkPriority值
            return 3 - (int)priority;
        }

        /// <summary>
        /// 内部停止方法（Dispose调用，不检查Disposed状态）
        /// Internal stop method (called by Dispose, without disposed check)
        /// </summary>
        private void InternalStop() {
            if (!IsRunning)
                return;

            lock (_lockObject) {
                if (!IsRunning)
                    return;

                _poolState.InterlockedValue = (PoolStates.NotRunning);

                try {
                    // 停止监控线程
                    StopMonitorThread();

                    // 停止所有工作线程
                    foreach (WorkerThread worker in _workerThreads) {
                        try {
                            worker.Stop();
                        }
                        catch (Exception ex) {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error stopping worker thread {worker.ThreadId}: {ex.Message}");
#else
                            Console.WriteLine($"Error stopping worker thread {worker.ThreadId}: {ex.Message}");
#endif
                        }
                    }

                    // 等待所有线程完成，设置更长的超时时间
                    foreach (WorkerThread worker in _workerThreads) {
                        try {
                            worker.Join();

                            // 检查线程是否真的停止了
                            if (worker.Thread != null && worker.Thread.IsAlive) {
#if UNITY
                                Debug.LogWarning($"Worker thread {worker.ThreadId} did not stop gracefully after timeout");
#else
                                Console.WriteLine($"Worker thread {worker.ThreadId} did not stop gracefully after timeout");
#endif
                            }
                        }
                        catch (Exception ex) {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#else
                            Console.WriteLine($"Error joining worker thread {worker.ThreadId}: {ex.Message}");
#endif
                        }
                    }

                    _workerThreads.Clear();

                    // 触发停止事件
                    if (PoolStopped != null) {
                        try {
                            PoolStopped(this,new PoolStoppedEventArgs(DateTime.Now,_completedWorkItems,_failedWorkItems));
                        }
                        catch (Exception ex) {
#if UNITY
                            UnityEngine.Debug.LogWarning($"Error triggering PoolStopped event: {ex.Message}");
#else
                            Console.WriteLine($"Error triggering PoolStopped event: {ex.Message}");
#endif
                        }
                    }
                }
                catch (Exception ex) {
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
        public void Dispose() {
            _logger.Info($"PowerPool disposing. Final stats: Completed: {_completedWorkItems}, Failed: {_failedWorkItems}");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 安全释放WaitHandle
        /// Safely dispose WaitHandle
        /// </summary>
        private void SafeDisposeWaitHandle(WaitHandle handle) {
            if (handle != null) {
                try {
                    // 直接调用Close()方法，这是WaitHandle的标准释放方式
                    handle.Close();
                }
                catch (ObjectDisposedException) {
                    // 忽略已经被释放的情况
                }
                catch (NotSupportedException) {
                    // 某些WaitHandle实现可能不支持Close
                    try {
                        //var disposable = handle as IDisposable;
                        //if (disposable != null) {
                        //    disposable.Dispose();
                        //}
                        handle.Close();
                    }
                    catch (ObjectDisposedException) {
                        // 忽略已经被释放的情况
                    }
                    catch (Exception) {
                        // 忽略其他释放异常，确保Dispose过程不会中断
                    }
                }
                catch (Exception) {
                    // 忽略其他释放异常，确保Dispose过程不会中断
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!_disposed.Value) {
                if (disposing) {
                    _disposing.SetTrue();

                    try {
                        // 先停止线程池，确保所有线程都正确停止
                        InternalStop();

                        // 清理结果缓存
                        lock (_resultCacheLock) {
                            _resultCache.Clear();
                        }

                        // 清理超时线程引用
                        lock (_timeoutThreadsLock) {
                            _timeoutThreads.Clear();
                        }

                        // 清理分组字典（新增：防止内存泄漏）
                        SafeDisposeGroups();

                        // 清理延迟工作字典 & 释放工作调度器                 
                        SafeDisposeScheduler();
                        //                        

                        // 释放无锁队列资源
                        _workQueue?.Dispose();
                        _suspendedWorkQueue?.Dispose();

                        // 安全地释放WaitHandle，避免ObjectDisposedException
                        SafeDisposeWaitHandle(_waitAllSignal);
                        SafeDisposeWaitHandle(_pauseSignal);

                    }
                    catch (Exception ex) {
                        // 记录Dispose过程中的异常，但不抛出
                        _logger.Error($"PowerPool dispose error: {ex.Message}",ex);
                    }
                    finally {
                        // 确保状态始终被正确设置
                        _disposed.SetTrue();
                        _disposing.SetFalse();
                        _logger.Info("PowerPool dispose completed");
                    }
                }
                else {
                    // 从析构函数调用，不管理其他对象
                    _disposed.SetTrue();
                }
                // 安全释放Logger（如果实现了IDisposable）
                SafeDisposeLogger(_logger);
            }
        }

        /// <summary>
        /// 安全释放Logger（如果实现了IDisposable）
        /// Safely dispose logger (if it implements IDisposable)
        /// </summary>
        private void SafeDisposeLogger(ILogger logger) {
            if (logger != null && logger is IDisposable) {
                try {
                    (logger as IDisposable).Dispose();
                }
                catch (ObjectDisposedException) {
                    // 忽略已经被释放的情况
                }
                catch (Exception ex) {
                    // 记录其他释放异常，但不抛出
                    Console.WriteLine($"Error disposing logger: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 等待暂停信号的公共接口 / Public interface for waiting pause signal
        /// </summary>
        /// <returns>是否成功等待 / Whether wait was successful</returns>
        internal bool WaitForPauseSignal() {
            if (_disposed.Value)
                return false;

            try {
                return _pauseSignal.WaitOne(0); // 非阻塞检查
            }
            catch (ObjectDisposedException) {
                return false;
            }
        }

        /// <summary>
        /// 注册超时线程
        /// Register timeout thread
        /// </summary>
        internal void RegisterTimeoutThread(WorkID workId,Thread thread) {
            lock (_timeoutThreadsLock) {
                if (!_timeoutThreads.ContainsKey(workId)) {
                    _timeoutThreads[workId] = thread;
                }
            }
        }

        /// <summary>
        /// 清理死亡的超时线程
        /// Cleanup dead timeout threads
        /// </summary>
        private void CleanupTimeoutThreads() {
            lock (_timeoutThreadsLock) {
                var deadThreads = new List<WorkID>();
                foreach (var kvp in _timeoutThreads) {
                    if (kvp.Value == null || !kvp.Value.IsAlive) {
                        deadThreads.Add(kvp.Key);
                    }
                }

                foreach (var id in deadThreads) {
                    _timeoutThreads.Remove(id);
                }
            }
        }

        /// <summary>
        /// 添加执行时间统计 / Add execution time statistics
        /// </summary>
        /// <param name="executeTime">执行时间 / Execution time</param>
        internal void AddExecuteTime(long executeTime) {
            InterlockedHelper.Add(ref _totalExecuteTime,executeTime);

            // 定期清理超时线程（每100次执行清理一次）
            if (_completedWorkItems % 100 == 0) {
                CleanupTimeoutThreads();
            }
        }

        /// <summary>
        /// 析构函数
        /// Destructor
        /// </summary>
        ~PowerPool() {
            Dispose(false);
        }
    }
}