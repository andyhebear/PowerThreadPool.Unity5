using System;
using System.Threading;

namespace PowerThreadPool_Net20.Helpers
{


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
   
}