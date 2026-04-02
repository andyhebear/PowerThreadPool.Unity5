using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerThreadPool_Net20.Threading
{
    internal class Volatile
    {
      
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static long Read(ref long location)
        {
            return VolatileRead(ref location);
        }
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static void Write(ref long location, long value)
        {
            VolatileWrite(ref location, value);
        }
        public static long VolatileRead(ref long value) {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            return System.Threading.Volatile.Read(ref value);
#else
            return Thread.VolatileRead(ref value);
#endif
        }

        public static void VolatileWrite(ref long value,long newValue) {
#if (NET45_OR_GREATER || NET5_0_OR_GREATER)
            System.Threading.Volatile.Write(ref value, newValue);
#else
            Thread.VolatileWrite(ref value,newValue);
#endif
        }
    }
}
