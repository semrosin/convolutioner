namespace Convolutioner.Core.WorkPartitioning;

public readonly record struct WorkRect(int X, int Y, int Width, int Height)
{
    public int X2Exclusive => X + Width;
    public int Y2Exclusive => Y + Height;
}

