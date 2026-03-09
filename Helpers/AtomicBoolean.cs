using System;
using System.Threading;

namespace PowerThreadPool_Net20.Helpers
{
    struct AtomicBooleanValue
    {
        int flag;
        const int UnSet = 0;
        const int Set = 1;

        public bool CompareAndExchange(bool expected,bool newVal) {
            int newTemp = newVal ? Set : UnSet;
            int expectedTemp = expected ? Set : UnSet;

            return Interlocked.CompareExchange(ref flag,newTemp,expectedTemp) == expectedTemp;
        }

        public static AtomicBooleanValue FromValue(bool value) {
            AtomicBooleanValue temp = new AtomicBooleanValue();
            temp.Value = value;

            return temp;
        }

        public bool TrySet() {
            return !Exchange(true);
        }

        public bool TryRelaxedSet() {
            return flag == UnSet && !Exchange(true);
        }

        public bool Exchange(bool newVal) {
            int newTemp = newVal ? Set : UnSet;
            return Interlocked.Exchange(ref flag,newTemp) == Set;
        }

        public bool Value {
            get {
                return flag == Set;
            }
            set {
                Exchange(value);
            }
        }

        public bool Equals(AtomicBooleanValue rhs) {
            return this.flag == rhs.flag;
        }

        public override bool Equals(object rhs) {
            return rhs is AtomicBooleanValue ? Equals((AtomicBooleanValue)rhs) : false;
        }

        public override int GetHashCode() {
            return flag.GetHashCode();
        }

        public static explicit operator bool(AtomicBooleanValue rhs) {
            return rhs.Value;
        }

        public static implicit operator AtomicBooleanValue(bool rhs) {
            return AtomicBooleanValue.FromValue(rhs);
        }
    }

    class AtomicBoolean
    {
        int flag;
        const int UnSet = 0;
        const int Set = 1;

        public bool CompareAndExchange(bool expected,bool newVal) {
            int newTemp = newVal ? Set : UnSet;
            int expectedTemp = expected ? Set : UnSet;

            return Interlocked.CompareExchange(ref flag,newTemp,expectedTemp) == expectedTemp;
        }

        public static AtomicBoolean FromValue(bool value) {
            AtomicBoolean temp = new AtomicBoolean();
            temp.Value = value;

            return temp;
        }

        public bool TrySet() {
            return !Exchange(true);
        }

        public bool TryRelaxedSet() {
            return flag == UnSet && !Exchange(true);
        }

        public bool Exchange(bool newVal) {
            int newTemp = newVal ? Set : UnSet;
            return Interlocked.Exchange(ref flag,newTemp) == Set;
        }

        public bool Value {
            get {
                return flag == Set;
            }
            set {
                Exchange(value);
            }
        }
        public void SetTrue() { Value = true; }
        public void SetFalse() { Value = false; }

        public bool Equals(AtomicBoolean rhs) {
            return this.flag == rhs.flag;
        }

        public override bool Equals(object rhs) {
            return rhs is AtomicBoolean ? Equals((AtomicBoolean)rhs) : false;
        }

        public override int GetHashCode() {
            return flag.GetHashCode();
        }

        public static explicit operator bool(AtomicBoolean rhs) {
            return rhs.Value;
        }

        public static implicit operator AtomicBoolean(bool rhs) {
            return AtomicBoolean.FromValue(rhs);
        }
    }

    ///// <summary>
    ///// 原子标志类
    ///// Atomic flag class
    ///// </summary>
    //public class AtomicBoolean
    //{
    //    private volatile bool _value;
    //    private readonly object _lockObject = new object();

    //    /// <summary>
    //    /// 当前值
    //    /// Current value
    //    /// </summary>
    //    public bool Value {
    //        get { return _value; }
    //    }

    //    /// <summary>
    //    /// 构造函数
    //    /// Constructor
    //    /// </summary>
    //    public AtomicBoolean(bool initialValue = false) {
    //        _value = initialValue;
    //    }

    //    /// <summary>
    //    /// 设置为真
    //    /// Set to true
    //    /// </summary>
    //    public bool SetTrue() {
    //        lock (_lockObject) {
    //            bool original = _value;
    //            _value = true;
    //            return !original; // 如果之前是false，返回true表示设置成功
    //        }
    //    }

    //    /// <summary>
    //    /// 设置为假
    //    /// Set to false
    //    /// </summary>
    //    public bool SetFalse() {
    //        lock (_lockObject) {
    //            bool original = _value;
    //            _value = false;
    //            return original; // 如果之前是true，返回true表示设置成功
    //        }
    //    }

    //    /// <summary>
    //    /// 设置为指定值
    //    /// Set to specified value
    //    /// </summary>
    //    public bool SetValue(bool newValue) {
    //        lock (_lockObject) {
    //            bool original = _value;
    //            _value = newValue;
    //            return original != newValue; // 如果值发生变化，返回true
    //        }
    //    }

    //    /// <summary>
    //    /// 比较并设置
    //    /// Compare and set
    //    /// </summary>
    //    public bool CompareAndSet(bool expectedValue,bool newValue) {
    //        // .NET 2.0兼容性修复：使用实例级锁对象确保线程安全
    //        // .NET 2.0 compatibility fix: Use instance-level lock object to ensure thread safety
    //        lock (_lockObject) {
    //            bool original = _value;
    //            if (original == expectedValue) {
    //                _value = newValue;
    //                return true;
    //            }
    //            return false;
    //        }
    //    }

    //    /// <summary>
    //    /// 转换为字符串
    //    /// Convert to string
    //    /// </summary>
    //    public override string ToString() {
    //        return $"AtomicFlag({_value})";
    //    }
    //}

}