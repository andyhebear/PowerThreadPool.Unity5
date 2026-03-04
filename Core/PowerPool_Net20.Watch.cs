using System;
using System.Collections.Generic;
using PowerThreadPool_Net20.Collections;
using PowerThreadPool_Net20.Groups;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
using PowerThreadPool_Net20.Works;
using PowerThreadPool_Net20.Groups;
using PowerThreadPool_Net20.Constants;

namespace PowerThreadPool_Net20
{
    public partial class PowerPool
    {
        /*
         Watch 的工作原理：
1:生产者-消费者模式：
    taskQueue 是临时缓冲队列，不是任务存储容器
    Watch 机制会立即将元素从 taskQueue 中取出并提交到线程池
    一旦取出，元素就从 taskQueue 中移除
2:事件驱动：
    Add("Task 1")  // 添加到队列
    → 触发 CollectionChanged 事件
    → onCollectionChanged 处理器响应
    → TryTake() 取出元素并提交到线程池
    → 队列中的元素被移除（Count--）
3:同步处理：
    onCollectionChanged 在同步线程中执行
    while (source.TryTake(out item)) 会一次性取空整个队列
    所以 Add() 返回后，队列已经被清空了
         */
        /// <summary>
        /// 监视可观察集合的变化并处理每个元素
        /// Watch an observable collection for changes and process each element
        /// </summary>
        /// <typeparam name="TSource">元素类型 / Element type</typeparam>
        /// <param name="source">源集合 / Source collection</param>
        /// <param name="body">操作体 / Action body</param>
        /// <param name="addBackWhenWorkCanceled">工作取消时将元素添加回集合 / Add element back to collection when work is canceled</param>
        /// <param name="addBackWhenWorkStopped">工作停止时将元素添加回集合 / Add element back to collection when work is stopped</param>
        /// <param name="addBackWhenWorkFailed">工作失败时将元素添加回集合 / Add element back to collection when work is failed</param>
        /// <param name="maxRetryCount">最大重试次数 / Maximum retry count</param>
        /// <param name="groupName">可选的组名称 / Optional group name</param>
        /// <returns>返回组对象 / Returns group object</returns>
        public Group Watch<TSource>(
            ConcurrentObservableCollection_Net20<TSource> source,
            Action<TSource> body,
            bool addBackWhenWorkCanceled = true,
            bool addBackWhenWorkStopped = true,
            bool addBackWhenWorkFailed = true,
            int maxRetryCount = 3,
            string groupName = null)
        {
            CheckDisposed();

            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            string groupID = string.IsNullOrEmpty(groupName) ? Guid.NewGuid().ToString() : groupName;
            WorkOption workOption = new WorkOption();
            Dictionary<WorkID, TSource> idDict = new Dictionary<WorkID, TSource>();
            Dictionary<TSource, int> retryCountDict = new Dictionary<TSource, int>();
            object idDictLock = new object();

            // 注册失败回退事件
            if (addBackWhenWorkFailed || addBackWhenWorkCanceled || addBackWhenWorkStopped)
            {
                EventHandler<WorkFailedEventArgs> onFailed = (_, e) =>
                {
                    bool shouldAddBack = addBackWhenWorkFailed ||
                        (addBackWhenWorkCanceled && e.IsCanceled) ||
                        (addBackWhenWorkStopped && e.IsTimeout);
                    
                    if (shouldAddBack)
                    {
                        lock (idDictLock)
                        {
                            if (idDict.ContainsKey(e.WorkID))
                            {
                                TSource item = idDict[e.WorkID];
                                
                                // 检查重试次数
                                int currentRetryCount = 0;
                                if (retryCountDict.ContainsKey(item))
                                {
                                    currentRetryCount = retryCountDict[item];
                                }
                                
                                if (currentRetryCount < maxRetryCount)
                                {
                                    // 增加重试计数并将任务添加回集合
                                    retryCountDict[item] = currentRetryCount + 1;
                                    source.TryAdd(item);
                                }
                                
                                idDict.Remove(e.WorkID);
                            }
                        }
                    }
                };
                WorkFailed += onFailed;
                source.SetWatchFailedHandler(onFailed);
            }

            // 处理集合变更
            EventHandler<NotifyCollectionChangedEventArgs_Net20<TSource>> onCollectionChanged = null;
            onCollectionChanged = (s, e) =>
            {
                // 先检查是否可以继续监视
                if (source.GetCanWatch() == CanWatch.Allowed)
                {
                    while (source.TryTake(out TSource item))
                    {
                        TSource tempItem = item; // 修复闭包引用问题
                        WorkID id = QueueWorkItem(() => body(tempItem), workOption);
                        AddWorkToGroup(groupID, id);
                        lock (idDictLock)
                        {
                            idDict[id] = tempItem;
                        }
                    }

                    // 检查是否仍在监视状态，如果是则保持事件处理器注册
                    // 不要反注册再重新注册，这会导致竞态条件
                    if (source.GetWatchState() != WatchStates.Watching)
                    {
                        source.CollectionChanged -= onCollectionChanged;
                    }
                }
                else
                {
                    // 不允许监视，移除事件处理器
                    source.CollectionChanged -= onCollectionChanged;
                }
            };

            if (!source.StartWatching(onCollectionChanged))
            {
                return null;
            }

            Group group = GetGroup(groupID);
            
            // 手动触发一次处理，处理初始队列中的元素
            onCollectionChanged(null, null);

            return group;
        }

        /// <summary>
        /// 停止监视可观察集合
        /// Stop watching observable collection
        /// </summary>
        /// <typeparam name="TSource">元素类型 / Element type</typeparam>
        /// <param name="source">源集合 / Source collection</param>
        /// <param name="group">工作组对象 / Group object</param>
        /// <param name="keepRunning">是否保持运行 / Whether to keep running</param>
        public void StopWatching<TSource>(ConcurrentObservableCollection_Net20<TSource> source, Group group = null, bool keepRunning = false)
        {
            //StopWatchingCore(source, group, false, keepRunning);
            StopWatchingCore(source,group,true,keepRunning);
        }

        ///// <summary>
        ///// 强制停止监视可观察集合
        ///// Force stop watching observable collection
        ///// 虽然这种方法比Thread.Abort更安全，但从业务逻辑的角度来看，
        ///// 它仍可能导致不可预测的结果，无法保证退出线程的时间消耗，
        ///// 因此应尽可能避免使用强制停止。
        ///// Although this approach is safer than Thread.Abort, from the perspective of the business logic,
        ///// it can still potentially lead to unpredictable results and cannot guarantee the time consumption of exiting the thread,
        ///// therefore you should avoid using force stop as much as possible.
        ///// </summary>
        ///// <typeparam name="TSource">元素类型 / Element type</typeparam>
        ///// <param name="source">源集合 / Source collection</param>
        ///// <param name="group">工作组对象 / Group object</param>
        ///// <param name="keepRunning">是否保持运行 / Whether to keep running</param>
        //public void ForceStopWatching<TSource>(ConcurrentObservableCollection_Net20<TSource> source, Group group = null, bool keepRunning = false)
        //{
        //    StopWatchingCore(source, group, true, keepRunning);
        //}

        /// <summary>
        /// 停止监视的核心逻辑
        /// Core logic for stopping watching
        /// </summary>
        private void StopWatchingCore<TSource>(
            ConcurrentObservableCollection_Net20<TSource> source,
            Group group,
            bool forceStop,
            bool keepRunning = false)
        {
            //不使用forceStop很难保证可控性
            if (forceStop)
            {
                source.ForceStopWatching(keepRunning);
                // 如果是强制停止且提供了组，停止组内所有工作项
                if (group != null)
                {
                    ForceStopGroupWorks(group);
                }
            }
            else
            {
                source.StopWatching(keepRunning);
            }

            if (!keepRunning && group != null)
            {
                group.Wait(30000);
            }

            EventHandler<WorkFailedEventArgs> watchFailedHandler = source.GetWatchFailedHandler();
            if (watchFailedHandler != null)
            {
                WorkFailed -= watchFailedHandler;
                source.SetWatchFailedHandler(null);
            }
        }
    }
}