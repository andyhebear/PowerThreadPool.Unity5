using System;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Results
{
    ///// <summary>
    ///// 工作取消事件参数
    ///// Work canceled event arguments
    ///// </summary>
    //public class WorkCanceledEventArgs : EventArgs
    //{
    //    /// <summary>
    //    /// 工作ID
    //    /// Work ID
    //    /// </summary>
    //    public WorkID WorkID { get; private set; }
        
    //    /// <summary>
    //    /// 取消时间
    //    /// Cancel time
    //    /// </summary>
    //    public DateTime CancelTime { get; private set; }
        
    //    /// <summary>
    //    /// 取消原因（异常信息）
    //    /// Cancel reason (exception information)
    //    /// </summary>
    //    public Exception CancelReason { get; private set; }
        
    //    /// <summary>
    //    /// 是否成功中断执行线程
    //    /// Whether successfully interrupted the execution thread
    //    /// </summary>
    //    public bool ThreadInterrupted { get; private set; }
        
    //    /// <summary>
    //    /// 工作排队时间
    //    /// Work queue time
    //    /// </summary>
    //    public DateTime QueueTime { get; private set; }
        
    //    /// <summary>
    //    /// 工作开始时间
    //    /// Work start time
    //    /// </summary>
    //    public DateTime StartTime { get; private set; }
        
    //    /// <summary>
    //    /// 工作执行时长（毫秒）
    //    /// Work execution duration (milliseconds)
    //    /// </summary>
    //    public long Duration { get; private set; }
        
    //    /// <summary>
    //    /// 构造函数
    //    /// Constructor
    //    /// </summary>
    //    public WorkCanceledEventArgs(WorkID workID, DateTime cancelTime, Exception cancelReason, 
    //                                bool threadInterrupted, DateTime queueTime, DateTime startTime, long duration)
    //    {
    //        WorkID = workID;
    //        CancelTime = cancelTime;
    //        CancelReason = cancelReason;
    //        ThreadInterrupted = threadInterrupted;
    //        QueueTime = queueTime;
    //        StartTime = startTime;
    //        Duration = duration;
    //    }
        
    //    /// <summary>
    //    /// 简化的构造函数（用于基本取消事件）
    //    /// Simplified constructor (for basic cancel events)
    //    /// </summary>
    //    public WorkCanceledEventArgs(WorkID workID, DateTime cancelTime, Exception cancelReason)
    //        : this(workID, cancelTime, cancelReason, false, DateTime.MinValue, DateTime.MinValue, 0)
    //    {
    //    }
        
    //    /// <summary>
    //    /// 转换为字符串
    //    /// Convert to string
    //    /// </summary>
    //    public override string ToString()
    //    {
    //        return $"WorkCanceled[ID={WorkID}, Time={CancelTime}, Reason={CancelReason?.Message ?? "Unknown"}, Interrupted={ThreadInterrupted}, Duration={Duration}ms]";
    //    }
    //}
    /// <summary>
    /// 工作完成事件参数
    /// Work completed event arguments
    /// </summary>
    public class WorkCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID WorkID { get; private set; }
        
        /// <summary>
        /// 结果
        /// Result
        /// </summary>
        public object Result { get; private set; }
        
        /// <summary>
        /// 完成时间
        /// Completion time
        /// </summary>
        public DateTime CompletionTime { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkCompletedEventArgs(WorkID workID, object result, DateTime completionTime)
        {
            WorkID = workID;
            Result = result;
            CompletionTime = completionTime;
        }
    }
    
    /// <summary>
    /// 工作失败事件参数
    /// Work failed event arguments
    /// </summary>
    public class WorkFailedEventArgs : EventArgs
    {
        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID WorkID { get; private set; }
        
        /// <summary>
        /// 异常
        /// Exception
        /// </summary>
        public Exception Exception { get; private set; }
        
        /// <summary>
        /// 失败时间
        /// Failure time
        /// </summary>
        public DateTime FailureTime { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkFailedEventArgs(WorkID workID, Exception exception, DateTime failureTime)
        {
            WorkID = workID;
            Exception = exception;
            FailureTime = failureTime;
        }
    }
    
    /// <summary>
    /// 线程池启动事件参数
    /// Pool started event arguments
    /// </summary>
    public class PoolStartedEventArgs : EventArgs
    {
        /// <summary>
        /// 启动时间
        /// Start time
        /// </summary>
        public DateTime StartTime { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public PoolStartedEventArgs(DateTime startTime)
        {
            StartTime = startTime;
        }
    }
    
    /// <summary>
    /// 线程池停止事件参数
    /// Pool stopped event arguments
    /// </summary>
    public class PoolStoppedEventArgs : EventArgs
    {
        /// <summary>
        /// 停止时间
        /// Stop time
        /// </summary>
        public DateTime StopTime { get; private set; }
        
        /// <summary>
        /// 完成的工作数
        /// Number of completed works
        /// </summary>
        public int CompletedWorks { get; private set; }
        
        /// <summary>
        /// 失败的工作数
        /// Number of failed works
        /// </summary>
        public int FailedWorks { get; private set; }
        
        /// <summary>
        /// 总工作数
        /// Total number of works
        /// </summary>
        public int TotalWorks => CompletedWorks + FailedWorks;
        
        /// <summary>
        /// 成功率
        /// Success rate
        /// </summary>
        public double SuccessRate
        {
            get
            {
                return TotalWorks > 0 ? (double)CompletedWorks / TotalWorks * 100 : 0;
            }
        }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public PoolStoppedEventArgs(DateTime stopTime, int completedWorks, int failedWorks)
        {
            StopTime = stopTime;
            CompletedWorks = completedWorks;
            FailedWorks = failedWorks;
        }
        
        /// <summary>
        /// 转换为字符串
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            return $"PoolStopped[Time={StopTime}, Completed={CompletedWorks}, Failed={FailedWorks}, SuccessRate={SuccessRate:F1}%]";
        }
    }
}