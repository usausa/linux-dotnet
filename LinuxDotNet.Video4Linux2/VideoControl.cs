namespace LinuxDotNet.Video4Linux2;

using System.Globalization;
using System.Text;

using static LinuxDotNet.Video4Linux2.NativeMethods;

#pragma warning disable IDE0051
public enum VideoControlType
{
    Unknown = 0,
    Numeric = 1,
    Boolean = 2,
    Menu = 3,
    Button = 4,
    Numeric64 = 5,
    ControlClass = 6,
    Text = 7,
    Bitmask = 8,
    NumericMenu = 9
}

[Flags]
public enum VideoControlAttributes
{
    None = 0,
    Disabled = 0x0001,
    Grabbed = 0x0002,
    ReadOnly = 0x0004,
    Update = 0x0008,
    Inactive = 0x0010,
    Slider = 0x0020,
    WriteOnly = 0x0040,
    Volatile = 0x0080,
    HasPayload = 0x0100,
    ExecuteOnWrite = 0x0200,
    ModifyLayout = 0x0400,
    DynamicArray = 0x0800,
    HasWhichMinMax = 0x1000
}

public static class VideoControlId
{
    public const int Brightness = 0x00980900;
    public const int Contrast = 0x00980901;
    public const int Saturation = 0x00980902;
    public const int Hue = 0x00980903;
    public const int AutoWhiteBalance = 0x0098090C;
    public const int DoWhiteBalance = 0x0098090D;
    public const int RedBalance = 0x0098090E;
    public const int BlueBalance = 0x0098090F;
    public const int Gamma = 0x00980910;
    public const int Exposure = 0x00980911;
    public const int AutoGain = 0x00980912;
    public const int Gain = 0x00980913;
    public const int HorizontalFlip = 0x00980914;
    public const int VerticalFlip = 0x00980915;
    public const int PowerLineFrequency = 0x00980918;
    public const int HueAuto = 0x00980919;
    public const int WhiteBalanceTemperature = 0x0098091A;
    public const int Sharpness = 0x0098091B;
    public const int BacklightCompensation = 0x0098091C;
    public const int AutoBrightness = 0x00980920;
    public const int Rotate = 0x00980922;
    public const int ExposureAuto = 0x009A0901;
    public const int ExposureAbsolute = 0x009A0902;
    public const int ExposureAutoPriority = 0x009A0903;
    public const int PanRelative = 0x009A0904;
    public const int TiltRelative = 0x009A0905;
    public const int PanAbsolute = 0x009A0908;
    public const int TiltAbsolute = 0x009A0909;
    public const int FocusAbsolute = 0x009A090A;
    public const int FocusRelative = 0x009A090B;
    public const int FocusAuto = 0x009A090C;
    public const int ZoomAbsolute = 0x009A090D;
    public const int ZoomRelative = 0x009A090E;
    public const int ZoomContinuous = 0x009A090F;
    public const int Privacy = 0x009A0910;
}
#pragma warning restore IDE0051

public sealed class VideoControlMenuItem
{
    public int Index { get; }

    public string Name { get; }

    public long Value { get; }

    internal VideoControlMenuItem(int index, string name, long value)
    {
        Index = index;
        Name = name;
        Value = value;
    }

    public override string ToString() => Name.Length > 0 ? Name : Value.ToString(CultureInfo.InvariantCulture);
}

public sealed class VideoControl
{
    public int Id { get; }

    public string Name { get; }

    public VideoControlType Type { get; }

    public int Minimum { get; }

    public int Maximum { get; }

    public int Step { get; }

    public int DefaultValue { get; }

    public VideoControlAttributes Attributes { get; }

    public int? Value { get; }

    public IReadOnlyList<VideoControlMenuItem> MenuItems { get; }

    public bool IsReadOnly => (Attributes & VideoControlAttributes.ReadOnly) != 0;

    public bool IsInactive => (Attributes & VideoControlAttributes.Inactive) != 0;

    internal VideoControl(int id, string name, VideoControlType type, int minimum, int maximum, int step, int defaultValue, VideoControlAttributes attributes, int? value, IReadOnlyList<VideoControlMenuItem> menuItems)
    {
        Id = id;
        Name = name;
        Type = type;
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
        DefaultValue = defaultValue;
        Attributes = attributes;
        Value = value;
        MenuItems = menuItems;
    }

    public override string ToString() => $"{Name} (0x{Id:X8})";

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    internal static unsafe List<VideoControl> Query(int fd)
    {
        var controls = new List<VideoControl>();

        var next = V4L2_CTRL_FLAG_NEXT_CTRL;
        while (true)
        {
            var query = default(v4l2_queryctrl);
            query.id = next;
            if (ioctl(fd, VIDIOC_QUERYCTRL, (IntPtr)(&query)) < 0)
            {
                break;
            }

            next = query.id | V4L2_CTRL_FLAG_NEXT_CTRL;
            if (((query.flags & V4L2_CTRL_FLAG_DISABLED) != 0) || (query.type == V4L2_CTRL_TYPE_CTRL_CLASS))
            {
                continue;
            }

            var id = (int)query.id;
            var type = (VideoControlType)query.type;
            var readable = ((query.flags & V4L2_CTRL_FLAG_WRITE_ONLY) == 0) &&
                           (type is VideoControlType.Numeric or VideoControlType.Boolean or VideoControlType.Menu or VideoControlType.NumericMenu or VideoControlType.Bitmask);
            var menuItems = (query.type == V4L2_CTRL_TYPE_MENU) || (query.type == V4L2_CTRL_TYPE_INTEGER_MENU)
                ? QueryMenu(fd, query.id, query.minimum, query.maximum, query.type == V4L2_CTRL_TYPE_INTEGER_MENU)
                : [];

            controls.Add(new VideoControl(
                id,
                Encoding.ASCII.GetString(query.name, v4l2_queryctrl.NameSize).TrimEnd('\0'),
                type,
                query.minimum,
                query.maximum,
                query.step,
                query.default_value,
                (VideoControlAttributes)query.flags,
                readable ? GetValue(fd, id) : null,
                menuItems));
        }

        return controls;
    }

    internal static int? GetValue(int fd, int id)
    {
        var control = default(v4l2_control);
        control.id = (uint)id;
        return ioctl(fd, VIDIOC_G_CTRL, (IntPtr)(&control)) < 0 ? null : control.value;
    }

    internal static bool SetValue(int fd, int id, int value)
    {
        var control = default(v4l2_control);
        control.id = (uint)id;
        control.value = value;
        return ioctl(fd, VIDIOC_S_CTRL, (IntPtr)(&control)) >= 0;
    }

    private static unsafe List<VideoControlMenuItem> QueryMenu(int fd, uint id, int minimum, int maximum, bool integer)
    {
        var items = new List<VideoControlMenuItem>();
        for (var index = minimum; index <= maximum; index++)
        {
            var menu = default(v4l2_querymenu);
            menu.id = id;
            menu.index = (uint)index;
            if (ioctl(fd, VIDIOC_QUERYMENU, (IntPtr)(&menu)) < 0)
            {
                continue;
            }

            items.Add(integer
                ? new VideoControlMenuItem(index, string.Empty, menu.value)
                : new VideoControlMenuItem(index, Encoding.ASCII.GetString(menu.name, v4l2_querymenu.NameSize).TrimEnd('\0'), index));
        }

        return items;
    }
}
