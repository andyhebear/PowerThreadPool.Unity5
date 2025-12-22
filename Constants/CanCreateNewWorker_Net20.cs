using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可创建新工作线程枚举 / Can create new worker enumeration
    /// </summary>
    internal enum CanCreateNewWorker
    {
        /// <summary>
        /// 允许创建 / Allowed to create
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许创建 / Not allowed to create
        /// </summary>
        NotAllowed = 1,
    }
}