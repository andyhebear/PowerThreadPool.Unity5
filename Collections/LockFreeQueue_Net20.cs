using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PowerThreadPool_Net20.Collections
{
    internal class SingleLinkNode<T>
    {
        public SingleLinkNode<T> Next;
        public T Item;
    }
    /// <summary>
	/// Represents a lock-free, thread-safe, last-in, first-out collection of objects.
	/// </summary>
	/// <typeparam name="T">specifies the type of the elements in the stack</typeparam>
    public class LockFreeStack<T>:IDisposable
    {
        private SingleLinkNode<T> _head;
        private volatile int _disposing;
        private int _count;

        /// <summary>
        /// 获取栈中的元素数量
        /// Gets the number of elements in the stack
        /// </summary>
        public int Count
        {
            get { return Thread.VolatileRead(ref _count); }
        }

        /// <summary>
        /// Default constructors.
        /// </summary>
        public LockFreeStack() {
            _head = new SingleLinkNode<T>();
        }

        /// <summary>
        /// Inserts an object at the top of the stack.
        /// </summary>
        /// <param name="item">the object to push onto the stack</param>
        public void Push(T item) {
            SingleLinkNode<T> newNode = new SingleLinkNode<T>();
            newNode.Item = item;

            do {
                newNode.Next = _head.Next;
            } while (Interlocked.CompareExchange<SingleLinkNode<T>>(ref _head.Next,newNode,newNode.Next) != newNode.Next);

            Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// Removes and returns the object at the top of the stack.
        /// </summary>
        /// <param name="item">
        /// when the method returns, contains the object removed from the top of the stack, 
        /// if the queue is not empty; otherwise it is the default value for the element type
        /// </param>
        /// <returns>
        /// true if an object from removed from the top of the stack 
        /// false if the stack is empty
        /// </returns>
        public bool Pop(out T item) {
            SingleLinkNode<T> node;

            do {
                node = _head.Next;

                if (node == null) {
                    item = default(T);
                    return false;
                }
            } while (Interlocked.CompareExchange<SingleLinkNode<T>>(ref _head.Next,node.Next,node) != node);

            item = node.Item;

            Interlocked.Decrement(ref _count);
            return true;
        }

        /// <summary>
        /// Removes and returns the object at the top of the stack.
        /// </summary>
        /// <returns>the object that is removed from the top of the stack</returns>
        public T Pop() {
            T result;

            if (!Pop(out result))
                throw new InvalidOperationException("the stack is empty");

            return result;
        }

        /// <summary>
        /// 查看栈顶元素但不移除
        /// Peeks at the top of the stack without removing it
        /// </summary>
        /// <param name="item">when the method returns, contains the object at the top of the stack</param>
        /// <returns>true if the stack is not empty; otherwise, false</returns>
        public bool TryPeek(out T item) {
            SingleLinkNode<T> node = _head.Next;

            if (node == null) {
                item = default(T);
                return false;
            }

            item = node.Item;
            return true;
        }

        /// <summary>
        /// 查看栈顶元素但不移除
        /// Peeks at the top of the stack without removing it
        /// </summary>
        /// <returns>the object at the top of the stack</returns>
        public T Peek() {
            T result;

            if (!TryPeek(out result))
                throw new InvalidOperationException("the stack is empty");

            return result;
        }

        /// <summary>
        /// 清空栈
        /// Clears the stack
        /// </summary>
        /// <remarks>This method is not thread-safe.</remarks>
        public void Clear() {
            SingleLinkNode<T> tempNode;
            SingleLinkNode<T> currentNode = _head;

            while (currentNode != null) {
                tempNode = currentNode;
                currentNode = currentNode.Next;

                tempNode.Item = default(T);
                tempNode.Next = null;
            }

            _head = new SingleLinkNode<T>();
            _count = 0;
        }

        /// <summary>
        /// 将栈元素复制到数组
        /// Copies the stack elements to an array
        /// </summary>
        /// <returns>an array containing all elements in the stack</returns>
        public T[] ToArray() {
            // 检查是否正在释放
            if (_disposing != 0)
                return new T[0];

            var snapshot = new List<T>();
            SingleLinkNode<T> currentNode = _head.Next;

            Thread.MemoryBarrier();

            while (currentNode != null)
            {
                snapshot.Add(currentNode.Item);
                currentNode = currentNode.Next;

                // 防止无限循环，设置合理上限
                if (snapshot.Count > _count * 2)
                    break;
            }

            return snapshot.ToArray();
        }

        #region IDisposable Support

        private bool _disposed = false;

        /// <summary>
        /// 检查栈是否正在释放
        /// Check if stack is being disposed
        /// </summary>
        public bool IsDisposing
        {
            get { return _disposing != 0; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposing, 1, 0) == 0)
            {
                if (disposing)
                {
                    // 等待其他操作完成，然后清理所有节点的引用
                    Thread.Sleep(1);

                    SingleLinkNode<T> currentNode = _head;
                    while (currentNode != null)
                    {
                        SingleLinkNode<T> next = currentNode.Next;
                        currentNode.Item = default(T);
                        currentNode.Next = null;
                        currentNode = next;
                    }

                    _head = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the stack.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
    /// <summary>
    /// Represents a lock-free, thread-safe, first-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T">specifies the type of the elements in the queue</typeparam>
    /// <remarks>Enumeration and clearing are not thread-safe.</remarks>
    public class LockFreeQueue<T> : IEnumerable<T>,IDisposable
    {
        private SingleLinkNode<T> _head;
        private SingleLinkNode<T> _tail;
        private int _count;
        private volatile int _disposing;

        /// <summary>
        /// 检查队列是否正在释放
        /// Check if queue is being disposed
        /// </summary>
        public bool IsDisposing
        {
            get { return _disposing != 0; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LockFreeQueue() {
            _head = new SingleLinkNode<T>();
            _tail = _head;
        }

        public LockFreeQueue(IEnumerable<T> items) : this() {
            foreach (var item in items) {
                Enqueue(item);
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the queue.
        /// </summary>
        public int Count {
            get { return Thread.VolatileRead(ref _count); }
        }

        /// <summary>
        /// Adds an object to the end of the queue.
        /// </summary>
        /// <param name="item">the object to add to the queue</param>
        public void Enqueue(T item) {
            SingleLinkNode<T> oldTail = null;
            SingleLinkNode<T> oldTailNext;

            var newNode = new SingleLinkNode<T> { Item = item };

            bool newNodeWasAdded = false;

            while (!newNodeWasAdded) {
                oldTail = _tail;
                oldTailNext = oldTail.Next;

                if (_tail == oldTail) {
                    if (oldTailNext == null) {
                        newNodeWasAdded =
                            Interlocked.CompareExchange<SingleLinkNode<T>>(ref _tail.Next,newNode,null) == null;
                    }
                    else {
                        Interlocked.CompareExchange<SingleLinkNode<T>>(ref _tail,oldTailNext,oldTail);
                    }
                }
            }

            Interlocked.CompareExchange<SingleLinkNode<T>>(ref _tail,newNode,oldTail);
            Interlocked.Increment(ref _count);
        }

        public T TryDequeue() {
            T item;
            TryDequeue(out item);
            return item;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <param name="item">
        /// when the method returns, contains the object removed from the beginning of the queue, 
        /// if the queue is not empty; otherwise it is the default value for the element type
        /// </param>
        /// <returns>
        /// true if an object from removed from the beginning of the queue; 
        /// false if the queue is empty
        /// </returns>
        public bool TryDequeue(out T item) {
            item = default(T);
            SingleLinkNode<T> oldHead = null;

            bool haveAdvancedHead = false;
            while (!haveAdvancedHead) {
                oldHead = _head;
                SingleLinkNode<T> oldTail = _tail;
                SingleLinkNode<T> oldHeadNext = oldHead.Next;

                if (oldHead == _head) {
                    if (oldHead == oldTail) {
                        if (oldHeadNext == null)
                            return false;

                        Interlocked.CompareExchange<SingleLinkNode<T>>(ref _tail,oldHeadNext,oldTail);
                    }

                    else {
                        item = oldHeadNext.Item;
                        haveAdvancedHead =
                          Interlocked.CompareExchange<SingleLinkNode<T>>(ref _head,oldHeadNext,oldHead) == oldHead;
                    }
                }
            }

            Interlocked.Decrement(ref _count);
            return true;
        }

        /// <summary>
        /// 查看队首元素但不移除
        /// Peeks at the first element without removing it
        /// </summary>
        /// <param name="item">when the method returns, contains the object at the front of the queue</param>
        /// <returns>true if the queue is not empty; otherwise, false</returns>
        public bool TryPeek(out T item)
        {
            item = default(T);

            while (true)
            {
                SingleLinkNode<T> oldHead = _head;
                SingleLinkNode<T> oldTail = _tail;
                SingleLinkNode<T> oldHeadNext = oldHead.Next;

                if (oldHead == _head)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                            return false;

                        Interlocked.CompareExchange<SingleLinkNode<T>>(ref _tail,oldHeadNext,oldTail);
                    }
                    else
                    {
                        item = oldHeadNext.Item;

                        if (!typeof(T).IsValueType && item == null)
                        {
                            // 如果值为 null，重试
                            continue;
                        }

                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// 查看队首元素但不移除
        /// Peeks at the first element without removing it
        /// </summary>
        /// <returns>the object at the front of the queue</returns>
        public T Peek()
        {
            T result;

            if (!TryPeek(out result))
                throw new InvalidOperationException("the queue is empty");

            return result;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <returns>the object that is removed from the beginning of the queue</returns>
        public T Dequeue() {
            T result;

            if (!TryDequeue(out result)) {
                throw new InvalidOperationException("the queue is empty");
            }

            return result;
        }

        #region IEnumerable<T> Members

        /// <summary>
        /// Returns an enumerator that iterates through the queue.
        /// </summary>
        /// <returns>an enumerator for the queue</returns>
        public IEnumerator<T> GetEnumerator() {
            SingleLinkNode<T> currentNode = _head;

            do {
                if (currentNode.Item == null) {
                    yield break;
                }
                else {
                    yield return currentNode.Item;
                }
            }
            while ((currentNode = currentNode.Next) != null);

            yield break;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the queue.
        /// </summary>
        /// <returns>an enumerator for the queue</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Clears the queue.
        /// </summary>
        /// <remarks>This method is not thread-safe.</remarks>
        public void Clear() {
            SingleLinkNode<T> tempNode;
            SingleLinkNode<T> currentNode = _head;

            while (currentNode != null) {
                tempNode = currentNode;
                currentNode = currentNode.Next;

                tempNode.Item = default(T);
                tempNode.Next = null;
            }

            _head = new SingleLinkNode<T>();
            _tail = _head;
            _count = 0;
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

            var snapshot = new List<T>();
            SingleLinkNode<T> currentNode = _head.Next;

            Thread.MemoryBarrier();

            while (currentNode != null)
            {
                snapshot.Add(currentNode.Item);
                currentNode = currentNode.Next;

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
                    Thread.Sleep(1);

                    SingleLinkNode<T> currentNode = _head;
                    while (currentNode != null)
                    {
                        SingleLinkNode<T> next = currentNode.Next;
                        currentNode.Item = default(T);
                        currentNode.Next = null;
                        currentNode = next;
                    }

                    _head = null;
                    _tail = null;
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
        /// 查看队首元素（按优先级）但不移除
        /// Peeks at the first element (by priority) without removing it
        /// </summary>
        /// <param name="item">when the method returns, contains the object at the front of the queue</param>
        /// <returns>true if the queue is not empty; otherwise, false</returns>
        public bool TryPeek(out T item)
        {
            item = null;

            // 按优先级从高到低尝试查看
            for (int i = 0; i < _priorityCount; i++)
            {
                if (_queues[i].TryPeek(out item))
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
        /// 查看队首元素（按优先级）但不移除
        /// Peeks at the first element (by priority) without removing it
        /// </summary>
        /// <returns>the object at the front of the queue</returns>
        public T Peek()
        {
            T result;

            if (!TryPeek(out result))
                throw new InvalidOperationException("the queue is empty");

            return result;
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