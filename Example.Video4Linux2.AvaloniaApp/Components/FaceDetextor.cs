namespace Example.Video4Linux2.AvaloniaApp.Components;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

#pragma warning disable CA1815
public readonly struct FaceBox
{
    public float Left { get; init; }

    public float Top { get; init; }

    public float Right { get; init; }

    public float Bottom { get; init; }

    public float Confidence { get; init; }
}
#pragma warning restore CA1815

public sealed class FaceDetector : IDisposable
{
    private static readonly ConfidenceDescendingComparer Comparer = new();

    private readonly InferenceSession session;

    private readonly DenseTensor<float> inputTensor;

#pragma warning disable CA1002
    public List<FaceBox> DetectedFaceBoxes { get; } = new();
#pragma warning restore CA1002

    public int ModelWidth { get; }

    public int ModelHeight { get; }

    public FaceDetector(string modelPath)
    {
#pragma warning disable CA2000
        session = new InferenceSession(modelPath, new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR,
            EnableCpuMemArena = true,
            EnableMemoryPattern = true,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        });
#pragma warning restore CA2000

        // Get input tensor shape
        var inputMetadata = session.InputMetadata.First().Value;
        var dimensions = inputMetadata.Dimensions;

        // [batch, channels, height, width]
        if (dimensions.Length >= 4)
        {
            ModelHeight = dimensions[2];
            ModelWidth = dimensions[3];
        }
        else
        {
            throw new ArgumentException("Invalid model type");
        }

        inputTensor = new DenseTensor<float>(new[] { 1, 3, ModelHeight, ModelWidth });
    }

    public void Dispose()
    {
        session.Dispose();
    }

    public void Detect(ReadOnlySpan<byte> image, int width, int height, float confidenceThreshold = 0.7f, float iouThreshold = 0.3f)
    {
        // Resize and normalize
        if ((width == ModelWidth) && (height == ModelHeight))
        {
            CopyDirectToTensor(image, inputTensor, width, height);
        }
        else
        {
            ResizeBilinearDirectToTensor(image, inputTensor, width, height, ModelWidth, ModelHeight);
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(session.InputMetadata.First().Key, inputTensor)
        };

        using var results = session.Run(inputs);
        var outputList = results.ToList();

        var scoresTensor = outputList[0].AsTensor<float>();
        var boxesTensor = outputList[1].AsTensor<float>();
        var numBoxes = scoresTensor.Dimensions[1];

        DetectedFaceBoxes.Clear();
        if (numBoxes == 0)
        {
            return;
        }

        var detectionBuffer = ArrayPool<FaceBox>.Shared.Rent(numBoxes);
        try
        {
            var detectionCount = 0;
            for (var i = 0; i < numBoxes; i++)
            {
                var faceScore = scoresTensor[0, i, 1];
                if (faceScore > confidenceThreshold)
                {
                    detectionBuffer[detectionCount++] = new FaceBox
                    {
                        Left = boxesTensor[0, i, 0],
                        Top = boxesTensor[0, i, 1],
                        Right = boxesTensor[0, i, 2],
                        Bottom = boxesTensor[0, i, 3],
                        Confidence = faceScore
                    };
                }
            }

            ApplyNMS(detectionBuffer.AsSpan(0, detectionCount), iouThreshold);
        }
        finally
        {
            ArrayPool<FaceBox>.Shared.Return(detectionBuffer);
        }
    }

    private void ApplyNMS(ReadOnlySpan<FaceBox> boxes, float iouThreshold)
    {
        if (boxes.IsEmpty)
        {
            return;
        }

        var count = boxes.Length;

        var boxArray = ArrayPool<FaceBox>.Shared.Rent(count);
        var suppressed = ArrayPool<bool>.Shared.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                boxArray[i] = boxes[i];
                suppressed[i] = false;
            }

            // Sort boxes by confidence in descending order
            Array.Sort(boxArray, 0, count, Comparer);

            for (var i = 0; i < count; i++)
            {
                if (suppressed[i])
                {
                    continue;
                }

                ref readonly var currentBox = ref boxArray[i];
                DetectedFaceBoxes.Add(currentBox);

                // Calculate IOU and suppress boxes
                for (var j = i + 1; j < count; j++)
                {
                    if (suppressed[j])
                    {
                        continue;
                    }

                    if (CalculateIOU(in currentBox, in boxArray[j]) >= iouThreshold)
                    {
                        suppressed[j] = true;
                    }
                }
            }
        }
        finally
        {
            ArrayPool<FaceBox>.Shared.Return(boxArray);
            ArrayPool<bool>.Shared.Return(suppressed);
        }
    }

    private static float CalculateIOU(in FaceBox box1, in FaceBox box2)
    {
        var x1 = Math.Max(box1.Left, box2.Left);
        var y1 = Math.Max(box1.Top, box2.Top);
        var x2 = Math.Min(box1.Right, box2.Right);
        var y2 = Math.Min(box1.Bottom, box2.Bottom);

        var intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var box1Area = (box1.Right - box1.Left) * (box1.Bottom - box1.Top);
        var box2Area = (box2.Right - box2.Left) * (box2.Bottom - box2.Top);
        var unionArea = box1Area + box2Area - intersectionArea;

        return (unionArea > 0) ? (intersectionArea / unionArea) : 0;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Min(int a, int b) => a < b ? a : b;

    //--------------------------------------------------------------------------------
    // Copy & Resize
    //--------------------------------------------------------------------------------

    private static void CopyDirectToTensor(ReadOnlySpan<byte> source, DenseTensor<float> tensor, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = ((y * width) + x) * 4;

                tensor[0, 0, y, x] = (source[idx] - 127f) / 128f;
                tensor[0, 1, y, x] = (source[idx + 1] - 127f) / 128f;
                tensor[0, 2, y, x] = (source[idx + 2] - 127f) / 128f;
            }
        }
    }

    private static void ResizeBilinearDirectToTensor(ReadOnlySpan<byte> source, DenseTensor<float> tensor, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        var xRatio = (float)(srcWidth - 1) / dstWidth;
        var yRatio = (float)(srcHeight - 1) / dstHeight;

        for (var y = 0; y < dstHeight; y++)
        {
            var srcY = y * yRatio;
            var srcYInt = (int)srcY;
            var yDiff = srcY - srcYInt;
            var yDiffInv = 1.0f - yDiff;
            var srcY1 = Min(srcYInt + 1, srcHeight - 1);

            var srcRow0 = srcYInt * srcWidth * 4;
            var srcRow1 = srcY1 * srcWidth * 4;

            for (var x = 0; x < dstWidth; x++)
            {
                var srcX = x * xRatio;
                var srcXInt = (int)srcX;
                var xDiff = srcX - srcXInt;
                var xDiffInv = 1.0f - xDiff;
                var srcX1 = Min(srcXInt + 1, srcWidth - 1);

                var srcCol0 = srcXInt * 4;
                var srcCol1 = srcX1 * 4;

                var idx00 = srcRow0 + srcCol0;
                var idx10 = srcRow0 + srcCol1;
                var idx01 = srcRow1 + srcCol0;
                var idx11 = srcRow1 + srcCol1;

                var w00 = xDiffInv * yDiffInv;
                var w10 = xDiff * yDiffInv;
                var w01 = xDiffInv * yDiff;
                var w11 = xDiff * yDiff;

                // R channel
                var r = (source[idx00] * w00) + (source[idx10] * w10) + (source[idx01] * w01) + (source[idx11] * w11);
                tensor[0, 0, y, x] = (r - 127f) / 128f;
                // G channel
                var g = (source[idx00 + 1] * w00) + (source[idx10 + 1] * w10) + (source[idx01 + 1] * w01) + (source[idx11 + 1] * w11);
                tensor[0, 1, y, x] = (g - 127f) / 128f;
                // B channel
                var b = (source[idx00 + 2] * w00) + (source[idx10 + 2] * w10) + (source[idx01 + 2] * w01) + (source[idx11 + 2] * w11);
                tensor[0, 2, y, x] = (b - 127f) / 128f;
            }
        }
    }

    private sealed class ConfidenceDescendingComparer : IComparer<FaceBox>
    {
        public int Compare(FaceBox x, FaceBox y)
        {
            return y.Confidence.CompareTo(x.Confidence);
        }
    }
}
