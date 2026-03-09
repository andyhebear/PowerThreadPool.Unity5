
using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerThreadPool_Net20.Collections.Comparer
{


    //This class is needed for Unity iOS compatibility
    [Serializable]
    public sealed class DefaultComparer<T> : IEqualityComparer, IEqualityComparer <T> {

        public int GetHashCode (T obj)
        {
            if (obj == null)
                return 0;
            return obj.GetHashCode ();
        }

        public bool Equals (T x, T y)
        {
            if (x == null)
                return y == null;

            return x.Equals (y);
        }

        public int GetHashCode(object obj)
        {
            if (obj == null)
                return 0;

            if (!(obj is T))
                throw new ArgumentException ("Argument is not compatible", "obj");

            return GetHashCode ((T)obj);
        }

        public bool Equals(object x, object y)
        {

            if (x == null || y == null) {
                return false;
            }

            if (!(x is T) || !(y is T))  {
                return false;
            }

            return Equals((T)x, (T)y);
        }
    }

}


