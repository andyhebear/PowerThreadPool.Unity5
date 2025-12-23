#if UNITY
using System;
using UnityEngine;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;

namespace PowerThreadPool_Net20.Examples
{
    /// <summary>
    /// 并行循环和ExecuteResult使用示例
    /// Parallel loops and ExecuteResult usage examples
    /// </summary>
    public class ParallelAndResultExample : MonoBehaviour
    {
        private PowerPool _threadPool;
        
        void Start()
        {
            // 创建线程池
            var options = new PowerPoolOption
            {
                MinWorkerThreads = 2,
                MaxWorkerThreads = 4,
                IdleTimeout = 30000
            };
            
            _threadPool = new PowerPool(options);
            _threadPool.Start();
            
            Debug.Log("=== 并行循环和ExecuteResult示例开始 ===");
            
            // 1. ParallelFor示例
            TestParallelFor();
            
            // 2. ParallelForEach示例
            TestParallelForEach();
            
            // 3. ParallelInvoke示例
            TestParallelInvoke();
            
            // 4. ExecuteResult示例
            TestExecuteResults();
        }
        
        /// <summary>
        /// 测试ParallelFor
        /// </summary>
        void TestParallelFor()
        {
            Debug.Log("--- ParallelFor 测试 ---");
            
            // 并行计算0-99的平方
            WorkID[] workIds = _threadPool.ParallelFor(0, 100, i =>
            {
                int result = i * i;
                Debug.Log($"计算 {i}^2 = {result}");
            });
            
            Debug.Log($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();
            
            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Debug.Log($"ParallelFor 完成，成功: {Array.FindAll(results, r => r.IsSuccess).Length}");
        }
        
        /// <summary>
        /// 测试ParallelForEach
        /// </summary>
        void TestParallelForEach()
        {
            Debug.Log("--- ParallelForEach 测试 ---");
            
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // 并行处理每个数字
            WorkID[] workIds = _threadPool.ParallelForEach(numbers, number =>
            {
                Debug.Log($"处理数字: {number}");
                // 模拟一些计算
                int result = 0;
                for (int i = 0; i < 100000; i++)
                {
                    result += number * i;
                }
            });
            
            Debug.Log($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();
            
            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Debug.Log($"ParallelForEach 完成，成功: {Array.FindAll(results, r => r.IsSuccess).Length}");
        }
        
        /// <summary>
        /// 测试ParallelInvoke
        /// </summary>
        void TestParallelInvoke()
        {
            Debug.Log("--- ParallelInvoke 测试 ---");
            
            // 并行执行多个操作
            WorkID[] workIds = _threadPool.ParallelInvoke(
                () => Debug.Log("任务1: 执行数据处理"),
                () => Debug.Log("任务2: 执行网络请求"),
                () => Debug.Log("任务3: 执行文件读写"),
                () => Debug.Log("任务4: 执行计算任务")
            );
            
            Debug.Log($"提交了 {workIds.Length} 个并行工作项");
            _threadPool.WaitAll();
            
            // 获取结果
            var results = _threadPool.GetResults(workIds);
            Debug.Log($"ParallelInvoke 完成，成功: {Array.FindAll(results, r => r.IsSuccess).Length}");
        }
        
        /// <summary>
        /// 测试ExecuteResult功能
        /// </summary>
        void TestExecuteResults()
        {
            Debug.Log("--- ExecuteResult 测试 ---");
            
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
            Debug.Log("等待工作完成...");
            var result1 = _threadPool.GetResultAndWait(workId1);
            var result2 = _threadPool.GetResultAndWait(workId2);
            var result3 = _threadPool.GetResultAndWait(workId3);
            
            Debug.Log($"结果1: {result1.Result}, 状态: {result1.Status}, 耗时: {result1.Duration.TotalMilliseconds:F0}ms");
            Debug.Log($"结果2: {result2.Result}, 状态: {result2.Status}, 耗时: {result2.Duration.TotalMilliseconds:F0}ms");
            Debug.Log($"结果3: 异常={result3.Exception?.Message}, 状态: {result3.Status}, 耗时: {result3.Duration.TotalMilliseconds:F0}ms");
            
            // 批量获取结果
            Debug.Log($"当前缓存的结果数量: {_threadPool.CachedResultCount}");
            
            var allResults = _threadPool.GetResultsAndWait(new WorkID[] { workId1, workId2, workId3 });
            Debug.Log($"批量获取了 {allResults.Length} 个结果");
            
            // 清理结果缓存
            _threadPool.ClearAllResults();
            Debug.Log($"清理后缓存结果数量: {_threadPool.CachedResultCount}");
        }
        
        void OnDestroy()
        {
            if (_threadPool != null)
            {
                _threadPool.Dispose();
                Debug.Log("线程池已释放");
            }
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("并行循环和ExecuteResult示例");
            
            if (GUILayout.Button("测试 ParallelFor"))
            {
                TestParallelFor();
            }
            
            if (GUILayout.Button("测试 ParallelForEach"))
            {
                TestParallelForEach();
            }
            
            if (GUILayout.Button("测试 ParallelInvoke"))
            {
                TestParallelInvoke();
            }
            
            if (GUILayout.Button("测试 ExecuteResults"))
            {
                TestExecuteResults();
            }
            
            GUILayout.Label($"缓存结果: {_threadPool.CachedResultCount}");
            GUILayout.EndArea();
        }
    }
}

#endif