namespace MySpaceAINet.GameActionProcessor;

public class OllamaActionProvider : IGameActionProvider
{
    private readonly HttpClient _http = new();
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaActionProvider(string? model = null, string? baseUrl = null)
    {
        _model = model ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";
        // _model = model ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "phi3.5:latest";
        _baseUrl = baseUrl ?? Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
    }
    

    public async ValueTask<(GameAction action, string? reason)> GetNextActionAsync(IReadOnlyList<string> lastFrames, GameAction lastAction, CancellationToken ct = default)
    {
        var prompt = $"You are playing a console Space Invaders like game. Decide the next action given the ASCII frames and last action. Only reply JSON {{\"action\":\"move_left|move_right|fire|stop\", \"reason\":\"...\"}}.\nLast action: {lastAction}\nFrames (oldest->newest):\n" + string.Join("\n---\n", lastFrames);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/api/generate");
        var payload = new
        {
            model = _model,
            prompt,
            stream = false
        };
        req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var jsonText = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            using var gen = System.Text.Json.JsonDocument.Parse(jsonText);
            var responseText = gen.RootElement.GetProperty("response").GetString() ?? string.Empty;
            using var ai = System.Text.Json.JsonDocument.Parse(responseText);
            var act = ai.RootElement.GetProperty("action").GetString();
            var reason = ai.RootElement.TryGetProperty("reason", out var r) ? r.GetString() : null;
            return (act switch
            {
                "move_left" => GameAction.MoveLeft,
                "move_right" => GameAction.MoveRight,
                "fire" => GameAction.Fire,
                _ => GameAction.Stop
            }, reason);
        }
        catch
        {
            return (GameAction.Stop, "parse_error");
        }
    }
}
