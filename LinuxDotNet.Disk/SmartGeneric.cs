namespace LinuxDotNet.Disk;

using System.Buffers;
using System.Runtime.InteropServices;

using static LinuxDotNet.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private const int SmartDataSize = 512;
    private const int MaxAttributes = 30;
    private const int TableOffset = 2;
    private const int EntrySize = 12;
    private const int SenseSize = 64;

    private const byte SmartReadData = 0xD0;
    private const byte SmartReadThresholds = 0xD1;
    private const byte SmartReturnStatus = 0xDA;

    private readonly SafeFileDescriptor handle;

    private readonly int openError;

    private readonly byte[] thresholds = new byte[SmartDataSize];

    private byte[] buffer;

    private bool thresholdsLoaded;

    private bool use16;

    private bool disposed;

    public bool LastUpdate { get; private set; }

    public int LastError { get; private set; }

    public SmartAssessment Assessment { get; private set; }

    public SmartGeneric(string devicePath)
    {
        var fd = open(devicePath, O_RDONLY);
        openError = fd < 0 ? Marshal.GetLastPInvokeError() : 0;
        handle = new SafeFileDescriptor(fd);
        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(SmartDataSize);
            buffer.AsSpan(0, SmartDataSize).Clear();
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        handle.Dispose();

        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }

        disposed = true;
    }

    // ReSharper disable once RedundantUnsafeContext
    public unsafe bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (handle.IsInvalid)
        {
            LastError = openError;
            LastUpdate = false;
            return false;
        }

        fixed (byte* ptr = buffer)
        {
            if (!ReadData(ptr, SmartReadData))
            {
                LastUpdate = false;
                return false;
            }
        }

        if (!thresholdsLoaded)
        {
            fixed (byte* ptr = thresholds)
            {
                thresholdsLoaded = ReadData(ptr, SmartReadThresholds);
            }
        }

        Assessment = ReadAssessment();

        LastError = 0;
        LastUpdate = true;
        return true;
    }

    // ReSharper disable once RedundantUnsafeContext
    private unsafe bool ReadData(byte* data, byte feature)
    {
        if (!use16)
        {
            // Try PT12 first
            new Span<byte>(data, SmartDataSize).Clear();
            if (ReadPassThrough12(data, feature))
            {
                return true;
            }
        }

        // Try PT16 as fallback
        new Span<byte>(data, SmartDataSize).Clear();
        if (ReadPassThrough16(data, feature))
        {
            use16 = true;
            return true;
        }

        return false;
    }

    private unsafe bool ReadPassThrough12(byte* data, byte feature)
    {
        var cdb = stackalloc byte[12];
        cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = feature;   // features
        cdb[4] = 0x01;      // sector_count
        cdb[5] = 0x00;      // lba_low
        cdb[6] = 0x4F;      // lba_mid (SMART signature)
        cdb[7] = 0xC2;      // lba_high (SMART signature)
        cdb[8] = 0x00;      // device
        cdb[9] = 0xB0;      // command (SMART)
        cdb[10] = 0x00;
        cdb[11] = 0x00;

        var sense = stackalloc byte[SenseSize];
        return ExecuteScsiCommand(cdb, 12, data, SmartDataSize, sense);
    }

    private unsafe bool ReadPassThrough16(byte* data, byte feature)
    {
        var cdb = stackalloc byte[16];
        cdb[0] = 0x85;      // ATA PASS-THROUGH(16)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0x00;
        cdb[4] = feature;   // features
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

        var sense = stackalloc byte[SenseSize];
        return ExecuteScsiCommand(cdb, 16, data, SmartDataSize, sense);
    }

    private unsafe bool ExecuteScsiCommand(byte* cdb, int cdbLen, byte* data, int dataLen, byte* sense)
    {
        var io = new sg_io_hdr_t
        {
            interface_id = 'S',
            cmdp = cdb,
            cmd_len = (byte)cdbLen,
            dxferp = data,
            dxfer_len = (uint)dataLen,
            dxfer_direction = SG_DXFER_FROM_DEV,
            sbp = sense,
            mx_sb_len = SenseSize,
            timeout = 5000
        };

        if (ioctl(handle.Descriptor, SG_IO, &io) < 0)
        {
            LastError = Marshal.GetLastPInvokeError();
            return false;
        }

        if (((io.info & SG_INFO_OK_MASK) == SG_INFO_OK) && (io is { status: 0, host_status: 0, driver_status: 0 }))
        {
            return true;
        }

        LastError = EIO;
        return false;
    }

    private unsafe SmartAssessment ReadAssessment()
    {
        var cdb = stackalloc byte[16];
        new Span<byte>(cdb, 16).Clear();
        int cdbLen;
        if (use16)
        {
            cdb[0] = 0x85;      // ATA PASS-THROUGH(16)
            cdb[1] = 3 << 1;    // protocol = 3 (Non-data)
            cdb[2] = 0x20;      // ck_cond=1
            cdb[4] = SmartReturnStatus;
            cdb[10] = 0x4F;     // lba_mid (SMART signature)
            cdb[12] = 0xC2;     // lba_high (SMART signature)
            cdb[14] = 0xB0;     // command (SMART)
            cdbLen = 16;
        }
        else
        {
            cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
            cdb[1] = 3 << 1;    // protocol = 3 (Non-data)
            cdb[2] = 0x20;      // ck_cond=1
            cdb[3] = SmartReturnStatus;
            cdb[6] = 0x4F;      // lba_mid (SMART signature)
            cdb[7] = 0xC2;      // lba_high (SMART signature)
            cdb[9] = 0xB0;      // command (SMART)
            cdbLen = 12;
        }

        var sense = stackalloc byte[SenseSize];
        new Span<byte>(sense, SenseSize).Clear();
        var io = new sg_io_hdr_t
        {
            interface_id = 'S',
            cmdp = cdb,
            cmd_len = (byte)cdbLen,
            dxfer_direction = SG_DXFER_NONE,
            sbp = sense,
            mx_sb_len = SenseSize,
            timeout = 5000
        };

        if (ioctl(handle.Descriptor, SG_IO, &io) < 0)
        {
            return SmartAssessment.Unknown;
        }

        return ParseAssessment(new ReadOnlySpan<byte>(sense, Math.Min((int)io.sb_len_wr, SenseSize)));
    }

    private static SmartAssessment ParseAssessment(ReadOnlySpan<byte> sense)
    {
        if (sense.Length < 8)
        {
            return SmartAssessment.Unknown;
        }

        var responseCode = sense[0] & 0x7F;
        if (responseCode is 0x72 or 0x73)
        {
            var end = Math.Min(sense.Length, 8 + sense[7]);
            var offset = 8;
            while (offset + 1 < end)
            {
                if ((sense[offset] == 0x09) && (offset + 14 <= end))
                {
                    return ToAssessment(sense[offset + 9], sense[offset + 11]);
                }

                offset += sense[offset + 1] + 2;
            }

            return SmartAssessment.Unknown;
        }

        if ((responseCode is 0x70 or 0x71) && (sense.Length >= 12))
        {
            return ToAssessment(sense[10], sense[11]);
        }

        return SmartAssessment.Unknown;
    }

    private static SmartAssessment ToAssessment(byte lbaMid, byte lbaHigh) =>
        (lbaMid, lbaHigh) switch
        {
            (0x4F, 0xC2) => SmartAssessment.Passed,
            (0xF4, 0x2C) => SmartAssessment.Failed,
            _ => SmartAssessment.Unknown
        };

    public IReadOnlyList<SmartId> GetSupportedIds()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var list = new List<SmartId>();

        if (buffer.Length == 0)
        {
            return list;
        }

        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            var id = buffer[offset];
            if ((id != 0) && (id != 0xff))
            {
                list.Add((SmartId)id);
            }
        }

        return list;
    }

    public SmartAttribute? GetAttribute(SmartId id)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (buffer.Length == 0)
        {
            return null;
        }

        var target = (byte)id;
        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            if (buffer[offset] == target)
            {
                var rawOffset = offset + 5;
                return new SmartAttribute
                {
                    Id = buffer[offset],
                    Flags = (short)(buffer[offset + 1] | (buffer[offset + 2] << 8)),
                    CurrentValue = buffer[offset + 3],
                    WorstValue = buffer[offset + 4],
                    Threshold = FindThreshold(target),
                    RawValue = Raw48ToU64(buffer, rawOffset)
                };
            }
        }

        return null;
    }

    private byte FindThreshold(byte id)
    {
        if (!thresholdsLoaded)
        {
            return 0;
        }

        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            if (thresholds[offset] == id)
            {
                return thresholds[offset + 1];
            }
        }

        return 0;
    }

    private static ulong Raw48ToU64(byte[] data, int offset)
    {
        var v = 0ul;
        for (var i = 5; i >= 0; i--)
        {
            v = (v << 8) | data[offset + i];
        }
        return v;
    }
}
