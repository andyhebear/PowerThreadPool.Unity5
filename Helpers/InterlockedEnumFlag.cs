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
    internal class InterlockedEnumFlag<T> where T : Enum
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

        private InterlockedEnumFlag(T initialValue) {
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

        public static bool operator ==(InterlockedEnumFlag<T> flag1,InterlockedEnumFlag<T> flag2) {
            if (ReferenceEquals(flag1,null)) {
                return ReferenceEquals(flag2,null);
            }
            else if (ReferenceEquals(flag2,null)) {
                return ReferenceEquals(flag1,null);
            }

            return flag1._innerValue == flag2._innerValue;
        }

        public static bool operator !=(InterlockedEnumFlag<T> flag1,InterlockedEnumFlag<T> flag2)
            => !(flag1 == flag2);

        public static bool operator ==(InterlockedEnumFlag<T> flag1,T flag2) {
            if (ReferenceEquals(flag1,null)) {
                return ReferenceEquals(flag2,null);
            }

#if NET5_0_OR_GREATER
            return flag1._innerValue == Unsafe.As<T, int>(ref flag2);
#else
            return flag1._innerValue == (int)(object)flag2;
#endif
        }

        public static bool operator !=(InterlockedEnumFlag<T> flag1,T flag2)
            => !(flag1 == flag2);

        public static implicit operator InterlockedEnumFlag<T>(T value)
            => new InterlockedEnumFlag<T>(value);

        public static implicit operator T(InterlockedEnumFlag<T> flag)
            => flag.InterlockedValue;

        public override bool Equals(object obj) {
            if (obj != null) {
                if (obj is InterlockedEnumFlag<T> otherFlag) {
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
