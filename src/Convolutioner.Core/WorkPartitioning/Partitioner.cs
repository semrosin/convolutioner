namespace Convolutioner.Core.WorkPartitioning;

public static class Partitioner
{
    public static IEnumerable<WorkRect> Create(int width, int height, PartitioningMode mode, int gridX = 1, int gridY = 1)
    {
        ArgumentNullException.ThrowIfNull(width);
        ArgumentNullException.ThrowIfNull(height);

        return mode switch
        {
            PartitioningMode.Pixels => Pixels(width, height),
            PartitioningMode.Rows => Rows(width, height),
            PartitioningMode.Columns => Columns(width, height),
            PartitioningMode.Grid => Grid(width, height, gridX, gridY),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown partitioning mode."),
        };
    }

    private static IEnumerable<WorkRect> Pixels(int width, int height)
    {
        // One pixel per task is intentionally "bad" for overhead and cache locality,
        // but it's useful to study the effects the user asked about.
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            yield return new WorkRect(x, y, 1, 1);
    }

    private static IEnumerable<WorkRect> Rows(int width, int height)
    {
        for (var y = 0; y < height; y++)
            yield return new WorkRect(0, y, width, 1);
    }

    private static IEnumerable<WorkRect> Columns(int width, int height)
    {
        for (var x = 0; x < width; x++)
            yield return new WorkRect(x, 0, 1, height);
    }

    private static IEnumerable<WorkRect> Grid(int width, int height, int gridX, int gridY)
    {
        ArgumentNullException.ThrowIfNull(gridX);
        ArgumentNullException.ThrowIfNull(gridY);

        gridX = Math.Min(gridX, width);
        gridY = Math.Min(gridY, height);

        var baseWidth = width / gridX;
        var remWidth = width % gridX;
        var baseHeight = height / gridY;
        var remHeight = height % gridY;

        var y0 = 0;
        for (var gy = 0; gy < gridY; gy++)
        {
            var squareHeight = baseHeight + (gy < remHeight ? 1 : 0); // первые remHeight блоков выше на 1 пиксель
            var x0 = 0;
            for (var gx = 0; gx < gridX; gx++)
            {
                var squareWidth = baseWidth + (gx < remWidth ? 1 : 0); // первые remWidth блоков шире на 1 пиксель
                yield return new WorkRect(x0, y0, squareWidth, squareHeight);

                x0 += squareWidth;
            }
            y0 += squareHeight;
        }
    }
}

