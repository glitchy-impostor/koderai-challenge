using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DebateScoringEngine.Api.Services;

/// <summary>
/// Calls the OpenAI Chat Completions API.
/// Compatible with any OpenAI-format API (OpenAI, Azure OpenAI, local Ollama, etc.)
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _http;
    private readonly string _model;

    public string ProviderName => "OpenAI";

    public OpenAiProvider(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http  = httpClientFactory.CreateClient("openai");
        _model = config["Llm:OpenAiModel"] ?? "gpt-4o";
    }

    public async Task<LlmResponse> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model    = _model,
            messages = new[]
            {
                new { role = "system",  content = systemPrompt },
                new { role = "user",    content = userPrompt   }
            },
            max_tokens = 1024
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Content = content;
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            var body     = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new LlmResponse
                {
                    Success      = false,
                    ErrorMessage = $"OpenAI API returned {(int)response.StatusCode}: {body}"
                };

            var doc    = JsonNode.Parse(body);
            var text   = doc?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? string.Empty;
            var inTok  = doc?["usage"]?["prompt_tokens"]?.GetValue<int>()     ?? 0;
            var outTok = doc?["usage"]?["completion_tokens"]?.GetValue<int>() ?? 0;

            return new LlmResponse
            {
                Success      = true,
                Content      = text,
                InputTokens  = inTok,
                OutputTokens = outTok
            };
        }
        catch (Exception ex)
        {
            return new LlmResponse
            {
                Success      = false,
                ErrorMessage = $"Request failed: {ex.Message}"
            };
        }
    }
}
