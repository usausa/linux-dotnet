namespace LinuxDotNet.SystemInfo;

using System.Globalization;
using System.Text.RegularExpressions;

public sealed class CpuCore
{
    private readonly string frequencyPath;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public ulong Frequency { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal CpuCore(string name, string frequencyPath)
    {
        Name = name;
        this.frequencyPath = frequencyPath;
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        if (!FileHelper.TryReadText(frequencyPath, out var text))
        {
            return false;
        }

        Frequency = UInt64.TryParse(text.AsSpan().Trim(), CultureInfo.InvariantCulture, out var value) ? value : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed class CpuPower
{
    private readonly string energyPath;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public ulong Energy { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal CpuPower(string name, string energyPath)
    {
        Name = name;
        this.energyPath = energyPath;
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        if (!FileHelper.TryReadText(energyPath, out var text))
        {
            return false;
        }

        Energy = UInt64.TryParse(text.AsSpan().Trim(), CultureInfo.InvariantCulture, out var value) ? value : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed partial class CpuDevice
{
    public IReadOnlyList<CpuCore> Cores { get; }

    public IReadOnlyList<CpuPower> Powers { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal CpuDevice()
    {
        Cores = GetCores();
        Powers = GetPowers();
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [GeneratedRegex(@"^cpu\d+$")]
    private static partial Regex CpuCoreRegex();

    // ReSharper disable StringLiteralTypo
    private static CpuCore[] GetCores()
    {
        var cores = new List<CpuCore>();

        foreach (var dir in Directory.GetDirectories("/sys/devices/system/cpu"))
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

            cores.Add(new CpuCore(name, path));
        }

        return cores.OrderBy(static x => Int32.TryParse(x.Name.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : Int32.MaxValue).ToArray();
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
        catch (UnauthorizedAccessException)
        {
        }

        return powers.ToArray();
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

        powers.Add(new CpuPower(name, energyPath));
    }
    // ReSharper restore StringLiteralTypo

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public void Update()
    {
        foreach (var core in Cores)
        {
            core.Update();
        }

        foreach (var power in Powers)
        {
            power.Update();
        }
    }
}
