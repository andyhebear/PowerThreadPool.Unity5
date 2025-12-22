using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可销毁枚举 / Can dispose enumeration
    /// </summary>
    internal enum CanDispose
    {
        /// <summary>
        /// 允许销毁 / Allowed to dispose
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许销毁 / Not allowed to dispose
        /// </summary>
        NotAllowed = 1,
    }
}