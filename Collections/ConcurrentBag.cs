using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace PowerThreadPool_Net20.Collections
{
    [ComVisible(false)]
    [DebuggerDisplay("Count={Count}")]
    public class ConcurrentBag<T> : IProducerConsumerCollection<T>, IEnumerable<T>, ICollection, IEnumerable
    {
        // 替换SyncRoot为专用对象
        private readonly object _syncRoot = new object();
        private readonly ConcurrentDictionary<T,int> _dictionary;
        private readonly Random _random;
        private readonly object _randomLock;

        public ConcurrentBag() {
            _dictionary = new ConcurrentDictionary<T,int>();
            _random = new Random();
            _randomLock = new object();
        }

        // 修复：移除item == null判断（原生ConcurrentBag允许null）
        public bool TryAdd(T item) {
            _dictionary.AddOrUpdate(item,1,(key,count) => count + 1);
            return true;
        }

        public bool TryTake(out T item) {
            item = default(T);

            int maxRetries = 10;
            for (int retry = 0; retry < maxRetries; retry++) {
                // 修复：原子化获取键数组（避免Count和CopyTo的竞态）
                T[] keys = GetKeysSnapshot();
                if (keys.Length == 0)
                    return false;

                T selectedKey = default(T);
                lock (_randomLock) {
                    int index = _random.Next(keys.Length);
                    selectedKey = keys[index];
                }

                if (TryTakeSpecific(selectedKey,out item))
                    return true;
            }

            return false;
        }

        // 原子化获取键快照（解决竞态问题）
        private T[] GetKeysSnapshot() {
            lock (_syncRoot) {
                T[] keys = new T[_dictionary.Keys.Count];
                _dictionary.Keys.CopyTo(keys,0);
                return keys;
            }
        }

        // 修复：使用锁减少竞态（或改用ConcurrentDictionary的原子操作）
        private bool TryTakeSpecific(T key,out T item) {
            item = default(T);
            lock (_syncRoot) {
                if (!_dictionary.TryGetValue(key,out int currentCount) || currentCount <= 0)
                    return false;

                if (currentCount == 1) {
                    if (_dictionary.TryRemove(key,out _)) {
                        item = key;
                        return true;
                    }
                }
                else {
                    if (_dictionary.TryUpdate(key,currentCount - 1,currentCount)) {
                        item = key;
                        return true;
                    }
                }
            }
            return false;
        }

        public T[] ToArray() {
            var list = new List<T>();
            foreach (var kvp in _dictionary.ToArray()) {
                for (int i = 0; i < kvp.Value; i++) {
                    list.Add(kvp.Key);
                }
            }
            return list.ToArray();
        }

        public void CopyTo(T[] array,int index) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Rank != 1)
                throw new ArgumentException("数组必须是一维的 / Array must be one-dimensional");

            T[] sourceArray = ToArray();
            if (sourceArray.Length > array.Length - index)
                throw new ArgumentException("目标数组不够大，无法容纳集合中的所有元素");

            Array.Copy(sourceArray,0,array,index,sourceArray.Length);
        }

        public int Count {
            get {
                int count = 0;
                foreach (var kvp in _dictionary.ToArray()) {
                    count += kvp.Value;
                }
                return count;
            }
        }

        public bool IsSynchronized => true;

        // 修复：返回专用同步对象
        public object SyncRoot => _syncRoot;

        void ICollection.CopyTo(Array array,int index) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (array.Rank != 1)
                throw new ArgumentException("数组必须是一维的 / Array must be one-dimensional");

            T[] sourceArray = ToArray();
            if (sourceArray.Length > array.Length - index)
                throw new ArgumentException("目标数组不够大，无法容纳集合中的所有元素");

            for (int i = 0; i < sourceArray.Length; i++) {
                array.SetValue(sourceArray[i],index + i);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            foreach (var kvp in _dictionary.ToArray()) {
                for (int i = 0; i < kvp.Value; i++) {
                    yield return kvp.Key;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }


}