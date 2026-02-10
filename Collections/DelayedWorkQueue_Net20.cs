using System;
using System.Collections.Generic;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 基于执行时间的优先级队列（用于延迟任务）
    /// Time-based priority queue for delayed works
    /// 
    /// 使用最小堆实现，堆顶始终是最早需要执行的任务
    /// Uses min-heap implementation, heap top always contains the earliest task to execute
    /// </summary>
    internal class DelayedWorkQueue
    {
        /// <summary>
        /// 延迟工作节点
        /// Delayed work node
        /// </summary>
        private class DelayedWorkNode
        {
            public WorkItem WorkItem { get; set; }
            public DateTime ExecuteTime { get; set; }
            public int HeapIndex { get; set; }
        }

        private List<DelayedWorkNode> _heap;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 队列中的工作项数量
        /// Number of work items in queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _heap.Count;
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public DelayedWorkQueue()
        {
            _heap = new List<DelayedWorkNode>();
        }

        /// <summary>
        /// 添加延迟工作项
        /// Add delayed work item
        /// 
        /// 复杂度：O(log n)
        /// Complexity: O(log n)
        /// </summary>
        /// <param name="workItem">工作项 / Work item</param>
        /// <param name="executeTime">执行时间 / Execution time</param>
        public void Enqueue(WorkItem workItem, DateTime executeTime)
        {
            if (workItem == null)
                throw new ArgumentNullException("workItem");

            lock (_lockObject)
            {
                var node = new DelayedWorkNode
                {
                    WorkItem = workItem,
                    ExecuteTime = executeTime,
                    HeapIndex = _heap.Count
                };
                _heap.Add(node);
                HeapifyUp(_heap.Count - 1);
            }
        }

        /// <summary>
        /// 移除指定工作项
        /// Remove specified work item
        ///
        /// 复杂度：O(n) - 需要线性查找节点
        /// Complexity: O(n) - requires linear search for the node
        ///
        /// 注意：立即从堆中删除以避免内存泄漏
        /// Note: Immediately removes from heap to avoid memory leak
        /// </summary>
        /// <param name="workID">工作ID / Work ID</param>
        /// <returns>是否成功移除 / Whether successfully removed</returns>
        public bool TryRemove(WorkID workID)
        {
            if (workID == WorkID.Empty)
                return false;

            lock (_lockObject)
            {
                // 线性查找节点
                // Linear search for node
                for (int i = 0; i < _heap.Count; i++)
                {
                    if (_heap[i].WorkItem.ID == workID)
                    {
                        // 标记为已取消
                        _heap[i].WorkItem.IsDelayedWork = false;

                        // 立即从堆中删除（与最后一个元素交换）
                        // Immediately remove from heap (swap with last element)
                        int lastIndex = _heap.Count - 1;
                        Swap(i, lastIndex);
                        _heap.RemoveAt(lastIndex);

                        // 如果删除的不是最后一个元素，需要重新调整堆
                        // If deleted element was not the last one, need to reheapify
                        if (i < lastIndex)
                        {
                            // 先尝试向上调整，再尝试向下调整
                            // Try heapify up first, then heapify down
                            HeapifyUp(i);
                            HeapifyDown(i);
                        }

                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 查找并移除所有到期的工作项
        /// Find and remove all expired work items
        ///
        /// 复杂度：O(k * log n)，k 是到期任务数
        /// Complexity: O(k * log n), where k is number of expired tasks
        /// </summary>
        /// <param name="now">当前时间 / Current time</param>
        /// <returns>到期的工作项列表 / List of expired work items</returns>
        public List<WorkItem> DequeueExpired(DateTime now)
        {
            List<WorkItem> expiredWorks = new List<WorkItem>();

            lock (_lockObject)
            {
                // 堆顶是最早的任务
                // Heap top is the earliest task
                while (_heap.Count > 0)
                {
                    var top = _heap[0];

                    // 检查是否到期
                    // Check if expired
                    if (top.ExecuteTime > now)
                        break;  // 未到期，停止 / Not expired, stop

                    // 从堆顶移除元素（与最后一个元素交换后删除）
                    // Remove element from heap top (swap with last then remove)
                    Swap(0, _heap.Count - 1);
                    _heap.RemoveAt(_heap.Count - 1);

                    // 如果还有元素，需要向下调整堆
                    // If there are still elements, need to heapify down
                    if (_heap.Count > 0)
                        HeapifyDown(0);

                    // 只添加有效（未取消）的任务到结果列表
                    // Only add valid (not cancelled) tasks to result list
                    if (top.WorkItem.IsDelayedWork)
                    {
                        expiredWorks.Add(top.WorkItem);
                    }
                }
            }

            return expiredWorks;
        }

        /// <summary>
        /// 查看最早到期的工作项（不移除）
        /// Peek at earliest expired work item (without removing)
        /// 
        /// 复杂度：O(1)
        /// Complexity: O(1)
        /// </summary>
        /// <returns>工作项 / Work item，如果队列空返回 null</returns>
        public WorkItem Peek()
        {
            lock (_lockObject)
            {
                if (_heap.Count == 0)
                    return null;

                return _heap[0].WorkItem;
            }
        }

        /// <summary>
        /// 查看最早到期的工作项的执行时间
        /// Peek at execution time of earliest work item
        /// 
        /// 复杂度：O(1)
        /// Complexity: O(1)
        /// </summary>
        /// <returns>执行时间 / Execution time，如果队列空返回 null</returns>
        public DateTime? PeekExecuteTime()
        {
            lock (_lockObject)
            {
                if (_heap.Count == 0)
                    return null;

                return _heap[0].ExecuteTime;
            }
        }

        /// <summary>
        /// 清空队列
        /// Clear queue
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _heap.Clear();
            }
        }

        /// <summary>
        /// 向上调整堆（保持最小堆性质）
        /// Heapify up (maintain min-heap property)
        /// 
        /// 复杂度：O(log n)
        /// Complexity: O(log n)
        /// </summary>
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                
                // 如果父节点的执行时间小于等于当前节点，已满足最小堆性质
                // If parent's execute time is less than or equal to current, min-heap property satisfied
                if (_heap[parentIndex].ExecuteTime <= _heap[index].ExecuteTime)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        /// <summary>
        /// 向下调整堆（保持最小堆性质）
        /// Heapify down (maintain min-heap property)
        /// 
        /// 复杂度：O(log n)
        /// Complexity: O(log n)
        /// </summary>
        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                // 找到当前节点和子节点中最小的
                // Find smallest among current node and children
                if (leftChild < _heap.Count && 
                    _heap[leftChild].ExecuteTime < _heap[smallest].ExecuteTime)
                {
                    smallest = leftChild;
                }

                if (rightChild < _heap.Count && 
                    _heap[rightChild].ExecuteTime < _heap[smallest].ExecuteTime)
                {
                    smallest = rightChild;
                }

                if (smallest == index)
                    break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        /// <summary>
        /// 交换堆中的两个节点
        /// Swap two nodes in heap
        /// </summary>
        private void Swap(int index1, int index2)
        {
            var temp = _heap[index1];
            _heap[index1] = _heap[index2];
            _heap[index2] = temp;
            
            // 更新索引
            // Update indices
            _heap[index1].HeapIndex = index1;
            _heap[index2].HeapIndex = index2;
        }
    }
}
