```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Core i5-8365U CPU 1.60GHz (Max: 0.40GHz) (Coffee Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean           | Error         | StdDev          | Median         | Allocated |
|------------------ |---------------:|--------------:|----------------:|---------------:|----------:|
| SystemStat        |    15,529.4 ns |     333.50 ns |       983.34 ns |    16,029.7 ns |         - |
| MemoryStat        |     7,294.8 ns |      28.57 ns |        26.73 ns |     7,305.7 ns |         - |
| VirtualMemoryStat |    21,982.7 ns |      80.00 ns |        70.92 ns |    21,964.9 ns |         - |
| LoadAverage       |     1,543.1 ns |       6.38 ns |         5.97 ns |     1,544.1 ns |         - |
| Uptime            |     1,711.3 ns |       7.65 ns |         7.16 ns |     1,713.1 ns |         - |
| FileHandleStat    |     2,120.1 ns |      12.59 ns |        11.77 ns |     2,121.0 ns |         - |
| DiskStat          |    22,035.4 ns |      84.48 ns |        79.02 ns |    22,077.2 ns |         - |
| NetworkStat       |     5,963.3 ns |      18.87 ns |        17.65 ns |     5,959.7 ns |         - |
| TcpStat           |   188,021.0 ns |   1,035.23 ns |       968.35 ns |   187,797.9 ns |       1 B |
| Tcp6Stat          |   188,660.3 ns |     568.79 ns |       532.04 ns |   188,621.2 ns |       1 B |
| WirelessStat      |   880,052.0 ns |  48,242.70 ns |   138,417.36 ns |   884,769.3 ns |       5 B |
| ProcessSummary    |   116,049.1 ns |   2,265.53 ns |     2,119.18 ns |   116,093.2 ns |     209 B |
| CpuDevice         |     6,394.3 ns |     125.93 ns |       139.97 ns |     6,312.5 ns |         - |
| BatteryDevice     |     4,727.3 ns |      92.06 ns |       106.02 ns |     4,701.5 ns |         - |
| MainsDevice       |   124,992.3 ns |   1,307.38 ns |     1,091.72 ns |   125,443.6 ns |       1 B |
| HardwareMonitors  | 1,527,555.7 ns | 112,628.54 ns |   332,087.80 ns | 1,631,829.8 ns |      10 B |
| FileSystemUsage   |       635.3 ns |      12.58 ns |        15.45 ns |       629.7 ns |         - |
| All               | 5,464,617.5 ns | 491,151.24 ns | 1,448,170.61 ns | 5,697,022.4 ns |     245 B |
