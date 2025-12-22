using System;
using System.Threading;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Works;

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
        private volatile bool _shouldStop;
        private volatile bool _isIdle;
        private WorkID _currentWorkID = WorkID.Empty;
        
        /// <summary>
        /// 线程ID
        /// Thread ID
        /// </summary>
        public int ThreadId => _threadId;
        
        /// <summary>
        /// 是否空闲
        /// Whether idle
        /// </summary>
        public bool IsIdle => _isIdle;
        
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
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkerThread(PowerPool pool, int threadId)
        {
            _pool = pool;
            _threadId = threadId;
            _thread = new Thread(ThreadProc)
            {
                Name = $"PowerPool-Worker-{threadId}",
                IsBackground = true
            };
            _shouldStop = false;
            _isIdle = true;
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
            _shouldStop = true;
        }
        
        /// <summary>
        /// 标记为要停止（优雅停止）
        /// Mark for stop (graceful stop)
        /// </summary>
        public void MarkForStop()
        {
            _shouldStop = true;
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
                while (!_shouldStop)
                {
                    _isIdle = true;
                    _currentWorkID = WorkID.Empty;
                    
                    // 从线程池获取工作项
                    WorkItem workItem = _pool.GetWorkItem();
                    
                    if (workItem == null)
                        break;
                        
                    _isIdle = false;
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
                _isIdle = true;
                _currentWorkID = WorkID.Empty;
            }
        }
        
        /// <summary>
        /// 执行工作项
        /// Execute work item
        /// </summary>
        private void ExecuteWorkItem(WorkItem workItem)
        {
            DateTime startTime = DateTime.Now;
            object result = null;
            Exception exception = null;
            
            try
            {
                // 检查暂停信号 - 通过公共接口而不是直接访问私有字段
                if (!_pool.WaitForPauseSignal())
                    return;
                
                // 执行工作
                result = workItem.Execute();
                
                // 更新执行时间统计 - 通过公共接口
                if (_pool.Options.EnableStatisticsCollection)
                {
                    long executeTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    _pool.AddExecuteTime(executeTime);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            
            // 通知线程池工作完成
            _pool.OnWorkCompleted(workItem, result, exception);
        }
    }
}