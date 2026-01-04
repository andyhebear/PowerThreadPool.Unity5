using System;
using System.Threading;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Works;
using PowerThreadPool_Net20.Helpers;
using PowerThreadPool_Net20.Constants;

namespace PowerThreadPool_Net20.Works
{
    /// <summary>
    /// 工作线程类
    /// Worker thread class
    /// </summary>
    internal class WorkerThread
    {
        private readonly PowerPool _pool;
        private readonly int _threadId;
        private readonly Thread _thread;
        private readonly AtomicFlag _shouldStop = new AtomicFlag();
        private readonly AtomicFlag _isIdle = new AtomicFlag();
        private readonly InterlockedFlag<WorkerStates> _workerState = WorkerStates.Idle;
        private WorkID _currentWorkID = WorkID.Empty;
        private DateTime _idleStartTime = DateTime.Now;

        /// <summary>
        /// 线程ID
        /// Thread ID
        /// </summary>
        public int ThreadId => _threadId;

        /// <summary>
        /// 是否空闲
        /// Whether idle
        /// </summary>
        public bool IsIdle => _isIdle.Value;

        /// <summary>
        /// 工作线程状态
        /// Worker thread state
        /// </summary>
        public WorkerStates WorkerState => _workerState.Value;

        /// <summary>
        /// 当前工作ID / Current work ID
        /// </summary>
        public WorkID CurrentWorkID => _currentWorkID;

        /// <summary>
        /// 线程对象
        /// Thread object
        /// </summary>
        public Thread Thread => _thread;

        /// <summary>
        /// 空闲开始时间
        /// Idle start time
        /// </summary>
        public DateTime IdleStartTime => _idleStartTime;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkerThread(PowerPool pool,int threadId)
        {
            _pool = pool;
            _threadId = threadId;
            _thread = new Thread(ThreadProc)
            {
                Name = pool.Options.ThreadNamePrefix + $"-Worker-{threadId}",
                IsBackground = pool.Options.UseBackgroundThreads,
                Priority = pool.Options.ThreadPriority,

            };
            _shouldStop.SetValue(false);
            _isIdle.SetValue(true);
            _currentWorkID = WorkID.Empty;
        }

        /// <summary>
        /// 启动线程
        /// Start thread
        /// </summary>
        public void Start()
        {
            if (!_thread.IsAlive)
            {
                _thread.Start();
            }
        }

        /// <summary>
        /// 停止线程
        /// Stop thread
        /// </summary>
        public void Stop()
        {
            _shouldStop.SetTrue();
            _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        }

        /// <summary>
        /// 标记为要停止（优雅停止）
        /// Mark for stop (graceful stop)
        /// </summary>
        public void MarkForStop()
        {
            _shouldStop.SetTrue();
            _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        }

        /// <summary>
        /// 标记为需要取消当前工作
        /// Mark for cancellation of current work
        /// </summary>
        public void MarkForCancellation()
        {
            // 设置停止标志，让线程在下一次检查时退出
            _shouldStop.SetTrue();
            _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        }

        /// <summary>
        /// 等待线程完成
        /// Wait for thread to complete
        /// </summary>
        public void Join()
        {
            if (_thread.IsAlive)
            {
                _thread.Join(1000); // 最多等待1秒，减少Dispose阻塞时间
            }
        }

        /// <summary>
        /// 线程主循环
        /// Thread main loop
        /// </summary>
        private void ThreadProc()
        {
            try
            {
                while (!_shouldStop.Value)
                {
                    _isIdle.SetValue(true);
                    _workerState.InterlockedValue=(WorkerStates.Idle);
                    _idleStartTime = DateTime.Now; // 更新空闲开始时间
                    _currentWorkID = WorkID.Empty;

                    // 从线程池获取工作项
                    WorkItem workItem = _pool.GetWorkItem();

                    if (workItem == null)
                        break;

                    _isIdle.SetValue(false);
                    _workerState.InterlockedValue=(WorkerStates.Running);
                    _currentWorkID = workItem.ID;

                    // 执行工作项
                    ExecuteWorkItem(workItem);

                    _currentWorkID = WorkID.Empty;
                }
            }
            catch (ThreadAbortException)
            {
                // 线程被中止，正常退出
            }
            catch (Exception ex)
            {
                // 记录未处理的异常
#if UNITY
                UnityEngine.Debug.LogError($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#else
                Console.WriteLine($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#endif
            }
            finally
            {
                _isIdle.SetValue(true);
                _workerState.InterlockedValue=(WorkerStates.Idle);
                _currentWorkID = WorkID.Empty;
            }
        }

        /// <summary>
        /// 执行工作项（异步回调模式）
        /// Execute work item (asynchronous callback mode)
        /// </summary>
        private void ExecuteWorkItem(WorkItem workItem)
        {
            DateTime startTime = DateTime.Now;

            // 检查暂停信号 - 通过公共接口而不是直接访问私有字段
            if (!_pool.WaitForPauseSignal())
                return;

            // 检查取消令牌 - 由WorkerThread执行线程负责检查
            if (workItem.Option.CancellationToken != null && workItem.Option.CancellationToken.IsCancellationRequested)
            {
                // 在释放锁之前调用OnWorkCompleted，避免死锁
                _pool.OnWorkCompleted(workItem, null, new OperationCanceledException());
                return;
            }

            // 异步执行工作项，通过回调获取结果
            workItem.ExecuteAsync((completedWorkItem, result, exception) =>
            {
                // 更新执行时间统计 - 通过公共接口
                if (_pool.Options.EnableStatisticsCollection)
                {
                    long executeTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    _pool.AddExecuteTime(executeTime);
                }

                // 通知线程池工作完成
                _pool.OnWorkCompleted(completedWorkItem, result, exception);
            });
        }
    }
}