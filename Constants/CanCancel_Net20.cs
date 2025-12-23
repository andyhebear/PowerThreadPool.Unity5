using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可取消枚举 / Can cancel enumeration
    /// </summary>
    internal enum CanCancel
    {
        /// <summary>
        /// 允许取消 / Allowed to cancel
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许取消 / Not allowed to cancel
        /// </summary>
        NotAllowed = 1,
    }
}