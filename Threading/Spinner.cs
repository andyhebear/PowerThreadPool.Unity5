using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PowerThreadPool_Net20.Threading
{
    internal class Spinner
    {
        // Spinner usage guidelines:
        // Before using spinning, you must ensure in tests that the spin duration in Ticks is less than 5000.
        // Otherwise, spinning should not be used and other optimization methods should be considered.
        // Unit tests that use mocks to force the Spinner to spin for a long time are exceptions.
        // If using the Spinner causes significant overhead, use the Spinner only in DEBUG mode for verification,
        // and manually spin in Release mode.
        //Spinner 使用指南：
        // 使用自旋之前，必须在测试中确保以时钟周期（Ticks）为单位的自旋持续时间小于 5000。
        // 否则，不应使用自旋，而应考虑其他优化方法。
        // 使用模拟（mocks）强制 Spinner 长时间自旋的单元测试属于例外情况。
        // 如果使用 Spinner 会导致显著的开销，则仅在 DEBUG 模式下使用 Spinner 进行验证，
        // 并在 Release 模式下手动自旋。
        internal static void Start(Func<bool> func,bool doPrecheck = false) {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            if (doPrecheck && func()) {
                return;
            }
            SpinWait sw = new SpinWait();
            while (!func()) {
                sw.SpinOnce();
            }
#if DEBUG
            stopwatch.Stop();
            if (stopwatch.Elapsed.Ticks >= 5000) {
                double milliseconds = (double)stopwatch.Elapsed.Ticks / Stopwatch.Frequency * 1000;

                Console.WriteLine($"The operation took too long to complete: {stopwatch.Elapsed.Ticks} ticks.");

            }
#endif

        }
    }
}
