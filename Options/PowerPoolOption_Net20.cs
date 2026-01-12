using System;
using System.Threading;

namespace PowerThreadPool_Net20.Options
{
    /// <summary>
    /// 线程池选项
    /// Thread pool options
    /// </summary>
    public class PowerPoolOption
    {
        private int _maxThreads = Environment.ProcessorCount * 2;
        private bool _enableStatisticsCollection = true;
        private bool _startSuspended = false;
        private int _threadQueueLimit = 100;
        //
        private TimeSpan _idleThreadTimeout = TimeSpan.FromMinutes(5);
        private int _minThreads = 1;
        private TimeSpan _resultCacheExpiration = TimeSpan.FromMinutes(10);
        private bool _enableResultCacheExpiration = true;
        
        /// <summary>
        /// 最大线程数
        /// Maximum number of threads
        /// </summary>
        public int MaxThreads
        {
            get { return _maxThreads; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("MaxThreads must be greater than 0");
                _maxThreads = value;
            }
        }
        
        /// <summary>
        /// 是否启用统计收集
        /// Whether to enable statistics collection
        /// </summary>
        public bool EnableStatisticsCollection
        {
            get { return _enableStatisticsCollection; }
            set { _enableStatisticsCollection = value; }
        }
        
        /// <summary>
        /// 是否以暂停状态启动
        /// Whether to start in suspended state
        /// </summary>
        public bool StartSuspended
        {
            get { return _startSuspended; }
            set { _startSuspended = value; }
        }
        
        /// <summary>
        /// 线程队列限制
        /// Thread queue limit
        /// </summary>
        public int ThreadQueueLimit
        {
            get { return _threadQueueLimit; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("ThreadQueueLimit must be greater than 0");
                _threadQueueLimit = value;
            }
        }
        
       
        
        /// <summary>
        /// 空闲线程超时时间（在保证最小线程运行数的情况下，如果工作线程空闲时间超过该设置时间则停止该工作线程）
        /// Idle thread timeout duration
        /// </summary>
        public TimeSpan IdleThreadTimeout
        {
            get { return _idleThreadTimeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("IdleThreadTimeout must be greater than zero");
                _idleThreadTimeout = value;
            }
        }
        
        /// <summary>
        /// 最小线程数
        /// Minimum number of threads
        /// </summary>
        public int MinThreads
        {
            get { return _minThreads; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("MinThreads must be greater than 0");
                _minThreads = value;
            }
        }

        /// <summary>
        /// 结果缓存过期时间
        /// Result cache expiration duration
        /// </summary>
        public TimeSpan ResultCacheExpiration
        {
            get { return _resultCacheExpiration; }
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentException("ResultCacheExpiration must be non-negative");
                _resultCacheExpiration = value;
            }
        }

        /// <summary>
        /// 是否启用结果缓存过期
        /// Whether to enable result cache expiration
        /// </summary>
        public bool EnableResultCacheExpiration
        {
            get { return _enableResultCacheExpiration; }
            set { _enableResultCacheExpiration = value; }
        }
        
        /// <summary>
        /// 线程优先级
        /// Thread priority
        /// </summary>
        public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;
        
        /// <summary>
        /// 是否使用后台线程
        /// Whether to use background threads
        /// </summary>
        public bool UseBackgroundThreads { get; set; } = true;
        
        /// <summary>
        /// 线程名称前缀
        /// Thread name prefix
        /// </summary>
        public string ThreadNamePrefix { get; set; } = "PowerPool";
        
        /// <summary>
        /// 默认构造函数
        /// Default constructor
        /// </summary>
        public PowerPoolOption()
        {
        }
        
        /// <summary>
        /// 复制构造函数
        /// Copy constructor
        /// </summary>
        public PowerPoolOption(PowerPoolOption other)
        {
            if (other != null)
            {
                _maxThreads = other._maxThreads;
                _enableStatisticsCollection = other._enableStatisticsCollection;
                _startSuspended = other._startSuspended;
                _threadQueueLimit = other._threadQueueLimit;
             
                _idleThreadTimeout = other._idleThreadTimeout;
                _minThreads = other._minThreads;
                ThreadPriority = other.ThreadPriority;
                UseBackgroundThreads = other.UseBackgroundThreads;
                ThreadNamePrefix = other.ThreadNamePrefix;
            }
        }
        
        /// <summary>
        /// 创建默认选项
        /// Create default options
        /// </summary>
        public static PowerPoolOption Default => new PowerPoolOption();
        
        /// <summary>
        /// 创建最小化选项
        /// Create minimal options
        /// </summary>
        public static PowerPoolOption Minimal => new PowerPoolOption
        {
            EnableStatisticsCollection = false,
            ThreadQueueLimit = 10
        };
        
        /// <summary>
        /// 创建高性能选项
        /// Create high performance options
        /// </summary>
        public static PowerPoolOption HighPerformance => new PowerPoolOption
        {
            MaxThreads = Environment.ProcessorCount * 4,
            MinThreads = Environment.ProcessorCount,
            ThreadQueueLimit = 1000,
            EnableStatisticsCollection = false,
            IdleThreadTimeout = TimeSpan.FromMinutes(1) // 高性能模式下更激进的线程回收
        };
    }
}