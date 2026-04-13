using PowerThreadPool_Net20.Collections;
using System;
using System.Collections.Generic;
using System.Threading;


namespace PowerThreadPool_Net20.Threading
{
    //class BaseSynchronizationContext : SynchronizationContext {
    //   // [__DynamicallyInvokable]
    //    public override void Send(SendOrPostCallback d, object state) {
    //        d(state);
    //    }

    //    //[__DynamicallyInvokable]
    //    public override void Post(SendOrPostCallback d, object state) {
    //        ThreadPool.QueueUserWorkItem(d.Invoke, state);
    //    }
    //} 


    //public class ConsoleSyncContext : SynchronizationContext
    //{
    //    BlockingCollection<Action> queue = new BlockingCollection<Action>();

    //    public override void Post(SendOrPostCallback d, object state) {
    //        queue.Add(() => d(state));
    //    }

    //    public override void Send(SendOrPostCallback d, object state) {
    //        Exception ex = null;
    //        using (var waiter = new ManualResetEventSlim(false)) {
    //            Post(wrapper => {
    //                try {
    //                    d.Invoke(state);
    //                }
    //                catch (Exception e) {
    //                    ex = e;
    //                }
    //                finally {
    //                    waiter.Set();
    //                }
    //            }, null);
    //            waiter.Wait();
    //        }

    //        if (ex != null)
    //            throw ex;
    //    }

    //    public void Run() {
    //        SetSynchronizationContext(this);

    //        Action item;
    //        while (true) {
    //            try {
    //                item = queue.Take();
    //            }
    //            catch (InvalidOperationException) {
    //                // The collection has been closed, so let's exit!
    //                return;
    //            }

    //            item();
    //        }
    //    }

    //    public void Exit() {
    //        queue.CompleteAdding();
    //    }
    //}

    ///// <summary>
    ///// 控制台 同步上下文
    ///// </summary>
    //public class MySynchronizationContext : SynchronizationContext
    //{
    //    private readonly MyControl ctrl;

    //    public MySynchronizationContext(MyControl ctrl) {
    //        this.ctrl = ctrl;
    //    }
    //    public override void Send(SendOrPostCallback d, object state) {
    //        ctrl.Invoke((Z) => d(Z), state);
    //    }

    //    public override void Post(SendOrPostCallback d, object state) {
    //        ctrl.BeginInvoke((Z) => d(Z), state);
    //    }


    //    /// <summary>
    //    /// 用于控制台线程同步上下文，类似winfor UI的WindowsFormsSynchronizationContext以及wpf的DispatcherSynchronizationContext
    //    /// </summary>
    //    public class ExeArg
    //    {
    //        //要放到主线程(UI线程)中执行的方法
    //        public Action<object> Action { get; set; }
    //        //方法执行时额外的参数
    //        public object State { get; set; }
    //        //是否同步执行
    //        public bool Sync { get; set; }
    //        //静态字典，key: 线程Id，value: 队列
    //        public static ConcurrentDictionary<int, BlockingCollection<ExeArg>> QueueDics = new ConcurrentDictionary<int, BlockingCollection<ExeArg>>();
    //        //当前线程对应的字典
    //        public static BlockingCollection<ExeArg> CurrentQueue => QueueDics.ContainsKey(Thread.CurrentThread.ManagedThreadId) ? QueueDics[Thread.CurrentThread.ManagedThreadId] : null;
    //    }
    //    public class MyControl
    //    {
    //        //记录创建这个控件的线程(UI线程)
    //        private Thread _createThread = null;
    //        public MyControl() {
    //            //第一个控件创建时初始化一个SynchronizationContext实例，并将它和当前线程绑定一起
    //            if (SynchronizationContext.Current == null) SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext(this));
    //            _createThread = Thread.CurrentThread;
    //            //初始化一个字典队列，key: 线程Id，value：参数队列
    //            ExeArg.QueueDics.TryAdd(Thread.CurrentThread.ManagedThreadId, new BlockingCollection<ExeArg>());
    //        }
    //        //同步调用
    //        public void Invoke(Action<object> action, object state) {
    //            var queues = ExeArg.QueueDics[_createThread.ManagedThreadId];
    //            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
    //            queues.Add(new ExeArg() {
    //                Action = obj => {
    //                    action(state);
    //                    manualResetEvent.Set();
    //                },
    //                State = state,
    //                Sync = true
    //            });
    //            manualResetEvent.WaitOne();
    //            (manualResetEvent as IDisposable).Dispose();
    //        }
    //        //异步调用
    //        public void BeginInvoke(Action<object> action, object state) {
    //            var queues = ExeArg.QueueDics[_createThread.ManagedThreadId];
    //            queues.Add(new ExeArg() {
    //                Action = action,
    //                State = state
    //            });
    //        }
    //    }
    //    class MySynchronizationContextTest
    //    {
    //        public static void Main(string[] arg) {

    //            Console.WriteLine($"主线程: {Thread.CurrentThread.ManagedThreadId}");
    //            //主线程创建控件
    //            var ctrl = new MyControl();
    //            var syncContext = SynchronizationContext.Current;
    //            //模拟一个用户操作
    //            Action ac =()=> {
    //                Thread.Sleep(2000);
    //                Console.WriteLine($"用户线程: {Thread.CurrentThread.ManagedThreadId},Post前");
    //                syncContext.Post((state) => {
    //                    Console.WriteLine($"Post内的方法执行线程: {Thread.CurrentThread.ManagedThreadId},参数:{state}");
    //                }, new { name = "小明" });
    //                Console.WriteLine($"用户线程: {Thread.CurrentThread.ManagedThreadId},Post后,Send前");
    //                syncContext.Send((state) => {
    //                    Thread.Sleep(3000);
    //                    Console.WriteLine($"Send内的方法执行线程: {Thread.CurrentThread.ManagedThreadId},参数:{state}");
    //                }, new { name = "小红" });
    //                Console.WriteLine($"用户线程: {Thread.CurrentThread.ManagedThreadId},Send后");
    //            };
    //            Thread mStaThread = new Thread(new ThreadStart( ac));
    //            mStaThread.Name = "STA Worker Thread";
    //            mStaThread.SetApartmentState(ApartmentState.STA);
    //            mStaThread.Start();
    //            //主线程开启消息垒
    //            while (true) {
    //                var exeArg = ExeArg.CurrentQueue.Take();
    //                exeArg.Action?.Invoke(exeArg.State);
    //            }
    //        }
    //    }
    //}






    //public class STASynchronizationContext : SynchronizationContext, IDisposable
    //{
    //    public  Dispatcher dispatcher {
    //        get;
    //        private set;
    //    }
    //    private object dispObj;
    //    private readonly Thread mainThread;

    //    public STASynchronizationContext() {
    //        mainThread = new Thread(MainThread) { Name = "STASynchronizationContextMainThread", IsBackground = false };
    //        mainThread.SetApartmentState(ApartmentState.STA);
    //        mainThread.Start();
    //        SpinWait wait = new SpinWait();
    //        //wait to get the main thread's dispatcher
    //        while (Thread.VolatileRead(ref dispObj) == null) {
    //            //Thread.Yield();
    //            wait.SpinOnce();
    //        }

    //        dispatcher = dispObj as Dispatcher;
    //    }

    //    public override void Post(SendOrPostCallback d, object state) {
    //        Func<object, object> fuc = (Z) => {
    //            d(state);
    //            return null;
    //        };
    //        dispatcher.BeginInvoke(new DispatcherOperationCallback(fuc),state);// (d, new object[] { state });
    //    }

    //    public override void Send(SendOrPostCallback d, object state) {
    //        Func<object, object> fuc = (Z) => {
    //            d(state);
    //            return null;
    //        };
    //        dispatcher.Invoke(TimeSpan.FromSeconds(10000),new DispatcherOperationCallback(fuc),state);// (d, new object[] { state });
    //    }

    //    private void MainThread(object param) {
    //        Thread.VolatileWrite(ref dispObj, Dispatcher.CurrentDispatcher);
    //        Console.WriteLine("Main Thread is setup ! Id = {0}", Thread.CurrentThread.ManagedThreadId);
    //        Dispatcher.Run();
    //    }

    //    public void Dispose() {
    //        if (!dispatcher.HasShutdownStarted && !dispatcher.HasShutdownFinished) {
    //            //dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
    //            Func<object, object> fuc = (Z) => {
    //                dispatcher.InvokeShutdown();
    //                return null;
    //            };
    //            dispatcher.BeginInvoke(new DispatcherOperationCallback(fuc), null);
    //        }

    //        GC.SuppressFinalize(this);
    //    }

    //    ~STASynchronizationContext() {
    //        Dispose();
    //    }
    //}

 
}
