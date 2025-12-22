using System;

namespace PowerThreadPool_Net20.Works
{
    /// <summary>
    /// 工作ID结构
    /// Work ID structure
    /// </summary>
    public struct WorkID : IEquatable<WorkID>
    {
        private readonly long _value;
        private static long _nextId = 1;
        
        /// <summary>
        /// 内部值
        /// Internal value
        /// </summary>
        public long Value => _value;
        
        /// <summary>
        /// 默认构造函数（生成唯一ID）
        /// Default constructor (generates unique ID)
        /// </summary>
        public WorkID(bool generateNew )
        {
            if (generateNew)
            {
                _value = System.Threading.Interlocked.Increment(ref _nextId);
            }
            else
            {
                _value = 0;
            }
        }
        
        /// <summary>
        /// 使用指定值构造
        /// Constructor with specified value
        /// </summary>
        public WorkID(long value)
        {
            _value = value;
        }
        
        /// <summary>
        /// 是否相等
        /// Whether equal
        /// </summary>
        public bool Equals(WorkID other)
        {
            return _value == other._value;
        }
        
        /// <summary>
        /// 是否相等
        /// Whether equal
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is WorkID)
                return Equals((WorkID)obj);
            return false;
        }
        
        /// <summary>
        /// 获取哈希码
        /// Get hash code
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
        
        /// <summary>
        /// 转换为字符串
        /// Convert to string
        /// </summary>
        public override string ToString()
        {
            return $"WorkID-{_value}";
        }
        
        /// <summary>
        /// 相等操作符
        /// Equality operator
        /// </summary>
        public static bool operator ==(WorkID left, WorkID right)
        {
            return left.Equals(right);
        }
        
        /// <summary>
        /// 不等操作符
        /// Inequality operator
        /// </summary>
        public static bool operator !=(WorkID left, WorkID right)
        {
            return !left.Equals(right);
        }
        
        /// <summary>
        /// 隐式转换为long
        /// Implicit conversion to long
        /// </summary>
        public static implicit operator long(WorkID id)
        {
            return id._value;
        }
        
        /// <summary>
        /// 隐式从long转换
        /// Implicit conversion from long
        /// </summary>
        public static implicit operator WorkID(long value)
        {
            return new WorkID(value);
        }
        
        /// <summary>
        /// 默认值
        /// Default value
        /// </summary>
        public static WorkID Default => new WorkID(1);
        
        /// <summary>
        /// 空值（表示无效ID）
        /// Empty value (represents invalid ID)
        /// </summary>
        public static WorkID Empty => new WorkID(0);
    }
}