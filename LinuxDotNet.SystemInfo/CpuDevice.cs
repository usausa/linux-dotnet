namespace LinuxDotNet.SystemInfo;

using System.Text.RegularExpressions;

public sealed class CpuCore
{
    private readonly string frequencyPath;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public long Frequency { get; private set; }

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
        Frequency = Int64.TryParse(File.ReadAllText(frequencyPath).AsSpan().Trim(), out var value) ? value : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed class CpuPower
{
    private readonly string energyPath;

    public DateTime UpdateAt { get; private set; }

    public string Name { get; }

    public long Energy { get; private set; }

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
        Energy = Int64.TryParse(File.ReadAllText(energyPath).AsSpan().Trim(), out var value) ? value : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}

public sealed class CpuDevice
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

    // ReSharper disable StringLiteralTypo
    private static CpuCore[] GetCores()
    {
        var cores = new List<CpuCore>();

        var pattern = new Regex(@"^cpu\d+$");
        foreach (var dir in Directory.GetDirectories("/sys/devices/system/cpu"))
        {
            if (!pattern.IsMatch(Path.GetFileName(dir)))
            {
                continue;
            }

            var path = Path.Combine(dir, "cpufreq", "scaling_cur_freq");
            if (!File.Exists(path))
            {
                continue;
            }

            var name = Path.GetFileName(dir);

            cores.Add(new CpuCore(name, path));
        }

        return cores.OrderBy(static x => x.Name).ToArray();
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
        var name = ReadFile(Path.Combine(path, "name"));
        if (String.IsNullOrEmpty(name))
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

    private static string ReadFile(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path).Trim();
        }

        return string.Empty;
    }

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
