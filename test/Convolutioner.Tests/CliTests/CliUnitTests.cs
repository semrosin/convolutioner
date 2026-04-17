using Convolutioner.Cli.KernelText;

namespace Convolutioner.Tests.CLITests;

public class CliTests
{
    [Fact]
    public void KernelTextParser_ParsesSemicolonSeparatedMatrix()
    {
        var k = KernelTextParser.Parse("0 -1 0; -1 5 -1; 0 -1 0");
        Assert.Equal(3, k.Width);
        Assert.Equal(3, k.Height);
        Assert.Equal(1, k.CenterX);
        Assert.Equal(1, k.CenterY);
        Assert.Equal(5f, k.Get(1, 1));
        Assert.Equal(-1f, k.Get(1, 0));
    }

    [Fact]
    public void KernelTextParser_ParsesNewlinesAndComments()
    {
        var text = """
                   0 0 0
                   0 1 0
                   0 0 0
                   """;
        var k = KernelTextParser.Parse(text);
        Assert.Equal(3, k.Width);
        Assert.Equal(3, k.Height);
        Assert.Equal(1f, k.Get(1, 1));
    }

    [Fact]
    public void KernelTextParser_ParsesCommaSeparatedValues()
    {
        var k = KernelTextParser.Parse("1,2,3;4,5,6;7,8,9");
        Assert.Equal(3, k.Width);
        Assert.Equal(3, k.Height);
        Assert.Equal(1f, k.Get(0, 0));
        Assert.Equal(5f, k.Get(1, 1));
        Assert.Equal(9f, k.Get(2, 2));
    }

    [Fact]
    public void KernelTextParser_ParsesSingleRow()
    {
        var k = KernelTextParser.Parse("1 2 3");
        Assert.Equal(3, k.Width);
        Assert.Equal(1, k.Height);
        Assert.Equal(1, k.CenterX);
        Assert.Equal(0, k.CenterY);
        Assert.Equal(1f, k.Get(0, 0));
        Assert.Equal(2f, k.Get(1, 0));
        Assert.Equal(3f, k.Get(2, 0));
    }

    [Fact]
    public void KernelTextParser_ParsesWithCustomCenter()
    {
        var k = KernelTextParser.Parse("1 2 3;4 5 6", centerX: 0, centerY: 1);
        Assert.Equal(3, k.Width);
        Assert.Equal(2, k.Height);
        Assert.Equal(0, k.CenterX);
        Assert.Equal(1, k.CenterY);
    }

    [Fact]
    public void KernelTextParser_ThrowsOnEmptyText()
    {
        Assert.Throws<FormatException>(() => KernelTextParser.Parse(""));
    }

    [Fact]
    public void KernelTextParser_ThrowsOnNoNumericRows()
    {
        Assert.Throws<FormatException>(() => KernelTextParser.Parse("   ;   ;   "));
    }

    [Fact]
    public void KernelTextParser_ThrowsOnInconsistentRowLengths()
    {
        Assert.Throws<FormatException>(() => KernelTextParser.Parse("1 2;3 4 5"));
    }

    [Fact]
    public void KernelTextParser_IgnoresEmptyLines()
    {
        var text = """
                   
                   1 2
                   
                   3 4
                   
                   """;
        var k = KernelTextParser.Parse(text);
        Assert.Equal(2, k.Width);
        Assert.Equal(2, k.Height);
        Assert.Equal(1f, k.Get(0, 0));
        Assert.Equal(4f, k.Get(1, 1));
    }

    [Fact]
    public void KernelTextParser_ParsesFloatingPointValues()
    {
        var k = KernelTextParser.Parse("0.5 -0.25 1.0");
        Assert.Equal(3, k.Width);
        Assert.Equal(1, k.Height);
        Assert.Equal(0.5f, k.Get(0, 0));
        Assert.Equal(-0.25f, k.Get(1, 0));
        Assert.Equal(1.0f, k.Get(2, 0));
    }
}