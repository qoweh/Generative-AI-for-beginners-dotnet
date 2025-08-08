namespace MySpaceAINet.Screenshot;

public class RenderState
{
    public int Width { get; }
    public int Height { get; }
    public char[,] Chars { get; }
    public ConsoleColor[,] Colors { get; }

    public RenderState(int width, int height)
    {
        Width = width; Height = height;
        Chars = new char[height, width];
        Colors = new ConsoleColor[height, width];
        Clear();
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Chars[y, x] = ' ';
            Colors[y, x] = ConsoleColor.Black;
        }
    }

    public RenderState Clone()
    {
        var copy = new RenderState(Width, Height);
        Array.Copy(Chars, copy.Chars, Chars.Length);
        Array.Copy(Colors, copy.Colors, Colors.Length);
        return copy;
    }
}
