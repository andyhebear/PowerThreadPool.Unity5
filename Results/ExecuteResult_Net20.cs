using System;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Results
{
    /// <summary>
    /// 执行状态
    /// Execution status
    /// </summary>
    public enum ExecutionStatus
    {
        /// <summary>
        /// 成功
        /// Success
        /// </summary>
        Success,
        
        /// <summary>
        /// 失败
        /// Failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// 取消
        /// Canceled
        /// </summary>
        Canceled,
        
        /// <summary>
        /// 超时
        /// Timeout
        /// </summary>
        Timeout
    }
    
    /// <summary>
    /// 执行结果基类
    /// Execution result base class
    /// </summary>
    public class ExecuteResult
    {
        private readonly WorkID _id;
        private readonly ExecutionStatus _status;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly object _result;
        private readonly Exception _exception;
        
        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID ID => _id;
        
        /// <summary>
        /// 执行状态
        /// Execution status
        /// </summary>
        public ExecutionStatus Status => _status;
        
        /// <summary>
        /// 开始时间
        /// Start time
        /// </summary>
        public DateTime StartTime => _startTime;
        
        /// <summary>
        /// 结束时间
        /// End time
        /// </summary>
        public DateTime EndTime => _endTime;
        
        /// <summary>
        /// 执行时长
        /// Execution duration
        /// </summary>
        public TimeSpan Duration => _endTime - _startTime;
        
        /// <summary>
        /// 结果
        /// Result
        /// </summary>
        public object Result => _result;
        
        /// <summary>
        /// 异常
        /// Exception
        /// </summary>
        public Exception Exception => _exception;
        
        /// <summary>
        /// 是否成功
        /// Whether successful
        /// </summary>
        public bool IsSuccess => _status == ExecutionStatus.Success;
        
        /// <summary>
        /// 是否失败
        /// Whether failed
        /// </summary>
        public bool IsFailed => _status == ExecutionStatus.Failed;
        
        /// <summary>
        /// 是否被取消
        /// Whether canceled
        /// </summary>
        public bool IsCanceled => _status == ExecutionStatus.Canceled;
        
        /// <summary>
        /// 是否超时
        /// Whether timeout
        /// </summary>
        public bool IsTimeout => _status == ExecutionStatus.Timeout;
        
        /// <summary>
        /// 构造函数（成功）
        /// Constructor (success)
        /// </summary>
        public ExecuteResult(WorkID id, object result, DateTime startTime, DateTime endTime)
        {
            _id = id;
            _status = ExecutionStatus.Success;
            _result = result;
            _startTime = startTime;
            _endTime = endTime;
            _exception = null;
        }
        
        /// <summary>
        /// 构造函数（失败）
        /// Constructor (failed)
        /// </summary>
        public ExecuteResult(WorkID id, Exception exception, DateTime startTime, DateTime endTime)
        {
            _id = id;
            _status = ExecutionStatus.Failed;
            _result = null;
            _startTime = startTime;
            _endTime = endTime;
            _exception = exception;
        }
        
        /// <summary>
        /// 构造函数（取消）
        /// Constructor (canceled)
        /// </summary>
        public ExecuteResult(WorkID id, DateTime startTime, DateTime endTime)
        {
            _id = id;
            _status = ExecutionStatus.Canceled;
            _result = null;
            _startTime = startTime;
            _endTime = endTime;
            _exception = null;
        }
        
        /// <summary>
        /// 构造函数（超时）
        /// Constructor (timeout)
        /// </summary>
        public ExecuteResult(WorkID id, DateTime startTime, DateTime endTime, Exception timeoutException)
        {
            _id = id;
            _status = ExecutionStatus.Timeout;
            _result = null;
            _startTime = startTime;
            _endTime = endTime;
            _exception = timeoutException;
        }
        
        /// <summary>
        /// 转换为字符串
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            return $"ExecuteResult[ID={_id}, Status={_status}, Duration={Duration.TotalMilliseconds:F1}ms Result={_result}]";
        }
    }
}