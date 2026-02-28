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
        //初始状态为WorkerStates.Running，等启动线程成功立马设置为WorkerStates.Idle
        private readonly InterlockedEnumFlag<WorkerStates> _workerState = WorkerStates.Running;
        private WorkID _currentWorkID = WorkID.Empty;
        private DateTime _idleStartTime = DateTime.Now;
        private volatile int _executingWorkCount = 0; // 当前线程正在执行的工作项数量（0或1）

        /// <summary>
        /// 线程ID
        /// Thread ID
        /// </summary>
        public int ThreadId => _threadId;

        /// <summary>
        /// 是否空闲
        /// Whether idle
        /// </summary>
        public bool IsIdle => _workerState.Value == WorkerStates.Idle;

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
        /// 当前正在执行的工作项数量（0或1）
        /// Current executing work item count (0 or 1)
        /// </summary>
        public int ExecutingWorkCount => 
            (_workerState.Value == WorkerStates.ToBeDisposed || !_thread.IsAlive) ? 0 : _executingWorkCount;

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
            //_workerState.InterlockedValue = WorkerStates.Idle;
            //_currentWorkID = WorkID.Empty;
        }

        /// <summary>
        /// 启动线程
        /// Start thread
        /// </summary>
        public void Start()
        {
            if (!_thread.IsAlive)
            {
                try
                {
                    _thread.Start();
                }
                catch (ThreadStateException)
                {
                    // 线程已经启动过，忽略
                }
                catch (OutOfMemoryException)
                {
                    // 内存不足，无法启动线程，标记为停止状态
                    _workerState.InterlockedValue = WorkerStates.ToBeDisposed;
#if UNITY
                    UnityEngine.Debug.LogError($"WorkerThread {ThreadId} failed to start: Out of memory");
#else
                    Console.WriteLine($"WorkerThread {ThreadId} failed to start: Out of memory");
#endif
                }
                catch (Exception ex)
                {
                    // 其他异常，标记为停止状态
                    _workerState.InterlockedValue = WorkerStates.ToBeDisposed;
#if UNITY
                    UnityEngine.Debug.LogError($"WorkerThread {ThreadId} failed to start: {ex.Message}");
#else
                    Console.WriteLine($"WorkerThread {ThreadId} failed to start: {ex.Message}");
#endif
                }
            }
        }

        /// <summary>
        /// 停止线程
        /// Stop thread
        /// </summary>
        public void Stop()
        {
            _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        }

        /// <summary>
        /// 标记为要停止（优雅停止）
        /// Mark for stop (graceful stop)
        /// </summary>
        public void MarkForStop()
        {
            _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        }

        ///// <summary>
        ///// 标记为需要取消当前工作
        ///// Mark for cancellation of current work
        ///// </summary>
        //public void MarkForCancellation()
        //{
        //    // 设置停止标志，让线程在下一次检查时退出
        //    _shouldStop.SetTrue();
        //    _workerState.InterlockedValue=(WorkerStates.ToBeDisposed);
        //}

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
            WorkItem currentWorkItem = null;
            _workerState.InterlockedValue = WorkerStates.Idle;
            Exception ex = null;
            try
            {               
                while (_workerState.Value != WorkerStates.ToBeDisposed)
                {
                    // 从线程池获取工作项
                    WorkItem workItem = _pool.GetWorkItem();
                    currentWorkItem = workItem; // 保存当前工作项引用
                    if (workItem == null)
                    {
                        // GetWorkItem返回null表示线程池已停止或释放                        
                        break;
                    }

                    _workerState.InterlockedValue = WorkerStates.Running;
                    _currentWorkID = workItem.ID;
                    _executingWorkCount = 1; // 增加执行计数

                    // 执行工作项（注意：状态更新在ExecuteWorkItem的回调内部执行）
                    ExecuteWorkItem(workItem);
                    // 工作真正完成后才标记为空闲
                    _workerState.InterlockedValue = WorkerStates.Idle;
                    _idleStartTime = DateTime.Now; // 更新空闲开始时间
                    _currentWorkID = WorkID.Empty;
                    _executingWorkCount = 0; // 减少执行计数
                    //
                }
            }
            catch (ThreadAbortException ex1)
            {
                // 线程被中止，正常退出
                ex = ex1;
            }
            catch (Exception ex2)
            {
                // 记录未处理的异常
                ex = ex2;
#if UNITY
                UnityEngine.Debug.LogError($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#else
                Console.WriteLine($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#endif
            }
            finally
            {
                // 如果线程被中止时正在执行工作项，调用OnWorkCompleted处理完成逻辑
                // 注意：计数器由WorkerThread内部维护，这里不再依赖OnWorkCompleted来维护计数
                if (currentWorkItem != null) {
                    _pool.OnWorkCompleted(currentWorkItem,null,
                        ex,0);
                }
                _workerState.InterlockedValue = WorkerStates.ToBeDisposed;
                _currentWorkID = WorkID.Empty;
                _executingWorkCount = 0; // 确保计数器归零
            }
        }

        /// <summary>
        /// 执行工作项（内部相当于同步执行）
        /// Execute work item (synchronous callback mode)
        /// </summary>
        private void ExecuteWorkItem(WorkItem workItem)
        {
            DateTime startTime = DateTime.Now;

            // 检查暂停信号 - 通过公共接口而不是直接访问私有字段
            // 如果暂停，等待暂停信号被设置（恢复）
            if (!_pool.WaitForPauseSignal()) {
                // 暂停状态下，将工作项重新放回队列
                _pool.RequeueWorkItem(workItem);
                _executingWorkCount = 0; // 重新入队时，工作项未真正执行，计数器归零
                return;
            }

            // 检查取消令牌 - 由WorkerThread执行线程负责检查
            if (workItem.Option.CancellationToken != null && workItem.Option.CancellationToken.IsCancellationRequested)
            {
                // 在释放锁之前调用OnWorkCompleted，避免死锁
                _pool.OnWorkCompleted(workItem, null, new OperationCanceledException(), 0);
                return;
            }

            // 同步执行工作项，通过回调获取结果
            workItem.Execute((completedWorkItem, result, exception, retryCount) =>
            {
                // 更新执行时间统计 - 通过公共接口
                if (_pool.Options.EnableStatisticsCollection)
                {
                    long executeTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    _pool.AddExecuteTime(executeTime);
                }

                // 通知线程池工作完成
                _pool.OnWorkCompleted(completedWorkItem, result, exception, retryCount);
                
               
            });
        }
    }
}