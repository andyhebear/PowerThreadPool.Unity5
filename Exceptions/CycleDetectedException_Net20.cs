using System;

namespace PowerThreadPool_Net20.Exceptions
{
    /// <summary>
    /// 循环检测异常 / Cycle detected exception
    /// </summary>
    public class CycleDetectedException : WorkExceptionBase
    {
        /// <summary>
        /// 默认构造函数 / Default constructor
        /// </summary>
        public CycleDetectedException() { }

        /// <summary>
        /// 带消息的构造函数 / Constructor with message
        /// </summary>
        /// <param name="message">异常消息 / Exception message</param>
        public CycleDetectedException(string message) : base(message) { }

        /// <summary>
        /// 带消息和内部异常的构造函数 / Constructor with message and inner exception
        /// </summary>
        /// <param name="message">异常消息 / Exception message</param>
        /// <param name="innerException">内部异常 / Inner exception</param>
        public CycleDetectedException(string message, Exception innerException) : base(message, innerException) { }
    }
}