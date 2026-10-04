namespace LinuxDotNet.Disk;

public interface ISmartGeneric : ISmart
{
    SmartAssessment Assessment { get; }

    IReadOnlyList<SmartId> GetSupportedIds();

    SmartAttribute? GetAttribute(SmartId id);
}
