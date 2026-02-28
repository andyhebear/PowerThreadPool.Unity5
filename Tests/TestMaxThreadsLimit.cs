using System;
using PowerThreadPool_Net20.Options;

namespace PowerThreadPool_Net20.Tests
{
    /// <summary>
    /// 测试MaxThreads的环境限制功能
    /// Test MaxThreads environment limitation
    /// </summary>
    public class TestMaxThreadsLimit
    {
        public static void Run()
        {
            Console.WriteLine("=== MaxThreads环境限制测试 ===");
            Console.WriteLine($"当前处理器数量: {Environment.ProcessorCount}");
            Console.WriteLine();

            // 测试1: 默认值
            Console.WriteLine("测试1: 默认MaxThreads值");
            var option1 = new PowerPoolOption();
            Console.WriteLine($"  默认值: {option1.MaxThreads} (预期: {Environment.ProcessorCount * 2})");
            Console.WriteLine();

            // 测试2: 设置合理值
            Console.WriteLine("测试2: 设置合理值");
            var option2 = new PowerPoolOption();
            option2.MaxThreads = 50;
            Console.WriteLine($"  设置值: 50, 实际值: {option2.MaxThreads}");
            Console.WriteLine();

            // 测试3: 设置超过处理器32倍的值
            Console.WriteLine("测试3: 设置超过处理器32倍的值");
            var option3 = new PowerPoolOption();
            int processorLimit = Environment.ProcessorCount * 32;
            option3.MaxThreads = processorLimit + 100;
            Console.WriteLine($"  设置值: {processorLimit + 100}");
            Console.WriteLine($"  处理器32倍限制: {processorLimit}");
            Console.WriteLine($"  实际值: {option3.MaxThreads} (应被限制为{processorLimit})");
            Console.WriteLine();

            // 测试4: 设置超过绝对上限512的值
            Console.WriteLine("测试4: 设置超过绝对上限512的值");
            var option4 = new PowerPoolOption();
            option4.MaxThreads = 1000;
            int absoluteLimit = 512;
            int expectedLimit = Math.Min(Environment.ProcessorCount * 32, absoluteLimit);
            Console.WriteLine($"  设置值: 1000");
            Console.WriteLine($"  绝对上限: {absoluteLimit}");
            Console.WriteLine($"  处理器32倍限制: {Environment.ProcessorCount * 32}");
            Console.WriteLine($"  实际值: {option4.MaxThreads} (应被限制为{expectedLimit})");
            Console.WriteLine();

            // 测试5: 设置小于等于0的值（应抛出异常）
            Console.WriteLine("测试5: 设置小于等于0的值");
            try
            {
                var option5 = new PowerPoolOption();
                option5.MaxThreads = 0;
                Console.WriteLine($"  错误: 未抛出异常");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  正确抛出异常: {ex.Message}");
            }
            Console.WriteLine();

            // 测试6: 设置负数（应抛出异常）
            Console.WriteLine("测试6: 设置负数");
            try
            {
                var option6 = new PowerPoolOption();
                option6.MaxThreads = -10;
                Console.WriteLine($"  错误: 未抛出异常");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  正确抛出异常: {ex.Message}");
            }
            Console.WriteLine();

            // 测试7: HighPerformance预设
            Console.WriteLine("测试7: HighPerformance预设");
            var option7 = PowerPoolOption.HighPerformance;
            Console.WriteLine($"  HighPerformance.MaxThreads: {option7.MaxThreads}");
            Console.WriteLine($"  预期值: {Environment.ProcessorCount * 4}");
            Console.WriteLine();

            // 测试8: 验证环境限制计算
            Console.WriteLine("测试8: 环境限制计算验证");
            int processorBasedLimit = Environment.ProcessorCount * 32;
            int absLimit = 512;
            int envLimit = Math.Min(processorBasedLimit, absLimit);
            Console.WriteLine($"  处理器数量: {Environment.ProcessorCount}");
            Console.WriteLine($"  处理器32倍限制: {processorBasedLimit}");
            Console.WriteLine($"  绝对上限: {absLimit}");
            Console.WriteLine($"  环境限制: {envLimit}");
            Console.WriteLine();

            Console.WriteLine("=== 测试完成 ===");
        }
    }
}
