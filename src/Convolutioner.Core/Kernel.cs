namespace Convolutioner.Core;

public sealed class Kernel
{
    public int Width { get; }
    public int Height { get; }
    public int CenterX { get; }
    public int CenterY { get; }
    public float[] Weights { get; }

    public Kernel(int width, int height, int centerX, int centerY, float[] weights)
    {
        if( width <= 0 ) throw new ArgumentOutOfRangeException(nameof(width));
        if( height <= 0 ) throw new ArgumentOutOfRangeException(nameof(height));
        if( centerX < 0 ) throw new ArgumentOutOfRangeException(nameof(centerX));
        if( centerX >= width ) throw new ArgumentOutOfRangeException(nameof(centerX));
        if( centerY < 0 ) throw new ArgumentOutOfRangeException(nameof(centerY));
        if( centerY >= height ) throw new ArgumentOutOfRangeException(nameof(centerY));
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != width * height) throw new ArgumentException("Weights length must be width*height.");

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
        var cx = width / 2;
        var cy = height / 2;
        return new Kernel(width, height, cx, cy, new float[width * height]);
    }

    /// <summary>
    /// Discrete shift kernel: output(x,y) = input(x-dx, y-dy)
    /// </summary>
    public static Kernel Delta(int dx, int dy)
    {
        var w = Math.Abs(dx) * 2 + 1;
        var h = Math.Abs(dy) * 2 + 1;
        var cx = w / 2;
        var cy = h / 2;

        var weights = new float[w * h];
        var kx = cx - dx;
        var ky = cy - dy;
        weights[(ky * w) + kx] = 1f;
        return new Kernel(w, h, cx, cy, weights);
    }

    /// <summary>
    /// Composition for Zero border (infinite zero-extended domain):
    /// applying a then b equals applying Compose(a, b).
    /// </summary>
    public static Kernel Compose(Kernel a, Kernel b)
    {
        var w = a.Width + b.Width - 1;
        var h = a.Height + b.Height - 1;
        var cx = a.CenterX + b.CenterX;
        var cy = a.CenterY + b.CenterY;
        var weights = new float[w * h];

        for (var by = 0; by < b.Height; by++)
        for (var bx = 0; bx < b.Width; bx++)
        {
            var bw = b.Get(bx, by);
            if (bw == 0f) continue;

            for (var ay = 0; ay < a.Height; ay++)
            for (var ax = 0; ax < a.Width; ax++)
            {
                var aw = a.Get(ax, ay);
                if (aw == 0f) continue;

                var rx = ax + bx;
                var ry = ay + by;
                weights[(ry * w) + rx] += bw * aw;
            }
        }

        return new Kernel(w, h, cx, cy, weights);
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
