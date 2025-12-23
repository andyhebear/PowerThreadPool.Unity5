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
        public static int Add(ref int location, int value)
        {
            int original, newValue;
            do
            {
                original = location;
                newValue = original + value;
            } while (Interlocked.CompareExchange(ref location, newValue, original) != original);
            
            return newValue;
        }
        
        /// <summary>
        /// 原子性地增加一个值
        /// Atomically increment a value
        /// </summary>
        public static long Add(ref long location, long value)
        {
            long original, newValue;
            do
            {
                original = location;
                newValue = original + value;
            } while (Interlocked.CompareExchange(ref location, newValue, original) != original);
            
            return newValue;
        }
        
        /// <summary>
        /// 原子性地减少一个值
        /// Atomically decrement a value
        /// </summary>
        public static int Subtract(ref int location, int value)
        {
            return Add(ref location, -value);
        }
        
        /// <summary>
        /// 原子性地减少一个值
        /// Atomically decrement a value
        /// </summary>
        public static long Subtract(ref long location, long value)
        {
            return Add(ref location, -value);
        }
        
        /// <summary>
        /// 原子性地交换值
        /// Atomically exchange values
        /// </summary>
        public static T Exchange<T>(ref T location, T value) where T : class
        {
            T original;
            do
            {
                original = location;
            } while (Interlocked.CompareExchange(ref location, value, original) != original);
            return original;
        }
        
        /// <summary>
        /// 原子性地比较并交换值
        /// Atomically compare and exchange values
        /// </summary>
        public static T CompareExchange<T>(ref T location, T value, T comparand) where T : class
        {
            return (T)Interlocked.CompareExchange(ref location, value, comparand);
        }
        
        /// <summary>
        /// 原子性地比较并交换整数值
        /// Atomically compare and exchange integer values
        /// </summary>
        public static int CompareExchange(ref int location, int value, int comparand)
        {
            return Interlocked.CompareExchange(ref location, value, comparand);
        }
        
        /// <summary>
        /// 原子性地设置布尔值为true，返回是否设置成功
        /// Atomically set boolean value to true, return whether setting was successful
        /// </summary>
        public static bool SetTrue(ref bool location)
        {
            // .NET 2.0兼容性修复：使用专用锁对象实现原子操作，避免锁定类型对象
            // .NET 2.0 compatibility fix: Use dedicated lock object for atomic operations, avoid locking type objects
            lock (_boolLock)
            {
                bool original = location;
                if (!original)
                {
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
        public static bool SetFalse(ref bool location)
        {
            // .NET 2.0兼容性修复：使用专用锁对象实现原子操作，避免锁定类型对象
            // .NET 2.0 compatibility fix: Use dedicated lock object for atomic operations, avoid locking type objects
            lock (_boolLock)
            {
                bool original = location;
                if (original)
                {
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
        public bool Value
        {
            get { return _value; }
        }
        
        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public AtomicFlag(bool initialValue = false)
        {
            _value = initialValue;
        }
        
        /// <summary>
        /// 设置为真
        /// Set to true
        /// </summary>
        public bool SetTrue()
        {
            lock (_lockObject)
            {
                bool original = _value;
                _value = true;
                return !original; // 如果之前是false，返回true表示设置成功
            }
        }
        
        /// <summary>
        /// 设置为假
        /// Set to false
        /// </summary>
        public bool SetFalse()
        {
            lock (_lockObject)
            {
                bool original = _value;
                _value = false;
                return original; // 如果之前是true，返回true表示设置成功
            }
        }
        
        /// <summary>
        /// 设置为指定值
        /// Set to specified value
        /// </summary>
        public bool SetValue(bool newValue)
        {
            lock (_lockObject)
            {
                bool original = _value;
                _value = newValue;
                return original != newValue; // 如果值发生变化，返回true
            }
        }
        
        /// <summary>
        /// 比较并设置
        /// Compare and set
        /// </summary>
        public bool CompareAndSet(bool expectedValue, bool newValue)
        {
            // .NET 2.0兼容性修复：使用实例级锁对象确保线程安全
            // .NET 2.0 compatibility fix: Use instance-level lock object to ensure thread safety
            lock (_lockObject)
            {
                bool original = _value;
                if (original == expectedValue)
                {
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
        public override string ToString()
        {
            return $"AtomicFlag({_value})";
        }
    }
}