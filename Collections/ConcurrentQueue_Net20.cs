using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 线程安全队列，适配.NET 2.0
    /// Thread-safe queue adapted for .NET 2.0
    /// </summary>
    public class ConcurrentQueue<T> : IEnumerable<T>
    {
        private readonly Queue<T> _queue;
        private readonly object _lockObject;
        
        /// <summary>
        /// 队列中的项目数
        /// Number of items in the queue
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _queue.Count;
                }
            }
        }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public ConcurrentQueue()
        {
            _queue = new Queue<T>();
            _lockObject = new object();
        }
        
        /// <summary>
        /// 构造函数（指定容量）
        /// Constructor with specified capacity
        /// </summary>
        public ConcurrentQueue(int capacity)
        {
            _queue = new Queue<T>(capacity);
            _lockObject = new object();
        }
        
        /// <summary>
        /// 构造函数（从集合初始化）
        /// Constructor with collection initialization
        /// </summary>
        public ConcurrentQueue(IEnumerable<T> collection)
        {
            _queue = new Queue<T>(collection);
            _lockObject = new object();
        }
        
        /// <summary>
        /// 将项目添加到队列末尾
        /// Add item to the end of the queue
        /// </summary>
        public void Enqueue(T item)
        {
            lock (_lockObject)
            {
                _queue.Enqueue(item);
                Monitor.Pulse(_lockObject);
            }
        }
        
        /// <summary>
        /// 尝试从队列开头移除项目
        /// Try to remove item from the beginning of the queue
        /// </summary>
        public bool TryDequeue(out T result)
        {
            lock (_lockObject)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Dequeue();
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// 尝试查看队列开头的项目但不移除
        /// Try to peek at the item at the beginning of the queue without removing it
        /// </summary>
        public bool TryPeek(out T result)
        {
            lock (_lockObject)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.Peek();
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// 清空队列
        /// Clear the queue
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _queue.Clear();
            }
        }
        
        /// <summary>
        /// 将队列中的元素复制到新数组中
        /// Copy elements from the queue to a new array
        /// </summary>
        public T[] ToArray()
        {
            lock (_lockObject)
            {
                return _queue.ToArray();
            }
        }
        
        /// <summary>
        /// 获取枚举器
        /// Get enumerator
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockObject)
            {
                // 创建队列的快照来枚举
                T[] snapshot = _queue.ToArray();
                return ((IEnumerable<T>)snapshot).GetEnumerator();
            }
        }
        
        /// <summary>
        /// 获取枚举器
        /// Get enumerator
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        /// <summary>
        /// 转换为字符串
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            return $"ConcurrentQueue Count={Count}";
        }
    }
}