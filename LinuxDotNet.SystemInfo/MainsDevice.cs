namespace LinuxDotNet.SystemInfo;

using System;

using static LinuxDotNet.SystemInfo.KernelFileParser;

public sealed class MainsDevice : IDisposable
{
    private const string PowerSupplyPath = "/sys/class/power_supply";

    private readonly string path;

    // Created only when an adapter is found
    private readonly KernelFile? onlineFile;

    private bool disposed;

    public DateTime UpdateAt { get; private set; }

    public bool Supported => !String.IsNullOrEmpty(path);

    public bool Online { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private MainsDevice(string path)
    {
        this.path = path;
        if (Supported)
        {
            onlineFile = new KernelFile(Path.Combine(path, "online"), bufferSize: 64, singleRead: true);
        }
    }

    internal static MainsDevice Create()
    {
        var instance = new MainsDevice(FindAdapter());
        instance.Update();
        return instance;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        onlineFile?.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (onlineFile is null)
        {
            return false;
        }

        // An unreadable file is offline
        Online = onlineFile.Read() && TrimEnd(onlineFile.Content).SequenceEqual("1"u8);

        UpdateAt = DateTime.Now;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string FindAdapter()
    {
        if (Directory.Exists(PowerSupplyPath))
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(PowerSupplyPath))
                {
                    var file = Path.Combine(dir, "type");
                    if (FileHelper.TryReadText(file, out var type) &&
                        type.AsSpan().Trim().StartsWith("Mains", StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Ignore
            }
        }

        return string.Empty;
    }
}
