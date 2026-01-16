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
        /// <summary>
        /// 超时时间：如果为负数或者0，则不超时。如果时间超过int.MaxValue，则抛出异常。
        /// Timeout duration
        /// </summary>
        public readonly TimeSpan Timeout;

        /// <summary>
        /// 是否长时间运行（根据超时时间自动计算）
        /// Whether long running (automatically calculated based on timeout)
        /// </summary>
        public readonly bool LongRunning;

        /// <summary>
        /// 最大重试次数（默认为0，表示不重试）
        /// Maximum retry count (default is 0, meaning no retry)
        /// </summary>
        public readonly int MaxRetries;

        /// <summary>
        /// 重试间隔时间（默认为1秒）
        /// Retry interval time (default is 1 second)
        /// </summary>
        public readonly TimeSpan RetryInterval;

        /// <summary>
        /// 重试条件委托（决定是否应该重试）
        /// Retry condition delegate (decides whether to retry)
        /// </summary>
        public readonly Func<Exception,bool> RetryCondition;

        /// <summary>
        /// 根据超时时间更新LongRunning属性
        /// Update LongRunning property based on timeout
        /// </summary>
        private bool CalculateLongRunning(TimeSpan timeout) {
            // 如果超时时间超过int.MaxValue毫秒，则报错
            if (timeout.TotalMilliseconds > int.MaxValue) {
                throw new ArgumentOutOfRangeException("超时时间超过int.MaxValue");
            }
            return timeout.TotalMilliseconds <= 0;
        }

        /// <summary>
        /// 取消令牌
        /// Cancellation token
        /// </summary>
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// 优先级
        /// Priority
        /// </summary>
        public readonly WorkPriority Priority;

        /// <summary>
        /// 工作名称
        /// Work name
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// 自定义标签
        /// Work tag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 用户数据
        /// User data
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkOption() {
            Timeout = TimeSpan.Zero;
            LongRunning = true;
            MaxRetries = 0;
            RetryInterval = TimeSpan.FromSeconds(1);
            RetryCondition = null;
            CancellationToken = null;
            Priority = WorkPriority.Normal;
            Name = "";
        }

        /// <summary>
        /// 完整构造函数
        /// Full constructor
        /// </summary>
        public WorkOption(TimeSpan timeout,int maxRetries = 0,TimeSpan? retryInterval = null,
                         Func<Exception,bool> retryCondition = null,CancellationToken cancellationToken = null,
                         WorkPriority priority = WorkPriority.Normal,string name = null,object userData = null) {
            Timeout = timeout;
            LongRunning = CalculateLongRunning(timeout);
            MaxRetries = maxRetries;
            RetryInterval = retryInterval ?? TimeSpan.FromSeconds(1);
            RetryCondition = retryCondition;
            CancellationToken = cancellationToken;
            Priority = priority;
            Name = name;
            UserData = userData;
        }



        /// <summary>
        /// 构造函数（超时时间和优先级）
        /// Constructor with timeout and priority
        /// </summary>      
        public WorkOption(TimeSpan timeout,CancellationToken cancellationToken ,WorkPriority priority = WorkPriority.Normal) : this(timeout,0,TimeSpan.FromSeconds(1),null,cancellationToken,priority,null,null) {

        }
        /// <summary>
        /// 构造函数（取消令牌）
        /// Constructor with cancellation token
        /// </summary>
        public WorkOption(CancellationToken cancellationToken,WorkPriority priority = WorkPriority.Normal) : this(TimeSpan.Zero,0,TimeSpan.FromSeconds(1),null,cancellationToken,priority,null,null) {
        }
        public WorkOption(int maxRetries,TimeSpan retryInterval,Func<Exception,bool> retryCondition = null,WorkPriority priority = WorkPriority.Normal)
              : this(TimeSpan.Zero,maxRetries,retryInterval,retryCondition,null,priority,null,null) {

        }
        /// <summary>
        /// 构造函数（优先级）
        /// Constructor with priority
        /// </summary>
        public WorkOption(WorkPriority priority) : this(TimeSpan.Zero,0,TimeSpan.FromSeconds(1),null,null,priority,null,null) {
        }

        /// <summary>
        /// 默认实例
        /// Default instance
        /// </summary>
        public static WorkOption Default => new WorkOption();


        /// <summary>
        /// 高优先级实例
        /// High priority instance
        /// </summary>
        public static WorkOption HighPriority => new WorkOption(WorkPriority.High);

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