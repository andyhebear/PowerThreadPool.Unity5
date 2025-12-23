using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 监视状态枚举 / Watch state enumeration
    /// </summary>
    internal enum WatchStates
    {
        /// <summary>
        /// 空闲 / Idle
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 监视中 / Watching
        /// </summary>
        Watching = 1,
    }
}