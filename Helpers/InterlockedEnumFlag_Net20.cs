using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerThreadPool_Net20.Helpers
{
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
    ///// <summary>
    ///// 为无锁算法提供支持 - .NET 2.0 兼容版本
    ///// Provide support for lock-free algorithms - .NET 2.0 compatible version
    ///// 将枚举用作无锁算法的状态标志，并通过原子操作实现线程安全的状态切换。
    ///// Use enumeration as the status flag of the lock-free algorithm and implement thread-safe state switching through atomic operations.
    ///// </summary>
    ///// <typeparam name="T">用于表示状态的枚举，Enumeration used to represent status</typeparam>
    //internal class InterlockedEnumFlag<T> where T : struct
    //{
    //    private int _innerValue;
    //    private readonly object _lockObject = new object();

    //    /// <summary>
    //    /// 当前值（原子读取）
    //    /// Current value (atomic read)
    //    /// </summary>
    //    public T Value
    //    {
    //        get
    //        {
    //            lock (_lockObject)
    //            {
    //                return EnumToObject(_innerValue);
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 类型名称
    //    /// Type name
    //    /// </summary>
    //    private string TypeName { get; } = typeof(T).Name;

    //    /// <summary>
    //    /// 调试器显示
    //    /// Debugger display
    //    /// </summary>
    //    internal string DebuggerDisplay => $"{TypeName}.{Value}";

    //    /// <summary>
    //    /// 构造函数
    //    /// Constructor
    //    /// </summary>
    //    internal InterlockedEnumFlag(T initialValue)
    //    {
    //        Set(initialValue);
    //    }

    //    /// <summary>
    //    /// 设置值（原子操作）
    //    /// Set value (atomic operation)
    //    /// </summary>
    //    public void Set(T value)
    //    {
    //        lock (_lockObject)
    //        {
    //            _innerValue = ObjectToEnum(value);
    //        }
    //    }

    //    /// <summary>
    //    /// 获取值
    //    /// Get value
    //    /// </summary>
    //    public T Get()
    //    {
    //        return Value;
    //    }

    //    /// <summary>
    //    /// 尝试设置值（原子比较并交换）
    //    /// Try to set value (atomic compare and swap)
    //    /// </summary>
    //    public bool TrySet(T value, T comparand)
    //    {
    //        lock (_lockObject)
    //        {
    //            int currentValue = _innerValue;
    //            T currentEnum = EnumToObject(currentValue);

    //            if (currentEnum.Equals(comparand))
    //            {
    //                _innerValue = ObjectToEnum(value);
    //                return true;
    //            }

    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 尝试设置值（原子比较并交换），并返回原始值
    //    /// Try to set value (atomic compare and swap), and return original value
    //    /// </summary>
    //    public bool TrySet(T value, T comparand, out T origValue)
    //    {
    //        lock (_lockObject)
    //        {
    //            int currentValue = _innerValue;
    //            T currentEnum = EnumToObject(currentValue);

    //            origValue = currentEnum;

    //            if (currentEnum.Equals(comparand))
    //            {
    //                _innerValue = ObjectToEnum(value);
    //                return true;
    //            }

    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 隐式转换为枚举类型
    //    /// Implicit conversion to enum type
    //    /// </summary>
    //    public static implicit operator T(InterlockedEnumFlag<T> flag)
    //        => flag.Value;

    //    /// <summary>
    //    /// 从枚举对象转换为整数值
    //    /// Convert from enum object to integer value
    //    /// </summary>
    //    private int ObjectToEnum(T value)
    //    {
    //        if (!typeof(T).IsEnum)
    //            throw new ArgumentException("T must be an enumerated type");

    //        return (int)(object)value;
    //    }

    //    /// <summary>
    //    /// 从整数值转换为枚举对象
    //    /// Convert from integer value to enum object
    //    /// </summary>
    //    private T EnumToObject(int value)
    //    {
    //        if (!typeof(T).IsEnum)
    //            throw new ArgumentException("T must be an enumerated type");

    //        return (T)(object)value;
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (obj != null)
    //        {
    //            if (obj is InterlockedEnumFlag<T> otherFlag)
    //            {
    //                return Value.Equals(otherFlag.Value);
    //            }
    //            else if (obj is T otherValue)
    //            {
    //                return Value.Equals(otherValue);
    //            }
    //        }

    //        return false;
    //    }

    //    public override int GetHashCode() => _innerValue.GetHashCode();

    //    public override string ToString() => DebuggerDisplay;
    //}
}
