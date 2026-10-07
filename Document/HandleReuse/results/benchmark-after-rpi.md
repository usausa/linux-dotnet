```

BenchmarkDotNet v0.15.8, Linux Debian GNU/Linux 12 (bookworm)
Cortex-A72
  [Host] : .NET 10.0.12 (10.0.12, 10.0.1226.42308), Arm64 RyuJIT armv8.0-a

Toolchain=InProcessEmitToolchain  

```
| Method            | Mean              | Error          | StdDev          | Median            | Allocated |
|------------------ |------------------:|---------------:|----------------:|------------------:|----------:|
| SystemStat        |    26,289.8485 ns |     53.7072 ns |      50.2377 ns |    26,291.4919 ns |         - |
| MemoryStat        |    14,192.6621 ns |     21.8949 ns |      19.4093 ns |    14,196.3575 ns |         - |
| VirtualMemoryStat |    28,563.1231 ns |     55.7232 ns |      46.5314 ns |    28,569.6438 ns |         - |
| LoadAverage       |     3,892.5038 ns |     15.8987 ns |      14.0937 ns |     3,891.5126 ns |         - |
| Uptime            |     3,565.7471 ns |      9.9980 ns |       9.3522 ns |     3,563.9534 ns |         - |
| FileHandleStat    |     4,673.2636 ns |     89.8065 ns |     110.2904 ns |     4,652.7816 ns |         - |
| DiskStat          |    87,618.7520 ns |    254.3957 ns |     237.9619 ns |    87,615.4675 ns |       1 B |
| NetworkStat       |    15,441.0479 ns |     22.4755 ns |      21.0236 ns |    15,442.4896 ns |         - |
| TcpStat           |   318,104.8079 ns |     32.0337 ns |      26.7496 ns |   318,107.5078 ns |       2 B |
| Tcp6Stat          |   312,222.0879 ns |     75.4342 ns |      70.5612 ns |   312,201.4321 ns |       2 B |
| WirelessStat      | 1,821,402.1765 ns | 48,131.7556 ns | 141,917.5775 ns | 1,931,452.2861 ns |       9 B |
| ProcessSummary    |   249,357.7186 ns |    218.3213 ns |     170.4509 ns |   249,288.6868 ns |     210 B |
| CpuDevice         |     8,848.7603 ns |    173.4636 ns |     243.1717 ns |     8,832.3256 ns |         - |
| BatteryDevice     |         3.3360 ns |      0.0020 ns |       0.0017 ns |         3.3355 ns |         - |
| MainsDevice       |         0.5562 ns |      0.0003 ns |       0.0002 ns |         0.5562 ns |         - |
| HardwareMonitors  |     2,833.9174 ns |     50.9140 ns |      47.6250 ns |     2,823.0861 ns |         - |
| FileSystemUsage   |     1,880.6920 ns |      0.8766 ns |       0.7320 ns |     1,880.4940 ns |         - |
| All               | 3,115,460.3926 ns |  6,942.3908 ns |   5,797.2108 ns | 3,117,119.3652 ns |     226 B |
