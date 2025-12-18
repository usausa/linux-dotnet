namespace Example.InputEvent.ConsoleApp;

using System.Text;

using LinuxDotNet.InputEvent;

internal sealed class BarcodeReader : IDisposable
{
    public event Action<string>? BarcodeScanned;

    private static readonly Dictionary<ushort, char> KeyMap = new()
    {
        { 2, '1' }, { 3, '2' }, { 4, '3' }, { 5, '4' }, { 6, '5' },
        { 7, '6' }, { 8, '7' }, { 9, '8' }, { 10, '9' }, { 11, '0' },
        { 16, 'Q' }, { 17, 'W' }, { 18, 'E' }, { 19, 'R' }, { 20, 'T' },
        { 21, 'Y' }, { 22, 'U' }, { 23, 'I' }, { 24, 'O' }, { 25, 'P' },
        { 30, 'A' }, { 31, 'S' }, { 32, 'D' }, { 33, 'F' }, { 34, 'G' },
        { 35, 'H' }, { 36, 'J' }, { 37, 'K' }, { 38, 'L' },
        { 44, 'Z' }, { 45, 'X' }, { 46, 'C' }, { 47, 'V' }, { 48, 'B' },
        { 49, 'N' }, { 50, 'M' },
        { 57, ' ' },
        { 12, '-' },
        { 13, '=' },
        { 26, '[' }, { 27, ']' }, { 39, ';' }, { 40, '\'' },
        { 41, '`' }, { 43, '\\' }, { 51, ',' }, { 52, '.' }, { 53, '/' }
    };

    private readonly EventDevice device;

    private readonly StringBuilder buffer = new();

    public bool IsOpen => device.IsOpen;

    public BarcodeReader(string path)
    {
        device = new EventDevice(path);
    }

    public void Dispose()
    {
        device.Dispose();
    }

    // TODO

    public bool Start()
    {
        return device.Open(true);
    }

    public void Stop()
    {
        device.Close();
    }

    public bool Process()
    {
        try
        {
            if (device.Read(out var result, 1000))
            {
                if ((result.Type == EventType.Key) && ((EventValue)result.Value == EventValue.Pressed))
                {
                    if (result.Code == 28) // Enter
                    {
                        BarcodeScanned?.Invoke(buffer.ToString());
                        buffer.Clear();
                    }
                    else if (KeyMap.TryGetValue(result.Code, out var c))
                    {
                        buffer.Append(c);
                    }
                }
            }

            return true;
        }
        catch (IOException e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}
