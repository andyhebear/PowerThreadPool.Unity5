using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 线程池状态枚举 / Thread pool state enumeration
    /// </summary>
    internal enum PoolStates
    {
        /// <summary>
        /// 未运行 / Not running
        /// </summary>
        NotRunning = 0,
        
        /// <summary>
        /// 空闲已检查 / Idle checked
        /// </summary>
        IdleChecked = 1,
        
        /// <summary>
        /// 运行中 / Running
        /// </summary>
        Running = 2,
    }
}