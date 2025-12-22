using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 工作线程状态枚举 / Worker thread state enumeration
    /// </summary>
    internal enum WorkerStates
    {
        /// <summary>
        /// 空闲 / Idle
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 运行中 / Running
        /// </summary>
        Running = 1,
        
        /// <summary>
        /// 待销毁 / To be disposed
        /// </summary>
        ToBeDisposed = 2,
    }
}