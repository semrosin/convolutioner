using BMPConvolver.Core;
using BMPConvolver.Core.WorkPartitioning;
using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BMPConvolver.Tests.CoreTests;

public class CoreTests
{
    private const int Seed = 1;
    private const float Epsilon = 1e-6f;

    [Theory]
    [InlineData(1, 1)]
    [InlineData(32, 32)]
    [InlineData(120, 30)]
    [InlineData(256, 128)]
    [InlineData(512, 256)]
    [InlineData(512, 512)]
    [InlineData(768, 512)]
    public void IdentityKernel_IsIdentity(int width, int height)
    {
        var img = RandomImage(width, height);
        var outImg = Convolver.ConvolveSequential(img, Kernel.Identity(), BorderMode.Zero);
        AssertImagesEqual(img, outImg);
    }

    [Theory]
    [InlineData(1, 1, 3, 3)]
    [InlineData(17, 9, 5, 7)]
    [InlineData(128, 512, 7, 5)]
    [InlineData(256, 256, 9, 9)]
    [InlineData(320, 240, 11, 7)]
    [InlineData(512, 128, 15, 13)]
    public void ZeroKernel_ProducesZeros(int width, int height, int kernelWidth, int kernelHeight)
    {
        var img = RandomImage(width, height);
        var kernel = Kernel.Zero(kernelWidth, kernelHeight);
        var outImg = Convolver.ConvolveSequential(img, kernel, BorderMode.Zero);
        Assert.All(outImg.Pixels, w => Assert.Equal(0f, w));
    }

    [Theory]
    [InlineData(5, 5, 1, 2, 3, 4)]
    [InlineData(19, 11, 2, 2, 2, 2)]
    [InlineData(31, 29, 4, 3, 5, 2)]
    [InlineData(64, 64, 8, 8, 8, 8)]
    [InlineData(127, 65, 6, 5, 6, 5)]
    public void PaddingKernelWithZeros_DoesNotChangeResult(int width, int height, int padLeft, int padTop, int padRight, int padBottom)
    {
        var img = RandomImage(width, height);
        var kernel = RandomKernelOdd(maxSize: 7);
        var padded = kernel.PadZeros(padLeft, padTop, padRight, padBottom);

        var image_A = Convolver.ConvolveSequential(img, kernel, BorderMode.Zero);
        var image_B = Convolver.ConvolveSequential(img, padded, BorderMode.Zero);
        AssertImagesEqual(image_A, image_B, Epsilon);
    }

    [Theory]
    [InlineData(9, 7)]
    [InlineData(31, 29)]
    [InlineData(63, 61)]
    [InlineData(80, 72)]
    [InlineData(128, 96)]
    public void ShiftAndInverseShift_ComposeToIdentity(int width, int height)
    {
        var img = RandomImage(width, height);
        var dx = 2;
        var dy = -1;
        var shift = Kernel.Delta(dx, dy);
        var inv = Kernel.Delta(-dx, -dy);
        var composed = Kernel.Compose(shift, inv);

        var seq = Convolver.ConvolveSequential(Convolver.ConvolveSequential(img, shift, BorderMode.Zero), inv, BorderMode.Zero);
        var one = Convolver.ConvolveSequential(img, composed, BorderMode.Zero);
        // Composition is guaranteed on the "safe" interior. When applying filters sequentially on a finite array,
        // the intermediate result is implicitly cropped, so border pixels can differ from a single composed kernel.
        AssertImagesEqualInterior(seq, one, marginX: Math.Abs(dx) * 2, marginY: Math.Abs(dy) * 2, eps: Epsilon);
    }

    [Theory]
    [InlineData(17, 13, 3, 3, 5, 5)]
    [InlineData(64, 31, 1, 1, 7, 3)]
    [InlineData(128, 128, 5, 5, 3, 7)]
    [InlineData(192, 97, 7, 7, 9, 5)]
    [InlineData(256, 64, 9, 9, 3, 3)]
    public void SequentialApplication_EqualsKernelComposition(int width, int height, int KernelWidth_1, int KernelHeight_1, int KernelWidth_2, int KernelHeight_2)
    {
        var img = RandomImage(width, height);
        var a = RandomKernelOddFrom(KernelWidth_1, KernelHeight_1);
        var b = RandomKernelOddFrom(KernelWidth_2, KernelHeight_2);

        var seq = Convolver.ConvolveSequential(Convolver.ConvolveSequential(img, a, BorderMode.Zero), b, BorderMode.Zero);
        var composed = Kernel.Compose(a, b);
        var one = Convolver.ConvolveSequential(img, composed, BorderMode.Zero);

        var marginX = (a.Width / 2) + (b.Width / 2);
        var marginY = (a.Height / 2) + (b.Height / 2);
        AssertImagesEqualInterior(seq, one, marginX, marginY, eps: Epsilon);
    }

    [Theory]
    [InlineData(1, 1, BorderMode.Zero)]
    [InlineData(2, 7, BorderMode.Zero)]
    [InlineData(13, 9, BorderMode.Zero)]
    [InlineData(64, 64, BorderMode.Zero)]
    [InlineData(127, 33, BorderMode.Zero)]
    [InlineData(128, 128, BorderMode.Zero)]
    [InlineData(400, 200, BorderMode.Zero)]
    [InlineData(1, 1, BorderMode.Clamp)]
    [InlineData(2, 7, BorderMode.Clamp)]
    [InlineData(13, 9, BorderMode.Clamp)]
    [InlineData(64, 64, BorderMode.Clamp)]
    [InlineData(127, 33, BorderMode.Clamp)]
    [InlineData(128, 128, BorderMode.Clamp)]
    [InlineData(400, 200, BorderMode.Clamp)]
    public void Parallel_EqualsSequential_ForAllPartitionings(int width, int height, BorderMode borderMode)
    {
        var kernel = RandomKernelOdd(maxSize: 9);

        var img = RandomImage(width, height);
        var seq = Convolver.ConvolveSequential(img, kernel, borderMode);

        var parPixel = Convolver.ConvolveParallel(img, kernel, borderMode, PartitioningMode.Pixels);
        var parRows = Convolver.ConvolveParallel(img, kernel, borderMode, PartitioningMode.Rows);
        var parCols = Convolver.ConvolveParallel(img, kernel, borderMode, PartitioningMode.Columns);
        var parGrid = Convolver.ConvolveParallel(img, kernel, borderMode, PartitioningMode.Grid, gridX: 8, gridY: 8);

        AssertImagesEqual(seq, parPixel);
        AssertImagesEqual(seq, parRows);
        AssertImagesEqual(seq, parCols);
        AssertImagesEqual(seq, parGrid);
    }

    [Theory]
    [InlineData(64, 48)]
    [InlineData(128, 128)]
    [InlineData(256, 64)]
    [InlineData(192, 512)]
    public void BoxBlur3_MatchesImageSharpBoxBlurRadius1(int width, int height)
    {
        var img = RandomImage(width, height);
        var kernel = Kernel.BoxBlur();

        var ours = Convolver.ConvolveSequential(img, kernel, BorderMode.Clamp);
        var oursBytes = ToL8BytesClamped(ours);

        using var sharp = ToImageL8(img);

        sharp.Mutate(c => c.BoxBlur(1));

        var sharpBytes = FromImageL8ToBytes(sharp);

        Assert.Equal(oursBytes.Length, sharpBytes.Length);
        for (var i = 0; i < oursBytes.Length; i++)
            Assert.True(Math.Abs(oursBytes[i] - sharpBytes[i]) <= 1, $"Byte index {i}: ours={oursBytes[i]} sharp={sharpBytes[i]}");
    }

    [Theory]
    [InlineData(BorderMode.Zero, 1, 1)]
    [InlineData(BorderMode.Zero, 2, 7)]
    [InlineData(BorderMode.Zero, 13, 9)]
    [InlineData(BorderMode.Zero, 64, 33)]
    [InlineData(BorderMode.Zero, 128, 64)]
    [InlineData(BorderMode.Zero, 192, 128)]
    [InlineData(BorderMode.Clamp, 1, 1)]
    [InlineData(BorderMode.Clamp, 2, 7)]
    [InlineData(BorderMode.Clamp, 13, 9)]
    [InlineData(BorderMode.Clamp, 64, 33)]
    [InlineData(BorderMode.Clamp, 128, 64)]
    [InlineData(BorderMode.Clamp, 192, 128)]
    public void Convolution_MatchesOpenCv_Filter2D_ForRandomKernels(BorderMode borderMode, int width, int height)
    {
        var kernel = RandomKernelOdd(maxSize: 9);

        var img = RandomImage(width, height);

        var ours = Convolver.ConvolveSequential(img, kernel, borderMode);
        var cv = OpenCvFilter2D(img, kernel, borderMode);

        AssertImagesEqual(ours, cv, eps: Epsilon);
    }

    private static GrayImage RandomImage(int width, int height)
    {
        var rnd = new Random(Seed);
        var pixels = new float[width * height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (float)rnd.NextDouble();
        return new GrayImage(width, height, pixels);
    }

    private static Kernel RandomKernelOdd(int maxSize)
    {
        var rnd = new Random(Seed);
        var width= 1 + 2 * rnd.Next(0, Math.Max(1, (maxSize + 1) / 2));
        var height = 1 + 2 * rnd.Next(0, Math.Max(1, (maxSize + 1) / 2));
        return RandomKernelOddFrom(width, height);
    }

    private static Kernel RandomKernelOddFrom(int wRequested, int hRequested)
    {
        var width= wRequested <= 1 ? 1 : (wRequested % 2 == 1 ? wRequested : wRequested + 1);
        var height = hRequested <= 1 ? 1 : (hRequested % 2 == 1 ? hRequested : hRequested + 1);

        var rnd = new Random(Seed);
        var weights = new float[width * height];
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = ((float)rnd.NextDouble() - 0.5f) * 0.5f;
        }
        return new Kernel(width, height, width/ 2, height / 2, weights);
    }

    private static void AssertImagesEqual(GrayImage a, GrayImage b, float eps = 0f)
    {
        Assert.Equal(a.Width, b.Width);
        Assert.Equal(a.Height, b.Height);
        Assert.Equal(a.Pixels.Length, b.Pixels.Length);

        for (var i = 0; i < a.Pixels.Length; i++)
        {
            var da = a.Pixels[i];
            var db = b.Pixels[i];
            if (eps == 0f)
                Assert.Equal(da, db);
            else
                Assert.True(MathF.Abs(da - db) <= eps, $"Index {i}: {da} != {db} (eps={eps})");
        }
    }

    private static void AssertImagesEqualInterior(GrayImage a, GrayImage b, int marginX, int marginY, float eps = 0f)
    {
        Assert.Equal(a.Width, b.Width);
        Assert.Equal(a.Height, b.Height);

        var width= a.Width;
        var height = a.Height;
        var x0 = Math.Clamp(marginX, 0, width);
        var y0 = Math.Clamp(marginY, 0, height);
        var x1 = Math.Clamp(width - marginX, 0, width);
        var y1 = Math.Clamp(height - marginY, 0, height);

        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            var i = (y * width) + x;
            var da = a.Pixels[i];
            var db = b.Pixels[i];
            Assert.True(MathF.Abs(da - db) <= eps, $"({x},{y}): {da} != {db} (eps={eps})");
        }
    }

    private static Image<L8> ToImageL8(GrayImage img)
    {
        var image = new Image<L8>(img.Width, img.Height);
        var width= img.Width;
        var height = img.Height;
        var src = img.Pixels;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var srcRow = y * width;
                for (var x = 0; x < width; x++)
                {
                    var weight = src[srcRow + x];
                    if (weight < 0f) weight = 0f;
                    if (weight > 1f) weight = 1f;
                    row[x] = new L8((byte)MathF.Round(weight * 255f));
                }
            }
        });

        return image;
    }

    private static byte[] FromImageL8ToBytes(Image<L8> img)
    {
        var width= img.Width;
        var height = img.Height;
        var bytes = new byte[width * height];
        img.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var dstRow = y * width;
                for (var x = 0; x < width; x++)
                    bytes[dstRow + x] = row[x].PackedValue;
            }
        });
        return bytes;
    }

    private static byte[] ToL8BytesClamped(GrayImage img)
    {
        var bytes = new byte[img.Pixels.Length];
        for (var i = 0; i < img.Pixels.Length; i++)
        {
            var weight = img.Pixels[i];
            if (weight < 0f) weight = 0f;
            if (weight > 1f) weight = 1f;
            bytes[i] = (byte)MathF.Round(weight * 255f);
        }
        return bytes;
    }

    private static GrayImage OpenCvFilter2D(GrayImage img, Kernel kernel, BorderMode borderMode)
    {
        using var src = new Mat(img.Height, img.Width, MatType.CV_32FC1);
        var i = 0;
        for (var y = 0; y < img.Height; y++)
        for (var x = 0; x < img.Width; x++)
            src.Set(y, x, img.Pixels[i++]);

        using var kernelMat = new Mat(kernel.Height, kernel.Width, MatType.CV_32FC1);
        i = 0;
        for (var y = 0; y < kernel.Height; y++)
        for (var x = 0; x < kernel.Width; x++)
            kernelMat.Set(y, x, kernel.Weights[i++]);

        using var dst = new Mat(img.Height, img.Width, MatType.CV_32FC1);

        var border = borderMode switch
        {
            BorderMode.Zero => BorderTypes.Constant,
            BorderMode.Clamp => BorderTypes.Replicate,
            _ => throw new ArgumentOutOfRangeException(nameof(borderMode))
        };

        Cv2.Filter2D(
            src: src,
            dst: dst,
            ddepth: MatType.CV_32FC1,
            kernel: kernelMat,
            anchor: new OpenCvSharp.Point(kernel.CenterX, kernel.CenterY),
            delta: 0,
            borderType: border);

        var outPixels = new float[img.Width * img.Height];
        i = 0;
        for (var y = 0; y < img.Height; y++)
        for (var x = 0; x < img.Width; x++)
            outPixels[i++] = dst.Get<float>(y, x);

        return new GrayImage(img.Width, img.Height, outPixels);
    }
}