using OllamaSharp;

public class OllamaGameActionProvider : GameActionProviderBase, IGameActionProvider
{
    // Default to a commonly available local chat model; can be overridden via constructor
    public OllamaGameActionProvider(string model = "phi3.5:latest", string uriString = "http://localhost:11434")
    {
        Uri uri = new(uriString);
        var ollama = new OllamaApiClient(uri);
        ollama.SelectedModel = model;

        chat = new OllamaApiClient(uri, model);
    }
}
