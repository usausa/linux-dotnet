```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Core i5-8365U CPU 1.60GHz (Max: 0.40GHz) (Coffee Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean           | Error         | StdDev        | Median         | Gen0      | Allocated |
|------------------ |---------------:|--------------:|--------------:|---------------:|----------:|----------:|
| SystemStat        |    27,876.2 ns |     245.46 ns |     241.08 ns |    27,808.1 ns |    5.4321 |   17185 B |
| MemoryStat        |    17,052.7 ns |     332.93 ns |     455.72 ns |    16,786.4 ns |    3.8757 |   12249 B |
| VirtualMemoryStat |    35,081.5 ns |     756.11 ns |   2,205.61 ns |    36,234.0 ns |    6.3477 |   20010 B |
| LoadAverage       |     8,412.4 ns |      53.42 ns |      49.97 ns |     8,428.5 ns |    2.5177 |    7921 B |
| Uptime            |     8,453.6 ns |      29.46 ns |      27.55 ns |     8,454.5 ns |    2.4719 |    7793 B |
| FileHandleStat    |     8,938.7 ns |      48.10 ns |      44.99 ns |     8,951.2 ns |    2.5177 |    7921 B |
| DiskStat          |    38,108.3 ns |     199.23 ns |     186.36 ns |    38,088.5 ns |    3.5400 |   11225 B |
| NetworkStat       |    19,042.9 ns |      96.41 ns |      90.18 ns |    19,054.4 ns |    2.8381 |    8921 B |
| TcpStat           |   203,674.9 ns |     769.03 ns |     719.35 ns |   203,649.1 ns |    2.9297 |    9554 B |
| Tcp6Stat          |   200,957.8 ns |     647.79 ns |     605.95 ns |   200,979.8 ns |    2.6855 |    9074 B |
| WirelessStat      |   922,043.4 ns |  34,801.98 ns | 102,614.44 ns |   943,018.0 ns |    1.9531 |    8190 B |
| ProcessSummary    | 3,653,196.2 ns |  71,045.93 ns |  72,958.94 ns | 3,690,570.9 ns |  933.5938 | 2931415 B |
| CpuDevice         |   152,825.5 ns |   3,024.79 ns |   4,709.23 ns |   153,604.2 ns |   21.2402 |   66798 B |
| BatteryDevice     |    77,295.7 ns |   1,291.74 ns |   1,268.66 ns |    77,063.2 ns |   15.0146 |   47468 B |
| MainsDevice       |   150,370.7 ns |   2,895.62 ns |   3,662.03 ns |   149,165.5 ns |    2.4414 |    7882 B |
| HardwareMonitors  | 2,460,251.7 ns | 186,220.62 ns | 549,075.73 ns | 2,635,177.8 ns |   23.4375 |   85539 B |
| FileSystemUsage   |       630.3 ns |      12.55 ns |      15.41 ns |       622.9 ns |         - |         - |
| All               | 9,741,689.5 ns | 193,446.46 ns | 416,413.95 ns | 9,718,743.6 ns | 1031.2500 | 3259305 B |
