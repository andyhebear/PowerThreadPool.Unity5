using System;
using System.Threading;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Collections;

namespace PowerThreadPool_Net20.Examples
{
    /// <summary>
    /// Watch功能示例 / Watch feature examples
    /// </summary>
    public class WatchExamples
    {
        /// <summary>
        /// 示例1: 基本的Watch用法
        /// Example 1: Basic Watch usage
        /// </summary>
        public static void Example1_BasicWatch()
        {
            Console.WriteLine("=== Example 1: Basic Watch Usage ===");

            PowerPool pool = new PowerPool(4);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"Processing task: {task}");
                    Thread.Sleep(500);
                    Console.WriteLine($"Completed task: {task}");
                },
                addBackWhenWorkCanceled: true,
                addBackWhenWorkStopped: true,
                addBackWhenWorkFailed: true,
                groupName: "TaskProcessor"
            );

            taskQueue.Add("Task 1");
            taskQueue.Add("Task 2");
            taskQueue.Add("Task 3");

            Thread.Sleep(2000);

            pool.StopWatching(taskQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例2: 批量添加元素
        /// Example 2: Batch add elements
        /// </summary>
        public static void Example2_BatchAdd()
        {
            Console.WriteLine("=== Example 2: Batch Add Elements ===");

            PowerPool pool = new PowerPool(4);
            pool.Start();

            ConcurrentObservableCollection_Net20<int> numberQueue = new ConcurrentObservableCollection_Net20<int>();

            Group group = pool.Watch<int>(
                source: numberQueue,
                body: (number) =>
                {
                    Console.WriteLine($"Processing number: {number}");
                    Thread.Sleep(200);
                },
                groupName: "NumberProcessor"
            );

            string[] numbers = { "10", "20", "30", "40", "50" };
            numberQueue.AddRange(new int[] { 10, 20, 30, 40, 50 });

            Thread.Sleep(1500);

            pool.StopWatching(numberQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例3: 处理失败的情况
        /// Example 3: Handle failure scenarios
        /// </summary>
        public static void Example3_HandleFailures()
        {
            Console.WriteLine("=== Example 3: Handle Failure Scenarios ===");

            PowerPool pool = new PowerPool(2);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"Starting task: {task}");

                    if (task == "FailTask")
                    {
                        Console.WriteLine($"Task {task} failed!");
                        throw new Exception("Task failed intentionally");
                    }

                    Thread.Sleep(300);
                    Console.WriteLine($"Task {task} completed successfully");
                },
                addBackWhenWorkFailed: true,
                addBackWhenWorkCanceled: true,
                addBackWhenWorkStopped: true,
                groupName: "FailureHandler"
            );

            taskQueue.Add("NormalTask1");
            taskQueue.Add("FailTask");
            taskQueue.Add("NormalTask2");

            Thread.Sleep(2000);

            Console.WriteLine($"Queue count after processing: {taskQueue.Count}");

            pool.StopWatching(taskQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例4: 动态添加任务
        /// Example 4: Dynamically add tasks
        /// </summary>
        public static void Example4_DynamicTasks()
        {
            Console.WriteLine("=== Example 4: Dynamic Tasks ===");

            PowerPool pool = new PowerPool(3);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Processing: {task}");
                    Thread.Sleep(1000);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Completed: {task}");
                },
                groupName: "DynamicProcessor"
            );

            taskQueue.Add("Initial Task 1");
            taskQueue.Add("Initial Task 2");

            Thread.Sleep(500);
            taskQueue.Add("Dynamic Task 1");

            Thread.Sleep(500);
            taskQueue.Add("Dynamic Task 2");

            Thread.Sleep(500);
            taskQueue.Add("Dynamic Task 3");

            Thread.Sleep(3000);

            pool.StopWatching(taskQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例5: 不回退失败的任务
        /// Example 5: Don't add back failed tasks
        /// </summary>
        public static void Example5_NoAddBackOnFailure()
        {
            Console.WriteLine("=== Example 5: No Add Back on Failure ===");

            PowerPool pool = new PowerPool(2);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"Processing: {task}");

                    if (task.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"Task {task} failed!");
                        throw new Exception("Task failed");
                    }

                    Thread.Sleep(200);
                    Console.WriteLine($"Task {task} succeeded");
                },
                addBackWhenWorkFailed: false,
                addBackWhenWorkCanceled: false,
                addBackWhenWorkStopped: false,
                groupName: "NoRetryProcessor"
            );

            taskQueue.Add("SuccessTask1");
            taskQueue.Add("FailTask");
            taskQueue.Add("SuccessTask2");

            Thread.Sleep(1500);

            Console.WriteLine($"Remaining tasks in queue: {taskQueue.Count}");

            pool.StopWatching(taskQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例6: 使用自定义对象
        /// Example 6: Use custom objects
        /// </summary>
        public static void Example6_CustomObjects()
        {
            Console.WriteLine("=== Example 6: Custom Objects ===");

            PowerPool pool = new PowerPool(3);
            pool.Start();

            ConcurrentObservableCollection_Net20<WorkItem> workQueue = new ConcurrentObservableCollection_Net20<WorkItem>();

            Group group = pool.Watch<WorkItem>(
                source: workQueue,
                body: (item) =>
                {
                    Console.WriteLine($"Processing WorkItem ID={item.ID}, Priority={item.Priority}");
                    Thread.Sleep(item.Priority == Priority.High ? 500 : 1000);
                    Console.WriteLine($"Completed WorkItem ID={item.ID}");
                },
                groupName: "WorkItemProcessor"
            );

            workQueue.Add(new WorkItem { ID = 1, Name = "Task A", Priority = Priority.High });
            workQueue.Add(new WorkItem { ID = 2, Name = "Task B", Priority = Priority.Low });
            workQueue.Add(new WorkItem { ID = 3, Name = "Task C", Priority = Priority.High });
            workQueue.Add(new WorkItem { ID = 4, Name = "Task D", Priority = Priority.Low });

            Thread.Sleep(3000);

            pool.StopWatching(workQueue, group);
            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例7: 强制停止监视
        /// Example 7: Force stop watching
        /// </summary>
        public static void Example7_ForceStop()
        {
            Console.WriteLine("=== Example 7: Force Stop Watching ===");

            PowerPool pool = new PowerPool(2);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"Starting long task: {task}");
                    Thread.Sleep(3000);
                    Console.WriteLine($"Completed long task: {task}");
                },
                groupName: "LongTaskProcessor"
            );

            taskQueue.Add("LongTask1");
            taskQueue.Add("LongTask2");

            Thread.Sleep(1000);

            Console.WriteLine("Force stopping watch...");
            pool.ForceStopWatching(taskQueue, group);

            pool.Dispose();
            Console.WriteLine();
        }

        /// <summary>
        /// 示例8: 保持运行状态停止监视
        /// Example 8: Stop watching but keep running
        /// </summary>
        public static void Example8_StopKeepRunning()
        {
            Console.WriteLine("=== Example 8: Stop Watching Keep Running ===");

            PowerPool pool = new PowerPool(2);
            pool.Start();

            ConcurrentObservableCollection_Net20<string> taskQueue = new ConcurrentObservableCollection_Net20<string>();

            Group group = pool.Watch<string>(
                source: taskQueue,
                body: (task) =>
                {
                    Console.WriteLine($"Processing: {task}");
                    Thread.Sleep(500);
                    Console.WriteLine($"Completed: {task}");
                },
                groupName: "KeepRunningProcessor"
            );

            taskQueue.Add("Task1");
            taskQueue.Add("Task2");

            Thread.Sleep(1500);

            Console.WriteLine("Stopping watch but keep running...");
            pool.StopWatching(taskQueue, group, keepRunning: true);

            taskQueue.Add("Task3");
            taskQueue.Add("Task4");

            Thread.Sleep(1500);

            pool.Dispose();
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 工作项类 / Work item class
    /// </summary>
    public class WorkItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Priority Priority { get; set; }
    }

    /// <summary>
    /// 优先级枚举 / Priority enumeration
    /// </summary>
    public enum Priority
    {
        Low,
        Medium,
        High
    }
}
