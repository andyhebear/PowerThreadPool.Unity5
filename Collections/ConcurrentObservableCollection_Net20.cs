using System;
using System.Collections.Generic;
using System.Threading;
using PowerThreadPool_Net20.Constants;
using PowerThreadPool_Net20.Helpers;
using PowerThreadPool_Net20.Results;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 线程安全的可观察集合，支持.NET 2.0 / Thread-safe observable collection supporting .NET 2.0
    /// </summary>
    /// <typeparam name="T">元素类型 / Element type</typeparam>
    public class ConcurrentObservableCollection<T>
    {
        private readonly ConcurrentQueue<T> _innerQueue;
        private readonly object _eventLock = new object();
        private InterlockedEnumFlag<WatchStates> _watchState;
        private InterlockedEnumFlag<CanWatch> _canWatch;
        private EventHandler<NotifyCollectionChangedEventArgs<T>> _collectionChangedHandler;
        private EventHandler<WorkFailedEventArgs> _watchFailedHandler;

        /// <summary>
        /// 集合变更事件 / Collection changed event
        /// </summary>
        public event EventHandler<NotifyCollectionChangedEventArgs<T>> CollectionChanged;

        /// <summary>
        /// 获取集合中元素的数量 / Get the number of elements in the collection
        /// </summary>
        public int Count => _innerQueue.Count;

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        public ConcurrentObservableCollection()
        {
            _innerQueue = new ConcurrentQueue<T>();
            _watchState = WatchStates.Idle;
            _canWatch = CanWatch.NotAllowed;
        }

        /// <summary>
        /// 设置监视失败处理器 / Set watch failed handler
        /// </summary>
        internal void SetWatchFailedHandler(EventHandler<WorkFailedEventArgs> handler)
        {
            _watchFailedHandler = handler;
        }

        /// <summary>
        /// 获取监视失败处理器 / Get watch failed handler
        /// </summary>
        internal EventHandler<WorkFailedEventArgs> GetWatchFailedHandler()
        {
            return _watchFailedHandler;
        }

        /// <summary>
        /// 尝试添加元素到集合 / Try to add element to collection
        /// </summary>
        public bool TryAdd(T item)
        {
            _innerQueue.Enqueue(item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
            return true;
        }

        /// <summary>
        /// 添加元素到集合 / Add element to collection
        /// </summary>
        public void Add(T item)
        {
            TryAdd(item);
        }

        /// <summary>
        /// 批量添加元素到集合 / Add multiple elements to collection
        /// </summary>
        /// <param name="items">要添加的元素集合 / Collection of elements to add</param>
        /// <param name="notifyPerItem">是否为每个元素触发事件 / Whether to trigger event for each item (default: false)</param>
        public void AddRange(IEnumerable<T> items, bool notifyPerItem = false)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (notifyPerItem)
            {
                // 为每个元素触发事件
                foreach (T item in items)
                {
                    TryAdd(item);
                }
            }
            else
            {
                // 批量添加，只触发一次Reset事件
                int addedItems =0;
                foreach (T item in items)
                {
                    _innerQueue.Enqueue(item);
                    addedItems++;
                }
                
                // 批量添加后触发一次事件
                if (addedItems > 0)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Reset, default(T));
                }
            }
        }

        /// <summary>
        /// 尝试批量添加元素到集合 / Try to add multiple elements to collection
        /// </summary>
        /// <param name="items">要添加的元素集合 / Collection of elements to add</param>
        /// <param name="notifyPerItem">是否为每个元素触发事件 / Whether to trigger event for each item (default: false)</param>
        /// <returns>是否成功添加 / Whether addition was successful</returns>
        public bool TryAddRange(IEnumerable<T> items, bool notifyPerItem = false)
        {
            if (items == null)
            {
                return false;
            }

            if (notifyPerItem)
            {
                // 为每个元素触发事件
                foreach (T item in items)
                {
                    TryAdd(item);
                }
            }
            else
            {
                // 批量添加，只触发一次Reset事件
                int addedItems = 0;
                foreach (T item in items)
                {
                    _innerQueue.Enqueue(item);
                    addedItems++;
                }
                
                // 批量添加后触发一次事件
                if (addedItems > 0)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Reset, default(T));
                }
            }
            return true;
        }

        /// <summary>
        /// 批量从集合中移除元素 / Remove multiple elements from collection
        /// </summary>
        /// <param name="count">要移除的元素数量 / Number of elements to remove</param>
        /// <param name="notifyPerItem">是否为每个元素触发事件 / Whether to trigger event for each item (default: false)</param>
        /// <returns>实际移除的元素列表 / List of actually removed elements</returns>
        public List<T> TakeRange(int count, bool notifyPerItem = false)
        {
            List<T> result = new List<T>();
            
            if (notifyPerItem)
            {
                // 为每个元素触发事件
                for (int i = 0; i < count; i++)
                {
                    T item;
                    if (TryTake(out item))
                    {
                        result.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                // 批量移除，只触发一次Reset事件
                for (int i = 0; i < count; i++)
                {
                    T item;
                    if (_innerQueue.TryDequeue(out item))
                    {
                        result.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
                
                // 批量移除后触发一次事件
                if (result.Count > 0)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Reset, default(T));
                }
            }
            
            return result;
        }

        /// <summary>
        /// 尝试批量从集合中移除元素 / Try to remove multiple elements from collection
        /// </summary>
        /// <param name="count">要移除的元素数量 / Number of elements to remove</param>
        /// <param name="items">实际移除的元素列表 / List of actually removed elements</param>
        /// <param name="notifyPerItem">是否为每个元素触发事件 / Whether to trigger event for each item (default: false)</param>
        /// <returns>是否成功移除至少一个元素 / Whether at least one element was removed</returns>
        public bool TryTakeRange(int count, out List<T> items, bool notifyPerItem = false)
        {
            items = new List<T>();
            
            if (notifyPerItem)
            {
                // 为每个元素触发事件
                for (int i = 0; i < count; i++)
                {
                    T item;
                    if (TryTake(out item))
                    {
                        items.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                // 批量移除，只触发一次Reset事件
                for (int i = 0; i < count; i++)
                {
                    T item;
                    if (_innerQueue.TryDequeue(out item))
                    {
                        items.Add(item);
                    }
                    else
                    {
                        break;
                    }
                }
                
                // 批量移除后触发一次事件
                if (items.Count > 0)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Reset, default(T));
                }
            }
            
            return items.Count > 0;
        }

        /// <summary>
        /// 尝试批量从集合中移除元素 / Try to remove multiple elements from collection
        /// </summary>
        /// <param name="count">要移除的元素数量 / Number of elements to remove</param>
        /// <param name="notifyPerItem">是否为每个元素触发事件 / Whether to trigger event for each item (default: false)</param>
        /// <returns>是否成功移除至少一个元素 / Whether at least one element was removed</returns>
        public bool TryTakeRange(int count, bool notifyPerItem = false)
        {
            List<T> items;
            return TryTakeRange(count, out items, notifyPerItem);
        }

        /// <summary>
        /// 尝试从集合中移除元素 / Try to remove element from collection
        /// </summary>
        public bool TryTake(out T item)
        {
            bool success = _innerQueue.TryDequeue(out item);
            if (success)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
            }
            return success;
        }

        /// <summary>
        /// 从集合中移除元素 / Remove element from collection
        /// </summary>
        public T Take()
        {
            T item;
            TryTake(out item);
            return item;
        }

        /// <summary>
        /// 查看集合中的第一个元素 / Peek at the first element in the collection
        /// </summary>
        public bool TryPeek(out T item)
        {
            return _innerQueue.TryPeek(out item);
        }

        /// <summary>
        /// 清空集合 / Clear the collection
        /// </summary>
        public void Clear()
        {
            _innerQueue.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset, default(T));
        }

        /// <summary>
        /// 将集合转换为数组 / Convert collection to array
        /// </summary>
        public T[] ToArray()
        {
            return _innerQueue.ToArray();
        }

        /// <summary>
        /// 触发集合变更事件 / Trigger collection changed event
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            // 先读取handler引用，避免在锁内读取
            EventHandler<NotifyCollectionChangedEventArgs<T>> handler = VolatileRead(ref CollectionChanged);
            
            if (handler != null)
            {
                NotifyCollectionChangedEventArgs<T> e = new NotifyCollectionChangedEventArgs<T>(action, item);
                lock (_eventLock)
                {
                    handler(this, e);
                }
            }
        }
        
        /// <summary>
        /// 线程安全地读取委托引用 / Thread-safe delegate read
        /// </summary>
        private static TDelegate VolatileRead<TDelegate>(ref TDelegate location) where TDelegate : class
        {
            TDelegate value = location;
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// 开始监视集合 / Start watching collection
        /// </summary>
        internal bool StartWatching(EventHandler<NotifyCollectionChangedEventArgs<T>> onCollectionChanged)
        {
            // 检查是否允许开始监视
            // 如果当前值是 NotAllowed，则设置为 Allowed
           if (!_canWatch.TrySet(CanWatch.Allowed, CanWatch.NotAllowed))
            //if (!_canWatch.TrySet(CanWatch.NotAllowed, CanWatch.Allowed))
            {
                return false;
            }

            // 设置监视状态和处理器
            _watchState.InterlockedValue = WatchStates.Watching;
            _collectionChangedHandler = onCollectionChanged;
            CollectionChanged += onCollectionChanged;
            return true;
        }

        /// <summary>
        /// 停止监视集合 / Stop watching collection
        /// </summary>
        internal void StopWatching(bool keepRunning = false)
        {
            if (_watchState.TrySet(WatchStates.Idle, WatchStates.Watching))
            {
                _canWatch.InterlockedValue = CanWatch.Allowed;
                if (!keepRunning)
                {
                    if (_collectionChangedHandler != null)
                    {
                        CollectionChanged -= _collectionChangedHandler;
                    }
                    _collectionChangedHandler = null;
                }
            }
        }

        /// <summary>
        /// 强制停止监视集合 / Force stop watching collection
        /// </summary>
        internal void ForceStopWatching(bool keepRunning = false)
        {
            _watchState.InterlockedValue = WatchStates.Idle;
            _canWatch.InterlockedValue = CanWatch.Allowed;
            if (!keepRunning)
            {
                if (_collectionChangedHandler != null)
                {
                    CollectionChanged -= _collectionChangedHandler;
                }
                _collectionChangedHandler = null;
            }
        }

        /// <summary>
        /// 获取监视状态 / Get watch state
        /// </summary>
        internal WatchStates GetWatchState()
        {
            return _watchState.InterlockedValue;
        }

        /// <summary>
        /// 获取是否可监视 / Get whether can watch
        /// </summary>
        internal CanWatch GetCanWatch()
        {
            return _canWatch.InterlockedValue;
        }

        /// <summary>
        /// 设置是否可监视 / Set whether can watch
        /// </summary>
        internal void SetCanWatch(CanWatch canWatch)
        {
            _canWatch.InterlockedValue = canWatch;
        }
    }

    /// <summary>
    /// 集合变更动作枚举 / Collection changed action enumeration
    /// </summary>
    public enum NotifyCollectionChangedAction
    {
        /// <summary>
        /// 添加 / Add
        /// </summary>
        Add,
        /// <summary>
        /// 移除 / Remove
        /// </summary>
        Remove,
        /// <summary>
        /// 重置 / Reset
        /// </summary>
        Reset,
        /// <summary>
        /// 替换 / Replace
        /// </summary>
        Replace,
        /// <summary>
        /// 移动 / Move
        /// </summary>
        Move
    }

    /// <summary>
    /// 集合变更事件参数 / Collection changed event arguments
    /// </summary>
    /// <typeparam name="T">元素类型 / Element type</typeparam>
    public class NotifyCollectionChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 变更动作 / Action
        /// </summary>
        public NotifyCollectionChangedAction Action { get; private set; }

        /// <summary>
        /// 受影响的元素 / Affected item
        /// </summary>
        public T Item { get; private set; }

        /// <summary>
        /// 构造函数 / Constructor
        /// </summary>
        public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, T item)
        {
            Action = action;
            Item = item;
        }
    }
}