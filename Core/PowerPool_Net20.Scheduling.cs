using System;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20
{
    /// <summary>
    /// PowerPool_Net20 部分类 - 调度功能
    /// PowerPool_Net20 partial class - Scheduling functionality
    /// </summary>
    public partial class PowerPool
    {
        private Scheduling.WorkScheduler _scheduler;

        /// <summary>
        /// 获取工作调度器实例
        /// Get work scheduler instance
        /// </summary>
        public Scheduling.WorkScheduler Scheduler
        {
            get
            {
                if (_scheduler == null)
                {
                    lock (_lockObject)
                    {
                        if (_scheduler == null)
                        {
                            _scheduler = new Scheduling.WorkScheduler(this);
                        }
                    }
                }
                return _scheduler;
            }
        }

        /// <summary>
        /// 延迟执行任务（一次性，有返回值）
        /// Delayed execution (one-time, with return value)
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
            return Scheduler.ScheduleDelayed(function, delayMilliseconds, option);
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
            return Scheduler.ScheduleDelayed(action, delayMilliseconds, option);
        }

        /// <summary>
        /// 定期执行任务（周期性，有返回值）
        /// Recurring execution (periodic, with return value)
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
            return Scheduler.ScheduleRecurring(function, intervalMilliseconds, maxExecutions, true,option);
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
            return Scheduler.ScheduleRecurring(action, intervalMilliseconds, maxExecutions,true, option);
        }

        /// <summary>
        /// 取消定时任务
        /// Cancel scheduled work
        /// </summary>
        /// <param name="scheduledWorkID">定时任务ID / Scheduled work ID</param>
        /// <returns>是否成功取消 / Whether successfully cancelled</returns>
        public bool CancelScheduledWork(string scheduledWorkID)
        {
            return Scheduler.CancelScheduledWork(scheduledWorkID);
        }

        /// <summary>
        /// 获取所有活跃的定时任务ID
        /// Get all active scheduled work IDs
        /// </summary>
        /// <returns>定时任务ID列表 / List of scheduled work IDs</returns>
        public List<string> GetActiveScheduledWorkIDs()
        {
            return Scheduler.GetActiveScheduledWorkIDs();
        }

        /// <summary>
        /// 获取活跃的定时任务数量
        /// Get active scheduled work count
        /// </summary>
        public int ActiveScheduledWorkCount
        {
            get
            {
                return Scheduler.ActiveScheduledWorkCount;
            }
        }

        /// <summary>
        /// 内部方法：队列延迟任务（供调度器使用）
        /// Internal method: queue delayed work (used by scheduler)
        /// </summary>
        internal WorkID QueueWorkItemInternalDelayed<T>(Func<T> function, WorkOption option, DateTime executeTime)
        {
            // 延迟任务需要特殊处理
            // 由于Net20版本的限制，我们使用一个字典来存储延迟任务
            // 在达到执行时间时，再将任务提交到主队列
            
            WorkID workID = new WorkID(true);
            WorkItem workItem = new WorkItem(workID, function, option, this);
            
            // 标记为延迟任务
            workItem.IsDelayedWork = true;
            workItem.ExecuteTime = executeTime;
            
            // 存储到延迟工作字典中
            lock (_lockObject)
            {
                if (_delayedWorkDictionary == null)
                    _delayedWorkDictionary = new Dictionary<DateTime, List<WorkItem>>();
                
                if (!_delayedWorkDictionary.ContainsKey(executeTime))
                    _delayedWorkDictionary[executeTime] = new List<WorkItem>();
                
                _delayedWorkDictionary[executeTime].Add(workItem);
            }
            
            return workID;
        }

        /// <summary>
        /// 内部方法：从延迟字典中移除工作项（供调度器取消时使用）
        /// Internal method: remove work item from delayed dictionary (used by scheduler during cancellation)
        /// </summary>
        internal void RemoveDelayedWorkFromDictionary(WorkID workID)
        {
            if (workID == WorkID.Empty)
                return;

            lock (_lockObject)
            {
                if (_delayedWorkDictionary == null)
                    return;

                // 遍历字典查找并移除对应的 WorkItem
                List<DateTime> keysToRemove = new List<DateTime>();
                foreach (var kvp in _delayedWorkDictionary)
                {
                    foreach (var workItem in kvp.Value)
                    {
                        if (workItem.ID == workID)
                        {
                            workItem.IsDelayedWork = false; // 标记为已取消
                            break;
                        }
                    }

                    // 检查该时间点下所有任务是否都已移除
                    bool allRemoved = true;
                    foreach (var workItem in kvp.Value)
                    {
                        if (workItem.IsDelayedWork)
                        {
                            allRemoved = false;
                            break;
                        }
                    }

                    if (allRemoved)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // 移除空的条目
                foreach (var key in keysToRemove)
                {
                    _delayedWorkDictionary.Remove(key);
                }
            }
        }

        /// <summary>
        /// 检查并恢复到期的延迟工作（供监控线程调用）
        /// Check and resume expired delayed work (called by monitor thread)
        /// </summary>
        internal void CheckAndResumeDelayedWorks()
        {
            lock (_lockObject)
            {
                if (_delayedWorkDictionary == null || _delayedWorkDictionary.Count == 0)
                    return;
                
                DateTime now = DateTime.UtcNow;
                List<DateTime> expiredTimes = new List<DateTime>();
                List<WorkItem> worksToQueue = new List<WorkItem>();
                
                // 查找所有到期的延迟任务
                foreach (var kvp in _delayedWorkDictionary)
                {
                    if (kvp.Key <= now)
                    {
                        expiredTimes.Add(kvp.Key);
                        worksToQueue.AddRange(kvp.Value);
                    }
                }
                
                // 移除已处理的条目
                foreach (var time in expiredTimes)
                {
                    _delayedWorkDictionary.Remove(time);
                }
                
                // 将到期的任务提交到主队列
                if (worksToQueue.Count > 0)
                {
                    foreach (var workItem in worksToQueue)
                    {
                        workItem.IsDelayedWork = false;
                        int queuePriority = ConvertPriority(workItem.Option.Priority);
                        _workQueue.Enqueue(workItem, queuePriority);
                        _totalWorkItems++;
                    }
                    
                    Monitor.PulseAll(_lockObject);
                    _logger.Info(string.Format("Resumed {0} delayed work items", worksToQueue.Count));
                }
            }
        }
    }
}
