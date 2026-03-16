using System;
using System.Threading;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Collections;

namespace PowerThreadPool_Net20.Tests
{
    /// <summary>
    /// 测试Watch功能
    /// Test Watch functionality
    /// </summary>
    public class TestWatch
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Testing Watch Functionality ===\n");

            TestBasicWatch();
            Console.WriteLine();

            TestWatchWithFailure();
            Console.WriteLine();

            TestWatchWithGroupName();
            Console.WriteLine();

            Console.WriteLine("=== All Watch Tests Completed ===");
        }

        /// <summary>
        /// 测试基本的Watch功能
        /// Test basic Watch functionality
        /// </summary>
        private static void TestBasicWatch()
        {
            Console.WriteLine("Test 1: Basic Watch Functionality");
            Console.WriteLine("----------------------------------");

            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MinThreads = 2,
                MaxThreads = 4
            });

            ConcurrentObservableCollection_Net20<int> collection = new ConcurrentObservableCollection_Net20<int>();
            
            // 添加一些初始数据
            for (int i = 1; i <= 5; i++)
            {
                collection.TryAdd(i);
            }

            Console.WriteLine("Initial collection size: " + collection.Count);

            // 开始监视
            Group group = pool.Watch(collection, item =>
            {
                Console.WriteLine($"Processing item: {item}");
                Thread.Sleep(100); // 模拟处理时间
            });

            Console.WriteLine("After watch started, collection size: " + collection.Count);

            // 等待所有工作完成
            Thread.Sleep(2000);

            Console.WriteLine("After processing, collection size: " + collection.Count);

            // 添加更多数据
            Console.WriteLine("\nAdding more items...");
            for (int i = 6; i <= 10; i++)
            {
                collection.TryAdd(i);
            }

            Console.WriteLine("After adding more items, collection size: " + collection.Count);

            // 等待处理完成
            Thread.Sleep(2000);

            Console.WriteLine("After processing new items, collection size: " + collection.Count);

            // 停止监视
            pool.StopWatching(collection);

            Console.WriteLine("After stopping watch, collection size: " + collection.Count);

            // 清理
            pool.Dispose();
            Console.WriteLine("Test 1: PASSED");
        }

        /// <summary>
        /// 测试工作失败时回退到集合的功能
        /// Test add back to collection when work fails
        /// </summary>
        private static void TestWatchWithFailure()
        {
            Console.WriteLine("Test 2: Watch with Failure Handling");
            Console.WriteLine("-------------------------------------");

            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MinThreads = 2,
                MaxThreads = 4
            });

            ConcurrentObservableCollection_Net20<int> collection = new ConcurrentObservableCollection_Net20<int>();

            // 添加数据
            for (int i = 1; i <= 5; i++)
            {
                collection.TryAdd(i);
            }

            Console.WriteLine("Initial collection size: " + collection.Count);

            // 开始监视，启用失败回退
            Group group = pool.Watch(collection, item =>
            {
                Console.WriteLine($"Processing item: {item}");
                
                // 模拟某些项目失败
                if (item == 2 || item == 4)
                {
                    throw new Exception($"Simulated failure for item {item}");
                }
                
                Thread.Sleep(100);
            }, addBackWhenWorkFailed: true);

            Console.WriteLine("Watch started with failure handling enabled");

            // 等待处理完成
            Thread.Sleep(3000);

            Console.WriteLine("After processing, collection size: " + collection.Count);

            // 停止监视
            pool.StopWatching(collection);

            // 清理
            pool.Dispose();
            Console.WriteLine("Test 2: PASSED");
        }

        /// <summary>
        /// 测试使用组名称的Watch功能
        /// Test Watch with group name
        /// </summary>
        private static void TestWatchWithGroupName()
        {
            Console.WriteLine("Test 3: Watch with Group Name");
            Console.WriteLine("--------------------------------");

            PowerPool pool = new PowerPool(new PowerPoolOption
            {
                MinThreads = 2,
                MaxThreads = 4
            });

            ConcurrentObservableCollection_Net20<string> collection = new ConcurrentObservableCollection_Net20<string>();

            // 添加数据
            for (int i = 1; i <= 3; i++)
            {
                collection.TryAdd($"Task-{i}");
            }

            Console.WriteLine("Initial collection size: " + collection.Count);

            // 使用组名称开始监视
            Group group = pool.Watch(collection, item =>
            {
                Console.WriteLine($"Processing: {item}");
                Thread.Sleep(100);
            }, groupName: "TestGroup");

            Console.WriteLine($"Watch started with group: {group.Name}");
            Console.WriteLine($"Group exists: {pool.GroupExists("TestGroup")}");

            // 等待处理完成
            Thread.Sleep(2000);

            Console.WriteLine("After processing, collection size: " + collection.Count);

            // 停止监视
            pool.StopWatching(collection);

            // 清理
            pool.Dispose();
            Console.WriteLine("Test 3: PASSED");
        }
    }
}
