using System;
using System.Threading;

namespace PowerThreadPool_Net20.Helpers
{
    /// <summary>
    /// 线程安全辅助类
    /// Thread-safe helper class
    /// </summary>
    public static class InterlockedHelper
    {
        /// <summary>
        /// 专用锁对象，用于布尔值原子操作
        /// Dedicated lock object for boolean atomic operations
        /// </summary>
        internal static readonly object _boolLock = new object();

        /// <summary>
        /// 原子性地增加一个值
        /// Atomically increment a value
        /// </summary>
        public static int Add(ref int location,int value) {
            int original, newValue;
            do {
                original = location;
                newValue = original + value;
            } while (Interlocked.CompareExchange(ref location,newValue,original) != original);

            return newValue;
        }

        /// <summary>
        /// 原子性地增加一个值
        /// Atomically increment a value
        /// </summary>
        public static long Add(ref long location,long value) {
            long original, newValue;
            do {
                original = location;
                newValue = original + value;
            } while (Interlocked.CompareExchange(ref location,newValue,original) != original);

            return newValue;
        }

        /// <summary>
        /// 原子性地减少一个值
        /// Atomically decrement a value
        /// </summary>
        public static int Subtract(ref int location,int value) {
            return Add(ref location,-value);
        }

        /// <summary>
        /// 原子性地减少一个值
        /// Atomically decrement a value
        /// </summary>
        public static long Subtract(ref long location,long value) {
            return Add(ref location,-value);
        }

        /// <summary>
        /// 原子性地交换值
        /// Atomically exchange values
        /// </summary>
        public static T Exchange<T>(ref T location,T value) where T : class {
            T original;
            do {
                original = location;
            } while (Interlocked.CompareExchange(ref location,value,original) != original);
            return original;
        }

        /// <summary>
        /// 原子性地比较并交换值
        /// Atomically compare and exchange values
        /// </summary>
        public static T CompareExchange<T>(ref T location,T value,T comparand) where T : class {
            return (T)Interlocked.CompareExchange(ref location,value,comparand);
        }

        /// <summary>
        /// 原子性地比较并交换整数值
        /// Atomically compare and exchange integer values
        /// </summary>
        public static int CompareExchange(ref int location,int value,int comparand) {
            return Interlocked.CompareExchange(ref location,value,comparand);
        }

        /// <summary>
        /// 原子性地设置布尔值为true，返回是否设置成功
        /// Atomically set boolean value to true, return whether setting was successful
        /// </summary>
        public static bool SetTrue(ref bool location) {
            // .NET 2.0兼容性修复：使用专用锁对象实现原子操作，避免锁定类型对象
            // .NET 2.0 compatibility fix: Use dedicated lock object for atomic operations, avoid locking type objects
            lock (_boolLock) {
                bool original = location;
                if (!original) {
                    location = true;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 原子性地设置布尔值为false，返回是否设置成功
        /// Atomically set boolean value to false, return whether setting was successful
        /// </summary>
        public static bool SetFalse(ref bool location) {
            // .NET 2.0兼容性修复：使用专用锁对象实现原子操作，避免锁定类型对象
            // .NET 2.0 compatibility fix: Use dedicated lock object for atomic operations, avoid locking type objects
            lock (_boolLock) {
                bool original = location;
                if (original) {
                    location = false;
                    return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// 原子标志类
    /// Atomic flag class
    /// </summary>
    public class AtomicFlag
    {
        private volatile bool _value;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 当前值
        /// Current value
        /// </summary>
        public bool Value {
            get { return _value; }
        }

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public AtomicFlag(bool initialValue = false) {
            _value = initialValue;
        }

        /// <summary>
        /// 设置为真
        /// Set to true
        /// </summary>
        public bool SetTrue() {
            lock (_lockObject) {
                bool original = _value;
                _value = true;
                return !original; // 如果之前是false，返回true表示设置成功
            }
        }

        /// <summary>
        /// 设置为假
        /// Set to false
        /// </summary>
        public bool SetFalse() {
            lock (_lockObject) {
                bool original = _value;
                _value = false;
                return original; // 如果之前是true，返回true表示设置成功
            }
        }

        /// <summary>
        /// 设置为指定值
        /// Set to specified value
        /// </summary>
        public bool SetValue(bool newValue) {
            lock (_lockObject) {
                bool original = _value;
                _value = newValue;
                return original != newValue; // 如果值发生变化，返回true
            }
        }

        /// <summary>
        /// 比较并设置
        /// Compare and set
        /// </summary>
        public bool CompareAndSet(bool expectedValue,bool newValue) {
            // .NET 2.0兼容性修复：使用实例级锁对象确保线程安全
            // .NET 2.0 compatibility fix: Use instance-level lock object to ensure thread safety
            lock (_lockObject) {
                bool original = _value;
                if (original == expectedValue) {
                    _value = newValue;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 转换为字符串
        /// Convert to string
        /// </summary>
        public override string ToString() {
            return $"AtomicFlag({_value})";
        }
    }
    /// <summary>
    /// 为无锁算法提供支持。Provide support for lock-free algorithms.
    /// 将枚举用作无锁算法的状态标志，并通过原子操作实现线程安全的状态切换。
    /// Use enumeration as the status flag of the lock-free algorithm and implement thread-safe state switching through atomic operations.
    /// </summary>
    /// <typeparam name="T">用于表示状态的枚举，Enumeration used to represent status</typeparam>
    internal class InterlockedFlag<T> where T : Enum
    {
        private int _innerValue;

        public T InterlockedValue {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => Get();
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            set => Set(value);
        }

        public T Value => InnerValueToT(_innerValue);

        private string TypeName { get; } = typeof(T).Name;

        internal string DebuggerDisplay => $"{TypeName}.{InterlockedValue}";

        private InterlockedFlag(T initialValue) {
            Set(initialValue);
        }

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
#if NET5_0_OR_GREATER
        private void Set(T value)
            => Interlocked.Exchange(ref _innerValue, Unsafe.As<T, int>(ref value));
#else
        private void Set(T value)
            => Interlocked.Exchange(ref _innerValue,(int)(object)value);
#endif

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public T Get()
            => InnerValueToT(_innerValue);

#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool TrySet(T value,T comparand)
            => TrySet(value,comparand,out _);

        public bool TrySet(T value,T comparand,out T origValue) {
#if NET5_0_OR_GREATER
            int valueAsInt = Unsafe.As<T, int>(ref value);
            int comparandAsInt = Unsafe.As<T, int>(ref comparand);
#else
            int valueAsInt = (int)(object)value;
            int comparandAsInt = (int)(object)comparand;
#endif

            int origInnerValue = Interlocked.CompareExchange(ref _innerValue,valueAsInt,comparandAsInt);

            origValue = InnerValueToT(origInnerValue);

            return origInnerValue == comparandAsInt;
        }

        public static bool operator ==(InterlockedFlag<T> flag1,InterlockedFlag<T> flag2) {
            if (ReferenceEquals(flag1,null)) {
                return ReferenceEquals(flag2,null);
            }
            else if (ReferenceEquals(flag2,null)) {
                return ReferenceEquals(flag1,null);
            }

            return flag1._innerValue == flag2._innerValue;
        }

        public static bool operator !=(InterlockedFlag<T> flag1,InterlockedFlag<T> flag2)
            => !(flag1 == flag2);

        public static bool operator ==(InterlockedFlag<T> flag1,T flag2) {
            if (ReferenceEquals(flag1,null)) {
                return ReferenceEquals(flag2,null);
            }

#if NET5_0_OR_GREATER
            return flag1._innerValue == Unsafe.As<T, int>(ref flag2);
#else
            return flag1._innerValue == (int)(object)flag2;
#endif
        }

        public static bool operator !=(InterlockedFlag<T> flag1,T flag2)
            => !(flag1 == flag2);

        public static implicit operator InterlockedFlag<T>(T value)
            => new InterlockedFlag<T>(value);

        public static implicit operator T(InterlockedFlag<T> flag)
            => flag.InterlockedValue;

        public override bool Equals(object obj) {
            if (obj != null) {
                if (obj is InterlockedFlag<T> otherFlag) {
                    return this == otherFlag;
                }
                else if (obj is T otherValue) {
                    return this == otherValue;
                }
            }

            return false;
        }

        public override int GetHashCode() => _innerValue.GetHashCode();

        private static T InnerValueToT(int innerValue)
#if NET5_0_OR_GREATER
            => Unsafe.As<int, T>(ref innerValue);
#else
            => (T)(object)innerValue;
#endif
    }
}