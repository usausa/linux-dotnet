// ReSharper disable IdentifierTypo
namespace LinuxDotNet.Disk;

using System.Runtime.InteropServices;

public sealed class SmartTest
{
    private static ushort Le16(byte[] p, int offset)
    {
        return (ushort)(p[offset] | (p[offset + 1] << 8));
    }

    private static ulong Raw48ToU64(byte[] raw, int offset)
    {
        ulong v = 0;
        for (var i = 5; i >= 0; i--)
        {
            v = (v << 8) | raw[offset + i];
        }
        return v;
    }

    private static void DumpHex(string tag, byte[] p, int n)
    {
        Console.Error.Write($"{tag} ({n}):");
        for (var i = 0; i < n; i++)
        {
            Console.Error.Write($" {p[i]:x2}");
        }
        Console.Error.WriteLine();
    }

    private static unsafe int DoSgIo(int fd, byte[] cdb, byte[] data, int dxferLen, int dxferDir, byte[] sense, int timeoutMs, bool verboseOnError)
    {
        fixed (byte* cdbPtr = cdb)
        fixed (byte* dataPtr = data)
        fixed (byte* sensePtr = sense)
        {
            var io = new NativeMethods.sg_io_hdr_t
            {
                interface_id = 'S',
                cmdp = cdbPtr,
                cmd_len = (byte)cdb.Length,
                dxferp = dataPtr,
                dxfer_len = (uint)dxferLen,
                dxfer_direction = dxferDir,
                sbp = sensePtr,
                mx_sb_len = (byte)sense.Length,
                timeout = (uint)timeoutMs
            };

            if (NativeMethods.ioctl(fd, NativeMethods.SG_IO, &io) < 0)
            {
                var err = Marshal.GetLastWin32Error();
                if (verboseOnError)
                {
                    Console.Error.WriteLine($"SG_IO ioctl failed: errno={err}");
                }
                return -err;
            }

            var ok = ((io.info & NativeMethods.SG_INFO_OK_MASK) == NativeMethods.SG_INFO_OK) &&
                     (io.status == 0) &&
                     (io.host_status == 0) &&
                     (io.driver_status == 0);

            if (!ok)
            {
                if (verboseOnError)
                {
                    Console.Error.WriteLine("SG_IO failed:");
                    Console.Error.WriteLine($"  status=0x{io.status:x} host=0x{io.host_status:x} " +
                        $"driver=0x{io.driver_status:x} info=0x{io.info:x} sb_len={io.sb_len_wr} resid={io.resid}");
                    DumpHex("  CDB", cdb, cdb.Length);
                    if (io.sb_len_wr > 0)
                    {
                        DumpHex("  SENSE", sense, io.sb_len_wr);
                    }
                }
                return -NativeMethods.EIO;
            }

            return 0;
        }
    }

    private static int SmartReadDataPt12V2(int fd, byte[] data512, bool verbose)
    {
        var cdb = new byte[12];
        cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0xD0;      // features
        cdb[4] = 0x01;      // sector_count
        cdb[5] = 0x00;      // lba_low
        cdb[6] = 0x4F;      // lba_mid (SMART signature)
        cdb[7] = 0xC2;      // lba_high (SMART signature)
        cdb[8] = 0x00;      // device
        cdb[9] = 0xB0;      // command (SMART)
        cdb[10] = 0x00;
        cdb[11] = 0x00;

        var sense = new byte[64];
        return DoSgIo(fd, cdb, data512, 512, NativeMethods.SG_DXFER_FROM_DEV, sense, 10000, verbose);
    }

    private static int SmartReadDataPt16V2(int fd, byte[] data512, bool verbose)
    {
        var cdb = new byte[16];
        cdb[0] = 0x85;      // ATA PASS-THROUGH(16)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0x00;
        cdb[4] = 0xD0;      // features
        cdb[5] = 0x00;
        cdb[6] = 0x01;      // sector_count
        cdb[7] = 0x00;
        cdb[8] = 0x00;      // lba_low
        cdb[9] = 0x00;
        cdb[10] = 0x4F;     // lba_mid (SMART signature)
        cdb[11] = 0x00;
        cdb[12] = 0xC2;     // lba_high (SMART signature)
        cdb[13] = 0x00;     // device
        cdb[14] = 0xB0;     // command (SMART)
        cdb[15] = 0x00;

        var sense = new byte[64];
        return DoSgIo(fd, cdb, data512, 512, NativeMethods.SG_DXFER_FROM_DEV, sense, 10000, verbose);
    }

    private static void DumpAttrs(byte[] smart)
    {
        Console.WriteLine($"SMART data revision (raw): 0x{smart[1]:x2}{smart[0]:x2}");

        const int tableOff = 2;
        const int entrySz = 12;
        const int n = 30;

        Console.WriteLine("\nID  FLAG   CUR WOR  RAW(HEX 6B)          RAW(U64)");
        Console.WriteLine("---------------------------------------------------------");

        for (var i = 0; i < n; i++)
        {
            var offset = tableOff + (i * entrySz);
            var id = smart[offset];
            if (id == 0x00 || id == 0xff)
            {
                continue;
            }

            var flags = Le16(smart, offset + 1);
            var cur = smart[offset + 3];
            var wor = smart[offset + 4];
            var rawOffset = offset + 5;

            Console.WriteLine($"{id,3}  0x{flags:x4}  {cur,3} {wor,3}  " +
                $"{smart[rawOffset]:x2} {smart[rawOffset + 1]:x2} {smart[rawOffset + 2]:x2} " +
                $"{smart[rawOffset + 3]:x2} {smart[rawOffset + 4]:x2} {smart[rawOffset + 5]:x2}  " +
                $"{Raw48ToU64(smart, rawOffset),10}");
        }

        uint sum = 0;
        for (var i = 0; i < 512; i++)
        {
            sum += smart[i];
        }
        Console.WriteLine($"\nChecksum: sum % 256 = {sum & 0xff} (0 means OK)");
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: SmartReader /dev/sdX");
            return 2;
        }

        var path = args[0];
        var fd = NativeMethods.open(path, NativeMethods.O_RDONLY | NativeMethods.O_NONBLOCK);
        if (fd < 0)
        {
            Console.Error.WriteLine($"open failed: {Marshal.GetLastWin32Error()}");
            return 1;
        }

        var smart = new byte[512];

        // Try PT12 first
        var rc = SmartReadDataPt12V2(fd, smart, true);
        if (rc != 0)
        {
            Console.Error.WriteLine("PT12 failed, trying PT16...");
            Array.Clear(smart, 0, smart.Length);
            rc = SmartReadDataPt16V2(fd, smart, true);
        }

        if (rc != 0)
        {
            Console.Error.WriteLine($"SMART READ DATA failed rc={rc}");
            _ = NativeMethods.close(fd);
            return 1;
        }

        DumpAttrs(smart);
        _ = NativeMethods.close(fd);
        return 0;
    }
}
