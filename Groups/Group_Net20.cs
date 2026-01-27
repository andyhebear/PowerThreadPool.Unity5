using System;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool_Net20.Collections;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Threading;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Groups
{
    /// <summary>
    /// 工作组类（简化版，不支持父子分组）
    /// Work group class (simplified version, no parent-child groups supported)
    /// </summary>
    public class Group
    {
        internal string _groupName;
        internal PowerPool _powerPool;

        /// <summary>
        /// 构造函数（内部使用）
        /// Internal constructor
        /// </summary>
        internal Group(PowerPool powerPool,string groupName) {
            _powerPool = powerPool;
            _groupName = groupName;
        }

        /// <summary>
        /// 分组名称
        /// Group name
        /// </summary>
        public string Name => _groupName;

        /// <summary>
        /// 添加工作到分组
        /// Add work to group
        /// </summary>
        /// <param name="workID">工作ID</param>
        /// <returns>
        /// 如果工作不存在返回 false
        /// 工作可以属于多个分组
        /// Returns false if the work does not exist
        /// A work can belong to multiple groups
        /// </returns>
        public bool Add(WorkID workID) {
            return _powerPool.AddWorkToGroup(Name,workID);
        }

        /// <summary>
        /// 从分组中移除工作
        /// Remove work from group
        /// </summary>
        /// <param name="workID">工作ID</param>
        /// <returns>
        /// 如果工作不存在或不属于该分组返回 false
        /// Returns false if work does not exist, or if the work does not belong to the group
        /// </returns>
        public bool Remove(WorkID workID) {
            return _powerPool.RemoveWorkFromGroup(Name,workID);
        }

        /// <summary>
        /// 等待属于该分组的所有工作完成
        /// Wait until all the work belonging to the group is done
        /// </summary>
        public void Wait(int waitTimeoutMs = 30000) {
            var members = _powerPool.GetGroupWorkItems(Name);

            if (members.Count > 0) {
                _powerPool.WaitWorks(members,waitTimeoutMs);
            }
        }

        /// <summary>
        /// 获取分组成员列表
        /// Get group member list
        /// </summary>
        /// <returns>工作ID集合</returns>       
        public List<WorkID> GetMembers() {
            return _powerPool.GetGroupWorkItems(Name);
        }
        /// <summary>
        /// 获取工作项的执行结果
        /// Get execution results of work items
        /// </summary>
        /// <returns>执行结果列表</returns>
        public List<ExecuteResult> GetResults() {
            var members = _powerPool.GetGroupWorkItems(Name);
            var rets = _powerPool.GetResults(members.ToArray());
            List<ExecuteResult> results = new List<ExecuteResult>();
            if (rets.Length > 0) {
                results.AddRange(rets);
            }
            return results;
        }

        /// <summary>
        /// 等待分组的所有工作完成并返回结果
        /// Wait for all group work to complete and return results
        /// </summary>
        /// <returns>执行结果列表</returns>
        public List<ExecuteResult> GetResultsAndWait(int waitTimeoutMs = 30000) {
            var members = _powerPool.GetGroupWorkItems(Name);
            var rets = _powerPool.GetResultsAndWait(members.ToArray(),waitTimeoutMs);
            List<ExecuteResult> results = new List<ExecuteResult>();
            if (rets.Length > 0) {
                results.AddRange(rets);
            }
            return results;
        }

        /// <summary>
        /// 批量并行执行Func<TResult>任务
        /// Execute Func<TResult> tasks in parallel 
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="tasks">任务数组</param>
        /// <returns>执行结果列表</returns>
        public List<ExecuteResult> ExecuteParallel<TResult>(Func<TResult>[] tasks,CancellationTokenSource cancelTokenSrc = null,int execTaskTimeoutMs = 0,int waitTimeoutMs = 30000) {

            List<WorkID> workIds = new List<WorkID>();
            List<ExecuteResult> results = new List<ExecuteResult>();
            WorkOption wo = new WorkOption(
                TimeSpan.FromMilliseconds(execTaskTimeoutMs),
               cancelTokenSrc?.Token);

            // 保存原始的缓存过期设置
            bool originalExpirationSetting = _powerPool.Options.EnableResultCacheExpiration;

            try
            {
                // 临时禁用结果缓存过期，防止在等待期间结果被清理
                _powerPool.Options.EnableResultCacheExpiration = false;

                // 创建所有任务
                foreach (var task in tasks) {
                    if (task == null)
                        continue;

                    WorkID workId = _powerPool.QueueWorkItem<TResult>(task,wo);
                    workIds.Add(workId);
                    this.Add(workId);
                }

                // 等待所有任务完成
                if (workIds.Count > 0) {
                    var tempResults = _powerPool.GetResultsAndWait(workIds.ToArray(),waitTimeoutMs);
                    results.AddRange(tempResults);
                }
            }
            finally
            {
                // 恢复原始的缓存过期设置
                _powerPool.Options.EnableResultCacheExpiration = originalExpirationSetting;
            }

            return results;
        }





        /// <summary>
        /// 批量并行执行Action任务
        /// Execute Action tasks in parallel 
        /// </summary>
        /// <param name="actions">动作数组</param>
        /// <param name="timeoutMs">超时毫秒数</param>
        /// <returns>执行结果列表</returns>
        public List<ExecuteResult> ExecuteParallel(Action[] actions,CancellationTokenSource cancelTokenSrc = null,int execTaskTimeoutMs = 0,int waitTimeoutMs = 30000) {
            List<WorkID> workIds = new List<WorkID>();
            List<ExecuteResult> results = new List<ExecuteResult>();
            WorkOption wo = new WorkOption(
               TimeSpan.FromMilliseconds(execTaskTimeoutMs),
                 cancelTokenSrc?.Token
            );

            // 保存原始的缓存过期设置
            bool originalExpirationSetting = _powerPool.Options.EnableResultCacheExpiration;

            try
            {
                // 临时禁用结果缓存过期，防止在等待期间结果被清理
                _powerPool.Options.EnableResultCacheExpiration = false;

                // 创建所有任务
                foreach (var action in actions) {
                    if (action == null)
                        continue;

                    WorkID workId = _powerPool.QueueWorkItem(action,wo);
                    workIds.Add(workId);
                    this.Add(workId);
                }

                // 等待所有任务完成
                if (workIds.Count > 0) {
                    var tempResults = _powerPool.GetResultsAndWait(workIds.ToArray(),waitTimeoutMs);
                    results.AddRange(tempResults);
                }
            }
            finally
            {
                // 恢复原始的缓存过期设置
                _powerPool.Options.EnableResultCacheExpiration = originalExpirationSetting;
            }

            return results;
        }

    }
}
