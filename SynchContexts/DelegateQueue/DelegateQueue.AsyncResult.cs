using System;

namespace PowerThreadPool_Net20.SynchContexts
{
    public partial class DelegateQueue
    {
        /// <summary>
        /// Notification type for asynchronous operations.
        /// 异步操作的通知类型。
        /// </summary>
        private enum NotificationType
        {
            None,
            BeginInvokeCompleted,
            PostCompleted
        }

        /// <summary>
        /// Implements the IAsyncResult interface for the DelegateQueue class.
        /// 为 DelegateQueue 类实现 IAsyncResult 接口。
        /// </summary>
        private class DelegateQueueAsyncResult : AsyncResult
        {
            /// <summary>
            /// Args to be passed to the delegate.
            /// 要传递给委托的参数。
            /// </summary>
            private readonly object[] args;

            /// <summary>
            /// Represents a possible exception thrown by invoking the method.
            /// 表示调用方法可能抛出的异常。
            /// </summary>
            private Exception error;

            /// <summary>
            /// The delegate to be invoked.
            /// 要调用的委托。
            /// </summary>
            private readonly Delegate method;

            /// <summary>
            /// The object returned from the delegate.
            /// 从委托返回的对象。
            /// </summary>
            private object returnValue;

            public DelegateQueueAsyncResult(
                object owner,
                Delegate method,
                object[] args,
                bool synchronously,
                NotificationType notificationType)
                : base(owner, null, null)
            {
                Method = method;
                this.args = args;
                NotificationType = notificationType;
            }

            public DelegateQueueAsyncResult(
                object owner,
                AsyncCallback callback,
                object state,
                Delegate method,
                object[] args,
                bool synchronously,
                NotificationType notificationType)
                : base(owner, callback, state)
            {
                Method = method;
                this.args = args;
                NotificationType = notificationType;
            }

            public object ReturnValue { get; private set; }

            public Exception Error { get; set; }

            public Delegate Method { get; }

            public NotificationType NotificationType { get; }

            public void Invoke()
            {
                try
                {
                    ReturnValue = Method.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
                finally
                {
                    Signal();
                }
            }

            public object[] GetArgs()
            {
                return args;
            }
        }
    }
}