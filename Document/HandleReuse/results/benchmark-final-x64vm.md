```

BenchmarkDotNet v0.15.8, Linux Rocky Linux 10.2 (Red Quartz)
AMD Ryzen 7 5700G with Radeon Graphics 3.01GHz, 1 CPU, 2 logical cores and 1 physical core
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean            | Error          | StdDev         | Median          | Allocated |
|------------------ |----------------:|---------------:|---------------:|----------------:|----------:|
| SystemStat        |   9,873.4726 ns |    251.2244 ns |    724.8398 ns |   9,864.0042 ns |         - |
| MemoryStat        |  10,602.3465 ns |    234.9650 ns |    685.4040 ns |  10,709.2549 ns |         - |
| VirtualMemoryStat |  31,158.7491 ns |    777.0807 ns |  2,279.0434 ns |  31,984.7681 ns |         - |
| LoadAverage       |   2,900.6269 ns |     57.2769 ns |    147.8500 ns |   2,923.2112 ns |         - |
| Uptime            |   2,719.0034 ns |     61.0791 ns |    180.0931 ns |   2,744.0236 ns |         - |
| FileHandleStat    |   2,895.1063 ns |     70.5570 ns |    194.3344 ns |   2,890.7588 ns |         - |
| DiskStat          |  12,156.9152 ns |    315.0698 ns |    914.0746 ns |  12,138.3993 ns |         - |
| NetworkStat       |  23,788.6729 ns |    475.8726 ns |  1,395.6520 ns |  24,236.4507 ns |         - |
| TcpStat           | 285,876.5721 ns |  7,066.4456 ns | 20,501.0414 ns | 288,302.8096 ns |       2 B |
| Tcp6Stat          | 282,724.1340 ns |  6,304.3836 ns | 18,489.6670 ns | 285,307.8652 ns |       2 B |
| WirelessStat      |       2.5864 ns |      0.1005 ns |      0.2965 ns |       2.6124 ns |         - |
| ProcessSummary    | 142,041.8556 ns |  3,062.8635 ns |  8,982.8490 ns | 144,247.3274 ns |     209 B |
| CpuDevice         |       1.5799 ns |      0.0672 ns |      0.1851 ns |       1.6202 ns |         - |
| BatteryDevice     |       2.1844 ns |      0.0998 ns |      0.2926 ns |       2.1991 ns |         - |
| MainsDevice       |       0.3142 ns |      0.0467 ns |      0.1370 ns |       0.3274 ns |         - |
| HardwareMonitors  |       0.6657 ns |      0.0516 ns |      0.1504 ns |       0.6771 ns |         - |
| FileSystemUsage   |   1,616.9341 ns |     51.8717 ns |    143.7363 ns |   1,623.1647 ns |         - |
| All               | 878,118.9897 ns | 22,974.3245 ns | 66,652.6861 ns | 881,802.9297 ns |     212 B |
