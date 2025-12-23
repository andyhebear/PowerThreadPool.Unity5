#if UNITY
using UnityEngine;
using System.Collections;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
using System;
using PowerThreadPool_Net20.Threading;

namespace PowerThreadPool_Net20.Examples
{
    /// <summary>
    /// Unity 5.6中的PowerThreadPool_Net20使用示例
    /// Example of using PowerThreadPool_Net20 in Unity 5.6
    /// </summary>
    public class UnityExample : MonoBehaviour
    {
        private PowerPool _threadPool;
        
        /// <summary>
        /// Unity Start方法 / Unity Start method
        /// </summary>
        void Start()
        {
            // 创建线程池选项 / Create thread pool options
            var options = new PowerPoolOption
            {
                 MinThreads = 2,
                MaxThreads = 8,
                 IdleThreadTimeout =TimeSpan.FromMilliseconds( 30000), // 30秒 / 30 seconds            
                  EnableStatisticsCollection = true,
                   StartSuspended = true,
            };
            
            // 创建并启动线程池 / Create and start thread pool
            _threadPool = new PowerPool(options);
            _threadPool.Start();
            
            Debug.Log("PowerThreadPool_Net20 started successfully!");
            
            // 添加一些测试工作项 / Add some test work items
            AddWorkItems();
        }
        
        /// <summary>
        /// 添加工作项示例 / Add work items example
        /// </summary>
        private void AddWorkItems()
        {
            // 简单工作项 / Simple work item
            var workId1 = _threadPool.QueueWorkItem(
                () => 
                {
                    // 模拟计算密集型任务 / Simulate compute-intensive task
                    int result = 0;
                    for (int i = 0; i < 1000000; i++)
                    {
                        result += i;
                    }
                    return result;
                },
                new WorkOption {  Timeout= TimeSpan.FromSeconds(5) }
              
            );
            
            Debug.Log("Queued compute work item: " + workId1);
            
            // 异步工作项 / Asynchronous work item
            var workId2 = _threadPool.QueueWorkItem(
                () => 
                {
                    // 模拟IO密集型任务 / Simulate IO-intensive task
                    System.Threading.Thread.Sleep(2000);
                    return "IO Task Completed";
                },
                new WorkOption { Timeout = TimeSpan.FromMilliseconds( 5000) } // 5秒超时 / 5 seconds timeout
           
            );
            
            Debug.Log("Queued IO work item: " + workId2);
            CancellationTokenSource cts = new CancellationTokenSource();
            // 带参数的工作项 / Work item with parameters
            var workId3 = _threadPool.QueueWorkItem(
                (param) => 
                {
                    string message = param as string;
                    Debug.Log("Worker thread message: " + message);
                    return "Processed: " + message;
                },
                "Hello from Unity!",
                new WorkOption {  CancellationToken= cts.Token }
               
            );
            
            Debug.Log("Queued parameterized work item: " + workId3);
        }
        
        /// <summary>
        /// Unity Update方法 / Unity Update method
        /// </summary>
        void Update()
        {
            // 按键测试功能 / Key testing functionality
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TestThreadPoolStats();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                CancelAllWork();
            }
            
            if (Input.GetKeyDown(KeyCode.W))
            {
                WaitAllWork();
            }
        }
        
        /// <summary>
        /// 测试线程池统计信息 / Test thread pool statistics
        /// </summary>
        private void TestThreadPoolStats()
        {
            var stats = _threadPool.GetStatistics();
            Debug.Log("=== Thread Pool Statistics ===");
            Debug.Log("Total Work Items: " + stats.TotalWorkItems);
            Debug.Log("Completed Work Items: " + stats.CompletedWorkItems);
            Debug.Log("Failed Work Items: " + stats.FailedWorkItems);
            Debug.Log("Active Worker Threads: " + stats.ActiveWorkerThreads);
            Debug.Log("Queued Work Items: " + stats.QueuedWorkItems);
            Debug.Log("Average Queue Time: " + stats.AverageQueueTime + "ms");
            Debug.Log("Average Execute Time: " + stats.AverageExecuteTime + "ms");
        }
        
        /// <summary>
        /// 取消所有工作 / Cancel all work
        /// </summary>
        private void CancelAllWork()
        {
            Debug.Log("Cancelling all work...");
            _threadPool.CancelAllWork();
        }
        
        /// <summary>
        /// 等待所有工作完成 / Wait for all work to complete
        /// </summary>
        private void WaitAllWork()
        {
            Debug.Log("Waiting for all work to complete...");
            _threadPool.WaitAll();
            Debug.Log("All work completed!");
        }
        
        /// <summary>
        /// Unity OnDestroy方法 / Unity OnDestroy method
        /// </summary>
        void OnDestroy()
        {
            // 清理线程池资源 / Cleanup thread pool resources
            if (_threadPool != null)
            {
                _threadPool.Dispose();
                _threadPool = null;
                Debug.Log("PowerThreadPool_Net20 disposed.");
            }
        }
        
        /// <summary>
        /// OnGUI方法，用于显示操作提示 / OnGUI method for showing operation hints
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("PowerThreadPool_Net20 Controls:");
            GUILayout.Label("Press SPACE: Show Statistics");
            GUILayout.Label("Press C: Cancel All Work");
            GUILayout.Label("Press W: Wait All Work");
            GUILayout.EndArea();
        }
    }
}
#endif