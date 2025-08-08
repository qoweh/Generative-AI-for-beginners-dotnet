namespace MySpaceAINet.ConsoleGame;

public class Bullet
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Dy { get; set; } // -1 up, +1 down
    public bool FromPlayer { get; set; }
    public char Glyph => FromPlayer ? '^' : 'v';
}
