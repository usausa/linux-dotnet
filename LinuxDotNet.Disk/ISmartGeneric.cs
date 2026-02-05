namespace LinuxDotNet.Disk;

public interface ISmartGeneric : ISmart
{
    IReadOnlyList<SmartId> GetSupportedIds();

    SmartAttribute? GetAttribute(SmartId id);
}
