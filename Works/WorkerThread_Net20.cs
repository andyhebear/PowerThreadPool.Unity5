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
        WorkItem currentWorkItem = null;
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

        ///// <summary>
        ///// 线程对象
        ///// Thread object
        ///// </summary>
        //internal Thread Thread => _thread;

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
        public WorkerThread(PowerPool pool,int threadId) {
            _pool = pool;
            _threadId = threadId;
            _thread = new Thread(ThreadProc) {
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
        public void Start() {
            if (!_thread.IsAlive) {
                try {
                    _thread.Start();
                }
                catch (ThreadStateException) {
                    // 线程已经启动过，忽略
                }
                catch (OutOfMemoryException) {
                    // 内存不足，无法启动线程，标记为停止状态
                    _workerState.InterlockedValue = WorkerStates.ToBeDisposed;
#if UNITY
                    UnityEngine.Debug.LogError($"WorkerThread {ThreadId} failed to start: Out of memory");
#else
                    Console.WriteLine($"WorkerThread {ThreadId} failed to start: Out of memory");
#endif
                }
                catch (Exception ex) {
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
        /// 强制停止线程,内部已经处理异常
        /// Stop thread
        /// </summary>
        public void Stop() {
            // 标记线程为停止状态
            MarkForStop();
            //等待一点时间让线程自然结束
            System.Threading.Thread.Sleep(5);
            try {
                //如何没有结束则强制结束
                if (this._currentWorkID != WorkID.Empty && this.currentWorkItem != null) {
                    //超时与取消支持执行内容是新开线程执行，这里就要结束新开的执行线程了
                    this.currentWorkItem.AbortAsyncThreadSafely();
                }
                // 如果线程正在运行且可以访问底层Thread对象，尝试中断
                if (_thread != null && _thread.IsAlive) {
                    //强制中断当前workerthread
                    try {
                        //#if !NET20
                        //                        _thread.Interrupt();
                        //#endif
                        // 注意：Thread.Abort 在 .NET Core/.NET 5+ 中已过时
                        // 这里为了兼容性保留，建议使用 CancellationToken 替代
                        // Note: Thread.Abort is obsolete in .NET Core/.NET 5+
                        // Kept here for compatibility, recommend using CancellationToken instead
                        this._thread.Abort();
                    }
                    catch (System.Security.SecurityException) {
                        // 在某些安全环境下可能不允许中断线程
                        Console.WriteLine("WorkerThread.Stop()失败1:在某些安全环境下可能不允许中断线程");
                    }
                    catch (ThreadStateException) {
                        // 线程状态不允许中断
                        Console.WriteLine("WorkerThread.Stop()失败2:线程状态不允许中断");
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Failed to force stop work item {_currentWorkID} on worker thread: {ex.Message}");
            }
        }

        /// <summary>
        /// 标记为要停止（优雅停止）
        /// Mark for stop (graceful stop)
        /// </summary>
        public void MarkForStop() {
            _workerState.InterlockedValue = (WorkerStates.ToBeDisposed);
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
        /// 等待线程完成,内部已经处理异常，执行Join之前最好执行过MarkForStop()
        /// Wait for thread to complete
        /// </summary>
        public void Join(int ms = 1000) {
            if (_thread.IsAlive) {

                try {
                    //_thread.Join(1000); // 最多等待1秒，减少Dispose阻塞时间
                    _thread.Join(ms); // 最多等待100ms
                }
                catch (Exception ex) {
#if UNITY
                    UnityEngine.Debug.LogWarning($"Error joining worker thread {_threadId}: {ex.Message}");
#else
                    Console.WriteLine($"Error joining worker thread {_threadId}: {ex.Message}");
#endif
                }
            }
        }

        /// <summary>
        /// 线程主循环
        /// Thread main loop
        /// </summary>
        private void ThreadProc() {

            _workerState.InterlockedValue = WorkerStates.Idle;
            Exception ex = null;
            try {
                while (_workerState.Value != WorkerStates.ToBeDisposed) {
                    // 从线程池获取工作项
                    WorkItem workItem = _pool.GetWorkItem();
                    currentWorkItem = workItem; // 保存当前工作项引用
                    if (workItem == null) {
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
            catch (ThreadAbortException ex1) {
                // 线程被中止，正常退出
                ex = ex1;
            }
            catch (Exception ex2) {
                // 记录未处理的异常
                ex = ex2;
#if UNITY
                UnityEngine.Debug.LogError($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#else
                Console.WriteLine($"WorkerThread {ThreadId} unexpected error: {ex.Message}");
#endif
            }
            finally {
                // 如果线程被中止时正在执行工作项，调用OnWorkCompleted处理完成逻辑
                // 注意：计数器由WorkerThread内部维护，这里不再依赖OnWorkCompleted来维护计数
                if (currentWorkItem != null) {
                    _pool.OnWorkCompleted(currentWorkItem,null,
                        ex,0);
                }
                _workerState.InterlockedValue = WorkerStates.ToBeDisposed;
                _currentWorkID = WorkID.Empty;
                currentWorkItem = null;
                _executingWorkCount = 0; // 确保计数器归零
            }
        }

        /// <summary>
        /// 执行工作项（内部相当于同步执行）
        /// Execute work item (synchronous callback mode)
        /// </summary>
        private void ExecuteWorkItem(WorkItem workItem) {
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
            if (workItem.Option.CancelToken.HasValue && workItem.Option.CancelToken.Value.IsCancellationRequested) {
                // 在释放锁之前调用OnWorkCompleted，避免死锁
                _pool.OnWorkCompleted(workItem,null,new OperationCanceledException(),0);
                return;
            }

            // 同步执行工作项，通过回调获取结果
            workItem.Execute((completedWorkItem,result,exception,retryCount) => {
                // 更新执行时间统计 - 通过公共接口
                if (_pool.Options.EnableStatisticsCollection) {
                    long executeTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    _pool.AddExecuteTime(executeTime);
                }

                // 通知线程池工作完成
                _pool.OnWorkCompleted(completedWorkItem,result,exception,retryCount);


            });
        }
    }
}