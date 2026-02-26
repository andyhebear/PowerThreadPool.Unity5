using System;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Constants;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20
{
    public partial class PowerPool
    {
        #region 带参数的 Action 方法 / Action Methods with Arguments

        /// <summary>
        /// 队列工作项（带1个参数，无返回值）
        /// Queue a work item with 1 parameter and no return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <param name="action">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1>(Action<T1> action, T1 arg1, WorkOption option = null)
        {
            return QueueWorkItem<object>(() => {
                action(arg1);
                return null;
            }, option);
        }

        /// <summary>
        /// 队列工作项（带2个参数，无返回值）
        /// Queue a work item with 2 parameters and no return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <param name="action">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, WorkOption option = null)
        {
            return QueueWorkItem<object>(() => {
                action(arg1, arg2);
                return null;
            }, option);
        }

        /// <summary>
        /// 队列工作项（带3个参数，无返回值）
        /// Queue a work item with 3 parameters and no return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <typeparam name="T3">第三个参数类型 / Third parameter type</typeparam>
        /// <param name="action">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="arg3">第三个参数 / Third parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, WorkOption option = null)
        {
            return QueueWorkItem<object>(() => {
                action(arg1, arg2, arg3);
                return null;
            }, option);
        }

        /// <summary>
        /// 队列工作项（带4个参数，无返回值）
        /// Queue a work item with 4 parameters and no return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <typeparam name="T3">第三个参数类型 / Third parameter type</typeparam>
        /// <typeparam name="T4">第四个参数类型 / Fourth parameter type</typeparam>
        /// <param name="action">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="arg3">第三个参数 / Third parameter</param>
        /// <param name="arg4">第四个参数 / Fourth parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WorkOption option = null)
        {
            return QueueWorkItem<object>(() => {
                action(arg1, arg2, arg3, arg4);
                return null;
            }, option);
        }

        #endregion

        #region 带参数的 Func 方法（带返回值）/ Func Methods with Arguments and Return Value

        /// <summary>
        /// 队列工作项（带2个参数，带返回值）
        /// Queue a work item with 2 parameters and return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <typeparam name="TResult">返回值类型 / Return value type</typeparam>
        /// <param name="func">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2, WorkOption option = null)
        {
            return QueueWorkItem(() => func(arg1, arg2), option);
        }

        /// <summary>
        /// 队列工作项（带3个参数，带返回值）
        /// Queue a work item with 3 parameters and return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <typeparam name="T3">第三个参数类型 / Third parameter type</typeparam>
        /// <typeparam name="TResult">返回值类型 / Return value type</typeparam>
        /// <param name="func">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="arg3">第三个参数 / Third parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3, WorkOption option = null)
        {
            return QueueWorkItem(() => func(arg1, arg2, arg3), option);
        }

        /// <summary>
        /// 队列工作项（带4个参数，带返回值）
        /// Queue a work item with 4 parameters and return value
        /// </summary>
        /// <typeparam name="T1">第一个参数类型 / First parameter type</typeparam>
        /// <typeparam name="T2">第二个参数类型 / Second parameter type</typeparam>
        /// <typeparam name="T3">第三个参数类型 / Third parameter type</typeparam>
        /// <typeparam name="T4">第四个参数类型 / Fourth parameter type</typeparam>
        /// <typeparam name="TResult">返回值类型 / Return value type</typeparam>
        /// <param name="func">要执行的方法 / Method to execute</param>
        /// <param name="arg1">第一个参数 / First parameter</param>
        /// <param name="arg2">第二个参数 / Second parameter</param>
        /// <param name="arg3">第三个参数 / Third parameter</param>
        /// <param name="arg4">第四个参数 / Fourth parameter</param>
        /// <param name="option">工作选项 / Work option</param>
        /// <returns>工作ID / Work ID</returns>
        public WorkID QueueWorkItem<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, WorkOption option = null)
        {
            return QueueWorkItem(() => func(arg1, arg2, arg3, arg4), option);
        }

        #endregion
    }
}
