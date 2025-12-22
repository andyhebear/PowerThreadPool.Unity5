using System;

namespace PowerThreadPool_Net20.Constants
{
    /// <summary>
    /// 依赖状态枚举 / Dependency status enumeration
    /// </summary>
    internal enum DependencyStatus
    {
        /// <summary>
        /// 正常 / Normal
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 已解决 / Solved
        /// </summary>
        Solved = 1,
        
        /// <summary>
        /// 失败 / Failed
        /// </summary>
        Failed = 2,
    }
}