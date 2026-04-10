using System;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Defines the type of synchronization context.
    /// 定义同步上下文的类型。
    /// </summary>
    public enum SynchronizationContextType
    {
        /// <summary>
        /// Unknown or custom synchronization context.
        /// 未知或自定义的同步上下文。
        /// </summary>
        Unknown,

        /// <summary>
        /// Default synchronization context (usually null in console apps).
        /// 默认同步上下文（在控制台应用中通常为null）。
        /// </summary>
        DefaultConsole,
        /// <summary>
        /// Custom  synchronization context.
        /// 自定义同步上下文,使用DelegateQueueSynchContext实现
        /// </summary>
        Custom,
        /// <summary>
        /// Windows Forms synchronization context.
        /// Windows Forms 同步上下文。
        /// </summary>
        WindowsForms,

        /// <summary>
        /// WPF synchronization context.
        /// WPF 同步上下文。
        /// </summary>
        WPF,

        /// <summary>
        /// ASP.NET synchronization context (legacy).
        /// ASP.NET 同步上下文（传统版）。
        /// </summary>
        AspNet,

        /// <summary>
        /// ASP.NET Core synchronization context (usually null).
        /// ASP.NET Core 同步上下文（通常为null）。
        /// </summary>
        AspNetCore,

        /// <summary>
        /// Unity synchronization context.
        /// Unity 同步上下文。
        /// </summary>
        Unity,

      
    }
}
