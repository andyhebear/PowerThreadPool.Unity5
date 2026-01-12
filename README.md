# PowerThreadPool_Net20

PowerThreadPool_Net20是一个为Unity 5.x及.NET 2.0环境设计的强大线程池库，提供了高效的任务调度和执行功能。基于开源库PowerThreadPool架构与思想重新设计编写。

## 特性

- **.NET 2.0兼容性**：完全兼容Unity 5.x的.NET 2.0环境
- **自动伸缩**：根据工作负载自动调整线程数量
- **任务优先级**：支持不同优先级的任务调度
- **同步与异步**：提供同步等待和异步执行模式
- **异常处理**：内置异常捕获和处理机制
- **任务取消**：支持任务取消功能
- **资源管理**：自动管理线程资源，避免资源泄露

## 安装

### Unity 5.x集成

1. 将PowerThreadPool_Net20项目编译为DLL
2. 将编译后的DLL文件导入Unity项目的Assets/Plugins目录
3. 在C#脚本中引用PowerThreadPool命名空间

### 直接引用（.NET 2.0项目）

1. 在Visual Studio中创建或打开.NET 2.0项目
2. 右键点击项目，选择"添加引用"
3. 浏览并选择PowerThreadPool_Net20.dll
4. 在代码中引用PowerThreadPool命名空间

## 基本用法

### 创建线程池

```csharp
// 创建默认配置的线程池
 PowerPool pool = new PowerPool(new PowerThreadPool_Net20.Options.PowerPoolOption()
 { MaxThreads = 4,ThreadNamePrefix = "rs.",ThreadQueueLimit = 8 });

 pool.Start();
 Console.WriteLine("Pool created with MinWorkers: 2, MaxWorkers: 4");
 Console.WriteLine("CurrentWorkerCount: {0}",pool.WaitingWorkCount);
 Console.WriteLine();
```

### 执行无返回值任务

```csharp
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
```

### 执行有返回值任务

```csharp
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
```

### 设置任务优先级

```csharp
// 设置任务优先级
var highPriorityOption = new WorkOption() { Priority = 1 }; // 优先级1（最高）
var lowPriorityOption = new WorkOption() { Priority = 5 };  // 优先级5（最低）

var highPriorityWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("高优先级任务开始");
    Thread.Sleep(1000);
    Console.WriteLine("高优先级任务完成");
}, highPriorityOption);

var lowPriorityWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("低优先级任务开始");
    Thread.Sleep(1000);
    Console.WriteLine("低优先级任务完成");
}, lowPriorityOption);
```

### 设置任务标签

```csharp
// 为任务设置标签以便于跟踪
var taggedOption = new WorkOption() { Tag = "重要计算任务" };

var taggedWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("带标签的任务开始执行");
    Thread.Sleep(500);
    return "计算结果";
}, taggedOption);

// 可以通过标签获取任务状态
var workStatus = pool.GetWorkStatus(taggedWork);
Console.WriteLine($"任务状态: {workStatus}, 标签: {taggedOption.Tag}");
```

### 等待所有任务完成

```csharp
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
```

### 带超时的等待

```csharp
// 带超时的等待
var timeoutWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("长时间运行任务开始");
    Thread.Sleep(5000); // 5秒任务
    return "任务完成";
});

// 等待最多3秒，超时则返回false
bool success = pool.WaitAll(TimeSpan.FromSeconds(3));
if (success)
{
    Console.WriteLine("所有任务在超时前完成");
}
else
{
    Console.WriteLine("等待超时，部分任务仍在运行");
}

// 单个任务带超时等待
bool workCompleted = pool.Wait(workId, TimeSpan.FromSeconds(2));
if (workCompleted)
{
    var result = pool.GetResult(workId);
    Console.WriteLine($"任务结果: {result}");
}
else
{
    Console.WriteLine("任务等待超时");
}
```

## 异常处理

```csharp
// 异常处理示例
var exceptionWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("可能抛出异常的任务开始");
    Thread.Sleep(1000);
    
    // 模拟异常
    if (DateTime.Now.Second % 2 == 0)
    {
        throw new InvalidOperationException("模拟异常");
    }
    
    return "正常完成";
});

var result = pool.GetResultAndWait(exceptionWork);
if (result.IsFaulted)
{
    Console.WriteLine($"任务执行失败: {result.Exception.Message}");
}
else
{
    Console.WriteLine($"任务执行成功: {result.Value}");
}

// 全局异常处理
pool.WorkFailed += (sender, e) =>
{
    Console.WriteLine($"工作失败: {e.WorkID}, 异常: {e.Exception.Message}");
};
```

## 任务取消

```csharp
// 任务取消示例
var cancelToken = new CancellationToken();
var cancelOption = new WorkOption() { CancellationToken = cancelToken };

var cancelableWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("可取消任务开始");
    
    for (int i = 0; i < 10; i++)
    {
        // 检查取消状态
        if (cancelToken.IsCancellationRequested)
        {
            Console.WriteLine("任务被取消");
            return "已取消";
        }
        
        Thread.Sleep(100); // 每次睡眠100ms
        Console.WriteLine($"任务进度: {i + 1}/10");
    }
    
    return "任务完成";
}, cancelOption);

// 启动取消任务
Thread.Sleep(2000);
cancelToken.Cancel();

var result = pool.GetResultAndWait(cancelableWork);
Console.WriteLine($"任务结果: {result.Value}");

// 取消事件处理
pool.WorkCanceled += (sender, e) =>
{
    Console.WriteLine($"工作被取消: {e.WorkID}");
};
```

## 线程池状态管理

```csharp
// 线程池状态管理
Console.WriteLine($"线程池状态: {pool.IsRunning}");
Console.WriteLine($"空闲线程数: {pool.IdleWorkerCount}");
Console.WriteLine($"等待任务数: {pool.WaitingWorkCount}");
Console.WriteLine($"总任务数: {pool.TotalWorkItems}");

// 暂停和恢复线程池
pool.Pause();
Console.WriteLine("线程池已暂停");

// 此时添加的任务会进入挂起队列
var suspendedWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("暂停期间添加的任务");
});

Thread.Sleep(1000);
pool.Resume();
Console.WriteLine("线程池已恢复");

// 清空队列
pool.ClearQueue();
Console.WriteLine("队列已清空");

// 停止线程池
pool.Dispose();
Console.WriteLine("线程池已停止");
```

## 高级用法

### 批量任务处理

```csharp
// 批量任务处理示例
int batchSize = 20;
WorkID[] batchWorks = new WorkID[batchSize];

// 创建批量任务
for (int i = 0; i < batchSize; i++)
{
    int index = i;
    batchWorks[i] = pool.QueueWorkItem(() =>
    {
        Console.WriteLine($"批量任务 {index} 开始");
        Thread.Sleep(new Random().Next(100, 500));
        return index * 10;
    });
}

// 等待所有任务完成
pool.WaitAll();

// 收集所有结果
List<int> results = new List<int>();
foreach (var workId in batchWorks)
{
    var result = pool.GetResult(workId);
    if (result.IsCompleted)
    {
        results.Add(result.Value);
    }
}

Console.WriteLine($"批量处理完成，共 {results.Count} 个结果");

// 并行处理数组数据
int[] data = Enumerable.Range(1, 100).ToArray();
int[] processedData = new int[data.Length];

pool.ParallelFor(0, data.Length, (i) =>
{
    processedData[i] = data[i] * data[i]; // 平方计算
});

Console.WriteLine($"并行处理完成，结果: {string.Join(", ", processedData.Take(10))}...");
```

### 优先级任务队列

```csharp
// 优先级任务队列示例
// 创建不同优先级的任务
var highPriorityOption = new WorkOption() { Priority = 1 };
var mediumPriorityOption = new WorkOption() { Priority = 3 };
var lowPriorityOption = new WorkOption() { Priority = 5 };

// 先添加低优先级任务
for (int i = 0; i < 3; i++)
{
    int index = i;
    pool.QueueWorkItem(() =>
    {
        Console.WriteLine($"低优先级任务 {index} 开始");
        Thread.Sleep(1000);
        Console.WriteLine($"低优先级任务 {index} 完成");
    }, lowPriorityOption);
}

// 然后添加高优先级任务
for (int i = 0; i < 2; i++)
{
    int index = i;
    pool.QueueWorkItem(() =>
    {
        Console.WriteLine($"高优先级任务 {index} 开始");
        Thread.Sleep(500);
        Console.WriteLine($"高优先级任务 {index} 完成");
    }, highPriorityOption);
}

// 最后添加中等优先级任务
for (int i = 0; i < 2; i++)
{
    int index = i;
    pool.QueueWorkItem(() =>
    {
        Console.WriteLine($"中等优先级任务 {index} 开始");
        Thread.Sleep(800);
        Console.WriteLine($"中等优先级任务 {index} 完成");
    }, mediumPriorityOption);
}

// 观察执行顺序：高优先级任务会优先执行
pool.WaitAll();
```

## 性能优化建议

1. **合理设置线程池大小**：
   - 对于CPU密集型任务，建议最大线程数设置为CPU核心数的1-2倍
   - 对于IO密集型任务，建议最大线程数设置为CPU核心数的4-8倍

2. **避免长时间运行的任务**：
   - 长时间运行的任务会占用线程资源，影响其他任务的执行
   - 考虑将长时间运行的任务拆分为多个短时间任务

3. **使用适当的任务优先级**：
   - 为不同类型的任务设置合理的优先级
   - 避免过多高优先级任务导致低优先级任务饥饿

4. **及时释放资源**：
   - 使用using语句确保线程池被正确释放
   - 避免创建过多的线程池实例

## 限制

1. **不支持异步/await**：由于.NET 2.0环境限制，不支持async/await语法
2. **不支持任务依赖**：当前版本不支持任务之间的依赖关系
3. **自定义取消令牌**：使用自定义的`CancellationToken`替代.NET 4.0+的`CancellationToken`
4. **线程池大小限制**：最大线程数受系统资源限制
5. **队列限制**：支持队列大小限制，防止内存溢出
6. **.NET 2.0兼容性**：部分现代.NET特性无法使用

## 版本历史

### v1.0.0：初始版本
- 基本线程池功能
- 支持任务优先级调度
- 支持任务取消
- 支持异常处理
- 支持线程池状态管理

### v1.1.0：增强功能
- 添加队列限制功能
- 完善线程回收机制
- 优化性能监控
- 增强异常处理

### v1.2.0：.NET 2.0完全兼容
- 完全兼容Unity 5.x和.NET 2.0环境
- 修复所有编译错误
- 优化资源管理
- 添加完整测试用例

## 技术支持

如果您在使用过程中遇到问题，请检查：
1. 确保目标框架设置为.NET Framework 2.0
2. 检查所有必要的引用是否已添加
3. 查看示例代码了解正确用法
4. 检查异常日志获取详细信息



## 许可证

MIT License

## 贡献

欢迎提交Issue和Pull Request！
## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
       <td align="center" valign="top" width="14.28%"><a href="https://github.com/andyhebear"><img src="https://avatars.githubusercontent.com/u/22004270?v=4?s=100" width="100px;" alt="rains"/><br /><sub><b>rains</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=andyhebear" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/dlnn"><img src="https://avatars.githubusercontent.com/u/22004270?v=4?s=100" width="100px;" alt="一条咸鱼"/><br /><sub><b>一条咸鱼</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=dlnn" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ZjzMisaka"><img src="https://avatars.githubusercontent.com/u/16731853?v=4?s=100" width="100px;" alt="ZjzMisaka"/><br /><sub><b>ZjzMisaka</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=ZjzMisaka" title="Code">💻</a> <a href="#maintenance-ZjzMisaka" title="Maintenance">🚧</a> <a href="https://github.com/ZjzMisaka/PowerThreadPool/commits?author=ZjzMisaka" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/r00tee"><img src="https://avatars.githubusercontent.com/u/32619657?v=4?s=100" width="100px;" alt="r00tee"/><br /><sub><b>r00tee</b></sub></a><br /><a href="#ideas-r00tee" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/aadog"><img src="https://avatars.githubusercontent.com/u/18098725?v=4?s=100" width="100px;" alt="aadog"/><br /><sub><b>aadog</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/issues?q=author%3Aaadog" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/RookieZWH"><img src="https://avatars.githubusercontent.com/u/17580767?v=4?s=100" width="100px;" alt="RookieZWH"/><br /><sub><b>RookieZWH</b></sub></a><br /><a href="#question-RookieZWH" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/hebinary"><img src="https://avatars.githubusercontent.com/u/86285187?v=4?s=100" width="100px;" alt="hebinary"/><br /><sub><b>hebinary</b></sub></a><br /><a href="#question-hebinary" title="Answering Questions">💬</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://blog.lindexi.com/"><img src="https://avatars.githubusercontent.com/u/16054566?v=4?s=100" width="100px;" alt="lindexi"/><br /><sub><b>lindexi</b></sub></a><br /><a href="https://github.com/ZjzMisaka/PowerThreadPool/issues?q=author%3Alindexi" title="Bug reports">🐛</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

## 联系方式

如有任何问题或建议，请通过以下方式联系：

- Email: [andyhebear@example.com](mailto:andyhebear@example.com)
- GitHub: [andyhebear](https://github.com/andyhebear)

## 致谢

感谢所有为PowerThreadPool项目做出贡献的开发者！
