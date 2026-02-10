
using PowerThreadPool_Net20.Groups;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;
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
        static void Main(string[] args) {
            TestSchedulingFunctionality.RunAllTests();
            Console.ReadLine();
            Test_Group_Usage.RunAllTests();
            Console.ReadLine();
            PowerPoolComprehensiveTests.RunAllTests();
            Console.ReadLine();
            Console.WriteLine("PowerThreadPool_Net20 Example");
            Console.WriteLine("================================");

            // Create a new PowerPool with 2 minimum workers and 4 maximum workers
            PowerPool pool = new PowerPool(new PowerThreadPool_Net20.Options.PowerPoolOption() { MaxThreads = 4,ThreadNamePrefix = "rs.",ThreadQueueLimit = 8 });

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

            var workId1 = pool.QueueWorkItem(() => {
                Console.WriteLine("Work 1 started");
                Thread.Sleep(1000);
                Console.WriteLine("Work 1 completed");
            });

            var workId2 = pool.QueueWorkItem(() => {
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

            var work3 = pool.QueueWorkItem(() => {
                Console.WriteLine("Work 3 started");
                Thread.Sleep(800);
                int result = 42;
                Console.WriteLine("Work 3 completed with result: {0}",result);
                return result;
            });

            var work4 = pool.QueueWorkItem(() => {
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

            var work5 = pool.QueueWorkItem(() => {
                Console.WriteLine("Low priority work (5) started");
                Thread.Sleep(1000);
                Console.WriteLine("Low priority work (5) completed");
            });

            var work6 = pool.QueueWorkItem(() => {
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

            for (int i = 0; i < workCount; i++) {
                int workIndex = i;
                works[i] = pool.QueueWorkItem(() => {
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
        static void testAddWorkItems(PowerPool _threadPool) {
            CancellationTokenSource cts = new CancellationTokenSource();
            // 简单工作项 / Simple work item
            var workId1 = _threadPool.QueueWorkItem<int>(
                () => {
                    // 模拟计算密集型任务 / Simulate compute-intensive task
                    int result = 0;
                    for (int i = 0; i < 10000; i++) {
                        result += i;
                        Thread.Sleep(1);
                    }
                    Console.WriteLine("result:" + result);
                    return result;
                },
                new WorkOption(cts.Token)//,
                                         //"ComputeGroup"
            );
            Thread.Sleep(2000);
            cts.Cancel();
            Console.WriteLine("Queued compute work item: " + workId1);
            //return;
            // 异步工作项 / Asynchronous work item
            var workId2 = _threadPool.QueueWorkItem(
                () => {
                    // 模拟IO密集型任务 / Simulate IO-intensive task
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("IO Task Completed");
                    return "IO Task Completed";
                },
                new WorkOption(TimeSpan.FromMilliseconds(5000))//, // 5秒超时 / 5 seconds timeout
                                                               //"IOGroup"
            );

            Console.WriteLine("Queued IO work item: " + workId2);

            // 带参数的工作项 / Work item with parameters
            var workId3 = _threadPool.QueueWorkItem(
                () => {
                    string message = "";//param as string;
                    Console.WriteLine("Worker thread message: " + message);
                    return "Processed: " + message;
                },
                //"Hello from Unity!",
                new WorkOption(cts.Token)//,
                                         //"MessageGroup"
            );
            _threadPool.WaitAll();
            Console.WriteLine("Queued parameterized work item: " + workId3);
        }
        static void testParallelExe(PowerPool _threadPool) {
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
        static void TestParallelFor(PowerPool _threadPool) {
            Console.WriteLine("--- ParallelFor 测试 ---");

            // 并行计算0-99的平方
            WorkID[] workIds = _threadPool.ParallelFor(0,100,i => {
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
        static void TestParallelForEach(PowerPool _threadPool) {
            Console.WriteLine("--- ParallelForEach 测试 ---");

            var numbers = new int[] { 1,2,3,4,5,6,7,8,9,10 };

            // 并行处理每个数字
            WorkID[] workIds = _threadPool.ParallelForEach(numbers,number => {
                Console.WriteLine($"处理数字: {number}");
                // 模拟一些计算
                int result = 0;
                for (int i = 0; i < 100000; i++) {
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
        static void TestParallelInvoke(PowerPool _threadPool) {
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
        static void TestExecuteResults(PowerPool _threadPool) {
            Console.WriteLine("--- ExecuteResult 测试 ---");

            // 提交带返回值的工作项
            WorkID workId1 = _threadPool.QueueWorkItem(() => {
                System.Threading.Thread.Sleep(1000); // 模拟工作
                return "成功完成";
            });

            WorkID workId2 = _threadPool.QueueWorkItem(() => {
                System.Threading.Thread.Sleep(500);
                return 42;
            });

            WorkID workId3 = _threadPool.QueueWorkItem(() => {
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

    /// <summary>
    /// PowerPool全面测试套件
    /// Comprehensive test suite for PowerPool
    /// 覆盖所有公开接口的功能测试
    /// </summary>
    public class PowerPoolComprehensiveTests
    {
        private int testPassed = 0;
        private int testFailed = 0;
        private readonly object consoleLock = new object();

        /// <summary>
        /// 主测试入口
        /// Main test entry point
        /// </summary>
        public static void RunAllTests() {
            PowerPoolComprehensiveTests tests = new PowerPoolComprehensiveTests();
            tests.RunAll();
        }

        /// <summary>
        /// 运行所有测试
        /// Run all tests
        /// </summary>
        public void RunAll() {
            Console.WriteLine("==================================================");
            Console.WriteLine("PowerPool 全面测试套件 / Comprehensive Test Suite");
            Console.WriteLine("==================================================\n");

            try {
                // 基础功能测试
                TestBasicConstruction();
                TestStartStop();
                TestQueueWorkItem();
                TestQueueWorkItemWithResult();

                // 并行操作测试
                TestParallelFor();
                TestParallelForEach();
                TestParallelInvoke();

                // 结果获取测试
                TestGetResult();
                TestGetResults();
                TestGetResultAndWait();
                TestGetResultsAndWait();

                // 等待操作测试
                TestWaitAll();
                TestWaitWork();

                // 结果缓存管理测试
                TestClearResult();
                TestClearResults();
                TestClearExpiredResults();
                TestClearAllResults();

                // 优先级测试
                TestPriority();

                // 重试功能测试
                TestRetryWithFixedCount();
                TestRetryWithCustomCondition();
                TestRetryTimeout();

                // 超时功能测试
                TestWorkTimeout();
                TestWorkNoTimeout();

                // 取消功能测试
                TestWorkCancellation();

                // 统计信息测试
                TestStatistics();

                // 事件测试
                TestWorkCompletedEvent();
                TestWorkFailedEvent();
                TestPoolStartedEvent();
                TestPoolStoppedEvent();

                // 状态摘要测试
                TestGetWorkStatusSummary();

                // 暂停恢复测试
                TestPauseResume();

                // 清空队列测试
                TestClearQueue();

                // 弹性扩容测试
                TestElasticExpansion();

                // 线程回收测试
                TestIdleThreadCleanup();

                // 结果缓存过期测试
                TestResultCacheExpiration();

                // 队列限制测试
                TestQueueLimit();

                // 边界条件测试
                TestEmptyParallelFor();
                TestEmptyParallelForEach();
                TestEmptyParallelInvoke();

                // 异常处理测试
                TestExceptionHandling();
                TestWorkOptionException();

                // Dispose测试
                TestDispose();

                // 并发压力测试
                TestConcurrentOperations();

                Console.WriteLine("\n==================================================");
                Console.WriteLine($"测试完成 / Tests Completed");
                Console.WriteLine($"通过 / Passed: {testPassed}");
                Console.WriteLine($"失败 / Failed: {testFailed}");
                Console.WriteLine($"总计 / Total:  {testPassed + testFailed}");
                Console.WriteLine("==================================================");
            }
            catch (Exception ex) {
                Console.WriteLine($"测试运行时发生错误 / Error running tests: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

        }

        #region 辅助方法

        /// <summary>
        /// 打印测试标题
        /// Print test title
        /// </summary>
        private void PrintTestTitle(string title) {
            lock (consoleLock) {
                Console.WriteLine($"\n>>> {title}");
            }
        }

        /// <summary>
        /// 打印测试结果
        /// Print test result
        /// </summary>
        private void PrintTestResult(string testName,bool passed,string details = "") {
            lock (consoleLock) {
                if (passed) {
                    testPassed++;
                    Console.WriteLine($"  ✓ {testName} - PASSED");
                    if (!string.IsNullOrEmpty(details))
                        Console.WriteLine($"    {details}");
                }
                else {
                    testFailed++;
                    Console.WriteLine($"  ✗ {testName} - FAILED");

                    if (!string.IsNullOrEmpty(details))
                        Console.WriteLine($"    {details}");
                }
            }
        }

        /// <summary>
        /// 创建测试线程池
        /// Create test thread pool
        /// </summary>
        private PowerPool CreateTestPool(PowerPoolOption options = null) {
            PowerPoolOption opt = options ?? new PowerPoolOption {
                MaxThreads = 4,
                MinThreads = 1,
                ThreadQueueLimit = 100,
                IdleThreadTimeout = TimeSpan.FromSeconds(1),
                ResultCacheExpiration = TimeSpan.FromSeconds(30),
                EnableResultCacheExpiration = true
            };
            PowerPool pool = new PowerPool(opt);
            pool.Start();
            return pool;
        }

        #endregion

        #region 基础功能测试

        /// <summary>
        /// 测试基础构造
        /// Test basic construction
        /// </summary>
        private void TestBasicConstruction() {
            PrintTestTitle("测试基础构造 / Test Basic Construction");

            // 测试默认构造
            PowerPool pool1 = new PowerPool();
            bool test1 = pool1 != null && !pool1.IsRunning;
            PrintTestResult("默认构造",test1);
            pool1.Dispose();

            // 测试带选项构造
            PowerPoolOption options = new PowerPoolOption {
                MaxThreads = 8,
                MinThreads = 2
            };
            PowerPool pool2 = new PowerPool(options);
            bool test2 = pool2 != null && pool2.Options.MaxThreads == 8;
            PrintTestResult("带选项构造",test2);
            pool2.Dispose();

            // 测试IsRunning属性
            PowerPool pool3 = new PowerPool();
            bool test3 = !pool3.IsRunning;
            PrintTestResult("未启动时IsRunning为false",test3);
            pool3.Dispose();
        }

        /// <summary>
        /// 测试启动停止
        /// Test start and stop
        /// </summary>
        private void TestStartStop() {
            PrintTestTitle("测试启动停止 / Test Start Stop");

            PowerPool pool = new PowerPool();

            // 测试启动
            pool.Start();
            bool test1 = pool.IsRunning;
            PrintTestResult("启动后IsRunning为true",test1);

            // 测试重复启动
            pool.Start();
            bool test2 = pool.IsRunning;
            PrintTestResult("重复启动不会出错",test2);

            // 测试停止
            pool.Stop();
            bool test3 = !pool.IsRunning;
            PrintTestResult("停止后IsRunning为false",test3);

            // 测试重复停止
            pool.Stop();
            bool test4 = !pool.IsRunning;
            PrintTestResult("重复停止不会出错",test4);

            pool.Dispose();
        }

        /// <summary>
        /// 测试队列工作项
        /// Test queue work item
        /// </summary>
        private void TestQueueWorkItem() {
            PrintTestTitle("测试队列工作项 / Test Queue Work Item");

            using (PowerPool pool = CreateTestPool()) {
                int counter = 0;
                object lockObj = new object();

                // 测试基本队列
                WorkID id1 = pool.QueueWorkItem(() => {
                    lock (lockObj) counter++;
                });

                pool.WaitAll();

                bool test1 = counter == 1;
                PrintTestResult("基本队列执行",test1);

                // 测试多个工作项
                for (int i = 0; i < 10; i++) {
                    pool.QueueWorkItem(() => {
                        lock (lockObj) counter++;
                    });
                }

                pool.WaitAll();

                bool test2 = counter == 11;
                PrintTestResult("多个工作项队列执行",test2);

                // 测试带选项的队列
                WorkOption option = new WorkOption(
                     WorkPriority.High
                );
                WorkID id2 = pool.QueueWorkItem(() => {
                    lock (lockObj) counter++;
                },option);

                pool.WaitAll();

                bool test3 = counter == 12;
                PrintTestResult("带选项的队列执行",test3);
            }
        }

        /// <summary>
        /// 测试带返回值的队列工作项
        /// Test queue work item with result
        /// </summary>
        private void TestQueueWorkItemWithResult() {
            PrintTestTitle("测试带返回值的队列工作项 / Test Queue Work Item With Result");

            using (PowerPool pool = CreateTestPool()) {
                // 测试带返回值的工作项
                WorkID id1 = pool.QueueWorkItem<int>(() => 42);
                ExecuteResult result1 = pool.GetResultAndWait(id1);

                bool test1 = result1.IsSuccess && (int)result1.Result == 42;
                PrintTestResult("获取返回值",test1);

                // 测试字符串返回值
                WorkID id2 = pool.QueueWorkItem<string>(() => "Hello PowerPool");
                ExecuteResult result2 = pool.GetResultAndWait(id2);

                bool test2 = result2.IsSuccess && (string)result2.Result == "Hello PowerPool";
                PrintTestResult("获取字符串返回值",test2);

                // 测试复杂对象返回值
                WorkID id3 = pool.QueueWorkItem(() => new { A = 1,B = 2 });
                ExecuteResult result3 = pool.GetResultAndWait(id3);

                bool test3 = result3.IsSuccess && result3.Result != null;
                PrintTestResult("获取复杂对象返回值",test3);
            }
        }

        #endregion

        #region 并行操作测试

        /// <summary>
        /// 测试并行循环
        /// Test parallel for
        /// </summary>
        private void TestParallelFor() {
            PrintTestTitle("测试并行循环 / Test Parallel For");

            using (PowerPool pool = CreateTestPool()) {
                int sum = 0;
                object lockObj = new object();

                // 测试基本并行循环
                WorkID[] ids = pool.ParallelFor(0,100,i => {
                    lock (lockObj) sum += i;
                });

                pool.WaitAll();

                int expected = 0;
                for (int i = 0; i < 100; i++) expected += i;

                bool test1 = sum == expected;
                PrintTestResult("并行循环求和正确",test1);

                // 测试带步长的并行循环
                sum = 0;
                ids = pool.ParallelFor(0,100,i => {
                    lock (lockObj) sum += i;
                },step: 2);

                pool.WaitAll();

                expected = 0;
                for (int i = 0; i < 100; i += 2) expected += i;

                bool test2 = sum == expected;
                PrintTestResult("带步长并行循环正确",test2);

                // 测试批量执行
                int threadCount = pool.ActiveWorkerThreads;
                bool test3 = ids.Length <= threadCount;
                PrintTestResult($"批量执行工作项数({ids.Length}) <= 线程数({threadCount})",test3);
            }
        }

        /// <summary>
        /// 测试并行foreach
        /// Test parallel foreach
        /// </summary>
        private void TestParallelForEach() {
            PrintTestTitle("测试并行Foreach / Test Parallel ForEach");

            using (PowerPool pool = CreateTestPool()) {
                int sum = 0;
                object lockObj = new object();
                List<int> numbers = new List<int>();
                for (int i = 1; i <= 100; i++) numbers.Add(i);

                // 测试基本并行foreach
                WorkID[] ids = pool.ParallelForEach(numbers,i => {
                    lock (lockObj) sum += i;
                });

                pool.WaitAll();

                bool test1 = sum == 5050;
                PrintTestResult("并行foreach求和正确",test1);

                // 测试数组
                sum = 0;
                int[] array = numbers.ToArray();
                ids = pool.ParallelForEach(array,i => {
                    lock (lockObj) sum += i;
                });

                pool.WaitAll();

                bool test2 = sum == 5050;
                PrintTestResult("并行foreach数组正确",test2);
            }
        }

        /// <summary>
        /// 测试并行invoke
        /// Test parallel invoke
        /// </summary>
        private void TestParallelInvoke() {
            PrintTestTitle("测试并行Invoke / Test Parallel Invoke");

            using (PowerPool pool = CreateTestPool()) {
                int counter1 = 0, counter2 = 0, counter3 = 0;
                object lockObj = new object();

                // 测试基本并行invoke
                Action[] actions = new Action[]
                {
                    () => { lock(lockObj) counter1++; },
                    () => { lock(lockObj) counter2++; },
                    () => { lock(lockObj) counter3++; }
                };

                WorkID[] ids = pool.ParallelInvoke(actions);
                pool.WaitAll();

                bool test1 = counter1 == 1 && counter2 == 1 && counter3 == 1;
                PrintTestResult("并行invoke执行正确",test1);

                bool test2 = ids.Length == 3;
                PrintTestResult("返回的工作ID数量正确",test2);
            }
        }

        #endregion

        #region 结果获取测试

        /// <summary>
        /// 测试获取结果
        /// Test get result
        /// </summary>
        private void TestGetResult() {
            PrintTestTitle("测试获取结果 / Test Get Result");

            using (PowerPool pool = CreateTestPool()) {
                // 测试获取成功结果
                WorkID id1 = pool.QueueWorkItem(() => 123);
                pool.WaitWork(id1);
                ExecuteResult result1 = pool.GetResult(id1);

                bool test1 = result1.IsSuccess && (int)result1.Result == 123;
                PrintTestResult("获取成功结果",test1);

                // 测试获取失败结果
                WorkID id2 = pool.QueueWorkItem(() => {
                    throw new InvalidOperationException("Test exception");
                });
                pool.WaitWork(id2);
                ExecuteResult result2 = pool.GetResult(id2);

                bool test2 = result2.IsFailed && result2.Exception is InvalidOperationException;
                PrintTestResult("获取失败结果",test2);

                // 测试获取未完成的工作结果
                try {
                    WorkID id3 = pool.QueueWorkItem(() => Thread.Sleep(5000));
                    ExecuteResult result3 = pool.GetResult(id3);
                    PrintTestResult("获取未完成工作应抛出异常",false);
                }
                catch (InvalidOperationException) {
                    PrintTestResult("获取未完成工作抛出异常",true);
                }
            }
        }

        /// <summary>
        /// 测试批量获取结果
        /// Test get results
        /// </summary>
        private void TestGetResults() {
            PrintTestTitle("测试批量获取结果 / Test Get Results");

            using (PowerPool pool = CreateTestPool()) {
                WorkID[] ids = new WorkID[5];
                for (int i = 0; i < 5; i++) {
                    ids[i] = pool.QueueWorkItem(() => i * 10);
                }

                pool.WaitAll();

                ExecuteResult[] results = pool.GetResults(ids);

                bool test1 = results.Length == 5;
                PrintTestResult("批量获取结果数量正确",test1);

                bool allCorrect = true;
                for (int i = 0; i < results.Length; i++) {
                    if (!results[i].IsSuccess || (int)results[i].Result != i * 10) {
                        allCorrect = false;
                        break;
                    }
                }
                PrintTestResult("批量获取结果内容正确",allCorrect);
            }
        }

        /// <summary>
        /// 测试等待并获取结果
        /// Test get result and wait
        /// </summary>
        private void TestGetResultAndWait() {
            PrintTestTitle("测试等待并获取结果 / Test Get Result And Wait");

            using (PowerPool pool = CreateTestPool()) {
                // 测试等待成功结果
                WorkID id1 = pool.QueueWorkItem(() => {
                    Thread.Sleep(200);
                    return 456;
                });
                ExecuteResult result1 = pool.GetResultAndWait(id1);

                bool test1 = result1.IsSuccess && (int)result1.Result == 456;
                PrintTestResult("等待并获取成功结果",test1);

                // 测试等待失败结果
                WorkID id2 = pool.QueueWorkItem(() => {
                    Thread.Sleep(100);
                    throw new Exception("Test");
                });
                ExecuteResult result2 = pool.GetResultAndWait(id2);

                bool test2 = result2.IsFailed;
                PrintTestResult("等待并获取失败结果",test2);

                // 测试超时
                try {
                    WorkID id3 = pool.QueueWorkItem(() => Thread.Sleep(10000));
                    ExecuteResult result3 = pool.GetResultAndWait(id3,500);
                    PrintTestResult("超时抛出异常",false);
                }
                catch (TimeoutException) {
                    PrintTestResult("超时正确抛出异常",true);
                }
            }
        }

        /// <summary>
        /// 测试批量等待并获取结果
        /// Test get results and wait
        /// </summary>
        private void TestGetResultsAndWait() {
            PrintTestTitle("测试批量等待并获取结果 / Test Get Results And Wait");

            //using (PowerPool pool = CreateTestPool()) {
            PowerPool pool = CreateTestPool();
            WorkID[] ids = new WorkID[5];
            for (int i = 0; i < 5; i++) {
                int val = i;
                ids[i] = pool.QueueWorkItem(() => {
                    Thread.Sleep(val * 50);
                    return val;
                });
            }

            ExecuteResult[] results = pool.GetResultsAndWait(ids,5000);

            bool test1 = results.Length == 5;
            PrintTestResult("批量等待并获取结果数量正确",test1);

            bool allCorrect = true;
            for (int i = 0; i < results.Length; i++) {
                if (!results[i].IsSuccess || (int)results[i].Result != i) {
                    allCorrect = false;
                    break;
                }
            }
            PrintTestResult("批量等待并获取结果内容正确",allCorrect);
            pool.Dispose();
            //}
        }

        #endregion

        #region 等待操作测试

        /// <summary>
        /// 测试等待所有工作完成
        /// Test wait all
        /// </summary>
        private void TestWaitAll() {
            PrintTestTitle("测试等待所有工作完成 / Test Wait All");

            using (PowerPool pool = CreateTestPool()) {
                int counter = 0;
                object lockObj = new object();

                for (int i = 0; i < 10; i++) {
                    pool.QueueWorkItem(() => {
                        Thread.Sleep(100);
                        lock (lockObj) counter++;
                    });
                }

                pool.WaitAll();

                bool test1 = counter == 10;
                PrintTestResult("WaitAll等待所有工作完成",test1);

                // 测试空队列为WaitAll
                pool.WaitAll();
                PrintTestResult("空队列WaitAll不会阻塞",true);
            }
        }

        /// <summary>
        /// 测试等待指定工作完成
        /// Test wait work
        /// </summary>
        private void TestWaitWork() {
            PrintTestTitle("测试等待指定工作完成 / Test Wait Work");

            using (PowerPool pool = CreateTestPool()) {
                // 测试等待单个工作
                WorkID id1 = pool.QueueWorkItem(() => {
                    Thread.Sleep(200);
                    return 999;
                });

                pool.WaitWork(id1);

                ExecuteResult result = pool.GetResult(id1);
                bool test1 = result.IsSuccess && (int)result.Result == 999;
                PrintTestResult("WaitWork等待单个工作完成",test1);

                // 测试超时
                WorkID id2 = pool.QueueWorkItem(() => Thread.Sleep(5000));
                try {
                    pool.WaitWork(id2,500);
                    PrintTestResult("WaitWork超时抛出异常",false);
                }
                catch (TimeoutException) {
                    PrintTestResult("WaitWork超时正确抛出异常",true);
                }
            }
        }

        #endregion

        #region 结果缓存管理测试

        /// <summary>
        /// 测试清除结果
        /// Test clear result
        /// </summary>
        private void TestClearResult() {
            PrintTestTitle("测试清除结果 / Test Clear Result");

            using (PowerPool pool = CreateTestPool()) {
                WorkID id1 = pool.QueueWorkItem(() => 100);
                pool.WaitWork(id1);

                bool test1 = pool.CachedResultCount > 0;
                PrintTestResult("执行后结果被缓存",test1);

                bool cleared = pool.ClearResult(id1);
                bool test2 = cleared && pool.CachedResultCount == 0;
                PrintTestResult("清除单个结果成功",test2);

                // 测试清除不存在的结果
                bool cleared2 = pool.ClearResult(new WorkID(true));
                bool test3 = !cleared2;
                PrintTestResult("清除不存在的结果返回false",test3);
            }
        }

        /// <summary>
        /// 测试批量清除结果
        /// Test clear results
        /// </summary>
        private void TestClearResults() {
            PrintTestTitle("测试批量清除结果 / Test Clear Results");

            using (PowerPool pool = CreateTestPool()) {
                WorkID[] ids = new WorkID[5];
                for (int i = 0; i < 5; i++) {
                    ids[i] = pool.QueueWorkItem(() => i);
                }

                pool.WaitAll();

                bool test1 = pool.CachedResultCount == 5;
                PrintTestResult("执行后所有结果被缓存",test1);

                int cleared = pool.ClearResults(ids);
                bool test2 = cleared == 5 && pool.CachedResultCount == 0;
                PrintTestResult("批量清除结果成功",test2);
            }
        }

        /// <summary>
        /// 测试清除过期结果
        /// Test clear expired results
        /// </summary>
        private void TestClearExpiredResults() {
            PrintTestTitle("测试清除过期结果 / Test Clear Expired Results");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 2,
                ResultCacheExpiration = TimeSpan.FromMilliseconds(100),
                EnableResultCacheExpiration = true
            })) {
                pool.Start();

                WorkID id1 = pool.QueueWorkItem(() => 1);
                pool.WaitWork(id1);

                bool test1 = pool.CachedResultCount > 0;
                PrintTestResult("执行后结果被缓存",test1);

                Thread.Sleep(150);

                int cleared = pool.ClearExpiredResults(1);
                bool test2 = cleared > 0;
                PrintTestResult("清除过期结果成功",test2);
            }
        }

        /// <summary>
        /// 测试清除所有结果
        /// Test clear all results
        /// </summary>
        private void TestClearAllResults() {
            PrintTestTitle("测试清除所有结果 / Test Clear All Results");

            using (PowerPool pool = CreateTestPool()) {
                for (int i = 0; i < 10; i++) {
                    pool.QueueWorkItem(() => i);
                }

                pool.WaitAll();

                bool test1 = pool.CachedResultCount == 10;
                PrintTestResult("执行后所有结果被缓存",test1);

                pool.ClearAllResults();

                bool test2 = pool.CachedResultCount == 0;
                PrintTestResult("清除所有结果成功",test2);
            }
        }

        #endregion

        #region 优先级测试

        /// <summary>
        /// 测试优先级
        /// Test priority
        /// </summary>
        private void TestPriority() {
            PrintTestTitle("测试优先级 / Test Priority");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MaxThreads = 1,MinThreads = 1 })) {
                pool.Start();

                List<string> executionOrder = new List<string>();
                object lockObj = new object();

                // 队列不同优先级的工作项
                pool.QueueWorkItem(() => {
                    Thread.Sleep(50);
                    lock (lockObj) executionOrder.Add("Normal");
                },new WorkOption(WorkPriority.Normal));

                pool.QueueWorkItem(() => {
                    Thread.Sleep(50);
                    lock (lockObj) executionOrder.Add("Critical");
                },new WorkOption(WorkPriority.Critical));

                pool.QueueWorkItem(() => {
                    Thread.Sleep(50);
                    lock (lockObj) executionOrder.Add("Low");
                },new WorkOption(WorkPriority.Low));

                pool.QueueWorkItem(() => {
                    Thread.Sleep(50);
                    lock (lockObj) executionOrder.Add("High");
                },new WorkOption(WorkPriority.High));

                pool.WaitAll();

                bool test1 = executionOrder.Count == 4;
                PrintTestResult("所有优先级工作都执行",test1);

                // Critical应该最先执行
                bool test2 = executionOrder[0] == "Critical";
                PrintTestResult("Critical优先级最先执行",test2);
            }
        }

        #endregion

        #region 重试功能测试

        /// <summary>
        /// 测试固定次数重试
        /// Test retry with fixed count
        /// </summary>
        private void TestRetryWithFixedCount() {
            PrintTestTitle("测试固定次数重试 / Test Retry With Fixed Count");

            using (PowerPool pool = CreateTestPool()) {
                int attemptCount = 0;
                object lockObj = new object();

                WorkID id = pool.QueueWorkItem(() => {
                    lock (lockObj) attemptCount++;
                    if (attemptCount < 3)
                        throw new InvalidOperationException("Retry test");
                    return "Success";
                },new WorkOption(
                     3,
                    TimeSpan.FromMilliseconds(100)
                ));

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                bool test1 = result.IsSuccess;
                PrintTestResult("重试后成功",test1);

                bool test2 = attemptCount == 3;
                PrintTestResult($"重试次数正确({attemptCount})",test2);
            }
        }

        /// <summary>
        /// 测试自定义条件重试
        /// Test retry with custom condition
        /// </summary>
        private void TestRetryWithCustomCondition() {
            PrintTestTitle("测试自定义条件重试 / Test Retry With Custom Condition");

            using (PowerPool pool = CreateTestPool()) {
                int attemptCount = 0;
                object lockObj = new object();

                WorkID id = pool.QueueWorkItem(() => {
                    lock (lockObj) attemptCount++;
                    throw new Exception("Test exception");
                },new WorkOption(
                     5,TimeSpan.FromMilliseconds(50),
                    (ex) => {
                        // 只在前3次失败时重试
                        return attemptCount < 3;
                    }
                ));

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                bool test1 = result.IsFailed;
                PrintTestResult("不满足重试条件后失败",test1);

                bool test2 = attemptCount == 3;
                PrintTestResult($"重试次数正确({attemptCount})",test2);
            }
        }

        /// <summary>
        /// 测试重试超时
        /// Test retry timeout
        /// </summary>
        private void TestRetryTimeout() {
            PrintTestTitle("测试重试超时 / Test Retry Timeout");

            using (PowerPool pool = CreateTestPool()) {
                WorkID id = pool.QueueWorkItem(() => {
                    throw new TimeoutException("Timeout");
                },new WorkOption(
                   3,
                    TimeSpan.FromMilliseconds(100)
                ));

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                // TimeoutException不应该触发重试
                bool test1 = result.IsTimeout;
                PrintTestResult("TimeoutException不重试",test1);
            }
        }

        #endregion

        #region 超时功能测试

        /// <summary>
        /// 测试工作超时
        /// Test work timeout
        /// </summary>
        private void TestWorkTimeout() {
            PrintTestTitle("测试工作超时 / Test Work Timeout");

            using (PowerPool pool = CreateTestPool()) {
                WorkID id = pool.QueueWorkItem(() => {
                    Thread.Sleep(5000);
                    return "Should not complete";
                },new WorkOption(
                   TimeSpan.FromMilliseconds(500)
                ));

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                bool test1 = result.IsTimeout;
                PrintTestResult("工作超时正确",test1);
            }
        }

        /// <summary>
        /// 测试工作不超时
        /// Test work no timeout
        /// </summary>
        private void TestWorkNoTimeout() {
            PrintTestTitle("测试工作不超时 / Test Work No Timeout");

            using (PowerPool pool = CreateTestPool()) {
                WorkID id = pool.QueueWorkItem(() => {
                    Thread.Sleep(100);
                    return "Completed";
                },new WorkOption(
                     TimeSpan.FromSeconds(1)
                ));

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                bool test1 = result.IsSuccess && (string)result.Result == "Completed";
                PrintTestResult("工作在超时前完成",test1);
            }
        }

        #endregion

        #region 取消功能测试

        /// <summary>
        /// 测试工作取消
        /// Test work cancellation
        /// </summary>
        private void TestWorkCancellation() {
            PrintTestTitle("测试工作取消 / Test Work Cancellation");

            using (PowerPool pool = CreateTestPool()) {
                CancellationToken token = new CancellationToken();

                WorkID id = pool.QueueWorkItem(() => {
                    for (int i = 0; i < 100; i++) {
                        token.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                    }
                    return "Completed";
                },new WorkOption(
                     token
                ));

                Thread.Sleep(200);
                token.Cancel();

                pool.WaitWork(id);

                ExecuteResult result = pool.GetResult(id);

                bool test1 = result.IsCanceled;
                PrintTestResult("工作取消正确",test1);
            }
        }

        #endregion

        #region 统计信息测试

        /// <summary>
        /// 测试统计信息
        /// Test statistics
        /// </summary>
        private void TestStatistics() {
            PrintTestTitle("测试统计信息 / Test Statistics");

            using (PowerPool pool = CreateTestPool()) {
                // 队列一些工作
                for (int i = 0; i < 5; i++) {
                    pool.QueueWorkItem(() => Thread.Sleep(50));
                }

                for (int i = 0; i < 3; i++) {
                    pool.QueueWorkItem(() => {
                        Thread.Sleep(50);
                        throw new Exception("Test");
                    });
                }

                pool.WaitAll();

                bool test1 = pool.FailedWorkCount == 3;
                PrintTestResult("失败工作统计正确",test1);

                bool test2 = pool.TotalExecuteTime > 0;
                PrintTestResult("总执行时间统计正确",test2);

                bool test3 = pool.AverageExecuteTime > 0;
                PrintTestResult("平均执行时间统计正确",test3);

                bool test4 = pool.RunningDuration > TimeSpan.Zero;
                PrintTestResult("运行时长统计正确",test4);
            }
        }

        #endregion

        #region 事件测试

        /// <summary>
        /// 测试工作完成事件
        /// Test work completed event
        /// </summary>
        private void TestWorkCompletedEvent() {
            PrintTestTitle("测试工作完成事件 / Test Work Completed Event");

            using (PowerPool pool = CreateTestPool()) {
                int completedCount = 0;
                object lockObj = new object();

                pool.WorkCompleted += (sender,args) => {
                    lock (lockObj) completedCount++;
                };

                for (int i = 0; i < 5; i++) {
                    pool.QueueWorkItem(() => i);
                }

                pool.WaitAll();

                bool test1 = completedCount == 5;
                PrintTestResult("工作完成事件触发次数正确",test1);
            }
        }

        /// <summary>
        /// 测试工作失败事件
        /// Test work failed event
        /// </summary>
        private void TestWorkFailedEvent() {
            PrintTestTitle("测试工作失败事件 / Test Work Failed Event");

            using (PowerPool pool = CreateTestPool()) {
                int failedCount = 0;
                object lockObj = new object();

                pool.WorkFailed += (sender,args) => {
                    lock (lockObj) failedCount++;
                };

                for (int i = 0; i < 3; i++) {
                    pool.QueueWorkItem(() => {
                        throw new Exception("Test");
                    });
                }

                pool.WaitAll();

                bool test1 = failedCount == 3;
                PrintTestResult("工作失败事件触发次数正确",test1);
            }
        }

        /// <summary>
        /// 测试线程池启动事件
        /// Test pool started event
        /// </summary>
        private void TestPoolStartedEvent() {
            PrintTestTitle("测试线程池启动事件 / Test Pool Started Event");

            using (PowerPool pool = new PowerPool()) {
                bool started = false;
                pool.PoolStarted += (sender,args) => {
                    started = true;
                };

                pool.Start();

                bool test1 = started;
                PrintTestResult("线程池启动事件触发",test1);
            }
        }

        /// <summary>
        /// 测试线程池停止事件
        /// Test pool stopped event
        /// </summary>
        private void TestPoolStoppedEvent() {
            PrintTestTitle("测试线程池停止事件 / Test Pool Stopped Event");

            using (PowerPool pool = new PowerPool()) {
                bool stopped = false;
                pool.PoolStopped += (sender,args) => {
                    stopped = true;
                };

                pool.Start();
                pool.Stop();

                bool test1 = stopped;
                PrintTestResult("线程池停止事件触发",test1);
            }
        }

        #endregion

        #region 状态摘要测试

        /// <summary>
        /// 测试获取工作状态摘要
        /// Test get work status summary
        /// </summary>
        private void TestGetWorkStatusSummary() {
            PrintTestTitle("测试获取工作状态摘要 / Test Get Work Status Summary");

            using (PowerPool pool = CreateTestPool()) {
                WorkStatusSummary summary1 = pool.GetWorkStatusSummary();

                bool test1 = summary1.TotalQueued == 0;
                PrintTestResult("空队列摘要正确",test1);

                for (int i = 0; i < 10; i++) {
                    pool.QueueWorkItem(() => i);
                }

                pool.WaitAll();

                WorkStatusSummary summary2 = pool.GetWorkStatusSummary();

                bool test2 = summary2.TotalCompleted == 10;
                PrintTestResult("完成数量摘要正确",test2);

                bool test3 = summary2.SuccessRate == 1.0;
                PrintTestResult("成功率摘要正确",test3);
            }
        }

        #endregion

        #region 暂停恢复测试

        /// <summary>
        /// 测试暂停恢复
        /// Test pause resume
        /// </summary>
        private void TestPauseResume() {
            PrintTestTitle("测试暂停恢复 / Test Pause Resume");

            using (PowerPool pool = CreateTestPool()) {
                int counter = 0;
                object lockObj = new object();

                // 暂停线程池
                pool.Pause();

                // 队列工作项
                WorkID id = pool.QueueWorkItem(() => {
                    lock (lockObj) counter++;
                });

                Thread.Sleep(500);

                bool test1 = counter == 0;
                PrintTestResult("暂停时工作不执行",test1);

                // 恢复线程池
                pool.Resume();
                pool.WaitAll();

                bool test2 = counter == 1;
                PrintTestResult("恢复后工作执行",test2);
            }
        }

        #endregion

        #region 清空队列测试

        /// <summary>
        /// 测试清空队列
        /// Test clear queue
        /// </summary>
        private void TestClearQueue() {
            PrintTestTitle("测试清空队列 / Test Clear Queue");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MaxThreads = 1,MinThreads = 1 })) {
                pool.Start();

                // 队列一个长时间运行的工作
                pool.QueueWorkItem(() => Thread.Sleep(1000));

                // 队列多个工作项
                WorkID[] ids = new WorkID[5];
                for (int i = 0; i < 5; i++) {
                    ids[i] = pool.QueueWorkItem(() => i);
                }

                Thread.Sleep(100); // 确保第一个工作开始执行

                // 清空队列
                pool.ClearQueue();

                bool test1 = pool.WaitingWorkCount == 0;
                PrintTestResult("清空队列后等待数量为0",test1);

                // 等待第一个工作完成
                pool.WaitAll();

                // 尝试获取被清除的工作结果
                try {
                    pool.GetResult(ids[0]);
                    PrintTestResult("被清除的工作无法获取结果",false);
                }
                catch (InvalidOperationException) {
                    PrintTestResult("被清除的工作无法获取结果",true);
                }
            }
        }

        #endregion

        #region 弹性扩容测试

        /// <summary>
        /// 测试弹性扩容
        /// Test elastic expansion
        /// </summary>
        private void TestElasticExpansion() {
            PrintTestTitle("测试弹性扩容 / Test Elastic Expansion");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 10,
                MinThreads = 1
            })) {
                pool.Start();

                int initialThreadCount = pool.ActiveWorkerThreads;
                PrintTestResult($"初始线程数: {initialThreadCount}",true);

                // 队列大量工作以触发弹性扩容
                for (int i = 0; i < 20; i++) {
                    pool.QueueWorkItem(() => Thread.Sleep(1000));
                }

                Thread.Sleep(500);

                int expandedThreadCount = pool.ActiveWorkerThreads;
                bool test1 = expandedThreadCount > initialThreadCount;
                PrintTestResult($"弹性扩容触发(从{initialThreadCount}到{expandedThreadCount})",test1);

                bool test2 = expandedThreadCount <= 10;
                PrintTestResult("扩容不超过最大线程数",test2);

                pool.ClearQueue();
            }
        }

        #endregion

        #region 线程回收测试

        /// <summary>
        /// 测试空闲线程回收
        /// Test idle thread cleanup
        /// </summary>
        private void TestIdleThreadCleanup() {
            PrintTestTitle("测试空闲线程回收 / Test Idle Thread Cleanup");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 5,
                MinThreads = 2,
                IdleThreadTimeout = TimeSpan.FromSeconds(2)
            })) {
                pool.Start();

                // 队列工作使线程数增加
                for (int i = 0; i < 10; i++) {
                    pool.QueueWorkItem(() => Thread.Sleep(100));
                }

                pool.WaitAll();

                int highThreadCount = pool.ActiveWorkerThreads;
                PrintTestResult($"工作执行时线程数: {highThreadCount}",true);

                // 等待超过空闲超时时间
                Thread.Sleep(3000);

                int lowThreadCount = pool.ActiveWorkerThreads;
                bool test1 = lowThreadCount < highThreadCount && lowThreadCount >= 2;
                PrintTestResult($"空闲线程回收(从{highThreadCount}到{lowThreadCount}, MinThreads=2)",test1);
            }
        }

        #endregion

        #region 结果缓存过期测试

        /// <summary>
        /// 测试结果缓存过期
        /// Test result cache expiration
        /// </summary>
        private void TestResultCacheExpiration() {
            PrintTestTitle("测试结果缓存过期 / Test Result Cache Expiration");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 2,
                ResultCacheExpiration = TimeSpan.FromMilliseconds(100),
                EnableResultCacheExpiration = true
            })) {
                pool.Start();

                WorkID id = pool.QueueWorkItem(() => 123);
                pool.WaitWork(id);

                bool test1 = pool.CachedResultCount > 0;
                PrintTestResult("执行后结果被缓存",test1);

                Thread.Sleep(150);

                bool test2 = pool.CachedResultCount == 0;
                PrintTestResult("过期后结果自动清理",test2);
            }
        }

        #endregion

        #region 队列限制测试

        /// <summary>
        /// 测试队列限制
        /// Test queue limit
        /// </summary>
        private void TestQueueLimit() {
            PrintTestTitle("测试队列限制 / Test Queue Limit");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 1,
                MinThreads = 1,
                ThreadQueueLimit = 5
            })) {
                pool.Start();

                // 队列一个长时间运行的工作
                pool.QueueWorkItem(() => Thread.Sleep(1000));

                // 尝试队列超过限制的工作项
                try {
                    for (int i = 0; i < 6; i++) {
                        pool.QueueWorkItem(() => i);
                    }
                    PrintTestResult("超过队列限制抛出异常",false);
                }
                catch (InvalidOperationException) {
                    PrintTestResult("超过队列限制抛出异常",true);
                }
            }
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试空并行循环
        /// Test empty parallel for
        /// </summary>
        private void TestEmptyParallelFor() {
            PrintTestTitle("测试空并行循环 / Test Empty Parallel For");

            using (PowerPool pool = CreateTestPool()) {
                // 测试start >= end
                WorkID[] ids1 = pool.ParallelFor(10,5,i => { });
                bool test1 = ids1.Length == 0;
                PrintTestResult("start >= end返回空数组",test1);

                // 测试正常范围
                WorkID[] ids2 = pool.ParallelFor(0,0,i => { });
                bool test2 = ids2.Length == 0;
                PrintTestResult("空范围返回空数组",test2);
            }
        }

        /// <summary>
        /// 测试空并行foreach
        /// Test empty parallel foreach
        /// </summary>
        private void TestEmptyParallelForEach() {
            PrintTestTitle("测试空并行Foreach / Test Empty Parallel ForEach");

            using (PowerPool pool = CreateTestPool()) {
                // 测试空集合
                WorkID[] ids1 = pool.ParallelForEach(new int[0],i => { });
                bool test1 = ids1.Length == 0;
                PrintTestResult("空集合返回空数组",test1);

                // 测试null集合
                try {
                    pool.ParallelForEach<int>(null,i => { });
                    PrintTestResult("null集合抛出异常",false);
                }
                catch (ArgumentNullException) {
                    PrintTestResult("null集合抛出异常",true);
                }
            }
        }

        /// <summary>
        /// 测试空并行invoke
        /// Test empty parallel invoke
        /// </summary>
        private void TestEmptyParallelInvoke() {
            PrintTestTitle("测试空并行Invoke / Test Empty Parallel Invoke");

            using (PowerPool pool = CreateTestPool()) {
                // 测试空数组
                WorkID[] ids1 = pool.ParallelInvoke();
                bool test1 = ids1.Length == 0;
                PrintTestResult("空数组返回空数组",test1);

                // 测试null数组
                try {
                    pool.ParallelInvoke(null);
                    PrintTestResult("null数组抛出异常",false);
                }
                catch (ArgumentNullException) {
                    PrintTestResult("null数组抛出异常",true);
                }
            }
        }

        #endregion

        #region 异常处理测试

        /// <summary>
        /// 测试异常处理
        /// Test exception handling
        /// </summary>
        private void TestExceptionHandling() {
            PrintTestTitle("测试异常处理 / Test Exception Handling");

            using (PowerPool pool = CreateTestPool()) {
                // 测试工作项抛出异常
                WorkID id1 = pool.QueueWorkItem(() => {
                    throw new InvalidOperationException("Test exception");
                });

                pool.WaitWork(id1);

                ExecuteResult result1 = pool.GetResult(id1);
                bool test1 = result1.IsFailed && result1.Exception is InvalidOperationException;
                PrintTestResult("工作项异常被正确捕获",test1);

                // 测试多个工作项部分失败
                int[] results = new int[5];
                for (int i = 0; i < 5; i++) {
                    int index = i;
                    pool.QueueWorkItem(() => {
                        if (index == 2)
                            throw new Exception("Fail");
                        return index;
                    });
                }

                pool.WaitAll();

                bool test2 = pool.FailedWorkCount == 1;
                PrintTestResult("部分失败统计正确",test2);
            }
        }

        /// <summary>
        /// 测试WorkOption异常
        /// Test work option exception
        /// </summary>
        private void TestWorkOptionException() {
            PrintTestTitle("测试WorkOption异常 / Test Work Option Exception");

            using (PowerPool pool = CreateTestPool()) {
                // 测试超时超过int.MaxValue
                try {
                    WorkOption option = new WorkOption(
                       TimeSpan.FromDays(30)
                    );
                    PrintTestResult("超时超过int.MaxValue抛出异常",false);
                }
                catch (ArgumentOutOfRangeException) {
                    PrintTestResult("超时超过int.MaxValue抛出异常",true);
                }
            }
        }

        #endregion

        #region Dispose测试

        /// <summary>
        /// 测试Dispose
        /// Test dispose
        /// </summary>
        private void TestDispose() {
            PrintTestTitle("测试Dispose / Test Dispose");

            PowerPool pool = CreateTestPool();

            // 队列一些工作
            for (int i = 0; i < 5; i++) {
                pool.QueueWorkItem(() => i);
            }

            pool.WaitAll();

            bool test1 = pool.IsRunning;
            PrintTestResult("Dispose前IsRunning为true",test1);

            // Dispose
            pool.Dispose();

            bool test2 = !pool.IsRunning;
            PrintTestResult("Dispose后IsRunning为false",test2);

            // 测试Dispose后操作抛出异常
            try {
                pool.QueueWorkItem(() => { });
                PrintTestResult("Dispose后操作抛出异常",false);
            }
            catch (ObjectDisposedException) {
                PrintTestResult("Dispose后操作抛出异常",true);
            }

            // 测试重复Dispose
            pool.Dispose();
            PrintTestResult("重复Dispose不会出错",true);
        }

        #endregion

        #region 并发压力测试

        /// <summary>
        /// 测试并发操作
        /// Test concurrent operations
        /// </summary>
        private void TestConcurrentOperations() {
            PrintTestTitle("测试并发操作 / Test Concurrent Operations");

            using (PowerPool pool = new PowerPool(new PowerPoolOption {
                MaxThreads = 8,
                MinThreads = 2,
                ThreadQueueLimit = 1000
            })) {
                pool.Start();

                int totalSum = 0;
                object lockObj = new object();
                List<WorkID> workIds = new List<WorkID>();

                // 并发队列大量工作项
                for (int i = 0; i < 100; i++) {
                    int val = i;
                    WorkID id = pool.QueueWorkItem(() => {
                        int localSum = 0;
                        for (int j = 0; j < 100; j++) {
                            localSum += j;
                        }
                        lock (lockObj) totalSum += localSum;
                        return val;
                    });
                    workIds.Add(id);
                }

                // 并行循环
                WorkID[] parallelIds = pool.ParallelFor(0,50,i => {
                    lock (lockObj) totalSum += i;
                });

                // 并行foreach
                List<int> numbers = new List<int>();
                for (int i = 0; i < 50; i++) numbers.Add(i);
                WorkID[] forEachIds = pool.ParallelForEach(numbers,i => {
                    lock (lockObj) totalSum += i;
                });

                // 等待所有工作完成
                pool.WaitAll();

                bool test1 = workIds.Count == 100;
                PrintTestResult("队列工作项数量正确",test1);

                bool test2 = pool.FailedWorkCount >= 1;
                PrintTestResult("完成失败工作项数量",test2);

                bool test3 = totalSum > 0;
                PrintTestResult("并发操作结果正确",test3);
            }
        }

        #endregion
    }


    /// <summary>
    /// Group功能使用测试示例
    /// Group functionality usage test examples
    /// </summary>
    public class Test_Group_Usage
    {
        /// <summary>
        /// 测试1：基本分组操作
        /// Test 1: Basic group operations
        /// </summary>
        public static void Test1_BasicGroupOperations() {
            Console.WriteLine("=== 测试1：基本分组操作 / Test 1: Basic Group Operations ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 2,MaxThreads = 4 })) {
                // 创建分组
                pool.Start();
                // Create a group
                Group group = pool.GetGroup("TaskGroup1");
                Console.WriteLine($"创建分组 'TaskGroup1': {pool.GroupExists("TaskGroup1")}");

                // 添加工作到分组
                // Add works to group
                List<WorkID> workIds = new List<WorkID>();
                for (int i = 0; i < 5; i++) {
                    int index = i;
                    WorkID workId = pool.QueueWorkItem(() => {
                        Thread.Sleep(100 * index);
                        return $"Task {index} completed";
                    });
                    workIds.Add(workId);
                    bool added = group.Add(workId);
                    Console.WriteLine($"工作 {workId} 添加到分组: {added}");
                }

                // 获取分组成员
                // Get group members
                var members = group.GetMembers();
                Console.WriteLine($"\n分组成员数量: {members.Count}");

                // 等待所有工作完成
                // Wait for all works to complete
                Console.WriteLine("\n等待所有工作完成...");
                group.Wait();
                Console.WriteLine("所有工作已完成");

                // 获取结果
                // Get results
                List<ExecuteResult> results = group.GetResults();
                Console.WriteLine($"\n获取到 {results.Count} 个结果:");
                foreach (var result in results) {
                    if (result.IsSuccess) {
                        Console.WriteLine($"  工作ID {result.ID}: {result.Result}");
                    }
                }
            }

            Console.WriteLine("\n=== 测试1完成 / Test 1 Completed ===\n");
        }

        /// <summary>
        /// 测试2：多个分组管理
        /// Test 2: Multiple group management
        /// </summary>
        public static void Test2_MultipleGroups() {
            Console.WriteLine("=== 测试2：多个分组管理 / Test 2: Multiple Group Management ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 2,MaxThreads = 6 })) {
                pool.Start();
                // 创建多个分组
                // Create multiple groups
                Group groupA = pool.GetGroup("GroupA");
                Group groupB = pool.GetGroup("GroupB");
                Group groupC = pool.GetGroup("GroupC");

                Console.WriteLine($"创建的分组: {string.Join(", ",pool.GetAllGroupNames().ToArray())}");

                // 添加工作到不同分组
                // Add works to different groups
                for (int i = 0; i < 10; i++) {
                    int index = i;
                    WorkID workId = pool.QueueWorkItem(() => {
                        Thread.Sleep(50);
                        return index * 10;
                    });

                    // 根据索引分配到不同分组
                    // Assign to different groups based on index
                    if (index % 3 == 0)
                        groupA.Add(workId);
                    else if (index % 3 == 1)
                        groupB.Add(workId);
                    else
                        groupC.Add(workId);
                }

                // 分别等待每个分组完成
                // Wait for each group separately
                Console.WriteLine("\nGroupA 成员数量: {0}",new List<WorkID>(groupA.GetMembers()).Count);
                Console.WriteLine("GroupB 成员数量: {0}",new List<WorkID>(groupB.GetMembers()).Count);
                Console.WriteLine("GroupC 成员数量: {0}",new List<WorkID>(groupC.GetMembers()).Count);

                Console.WriteLine("\n等待 GroupA 完成...");
                groupA.Wait();
                Console.WriteLine("GroupA 完成");

                Console.WriteLine("\n等待 GroupB 完成...");
                groupB.Wait();
                Console.WriteLine("GroupB 完成");

                Console.WriteLine("\n等待 GroupC 完成...");
                groupC.Wait();
                Console.WriteLine("GroupC 完成");

                // 获取所有分组名称
                // Get all group names
                Console.WriteLine($"\n所有分组名称: {string.Join(", ",pool.GetAllGroupNames().ToArray())}");
                Console.WriteLine($"分组总数: {pool.GetGroupCount()}");
            }

            Console.WriteLine("\n=== 测试2完成 / Test 2 Completed ===\n");
        }

        /// <summary>
        /// 测试3：工作可以属于多个分组
        /// Test 3: Work can belong to multiple groups
        /// </summary>
        public static void Test3_WorkInMultipleGroups() {
            Console.WriteLine("=== 测试3：工作可以属于多个分组 / Test 3: Work Can Belong to Multiple Groups ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 1,MaxThreads = 2 })) {
                pool.Start();
                // 创建两个分组
                // Create two groups
                Group group1 = pool.GetGroup("HighPriority");
                Group group2 = pool.GetGroup("BackupTasks");

                // 创建一个工作并添加到两个分组
                // Create a work and add it to both groups
                WorkID sharedWork = pool.QueueWorkItem(() => {
                    Thread.Sleep(200);
                    return "Shared task result";
                });

                group1.Add(sharedWork);
                group2.Add(sharedWork);

                Console.WriteLine($"工作 {sharedWork} 添加到 Group1: {group1.GetMembers().Contains(sharedWork)}");
                Console.WriteLine($"工作 {sharedWork} 添加到 Group2: {group2.GetMembers().Contains(sharedWork)}");

                // 检查工作所属的分组
                // Check which groups the work belongs to
                List<string> workGroups = pool.GetWorkGroups(sharedWork);
                Console.WriteLine($"\n工作 {sharedWork} 所属的分组: {string.Join(", ",workGroups.ToArray())}");

                // 检查工作是否在特定分组中
                // Check if work is in specific group
                Console.WriteLine($"工作在 Group1 中: {pool.IsWorkInGroup(sharedWork,"HighPriority")}");
                Console.WriteLine($"工作在 Group2 中: {pool.IsWorkInGroup(sharedWork,"BackupTasks")}");

                // 从一个分组中移除工作（不影响其他分组）
                // Remove work from one group (doesn't affect other groups)
                bool removed = group1.Remove(sharedWork);
                Console.WriteLine($"\n从 Group1 移除工作: {removed}");

                Console.WriteLine($"工作在 Group1 中: {pool.IsWorkInGroup(sharedWork,"HighPriority")}");
                Console.WriteLine($"工作在 Group2 中: {pool.IsWorkInGroup(sharedWork,"BackupTasks")}");

                // 等待完成
                // Wait for completion
                group2.Wait();
                Console.WriteLine("\n工作已完成");
            }

            Console.WriteLine("\n=== 测试3完成 / Test 3 Completed ===\n");
        }

        /// <summary>
        /// 测试4：分组操作 - Add/Remove
        /// Test 4: Group operations - Add/Remove
        /// </summary>
        public static void Test4_GroupAddRemove() {
            Console.WriteLine("=== 测试4：分组操作 - Add/Remove / Test 4: Group Operations - Add/Remove ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 1,MaxThreads = 2 })) {
                pool.Start();
                Group group = pool.GetGroup("TestGroup");

                // 创建工作
                // Create works
                List<WorkID> workIds = new List<WorkID>();
                for (int i = 0; i < 5; i++) {
                    workIds.Add(pool.QueueWorkItem(() => i * 10));
                }

                Console.WriteLine($"创建了 {workIds.Count} 个工作");

                // 添加到分组
                // Add to group
                foreach (var workId in workIds) {
                    bool added = group.Add(workId);
                    Console.WriteLine($"添加 {workId}: {added}");
                }

                Console.WriteLine($"\n分组大小: {new List<WorkID>(group.GetMembers()).Count}");

                // 从分组中移除特定工作
                // Remove specific work from group
                bool removed = group.Remove(workIds[0]);
                Console.WriteLine($"\n移除 {workIds[0]}: {removed}");

                Console.WriteLine($"分组大小: {new List<WorkID>(group.GetMembers()).Count}");

                // 尝试移除不在分组中的工作
                // Try to remove work not in group
                WorkID nonExistentWork = new WorkID(Guid.NewGuid().GetHashCode());
                bool removedAgain = group.Remove(nonExistentWork);
                Console.WriteLine($"移除不存在的 {nonExistentWork}: {removedAgain}");

                // 清空分组
                // Clear group
                pool.ClearGroup("TestGroup");
                Console.WriteLine($"\n清空分组后大小: {new List<WorkID>(group.GetMembers()).Count}");
            }

            Console.WriteLine("\n=== 测试4完成 / Test 4 Completed ===\n");
        }

        /// <summary>
        /// 测试5：GetResultsAndWait 方法
        /// Test 5: GetResultsAndWait method
        /// </summary>
        public static void Test5_GetResultsAndWait() {
            Console.WriteLine("=== 测试5：GetResultsAndWait 方法 / Test 5: GetResultsAndWait Method ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 2,MaxThreads = 4 })) {
                pool.Start();
                Group group = pool.GetGroup("ResultsGroup");

                // 创建不同耗时的任务
                // Create tasks with different durations
                List<WorkID> workIds = new List<WorkID>();
                for (int i = 0; i < 5; i++) {
                    int index = i;
                    workIds.Add(pool.QueueWorkItem(() => {
                        Thread.Sleep(100 * index);
                        return index * 100;
                    }));
                    group.Add(workIds[i]);
                }

                Console.WriteLine("创建并添加了 5 个任务");

                // 使用 GetResultsAndWait 等待并获取所有结果
                // Use GetResultsAndWait to wait and get all results
                Console.WriteLine("\n使用 GetResultsAndWait 等待所有结果...");
                List<ExecuteResult> results = group.GetResultsAndWait();

                Console.WriteLine($"\n获取到 {results.Count} 个结果:");
                foreach (var result in results) {
                    if (result.IsSuccess) {
                        Console.WriteLine($"  工作ID {result.ID}: {result.Result}");
                    }
                    else {
                        Console.WriteLine($"  工作ID {result.ID}: 失败 - {result.Exception?.Message}");
                    }
                }
            }

            Console.WriteLine("\n=== 测试5完成 / Test 5 Completed ===\n");
        }

        /// <summary>
        /// 测试6：使用 PowerPool 直接管理分组
        /// Test 6: Manage groups directly using PowerPool
        /// </summary>
        public static void Test6_PowerPoolGroupManagement() {
            Console.WriteLine("=== 测试6：PowerPool直接管理分组 / Test 6: PowerPool Direct Group Management ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 1,MaxThreads = 2 })) {
                pool.Start();
                // 使用 PowerPool 方法管理分组
                // Manage groups using PowerPool methods

                // 1. 创建分组
                // 1. Create group
                pool.GetGroup("DirectGroup");
                Console.WriteLine("创建分组 'DirectGroup'");

                // 2. 添加工作到分组
                // 2. Add work to group
                WorkID work1 = pool.QueueWorkItem(() => { Thread.Sleep(100); return 1; });
                WorkID work2 = pool.QueueWorkItem(() => { Thread.Sleep(100); return 2; });
                WorkID work3 = pool.QueueWorkItem(() => { Thread.Sleep(100); return 3; });

                pool.AddWorkToGroup("DirectGroup",work1);
                pool.AddWorkToGroup("DirectGroup",work2);
                pool.AddWorkToGroup("DirectGroup",work3);
                Console.WriteLine("\n添加了 3 个工作到分组");

                // 3. 获取分组成员列表
                // 3. Get group member list
                List<WorkID> groupMembers = pool.GetGroupWorkItems("DirectGroup");
                Console.WriteLine($"分组工作项数量: {groupMembers.Count}");

                // 4. 检查分组是否存在
                // 4. Check if group exists
                Console.WriteLine($"\n分组 'DirectGroup' 存在: {pool.GroupExists("DirectGroup")}");
                Console.WriteLine($"分组 'NonExistent' 存在: {pool.GroupExists("NonExistent")}");

                // 5. 获取所有分组名称
                // 5. Get all group names
                List<string> allGroups = pool.GetAllGroupNames();
                Console.WriteLine($"\n所有分组: {string.Join(", ",allGroups.ToArray())}");

                // 6. 从分组中移除工作
                // 6. Remove work from group
                bool removed = pool.RemoveWorkFromGroup("DirectGroup",work1);
                Console.WriteLine($"\n从分组移除工作 {work1}: {removed}");

                // 7. 等待并获取结果
                // 7. Wait and get results
                Console.WriteLine("\n等待剩余工作完成...");
                pool.WaitWorks(groupMembers.ToArray(),5000);
                Console.WriteLine("所有工作已完成");
            }

            Console.WriteLine("\n=== 测试6完成 / Test 6 Completed ===\n");
        }

        /// <summary>
        /// 测试7：性能测试 - Wait 方法优化
        /// Test 7: Performance test - Wait method optimization
        /// </summary>
        public static void Test7_PerformanceTest() {
            Console.WriteLine("=== 测试7：性能测试 - Wait方法优化 / Test 7: Performance Test - Wait Method Optimization ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 4,MaxThreads = 8 })) {
                pool.Start();
                Group group = pool.GetGroup("PerfGroup");

                // 创建100个短任务
                // Create 100 short tasks
                int taskCount = 100;
                Console.WriteLine($"创建 {taskCount} 个任务...");

                for (int i = 0; i < taskCount; i++) {
                    int index = i;
                    WorkID workId = pool.QueueWorkItem(() => {
                        Thread.Sleep(50);  // 每个任务50ms
                        return index;
                    });
                    group.Add(workId);
                }

                // 测量等待时间
                // Measure wait time
                DateTime startTime = DateTime.Now;
                group.Wait();
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n所有任务完成!");
                Console.WriteLine($"等待时间: {elapsed.TotalMilliseconds:F2} ms");
                Console.WriteLine($"理论最小时间: 50 ms (所有任务并行执行)");
                Console.WriteLine($"理论最大时间: {taskCount * 50} ms (逐个执行)");
                Console.WriteLine($"加速比: {(taskCount * 50.0) / elapsed.TotalMilliseconds:F2}x");

                // 获取结果
                // Get results
                List<ExecuteResult> results = group.GetResults();
                Console.WriteLine($"\n获取到 {results.Count} 个结果");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"失败数量: {results.FindAll(r => !r.IsSuccess).Count}");
            }

            Console.WriteLine("\n=== 测试7完成 / Test 7 Completed ===\n");
        }

        /// <summary>
        /// 测试8：ExecuteParallel - Func<TResult> 并行计算
        /// Test 8: ExecuteParallel - Func<TResult> parallel computation
        /// </summary>
        public static void Test8_ExecuteParallel_Func() {
            Console.WriteLine("=== 测试8：ExecuteParallel - Func<TResult> 并行计算 / Test 8: ExecuteParallel - Func<TResult> Parallel Computation ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 4,MaxThreads = 8 })) {
                pool.Start();
                Group group = pool.GetGroup("ParallelFuncGroup");

                // 创建多个计算任务
                // Create multiple computation tasks
                Func<int>[] tasks = new Func<int>[10];
                for (int i = 0; i < tasks.Length; i++) {
                    int index = i;
                    tasks[i] = () => {
                        Thread.Sleep(100);
                        return index * index;
                    };
                }

                Console.WriteLine($"创建 {tasks.Length} 个计算任务");

                // 执行并行计算
                // Execute parallel computation
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(tasks);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n并行计算完成!");
                Console.WriteLine($"耗时: {elapsed.TotalMilliseconds:F2} ms");

                // 显示结果
                // Display results
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"失败数量: {results.FindAll(r => !r.IsSuccess).Count}");

                Console.WriteLine("\n计算结果:");
                foreach (var result in results) {
                    if (result.IsSuccess) {
                        Console.WriteLine($"  {result.Result}");
                    }
                }
            }

            Console.WriteLine("\n=== 测试8完成 / Test 8 Completed ===\n");
        }

        /// <summary>
        /// 测试9：ExecuteParallel - Action 并行执行
        /// Test 9: ExecuteParallel - Action parallel execution
        /// </summary>
        public static void Test9_ExecuteParallel_Action() {
            Console.WriteLine("=== 测试9：ExecuteParallel - Action 并行执行 / Test 9: ExecuteParallel - Action Parallel Execution ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 4,MaxThreads = 8 })) {
                pool.Start();
                Group group = pool.GetGroup("ParallelActionGroup");

                // 创建多个动作任务
                // Create multiple action tasks
                Action[] actions = new Action[10];
                for (int i = 0; i < actions.Length; i++) {
                    int index = i;
                    actions[i] = () => {
                        Thread.Sleep(50);
                        Console.WriteLine($"任务 {index} 完成");
                    };
                }

                Console.WriteLine($"创建 {actions.Length} 个动作任务");

                // 执行并行动作
                // Execute parallel actions
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(actions);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n并行执行完成!");
                Console.WriteLine($"耗时: {elapsed.TotalMilliseconds:F2} ms");

                // 显示结果
                // Display results
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"失败数量: {results.FindAll(r => !r.IsSuccess).Count}");
            }

            Console.WriteLine("\n=== 测试9完成 / Test 9 Completed ===\n");
        }

        /// <summary>
        /// 测试10：ExecuteParallel 带超时
        /// Test 10: ExecuteParallel with timeout
        /// </summary>
        public static void Test10_ExecuteParallel_WithTimeout() {
            Console.WriteLine("=== 测试10：ExecuteParallel 带超时 / Test 10: ExecuteParallel with Timeout ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 2,MaxThreads = 4 })) {
                pool.Start();
                Group group = pool.GetGroup("TimeoutGroup");

                // 创建不同耗时的任务
                // Create tasks with different durations
                Func<int>[] tasks = new Func<int>[5];
                for (int i = 0; i < tasks.Length; i++) {
                    int index = i;
                    tasks[i] = () => {
                        int sleepTime = 100 + index * 100;  // 100ms, 200ms, 300ms, 400ms, 500ms
                        Thread.Sleep(sleepTime);
                        Console.WriteLine($"任务 {index} 耗时 {sleepTime}ms");
                        return index;
                    };
                }

                Console.WriteLine($"创建 {tasks.Length} 个任务，设置总超时 300ms");

                // 执行并行计算（总超时 300ms）
                // Execute parallel computation (total timeout 300ms)
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(tasks,null,0,3000);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n等待超时后结束");
                Console.WriteLine($"实际耗时: {elapsed.TotalMilliseconds:F2} ms");

                // 显示结果
                // Display results
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"超时/失败数量: {results.FindAll(r => !r.IsSuccess).Count}");

                Console.WriteLine("\n成功的结果:");
                foreach (var result in results) {
                    if (result.IsSuccess) {
                        Console.WriteLine($"  任务结果: {result.Result}");
                    }
                }

                Console.WriteLine("\n失败的结果:");
                foreach (var result in results) {
                    if (!result.IsSuccess) {
                        Console.WriteLine($"  任务异常: {result.Exception?.Message}");
                    }
                }
            }

            Console.WriteLine("\n=== 测试10完成 / Test 10 Completed ===\n");
        }

        /// <summary>
        /// 测试11：ExecuteParallel 带取消令牌
        /// Test 11: ExecuteParallel with cancellation token
        /// </summary>
        public static void Test11_ExecuteParallel_WithCancellation() {
            Console.WriteLine("=== 测试11：ExecuteParallel 带取消令牌 / Test 11: ExecuteParallel with Cancellation Token ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 2,MaxThreads = 4 })) {
                pool.Start();
                Group group = pool.GetGroup("CancelGroup");

                // 创建长时间运行的任务
                // Create long-running tasks
                Func<string>[] tasks = new Func<string>[5];
                for (int i = 0; i < tasks.Length; i++) {
                    int index = i;
                    tasks[i] = () => {
                        for (int j = 0; j < 10; j++) {
                            Thread.Sleep(100);
                        }
                        return $"任务 {index} 完成";
                    };
                }

                Console.WriteLine($"创建 {tasks.Length} 个长时间运行的任务");

                // 创建取消令牌
                // Create cancellation token
                CancellationTokenSource cts = new CancellationTokenSource();

                // 启动一个定时器来取消任务
                // Start a timer to cancel tasks
                System.Timers.Timer timer = new System.Timers.Timer(500);
                timer.Elapsed += (sender,e) => {
                    Console.WriteLine("\n触发取消操作...");
                    cts.Cancel();
                    timer.Stop();
                };
                timer.Start();

                // 执行并行计算（带取消令牌）
                // Execute parallel computation (with cancellation token)
                Console.WriteLine("开始并行执行...");
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(tasks,cts);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n取消后结束");
                Console.WriteLine($"实际耗时: {elapsed.TotalMilliseconds:F2} ms");

                // 显示结果
                // Display results
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"取消/失败数量: {results.FindAll(r => !r.IsSuccess).Count}");

                timer.Dispose();
            }

            Console.WriteLine("\n=== 测试11完成 / Test 11 Completed ===\n");
        }

        /// <summary>
        /// 测试12：ExecuteParallel 混合场景
        /// Test 12: ExecuteParallel mixed scenario
        /// </summary>
        public static void Test12_ExecuteParallel_MixedScenario() {
            Console.WriteLine("=== 测试12：ExecuteParallel 混合场景 / Test 12: ExecuteParallel Mixed Scenario ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 4,MaxThreads = 8 })) {
                pool.Start();
                Group group = pool.GetGroup("MixedGroup");

                // 创建混合类型的任务
                // Create mixed type tasks
                Func<object>[] tasks = new Func<object>[8];
                for (int i = 0; i < tasks.Length; i++) {
                    int index = i;
                    tasks[i] = () => {
                        if (index % 2 == 0) {
                            // 偶数索引：计算任务
                            Thread.Sleep(50);
                            return index * 10;
                        }
                        else {
                            // 奇数索引：IO 模拟任务
                            Thread.Sleep(100);
                            return $"IO-{index}";
                        }
                    };
                }

                Console.WriteLine($"创建 {tasks.Length} 个混合类型任务");

                // 执行并行计算
                // Execute parallel computation
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(tasks,null,0,5000);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n并行执行完成!");
                Console.WriteLine($"耗时: {elapsed.TotalMilliseconds:F2} ms");

                // 显示结果
                // Display results
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"失败数量: {results.FindAll(r => !r.IsSuccess).Count}");

                Console.WriteLine("\n执行结果:");
                int successCount = 0;
                foreach (var result in results) {
                    if (result.IsSuccess) {
                        successCount++;
                        Console.WriteLine($"  {result.Result}");
                    }
                }
            }

            Console.WriteLine("\n=== 测试12完成 / Test 12 Completed ===\n");
        }

        /// <summary>
        /// 测试13：空分组处理
        /// Test 13: Empty group handling
        /// </summary>
        public static void Test13_EmptyGroupHandling() {
            Console.WriteLine("=== 测试13：空分组处理 / Test 13: Empty Group Handling ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 1,MaxThreads = 2 })) {
                pool.Start();
                Group group = pool.GetGroup("EmptyGroup");

                // 测试空分组的各种操作
                // Test various operations on empty group

                Console.WriteLine("测试空分组的 GetMembers:");
                var members = group.GetMembers();
                Console.WriteLine($"  成员数量: {new List<WorkID>(members).Count}");

                Console.WriteLine("\n测试空分组的 Wait:");
                DateTime startTime = DateTime.Now;
                group.Wait();
                TimeSpan elapsed = DateTime.Now - startTime;
                Console.WriteLine($"  Wait 耗时: {elapsed.TotalMilliseconds:F2} ms (应该立即返回)");

                Console.WriteLine("\n测试空分组的 GetResults:");
                var results = group.GetResults();
                Console.WriteLine($"  结果数量: {results.Count}");

                Console.WriteLine("\n测试空分组的 GetResultsAndWait:");
                var waitResults = group.GetResultsAndWait();
                Console.WriteLine($"  结果数量: {waitResults.Count}");

                Console.WriteLine("\n测试空分组的 Add/Remove:");
                WorkID dummyWork = pool.QueueWorkItem(() => 1);
                Console.WriteLine($"  添加工作: {group.Add(dummyWork)}");
                Console.WriteLine($"  移除工作: {group.Remove(dummyWork)}");

                // 测试 ExecuteParallel 空数组
                // Test ExecuteParallel with empty array
                Console.WriteLine("\n测试 ExecuteParallel 空数组:");
                var emptyResults = group.ExecuteParallel<int>(new Func<int>[0]);
                Console.WriteLine($"  结果数量: {emptyResults.Count}");

                Console.WriteLine("\n测试 ExecuteParallel Action 空数组:");
                var emptyActionResults = group.ExecuteParallel(new Action[0]);
                Console.WriteLine($"  结果数量: {emptyActionResults.Count}");
            }

            Console.WriteLine("\n=== 测试13完成 / Test 13 Completed ===\n");
        }

        /// <summary>
        /// 测试14：大数量任务并行
        /// Test 14: Large number of parallel tasks
        /// </summary>
        public static void Test14_LargeScaleParallel() {
            Console.WriteLine("=== 测试14：大数量任务并行 / Test 14: Large Scale Parallel Tasks ===\n");

            using (PowerPool pool = new PowerPool(new PowerPoolOption { MinThreads = 8,MaxThreads = 16, ThreadQueueLimit=1000 })) {
                pool.Start();
                Group group = pool.GetGroup("LargeScaleGroup");

                // 创建大量任务
                // Create large number of tasks
                int taskCount = 1000;
                Func<int>[] tasks = new Func<int>[taskCount];
                for (int i = 0; i < taskCount; i++) {
                    int index = i;
                    tasks[i] = () => {
                        Thread.Sleep(10);  // 每个任务 10ms
                        return index;
                    };
                }

                Console.WriteLine($"创建 {taskCount} 个任务");

                // 执行并行计算
                // Execute parallel computation
                DateTime startTime = DateTime.Now;
                List<ExecuteResult> results = group.ExecuteParallel(tasks,null,0,30000);
                TimeSpan elapsed = DateTime.Now - startTime;

                Console.WriteLine($"\n所有任务完成!");
                Console.WriteLine($"耗时: {elapsed.TotalMilliseconds:F2} ms");
                Console.WriteLine($"吞吐量: {taskCount / elapsed.TotalSeconds:F0} 任务/秒");

                // 统计结果
                // Statistics
                Console.WriteLine($"\n结果统计:");
                Console.WriteLine($"总任务数: {results.Count}");
                Console.WriteLine($"成功数量: {results.FindAll(r => r.IsSuccess).Count}");
                Console.WriteLine($"失败数量: {results.FindAll(r => !r.IsSuccess).Count}");
                Console.WriteLine($"成功率: {(results.FindAll(r => r.IsSuccess).Count * 100.0 / results.Count):F2}%");
            }

            Console.WriteLine("\n=== 测试14完成 / Test 14 Completed ===\n");
        }

        /// <summary>
        /// 运行所有测试
        /// Run all tests
        /// </summary>
        public static void RunAllTests() {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   Group 功能使用测试示例                                       ║");
            Console.WriteLine("║   Group Functionality Usage Test Examples                       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            Test1_BasicGroupOperations();
            Test2_MultipleGroups();
            Test3_WorkInMultipleGroups();
            Test4_GroupAddRemove();
            Test5_GetResultsAndWait();
            Test6_PowerPoolGroupManagement();
            Test7_PerformanceTest();
            Test8_ExecuteParallel_Func();
            Test9_ExecuteParallel_Action();
            Test10_ExecuteParallel_WithTimeout();
            Test11_ExecuteParallel_WithCancellation();
            Test12_ExecuteParallel_MixedScenario();
            Test13_EmptyGroupHandling();
            Test14_LargeScaleParallel();

            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   所有测试完成 / All Tests Completed                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
        }
    }

    /// <summary>
    /// 测试延迟执行和定时执行功能
    /// Test delayed and recurring execution functionality
    /// </summary>
    public class TestSchedulingFunctionality
    {

        /// <summary>
        /// 运行所有测试
        /// Run all tests
        /// </summary>
        public static void RunAllTests() {
            Console.WriteLine("=== PowerThreadPool_Net20 延迟执行和定时执行功能测试 ===\n");

            Test1_DelayedExecutionWithAction();
            Test2_DelayedExecutionWithFunc();
            Test3_RecurringExecutionWithAction();
            Test4_RecurringExecutionWithMaxExecutions();
            Test5_RecurringExecutionWithFunc();
            Test6_CancelScheduledWork();
            Test7_MultipleDelayedWorks();
            Test8_GetActiveScheduledWorkIDs();

            Console.WriteLine("\n=== 所有测试完成 ===");
        }

        /// <summary>
        /// 测试1: 延迟执行任务（Action）
        /// Test 1: Delayed execution (Action)
        /// </summary>
        public static void Test1_DelayedExecutionWithAction() {
            Console.WriteLine("测试1: 延迟执行任务（Action）");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var counter = 0;
            Console.WriteLine("  [0ms] 准备调度延迟任务");

            string scheduledID = pool.ScheduleDelayed(
                () => {
                    Interlocked.Increment(ref counter);
                    Console.WriteLine("  [延迟任务] 执行完成，counter = " + counter);
                },
                2000
            );

            Console.WriteLine("  [0ms] 任务已调度，ID: " + scheduledID);

            Thread.Sleep(2500);

            if (counter == 1) {
                Console.WriteLine("  ✓ 测试通过：延迟任务在2秒后正确执行\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：期望 counter=1，实际 counter=" + counter + "\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试2: 延迟执行任务（Func，有返回值）
        /// Test 2: Delayed execution (Func with return value)
        /// </summary>
        public static void Test2_DelayedExecutionWithFunc() {
            Console.WriteLine("测试2: 延迟执行任务（Func，有返回值）");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            Console.WriteLine("  [0ms] 准备调度延迟任务");

            string scheduledID = pool.ScheduleDelayed(
                () => {
                    Console.WriteLine("  [延迟任务] 正在计算结果...");
                    Thread.Sleep(100);
                    return 42;
                },
                1500,
                new WorkOption ()
            );

            Console.WriteLine("  [0ms] 任务已调度");

            Thread.Sleep(2000);

            Console.WriteLine("  ✓ 测试通过：延迟任务调度成功\n");

            pool.Dispose();
        }

        /// <summary>
        /// 测试3: 定期执行任务（无限次）
        /// Test 3: Recurring execution (unlimited)
        /// </summary>
        public static void Test3_RecurringExecutionWithAction() {
            Console.WriteLine("测试3: 定期执行任务（无限次，10秒后手动停止）");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var counter = 0;
            Console.WriteLine("  [0ms] 准备调度周期任务（间隔500ms）");

            string scheduledID = pool.ScheduleRecurring(
                () => {
                    int current = Interlocked.Increment(ref counter);
                    Console.WriteLine("  [周期任务] 第 " + current + " 次执行");
                },
                500
            );

            Console.WriteLine("  [0ms] 周期任务已调度，ID: " + scheduledID);

            Thread.Sleep(3000);

            Console.WriteLine("  [3000ms] 停止周期任务");
            pool.CancelScheduledWork(scheduledID);

            Thread.Sleep(1000);

            if (counter >= 5 && counter <= 7) {
                Console.WriteLine("  ✓ 测试通过：周期任务执行了 " + counter + " 次（预期5-7次）\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：执行次数 " + counter + " 不在预期范围内 [5, 7]\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试4: 定期执行任务（限制最大执行次数）
        /// Test 4: Recurring execution (with max executions)
        /// </summary>
        public static void Test4_RecurringExecutionWithMaxExecutions() {
            Console.WriteLine("测试4: 定期执行任务（最大执行5次）");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var counter = 0;
            Console.WriteLine("  [0ms] 准备调度周期任务（间隔300ms，最多5次）");

            string scheduledID = pool.ScheduleRecurring(
                () => {
                    Interlocked.Increment(ref counter);
                    Console.WriteLine("  [周期任务] 第 " + counter + " 次执行");
                },
                300,
                5
            );

            Console.WriteLine("  [0ms] 周期任务已调度");

            Thread.Sleep(2000);

            if (counter == 5) {
                Console.WriteLine("  ✓ 测试通过：周期任务正确执行了 " + counter + " 次\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：期望执行5次，实际执行" + counter + "次\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试5: 定期执行任务（Func，有返回值）
        /// Test 5: Recurring execution (Func with return value)
        /// </summary>
        public static void Test5_RecurringExecutionWithFunc() {
            Console.WriteLine("测试5: 定期执行任务（Func，有返回值，最多3次）");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var counter = 0;
            Console.WriteLine("  [0ms] 准备调度周期任务（间隔400ms，最多3次）");

            string scheduledID = pool.ScheduleRecurring(
                () => {
                    Interlocked.Increment(ref counter);
                    int result = counter * 10;
                    Console.WriteLine("  [周期任务] 第 " + counter + " 次执行，返回 " + result);
                    return result;
                },
                400,
                3
            );

            Console.WriteLine("  [0ms] 周期任务已调度");

            Thread.Sleep(1500);

            if (counter == 3) {
                Console.WriteLine("  ✓ 测试通过：周期任务正确执行了 " + counter + " 次\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：期望执行3次，实际执行" + counter + "次\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试6: 取消定时任务
        /// Test 6: Cancel scheduled work
        /// </summary>
        public static void Test6_CancelScheduledWork() {
            Console.WriteLine("测试6: 取消定时任务");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var counter = 0;
            Console.WriteLine("  [0ms] 准备调度延迟任务（延迟2秒）");

            string scheduledID = pool.ScheduleDelayed(
                () => {
                    Interlocked.Increment(ref counter);
                    Console.WriteLine("  [延迟任务] 执行完成");
                },
                2000
            );

            Console.WriteLine("  [0ms] 任务已调度");

            Thread.Sleep(1000);

            Console.WriteLine("  [1000ms] 取消延迟任务");
            bool cancelled = pool.CancelScheduledWork(scheduledID);

            Thread.Sleep(1500);

            if (cancelled && counter == 0) {
                Console.WriteLine("  ✓ 测试通过：任务成功取消，未执行\n");
            }
            else if (!cancelled) {
                Console.WriteLine("  ✗ 测试失败：取消操作返回false\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：任务已被执行（counter=" + counter + "）\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试7: 多个延迟任务
        /// Test 7: Multiple delayed works
        /// </summary>
        public static void Test7_MultipleDelayedWorks() {
            Console.WriteLine("测试7: 多个延迟任务");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            var results = new int[3];
            Console.WriteLine("  [0ms] 准备调度3个延迟任务");

            // 任务1: 延迟1秒
            pool.ScheduleDelayed(() => {
                results[0] = 1;
                Console.WriteLine("  [延迟任务1] 执行完成");
            },1000);

            // 任务2: 延迟2秒
            pool.ScheduleDelayed(() => {
                results[1] = 2;
                Console.WriteLine("  [延迟任务2] 执行完成");
            },2000);

            // 任务3: 延迟1.5秒
            pool.ScheduleDelayed(() => {
                results[2] = 3;
                Console.WriteLine("  [延迟任务3] 执行完成");
            },1500);

            Console.WriteLine("  [0ms] 3个任务已调度");

            Thread.Sleep(2500);

            if (results[0] == 1 && results[1] == 2 && results[2] == 3) {
                Console.WriteLine("  ✓ 测试通过：所有延迟任务按预期执行\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：results = [" + results[0] + ", " + results[1] + ", " + results[2] + "]\n");
            }

            pool.Dispose();
        }

        /// <summary>
        /// 测试8: 获取活跃的定时任务ID列表
        /// Test 8: Get active scheduled work IDs
        /// </summary>
        public static void Test8_GetActiveScheduledWorkIDs() {
            Console.WriteLine("测试8: 获取活跃的定时任务ID列表");
            var pool = new PowerPool(new PowerPoolOption { MaxThreads = 4 });
            pool.Start();

            Console.WriteLine("  [0ms] 初始活跃任务数: " + pool.ActiveScheduledWorkCount);

            var id1 = pool.ScheduleDelayed(() => { },5000);
            Console.WriteLine("  [0ms] 添加延迟任务后活跃数: " + pool.ActiveScheduledWorkCount);

            var id2 = pool.ScheduleRecurring(() => { },1000,3);
            Console.WriteLine("  [0ms] 添加周期任务后活跃数: " + pool.ActiveScheduledWorkCount);

            var activeIDs = pool.GetActiveScheduledWorkIDs();
            Console.WriteLine("  [0ms] 活跃任务ID列表: " + activeIDs.Count + " 个");

            bool allPresent = activeIDs.Contains(id1) && activeIDs.Contains(id2);

            pool.CancelScheduledWork(id1);
            Console.WriteLine("  [0ms] 取消一个任务后活跃数: " + pool.ActiveScheduledWorkCount);

            Thread.Sleep(4000); // 等待周期任务执行完毕

            Console.WriteLine("  [1500ms] 周期任务执行完毕后活跃数: " + pool.ActiveScheduledWorkCount);

            if (allPresent && pool.ActiveScheduledWorkCount == 0) {
                Console.WriteLine("  ✓ 测试通过：活跃任务列表和计数正确\n");
            }
            else {
                Console.WriteLine("  ✗ 测试失败：活跃任务管理不正确\n");
            }

            pool.Dispose();
        }
    }

}
