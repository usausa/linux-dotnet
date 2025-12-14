namespace Example.Video4Linux2.AvaloniaApp.Helper;

using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
internal static class ImageHelper
{
    public static unsafe void ConvertYUYV2RGBA(ReadOnlySpan<byte> yuyv, Span<byte> rgba)
    {
        fixed (byte* yuyvPtr = yuyv)
        fixed (byte* rgbaPtr = rgba)
        {
            var src = yuyvPtr;
            var dst = rgbaPtr;
            var end = yuyvPtr + yuyv.Length;

            while (src < end)
            {
                int y0 = src[0];
                int u = src[1];
                int y1 = src[2];
                int v = src[3];

                var c0 = y0 - 16;
                var c1 = y1 - 16;
                var d = u - 128;
                var e = v - 128;

                // Calculate common values
                var c0298 = 298 * c0;
                var c1298 = 298 * c1;
                var de = -(100 * d) - (208 * e);
                var e409 = 409 * e;
                var d516 = 516 * d;

                // Pixel1 R,G,B,A
                dst[0] = Clamp((c0298 + e409 + 128) >> 8);
                dst[1] = Clamp((c0298 + de + 128) >> 8);
                dst[2] = Clamp((c0298 + d516 + 128) >> 8);
                dst[3] = 255;

                // Pixel2 R,G,B,A
                dst[4] = Clamp((c1298 + e409 + 128) >> 8);
                dst[5] = Clamp((c1298 + de + 128) >> 8);
                dst[6] = Clamp((c1298 + d516 + 128) >> 8);
                dst[7] = 255;

                src += 4;
                dst += 8;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clamp(int value)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value > 255)
        {
            return 255;
        }
        return (byte)value;
    }
}
// ReSharper restore InconsistentNaming
