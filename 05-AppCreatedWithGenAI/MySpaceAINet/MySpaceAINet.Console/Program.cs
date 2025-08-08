using System.Text;
using MySpaceAINet.ConsoleGame;

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

StartScreen.Show();

var speed = Speed.Slow;
ConsoleKeyInfo key;
while (true)
{
    if (Console.KeyAvailable)
    {
        key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
        {
            speed = Speed.Slow; break;
        }
        if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
        {
            speed = Speed.Medium; break;
        }
        if (key.Key == ConsoleKey.D3 || key.Key == ConsoleKey.NumPad3)
        {
            speed = Speed.Fast; break;
        }
    }
}

Console.Clear();
GameManager.RunGameLoop(speed);
