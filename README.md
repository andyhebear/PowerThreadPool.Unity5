# PowerThreadPool_Net20

基于PowerThreadPool接口与设计的.NET 2.0兼容版本，专为Unity 5.x设计。

## 特性

- **.NET 2.0兼容**: 兼容Unity 5.x的.NET 2.0运行时
- **线程池管理**: 高效的工作线程池实现
- **工作队列**: 线程安全的工作队列管理
- **异常处理**: 完整的异常处理机制
- **中文注解**: 所有代码都包含详细的中文和英文注释

## 项目结构

```
PowerThreadPool_Net20/
├── Core/                          # 核心实现
│   └── PowerPool_Net20.cs        # 主线程池类
├── Works/                        # 工作相关类
│   ├── WorkBase_Net20.cs         # 工作基类
│   ├── WorkID_Net20.cs           # 工作ID
│   ├── WorkItem_Net20.cs         # 工作项
│   └── WorkerThread_Net20.cs     # 工作线程
├── Collections/                  # 集合类
│   └── ConcurrentQueue_Net20.cs  # 线程安全队列
├── Constants/                    # 常量定义
│   ├── PoolStates_Net20.cs       # 线程池状态
│   ├── WorkerStates_Net20.cs     # 工作线程状态
│   ├── WorkStealability_Net20.cs # 工作可窃取性
│   ├── WorkHeldStates_Net20.cs   # 工作保持状态
│   ├── CanCancel_Net20.cs        # 是否可取消
│   ├── CanGetWork_Net20.cs       # 是否可获取工作
│   ├── CanCreateNewWorker_Net20.cs # 是否可创建新工作线程
│   ├── CanDeleteRedundantWorker_Net20.cs # 是否可删除冗余工作线程
│   ├── CanDispose_Net20.cs       # 是否可销毁
│   ├── CanForceStop_Net20.cs     # 是否可强制停止
│   ├── CanWatch_Net20.cs         # 是否可监视
│   ├── DependencyStatus_Net20.cs # 依赖状态
│   └── WatchStates_Net20.cs      # 监视状态
├── Exceptions/                   # 异常类
│   ├── WorkExceptionBase_Net20.cs      # 工作异常基类
│   ├── WorkRejectedException_Net20.cs  # 工作被拒绝异常
│   └── CycleDetectedException_Net20.cs # 循环检测异常
├── Options/                     # 选项类
│   ├── PowerPoolOption_Net20.cs  # 线程池选项
│   └── WorkOption_Net20.cs      # 工作选项
├── Results/                     # 结果类
│   ├── ExecuteResult_Net20.cs   # 执行结果
│   └── EventArguments_Net20.cs  # 事件参数
├── Helpers/                     # 辅助类
│   └── ThreadSafeHelper_Net20.cs # 线程安全辅助类
└── PowerThreadPool_Net20.csproj # 项目文件
```

## 使用示例

### 基本使用

```csharp
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Options;

// 创建线程池选项
var options = new PowerPoolOption_Net20
{
    MinWorkerThreads = 2,
    MaxWorkerThreads = 10,
    IdleTimeout = TimeSpan.FromMinutes(1)
};

// 创建线程池
using (var threadPool = new PowerPool_Net20(options))
{
    // 添加工作项
    var workId = threadPool.QueueWorkItem(() => 
    {
        Console.WriteLine("Hello from worker thread!");
        return "Work completed";
    });

    // 等待工作完成
    var result = threadPool.WaitForWork(workId);
    Console.WriteLine("Result: " + result.Result);
}
```

### 高级使用

```csharp
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;

// 创建自定义工作选项
var workOptions = new WorkOption_Net20
{
    CanCancel = true,
    Timeout = TimeSpan.FromSeconds(30)
};

// 添加带选项的工作项
var workId = threadPool.QueueWorkItem(
    () => DoComplexWork(),
    workOptions,
    "WorkGroup1" // 工作组名称
);

// 监听工作完成事件
threadPool.WorkCompleted += (sender, args) =>
{
    Console.WriteLine($"Work {args.WorkId} completed with result: {args.Result}");
};

// 取消工作
threadPool.CancelWork(workId);
```

## 核心类说明

### PowerPool_Net20
主线程池类，负责管理工作线程池、工作队列和执行调度。

### WorkItem_Net20
表示一个工作项，包含要执行的方法、参数和选项。

### WorkerThread_Net20
工作线程类，从队列中获取工作项并执行。

### ConcurrentQueue_Net20
.NET 2.0兼容的线程安全队列实现。

### WorkExceptionBase_Net20
工作相关异常的基类。

## 兼容性说明

此项目专为.NET 2.0设计，移除了以下.NET 4.0+特性：
- `System.Collections.Concurrent`命名空间
- `Task<T>`和async/await语法
- `Tuple`类型
- 一些现代线程同步原语

替代方案：
- 使用自定义的`ConcurrentQueue_Net20`替代`ConcurrentQueue<T>`
- 使用传统的回调和事件机制替代async/await
- 使用自定义结构体替代`Tuple`

## 编译要求

- .NET Framework 2.0
- Unity 5.6或更高版本
- C# 3.0编译器（Unity内置）

## 许可证

本项目基于原始PowerThreadPool项目进行适配，遵循相同的许可证条款。