namespace LinuxDotNet.SystemInfo;

using System.Globalization;
using System.Text.RegularExpressions;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class CpuCore
{
    private readonly KernelFile file;

    private bool closed;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public ulong Frequency { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    // Takes over the file (closed by the owner CpuDevice)
    internal CpuCore(string name, KernelFile file)
    {
        Name = name;
        this.file = file;
        Update();
    }

    internal void Close()
    {
        closed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(closed, this);

        if (!file.Read())
        {
            return false;
        }

        Frequency = ParseUInt64(TrimEnd(file.Content));

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed class CpuPower
{
    private readonly KernelFile file;

    private bool closed;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public ulong Energy { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    // Takes over the file (closed by the owner CpuDevice)
    internal CpuPower(string name, KernelFile file)
    {
        Name = name;
        this.file = file;
        Update();
    }

    internal void Close()
    {
        closed = true;
        file.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(closed, this);

        if (!file.Read())
        {
            return false;
        }

        Energy = ParseUInt64(TrimEnd(file.Content));

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed partial class CpuDevice : IDisposable
{
    private const string CpuPath = "/sys/devices/system/cpu";

    private readonly CpuCore[] cores;

    private readonly CpuPower[] powers;

    private bool disposed;

    public IReadOnlyList<CpuCore> Cores => cores;

    public IReadOnlyList<CpuPower> Powers => powers;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private CpuDevice(CpuCore[] cores, CpuPower[] powers)
    {
        this.cores = cores;
        this.powers = powers;
    }

    internal static CpuDevice Create() => new(GetCores(), GetPowers());

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var core in cores)
        {
            core.Close();
        }

        foreach (var power in powers)
        {
            power.Close();
        }
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [GeneratedRegex(@"^cpu\d+$")]
    private static partial Regex CpuCoreRegex();

    // ReSharper disable StringLiteralTypo
    private static CpuCore[] GetCores()
    {
        string[] dirs;
        try
        {
            dirs = Directory.GetDirectories(CpuPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var cores = new List<CpuCore>();

        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            if (!CpuCoreRegex().IsMatch(name))
            {
                continue;
            }

            var path = Path.Combine(dir, "cpufreq", "scaling_cur_freq");
            if (!File.Exists(path))
            {
                continue;
            }

            // A core whose scaling_cur_freq could not be opened is not added. A core that is offline is added: its file can be
            // opened (the read fails with EBUSY), and it is read again when the core comes online
            var file = new KernelFile(path, bufferSize: 64, singleRead: true);
            var core = new CpuCore(name, file);
            if (!file.Opened)
            {
                file.Dispose();
                continue;
            }

            cores.Add(core);
        }

#pragma warning disable IDE0028
        return cores.OrderBy(static x => Int32.TryParse(x.Name.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : Int32.MaxValue).ToArray();
#pragma warning restore IDE0028
    }
    // ReSharper restore StringLiteralTypo

    // ReSharper disable StringLiteralTypo
    private static CpuPower[] GetPowers()
    {
        var powers = new List<CpuPower>();

        var intelPath = "/sys/class/powercap/intel-rapl:0";
        try
        {
            if (Directory.Exists(intelPath))
            {
                AddCpuPower(powers, intelPath);
                foreach (var dir in Directory.GetDirectories(intelPath).Where(static x => Path.GetFileName(x).StartsWith("intel-rapl:0:", StringComparison.Ordinal)).OrderBy(static x => x))
                {
                    AddCpuPower(powers, dir);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ignore
        }

#pragma warning disable IDE0028
        return powers.ToArray();
#pragma warning restore IDE0028
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static void AddCpuPower(List<CpuPower> powers, string path)
    {
        if (!FileHelper.TryReadTrimmedText(Path.Combine(path, "name"), out var name) || String.IsNullOrEmpty(name))
        {
            return;
        }

        var energyPath = Path.Combine(path, "energy_uj");
        if (!File.Exists(energyPath))
        {
            return;
        }

        // A power whose energy_uj could not be opened is not added (energy_uj is readable only by root on some systems, so
        // Powers is empty for other users)
        var file = new KernelFile(energyPath, bufferSize: 64, singleRead: true);
        var power = new CpuPower(name, file);
        if (!file.Opened)
        {
            file.Dispose();
            return;
        }

        powers.Add(power);
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public void Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        foreach (var core in cores)
        {
            core.Update();
        }

        foreach (var power in powers)
        {
            power.Update();
        }
    }
}
