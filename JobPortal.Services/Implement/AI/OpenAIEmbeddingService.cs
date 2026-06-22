using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;

namespace JobPortal.Services.AI;

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "OpenAI:ApiKey is missing.");

        _client = new EmbeddingClient(
            model: "text-embedding-3-small",
            apiKey: apiKey);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var response =
            await _client.GenerateEmbeddingAsync(text);

        return response.Value.ToFloats().ToArray();
    }
}