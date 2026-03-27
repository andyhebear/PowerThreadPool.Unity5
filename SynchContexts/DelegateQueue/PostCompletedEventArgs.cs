

using System;
using System.ComponentModel;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Represents information about the PostCompleted event.
    /// 表示 PostCompleted 事件的信息。
    /// </summary>
    public class PostCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the PostCompletedEventArgs class.
        /// 初始化 PostCompletedEventArgs 类的新实例。
        /// </summary>
        /// <param name="callback">
        /// The SendOrPostCallback that was invoked.
        /// 被调用的 SendOrPostCallback。
        /// </param>
        /// <param name="error">
        /// Any exception that occurred during invocation.
        /// 调用期间发生的任何异常。
        /// </param>
        /// <param name="state">
        /// The user state object.
        /// 用户状态对象。
        /// </param>
        public PostCompletedEventArgs(SendOrPostCallback callback, Exception error, object state)
            : base(error, false, state)
        {
            Callback = callback;
        }

        /// <summary>
        /// Gets the SendOrPostCallback that was invoked.
        /// 获取被调用的 SendOrPostCallback。
        /// </summary>
        public SendOrPostCallback Callback { get; }
    }
}