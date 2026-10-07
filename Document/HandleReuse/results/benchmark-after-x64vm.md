```

BenchmarkDotNet v0.15.8, Linux Rocky Linux 10.2 (Red Quartz)
AMD Ryzen 7 5700G with Radeon Graphics 3.77GHz, 1 CPU, 2 logical cores and 1 physical core
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean              | Error           | StdDev          | Gen0   | Allocated |
|------------------ |------------------:|----------------:|----------------:|-------:|----------:|
| SystemStat        |    10,594.6203 ns |     394.0484 ns |   1,161.8606 ns |      - |         - |
| MemoryStat        |    11,257.3706 ns |     366.9177 ns |   1,081.8651 ns |      - |         - |
| VirtualMemoryStat |    30,807.3324 ns |     976.5006 ns |   2,833.0055 ns |      - |         - |
| LoadAverage       |     2,899.1883 ns |      80.0207 ns |     232.1545 ns |      - |         - |
| Uptime            |     2,843.2459 ns |      86.6336 ns |     254.0812 ns |      - |         - |
| FileHandleStat    |     3,031.3554 ns |      93.8321 ns |     275.1934 ns |      - |         - |
| DiskStat          |    12,142.8717 ns |     289.6523 ns |     849.5002 ns |      - |         - |
| NetworkStat       |    24,083.2944 ns |     477.6670 ns |   1,370.5163 ns |      - |         - |
| TcpStat           |   287,997.9765 ns |   7,546.7442 ns |  21,653.0255 ns |      - |       2 B |
| Tcp6Stat          |   285,630.6749 ns |   5,703.5335 ns |  14,824.2429 ns |      - |       2 B |
| WirelessStat      |    18,009.8666 ns |     376.5687 ns |   1,110.3212 ns | 0.0610 |     736 B |
| ProcessSummary    | 3,401,812.1322 ns |  67,604.0442 ns | 189,569.0016 ns |      - |   20664 B |
| CpuDevice         |         2.2170 ns |       0.1345 ns |       0.3965 ns |      - |         - |
| BatteryDevice     |         2.1727 ns |       0.1391 ns |       0.4058 ns |      - |         - |
| MainsDevice       |         0.3829 ns |       0.0626 ns |       0.1815 ns |      - |         - |
| HardwareMonitors  |         0.6481 ns |       0.0570 ns |       0.1618 ns |      - |         - |
| FileSystemUsage   |     1,720.7129 ns |      62.6110 ns |     184.6100 ns |      - |         - |
| All               | 4,270,207.5336 ns | 159,344.8322 ns | 464,816.3885 ns |      - |   21545 B |
