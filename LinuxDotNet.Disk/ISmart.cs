namespace LinuxDotNet.Disk;

public interface ISmart
{
    bool LastUpdate { get; }

    int LastError { get; }

    bool Update();
}
