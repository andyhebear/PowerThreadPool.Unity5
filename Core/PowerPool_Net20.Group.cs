using System;
using System.Collections.Generic;
using PowerThreadPool_Net20.Collections;
using PowerThreadPool_Net20.Groups;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20
{
    public partial class PowerPool
    {
        // 分组管理（用于WorkGroup功能）
        private Dictionary<string,ConcurrentSet<WorkID>> _workGroupDic = new Dictionary<string,ConcurrentSet<WorkID>>();
        private readonly object _groupLock = new object();
        /// <summary>
        /// 获取分组对象（创建组对象），只有执行 group.Add(workId)才会在PowerPool中GroupExists(groupName)
        /// Get group object
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>分组对象</returns>
        public Group GetGroup(string groupName)
        {
            return new Group(this, groupName);
        }

        /// <summary>
        /// 获取分组的所有成员
        /// Get all members of a group
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>工作ID集合</returns>
        public IEnumerable<WorkID> GetGroupMemberList(string groupName)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<WorkID> groupMemberList))
                {
                    return groupMemberList;
                }
                return new ConcurrentSet<WorkID>();
            }
        }

        /// <summary>
        /// 获取分组中的所有工作ID（线程安全）
        /// Get all work IDs in a group (thread-safe)
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>工作ID列表</returns>
        public List<WorkID> GetGroupWorkItems(string groupName)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<WorkID> workIdSet))
                {
                    return new List<WorkID>(workIdSet);
                }
                return new List<WorkID>();
            }
        }

        /// <summary>
        /// 获取分组数量
        /// Get group count
        /// </summary>
        /// <returns>分组数量</returns>
        public int GetGroupCount()
        {
            lock (_groupLock)
            {
                return _workGroupDic.Count;
            }
        }

        /// <summary>
        /// 清空指定分组
        /// Clear a specific group
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>被移除的工作ID数量</returns>
        public int ClearGroup(string groupName)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<WorkID> workIdSet))
                {
                    int count = workIdSet.Count;
                    workIdSet.Clear();
                    _workGroupDic.Remove(groupName);
                    return count;
                }
                return 0;
            }
        }

        /// <summary>
        /// 添加工作到分组
        /// Add work to group
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="workID">工作ID</param>
        /// <returns>是否成功添加</returns>
        /// 工作可以属于多个分组
        /// A work can belong to multiple groups
        public bool AddWorkToGroup(string groupName, WorkID workID)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.ContainsKey(groupName)==false) {
                    _workGroupDic.Add(groupName,new ConcurrentSet<WorkID>());                                      
                }
                _workGroupDic[groupName].Add(workID);
                
                return true;
            }
        }

        /// <summary>
        /// 从分组中移除工作
        /// Remove work from group
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="workID">工作ID</param>
        /// <returns>
        /// 如果分组不存在，或者工作不属于该分组返回 false
        /// Returns false if either the group does not exist, or if the work does not belong to the group
        /// </returns>
        public bool RemoveWorkFromGroup(string groupName, WorkID workID)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<WorkID> workIdSet))
                {
                    return workIdSet.Remove(workID);
                }
                return false;
            }
        }

        /// <summary>
        /// 获取所有分组名称
        /// Get all group names
        /// </summary>
        /// <returns>分组名称列表</returns>
        public List<string> GetAllGroupNames()
        {
            lock (_groupLock)
            {
                return new List<string>(_workGroupDic.Keys);
            }
        }

        /// <summary>
        /// 检查分组是否存在
        /// Check if group exists
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>是否存在</returns>
        public bool GroupExists(string groupName)
        {
            lock (_groupLock)
            {
                return _workGroupDic.ContainsKey(groupName);
            }
        }

        /// <summary>
        /// 获取工作项所属的所有分组
        /// Get all groups that a work item belongs to
        /// </summary>
        /// <param name="workID">工作ID</param>
        /// <returns>分组名称列表</returns>
        public List<string> GetWorkGroups(WorkID workID)
        {
            lock (_groupLock)
            {
                List<string> groups = new List<string>();
                foreach (var kvp in _workGroupDic)
                {
                    if (kvp.Value.Contains(workID))
                    {
                        groups.Add(kvp.Key);
                    }
                }
                return groups;
            }
        }

        /// <summary>
        /// 从所有分组中移除工作项
        /// Remove work item from all groups
        /// </summary>
        /// <param name="workID">工作ID</param>
        /// <returns>被移除的分组数量</returns>
        public int RemoveWorkFromAllGroups(WorkID workID)
        {
            lock (_groupLock)
            {
                int removedCount = 0;
                foreach (var kvp in _workGroupDic)
                {
                    if (kvp.Value.Remove(workID))
                    {
                        removedCount++;
                    }
                }
                return removedCount;
            }
        }

        /// <summary>
        /// 检查工作项是否属于指定分组
        /// Check if work item belongs to specified group
        /// </summary>
        /// <param name="workID">工作ID</param>
        /// <param name="groupName">分组名称</param>
        /// <returns>是否属于该分组</returns>
        public bool IsWorkInGroup(WorkID workID, string groupName)
        {
            lock (_groupLock)
            {
                if (_workGroupDic.TryGetValue(groupName, out ConcurrentSet<WorkID> workIdSet))
                {
                    return workIdSet.Contains(workID);
                }
                return false;
            }
        }



        private void SafeDisposeGroups()
        {
            lock (_groupLock)
            {
                _workGroupDic.Clear();
            }
        }
    }
}
 