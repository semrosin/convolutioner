namespace Convolutioner.Core;

public sealed class GrayImage
{
    public int Width { get; }
    public int Height { get; }
    public float[] Pixels { get; }

    public GrayImage(int width, int height, float[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.Length != width * height) throw new ArgumentException("Pixels length must be equal to width * height.");

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public float GetPixel(int x, int y) => Pixels[(y * Width) + x];
    public void SetPixel(int x, int y, float value) => Pixels[(y * Width) + x] = value;

    public GrayImage Clone()
    {
        var copy = new float[Pixels.Length];
        Array.Copy(Pixels, copy, copy.Length);
        return new GrayImage(Width, Height, copy);
    }
}
