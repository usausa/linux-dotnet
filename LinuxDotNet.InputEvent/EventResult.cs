namespace LinuxDotNet.InputEvent;

#pragma warning disable CA1028
public enum EventType : ushort
{
    Sync = 0,
    Key = 1,
    Relative = 2,
    Absolute = 3,
    Misc = 4
}
#pragma warning restore CA1028

public enum EventValue
{
    Released = 0,
    Pressed = 1,
    Repeat = 2
}

public struct EventResult : IEquatable<EventResult>
{
    public TimeSpan Timestamp { get; set; }

    public EventType Type { get; set; }

    public ushort Code { get; set; }

    public int Value { get; set; }

    public bool Equals(EventResult other) =>
        Timestamp == other.Timestamp &&
       Type == other.Type &&
       Code == other.Code &&
       Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is EventResult other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Timestamp, Type, Code, Value);

    public static bool operator ==(EventResult left, EventResult right) => left.Equals(right);

    public static bool operator !=(EventResult left, EventResult right) => !left.Equals(right);

    public override string ToString() =>
        $"Timestamp={Timestamp.TotalSeconds:F6}s, Type={Type}, Code={Code}, Value={Value}";
}
