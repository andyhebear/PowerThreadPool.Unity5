using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可删除冗余工作线程枚举 / Can delete redundant worker enumeration
    /// </summary>
    internal enum CanDeleteRedundantWorker
    {
        /// <summary>
        /// 允许删除 / Allowed to delete
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许删除 / Not allowed to delete
        /// </summary>
        NotAllowed = 1,
    }
}