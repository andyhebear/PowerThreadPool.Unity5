using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerThreadPool_Net20.Collections
{
    /// <summary>
    /// 线程安全的集合（兼容 .NET 2.0）
    /// Thread-safe set for .NET 2.0 compatibility
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    public class ConcurrentSet<T> : IEnumerable<T>
    {
        private readonly Dictionary<T, object> _dictionary;
        private static readonly object _dummyValue = new object();
        private readonly object _lockObject = new object();

        /// <summary>
        /// 最后添加的元素
        /// Last added element
        /// </summary>
        internal T Last { get; private set; }

        /// <summary>
        /// 获取一个值，指示该集合是否为只读
        /// Gets a value indicating whether the collection is read-only
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public ConcurrentSet()
        {
            _dictionary = new Dictionary<T, object>();
        }

        /// <summary>
        /// 使用初始元素构造
        /// Constructor with initial items
        /// </summary>
        /// <param name="items">初始元素集合</param>
        public ConcurrentSet(IEnumerable<T> items)
        {
            _dictionary = new Dictionary<T, object>();
            if (items != null)
            {
                foreach (T item in items)
                {
                    if (!_dictionary.ContainsKey(item))
                    {
                        _dictionary[item] = _dummyValue;
                        Last = item;
                    }
                }
            }
        }

        /// <summary>
        /// 添加元素
        /// Add item
        /// </summary>
        /// <param name="item">要添加的元素</param>
        /// <returns>如果成功添加返回 true，如果已存在返回 false</returns>
        public bool Add(T item)
        {
            lock (_lockObject)
            {
                if (!_dictionary.ContainsKey(item))
                {
                    _dictionary[item] = _dummyValue;
                    Last = item;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 移除元素
        /// Remove item
        /// </summary>
        /// <param name="item">要移除的元素</param>
        /// <returns>如果成功移除返回 true，如果不存在返回 false</returns>
        public bool Remove(T item)
        {
            lock (_lockObject)
            {
                return _dictionary.Remove(item);
            }
        }

        /// <summary>
        /// 获取元素数量
        /// Get item count
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _dictionary.Count;
                }
            }
        }

        /// <summary>
        /// 检查是否包含指定元素
        /// Check if contains item
        /// </summary>
        /// <param name="value">要查找的值</param>
        /// <returns>如果包含返回 true，否则返回 false</returns>
        public bool Contains(T value)
        {
            lock (_lockObject)
            {
                return _dictionary.ContainsKey(value);
            }
        }

        /// <summary>
        /// 清空所有元素
        /// Clear all items
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _dictionary.Clear();
            }
        }

        /// <summary>
        /// 从指定的数组索引开始，将集合元素复制到数组中
        /// Copies the elements of the collection to an array, starting at a particular array index
        /// </summary>
        /// <param name="array">目标数组</param>
        /// <param name="arrayIndex">目标数组中从零开始的索引，从此处开始复制</param>
        /// <exception cref="ArgumentNullException">array 为 null</exception>
        /// <exception cref="ArgumentOutOfRangeException">arrayIndex 小于 0</exception>
        /// <exception cref="ArgumentException">源集合中的元素数量大于目标数组从 arrayIndex 到末尾的可用空间</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is less than 0.");

            lock (_lockObject)
            {
                if (arrayIndex + _dictionary.Count > array.Length)
                    throw new ArgumentException("The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.");

                int index = arrayIndex;
                foreach (T item in _dictionary.Keys)
                {
                    array[index++] = item;
                }
            }
        }

        /// <summary>
        /// 获取枚举器
        /// Get enumerator
        /// </summary>
        /// <returns>枚举器</returns>
        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockObject)
            {
                return new List<T>(_dictionary.Keys).GetEnumerator();
            }
        }

        /// <summary>
        /// 获取非泛型枚举器
        /// Get non-generic enumerator
        /// </summary>
        /// <returns>枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
