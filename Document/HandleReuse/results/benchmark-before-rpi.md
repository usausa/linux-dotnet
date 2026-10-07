```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
Cortex-A72
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean              | Error          | StdDev         | Gen0     | Allocated |
|------------------ |------------------:|---------------:|---------------:|---------:|----------:|
| SystemStat        |     55,456.661 ns |     48.4379 ns |     37.8171 ns |   4.1504 |    8905 B |
| MemoryStat        |     54,943.638 ns |     43.5160 ns |     38.5758 ns |   5.1880 |   10913 B |
| VirtualMemoryStat |     74,501.826 ns |     80.8994 ns |     71.7152 ns |   7.4463 |   15794 B |
| LoadAverage       |     30,620.897 ns |     38.3287 ns |     33.9774 ns |   3.7842 |    7921 B |
| Uptime            |     30,554.234 ns |     48.4321 ns |     42.9338 ns |   3.7231 |    7793 B |
| FileHandleStat    |     34,186.504 ns |    118.0770 ns |    104.6722 ns |   3.7842 |    7921 B |
| DiskStat          |    141,538.398 ns |    161.4961 ns |    143.1621 ns |   5.3711 |   11306 B |
| NetworkStat       |     56,935.084 ns |     79.1309 ns |     66.0778 ns |   4.2114 |    8897 B |
| TcpStat           |    360,919.645 ns |    211.6199 ns |    187.5955 ns |   4.3945 |    9235 B |
| Tcp6Stat          |    352,882.620 ns |    200.8728 ns |    167.7379 ns |   3.9063 |    8699 B |
| WirelessStat      |  2,012,515.048 ns | 10,635.5966 ns |  9,428.1794 ns |   3.9063 |    8186 B |
| ProcessSummary    |  9,351,221.454 ns | 12,275.1049 ns | 11,482.1407 ns | 828.1250 | 1758620 B |
| CpuDevice         |    161,916.708 ns |    472.1930 ns |    394.3025 ns |  14.6484 |   31140 B |
| BatteryDevice     |          5.005 ns |      0.0008 ns |      0.0007 ns |        - |         - |
| MainsDevice       |          2.224 ns |      0.0003 ns |      0.0003 ns |        - |         - |
| HardwareMonitors  |     42,883.412 ns |    122.9017 ns |    102.6285 ns |   3.6621 |    7777 B |
| FileSystemUsage   |      1,927.460 ns |      1.0897 ns |      0.9660 ns |        - |         - |
| All               | 12,932,949.188 ns | 33,733.4305 ns | 29,903.8075 ns | 890.6250 | 1893055 B |
