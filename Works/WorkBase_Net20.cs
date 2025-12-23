using System;

namespace PowerThreadPool_Net20.Works
{
    /// <summary>
    /// 工作基类
    /// Work base class
    /// </summary>
    public abstract class WorkBase : IDisposable
    {
        private readonly WorkID _id;
        private readonly DateTime _createTime;
        private bool _disposed;
        
        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID ID => _id;
        
        /// <summary>
        /// 创建时间
        /// Create time
        /// </summary>
        public DateTime CreateTime => _createTime;
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        protected WorkBase(WorkID id)
        {
            _id = id;
            _createTime = DateTime.Now;
            _disposed = false;
        }
        
        /// <summary>
        /// 执行工作
        /// Execute work
        /// </summary>
        public abstract object Execute();
        
        /// <summary>
        /// 检查是否已释放
        /// Check if disposed
        /// </summary>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// 释放资源
        /// Dispose resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    DisposeManagedResources();
                }
                
                // 释放非托管资源
                DisposeUnmanagedResources();
                
                _disposed = true;
            }
        }
        
        /// <summary>
        /// 释放托管资源
        /// Dispose managed resources
        /// </summary>
        protected virtual void DisposeManagedResources()
        {
            // 子类重写此方法释放托管资源
        }
        
        /// <summary>
        /// 释放非托管资源
        /// Dispose unmanaged resources
        /// </summary>
        protected virtual void DisposeUnmanagedResources()
        {
            // 子类重写此方法释放非托管资源
        }
        
        /// <summary>
        /// 析构函数
        /// Destructor
        /// </summary>
        ~WorkBase()
        {
            Dispose(false);
        }
    }
}