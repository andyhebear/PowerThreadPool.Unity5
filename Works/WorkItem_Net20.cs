using System;
using System.Reflection;
using System.Threading;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Threading;

namespace PowerThreadPool_Net20.Works
{
    /// <summary>
    /// 工作项类
    /// Work item class
    /// </summary>
    internal class WorkItem
    {
        private readonly WorkID _id;
        private readonly Delegate _method;
        private readonly WorkOption _option;
        private readonly DateTime _createTime;
        private readonly PowerPool _pool; // 添加PowerPool引用用于超时线程注册

        /// <summary>
        /// 工作ID
        /// Work ID
        /// </summary>
        public WorkID ID => _id;

        /// <summary>
        /// 选项
        /// Option
        /// </summary>
        public WorkOption Option => _option;

        /// <summary>
        /// 创建时间
        /// Create time
        /// </summary>
        public DateTime CreateTime => _createTime;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public WorkItem(WorkID id,Delegate method,WorkOption option)
            : this(id,method,option,null)
        {
        }

        /// <summary>
        /// 构造函数（内部使用，支持PowerPool引用）
        /// Internal constructor with PowerPool reference
        /// </summary>
        public WorkItem(WorkID id,Delegate method,WorkOption option,PowerPool pool)
        {
            _id = id;
            _method = method;
            _option = option ?? new WorkOption();
            _createTime = DateTime.Now;
            _pool = pool;
        }

        /// <summary>
        /// 执行工作
        /// Execute work
        /// </summary>
        public object Execute()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                // 检查取消
                if (_option.CancellationToken != null && _option.CancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                // 检查超时设置
                if (_option.Timeout != null && _option.Timeout != TimeSpan.Zero)
                {
                    // 使用ManualResetEvent来实现超时控制（.NET 2.0兼容方案）
                    ManualResetEvent doneEvent = new ManualResetEvent(false);
                    object result = null;
                    Exception exception = null;
                    // 创建工作线程
                    Thread workThread = new Thread(delegate ()
                    {
                        try
                        {
                            result = _method.DynamicInvoke();
                        }
                        catch (TargetInvocationException tie)
                        {
                            exception = tie.InnerException ?? tie;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        finally
                        {
                            doneEvent.Set();
                        }
                    });

                    workThread.Start();

                    // 等待完成或超时
                    bool completed = doneEvent.WaitOne(_option.Timeout);
                    doneEvent.Close();
                    if (!completed)
                    {
                        // 超时了，在.NET 2.0中我们无法安全地强制停止线程
                        // 将超时线程标记为后台线程，让它在后台自然结束
                        if (workThread.IsAlive)
                        {
                            workThread.IsBackground = true;
                            workThread.Name = $"TimeoutThread-{_id}";

                            // 注册超时线程到线程池进行跟踪
                            if (_pool != null)
                            {
                                _pool.RegisterTimeoutThread(_id,workThread);
                            }
                        }
                        throw new TimeoutException($"Work {_id} timed out after {_option.Timeout.TotalMilliseconds}ms");
                    }

                    // 检查执行异常
                    if (exception != null)
                    {
                        throw exception;
                    }

                    return result;


                }
                else
                {
                    // 无超时限制，直接执行
                    return _method.DynamicInvoke();
                }
            }
            catch (TargetInvocationException tie)
            {
                // 解包TargetInvocationException
                throw tie.InnerException ?? tie;
            }
            catch (TimeoutException)
            {
                // 重新抛出超时异常
                throw;
            }
            catch (Exception ex)
            {
                // 包装其他异常
                throw new Exception($"Work {_id} execution failed",ex);
            }
        }
    }
}