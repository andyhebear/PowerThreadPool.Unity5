using System;
using PowerThreadPool_Net20.Threading;

namespace PowerThreadPool_Net20.Options
{
    /// <summary>
    /// 工作选项
    /// Work options
    /// </summary>
    public class WorkOption
    {
        private TimeSpan _timeout = TimeSpan.MaxValue;
        private bool _longRunning = false;
        
        /// <summary>
        /// 超时时间
        /// Timeout duration
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
        
        /// <summary>
        /// 是否长时间运行
        /// Whether long running
        /// </summary>
        public bool LongRunning
        {
            get { return _longRunning; }
            set { _longRunning = value; }
        }
        
        /// <summary>
        /// 取消令牌
        /// Cancellation token
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
        
        /// <summary>
        /// 优先级
        /// Priority
        /// </summary>
        public WorkPriority Priority { get; set; } = WorkPriority.Normal;
        
        /// <summary>
        /// 工作名称
        /// Work name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 工作分组
        /// Work group
        /// </summary>
        public string Group { get; set; }
        
        /// <summary>
        /// 用户数据
        /// User data
        /// </summary>
        public object UserData { get; set; }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkOption()
        {
        }
        
        /// <summary>
        /// 默认实例
        /// Default instance
        /// </summary>
        public static WorkOption Default => new WorkOption();
        
        /// <summary>
        /// 长时间运行实例
        /// Long running instance
        /// </summary>
        public static WorkOption LongRunningInstance => new WorkOption
        {
            LongRunning = true
        };
        
        /// <summary>
        /// 高优先级实例
        /// High priority instance
        /// </summary>
        public static WorkOption HighPriority => new WorkOption
        {
            Priority = WorkPriority.High
        };
    }
    
    /// <summary>
    /// 工作优先级
    /// Work priority
    /// </summary>
    public enum WorkPriority
    {
        /// <summary>
        /// 低优先级
        /// Low priority
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// 普通优先级
        /// Normal priority
        /// </summary>
        Normal = 1,
        
        /// <summary>
        /// 高优先级
        /// High priority
        /// </summary>
        High = 2,
        
        /// <summary>
        /// 紧急优先级
        /// Critical priority
        /// </summary>
        Critical = 3
    }
}