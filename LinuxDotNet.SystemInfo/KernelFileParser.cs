namespace LinuxDotNet.SystemInfo;

using System.Buffers.Text;

// Parses the content of a KernelFile as bytes, without creating strings
internal static class KernelFileParser
{
    // Magnitude limits of the signed types (the negative side is one larger)
    private const ulong Int64PositiveLimit = Int64.MaxValue;
    private const ulong Int64NegativeLimit = Int64PositiveLimit + 1;
    private const ulong Int32PositiveLimit = Int32.MaxValue;
    private const ulong Int32NegativeLimit = Int32PositiveLimit + 1;

    //--------------------------------------------------------------------------------
    // Line and token
    //--------------------------------------------------------------------------------

    // Takes the next line without '\n' from remaining (the last line may have no '\n')
    public static bool TryReadLine(ref ReadOnlySpan<byte> remaining, out ReadOnlySpan<byte> line)
    {
        if (remaining.IsEmpty)
        {
            line = default;
            return false;
        }

        var index = remaining.IndexOf((byte)'\n');
        if (index < 0)
        {
            line = remaining;
            remaining = default;
        }
        else
        {
            line = remaining[..index];
            remaining = remaining[(index + 1)..];
        }

        return true;
    }

    // Skips spaces and tabs and takes the next token from span (empty when there is no token left)
    public static ReadOnlySpan<byte> NextToken(ref ReadOnlySpan<byte> span)
    {
        var start = span.IndexOfAnyExcept((byte)' ', (byte)'\t');
        if (start < 0)
        {
            span = default;
            return default;
        }

        var token = span[start..];
        var end = token.IndexOfAny((byte)' ', (byte)'\t');
        if (end < 0)
        {
            span = default;
            return token;
        }

        span = token[end..];
        return token[..end];
    }

    // Removes the trailing '\n', '\r', ' ' and '\t'
    public static ReadOnlySpan<byte> TrimEnd(ReadOnlySpan<byte> span)
    {
        var length = span.Length;
        while ((length > 0) && (span[length - 1] is (byte)'\n' or (byte)'\r' or (byte)' ' or (byte)'\t'))
        {
            length--;
        }

        return span[..length];
    }

    //--------------------------------------------------------------------------------
    // Number
    //--------------------------------------------------------------------------------

    // The Parse methods return 0 for an empty span, an invalid character or an overflow (the same as TryParse ? value : 0)

    // Accepts one leading '+' ('-' is 0 as UInt64.TryParse gives 0 or fails for it)
    public static ulong ParseUInt64(ReadOnlySpan<byte> span) =>
        !SkipSign(ref span) && TryParseDigits(span, UInt64.MaxValue, out var value) ? value : 0;

    // Accepts one leading '-' or '+'
    public static long ParseInt64(ReadOnlySpan<byte> span)
    {
        var negative = SkipSign(ref span);
        if (!TryParseDigits(span, negative ? Int64NegativeLimit : Int64PositiveLimit, out var value))
        {
            return 0;
        }

        return negative ? unchecked(-(long)value) : (long)value;
    }

    // Accepts one leading '-' or '+'
    public static int ParseInt32(ReadOnlySpan<byte> span)
    {
        var negative = SkipSign(ref span);
        if (!TryParseDigits(span, negative ? Int32NegativeLimit : Int32PositiveLimit, out var value))
        {
            return 0;
        }

        return negative ? unchecked(-(int)value) : (int)value;
    }

    // Hexadecimal digits (0-9, a-f, A-F) without a prefix
    public static ulong ParseHex(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return 0;
        }

        var value = 0UL;
        foreach (var c in span)
        {
            var digit = GetHexDigit(c);
            if ((digit < 0) || (value > (UInt64.MaxValue >> 4)))
            {
                return 0;
            }

            value = (value << 4) | (uint)digit;
        }

        return value;
    }

    // The whole span must be a number
    public static double ParseDouble(ReadOnlySpan<byte> span) =>
        Utf8Parser.TryParse(span, out double value, out var consumed) && (consumed == span.Length) ? value : 0;

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    // Removes a leading '-' or '+' and returns whether it was '-'
    private static bool SkipSign(ref ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty || (span[0] is not ((byte)'-' or (byte)'+')))
        {
            return false;
        }

        var negative = span[0] == (byte)'-';
        span = span[1..];
        return negative;
    }

    // Parses decimal digits up to max (false for an empty span, a non-digit or a value over max)
    private static bool TryParseDigits(ReadOnlySpan<byte> span, ulong max, out ulong value)
    {
        value = 0;
        if (span.IsEmpty)
        {
            return false;
        }

        foreach (var c in span)
        {
            var digit = (uint)(c - '0');
            if ((digit > 9) || (value > ((max - digit) / 10)))
            {
                value = 0;
                return false;
            }

            value = (value * 10) + digit;
        }

        return true;
    }

    private static int GetHexDigit(byte value) =>
        value switch
        {
            >= (byte)'0' and <= (byte)'9' => value - '0',
            >= (byte)'a' and <= (byte)'f' => value - 'a' + 10,
            >= (byte)'A' and <= (byte)'F' => value - 'A' + 10,
            _ => -1
        };
}
