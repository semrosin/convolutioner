using Convolutioner.Core.WorkPartitioning;

namespace Convolutioner.Core;

public static class Convolver
{
    public static GrayImage ConvolveSequential(GrayImage input, Kernel kernel, BorderMode borderMode)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(kernel);

        var output = new float[input.Pixels.Length];
        ConvolveInternal(input, output, kernel, borderMode, new WorkRect(0, 0, input.Width, input.Height));
        return new GrayImage(input.Width, input.Height, output);
    }

    public static GrayImage ConvolveParallel(
        GrayImage input,
        Kernel kernel,
        BorderMode borderMode,
        PartitioningMode partitioningMode,
        int gridX = 1,
        int gridY = 1,
        int? maxDegreeOfParallelism = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(kernel);

        var output = new float[input.Pixels.Length];

        var rects = Partitioner.Create(input.Width, input.Height, partitioningMode, gridX, gridY);
        var opts = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? -1
        };

        Parallel.ForEach(rects, opts, rect =>
        {
            ConvolveInternal(input, output, kernel, borderMode, rect);
        });

        return new GrayImage(input.Width, input.Height, output);
    }

    private static void ConvolveInternal(GrayImage input, float[] output, Kernel kernel, BorderMode borderMode, WorkRect rect)
    {
        var src = input.Pixels;
        var weights = kernel.Weights;

        var yEnd = rect.Y2Exclusive;
        var xEnd = rect.X2Exclusive;

        for (var y = rect.Y; y < yEnd; y++)
        for (var x = rect.X; x < xEnd; x++)
        {
            var sum = 0f;

            for (var ky = 0; ky < kernel.Height; ky++)
            {
                var iy = y + (ky - kernel.CenterY);
                var yInRange = (uint)iy < (uint)input.Height;
                if (!yInRange && borderMode == BorderMode.Zero) continue;
                if (!yInRange && borderMode == BorderMode.Clamp) iy = iy < 0 ? 0 : (input.Height - 1);

                var srcRow = iy * input.Width;
                var kRow = ky * kernel.Width;

                for (var kx = 0; kx < kernel.Width; kx++)
                {
                    var ix = x + (kx - kernel.CenterX);
                    var xInRange = (uint)ix < (uint)input.Width;
                    if (!xInRange && borderMode == BorderMode.Zero) continue;
                    if (!xInRange && borderMode == BorderMode.Clamp) ix = ix < 0 ? 0 : (input.Width - 1);

                    var pixel = src[srcRow + ix];
                    var weight = weights[kRow + kx];
                    sum += pixel * weight;
                }
            }

            output[(y * input.Width) + x] = sum;
        }
    }
}

