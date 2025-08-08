namespace MySpaceAINet.ConsoleGame;

public class Enemy
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Shape { get; set; } = "><";
    public ConsoleColor Color { get; set; } = ConsoleColor.Red;
    public bool CanShoot { get; set; } = false;
}
