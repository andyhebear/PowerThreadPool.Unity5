using System;
using System.IO;
using System.Threading;

namespace PowerThreadPool_Net20.Logging
{
    /// <summary>
    /// 日志级别枚举
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    /// 日志接口
    /// Log interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录日志
        /// Log message
        /// </summary>
        void Log(LogLevel level, string message, Exception exception = null);

        /// <summary>
        /// 记录跟踪日志
        /// Log trace message
        /// </summary>
        void Trace(string message);

        /// <summary>
        /// 记录调试日志
        /// Log debug message
        /// </summary>
        void Debug(string message);

        /// <summary>
        /// 记录信息日志
        /// Log info message
        /// </summary>
        void Info(string message);

        /// <summary>
        /// 记录警告日志
        /// Log warning message
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// 记录错误日志
        /// Log error message
        /// </summary>
        void Error(string message, Exception exception = null);

        /// <summary>
        /// 记录严重错误日志
        /// Log critical message
        /// </summary>
        void Critical(string message, Exception exception = null);

        /// <summary>
        /// 最小日志级别
        /// Minimum log level
        /// </summary>
        LogLevel MinLevel { get; set; }
    }

    /// <summary>
    /// 控制台日志记录器
    /// Console logger
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private LogLevel _minLevel = LogLevel.Info;

        public LogLevel MinLevel
        {
            get { return _minLevel; }
            set { _minLevel = value; }
        }

        public virtual void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < _minLevel)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();

            if (exception != null)
            {
                message += $" Exception: {exception.Message}";
                if (level >= LogLevel.Debug)
                {
                    message += $"\nStackTrace: {exception.StackTrace}";
                }
            }

            Console.WriteLine($"[{timestamp}] [{levelStr}] {message}");
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);
    }

    /// <summary>
    /// 文件日志记录器
    /// File logger
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();
        private LogLevel _minLevel = LogLevel.Info;
        private StreamWriter _logWriter;
        private Timer _flushTimer;
        private bool _disposed = false;

        public LogLevel MinLevel
        {
            get { return _minLevel; }
            set { _minLevel = value; }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public FileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            InitializeLogFile();
            
            // 设置定时刷新（每5秒刷新一次）
            _flushTimer = new Timer(FlushLog, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private void InitializeLogFile()
        {
            try
            {
                // 确保日志目录存在
                string logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 创建或追加日志文件
                _logWriter = new StreamWriter(_logFilePath, true, System.Text.Encoding.UTF8);
                _logWriter.AutoFlush = false; // 手动控制刷新以提高性能
            }
            catch (Exception ex)
            {
                // 如果初始化失败，回退到控制台
                Console.WriteLine($"Failed to initialize file logger: {ex.Message}");
                _logWriter = null;
            }
        }

        public virtual void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < _minLevel || _disposed || _logWriter == null)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();

            if (exception != null)
            {
                message += $" Exception: {exception.Message}";
                if (level >= LogLevel.Debug)
                {
                    message += $"\nStackTrace: {exception.StackTrace}";
                }
            }

            string logLine = $"[{timestamp}] [{levelStr}] {message}";

            lock (_lockObject)
            {
                try
                {
                    _logWriter.WriteLine(logLine);
                }
                catch (Exception ex)
                {
                    // 写入失败时回退到控制台
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    Console.WriteLine(logLine);
                }
            }
        }

        private void FlushLog(object state)
        {
            if (_disposed || _logWriter == null)
                return;

            lock (_lockObject)
            {
                try
                {
                    _logWriter.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to flush log file: {ex.Message}");
                }
            }
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_flushTimer != null)
            {
                _flushTimer.Dispose();
                _flushTimer = null;
            }

            if (_logWriter != null)
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logWriter.Flush();
                        _logWriter.Close();
                        _logWriter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing log file: {ex.Message}");
                    }
                    finally
                    {
                        _logWriter = null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 组合日志记录器（同时输出到控制台和文件）
    /// Composite logger (outputs to both console and file)
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly ILogger[] _loggers;

        public LogLevel MinLevel 
        { 
            get => _loggers[0]?.MinLevel ?? LogLevel.Info; 
            set 
            { 
                foreach (var logger in _loggers)
                {
                    if (logger != null)
                        logger.MinLevel = value;
                }
            } 
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers = loggers ?? new ILogger[0];
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            foreach (var logger in _loggers)
            {
                try
                {
                    logger?.Log(level, message, exception);
                }
                catch
                {
                    // 忽略日志记录器的异常，避免影响主程序
                }
            }
        }

        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);
    }

    /// <summary>
    /// 日志工厂类
    /// Logger factory class
    /// </summary>
    public static class LoggerFactory
    {
        private static ILogger _defaultLogger;

        /// <summary>
        /// 默认日志记录器
        /// Default logger
        /// </summary>
        public static ILogger Default
        {
            get
            {
                if (_defaultLogger == null)
                {
                    _defaultLogger = new ConsoleLogger();
                }
                return _defaultLogger;
            }
            set
            {
                _defaultLogger = value;
            }
        }

        /// <summary>
        /// 创建控制台日志记录器
        /// Create console logger
        /// </summary>
        public static ILogger CreateConsoleLogger(LogLevel minLevel = LogLevel.Info)
        {
            return new ConsoleLogger { MinLevel = minLevel };
        }

        /// <summary>
        /// 创建文件日志记录器
        /// Create file logger
        /// </summary>
        public static ILogger CreateFileLogger(string logFilePath, LogLevel minLevel = LogLevel.Info)
        {
            return new FileLogger(logFilePath) { MinLevel = minLevel };
        }

        /// <summary>
        /// 创建组合日志记录器
        /// Create composite logger
        /// </summary>
        public static ILogger CreateCompositeLogger(params ILogger[] loggers)
        {
            return new CompositeLogger(loggers);
        }

        /// <summary>
        /// 创建带文件和控制台输出的日志记录器
        /// Create logger with both file and console output
        /// </summary>
        public static ILogger CreateDefaultLogger(string logFilePath = null, LogLevel minLevel = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                // 默认日志文件路径
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                logFilePath = Path.Combine(appData, "PowerThreadPool", "Logs", $"PowerThreadPool_{DateTime.Now:yyyyMMdd}.log");
            }

            var consoleLogger = CreateConsoleLogger(minLevel);
            var fileLogger = CreateFileLogger(logFilePath, minLevel);
            
            return CreateCompositeLogger(consoleLogger, fileLogger);
        }
    }
}