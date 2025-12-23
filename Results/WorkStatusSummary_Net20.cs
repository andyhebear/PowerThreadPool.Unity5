using System;

namespace PowerThreadPool_Net20.Results
{
    /// <summary>
    /// 工作状态摘要信息
    /// Work status summary information
    /// </summary>
    public struct WorkStatusSummary
    {
        /// <summary>
        /// 队列中等待的工作数量
        /// Number of work items waiting in queue
        /// </summary>
        public int TotalQueued;

        /// <summary>
        /// 正在执行的工作数量
        /// Number of work items currently executing
        /// </summary>
        public int TotalExecuting;

        /// <summary>
        /// 已完成的工作数量
        /// Number of completed work items
        /// </summary>
        public int TotalCompleted;

        /// <summary>
        /// 失败的工作数量
        /// Number of failed work items
        /// </summary>
        public int TotalFailed;

        /// <summary>
        /// 缓存的结果数量
        /// Number of cached results
        /// </summary>
        public int CachedResults;

        /// <summary>
        /// 成功率（0.0-1.0）
        /// Success rate (0.0-1.0)
        /// </summary>
        public double SuccessRate;

        /// <summary>
        /// 获取状态摘要的字符串表示
        /// Get string representation of status summary
        /// </summary>
        /// <returns>摘要字符串 / Summary string</returns>
        public override string ToString()
        {
            return string.Format(
                "Queued: {0}, Executing: {1}, Completed: {2}, Failed: {3}, Cached: {4}, Success Rate: {5:P2}",
                TotalQueued, TotalExecuting, TotalCompleted, TotalFailed, CachedResults, SuccessRate
            );
        }
    }
}