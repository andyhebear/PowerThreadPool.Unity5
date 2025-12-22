using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 是否可获取工作枚举 / Can get work enumeration
    /// </summary>
    internal enum CanGetWork
    {
        /// <summary>
        /// 允许获取工作 / Allowed to get work
        /// </summary>
        Allowed = 0,
        
        /// <summary>
        /// 不允许获取工作 / Not allowed to get work
        /// </summary>
        NotAllowed = 1,
        
        /// <summary>
        /// 将被禁用 / To be disabled
        /// </summary>
        ToBeDisabled = 2,
        
        /// <summary>
        /// 已禁用 / Disabled
        /// </summary>
        Disabled = -1,
    }
}