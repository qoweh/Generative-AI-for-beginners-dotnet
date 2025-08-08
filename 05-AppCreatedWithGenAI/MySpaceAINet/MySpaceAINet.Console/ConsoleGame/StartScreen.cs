namespace MySpaceAINet.ConsoleGame;

public static class StartScreen
{
    public static void Show()
    {
        Console.Clear();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;

        var w = Console.WindowWidth;
        var h = Console.WindowHeight;

        string title = "Space.AI.NET()";
        string subtitle = "Built with .NET + AI for galactic defense";

        int titleLeft = Math.Max(0, (w - title.Length) / 2);
        int subLeft = Math.Max(0, (w - subtitle.Length) / 2);
        int top = Math.Max(1, h / 5);

        Console.SetCursorPosition(titleLeft, top);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(title);

        Console.SetCursorPosition(subLeft, top + 1);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(subtitle);

        Console.ForegroundColor = ConsoleColor.White;
        int y = top + 4;
        void Line(string s)
        {
            Console.SetCursorPosition(2, y++);
            Console.Write(s);
        }

        Line("How to Play:");
        Line("\u2190   Move Left");
        Line("\u2192   Move Right");
        Line("SPACE   Shoot");
        Line("S   Take Screenshot");
        Line("O   Toggle Ollama AI mode");
        Line("Q   Quit");
        y++;
        Line("Select Game Speed:");
        Line("[1] Slow (default)");
        Line("[2] Medium");
        Line("[3] Fast");
        Line("Press ENTER for default");
    }
}
