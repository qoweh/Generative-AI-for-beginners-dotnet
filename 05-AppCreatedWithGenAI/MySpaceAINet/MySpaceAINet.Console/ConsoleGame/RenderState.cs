// This file forwards to the shared RenderState in MySpaceAINet.Screenshot
namespace MySpaceAINet.ConsoleGame;

using SharedRenderState = MySpaceAINet.Screenshot.RenderState;

public class RenderState : SharedRenderState
{
    public RenderState(int width, int height) : base(width, height) { }
}
