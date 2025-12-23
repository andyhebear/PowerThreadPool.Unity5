using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可强制停止枚举 / Can force stop enumeration
    /// </summary>
    internal enum CanForceStop
    {
        /// <summary>
        /// 允许强制停止 / Allowed to force stop
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许强制停止 / Not allowed to force stop
        /// </summary>
        NotAllowed = 1,
    }
}