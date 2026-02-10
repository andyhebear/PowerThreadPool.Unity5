# PowerThreadPool_Net20

PowerThreadPool_Net20 是为 Unity 5.x 及 .NET 2.0 环境设计的高性能线程池库，基于开源库 PowerThreadPool 架构重新设计编写，提供强大的任务调度、执行和管理功能。

PowerThreadPool_Net20 is a high-performance thread pool library designed for Unity 5.x and .NET 2.0 environments, redesigned based on the open-source PowerThreadPool architecture, providing powerful task scheduling, execution, and management capabilities.

## 特性 / Features

### 核心功能 / Core Features

- **.NET 2.0 完全兼容** / **Full .NET 2.0 Compatibility**
  - 支持 Unity 5.6+ 项目 / Supports Unity 5.6+ projects
  - 无需依赖 .NET 4.0+ 特性 / No dependency on .NET 4.0+ features

- **智能线程管理** / **Intelligent Thread Management**
  - 自动伸缩线程池 / Auto-scaling thread pool
  - 动态调整工作线程数量 / Dynamic worker thread adjustment
  - 空闲线程回收 / Idle thread recycling
  - 可配置的最小/最大线程数 / Configurable min/max thread count

- **优先级调度** / **Priority Scheduling**
  - 4级优先级队列 / 4-level priority queues
  - 基于无锁队列的高性能调度 / High-performance scheduling based on lock-free queues
  - 工作窃取支持 / Work stealing support

- **定时任务调度** / **Scheduled Task Scheduling**
  - 延迟执行（一次性任务）/ Delayed execution (one-time tasks)
  - 定期执行（周期性任务）/ Recurring execution (periodic tasks)
  - 可配置执行间隔和最大执行次数 / Configurable interval and max execution count

- **工作组管理** / **Work Group Management**
  - 任务分组和批量等待 / Task grouping and batch waiting
  - 按组等待完成 / Wait by group completion
  - 灵活的任务组织 / Flexible task organization

- **结果缓存** / **Result Caching**
  - 自动结果缓存管理 / Automatic result cache management
  - 可配置的缓存过期时间 / Configurable cache expiration time
  - 防止内存泄漏 / Prevents memory leaks

- **异常处理** / **Exception Handling**
  - 内置异常捕获和报告 / Built-in exception capture and reporting
  - 事件驱动异常通知 / Event-driven exception notification
  - 任务级异常隔离 / Task-level exception isolation

- **任务取消** / **Task Cancellation**
  - 自定义取消令牌 / Custom cancellation tokens
  - 实时取消检查 / Real-time cancellation check
  - 取消事件通知 / Cancellation event notification

- **状态监控** / **Status Monitoring**
  - 线程池状态查询 / Thread pool status query
  - 统计信息收集 / Statistics collection
  - 工作项跟踪 / Work item tracking

## 安装 / Installation

### Unity 5.x 集成 / Integration

1. 将 PowerThreadPool_Net20 项目编译为 DLL / Compile PowerThreadPool_Net20 project to DLL
2. 将编译后的 DLL 文件导入 Unity 项目的 `Assets/Plugins` 目录 / Import compiled DLL to `Assets/Plugins` folder
3. 在 C# 脚本中引用 `PowerThreadPool_Net20` 命名空间 / Reference `PowerThreadPool_Net20` namespace in C# scripts

### 直接引用 / Direct Reference (.NET 2.0)

1. 在 Visual Studio 中创建或打开 .NET 2.0 项目 / Create or open .NET 2.0 project in Visual Studio
2. 右键点击项目，选择"添加引用" / Right-click project, select "Add Reference"
3. 浏览并选择 PowerThreadPool_Net20.dll / Browse and select PowerThreadPool_Net20.dll
4. 在代码中引用 PowerThreadPool 命名空间 / Reference PowerThreadPool namespace in code

## 基本用法 / Basic Usage

### 创建线程池 / Create Thread Pool

```csharp
// 创建默认配置的线程池 / Create thread pool with default configuration
var pool = new PowerPool(new PowerPoolOption()
{
    MaxThreads = 4,
    MinThreads = 1,
    ThreadNamePrefix = "Worker",
    ThreadQueueLimit = 8
});

pool.Start();
Console.WriteLine("Pool created with MinWorkers: 1, MaxWorkers: 4");
Console.WriteLine("CurrentWorkerCount: {0}", pool.WaitingWorkCount);
Console.WriteLine();
```

### 配置选项 / Configuration Options

```csharp
// 预定义配置 / Predefined configurations
var minimalPool = new PowerPool(PowerPoolOption.Minimal); // 最小配置 / Minimal config
var highPerfPool = new PowerPool(PowerPoolOption.HighPerformance); // 高性能配置 / High performance config

// 自定义配置 / Custom configuration
var customPool = new PowerPool(new PowerPoolOption()
{
    MaxThreads = 8,
    MinThreads = 2,
    IdleThreadTimeout = TimeSpan.FromMinutes(5),
    ThreadQueueLimit = 100,
    EnableStatisticsCollection = true,
    EnableResultCacheExpiration = true,
    ResultCacheExpiration = TimeSpan.FromMinutes(10),
    ThreadPriority = ThreadPriority.Normal,
    UseBackgroundThreads = true,
    ThreadNamePrefix = "MyPool"
});
```

### 执行无返回值任务 / Execute Tasks Without Return Value

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

// Wait for both works to complete / 等待两个任务完成
var result1 = pool.GetResultAndWait(workId1);
var result2 = pool.GetResultAndWait(workId2);
```

### 执行有返回值任务 / Execute Tasks With Return Value

```csharp
// Example 2: Queue functions with results
Console.WriteLine("Example 2: Queue functions with results");

var work3 = pool.QueueWorkItem(() =>
{
    Console.WriteLine("Work 3 started");
    Thread.Sleep(800);
    int result = 42;
    Console.WriteLine("Work 3 completed with result: {0}", result);
    return result;
});

var work4 = pool.QueueWorkItem(() =>
{
    Console.WriteLine("Work 4 started");
    Thread.Sleep(600);
    string result = "Hello from PowerThreadPool";
    Console.WriteLine("Work 4 completed with result: {0}", result);
    return result;
});

// Get results / 获取结果
var result3 = pool.GetResultAndWait(work3);
var result4 = pool.GetResultAndWait(work4);
Console.WriteLine("Result 3: {0}, Result 4: {1}", result3.Value, result4.Value);
```

### 设置任务优先级 / Set Task Priority

```csharp
// 设置任务优先级 / Set task priority
var highPriorityOption = new WorkOption() { Priority = 1 }; // 优先级1（最高）/ Priority 1 (Highest)
var mediumPriorityOption = new WorkOption() { Priority = 3 }; // 优先级3（中等）/ Priority 3 (Medium)
var lowPriorityOption = new WorkOption() { Priority = 5 };  // 优先级5（最低）/ Priority 5 (Lowest)

var highPriorityWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("高优先级任务开始 / High priority task started");
    Thread.Sleep(1000);
    Console.WriteLine("高优先级任务完成 / High priority task completed");
}, highPriorityOption);

var lowPriorityWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("低优先级任务开始 / Low priority task started");
    Thread.Sleep(1000);
    Console.WriteLine("低优先级任务完成 / Low priority task completed");
}, lowPriorityOption);
```

### 等待所有任务完成 / Wait All Tasks

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
        Console.WriteLine("Batch work {0} started", workIndex);
        Thread.Sleep(new Random().Next(200, 800));
        Console.WriteLine("Batch work {0} completed", workIndex);
    });
}

// Wait for all works to complete / 等待所有任务完成
pool.WaitAll();
```

### 带超时的等待 / Wait With Timeout

```csharp
// 带超时的等待 / Wait with timeout
var timeoutWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("长时间运行任务开始 / Long running task started");
    Thread.Sleep(5000); // 5秒任务 / 5 seconds task
    return "任务完成 / Task completed";
});

// 等待最多3秒，超时则返回false / Wait up to 3 seconds, return false on timeout
bool success = pool.WaitAll(TimeSpan.FromSeconds(3));
if (success)
{
    Console.WriteLine("所有任务在超时前完成 / All tasks completed before timeout");
}
else
{
    Console.WriteLine("等待超时，部分任务仍在运行 / Timeout, some tasks still running");
}

// 单个任务带超时等待 / Single task wait with timeout
bool workCompleted = pool.Wait(workId, TimeSpan.FromSeconds(2));
if (workCompleted)
{
    var result = pool.GetResult(workId);
    Console.WriteLine($"任务结果 / Task result: {result.Value}");
}
else
{
    Console.WriteLine("任务等待超时 / Task wait timeout");
}
```

## 高级功能 / Advanced Features

### 定时任务调度 / Scheduled Task Scheduling

```csharp
// 延迟执行（一次性）/ Delayed execution (one-time)
var scheduledWorkId = pool.WorkScheduler.ScheduleDelayed(
    () =>
    {
        Console.WriteLine("延迟2秒后执行的任务 / Task executed after 2 seconds delay");
        return "Delayed result";
    },
    TimeSpan.FromSeconds(2)
);

// 取消定时任务 / Cancel scheduled task
pool.WorkScheduler.CancelScheduledWork(scheduledWorkId);

// 定期执行（周期性）/ Recurring execution (periodic)
var recurringWorkId = pool.WorkScheduler.ScheduleRecurring(
    () =>
    {
        Console.WriteLine("每1秒执行一次的任务 / Task executed every 1 second");
    },
    TimeSpan.FromSeconds(1),
    maxExecutions: 5 // 最多执行5次 / Max 5 executions
);

// 获取定时任务信息 / Get scheduled work info
var info = pool.WorkScheduler.GetScheduledWorkInfo(recurringWorkId);
Console.WriteLine($"执行次数 / Executions: {info.ExecutedCount}");
```

### 工作组管理 / Work Group Management

```csharp
// 创建工作组 / Create work group
var group = pool.CreateGroup("DataProcessing");

// 添加任务到工作组 / Add tasks to group
var task1 = pool.QueueWorkItem(() => ProcessData("data1.txt"));
var task2 = pool.QueueWorkItem(() => ProcessData("data2.txt"));
var task3 = pool.QueueWorkItem(() => ProcessData("data3.txt"));

group.Add(task1);
group.Add(task2);
group.Add(task3);

// 等待工作组中所有任务完成 / Wait for all tasks in group to complete
group.Wait();

// 获取工作组成员 / Get group members
var members = group.GetMembers();
Console.WriteLine($"工作组包含 {members.Count} 个任务 / Group contains {members.Count} tasks");

// 移除任务 / Remove task
group.Remove(task2);
```

### 批量并行处理 / Batch Parallel Processing

```csharp
// 批量任务处理示例 / Batch task processing example
int batchSize = 20;
WorkID[] batchWorks = new WorkID[batchSize];

// 创建批量任务 / Create batch tasks
for (int i = 0; i < batchSize; i++)
{
    int index = i;
    batchWorks[i] = pool.QueueWorkItem(() =>
    {
        Console.WriteLine($"批量任务 {index} 开始 / Batch task {index} started");
        Thread.Sleep(new Random().Next(100, 500));
        return index * 10;
    });
}

// 等待所有任务完成 / Wait for all tasks to complete
pool.WaitAll();

// 收集所有结果 / Collect all results
List<int> results = new List<int>();
foreach (var workId in batchWorks)
{
    var result = pool.GetResult(workId);
    if (result.IsCompleted)
    {
        results.Add(result.Value);
    }
}

Console.WriteLine($"批量处理完成，共 {results.Count} 个结果 / Batch processing completed, {results.Count} results");

// 并行处理数组数据 / Parallel processing of array data
int[] data = Enumerable.Range(1, 100).ToArray();
int[] processedData = new int[data.Length];

pool.ParallelFor(0, data.Length, (i) =>
{
    processedData[i] = data[i] * data[i]; // 平方计算 / Square calculation
});

Console.WriteLine($"并行处理完成 / Parallel processing completed");
```

### 异常处理 / Exception Handling

```csharp
// 异常处理示例 / Exception handling example
var exceptionWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("可能抛出异常的任务开始 / Task that may throw exception started");
    Thread.Sleep(1000);

    // 模拟异常 / Simulate exception
    if (DateTime.Now.Second % 2 == 0)
    {
        throw new InvalidOperationException("模拟异常 / Simulated exception");
    }

    return "正常完成 / Normal completion";
});

var result = pool.GetResultAndWait(exceptionWork);
if (result.IsFaulted)
{
    Console.WriteLine($"任务执行失败 / Task execution failed: {result.Exception.Message}");
}
else
{
    Console.WriteLine($"任务执行成功 / Task execution succeeded: {result.Value}");
}

// 全局异常处理 / Global exception handling
pool.WorkFailed += (sender, e) =>
{
    Console.WriteLine($"工作失败 / Work failed: {e.WorkID}, 异常 / Exception: {e.Exception.Message}");
};
```

### 任务取消 / Task Cancellation

```csharp
// 任务取消示例 / Task cancellation example
var cancelToken = new CancellationToken();
var cancelOption = new WorkOption() { CancellationToken = cancelToken };

var cancelableWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("可取消任务开始 / Cancellable task started");

    for (int i = 0; i < 10; i++)
    {
        // 检查取消状态 / Check cancellation status
        if (cancelToken.IsCancellationRequested)
        {
            Console.WriteLine("任务被取消 / Task cancelled");
            return "已取消 / Cancelled";
        }

        Thread.Sleep(100); // 每次睡眠100ms / Sleep 100ms each iteration
        Console.WriteLine($"任务进度 / Task progress: {i + 1}/10");
    }

    return "任务完成 / Task completed";
}, cancelOption);

// 启动取消任务 / Start cancellation task
Thread.Sleep(2000);
cancelToken.Cancel();

var result = pool.GetResultAndWait(cancelableWork);
Console.WriteLine($"任务结果 / Task result: {result.Value}");

// 取消事件处理 / Cancellation event handling
pool.WorkCanceled += (sender, e) =>
{
    Console.WriteLine($"工作被取消 / Work cancelled: {e.WorkID}");
};
```

### 线程池状态管理 / Thread Pool State Management

```csharp
// 线程池状态管理 / Thread pool state management
Console.WriteLine($"线程池状态 / Pool state: {pool.IsRunning}");
Console.WriteLine($"空闲线程数 / Idle thread count: {pool.IdleWorkerCount}");
Console.WriteLine($"等待任务数 / Waiting task count: {pool.WaitingWorkCount}");
Console.WriteLine($"总任务数 / Total task count: {pool.TotalWorkItems}");

// 获取统计信息 / Get statistics
var stats = pool.GetStatistics();
Console.WriteLine($"已完成任务 / Completed tasks: {stats.CompletedWorkItems}");
Console.WriteLine($"失败任务 / Failed tasks: {stats.FailedWorkItems}");
Console.WriteLine($"平均执行时间 / Average execution time: {stats.AverageExecutionTime}ms");

// 暂停和恢复线程池 / Pause and resume thread pool
pool.Pause();
Console.WriteLine("线程池已暂停 / Pool paused");

// 此时添加的任务会进入挂起队列 / Tasks added now go to suspended queue
var suspendedWork = pool.QueueWorkItem(() =>
{
    Console.WriteLine("暂停期间添加的任务 / Task added during pause");
});

Thread.Sleep(1000);
pool.Resume();
Console.WriteLine("线程池已恢复 / Pool resumed");

// 清空队列 / Clear queue
pool.ClearQueue();
Console.WriteLine("队列已清空 / Queue cleared");

// 停止线程池 / Stop thread pool
pool.Dispose();
Console.WriteLine("线程池已停止 / Pool disposed");
```

## 性能优化建议 / Performance Optimization Tips

1. **合理设置线程池大小 / Reasonable Thread Pool Size**
   - CPU 密集型任务：设置为 CPU 核心数的 1-2 倍
   - CPU-intensive tasks: 1-2x CPU cores
   - IO 密集型任务：设置为 CPU 核心数的 4-8 倍
   - IO-intensive tasks: 4-8x CPU cores

2. **避免长时间运行的任务 / Avoid Long-Running Tasks**
   - 长时间运行的任务会占用线程资源，影响其他任务执行
   - Long-running tasks occupy thread resources, affecting other tasks
   - 考虑将长时间运行的任务拆分为多个短时间任务
   - Consider splitting long-running tasks into multiple short tasks

3. **使用适当的任务优先级 / Use Appropriate Task Priority**
   - 为不同类型的任务设置合理的优先级
   - Set appropriate priorities for different task types
   - 避免过多高优先级任务导致低优先级任务饥饿
   - Avoid too many high-priority tasks causing starvation of low-priority tasks

4. **及时释放资源 / Release Resources Timely**
   - 使用 using 语句确保线程池被正确释放
   - Use using statement to ensure proper disposal of thread pool
   - 避免创建过多的线程池实例
   - Avoid creating too many thread pool instances

5. **配置结果缓存过期 / Configure Result Cache Expiration**
   - 根据任务结果的使用频率配置合适的缓存过期时间
   - Configure appropriate cache expiration based on result usage frequency
   - 启用结果缓存过期可以防止内存泄漏
   - Enable result cache expiration to prevent memory leaks

## 限制 / Limitations

1. **不支持异步/await / No async/await Support**
   - 由于 .NET 2.0 环境限制，不支持 async/await 语法
   - Due to .NET 2.0 environment limitations, async/await syntax is not supported

2. **不支持任务依赖 / No Task Dependencies**
   - 当前版本不支持任务之间的依赖关系
   - Current version does not support dependencies between tasks

3. **自定义取消令牌 / Custom Cancellation Tokens**
   - 使用自定义的 `CancellationToken` 替代 .NET 4.0+ 的 `CancellationToken`
   - Uses custom `CancellationToken` instead of .NET 4.0+ `CancellationToken`

4. **线程池大小限制 / Thread Pool Size Limit**
   - 最大线程数受系统资源限制
   - Maximum thread count is limited by system resources

5. **队列限制 / Queue Limit**
   - 支持队列大小限制，防止内存溢出
   - Queue size limit supported to prevent memory overflow

6. **.NET 2.0 兼容性 / .NET 2.0 Compatibility**
   - 部分现代 .NET 特性无法使用
   - Some modern .NET features are not available

## 技术架构 / Technical Architecture

### 核心组件 / Core Components

- **PowerPool** / **PowerPool**: 线程池主类 / Main thread pool class
- **WorkScheduler** / **WorkScheduler**: 定时任务调度器 / Scheduled task scheduler
- **Group** / **Group**: 工作组管理 / Work group management
- **WorkItem** / **WorkItem**: 工作项封装 / Work item wrapper
- **WorkerThread** / **WorkerThread**: 工作线程 / Worker thread

### 数据结构 / Data Structures

- **LockFreePriorityQueue** / **LockFreePriorityQueue**: 无锁优先级队列 / Lock-free priority queue
- **LockFreeQueue** / **LockFreeQueue**: 无锁队列 / Lock-free queue
- **LockFreeStack** / **LockFreeStack**: 无锁栈 / Lock-free stack
- **ConcurrentSet** / **ConcurrentSet**: 并发集合 / Concurrent set

### 线程安全 / Thread Safety

- 基于 CAS (Compare-And-Swap) 的无锁算法
- CAS-based lock-free algorithms
- 原子操作和内存屏障
- Atomic operations and memory barriers
- 线程安全的计数和状态管理
- Thread-safe counting and state management

## 版本历史 / Version History

### v1.3.0: 功能增强 / Feature Enhancement
- 添加定时任务调度功能 / Added scheduled task scheduling
- 添加工作组管理功能 / Added work group management
- 完善无锁队列实现 / Improved lock-free queue implementation
- 添加结果缓存过期机制 / Added result cache expiration mechanism
- 优化线程回收逻辑 / Optimized thread recycling logic
- 添加完整的中英文注释 / Added complete Chinese and English comments

### v1.2.0: .NET 2.0 完全兼容 / Full .NET 2.0 Compatibility
- 完全兼容 Unity 5.x 和 .NET 2.0 环境
- Fully compatible with Unity 5.x and .NET 2.0 environments
- 修复所有编译错误 / Fixed all compilation errors
- 优化资源管理 / Optimized resource management
- 添加完整测试用例 / Added complete test cases

### v1.1.0: 功能增强 / Feature Enhancement
- 添加队列限制功能 / Added queue limit feature
- 完善线程回收机制 / Improved thread recycling mechanism
- 优化性能监控 / Optimized performance monitoring
- 增强异常处理 / Enhanced exception handling

### v1.0.0: 初始版本 / Initial Version
- 基本线程池功能 / Basic thread pool functionality
- 支持任务优先级调度 / Support task priority scheduling
- 支持任务取消 / Support task cancellation
- 支持异常处理 / Support exception handling
- 支持线程池状态管理 / Support thread pool state management

## 测试 / Testing

项目包含完整的测试套件，位于 `Tests/PowerPool_Comprehensive_Tests.cs`。

The project includes a comprehensive test suite located at `Tests/PowerPool_Comprehensive_Tests.cs`.

运行测试 / Run Tests:
```bash
# 运行综合测试 / Run comprehensive tests
dotnet test

# 或使用示例程序 / Or use the example program
PowerThreadPool_Net20.exe
```

## 技术支持 / Technical Support

如果您在使用过程中遇到问题，请检查：
If you encounter issues, please check:

1. 确保目标框架设置为 .NET Framework 2.0
   Ensure target framework is set to .NET Framework 2.0

2. 检查所有必要的引用是否已添加
   Check if all necessary references are added

3. 查看示例代码了解正确用法
   Review example code for correct usage

4. 检查异常日志获取详细信息
   Check exception logs for detailed information

## 贡献 / Contributing

欢迎提交 Issue 和 Pull Request！
Issues and Pull Requests are welcome!

## 许可证 / License

MIT License

## 致谢 / Acknowledgments

感谢所有为 PowerThreadPool 项目做出贡献的开发者！
Thanks to all developers who contributed to the PowerThreadPool project!

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

## 联系方式 / Contact

如有任何问题或建议，请通过以下方式联系：
For any questions or suggestions, please contact:

- Email: [andyhebear@example.com](mailto:andyhebear@example.com)
- GitHub: [andyhebear](https://github.com/andyhebear)
