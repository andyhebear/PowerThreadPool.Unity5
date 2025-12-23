
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Threading;
using PowerThreadPool_Net20.Works;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PowerThreadPool_Net20
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PowerThreadPool_Net20 Example");
            Console.WriteLine("================================");

            // Create a new PowerPool with 2 minimum workers and 4 maximum workers
            PowerPool pool = new PowerPool(new PowerThreadPool_Net20.Options.PowerPoolOption()
            { MaxThreads = 4,ThreadNamePrefix = "rs.",ThreadQueueLimit = 8 });

            pool.Start();
            Console.WriteLine("Pool created with MinWorkers: 2, MaxWorkers: 4");
            Console.WriteLine("CurrentWorkerCount: {0}",pool.WaitingWorkCount);
            Console.WriteLine();
            ///////////////          
            testAddWorkItems(pool);
            //Console.ReadKey();

            testParallelExe(pool);
            // Example 1: Queue simple actions
            Console.WriteLine("Example 1: Queue simple actions");

            var workId1 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("Work 1 started");
                Thread.Sleep(1000);
                Console.WriteLine("Work 1 completed");
            });

            var workId2 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("Work 2 started");
                Thread.Sleep(500);
                Console.WriteLine("Work 2 completed");
            });

            // Wait for both works to complete
            var result1 = pool.GetResultAndWait(workId1);
            var result2 = pool.GetResultAndWait(workId2);
            //pool.WaitAll();
            Console.WriteLine("================================");
            Console.WriteLine();

            // Example 2: Queue functions with results
            Console.WriteLine("Example 2: Queue functions with results");

            var work3 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("Work 3 started");
                Thread.Sleep(800);
                int result = 42;
                Console.WriteLine("Work 3 completed with result: {0}",result);
                return result;
            });

            var work4 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("Work 4 started");
                Thread.Sleep(600);
                string result = "Hello from PowerThreadPool";
                Console.WriteLine("Work 4 completed with result: {0}",result);
                return result;
            });

            // Get results         
            var result3 = pool.GetResultAndWait(work3);
            var result4 = pool.GetResultAndWait(work4);

            Console.WriteLine("Retrieved results: work3={0}, work4={1}",result3.Result,result4.Result);
            Console.WriteLine("================================");
            Console.WriteLine();

            // Example 3: Queue with priorities
            Console.WriteLine("Example 3: Queue with priorities");

            var work5 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("Low priority work (5) started");
                Thread.Sleep(1000);
                Console.WriteLine("Low priority work (5) completed");
            });

            var work6 = pool.QueueWorkItem(() =>
            {
                Console.WriteLine("High priority work (6) started");
                Thread.Sleep(500);
                Console.WriteLine("High priority work (6) completed");
            });

            // Wait for both works to complete
            //work5.Wait();
            //work6.Wait();
            pool.WaitAll();
            Console.WriteLine("================================");
            Console.WriteLine();

            // Example 4: Queue multiple works and wait all
            Console.WriteLine("Example 4: Queue multiple works and wait all");

            int workCount = 8;
            WorkID[] works = new WorkID[workCount];

            for (int i = 0; i < workCount; i++)
            {
                int workIndex = i;
                works[i] = pool.QueueWorkItem(() =>
                {
                    Console.WriteLine("Batch work {0} started",workIndex);
                    Thread.Sleep(new Random().Next(2000,8000));
                    Console.WriteLine("Batch work {0} completed",workIndex);
                });
            }

            // Wait for all works to complete
            pool.WaitAll();

            Console.WriteLine();
            Console.WriteLine("All works completed");
            Console.WriteLine("Final CurrentWorkerCount: {0}",pool.WaitingWorkCount);
            Console.WriteLine("================================");

            Console.WriteLine();
            Console.WriteLine("PowerPool disposed");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// 添加工作项示例 / Add work items example
        /// </summary>
        static void testAddWorkItems(PowerPool _threadPool)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            // 简单工作项 / Simple work item
            var workId1 = _threadPool.QueueWorkItem<int>(
                () =>
                {
                    // 模拟计算密集型任务 / Simulate compute-intensive task
                    int result = 0;
                    for (int i = 0; i < 10000; i++)
                    {
                        result += i;
                        Thread.Sleep(1);
                    }
                    Console.WriteLine("result:" + result);
                    return result;
                },
                new WorkOption { CancellationToken = cts.Token }//,
                //"ComputeGroup"
            );
            Thread.Sleep(2000);
            cts.Cancel();
            Console.WriteLine("Queued compute work item: " + workId1);
            //return;
            // 异步工作项 / Asynchronous work item
            var workId2 = _threadPool.QueueWorkItem(
                () =>
                {
                    // 模拟IO密集型任务 / Simulate IO-intensive task
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("IO Task Completed");
                    return "IO Task Completed";
                },
                new WorkOption { Timeout = TimeSpan.FromMilliseconds(5000) }//, // 5秒超时 / 5 seconds timeout
                //"IOGroup"
            );

            Console.WriteLine("Queued IO work item: " + workId2);

            // 带参数的工作项 / Work item with parameters
            var workId3 = _threadPool.QueueWorkItem(
                () =>
                {
                    string message = "";//param as string;
                    Console.WriteLine("Worker thread message: " + message);
                    return "Processed: " + message;
                },
                //"Hello from Unity!",
                new WorkOption { CancellationToken = cts.Token }//,
                //"MessageGroup"
            );
            _threadPool.WaitAll();
            Console.WriteLine("Queued parameterized work item: " + workId3);
        }
        static void testParallelExe(PowerPool _threadPool)
        {
            // 创建线程池           

            Console.WriteLine("=== 并行循环和ExecuteResult示例开始 ===");

            // 1. ParallelFor示例
            TestParallelFor(_threadPool);

            // 2. ParallelForEach示例
            TestParallelForEach(_threadPool);

            // 3. ParallelInvoke示例
            TestParallelInvoke(_threadPool);

            // 4. ExecuteResult示例
            TestExecuteResults(_threadPool);
        }

        /// <summary>
        /// 测试ParallelFor
        /// </summary>
        static void TestParallelFor(PowerPool _threadPool)
        {
            Console.WriteLine("--- ParallelFor 测试 ---");

            // 并行计算0-99的平方
            WorkID[] workIds = _threadPool.ParallelFor(0,100,i =>
            {
                int result = i * i;
                Console.WriteLine($"计算 {i}^2 = {result}");
            });

            Console.WriteLine($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();

            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Console.WriteLine($"ParallelFor 完成，成功: {Array.FindAll(results,r => r.IsSuccess).Length}");
        }

        /// <summary>
        /// 测试ParallelForEach
        /// </summary>
        static void TestParallelForEach(PowerPool _threadPool)
        {
            Console.WriteLine("--- ParallelForEach 测试 ---");

            var numbers = new int[] { 1,2,3,4,5,6,7,8,9,10 };

            // 并行处理每个数字
            WorkID[] workIds = _threadPool.ParallelForEach(numbers,number =>
            {
                Console.WriteLine($"处理数字: {number}");
                // 模拟一些计算
                int result = 0;
                for (int i = 0; i < 100000; i++)
                {
                    result += number * i;
                }
            });

            Console.WriteLine($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();

            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Console.WriteLine($"ParallelForEach 完成，成功: {Array.FindAll(results,r => r.IsSuccess).Length}");
        }

        /// <summary>
        /// 测试ParallelInvoke
        /// </summary>
        static void TestParallelInvoke(PowerPool _threadPool)
        {
            Console.WriteLine("--- ParallelInvoke 测试 ---");

            // 并行执行多个操作
            WorkID[] workIds = _threadPool.ParallelInvoke(
                () => Console.WriteLine("任务1: 执行数据处理"),
                () => Console.WriteLine("任务2: 执行网络请求"),
                () => Console.WriteLine("任务3: 执行文件读写"),
                () => Console.WriteLine("任务4: 执行计算任务")
            );

            Console.WriteLine($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();

            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Console.WriteLine($"ParallelInvoke 完成，成功: {Array.FindAll(results,r => r.IsSuccess).Length}");
        }

        /// <summary>
        /// 测试ExecuteResult功能
        /// </summary>
        static void TestExecuteResults(PowerPool _threadPool)
        {
            Console.WriteLine("--- ExecuteResult 测试 ---");

            // 提交带返回值的工作项
            WorkID workId1 = _threadPool.QueueWorkItem(() =>
            {
                System.Threading.Thread.Sleep(1000); // 模拟工作
                return "成功完成";
            });

            WorkID workId2 = _threadPool.QueueWorkItem(() =>
            {
                System.Threading.Thread.Sleep(500);
                return 42;
            });

            WorkID workId3 = _threadPool.QueueWorkItem(() =>
            {
                // 模拟失败
                throw new InvalidOperationException("测试异常");
            });

            // 等待并获取结果
            Console.WriteLine("等待工作完成...");
            var result1 = _threadPool.GetResultAndWait(workId1);
            var result2 = _threadPool.GetResultAndWait(workId2);
            var result3 = _threadPool.GetResultAndWait(workId3);

            Console.WriteLine($"结果1: {result1.Result}, 状态: {result1.Status}, 耗时: {result1.Duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"结果2: {result2.Result}, 状态: {result2.Status}, 耗时: {result2.Duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"结果3: 异常={result3.Exception?.Message}, 状态: {result3.Status}, 耗时: {result3.Duration.TotalMilliseconds:F0}ms");

            // 批量获取结果
            Console.WriteLine($"当前缓存的结果数量: {_threadPool.CachedResultCount}");

            var allResults = _threadPool.GetResultsAndWait(new WorkID[] { workId1,workId2,workId3 });
            Console.WriteLine($"批量获取了 {allResults.Length} 个结果");

            // 清理结果缓存
            _threadPool.ClearAllResults();
            Console.WriteLine($"清理后缓存结果数量: {_threadPool.CachedResultCount}");
        }
    }
}
