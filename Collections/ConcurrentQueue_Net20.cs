using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 线程安全队列，适配.NET 2.0,继承IProducerConsumerCollection
    /// Thread-safe queue adapted for .NET 2.0
    /// </summary>
    public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IEnumerable<T>, ICollection,
                                   IEnumerable
    {
        class Node
        {
            public T Value;
            public Node Next;
        }

        Node head = new Node();
        Node tail;
        int count;

        public ConcurrentQueue() {
            tail = head;
        }

        public ConcurrentQueue(IEnumerable<T> collection)
            : this() {
            foreach (T item in collection)
                Enqueue(item);
        }

        public void Enqueue(T item) {
            Node node = new Node();
            node.Value = item;

            Node oldTail = null;
            Node oldNext = null;

            bool update = false;
            while (!update) {
                oldTail = tail;
                oldNext = oldTail.Next;

                // Did tail was already updated ?
                if (tail == oldTail) {
                    if (oldNext == null) {
                        // The place is for us
                        update = Interlocked.CompareExchange(ref tail.Next,node,null) == null;
                    }
                    else {
                        // another Thread already used the place so give him a hand by putting tail where it should be
                        Interlocked.CompareExchange(ref tail,oldNext,oldTail);
                    }
                }
            }
            // At this point we added correctly our node, now we have to update tail. If it fails then it will be done by another thread
            Interlocked.CompareExchange(ref tail,node,oldTail);
            Interlocked.Increment(ref count);
        }

        bool IProducerConsumerCollection<T>.TryAdd(T item) {
            Enqueue(item);
            return true;
        }

        public bool TryDequeue(out T result) {
            result = default(T);
            Node oldNext = null;
            bool advanced = false;

            while (!advanced) {
                Node oldHead = head;
                Node oldTail = tail;
                oldNext = oldHead.Next;

                if (oldHead == head) {
                    // Empty case ?
                    if (oldHead == oldTail) {
                        // This should be false then
                        if (oldNext != null) {
                            // If not then the linked list is mal formed, update tail
                            Interlocked.CompareExchange(ref tail,oldNext,oldTail);
                            continue;
                        }
                        result = default(T);
                        return false;
                    }
                    else {
                        result = oldNext.Value;
                        advanced = Interlocked.CompareExchange(ref head,oldNext,oldHead) == oldHead;
                    }
                }
            }

            oldNext.Value = default(T);

            Interlocked.Decrement(ref count);

            return true;
        }

        public bool TryPeek(out T result) {
            result = default(T);
            bool update = true;

            while (update) {
                Node oldHead = head;
                Node oldNext = oldHead.Next;

                if (oldNext == null) {
                    result = default(T);
                    return false;
                }

                result = oldNext.Value;

                //check if head has been updated
                update = head != oldHead;
            }
            return true;
        }

        internal void Clear() {
            count = 0;
            tail = head = new Node();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (IEnumerator)InternalGetEnumerator();
        }

        public IEnumerator<T> GetEnumerator() {
            return InternalGetEnumerator();
        }

        IEnumerator<T> InternalGetEnumerator() {
            Node my_head = head;
            while ((my_head = my_head.Next) != null) {
                yield return my_head.Value;
            }
        }

        void ICollection.CopyTo(Array array,int index) {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank > 1)
                throw new ArgumentException("The array can't be multidimensional");
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("The array needs to be 0-based");

            T[] dest = array as T[];
            if (dest == null)
                throw new ArgumentException("The array cannot be cast to the collection element type","array");
            CopyTo(dest,index);
        }

        public void CopyTo(T[] array,int index) {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (index >= array.Length)
                throw new ArgumentException("index is equals or greather than array length","index");

            IEnumerator<T> e = InternalGetEnumerator();
            int i = index;
            while (e.MoveNext()) {
                if (i == array.Length - index)
                    throw new ArgumentException("The number of elememts in the collection exceeds the capacity of array","array");
                array[i++] = e.Current;
            }
        }

        public T[] ToArray() {
            return new List<T>(this).ToArray();
        }

        bool ICollection.IsSynchronized {
            get { return true; }
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item) {
            return TryDequeue(out item);
        }

        object syncRoot = new object();
        object ICollection.SyncRoot {
            get { return syncRoot; }
        }

        public int Count {
            get {
                return count;
            }
        }

        public bool IsEmpty {
            get {
                return count == 0;
            }
        }
    }
    /*
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
    */

    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    //[System.Diagnostics.DebuggerTypeProxy (typeof (CollectionDebuggerView<>))]
    public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IEnumerable<T>,
                                      ICollection, IEnumerable
    {
        class Node
        {
            public T Value = default(T);
            public Node Next;
        }

        object head;
        Node Head {
            get { return (Node)head; }
        }

        int count;

        public ConcurrentStack() {
        }

        public ConcurrentStack(IEnumerable<T> collection) {
            if (collection == null)
                throw new ArgumentNullException("collection");

            foreach (T item in collection)
                Push(item);
        }

        bool IProducerConsumerCollection<T>.TryAdd(T elem) {
            Push(elem);
            return true;
        }

        public void Push(T item) {
            Node temp = new Node();
            temp.Value = item;
            do {
                temp.Next = Head;
            } while (Interlocked.CompareExchange(ref head,temp,temp.Next) != temp.Next);

            Interlocked.Increment(ref count);
        }

        public void PushRange(T[] items) {
            if (items == null)
                throw new ArgumentNullException("items");

            PushRange(items,0,items.Length);
        }

        public void PushRange(T[] items,int startIndex,int count) {
            RangeArgumentsCheck(items,startIndex,count);

            Node insert = null;
            Node first = null;

            for (int i = startIndex; i < count; i++) {
                Node temp = new Node();
                temp.Value = items[i];
                temp.Next = insert;
                insert = temp;

                if (first == null)
                    first = temp;
            }

            do {
                first.Next = Head;
            } while (Interlocked.CompareExchange(ref head,insert,first.Next) != first.Next);

            Interlocked.Add(ref this.count,count);
        }

        public bool TryPop(out T result) {
            Node temp;
            do {
                temp = Head;
                // The stak is empty
                if (temp == null) {
                    result = default(T);
                    return false;
                }
            } while (Interlocked.CompareExchange(ref head,temp.Next,temp) != temp);

            Interlocked.Decrement(ref count);

            result = temp.Value;

            return true;
        }

        public int TryPopRange(T[] items) {
            if (items == null)
                throw new ArgumentNullException("items");
            return TryPopRange(items,0,items.Length);
        }

        public int TryPopRange(T[] items,int startIndex,int count) {
            RangeArgumentsCheck(items,startIndex,count);

            Node temp;
            Node end;

            do {
                temp = Head;
                if (temp == null)
                    return 0;
                end = temp;
                for (int j = 0; j < count; j++) {
                    end = end.Next;
                    if (end == null)
                        break;
                }
            } while (Interlocked.CompareExchange(ref head,end,temp) != temp);

            int i;
            for (i = startIndex; i < startIndex + count && temp != null; i++) {
                items[i] = temp.Value;
                end = temp;
                temp = temp.Next;
            }
            Interlocked.Add(ref this.count,-(i - startIndex));

            return i - startIndex;
        }

        public bool TryPeek(out T result) {
            Node myHead = Head;
            if (myHead == null) {
                result = default(T);
                return false;
            }
            result = myHead.Value;
            return true;
        }

        public void Clear() {
            // This is not satisfactory
            count = 0;
            head = null;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (IEnumerator)InternalGetEnumerator();
        }

        public IEnumerator<T> GetEnumerator() {
            return InternalGetEnumerator();
        }

        IEnumerator<T> InternalGetEnumerator() {
            Node my_head = Head;
            if (my_head == null) {
                yield break;
            }
            else {
                do {
                    yield return my_head.Value;
                } while ((my_head = my_head.Next) != null);
            }
        }

        void ICollection.CopyTo(Array array,int index) {
            ICollection ic = new List<T>(this);
            ic.CopyTo(array,index);
        }

        public void CopyTo(T[] array,int index) {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (index > array.Length)
                throw new ArgumentException("index is equals or greather than array length","index");

            IEnumerator<T> e = InternalGetEnumerator();
            int i = index;
            while (e.MoveNext()) {
                if (i == array.Length - index)
                    throw new ArgumentException("The number of elememts in the collection exceeds the capacity of array","array");
                array[i++] = e.Current;
            }
        }

        bool ICollection.IsSynchronized {
            get { return false; }
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item) {
            return TryPop(out item);
        }

        object ICollection.SyncRoot {
            get {
                throw new NotSupportedException();
            }
        }

        public T[] ToArray() {
            return new List<T>(this).ToArray();
        }

        public int Count {
            get {
                return count;
            }
        }

        public bool IsEmpty {
            get {
                return count == 0;
            }
        }

        static void RangeArgumentsCheck(T[] items,int startIndex,int count) {
            if (items == null)
                throw new ArgumentNullException("items");
            if (startIndex < 0 || startIndex >= items.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (startIndex + count > items.Length)
                throw new ArgumentException("startIndex + count is greater than the length of items.");
        }
    }

}