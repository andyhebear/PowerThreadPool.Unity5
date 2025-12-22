# PowerThreadPool_Net20

基于PowerThreadPool接口与设计的.NET 2.0兼容版本，专为Unity 5.x设计。

<<<<<<< HEAD
## 特性

- **.NET 2.0兼容**: 兼容Unity 5.x的.NET 2.0运行时
- **线程池管理**: 高效的工作线程池实现
- **工作队列**: 线程安全的工作队列管理
- **异常处理**: 完整的异常处理机制
- **中文注解**: 所有代码都包含详细的中文和英文注释
=======
<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-7-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

A comprehensive and efficient low-contention thread pool for easily managing both sync and async workloads. It provides granular work control, flexible concurrency, and robust error handling.  

## Documentation
Access the Wiki in [English](https://github.com/ZjzMisaka/PowerThreadPool/wiki) | [中文](https://github.com/ZjzMisaka/PowerThreadPool.zh-CN.Wiki/wiki) | [日本語](https://github.com/ZjzMisaka/PowerThreadPool.ja-JP.Wiki/wiki).  
Visit the [DeepWiki](https://deepwiki.com/ZjzMisaka/PowerThreadPool) for more information.  

## Pack
```ps1
powershell -File build.ps1 -Version {Version}
```
>>>>>>> 5fead13c3e741674785f5b6002f49b4043c5abf4

## 项目结构

<<<<<<< HEAD
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
=======
## Features
- [Sync | Async](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Sync-Async)
- [Pool Control | Work Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control)
    - [Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Pause](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Resume](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#pause-resume-stop)
    - [Force Stop](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#force-stop)
    - [Wait](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#wait)
    - [Fetch](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#fetch)
    - [Cancel](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Control#cancel)
- [Divide And Conquer](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Divide-And-Conquer)
- [Thread Pool Sizing](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing)
    - [Idle Thread Scheduled Destruction](https://github.com/ZjzMisaka/PowerThreadPool/wiki/DestroyThreadOption)
    - [Thread Starvation Countermeasures (Long-running Work Support)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Thread-Pool-Sizing#thread-starvation)
- [Work Callback | Default Callback](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Callback)
- [Rejection Policy](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Rejection-Policy)
- [Parallel Execution](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution)
    - [For](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#For)
    - [ForEach](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#ForEach)
    - [Watch](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Parallel-Execution#Watch)
- [Work Priority | Thread Priority](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Priority)
- [Error Handling](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Error-Handling)
    - [Retry](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Retry)
- [Work Timeout | Cumulative Work Timeout](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Timeout)
- [Work Dependency](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Dependency)
- [Work Group](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Group)
    - [Group Control](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Work-Group#group-control)
    - [Group Relation](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Group-Relation)
- [Events](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Events)
- [Runtime Status](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Runtime-Status)
- [Running Timer](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Running-Timer)
- [Queue Type (FIFO | LIFO | Deque | Custom)](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Queue-Type)
- [Load Balancing](https://en.wikipedia.org/wiki/Work_stealing)
- [Low-Contention Design](https://en.wikipedia.org/wiki/Non-blocking_algorithm)

## Getting started
### Out-of-the-box: Run a simple work
PTP is designed to be out-of-the-box. For simple works, you can get started without any complex configuration.  
```csharp
PowerPool powerPool = new PowerPool();
// Sync
powerPool.QueueWorkItem(() => 
{
    // Do something
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
});
>>>>>>> 5fead13c3e741674785f5b6002f49b4043c5abf4
```

### 高级使用

```csharp
<<<<<<< HEAD
using PowerThreadPool_Net20;
using PowerThreadPool_Net20.Options;
using PowerThreadPool_Net20.Results;

// 创建自定义工作选项
var workOptions = new WorkOption_Net20
=======
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
// Sync
powerPool.QueueWorkItem(() => 
>>>>>>> 5fead13c3e741674785f5b6002f49b4043c5abf4
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
<<<<<<< HEAD
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
=======
    // Callback of the work
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
}, (res) =>
{
    // Callback of the work
});
```

### With option
```csharp
PowerPool powerPool = new PowerPool(new PowerPoolOption() { /* Some options */ });
// Sync
powerPool.QueueWorkItem(() => 
{
    // Do something
    return result;
}, new WorkOption()
{
    // Some options
});
// Async
powerPool.QueueWorkItemAsync(async () =>
{
    // Do something
    // await ...;
}, new WorkOption()
{
    // Some options
});
```

### Reference
``` csharp
WorkID QueueWorkItem<T1, ...>(Action<T1, ...> action, T1 param1, ..., *);
WorkID QueueWorkItem(Action action, *);
WorkID QueueWorkItem(Action<object[]> action, object[] param, *);
WorkID QueueWorkItem<T1, ..., TResult>(Func<T1, ..., TResult> function, T1 param1, ..., *);
WorkID QueueWorkItem<TResult>(Func<TResult> function, *);
WorkID QueueWorkItem<TResult>(Func<object[], TResult> function, object[] param, *);
WorkID QueueWorkItemAsync(Func<Task> asyncFunc, *);
WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, *);
WorkID QueueWorkItemAsync(Func<Task> asyncFunc, out Task task, *);
WorkID QueueWorkItemAsync<TResult>(Func<Task<TResult>> asyncFunc, out Task<ExecuteResult<TResult>> task, *);
```
- Asterisk (*) denotes an optional parameter, either a WorkOption or a delegate (`Action<ExecuteResult<object>>` or `Action<ExecuteResult<TResult>>`), depending on whether the first parameter is an Action or a Func. 
- In places where you see ellipses (...), you can provide up to five generic type parameters.

## More
[Testing And Performance Analysis](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Testing-And-Performance-Analysis) | [Feature Comparison](https://github.com/ZjzMisaka/PowerThreadPool/wiki/Feature-Comparison)  
**Get involved**: [Join our growing community](https://github.com/ZjzMisaka/PowerThreadPool/discussions/258)  

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
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

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
>>>>>>> 5fead13c3e741674785f5b6002f49b4043c5abf4
