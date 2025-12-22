using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 工作可窃取性枚举 / Work stealability enumeration
    /// </summary>
    internal enum WorkStealability
    {
        /// <summary>
        /// 允许窃取 / Allowed to steal
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许窃取 / Not allowed to steal
        /// </summary>
        NotAllowed = 1,
    }
}