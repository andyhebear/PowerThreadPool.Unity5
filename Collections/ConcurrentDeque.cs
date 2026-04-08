using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Volatile = PowerThreadPool_Net20.Threading.Volatile;
namespace PowerThreadPool_Net20.Collections
{
	/// <summary>
	///     A thread-safe, concurrent double-ended queue based on Chase & Lev Dynamic Circular Work-Stealing Deque.
	///     基于Chase & Lev动态循环工作窃取双端队列的线程安全并发双端队列。
	/// </summary>
	/// <remarks>
	///     This implementation provides a lock-free, concurrent deque that supports:
	///     - Owner thread operations: PushBottom, TryPopBottom (not concurrent with themselves)
	///     - Stealer thread operations: TrySteal (fully concurrent with all operations)
	///     - Standard deque operations: PushFront, PushBack, PopFront, PopBack, PeekFront, PeekBack
	///     
	///     [1] Chase, D., & Lev, Y. (2005). Dynamic circular work-stealing deque. Proceedings of Seventeenth Annual ACM
	///         Symposium on Parallelism in Algorithms and Architectures. ⟨10.1145/1073970.1073974⟩.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the deque / 队列中元素的类型</typeparam>
	public class ConcurrentDeque<T> : ICollection, IEnumerable<T>, ICloneable
	{
		#region Fields

		private long _bottom = 0L;
		private long _top = 0L;
		private long _lastTopValue = 0L;
		private volatile CircularArray<T> _activeArray;
		private readonly object _syncRoot = new object();

		#endregion

		#region Construction

		/// <summary>
		/// Create a new ConcurrentDeque with default capacity.
		/// 创建具有默认容量的新ConcurrentDeque。
		/// </summary>
		public ConcurrentDeque()
			: this(16)
		{
		}

		/// <summary>
		/// Create a new ConcurrentDeque with the specified capacity.
		/// 创建具有指定容量的新ConcurrentDeque。
		/// </summary>
		/// <param name="capacity">The initial capacity / 初始容量</param>
		public ConcurrentDeque(int capacity)
		{
			_activeArray = new CircularArray<T>((int)Math.Ceiling(Math.Log(capacity, 2.0)));
		}

		/// <summary>
		/// Create a new ConcurrentDeque containing elements from the specified collection.
		/// 创建包含指定集合元素的新ConcurrentDeque。
		/// </summary>
		/// <param name="collection">The collection whose elements are copied / 要复制元素的集合</param>
		public ConcurrentDeque(IEnumerable<T> collection)
			: this()
		{
			if (collection == null) throw new ArgumentNullException(nameof(collection));
			foreach (var item in collection)
			{
				PushBottom(item);
			}
		}

		#endregion

		#region Work-Stealing Methods (Chase & Lev Algorithm)

		private bool CASTop(long oldVal, long newVal)
		{
			return Interlocked.CompareExchange(ref _top, newVal, oldVal) == oldVal;
		}

		/// <summary>
		/// Push an item to the bottom of the deque.
		/// 将项目推入双端队列的底部。
		/// </summary>
		/// <remarks>
		/// This method must ONLY be called by the deque's owning process.
		/// It is not concurrent with itself, only with TrySteal.
		/// 此方法只能由双端队列的拥有者进程调用。
		/// 它不与自身并发，只与TrySteal并发。
		/// </remarks>
		/// <param name="item">The item to add / 要添加的项目</param>
		public void PushBottom(T item)
		{
			long b = Volatile.Read(ref _bottom);
			CircularArray<T> a = _activeArray;
			long sizeUpperBound = b - _lastTopValue;
			if (sizeUpperBound >= a.Capacity - 1)
			{
				long t = (_lastTopValue = Interlocked.Read(ref _top));
				long actualSize = b - t;
				if (actualSize >= a.Capacity - 1)
				{
					a = (_activeArray = a.EnsureCapacity(b, t));
				}
			}
			a[b] = item;
			Volatile.Write(ref _bottom, b + 1);
		}

		/// <summary>
		/// Attempt to pop an item from the bottom of the deque.
		/// 尝试从双端队列的底部弹出一个项目。
		/// </summary>
		/// <remarks>
		/// This method must ONLY be called by the deque's owning process.
		/// It is not concurrent with itself, only with TrySteal.
		/// 此方法只能由双端队列的拥有者进程调用。
		/// 它不与自身并发，只与TrySteal并发。
		/// </remarks>
		/// <param name="item">Set to the popped item if success / 成功时设置为弹出的项目</param>
		/// <returns>True if popped successfully / 如果成功弹出则为true</returns>
		public bool TryPopBottom(out T item)
		{
			item = default(T);
			long b = Volatile.Read(ref _bottom);
			CircularArray<T> a = _activeArray;
			b--;
			Interlocked.Exchange(ref _bottom, b);
			long t = Interlocked.Read(ref _top);
			long size = b - t;
			if (size < 0)
			{
				Volatile.Write(ref _bottom, t);
				return false;
			}
			T popped = a[b];
			if (size > 0)
			{
				item = popped;
				return true;
			}
			if (!CASTop(t, t + 1))
			{
				Interlocked.Exchange(ref _bottom, t + 1);
				return false;
			}
			Interlocked.Exchange(ref _bottom, t + 1);
			item = popped;
			return true;
		}

		/// <summary>
		/// Attempt to steal an item from the top of the deque.
		/// 尝试从双端队列的顶部窃取一个项目。
		/// </summary>
		/// <remarks>
		/// Unlike PushBottom and TryPopBottom, this method can be called from any thread
		/// at any time, and it is guaranteed to be concurrently compatible with all other methods.
		/// 与PushBottom和TryPopBottom不同，此方法可以随时从任何线程调用，
		/// 并且保证与所有其他方法并发兼容。
		/// </remarks>
		/// <param name="item">Set to the stolen item if success / 成功时设置为窃取的项目</param>
		/// <returns>True if stole successfully / 如果成功窃取则为true</returns>
		public bool TrySteal(out T item)
		{
			item = default(T);
			long t = Interlocked.Read(ref _top);
			long b = Volatile.Read(ref _bottom);
			CircularArray<T> a = _activeArray;
			long size = b - t;
			if (size <= 0)
			{
				return false;
			}
			T stolen = a[t];
			if (!CASTop(t, t + 1))
			{
				return false;
			}
			item = stolen;
			return true;
		}

		#endregion

		#region Standard Deque Operations

		/// <summary>
		/// Inserts an object at the front of the deque.
		/// 在双端队列的前端插入一个对象。
		/// </summary>
		/// <param name="item">The object to push onto the deque / 要推入双端队列的对象</param>
		public void PushFront(T item)
		{
			PushBottom(item);
		}

		/// <summary>
		/// Inserts an object at the back of the deque.
		/// 在双端队列的后端插入一个对象。
		/// </summary>
		/// <param name="item">The object to push onto the deque / 要推入双端队列的对象</param>
		public void PushBack(T item)
		{
			lock (_syncRoot)
			{
				InternalPushTop(item);
			}
		}

		private void InternalPushTop(T item)
		{
			long t = Interlocked.Read(ref _top);
			CircularArray<T> a = _activeArray;
			long sizeUpperBound = Volatile.Read(ref _bottom) - t;
			if (sizeUpperBound >= a.Capacity - 1)
			{
				a = (_activeArray = a.EnsureCapacity(Volatile.Read(ref _bottom), t));
			}
			long newT = t - 1;
			if (!CASTop(t, newT))
			{
				throw new InvalidOperationException("Concurrent modification detected during PushBack.");
			}
			a[newT] = item;
			_lastTopValue = newT;
		}

		/// <summary>
		/// Removes and returns the object at the front of the deque.
		/// 移除并返回双端队列前端的对象。
		/// </summary>
		/// <returns>The object at the front of the deque / 双端队列前端的对象</returns>
		public T PopFront()
		{
			if (TryPopBottom(out T item))
			{
				return item;
			}
			throw new InvalidOperationException("Deque is empty.");
		}

		/// <summary>
		/// Removes and returns the object at the back of the deque.
		/// 移除并返回双端队列后端的对象。
		/// </summary>
		/// <returns>The object at the back of the deque / 双端队列后端的对象</returns>
		public T PopBack()
		{
			lock (_syncRoot)
			{
				if (TryPopTop(out T item))
				{
					return item;
				}
			}
			throw new InvalidOperationException("Deque is empty.");
		}

		private bool TryPopTop(out T item)
		{
			item = default(T);
			long t = Interlocked.Read(ref _top);
			long b = Volatile.Read(ref _bottom);
			CircularArray<T> a = _activeArray;
			long size = b - t;
			if (size <= 0)
			{
				return false;
			}
			item = a[t];
			if (!CASTop(t, t + 1))
			{
				return false;
			}
			_lastTopValue = t + 1;
			return true;
		}

		/// <summary>
		/// Returns the object at the front of the deque without removing it.
		/// 返回双端队列前端的对象而不移除它。
		/// </summary>
		/// <returns>The object at the front of the deque / 双端队列前端的对象</returns>
		public T PeekFront()
		{
			long b = Volatile.Read(ref _bottom);
			long t = Interlocked.Read(ref _top);
			CircularArray<T> a = _activeArray;
			long size = b - t;
			if (size <= 0)
			{
				throw new InvalidOperationException("Deque is empty.");
			}
			return a[b - 1];
		}

		/// <summary>
		/// Returns the object at the back of the deque without removing it.
		/// 返回双端队列后端的对象而不移除它。
		/// </summary>
		/// <returns>The object at the back of the deque / 双端队列后端的对象</returns>
		public T PeekBack()
		{
			long t = Interlocked.Read(ref _top);
			long b = Volatile.Read(ref _bottom);
			CircularArray<T> a = _activeArray;
			long size = b - t;
			if (size <= 0)
			{
				throw new InvalidOperationException("Deque is empty.");
			}
			return a[t];
		}

		/// <summary>
		/// Removes all objects from the deque.
		/// 从双端队列中移除所有对象。
		/// </summary>
		public void Clear()
		{
			lock (_syncRoot)
			{
				_bottom = 0;
				_top = 0;
				_lastTopValue = 0;
				_activeArray = new CircularArray<T>((int)Math.Ceiling(Math.Log(16, 2.0)));
			}
		}

		/// <summary>
		/// Determines whether an element is in the deque.
		/// 确定元素是否在双端队列中。
		/// </summary>
		/// <param name="item">The object to locate / 要定位的对象</param>
		/// <returns>True if found in the deque / 如果在双端队列中找到则为true</returns>
		public bool Contains(T item)
		{
			foreach (var element in this)
			{
				if (EqualityComparer<T>.Default.Equals(element, item))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Copies the deque elements to a new array.
		/// 将双端队列元素复制到新数组。
		/// </summary>
		/// <returns>A new array containing copies of the elements / 包含元素副本的新数组</returns>
		public T[] ToArray()
		{
			lock (_syncRoot)
			{
				long b = Volatile.Read(ref _bottom);
				long t = Interlocked.Read(ref _top);
				long size = b - t;
				if (size <= 0)
				{
					return new T[0];
				}
				
				var array = new T[size];
				CircularArray<T> a = _activeArray;
				for (long i = t; i < b; i++)
				{
					array[i - t] = a[i];
				}
				return array;
			}
		}

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Creates a shallow copy of the deque.
		/// 创建双端队列的浅拷贝。
		/// </summary>
		/// <returns>A shallow copy of the deque / 双端队列的浅拷贝</returns>
		public object Clone()
		{
			return new ConcurrentDeque<T>(this);
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEnumerable<T> Members

		/// <summary>
		/// Returns an enumerator that iterates through the deque.
		/// 返回遍历双端队列的枚举器。
		/// </summary>
		/// <returns>An enumerator for the deque / 双端队列的枚举器</returns>
		public IEnumerator<T> GetEnumerator()
		{
			return new ConcurrentDequeEnumerator(this);
		}

		#endregion

		#region ICollection Members

		/// <summary>
		/// Gets a value indicating whether access to the deque is synchronized (thread-safe).
		/// 获取一个值，指示对双端队列的访问是否同步（线程安全）。
		/// </summary>
		public bool IsSynchronized => true;

		/// <summary>
		/// Gets the number of elements contained in the deque.
		/// 获取双端队列中包含的元素数量。
		/// </summary>
		public int Count
		{
			get
			{
				long b = Volatile.Read(ref _bottom);
				long t = Interlocked.Read(ref _top);
				return (int)(b - t);
			}
		}

		/// <summary>
		/// Copies the deque elements to an existing one-dimensional Array.
		/// 将双端队列元素复制到现有的一维数组。
		/// </summary>
		/// <param name="array">The destination array / 目标数组</param>
		/// <param name="index">The zero-based index in array at which copying begins / 开始复制的从零开始的索引</param>
		public void CopyTo(Array array, int index)
		{
			if (array == null) throw new ArgumentNullException(nameof(array));
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
			if (array.Rank > 1) throw new ArgumentException("Array is multidimensional.");
			if (index >= array.Length) throw new ArgumentException("Index is equal to or greater than the length of array.");
			if (Count > array.Length - index) throw new ArgumentException("The number of elements in the source deque is greater than the available space.");

			var i = index;
			foreach (var item in this)
			{
				array.SetValue(item, i++);
			}
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the deque.
		/// 获取可用于同步对双端队列访问的对象。
		/// </summary>
		public object SyncRoot => _syncRoot;

		#endregion

		#region Enumerator Class

		private class ConcurrentDequeEnumerator : IEnumerator<T>
		{
			private readonly ConcurrentDeque<T> _deque;
			private readonly T[] _snapshot;
			private int _currentIndex;
			private bool _disposed;

			public ConcurrentDequeEnumerator(ConcurrentDeque<T> deque)
			{
				_deque = deque;
				_snapshot = deque.ToArray();
				_currentIndex = -1;
			}

			public T Current
			{
				get
				{
					if (_currentIndex < 0 || _currentIndex >= _snapshot.Length)
					{
						throw new InvalidOperationException("Enumerator is positioned before the first element or after the last element.");
					}
					return _snapshot[_currentIndex];
				}
			}

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				_currentIndex++;
				return _currentIndex < _snapshot.Length;
			}

			public void Reset()
			{
				_currentIndex = -1;
			}

			public void Dispose()
			{
				_disposed = true;
			}
		}

		#endregion
	}
}
