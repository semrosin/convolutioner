namespace BMPConvolver.Core;

public sealed class Kernel
{
    public int Width { get; }
    public int Height { get; }
    public int CenterX { get; }
    public int CenterY { get; }
    public float[] Weights { get; }

    public Kernel(int width, int height, int centerX, int centerY, float[] weights)
    {
        if( width <= 0 ) throw new ArgumentOutOfRangeException(nameof(width), width, "Size must be a positive integer.");
        if( height <= 0 ) throw new ArgumentOutOfRangeException(nameof(height), height, "Size must be a positive integer.");
        if( centerX < 0 ) throw new ArgumentOutOfRangeException(nameof(centerX), centerX, "Center must be within the kernel bounds.");
        if( centerX >= width ) throw new ArgumentOutOfRangeException(nameof(centerX), centerX, "Center must be within the kernel bounds.");
        if( centerY < 0 ) throw new ArgumentOutOfRangeException(nameof(centerY), centerY, "Center must be within the kernel bounds.");
        if( centerY >= height ) throw new ArgumentOutOfRangeException(nameof(centerY), centerY, "Center must be within the kernel bounds.");
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != width * height) throw new ArgumentException(nameof(weights), "Weights length must be width*height.");

        Width = width;
        Height = height;
        CenterX = centerX;
        CenterY = centerY;
        Weights = weights;
    }

    public float Get(int x, int y) => Weights[(y * Width) + x];

    public static Kernel Identity()
        => Delta(0, 0);

    public static Kernel Zero(int width, int height)
    {
        var centerX = width / 2;
        var centerY = height / 2;
        return new Kernel(width, height, centerX, centerY, new float[width * height]);
    }

    /// <summary>
    /// Discrete shift kernel: output(x,y) = input(x-dx, y-dy)
    /// </summary>
    public static Kernel Delta(int dx, int dy)
    {
        var width = Math.Abs(dx) * 2 + 1;
        var height = Math.Abs(dy) * 2 + 1;
        var centerX = width / 2;
        var centerY = height / 2;

        var weights = new float[width * height];
        var kx = centerX - dx;
        var ky = centerY - dy;
        weights[(ky * width) + kx] = 1f;
        return new Kernel(width, height, centerX, centerY, weights);
    }

    /// <summary>
    /// Box blur kernel of the given size.
    /// </summary>
    public static Kernel BoxBlur(int size = 3)
    {
        if (size <= 0 || size % 2 == 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be a positive odd number.");

        var weight = 1f / (size * size);
        var weights = new float[size * size];
        Array.Fill(weights, weight);
        var center = size / 2;

        return new Kernel(size, size, center, center, weights);
    }

    /// <summary>
    /// Sharpening kernel (Laplacian-based unsharp mask).
    /// </summary>
    public static Kernel Sharpen()
        => new(
            width: 3,
            height: 3,
            centerX: 1,
            centerY: 1,
            weights:
            [
                 0f, -1f,  0f,
                -1f,  5f, -1f,
                 0f, -1f,  0f,
            ]);

    /// <summary>
    /// Composition for Zero border (infinite zero-extended domain):
    /// applying a then b equals applying Compose(a, b).
    /// </summary>
    public static Kernel Compose(Kernel a, Kernel b)
    {
        var width = a.Width + b.Width - 1;
        var height = a.Height + b.Height - 1;
        var centerX = a.CenterX + b.CenterX;
        var centerY = a.CenterY + b.CenterY;
        var weights = new float[width * height];

        for (var bY = 0; bY < b.Height; bY++)
        for (var bX = 0; bX < b.Width; bX++)
        {
            var bWeight = b.Get(bX, bY);
            if (bWeight == 0f) continue;

            for (var aY = 0; aY < a.Height; aY++)
            for (var aX = 0; aX < a.Width; aX++)
            {
                var aWeight = a.Get(aX, aY);
                if (aWeight == 0f) continue;

                var rX = aX + bX;
                var rY = aY + bY;
                weights[(rY * width) + rX] += bWeight * aWeight;
            }
        }

        return new Kernel(width, height, centerX, centerY, weights);
    }

    public Kernel PadZeros(int padLeft, int padTop, int padRight, int padBottom)
    {
        if (padLeft < 0) throw new ArgumentOutOfRangeException(nameof(padLeft));
        if (padTop < 0) throw new ArgumentOutOfRangeException(nameof(padTop));
        if (padRight < 0) throw new ArgumentOutOfRangeException(nameof(padRight));
        if (padBottom < 0) throw new ArgumentOutOfRangeException(nameof(padBottom));

        var newW = Width + padLeft + padRight;
        var newH = Height + padTop + padBottom;
        var newWeights = new float[newW * newH];

        for (var y = 0; y < Height; y++)
        {
            var srcRow = y * Width;
            var dstRow = (y + padTop) * newW + padLeft;
            Array.Copy(Weights, srcRow, newWeights, dstRow, Width);
        }

        return new Kernel(newW, newH, CenterX + padLeft, CenterY + padTop, newWeights);
    }
}
