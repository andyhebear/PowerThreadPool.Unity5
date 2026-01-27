using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 无锁队列节点
    /// Lock-free queue node
    /// </summary>
    /// <typeparam name="T">节点数据类型</typeparam>
    internal class Node<T>
    {
        public T Value;
        public Node<T> Next;

        public Node(T value)
        {
            Value = value;
            Next = null;
        }
    }

    /// <summary>
    /// 无锁队列实现
    /// Lock-free queue implementation
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public class LockFreeQueue<T> : IDisposable
    {
        private volatile Node<T> _head;
        private volatile Node<T> _tail;
        private volatile int _count;
        private volatile int _disposing;

        /// <summary>
        /// 队列元素数量（近似值）
        /// Approximate queue element count
        /// </summary>
        public int Count
        {
            get { return _count; }
            //get { return Thread.VolatileRead(ref _count); }
        }

        /// <summary>
        /// 检查队列是否正在释放
        /// Check if queue is being disposed
        /// </summary>
        public bool IsDisposing
        {
            get { return _disposing != 0; }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public LockFreeQueue()
        {
            // 创建哑节点，简化入队操作
            Node<T> dummy = new Node<T>(default(T));
            _head = dummy;
            _tail = dummy;
            _count = 0;
        }

        /// <summary>
        /// 入队操作
        /// Enqueue operation
        /// </summary>
        /// <param name="item">要入队的元素</param>
        public void Enqueue(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // 检查是否正在释放
            if (_disposing != 0)
                throw new ObjectDisposedException("LockFreeQueue");

            Node<T> newNode = new Node<T>(item);
            Node<T> tail;
            Node<T> next;

            while (true)
            {
                tail = _tail;
                next = tail.Next;

                // 检查tail是否仍然指向队列末尾
                if (tail == _tail)
                {
                    if (next == null)
                    {
                        // tail确实指向队尾，尝试插入新节点
                        // 使用object版本的Interlocked.CompareExchange并进行类型转换
                        if ((Node<T>)Interlocked.CompareExchange(ref tail.Next, newNode, null) == null)
                        {
                            // 插入成功，更新tail指向新节点
                            Interlocked.CompareExchange(ref _tail, newNode, tail);
                            Interlocked.Increment(ref _count);
                            Thread.MemoryBarrier(); // 确保内存屏障
                            break;
                        }
                    }
                    else
                    {
                        // tail不是指向队尾，帮助推进tail
                        Interlocked.CompareExchange(ref _tail, next, tail);
                    }
                }
            }
        }

        /// <summary>
        /// 出队操作
        /// Dequeue operation
        /// </summary>
        /// <param name="item">出队的元素</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out T item)
        {
            item = default(T);

            // 检查是否正在释放
            if (_disposing != 0)
                return false;

            while (true)
            {
                Node<T> head = _head;
                Node<T> tail = _tail;
                Node<T> next = head.Next;

                // 检查head是否仍然指向队列头部
                if (head == _head)
                {
                    if (head == tail)
                    {
                        if (next == null)
                        {
                            // 队列为空
                            return false;
                        }

                        // 推进tail指向next
                        Interlocked.CompareExchange(ref _tail, next, tail);
                    }
                    else
                    {
                        // 尝试推进head指向next
                        if (Interlocked.CompareExchange(ref _head, next, head) == head)
                        {
                            // CAS成功后再读取值，确保一致性
                            item = next.Value;

                            // 添加 null 检查，防止返回 null 值
                            if (item == null && !typeof(T).IsValueType)
                            {
                                // 如果返回了 null，说明节点值被提前清理，重试
                                Interlocked.CompareExchange(ref _head, head, next);  // 回退
                                continue;
                            }

                            Interlocked.Decrement(ref _count);
                            Thread.MemoryBarrier(); // 确保内存屏障

                            // 延迟清理节点的值引用，防止并发读取到已清理的值
                            // 只清理 Next 引用，帮助 GC
                            head.Next = null;
                            return true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查队列是否为空
        /// Check if queue is empty
        /// </summary>
        public bool IsEmpty
        {
            get { return _count == 0; }
        }

        /// <summary>
        /// 尝试查看队首元素（不移除）
        /// Try peek at the first element without removing
        /// </summary>
        /// <param name="item">队首元素</param>
        /// <returns>是否成功查看</returns>
        public bool TryPeek(out T item)
        {
            item = default(T);

            // 检查是否正在释放
            if (_disposing != 0)
                return false;

            int retryCount = 0;
            const int maxRetries = 3;  // 限制重试次数，防止无限循环

            while (retryCount < maxRetries)
            {
                Node<T> head = _head;
                Node<T> tail = _tail;
                Node<T> next = head.Next;

                // 检查head是否仍然指向队列头部
                if (head == _head)
                {
                    if (head == tail)
                    {
                        if (next == null)
                        {
                            // 队列为空
                            return false;
                        }

                        // 推进tail指向next
                        Interlocked.CompareExchange(ref _tail, next, tail);
                    }
                    else
                    {
                        // 直接读取下一个节点的值
                        item = next.Value;

                        // 添加 null 检查，防止无限循环
                        if (!typeof(T).IsValueType && item == null)
                        {
                            // 如果值为 null，重试
                            retryCount++;
                            continue;
                        }

                        return true;
                    }
                }
            }

            // 超过重试次数，返回 false
            return false;
        }
               

        /// <summary>
        /// 清空队列
        /// Clear the queue
        /// </summary>
        public void Clear()
        {
            while (TryDequeue(out _)) { }
        }

        /// <summary>
        /// 将队列元素复制到数组
        /// Copy queue elements to array
        /// </summary>
        /// <returns>包含所有队列元素的数组</returns>
        public T[] ToArray()
        {
            // 检查是否正在释放
            if (_disposing != 0)
                return new T[0];

            // 创建快照，避免并发修改问题
            var snapshot = new List<T>();
            Node<T> current = _head.Next;
            
            Thread.MemoryBarrier(); // 确保内存屏障
            
            while (current != null)
            {
                snapshot.Add(current.Value);
                current = current.Next;
                
                // 防止无限循环，设置合理上限
                if (snapshot.Count > _count * 2)
                    break;
            }

            return snapshot.ToArray();
        }

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposing, 1, 0) == 0)
            {
                if (disposing)
                {
                    // 等待其他操作完成，然后清理所有节点的引用
                    Thread.Sleep(1); // 给其他线程一个完成的机会
                    
                    Node<T> current = _head;
                    while (current != null)
                    {
                        Node<T> next = current.Next;
                        current.Value = default(T);
                        current.Next = null;
                        current = next;
                    }
                    
                    _head = null;
                    _tail = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    /// <summary>
    /// 无锁优先级队列
    /// Lock-free priority queue
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public class LockFreePriorityQueue<T> : IEnumerable<T>,IDisposable where T : class
    {
        private readonly LockFreeQueue<T>[] _queues;
        private readonly int _priorityCount;
        private volatile int _disposing;

        /// <summary>
        /// 总元素数量（近似值）
        /// Approximate total element count
        /// </summary>
        public int Count
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _priorityCount; i++)
                {
                    total += _queues[i].Count;
                    Thread.MemoryBarrier(); // 确保内存屏障
                }
                return total;
            }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="priorityCount">优先级数量</param>
        public LockFreePriorityQueue(int priorityCount = 4)
        {
            if (priorityCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(priorityCount));

            _priorityCount = priorityCount;
            _queues = new LockFreeQueue<T>[priorityCount];

            for (int i = 0; i < priorityCount; i++)
            {
                _queues[i] = new LockFreeQueue<T>();
            }
        }

        /// <summary>
        /// 入队操作
        /// Enqueue operation
        /// </summary>
        /// <param name="item">要入队的元素</param>
        /// <param name="priority">优先级（0最高，priorityCount-1最低）</param>
        public void Enqueue(T item, int priority)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (priority < 0 || priority >= _priorityCount)
                throw new ArgumentOutOfRangeException(nameof(priority));

            _queues[priority].Enqueue(item);
        }

        /// <summary>
        /// 出队操作（按优先级）
        /// Dequeue operation (by priority)
        /// </summary>
        /// <param name="item">出队的元素</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out T item)
        {
            item = null;

            // 按优先级从高到低尝试出队
            for (int i = 0; i < _priorityCount; i++)
            {
                if (_queues[i].TryDequeue(out item))
                {
                    // 添加 null 检查，防止返回 null 值
                    if (item == null && !typeof(T).IsValueType)
                    {
                        // 如果返回了 null，继续尝试下一个优先级队列
                        continue;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试从指定优先级出队
        /// Try dequeue from specific priority
        /// </summary>
        /// <param name="item">出队的元素</param>
        /// <param name="priority">优先级</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out T item, int priority)
        {
            item = null;

            if (priority < 0 || priority >= _priorityCount)
                return false;

            if (_queues[priority].TryDequeue(out item))
            {
                // 添加 null 检查，与无参重载保持一致
                if (item == null)
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查队列是否为空
        /// Check if queue is empty
        /// </summary>
        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// 获取指定优先级的元素数量
        /// Get element count for specific priority
        /// </summary>
        /// <param name="priority">优先级</param>
        /// <returns>元素数量</returns>
        public int GetCount(int priority)
        {
            if (priority < 0 || priority >= _priorityCount)
                return 0;

            return _queues[priority].Count;
        }

        /// <summary>
        /// 清空所有优先级队列
        /// Clear all priority queues
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _priorityCount; i++)
            {
                _queues[i].Clear();
            }
        }

        /// <summary>
        /// 清空指定优先级的队列
        /// Clear queue for specific priority
        /// </summary>
        /// <param name="priority">优先级</param>
        public void Clear(int priority)
        {
            if (priority >= 0 && priority < _priorityCount)
            {
                _queues[priority].Clear();
            }
        }

        /// <summary>
        /// 获取枚举器
        /// Get enumerator
        /// </summary>
        /// <returns>元素枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            // 检查是否正在释放
            if (_disposing != 0)
                throw new ObjectDisposedException("LockFreePriorityQueue");

            // 创建快照枚举器
            return GetEnumeratorInternal();
        }

        /// <summary>
        /// 内部枚举器实现
        /// Internal enumerator implementation
        /// </summary>
        private IEnumerator<T> GetEnumeratorInternal()
        {
            Thread.MemoryBarrier();

            // 按优先级从高到低遍历
            for (int i = 0; i < _priorityCount; i++)
            {
                if (_queues[i].IsDisposing)
                    continue;

                var snapshot = _queues[i].ToArray();
                foreach (var item in snapshot)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        /// <summary>
        /// 获取枚举器（非泛型）
        /// Get enumerator (non-generic)
        /// </summary>
        /// <returns>枚举器</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposing, 1, 0) == 0)
            {
                if (disposing)
                {
                    if (_queues != null)
                    {
                        for (int i = 0; i < _queues.Length; i++)
                        {
                            _queues[i]?.Dispose();
                        }
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}