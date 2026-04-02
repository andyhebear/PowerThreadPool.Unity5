using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PowerThreadPool_Net20.Threading
{
    /// <summary>
    /// 高性能信号量（在高负载场景下比标准SemaphoreSlim更高效）
    /// High-performance semaphore (more efficient than standard SemaphoreSlim in high-load scenarios)
    /// </summary>
    [DebuggerDisplay("Current Count = {_currentCount}")]
    internal class SemaphoreSlimE : IDisposable
    {
        /// <summary>
        /// 处理器数量 / Processor count
        /// </summary>
        private static readonly int _processorCount = Environment.ProcessorCount;

        /// <summary>
        /// 取消令牌取消事件处理器 / Cancellation token canceled event handler
        /// </summary>
        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// 取消令牌取消事件处理器
        /// Cancellation token canceled event handler
        /// </summary>
        /// <param name="obj">SemaphoreSlimE对象 / SemaphoreSlimE object</param>
        private static void CancellationTokenCanceledEventHandler(object obj) {
            SemaphoreSlimE semaphore = obj as SemaphoreSlimE;
            //Contract.Assert(semaphore != null);
            lock (semaphore._lockObj) {
                Monitor.PulseAll(semaphore._lockObj);
            }
        }


        /// <summary>
        /// 获取时间戳（毫秒）
        /// Get timestamp in milliseconds
        /// </summary>
        /// <returns>时间戳 / Timestamp</returns>
        private static uint GetTimestamp() {
            return (uint)Environment.TickCount;
        }
        
        /// <summary>
        /// 更新超时时间
        /// Update timeout
        /// </summary>
        /// <param name="startTime">开始时间 / Start time</param>
        /// <param name="originalTimeout">原始超时时间 / Original timeout</param>
        /// <returns>剩余时间 / Remaining time</returns>
        private static int UpdateTimeout(uint startTime,int originalTimeout) {
            uint elapsed = GetTimestamp() - startTime;
            if (elapsed > (uint)int.MaxValue)
                return 0;

            int rest = originalTimeout - (int)elapsed;
            if (rest <= 0)
                return 0;

            return rest;
        }

        // =============

        /// <summary>
        /// 进入等待的线程编号源 / Source of thread numbers entering Wait
        /// </summary>
        private int _entringWaitersNumber;
        
        /// <summary>
        /// 退出Wait的线程编号源（用于评估准备获取锁的线程数）
        /// Source of thread numbers exiting Wait (used to estimate number of threads preparing to acquire lock)
        /// </summary>
        private volatile int _finishedWaitersNumber;


        /// <summary>
        /// 信号量计数，在构造函数中初始化为初始值，每次Release调用增加，
        /// 每次Wait调用减少（只要其值为正），否则Wait将阻塞。
        /// 其值必须在最大信号量值和零之间。
        /// Semaphore count, initialized to initial value in constructor, incremented by every Release call
        /// and decremented by every Wait call as long as its value is positive, otherwise Wait will block.
        /// Its value must be between the maximum semaphore value and zero.
        /// </summary>
        private volatile int _currentCount;

        /// <summary>
        /// 最大信号量值，如果客户端未指定则初始化为Int.MaxValue。
        /// 用于检查计数是否超过最大值。
        /// Maximum semaphore value, initialized to Int.MaxValue if client didn't specify it.
        /// Used to check if count exceeded max value.
        /// </summary>
        private readonly int _maxCount;

        /// <summary>
        /// 同步等待的线程数，在构造函数中设置为零，在阻塞线程之前递增，
        /// 之后递减。用作Release调用的标志，以了解监视器中是否有等待线程。
        /// Number of synchronously waiting threads, set to zero in constructor and incremented before blocking
        /// the thread and decremented back after that. Used as flag for Release call to know if there are
        /// waiting threads in the monitor.
        /// </summary>
        private volatile int _waitCount;

        /// <summary>
        /// 用于lock语句的虚拟对象，用于保护信号量计数、等待句柄和取消操作
        /// Dummy object used in lock statements to protect semaphore count, wait handle and cancellation
        /// </summary>
        private object _lockObj;

        /// <summary>
        /// 充当信号量等待句柄，延迟初始化（如果需要），第一次WaitHandle调用初始化它，
        /// Wait和Release分别设置和重置它（只要它不为null）
        /// Act as semaphore wait handle, lazily initialized if needed, first WaitHandle call initializes it
        /// and Wait and Release sets and resets it respectively as long as it is not null
        /// </summary>
        private volatile ManualResetEvent _waitHandle;



        /// <summary>
        /// SemaphoreSlimE构造函数
        /// SemaphoreSlimE constructor
        /// </summary>
        /// <param name="initialCount">信号量初始值 / Initial semaphore value</param>
        /// <param name="maxCount">信号量最大值 / Maximum semaphore value</param>
        public SemaphoreSlimE(int initialCount,int maxCount) {
            if (initialCount < 0 || initialCount > maxCount)
                throw new ArgumentOutOfRangeException("initialCount","initialCount should be in range [0, maxCount]");
            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount","maxCount should be positive");

            _maxCount = maxCount;
            _lockObj = new object();
            _currentCount = initialCount;
        }
        
        /// <summary>
        /// SemaphoreSlimE构造函数
        /// SemaphoreSlimE constructor
        /// </summary>
        /// <param name="initialCount">信号量初始值 / Initial semaphore value</param>
        public SemaphoreSlimE(int initialCount)
            : this(initialCount,int.MaxValue) {
        }

        /// <summary>
        /// 可用槽位数 / Number of available slots
        /// </summary>
        public int CurrentCount {
            get { return _currentCount; }
        }

        /// <summary>
        /// 等待句柄 / Wait handle
        /// </summary>
        public WaitHandle AvailableWaitHandle {
            get {
                CheckDispose();

                if (_waitHandle == null) {
                    lock (_lockObj) {
                        if (_waitHandle == null)
                            _waitHandle = new ManualResetEvent(_currentCount != 0);
                    }
                }
                return _waitHandle;
            }
        }


        /// <summary>
        /// 检查对象是否已被释放
        /// Check if object has been disposed
        /// </summary>
        private void CheckDispose() {
            if (_lockObj == null)
                throw new ObjectDisposedException("SemaphoreSlimE");
        }




        /// <summary>
        /// 执行等待（必须在_lockObj的lock内调用）
        /// Execute wait (must be called inside lock on _lockObj)
        /// </summary>
        /// <param name="timeout">超时时间 / Timeout</param>
        /// <param name="startTime">开始时间（用于跟踪超时） / Start time (for timeout tracking)</param>
        /// <param name="token">取消令牌 / Cancellation token</param>
        /// <returns>是否成功等待 / Whether wait succeeded</returns>
        private bool WaitUntilCountOrTimeout(int timeout,uint startTime,CancellationToken token) {
            int remainingWaitMilliseconds = Timeout.Infinite;

            while (_currentCount == 0) {
                if (token.IsCancellationRequested)
                    return false;

                if (timeout != Timeout.Infinite) {
                    remainingWaitMilliseconds = UpdateTimeout(startTime,timeout);
                    if (remainingWaitMilliseconds <= 0)
                        return false;
                }

                if (!Monitor.Wait(_lockObj,remainingWaitMilliseconds))
                    return false;
            }

            return true;
        }


        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        /// <param name="timeout">超时时间 / Timeout</param>
        /// <param name="token">取消令牌 / Cancellation token</param>
        /// <returns>是否成功获取槽位 / Whether slot was successfully acquired</returns>
        public bool Wait(int timeout,CancellationToken token) {
            CheckDispose();

            if (timeout < -1)
                timeout = Timeout.Infinite;

            token.ThrowIfCancellationRequested();

            uint startTime = 0;
            if (timeout != Timeout.Infinite && timeout > 0)
                startTime = GetTimestamp();


            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
            if (token.CanBeCanceled && timeout != 0)
                cancellationTokenRegistration = token.Register(_cancellationTokenCanceledEventHandler,this);

            bool lockTaken = false;
            bool enterWaitUpdated = false;


            try {
                int myId = 0;
                try { }
                finally {
                    myId = Interlocked.Increment(ref _entringWaitersNumber) - 1;
                    enterWaitUpdated = true;
                }

                if (_processorCount > 1) {
                    if (_currentCount <= (myId - _finishedWaitersNumber)) {
                        Thread.SpinWait(128);
                        int spinCnt = 0;
                        while (spinCnt++ < 28 && (_currentCount <= (myId - _finishedWaitersNumber)))
                            Thread.SpinWait(300);
                        if (myId - _finishedWaitersNumber > _processorCount) {
                            //Thread.Yield();
                            SpinWait spinWait = new SpinWait();
                            spinWait.SpinOnce();
                        }
                    }
                }
                else if (timeout == 0 && _currentCount == 0) {
                    return false;
                }



                try { }
                finally {
                    Monitor.Enter(_lockObj);
                    lockTaken = true;
                    //Contract.Assert(lockTaken);
                    _waitCount++;
                }


                if (_currentCount == 0) {
                    if (timeout == 0)
                        return false;

                    bool waitSuccessful = WaitUntilCountOrTimeout(timeout,startTime,token);
                    if (!waitSuccessful) {
                        if (token.IsCancellationRequested)
                            throw new OperationCanceledException();

                        return false;
                    }
                }


                //Contract.Assert(_currentCount > 0);
                _currentCount--;


                var waitHandle = _waitHandle;
                if (waitHandle != null && _currentCount == 0)
                    waitHandle.Reset();
            }
            finally {
                if (enterWaitUpdated) {
                    if (lockTaken)
                        _finishedWaitersNumber++;
                    else
                        Interlocked.Decrement(ref _entringWaitersNumber); // 这样不会丢失更新
                }

                if (lockTaken) {
                    _waitCount--;
                    Monitor.Exit(_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return true;
        }




        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        public void Wait() {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite,new CancellationToken());
            //Contract.Assert(semaphoreSlotTaken);
        }
        
        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        /// <param name="token">取消令牌 / Cancellation token</param>
        public void Wait(CancellationToken token) {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite,token);
            //Contract.Assert(semaphoreSlotTaken);
        }
        
        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        /// <param name="timeout">超时时间 / Timeout</param>
        /// <returns>是否成功获取槽位 / Whether slot was successfully acquired</returns>
        public bool Wait(TimeSpan timeout) {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");

            return Wait((int)timeoutMs,new CancellationToken());
        }
        
        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        /// <param name="timeout">超时时间 / Timeout</param>
        /// <param name="token">取消令牌 / Cancellation token</param>
        /// <returns>是否成功获取槽位 / Whether slot was successfully acquired</returns>
        public bool Wait(TimeSpan timeout,CancellationToken token) {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");

            return Wait((int)timeoutMs,token);
        }
        
        /// <summary>
        /// 执行等待以获取信号量中的1个槽位。
        /// 成功获取后，可用槽位数减1。
        /// Execute wait to acquire 1 slot in the semaphore.
        /// On successful acquisition, the number of available slots is decreased by 1.
        /// </summary>
        /// <param name="timeout">超时时间 / Timeout</param>
        /// <returns>是否成功获取槽位 / Whether slot was successfully acquired</returns>
        public bool Wait(int timeout) {
            return Wait(timeout,new CancellationToken());
        }


        /// <summary>
        /// 释放N个槽位
        /// Release N slots
        /// </summary>
        /// <param name="releaseCount">要释放的槽位数 / Number of slots to release</param>
        /// <returns>释放前的槽位数 / Number of slots before release</returns>
        public int Release(int releaseCount) {
            CheckDispose();

            if (releaseCount < 1)
                throw new ArgumentOutOfRangeException("releaseCount","releaseCount should be positive");

            int returnCount = 0;

            lock (_lockObj) {
                var currentCount = _currentCount;
                if (_maxCount - currentCount < releaseCount)
                    throw new SemaphoreFullException();

                returnCount = currentCount;
                currentCount += releaseCount;

                int waitCount = _waitCount;
                if (currentCount == 1 || waitCount == 1) {
                    Monitor.Pulse(_lockObj);
                }
                else if (waitCount > 1) {
                    Monitor.PulseAll(_lockObj);
                }

                _currentCount = currentCount;

                var waitHandle = _waitHandle;
                if (waitHandle != null && returnCount == 0 && currentCount > 0)
                    waitHandle.Set();
            }

            return returnCount;
        }
        
        /// <summary>
        /// 释放1个槽位
        /// Release 1 slot
        /// </summary>
        /// <returns>释放前的槽位数 / Number of slots before release</returns>
        public int Release() {
            return Release(1);
        }



        /// <summary>
        /// 释放资源
        /// Release resources
        /// </summary>
        /// <param name="isUserCall">是否由用户调用 / Whether called by user</param>
        protected virtual void Dispose(bool isUserCall) {
            if (isUserCall) {
                var waitHandle = _waitHandle;
                if (waitHandle != null) {
                    _waitHandle = null;
                    waitHandle.Close();
                }
                _lockObj = null;
            }
        }

        /// <summary>
        /// 释放资源
        /// Release resources
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
