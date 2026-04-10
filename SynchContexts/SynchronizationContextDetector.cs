using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace PowerThreadPool_Net20.SynchContexts
{
    /// <summary>
    /// Detects the current synchronization context type.
    /// 检测当前的同步上下文类型。
    /// </summary>
    public static class SynchronizationContextDetector
    {
        /// <summary>
        /// Detects the synchronization context type of the current thread.
        /// 检测当前线程的同步上下文类型。
        /// </summary>
        /// <returns>
        /// The detected synchronization context type.
        /// 检测到的同步上下文类型。
        /// </returns>
        public static SynchronizationContextType DetectCurrentContext()
        {
            SynchronizationContext currentContext = SynchronizationContext.Current;

            if (currentContext == null)
            {
                return DetectContextByEnvironment();
            }

            return DetectContextByType(currentContext);
        }

        /// <summary>
        /// Detects the synchronization context type by analyzing the context object type.
        /// 通过分析上下文对象类型来检测同步上下文类型。
        /// </summary>
        /// <param name="context">
        /// The synchronization context to analyze.
        /// 要分析的同步上下文。
        /// </param>
        /// <returns>
        /// The detected synchronization context type.
        /// 检测到的同步上下文类型。
        /// </returns>
        internal static SynchronizationContextType DetectContextByType(SynchronizationContext context)
        {
            if (context == null)
            {
                return SynchronizationContextType.DefaultConsole;
            }

            Type contextType = context.GetType();

            if (contextType.FullName != null)
            {
                string fullName = contextType.FullName;

                if (fullName.Contains("WindowsFormsSynchronizationContext") ||
                    fullName.Contains("WindowsForms"))
                {
                    return SynchronizationContextType.WindowsForms;
                }

                if (fullName.Contains("DispatcherSynchronizationContext") ||
                    fullName.Contains("Dispatcher"))
                {
                    return SynchronizationContextType.WPF;
                }

                if (fullName.Contains("AspNetSynchronizationContext") ||
                    fullName.Contains("AspNet"))
                {
                    return SynchronizationContextType.AspNet;
                }

                if (fullName.Contains("UnitySynchronizationContext") ||
                    fullName.Contains("Unity"))
                {
                    return SynchronizationContextType.Unity;
                }

                if (fullName.Contains("ConsoleSynchronizationContext") ||
                    fullName.Contains("CustomConsole"))
                {
                    return SynchronizationContextType.Custom;
                }
            }

            return SynchronizationContextType.Unknown;
        }

        /// <summary>
        /// Detects the synchronization context type by analyzing the current environment.
        /// 通过分析当前环境来检测同步上下文类型。
        /// </summary>
        /// <returns>
        /// The detected synchronization context type.
        /// 检测到的同步上下文类型。
        /// </returns>
        private static SynchronizationContextType DetectContextByEnvironment()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            if (currentDomain != null)
            {
                string friendlyName = currentDomain.FriendlyName;

                if (friendlyName != null)
                {
                    if (friendlyName.Contains("vshost") ||
                        friendlyName.Contains(".vshost"))
                    {
                        return SynchronizationContextType.DefaultConsole;
                    }

                    if (friendlyName.Contains("Unity") ||
                        friendlyName.Contains("unity"))
                    {
                        return SynchronizationContextType.Unity;
                    }
                }

                string setupInformation = currentDomain.SetupInformation != null
                    ? currentDomain.SetupInformation.ApplicationBase
                    : string.Empty;

                if (!string.IsNullOrEmpty(setupInformation))
                {
                    if (setupInformation.Contains("Unity"))
                    {
                        return SynchronizationContextType.Unity;
                    }
                }
            }

            Assembly entryAssembly = Assembly.GetEntryAssembly();

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            // AOT 和 .NET Core/.NET 5+ 中，Assembly.GetEntryAssembly() 和 GetReferencedAssemblies() 可能受限
            // Assembly.GetEntryAssembly() and GetReferencedAssemblies() may be limited in AOT and .NET Core/.NET 5+
            // 在这种情况下，我们返回默认类型，让用户手动指定
            // In this case, we return default type and let user specify manually
            if (entryAssembly == null)
            {
                return SynchronizationContextType.Default;
            }
#endif

            if (entryAssembly != null)
            {
                string entryAssemblyName = entryAssembly.GetName().Name;

                if (entryAssemblyName != null)
                {
                    if (entryAssemblyName.Contains("Unity"))
                    {
                        return SynchronizationContextType.Unity;
                    }
                }

                AssemblyName[] referencedAssemblies = entryAssembly.GetReferencedAssemblies();

                foreach (AssemblyName referencedAssemblyName in referencedAssemblies)
                {
                    if (referencedAssemblyName != null && referencedAssemblyName.Name != null)
                    {
                        if (referencedAssemblyName.Name.Contains("PresentationFramework") ||
                            referencedAssemblyName.Name.Contains("WindowsBase"))
                        {
                            return SynchronizationContextType.WPF;
                        }

                        if (referencedAssemblyName.Name.Contains("System.Windows.Forms"))
                        {
                            return SynchronizationContextType.WindowsForms;
                        }

                        if (referencedAssemblyName.Name.Contains("System.Web") ||
                            referencedAssemblyName.Name.Contains("Microsoft.AspNetCore"))
                        {
                            return SynchronizationContextType.AspNetCore;
                        }
                    }
                }
            }

            return SynchronizationContextType.DefaultConsole;
        }

        /// <summary>
        /// Checks if the current environment is a console application.
        /// 检查当前环境是否为控制台应用程序。
        /// </summary>
        /// <returns>
        /// true if the current environment is a console application; otherwise, false.
        /// 如果当前环境是控制台应用程序，则为 true；否则为 false。
        /// </returns>
        public static bool IsConsoleApplication()
        {
            return Environment.UserInteractive &&
                   Console.OpenStandardInput(1) != Stream.Null;
        }

        /// <summary>
        /// Checks if the current environment is a Windows Forms application.
        /// 检查当前环境是否为 Windows Forms 应用程序。
        /// </summary>
        /// <returns>
        /// true if the current environment is a Windows Forms application; otherwise, false.
        /// 如果当前环境是 Windows Forms 应用程序，则为 true；否则为 false。
        /// </returns>
        public static bool IsWindowsFormsApplication()
        {
            return DetectCurrentContext() == SynchronizationContextType.WindowsForms;
        }

        /// <summary>
        /// Checks if the current environment is a WPF application.
        /// 检查当前环境是否为 WPF 应用程序。
        /// </summary>
        /// <returns>
        /// true if the current environment is a WPF application; otherwise, false.
        /// 如果当前环境是 WPF 应用程序，则为 true；否则为 false。
        /// </returns>
        public static bool IsWPFApplication()
        {
            return DetectCurrentContext() == SynchronizationContextType.WPF;
        }

        /// <summary>
        /// Checks if the current environment is an ASP.NET application.
        /// 检查当前环境是否为 ASP.NET 应用程序。
        /// </summary>
        /// <returns>
        /// true if the current environment is an ASP.NET application; otherwise, false.
        /// 如果当前环境是 ASP.NET 应用程序，则为 true；否则为 false。
        /// </returns>
        public static bool IsAspNetApplication()
        {
            SynchronizationContextType contextType = DetectCurrentContext();
            return contextType == SynchronizationContextType.AspNet ||
                   contextType == SynchronizationContextType.AspNetCore;
        }

        /// <summary>
        /// Checks if the current environment is a Unity application.
        /// 检查当前环境是否为 Unity 应用程序。
        /// </summary>
        /// <returns>
        /// true if the current environment is a Unity application; otherwise, false.
        /// 如果当前环境是 Unity 应用程序，则为 true；否则为 false。
        /// </returns>
        public static bool IsUnityApplication()
        {
            return DetectCurrentContext() == SynchronizationContextType.Unity;
        }
    }
}
