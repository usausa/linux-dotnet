namespace LinuxDotNet.Disk;

using System.Buffers;

using static LinuxDotNet.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private const int SmartDataSize = 512;
    private const int MaxAttributes = 30;
    private const int TableOffset = 2;
    private const int EntrySize = 12;

    private int fd;

    private byte[] buffer;

    private bool use16;

    public bool LastUpdate { get; private set; }

    public SmartGeneric(string devicePath)
    {
        fd = open(devicePath, O_RDONLY);
        buffer = ArrayPool<byte>.Shared.Rent(SmartDataSize);
    }

    public void Dispose()
    {
        if (fd >= 0)
        {
            _ = close(fd);
            fd = -1;
        }

        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }
    }

    public unsafe bool Update()
    {
        if (fd < 0)
        {
            LastUpdate = false;
            return false;
        }

        fixed (byte* ptr = buffer)
        {
            if (!use16)
            {
                // Try PT12 first
                buffer.AsSpan().Clear();
                if (ReadPassThrough12(fd, ptr))
                {
                    LastUpdate = true;
                    return true;
                }
            }

            // Try PT16 as fallback
            buffer.AsSpan().Clear();
            if (ReadPassThrough16(fd, ptr))
            {
                use16 = true;
                LastUpdate = true;
                return true;
            }
        }

        LastUpdate = false;
        return false;
    }

    private static unsafe bool ReadPassThrough12(int fd, byte* data)
    {
        var cdb = stackalloc byte[12];
        cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0xD0;      // features (SMART_READ_DATA)
        cdb[4] = 0x01;      // sector_count
        cdb[5] = 0x00;      // lba_low
        cdb[6] = 0x4F;      // lba_mid (SMART signature)
        cdb[7] = 0xC2;      // lba_high (SMART signature)
        cdb[8] = 0x00;      // device
        cdb[9] = 0xB0;      // command (SMART)
        cdb[10] = 0x00;
        cdb[11] = 0x00;

        var sense = stackalloc byte[64];
        return ExecuteScsiCommand(fd, cdb, 12, data, SmartDataSize, sense, 64);
    }

    private static unsafe bool ReadPassThrough16(int fd, byte* data)
    {
        var cdb = stackalloc byte[16];
        cdb[0] = 0x85;      // ATA PASS-THROUGH(16)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0x00;
        cdb[4] = 0xD0;      // features (SMART_READ_DATA)
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

        var sense = stackalloc byte[64];
        return ExecuteScsiCommand(fd, cdb, 16, data, SmartDataSize, sense, 64);
    }

    private static unsafe bool ExecuteScsiCommand(int fd, byte* cdb, int cdbLen, byte* data, int dataLen, byte* sense, int senseLen)
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
            mx_sb_len = (byte)senseLen,
            timeout = 5000
        };

        if (ioctl(fd, SG_IO, &io) < 0)
        {
            return false;
        }

        return ((io.info & SG_INFO_OK_MASK) == SG_INFO_OK) && (io is { status: 0, host_status: 0, driver_status: 0 });
    }

    public IReadOnlyList<SmartId> GetSupportedIds()
    {
        var list = new List<SmartId>();

        if (buffer.Length == 0)
        {
            return list;
        }

        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            var id = buffer[offset];
            if (id != 0 && (id != 0xff))
            {
                list.Add((SmartId)id);
            }
        }

        return list;
    }

    public SmartAttribute? GetAttribute(SmartId id)
    {
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
                    RawValue = Raw48ToU64(buffer, rawOffset)
                };
            }
        }

        return null;
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
