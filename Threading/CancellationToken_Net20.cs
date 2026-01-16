using System;
using System.Threading;

namespace PowerThreadPool_Net20.Threading
{
    /// <summary>
    /// .NET 2.0兼容的取消令牌
    /// .NET 2.0 compatible cancellation token
    /// </summary>
    public class CancellationToken
    {
        private bool _isCancelled = false;
        private readonly object _lock = new object();
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CancellationToken()
        {
        }
        
        /// <summary>
        /// 是否已取消
        /// Whether cancelled
        /// </summary>
        public bool IsCancellationRequested
        {
            get
            {
                lock (_lock)
                {
                    return _isCancelled;
                }
            }
        }
        
        /// <summary>
        /// 取消操作
        /// Cancel operation
        /// </summary>
        internal void Cancel()
        {
            lock (_lock)
            {
                _isCancelled = true;
            }
        }
        
        /// <summary>
        /// 重置令牌
        /// Reset token
        /// </summary>
        internal void Reset()
        {
            lock (_lock)
            {
                _isCancelled = false;
            }
        }
        
        /// <summary>
        /// 抛出取消异常（如果已取消）
        /// Throw cancellation exception if cancelled
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }
    }
    
    /// <summary>
    /// 取消令牌源
    /// Cancellation token source
    /// </summary>
    public class CancellationTokenSource
    {
        private CancellationToken _token;
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CancellationTokenSource()
        {
            _token = new CancellationToken();
        }
        
        /// <summary>
        /// 获取令牌
        /// Get token
        /// </summary>
        public CancellationToken Token
        {
            get { return _token; }
        }
        
        /// <summary>
        /// 取消
        /// Cancel
        /// </summary>
        public void Cancel()
        {
            _token.Cancel();
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            // .NET 2.0中没有复杂的资源需要释放
        }
    }
}