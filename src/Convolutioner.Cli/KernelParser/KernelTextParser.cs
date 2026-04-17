using System.Globalization;
using Convolutioner.Core;

namespace Convolutioner.Cli.KernelText;

public static class KernelTextParser
{
    /// <summary>
    /// Parses a 2D kernel from text.
    /// Rows can be separated by ';' or newlines, values separated by whitespace and/or ','.
    /// Center defaults to (width/2, height/2) unless specified.
    /// </summary>
    public static Kernel Parse(string text, int? centerX = null, int? centerY = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var rows = new List<float[]>();

        foreach (var line in SplitLinesAndSemicolons(text))
        {
            var values = line
                .Replace(',', ' ')
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => float.Parse(t, CultureInfo.InvariantCulture))
                .ToArray();

            if (values.Length == 0) continue;
            rows.Add(values);
        }

        if (rows.Count == 0) throw new FormatException("Kernel text has no numeric rows.");

        var width = rows[0].Length;
        if (width == 0) throw new FormatException("Kernel width must be > 0.");

        var height = rows.Count;
        for (var i = 1; i < height; i++)
            if (rows[i].Length != width)
                throw new FormatException("All kernel rows must have the same number of values.");

        
        var cx = centerX ?? (width / 2);
        var cy = centerY ?? (height / 2);

        var weights = new float[width * height];
        for (var y = 0; y < height; y++)
        {
            var row = rows[y];
            Array.Copy(row, 0, weights, y * width, width);
        }

        return new Kernel(width, height, cx, cy, weights);
    }

    private static IEnumerable<string> SplitLinesAndSemicolons(string text)
    {
        // Normalize CRLF/CR -> LF first, then split further by ';'
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (var line in normalized.Split('\n'))
        {
            var parts = line.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
            foreach (var p in parts)
                yield return p;
        }
    }
}