#if UNITY
using UnityEngine;
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Threading;
using System;

namespace PowerThreadPool_Net20.Examples
{
    /// <summary>
    /// CancellationToken功能测试示例
    /// CancellationToken functionality test example
    /// </summary>
    public class CancellationTokenTest : MonoBehaviour
    {
        private PowerPool _threadPool;
        
        void Start()
        {
            _threadPool = new PowerPool();
            _threadPool.Start();
            
            Debug.Log("=== CancellationToken测试开始 ===");
            
            // 测试1: 基本取消功能
            TestBasicCancellation();
            
            // 测试2: 取消令牌源
            TestCancellationTokenSource();
            
            // 测试3: 工作项取消
            TestWorkCancellation();
        }
        
        void TestBasicCancellation()
        {
            Debug.Log("--- 基本取消功能测试 ---");
            
            var token = new CancellationToken();
            Debug.Log($"初始状态: IsCancellationRequested = {token.IsCancellationRequested}");
            
            token.Cancel();
            Debug.Log($"取消后: IsCancellationRequested = {token.IsCancellationRequested}");
            
            token.Reset();
            Debug.Log($"重置后: IsCancellationRequested = {token.IsCancellationRequested}");
        }
        
        void TestCancellationTokenSource()
        {
            Debug.Log("--- 取消令牌源测试 ---");
            
            using (var cts = new CancellationTokenSource())
            {
                var token = cts.Token;
                Debug.Log($"初始状态: IsCancellationRequested = {token.IsCancellationRequested}");
                
                cts.Cancel();
                Debug.Log($"源取消后: IsCancellationRequested = {token.IsCancellationRequested}");
            }
        }
        
        void TestWorkCancellation()
        {
            Debug.Log("--- 工作项取消测试 ---");
            
            var cts = new CancellationTokenSource();
            var option = new WorkOption 
            { 
                CancellationToken = cts.Token 
            };
            
            var workId = _threadPool.QueueWorkItem(() => 
            {
                try
                {
                    // 模拟长时间工作
                    for (int i = 0; i < 10; i++)
                    {
                        // 检查取消状态
                        option.CancellationToken.ThrowIfCancellationRequested();
                        Debug.Log($"工作中... {i}/10");
                        System.Threading.Thread.Sleep(100);
                    }
                    return "工作完成";
                }
                catch (OperationCanceledException)
                {
                    return "工作被取消";
                }
            }, option);
            
            // 等待一小段时间后取消
            System.Threading.Thread.Sleep(300);
            cts.Cancel();
            
            var result = _threadPool.GetResultAndWait(workId, 2000);
            Debug.Log($"工作结果: {result.Result}");
        }
        
        void OnDestroy()
        {
            if (_threadPool != null)
            {
                _threadPool.Dispose();
                Debug.Log("CancellationToken测试完成，线程池已释放");
            }
        }
    }
}
#endif