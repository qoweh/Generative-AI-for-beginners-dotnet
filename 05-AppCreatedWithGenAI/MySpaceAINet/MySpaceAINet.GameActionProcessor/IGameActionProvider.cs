namespace MySpaceAINet.GameActionProcessor;

public enum GameAction { Stop, MoveLeft, MoveRight, Fire }

public interface IGameActionProvider
{
    ValueTask<(GameAction action, string? reason)> GetNextActionAsync(IReadOnlyList<string> lastFrames, GameAction lastAction, CancellationToken ct = default);
}
