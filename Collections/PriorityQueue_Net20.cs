using System;
using System.Collections;
using System.Collections.Generic;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 优先级队列实现（支持WorkPriority枚举）
    /// Priority queue implementation (supports WorkPriority enum)
    /// </summary>
    internal class PriorityQueue : IEnumerable<WorkItem>
    {
        private readonly Queue<WorkItem> _criticalQueue = new Queue<WorkItem>();
        private readonly Queue<WorkItem> _highQueue = new Queue<WorkItem>();
        private readonly Queue<WorkItem> _normalQueue = new Queue<WorkItem>();
        private readonly Queue<WorkItem> _lowQueue = new Queue<WorkItem>();
        
        private readonly object _syncRoot = new object();
        
        /// <summary>
        /// 获取队列中的工作项总数
        /// Get total count of work items in the queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _criticalQueue.Count + _highQueue.Count + _normalQueue.Count + _lowQueue.Count;
                }
            }
        }
        
        /// <summary>
        /// 将工作项按优先级入队
        /// Enqueue work item with priority
        /// </summary>
        public void Enqueue(WorkItem workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));
            
            lock (_syncRoot)
            {
                switch (workItem.Option.Priority)
                {
                    case WorkPriority.Critical:
                        _criticalQueue.Enqueue(workItem);
                        break;
                    case WorkPriority.High:
                        _highQueue.Enqueue(workItem);
                        break;
                    case WorkPriority.Normal:
                        _normalQueue.Enqueue(workItem);
                        break;
                    case WorkPriority.Low:
                        _lowQueue.Enqueue(workItem);
                        break;
                    default:
                        _normalQueue.Enqueue(workItem);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 按优先级顺序出队（Critical > High > Normal > Low）
        /// Dequeue work item in priority order (Critical > High > Normal > Low)
        /// </summary>
        public WorkItem Dequeue()
        {
            lock (_syncRoot)
            {
                if (_criticalQueue.Count > 0)
                    return _criticalQueue.Dequeue();
                
                if (_highQueue.Count > 0)
                    return _highQueue.Dequeue();
                
                if (_normalQueue.Count > 0)
                    return _normalQueue.Dequeue();
                
                if (_lowQueue.Count > 0)
                    return _lowQueue.Dequeue();
                
                return null;
            }
        }
        
        /// <summary>
        /// 尝试按优先级顺序出队
        /// Try to dequeue work item in priority order
        /// </summary>
        public bool TryDequeue(out WorkItem workItem)
        {
            lock (_syncRoot)
            {
                workItem = Dequeue();
                return workItem != null;
            }
        }
        
        /// <summary>
        /// 查看队列中的下一个工作项（不移除）
        /// Peek at the next work item in the queue (without removing)
        /// </summary>
        public WorkItem Peek()
        {
            lock (_syncRoot)
            {
                if (_criticalQueue.Count > 0)
                    return _criticalQueue.Peek();
                
                if (_highQueue.Count > 0)
                    return _highQueue.Peek();
                
                if (_normalQueue.Count > 0)
                    return _normalQueue.Peek();
                
                if (_lowQueue.Count > 0)
                    return _lowQueue.Peek();
                
                return null;
            }
        }
        
        /// <summary>
        /// 清空队列中的所有工作项
        /// Clear all work items from the queue
        /// </summary>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _criticalQueue.Clear();
                _highQueue.Clear();
                _normalQueue.Clear();
                _lowQueue.Clear();
            }
        }
        
        /// <summary>
        /// 获取队列中每个优先级的数量
        /// Get count for each priority level
        /// </summary>
        public void GetPriorityCounts(out int critical, out int high, out int normal, out int low)
        {
            lock (_syncRoot)
            {
                critical = _criticalQueue.Count;
                high = _highQueue.Count;
                normal = _normalQueue.Count;
                low = _lowQueue.Count;
            }
        }
        
        /// <summary>
        /// 获取枚举器（按优先级顺序：Critical > High > Normal > Low）
        /// Get enumerator (in priority order: Critical > High > Normal > Low)
        /// </summary>
        public IEnumerator<WorkItem> GetEnumerator()
        {
            lock (_syncRoot)
            {
                // 按优先级顺序返回所有工作项
                foreach (var item in _criticalQueue)
                    yield return item;
                foreach (var item in _highQueue)
                    yield return item;
                foreach (var item in _normalQueue)
                    yield return item;
                foreach (var item in _lowQueue)
                    yield return item;
            }
        }
        
        /// <summary>
        /// 获取非泛型枚举器
        /// Get non-generic enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}