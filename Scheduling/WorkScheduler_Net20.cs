using System;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Scheduling
{
    /// <summary>
    /// 定时任务类型
    /// Scheduled work type
    /// </summary>
    public enum ScheduledWorkType
    {
        /// <summary>
        /// 延迟执行（一次性）
        /// Delayed execution (one-time)
        /// </summary>
        Delayed,

        /// <summary>
        /// 定期执行（周期性）
        /// Recurring execution (periodic)
        /// </summary>
        Recurring
    }

    /// <summary>
    /// 定时任务信息
    /// Scheduled work information
    /// </summary>
    internal class ScheduledWorkInfo
    {
        public string ScheduledWorkID { get; set; }
        public WorkID WorkID { get; set; }
        public ScheduledWorkType Type { get; set; }
        public DateTime ExecuteTime { get; set; }
        public int IntervalMilliseconds { get; set; }
        public int? MaxExecutions { get; set; }
        public int ExecutedCount { get; set; }
        public bool IsCancelled { get; set; }
        public object Function { get; set; } // 存储委托，用于周期任务
        public WorkOption Option { get; set; } // 存储工作选项
    }

    /// <summary>
    /// 工作调度器 - 提供延迟执行和定时执行功能
    /// Work scheduler - provides delayed and recurring execution functionality
    /// </summary>
    public class WorkScheduler : IDisposable
    {
        private readonly PowerPool _pool;
        private readonly Dictionary<string, ScheduledWorkInfo> _scheduledWorks;
        private readonly object _scheduledWorksLock = new object();
        private Timer _schedulerTimer;
        private readonly object _timerLock = new object();
        private bool _disposed;

        /// <summary>
        /// 创建工作调度器实例
        /// Create work scheduler instance
        /// </summary>
        /// <param name="pool">线程池实例 / Thread pool instance</param>
        public WorkScheduler(PowerPool pool)
        {
            if (pool == null)
                throw new ArgumentNullException("pool");

            _pool = pool;
            _scheduledWorks = new Dictionary<string, ScheduledWorkInfo>();
            _schedulerTimer = new Timer(ScheduleCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 延迟执行任务（一次性）
        /// Delayed execution (one-time)
        /// </summary>
        /// <typeparam name="TResult">返回值类型 / Return type</typeparam>
        /// <param name="function">要执行的函数 / Function to execute</param>
        /// <param name="delayMilliseconds">延迟时间（毫秒）/ Delay time in milliseconds</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>定时任务ID / Scheduled work ID</returns>
        public string ScheduleDelayed<TResult>(
            Func<TResult> function,
            int delayMilliseconds,
            WorkOption option = null)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            if (delayMilliseconds < 0)
                throw new ArgumentOutOfRangeException("delayMilliseconds", "Delay must be non-negative");

            string scheduledWorkID = Guid.NewGuid().ToString();
            DateTime executeTime = DateTime.UtcNow.AddMilliseconds(delayMilliseconds);

            // 创建工作选项，初始设为暂停状态
            WorkOption scheduledOption = option ?? new WorkOption();
            // Net20版本使用自定义的暂停机制（通过Options.StartSuspended）

            // 提交任务到线程池（初始暂停）
            WorkID workID = _pool.QueueWorkItemInternalDelayed(function, scheduledOption, executeTime);

            ScheduledWorkInfo workInfo = new ScheduledWorkInfo
            {
                ScheduledWorkID = scheduledWorkID,
                WorkID = workID,
                Type = ScheduledWorkType.Delayed,
                ExecuteTime = executeTime,
                Function = function,
                Option = scheduledOption,
                IsCancelled = false
            };

            lock (_scheduledWorksLock)
            {
                _scheduledWorks[scheduledWorkID] = workInfo;
            }
            RescheduleTimer();

            return scheduledWorkID;
        }

        /// <summary>
        /// 延迟执行任务（一次性，无返回值）
        /// Delayed execution (one-time, no return value)
        /// </summary>
        /// <param name="action">要执行的动作 / Action to execute</param>
        /// <param name="delayMilliseconds">延迟时间（毫秒）/ Delay time in milliseconds</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>定时任务ID / Scheduled work ID</returns>
        public string ScheduleDelayed(
            Action action,
            int delayMilliseconds,
            WorkOption option = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Func<object> function = () =>
            {
                action();
                return null;
            };

            return ScheduleDelayed(function, delayMilliseconds, option);
        }

        /// <summary>
        /// 定期执行任务（周期性）
        /// Recurring execution (periodic)
        /// </summary>
        /// <typeparam name="TResult">返回值类型 / Return type</typeparam>
        /// <param name="function">要执行的函数 / Function to execute</param>
        /// <param name="intervalMilliseconds">执行间隔（毫秒）/ Execution interval in milliseconds</param>
        /// <param name="maxExecutions">最大执行次数（null表示无限次）/ Maximum execution count (null means unlimited)</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>定时任务ID / Scheduled work ID</returns>
        public string ScheduleRecurring<TResult>(
            Func<TResult> function,
            int intervalMilliseconds,
            int? maxExecutions = null,
            WorkOption option = null)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            if (intervalMilliseconds <= 0)
                throw new ArgumentOutOfRangeException("intervalMilliseconds", 
                    "Interval must be positive");

            if (maxExecutions.HasValue && maxExecutions.Value <= 0)
                throw new ArgumentOutOfRangeException("maxExecutions", 
                    "MaxExecutions must be positive if specified");

            string scheduledWorkID = Guid.NewGuid().ToString();
            DateTime executeTime = DateTime.UtcNow.AddMilliseconds(intervalMilliseconds);

            ScheduledWorkInfo workInfo = new ScheduledWorkInfo
            {
                ScheduledWorkID = scheduledWorkID,
                WorkID =  WorkID.Empty, // 周期任务在每次执行时创建新的WorkID
                Type = ScheduledWorkType.Recurring,
                ExecuteTime = executeTime,
                IntervalMilliseconds = intervalMilliseconds,
                MaxExecutions = maxExecutions,
                ExecutedCount = 0,
                Function = function,
                Option = option,
                IsCancelled = false
            };

            lock (_scheduledWorksLock)
            {
                _scheduledWorks[scheduledWorkID] = workInfo;
            }
            RescheduleTimer();

            return scheduledWorkID;
        }

        /// <summary>
        /// 定期执行任务（周期性，无返回值）
        /// Recurring execution (periodic, no return value)
        /// </summary>
        /// <param name="action">要执行的动作 / Action to execute</param>
        /// <param name="intervalMilliseconds">执行间隔（毫秒）/ Execution interval in milliseconds</param>
        /// <param name="maxExecutions">最大执行次数（null表示无限次）/ Maximum execution count (null means unlimited)</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>定时任务ID / Scheduled work ID</returns>
        public string ScheduleRecurring(
            Action action,
            int intervalMilliseconds,
            int? maxExecutions = null,
            WorkOption option = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Func<object> function = () =>
            {
                action();
                return null;
            };

            return ScheduleRecurring(function, intervalMilliseconds, maxExecutions, option);
        }

        /// <summary>
        /// 取消定时任务
        /// Cancel scheduled work
        /// </summary>
        /// <param name="scheduledWorkID">定时任务ID / Scheduled work ID</param>
        /// <returns>是否成功取消 / Whether successfully cancelled</returns>
        public bool CancelScheduledWork(string scheduledWorkID)
        {
            if (string.IsNullOrEmpty(scheduledWorkID))
                return false;

            ScheduledWorkInfo workInfo = null;
            lock (_scheduledWorksLock)
            {
                if (_scheduledWorks.TryGetValue(scheduledWorkID, out workInfo))
                {
                    workInfo.IsCancelled = true;
                    _scheduledWorks.Remove(scheduledWorkID);
                }
            }

            if (workInfo != null)
            {
                // 如果是延迟任务，还需要从延迟字典中移除
                if (workInfo.Type == ScheduledWorkType.Delayed &&
                    workInfo.WorkID != WorkID.Empty)
                {
                    _pool.RemoveDelayedWorkFromDictionary(workInfo.WorkID);
                }

                RescheduleTimer();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取所有活跃的定时任务ID
        /// Get all active scheduled work IDs
        /// </summary>
        /// <returns>定时任务ID列表 / List of scheduled work IDs</returns>
        public List<string> GetActiveScheduledWorkIDs()
        {
            lock (_scheduledWorksLock)
            {
                return new List<string>(_scheduledWorks.Keys);
            }
        }

        /// <summary>
        /// 获取活跃的定时任务数量
        /// Get active scheduled work count
        /// </summary>
        public int ActiveScheduledWorkCount
        {
            get
            {
                lock (_scheduledWorksLock)
                {
                    return _scheduledWorks.Count;
                }
            }
        }

        /// <summary>
        /// 定时器回调
        /// Timer callback
        /// </summary>
        private void ScheduleCallback(object state)
        {
            DateTime now = DateTime.UtcNow;
            List<ScheduledWorkInfo> readyWorks = new List<ScheduledWorkInfo>();

            // 查找所有到期任务
            lock (_scheduledWorksLock)
            {
                foreach (var kvp in _scheduledWorks)
                {
                    if (!kvp.Value.IsCancelled && kvp.Value.ExecuteTime <= now)
                    {
                        readyWorks.Add(kvp.Value);
                    }
                }
            }

            // 执行到期任务
            foreach (var workInfo in readyWorks)
            {
                if (workInfo.IsCancelled)
                    continue;

                try
                {
                    if (workInfo.Type == ScheduledWorkType.Delayed)
                    {
                        // 延迟任务：由监控线程的 CheckAndResumeDelayedWorks 处理
                        // 这里只需要从 _scheduledWorks 中移除
                        lock (_scheduledWorksLock)
                        {
                            _scheduledWorks.Remove(workInfo.ScheduledWorkID);
                        }
                    }
                    else if (workInfo.Type == ScheduledWorkType.Recurring)
                    {
                        // 周期任务：提交新任务
                        Delegate functionDelegate = workInfo.Function as Delegate;
                        if (functionDelegate != null)
                        {
                            // 创建包装函数，使用 DynamicInvoke 处理所有委托类型
                            Func<object> wrapper = () => functionDelegate.DynamicInvoke();
                            _pool.QueueWorkItem(wrapper, workInfo.Option);
                            workInfo.ExecutedCount++;
                        }

                        // 检查是否达到最大执行次数
                        if (workInfo.MaxExecutions.HasValue &&
                            workInfo.ExecutedCount >= workInfo.MaxExecutions.Value)
                        {
                            lock (_scheduledWorksLock)
                            {
                                _scheduledWorks.Remove(workInfo.ScheduledWorkID);
                            }
                        }
                        else
                        {
                            // 计算下次执行时间
                            workInfo.ExecuteTime = DateTime.UtcNow
                                .AddMilliseconds(workInfo.IntervalMilliseconds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误，但不影响其他任务
                    System.Diagnostics.Debug.WriteLine(
                        "Scheduled work execution failed: " + ex.Message);
                }
            }

            // 重新调度定时器
            RescheduleTimer();
        }

        /// <summary>
        /// 重新调度定时器
        /// Reschedule timer
        /// </summary>
        private void RescheduleTimer()
        {
            lock (_timerLock)
            {
                if (_disposed)
                    return;

                // 查找最近的待执行任务
                ScheduledWorkInfo nextWork = null;
                lock (_scheduledWorksLock)
                {
                    foreach (var workInfo in _scheduledWorks.Values)
                    {
                        if (!workInfo.IsCancelled)
                        {
                            if (nextWork == null || workInfo.ExecuteTime < nextWork.ExecuteTime)
                            {
                                nextWork = workInfo;
                            }
                        }
                    }
                }

                if (nextWork != null)
                {
                    int delay = Math.Max(0, (int)(
                        nextWork.ExecuteTime - DateTime.UtcNow).TotalMilliseconds);
                    _schedulerTimer.Change(delay, Timeout.Infinite);
                }
                else
                {
                    // 没有待执行任务，停止定时器
                    _schedulerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            lock (_timerLock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                // 取消所有定时任务
                List<string> workIds;
                lock (_scheduledWorksLock)
                {
                    workIds = new List<string>(_scheduledWorks.Keys);
                }

                foreach (var scheduledWorkID in workIds)
                {
                    CancelScheduledWork(scheduledWorkID);
                }

                if (_schedulerTimer != null)
                {
                    _schedulerTimer.Dispose();
                    _schedulerTimer = null;
                }
            }
        }
    }
}
