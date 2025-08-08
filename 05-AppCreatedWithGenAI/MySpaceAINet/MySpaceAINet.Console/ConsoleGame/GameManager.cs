using System.Text;
using MySpaceAINet.Screenshot;
using MySpaceAINet.GameActionProcessor;

namespace MySpaceAINet.ConsoleGame;

public static class GameManager
{
    private static RenderState _current = null!;
    private static RenderState _previous = null!;

    private static int _score = 0;
    private static DateTime _start;
    private static int _bulletsActive = 0;
    // Set to -1 for unlimited bullets, or any non-negative number for a cap
    private static int _bulletsMax = -1;

    private static int _left = 2, _top = 1, _right, _bottom;

    private static Player _player = new();
    private static readonly List<Enemy> _enemies = new();
    private static readonly List<Bullet> _bullets = new();
    private static int _enemyDir = 1; // 1 right, -1 left
    private static int _enemyStepTick = 0;
    private static int _enemyStepInterval = 6;

    private static readonly Queue<string> _frameHistory = new();
    private const int FrameHistoryCount = 3;
    private static bool _aiOllamaEnabled = false;
    private static GameAction _lastAction = GameAction.Stop;
    private static IGameActionProvider? _ollama;
    private static bool _showFps = false;
    private static double _lastFps = 0;

    private enum GameState { Playing, Win, GameOver }
    private static GameState _state = GameState.Playing;
    private static int _frozenSeconds = 0;
    private static int ElapsedSeconds => _state == GameState.Playing
        ? (int)(DateTime.UtcNow - _start).TotalSeconds
        : _frozenSeconds;

    public static RenderState GetRenderState() => _current;

    public static void RunGameLoop(Speed speed)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        int w = Math.Max(60, Console.WindowWidth);
        int h = Math.Max(25, Console.WindowHeight);

        _current = new RenderState(w, h);
        _previous = new RenderState(w, h);

        _right = w - 3;
        _bottom = h - 2;

        ScreenshotService.Init();
        _start = DateTime.UtcNow;

        int frameDelay = speed switch
        {
            Speed.Slow => 120,
            Speed.Medium => 70,
            Speed.Fast => 40,
            _ => 100
        };

    InitEntities();
    _state = GameState.Playing;
    _start = DateTime.UtcNow;
    _frozenSeconds = 0;

        while (true)
        {
            var frameStart = DateTime.UtcNow;
            HandleInput();
            Update(frameDelay);
            Render();

            if ((DateTime.UtcNow - frameStart).TotalMilliseconds < frameDelay)
                Thread.Sleep(frameDelay - (int)(DateTime.UtcNow - frameStart).TotalMilliseconds);

                var elapsed = (DateTime.UtcNow - frameStart).TotalSeconds;
                _lastFps = 1.0 / Math.Max(1e-6, elapsed);
        }
    }

    private static void HandleInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Q) Environment.Exit(0);
            if (key.Key == ConsoleKey.S) ScreenshotService.SaveScreenshot(GetRenderState());
            if (_state == GameState.Playing)
            {
                if (key.Key == ConsoleKey.LeftArrow) MovePlayer(-1);
                if (key.Key == ConsoleKey.RightArrow) MovePlayer(1);
                if (key.Key == ConsoleKey.Spacebar) TryFire();
                if (key.Key == ConsoleKey.F) _showFps = !_showFps;
                if (key.Key == ConsoleKey.O)
                {
                    _aiOllamaEnabled = !_aiOllamaEnabled;
                    if (_aiOllamaEnabled && _ollama == null) _ollama = new OllamaActionProvider();
                }
            }
            else
            {
                // Win or GameOver screen controls
                if (key.Key == ConsoleKey.Enter)
                {
                    InitEntities();
                    _score = 0;
                    _state = GameState.Playing;
                    _start = DateTime.UtcNow;
                    _frozenSeconds = 0;
                }
            }
        }
    }

    private static void Update(int frameDelay)
    {
        if (_state != GameState.Playing)
        {
            // freeze updates during win/lose screens
            return;
        }
        // AI assist
        if (_aiOllamaEnabled && _ollama != null)
        {
            // make a cheap ASCII snapshot string
            var snap = SnapshotString();
            _frameHistory.Enqueue(snap);
            while (_frameHistory.Count > FrameHistoryCount) _frameHistory.Dequeue();

            // Throttle: only ask AI every ~200ms
            if ((DateTime.UtcNow - _lastAiQuery).TotalMilliseconds > 200)
            {
                _lastAiQuery = DateTime.UtcNow;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var (act, reason) = await _ollama.GetNextActionAsync(_frameHistory.ToList(), _lastAction, CancellationToken.None);
                        _lastAction = act;
                        if (!string.IsNullOrWhiteSpace(reason))
                        {
                            _lastAiReason = reason!;
                            _lastAiReasonAt = DateTime.UtcNow;
                        }
                    }
                    catch { }
                });
            }

            // apply last action
            switch (_lastAction)
            {
                case GameAction.MoveLeft: MovePlayer(-1); break;
                case GameAction.MoveRight: MovePlayer(1); break;
                case GameAction.Fire: TryFire(); break;
            }
        }

        // Move bullets
    for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            b.Y += b.Dy;
            if (b.Y <= _top + 1 || b.Y >= _bottom)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            if (b.FromPlayer)
            {
                // hit enemy
                for (int e = _enemies.Count - 1; e >= 0; e--)
                {
                    var en = _enemies[e];
                    int len = en.Shape.Length;
                    if (b.Y == en.Y && b.X >= en.X && b.X < en.X + len)
                    {
                        _enemies.RemoveAt(e);
                        _bullets.RemoveAt(i);
                        _score += 50;
                        break;
                    }
                }
            }
        else
            {
                // enemy bullet hits player
                if (b.Y == _player.Y && b.X == _player.X)
                {
                    _bullets.Clear();
                    FreezeTime();
                    _state = GameState.GameOver;
            return;
                }
            }
        }

        // Enemy movement sweep
        _enemyStepTick++;
        if (_enemyStepTick >= _enemyStepInterval)
        {
            _enemyStepTick = 0;
            // check edges
            bool hitEdge = _enemies.Any(e => (e.X + (e.Shape.Length - 1) >= _right - 1 && _enemyDir > 0) || (e.X <= _left + 1 && _enemyDir < 0));
            if (hitEdge)
            {
                _enemyDir *= -1;
                foreach (var en in _enemies) en.Y += 1;
            }
            else
            {
                foreach (var en in _enemies) en.X += _enemyDir;
            }

            // occasional enemy shot: only one at a time
            if (!_bullets.Any(b => !b.FromPlayer) && _enemies.Count > 0)
            {
                var shooter = _enemies[Random.Shared.Next(_enemies.Count)];
                _bullets.Add(new Bullet { X = shooter.X + shooter.Shape.Length / 2, Y = shooter.Y + 1, Dy = 1, FromPlayer = false });
            }
        }

        // Check victory
        if (_enemies.Count == 0)
        {
            FreezeTime();
            _state = GameState.Win;
        }
    }

    private static void Render()
    {
        _current.Clear();

        DrawBoundingBox();
        DrawUI();

        // player
        Put(_player.X, _player.Y, 'A', ConsoleColor.Cyan);

        // enemies
        foreach (var en in _enemies)
        {
            for (int i = 0; i < en.Shape.Length; i++)
            {
                Put(en.X + i, en.Y, en.Shape[i], en.Color);
            }
        }

        // bullets
        foreach (var b in _bullets)
        {
            Put(b.X, b.Y, b.Glyph, ConsoleColor.White);
        }

        // overlay messages
        if (_state == GameState.Win)
        {
            DrawCenteredMessage(new[] { "YOU WIN!", "Press ENTER to play again, or Q to quit" });
        }
        else if (_state == GameState.GameOver)
        {
            DrawCenteredMessage(new[] { "GAME OVER", "Press ENTER to try again, or Q to quit" });
        }

        // AI status
        if (_aiOllamaEnabled)
        {
            var label = "AI: ON (Ollama)";
            for (int i = 0; i < label.Length && _left + 2 + i < _right; i++) Put(_left + 2 + i, _top + 1, label[i], ConsoleColor.Gray);
            if ((DateTime.UtcNow - _lastAiReasonAt).TotalSeconds < 3 && !string.IsNullOrWhiteSpace(_lastAiReason))
            {
                var r = _lastAiReason.Length > 40 ? _lastAiReason[..40] + "…" : _lastAiReason;
                for (int i = 0; i < r.Length && _left + 2 + i < _right; i++) Put(_left + 2 + i, _top + 2, r[i], ConsoleColor.DarkGray);
            }
        }

        // fps small hint
        if (_showFps)
        {
            var s = $"FPS:{_lastFps,5:0.0}";
            for (int i = 0; i < s.Length; i++) Put(_right - s.Length - 1 + i, _top, s[i], ConsoleColor.Gray);
        }

        // double-buffered diff
        for (int y = 0; y < _current.Height; y++)
        {
            for (int x = 0; x < _current.Width; x++)
            {
                if (_current.Chars[y, x] != _previous.Chars[y, x] || _current.Colors[y, x] != _previous.Colors[y, x])
                {
                    Console.ForegroundColor = _current.Colors[y, x];
                    Console.SetCursorPosition(x, y);
                    Console.Write(_current.Chars[y, x]);
                }
            }
        }

        // swap buffers
        var tmp = _previous; _previous = _current; _current = tmp;
        // clone previous into current for next frame baseline
        Array.Copy(_previous.Chars, _current.Chars, _previous.Chars.Length);
        Array.Copy(_previous.Colors, _current.Colors, _previous.Colors.Length);

    ScreenshotService.MaybeAutoCapture(_previous);
    }

    private static void DrawBoundingBox()
    {
        // Corners
        Put(_left, _top, '┌', ConsoleColor.White);
        Put(_right, _top, '┐', ConsoleColor.White);
        Put(_left, _bottom, '└', ConsoleColor.White);
        Put(_right, _bottom, '┘', ConsoleColor.White);

        // Horizontal
        for (int x = _left + 1; x < _right; x++)
        {
            Put(x, _top, '─', ConsoleColor.White);
            Put(x, _bottom, '─', ConsoleColor.White);
        }
        // Vertical
        for (int y = _top + 1; y < _bottom; y++)
        {
            Put(_left, y, '│', ConsoleColor.White);
            Put(_right, y, '│', ConsoleColor.White);
        }
    }

    private static void DrawUI()
    {
    _bulletsActive = _bullets.Count(b => b.FromPlayer);
    var maxText = _bulletsMax < 0 ? "∞" : _bulletsMax.ToString();
    string ui = $"Score: {_score:0000}   Time: {ElapsedSeconds:00}s   Bullets: {_bulletsActive}/{maxText}";
        int x = _left + 2;
        int y = _top;
        for (int i = 0; i < ui.Length && x + i < _right; i++)
        {
            Put(x + i, y, ui[i], ConsoleColor.White);
        }
    }

    private static void Put(int x, int y, char ch, ConsoleColor color)
    {
        if (x < 0 || x >= _current.Width || y < 0 || y >= _current.Height) return;
        _current.Chars[y, x] = ch;
        _current.Colors[y, x] = color;
    }

    private static void DrawCenteredMessage(string[] lines)
    {
        int areaWidth = _right - _left - 1;
        int areaHeight = _bottom - _top - 1;
        int startY = _top + areaHeight / 2 - lines.Length / 2;
        foreach (var line in lines)
        {
            int left = _left + 1 + Math.Max(0, (areaWidth - line.Length) / 2);
            for (int i = 0; i < line.Length && left + i < _right; i++)
            {
                Put(left + i, startY, line[i], ConsoleColor.White);
            }
            startY++;
        }
    }

    private static DateTime _lastAiQuery = DateTime.MinValue;
    private static string _lastAiReason = string.Empty;
    private static DateTime _lastAiReasonAt = DateTime.MinValue;

    private static void FreezeTime()
    {
        _frozenSeconds = (int)(DateTime.UtcNow - _start).TotalSeconds;
    }

    private static void InitEntities()
    {
        _player = new Player { X = (_right + _left) / 2, Y = _bottom - 2 };
        _enemies.Clear();
        _bullets.Clear();
        _enemyDir = 1; _enemyStepTick = 0;

        // Top row (5): ><, oo, ><, oo, >< — Red
        int startX = _left + 4;
        int yTop = _top + 3;
        string[] topShapes = ["><", "oo", "><", "oo", "><"];
        int x = startX;
        foreach (var s in topShapes)
        {
            _enemies.Add(new Enemy { X = x, Y = yTop, Shape = s, Color = ConsoleColor.Red });
            x += s.Length + 4;
        }

        // Bottom row (3): /O\ — DarkYellow
        int yBottom = yTop + 2;
        for (int i = 0; i < 3; i++)
        {
            _enemies.Add(new Enemy { X = startX + i * 8, Y = yBottom, Shape = "/O\\", Color = ConsoleColor.DarkYellow });
        }
    }

    private static void MovePlayer(int dx)
    {
        int newX = Math.Clamp(_player.X + dx, _left + 1, _right - 1);
        _player.X = newX;
    }

    private static void TryFire()
    {
    int activePlayerBullets = _bullets.Count(b => b.FromPlayer);
    if (_bulletsMax >= 0 && activePlayerBullets >= _bulletsMax) return;
        _bullets.Add(new Bullet { X = _player.X, Y = _player.Y - 1, Dy = -1, FromPlayer = true });
    }

    private static string SnapshotString()
    {
        var sb = new StringBuilder();
        for (int y = _top + 1; y < _bottom; y++)
        {
            for (int x = _left + 1; x < _right; x++)
            {
                char ch = ' ';
                if (y == _player.Y && x == _player.X) ch = 'A';
                else
                {
                    foreach (var en in _enemies)
                    {
                        int len = en.Shape.Length;
                        if (y == en.Y && x >= en.X && x < en.X + len) { ch = en.Shape[x - en.X]; break; }
                    }
                    if (ch == ' ')
                    {
                        foreach (var b in _bullets)
                        {
                            if (b.X == x && b.Y == y) { ch = b.Glyph; break; }
                        }
                    }
                }
                sb.Append(ch);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
