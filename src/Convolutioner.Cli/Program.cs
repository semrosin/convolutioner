using Convolutioner.Core;
using Convolutioner.Core.ImageSharp;
using Convolutioner.Cli.KernelText;
using Convolutioner.Core.WorkPartitioning;

static int PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Convolutioner.Cli <input.bmp> <output.bmp> [--mode seq|par] [--partition pixels|rows|cols|grid] [--grid XxY] [--border zero|clamp]");
    Console.Error.WriteLine("                   [--kernel box3|sharpen|identity] [--kernel-text \"...\"] [--kernel-file path.txt]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Kernel text format:");
    Console.Error.WriteLine("  Rows separated by ';' or newlines, values by spaces/commas.");
    return 2;
}

if (args.Length < 2) return PrintUsage();

var inputPath = args[0];
var outputPath = args[1];

var mode = "seq";
var partition = "rows";
var border = "zero";
var gridX = 4;
var gridY = 4;
string? kernelPreset = null;
string? kernelText = null;
string? kernelFile = null;

for (var i = 2; i < args.Length; i++)
{
    var a = args[i];
    if (a == "--mode" && i + 1 < args.Length) { mode = args[++i]; continue; }
    if (a == "--partition" && i + 1 < args.Length) { partition = args[++i]; continue; }
    if (a == "--border" && i + 1 < args.Length) { border = args[++i]; continue; }
    if (a == "--kernel" && i + 1 < args.Length) { kernelPreset = args[++i]; continue; }
    if (a == "--kernel-text" && i + 1 < args.Length) { kernelText = args[++i]; continue; }
    if (a == "--kernel-file" && i + 1 < args.Length) { kernelFile = args[++i]; continue; }
    if (a == "--grid" && i + 1 < args.Length)
    {
        var s = args[++i];
        var parts = s.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out gridX) || !int.TryParse(parts[1], out gridY))
            return PrintUsage();
        continue;
    }

    return PrintUsage();
}

BorderMode borderMode;
PartitioningMode partitionMode;
Kernel kernel;
try
{
    borderMode = border.ToLowerInvariant() switch
    {
        "zero" => BorderMode.Zero,
        "clamp" => BorderMode.Clamp,
        _ => throw new ArgumentException($"Unknown border mode: {border}")
    };

    partitionMode = partition.ToLowerInvariant() switch
    {
        "pixels" => PartitioningMode.Pixels,
        "rows" => PartitioningMode.Rows,
        "cols" or "columns" => PartitioningMode.Columns,
        "grid" => PartitioningMode.Grid,
        _ => throw new ArgumentException($"Unknown partition mode: {partition}")
    };

    kernel = ResolveKernel(kernelPreset, kernelText, kernelFile);
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

static Kernel ResolveKernel(string? preset, string? text, string? file)
{
    var specified = 0;
    if (!string.IsNullOrWhiteSpace(preset)) specified++;
    if (!string.IsNullOrWhiteSpace(text)) specified++;
    if (!string.IsNullOrWhiteSpace(file)) specified++;
    if (specified > 1) throw new ArgumentException("You can specify only one of --kernel, --kernel-text, --kernel-file.");

    if (!string.IsNullOrWhiteSpace(text))
        return KernelTextParser.Parse(text);

    if (!string.IsNullOrWhiteSpace(file))
        return KernelTextParser.Parse(File.ReadAllText(file));

    var p = (preset ?? "box3").Trim().ToLowerInvariant();
    return p switch
    {
        "identity" => Kernel.Identity(),
        "box3" => new Kernel(
            width: 3,
            height: 3,
            centerX: 1,
            centerY: 1,
            weights:
            [
                1f/9f, 1f/9f, 1f/9f,
                1f/9f, 1f/9f, 1f/9f,
                1f/9f, 1f/9f, 1f/9f,
            ]),
        "sharpen" => new Kernel(
            width: 3,
            height: 3,
            centerX: 1,
            centerY: 1,
            weights:
            [
                0f, -1f, 0f,
                -1f, 5f, -1f,
                0f, -1f, 0f,
            ]),
        _ => throw new ArgumentException($"Unknown kernel preset: {preset}. Use box3|sharpen|identity.")
    };
}

var input = GrayImageIo.LoadAsGray(inputPath);

var sw = System.Diagnostics.Stopwatch.StartNew();
GrayImage output;
try
{
    output = mode.ToLowerInvariant() switch
    {
        "seq" => Convolver.ConvolveSequential(input, kernel, borderMode),
        "par" => Convolver.ConvolveParallel(input, kernel, borderMode, partitionMode, gridX, gridY),
        _ => throw new ArgumentException($"Unknown mode: {mode}")
    };
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}
sw.Stop();

GrayImageIo.SaveGrayAsBmp(output, outputPath);
Console.WriteLine($"Done. {input.Width}x{input.Height}, mode={mode}, partition={partition}, border={border}, elapsed={sw.ElapsedMilliseconds} ms");
return 0;
