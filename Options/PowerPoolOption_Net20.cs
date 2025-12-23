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
        private TimeSpan _timeout = TimeSpan.FromHours(1);
        
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
        /// 超时时间
        /// Timeout duration
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentException("Timeout must be greater than zero");
                _timeout = value;
            }
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
                _timeout = other._timeout;
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
            ThreadQueueLimit = 1000,
            EnableStatisticsCollection = false
        };
    }
}