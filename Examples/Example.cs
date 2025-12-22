using System;
using System.Threading;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Works;

namespace PowerThreadPool.Examples
{
    class Example
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
            //var result1 = pool.GetResultAndWait(workId1);
            //var result2 = pool.GetResultAndWait(workId2);
            pool.WaitAll();
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
                    Thread.Sleep(new Random().Next(200,800));
                    Console.WriteLine("Batch work {0} completed",workIndex);
                });
            }

            // Wait for all works to complete
            pool.WaitAll();

            Console.WriteLine();
            Console.WriteLine("All works completed");
            Console.WriteLine("Final CurrentWorkerCount: {0}",pool.WaitingWorkCount);


            Console.WriteLine();
            Console.WriteLine("PowerPool disposed");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
