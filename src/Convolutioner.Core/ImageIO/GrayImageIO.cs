using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Convolutioner.Core.ImageSharp;

public static class GrayImageIo
{
    public static GrayImage LoadAsGray(string path)
    {
        using var image = Image.Load<L8>(path);
        var pixels = new float[image.Width * image.Height];
        var bytes = new byte[pixels.Length];

        image.CopyPixelDataTo(bytes);
        for (var i = 0; i < bytes.Length; i++)
            pixels[i] = bytes[i] / 255f;

        return new GrayImage(image.Width, image.Height, pixels);
    }

    public static void SaveGrayAsBmp(GrayImage image, string path)
    {
        using var outImage = new Image<L8>(image.Width, image.Height);
        var src = image.Pixels;
        var dest = new L8[src.Length];

        // Convert float to L8
        for (int i = 0; i < src.Length; i++)
        {
            float v = Math.Clamp(src[i], 0f, 1f);
            dest[i] = new L8((byte)MathF.Round(v * 255f));
        }

        outImage.ProcessPixelRows(accessor =>
        {
            int offset = 0;
            int width = image.Width;

            for (var y = 0; y < image.Height; y++)
            {
                dest.AsSpan(offset, width).CopyTo(accessor.GetRowSpan(y));
                offset += width;
            }
        });

        outImage.SaveAsBmp(path);
    }
}

