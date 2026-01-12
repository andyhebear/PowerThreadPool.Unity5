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

  
   
}