

using System;
using System.ComponentModel;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Represents information about the InvokeCompleted event.
    /// 表示 InvokeCompleted 事件的信息。
    /// </summary>
    public class InvokeCompletedEventArgs : AsyncCompletedEventArgs
    {
        /// <summary>
        /// The arguments passed to the delegate.
        /// 传递给委托的参数。
        /// </summary>
        private readonly object[] args;

        /// <summary>
        /// Initializes a new instance of the InvokeCompletedEventArgs class.
        /// 初始化 InvokeCompletedEventArgs 类的新实例。
        /// </summary>
        /// <param name="method">
        /// The delegate that was invoked.
        /// 被调用的委托。
        /// </param>
        /// <param name="args">
        /// The arguments passed to the delegate.
        /// 传递给委托的参数。
        /// </param>
        /// <param name="result">
        /// The result returned by the delegate.
        /// 委托返回的结果。
        /// </param>
        /// <param name="error">
        /// Any exception that occurred during invocation.
        /// 调用期间发生的任何异常。
        /// </param>
        public InvokeCompletedEventArgs(Delegate method, object[] args, object result, Exception error)
            : base(error, false, null)
        {
            Method = method;
            this.args = args;
            Result = result;
        }

        /// <summary>
        /// Gets the delegate that was invoked.
        /// 获取被调用的委托。
        /// </summary>
        public Delegate Method { get; }

        /// <summary>
        /// Gets the result returned by the delegate.
        /// 获取委托返回的结果。
        /// </summary>
        public object Result { get; }

        /// <summary>
        /// Gets the arguments passed to the delegate.
        /// 获取传递给委托的参数。
        /// </summary>
        /// <returns>
        /// An array of arguments.
        /// 参数数组。
        /// </returns>
        public object[] GetArgs()
        {
            return args;
        }
    }
}