using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DebateScoringEngine.Api.Services;

/// <summary>
/// Calls the Anthropic Messages API.
/// Uses HttpClient directly — no SDK dependency required.
/// Model is configurable via appsettings.json Llm:Model.
/// </summary>
public class AnthropicProvider : ILlmProvider
{
    private const string BaseUrl        = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly string _model;

    public string ProviderName => "Anthropic";

    public AnthropicProvider(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http  = httpClientFactory.CreateClient("anthropic");
        _model = config["Llm:Model"] ?? "claude-sonnet-4-20250514";
    }

    public async Task<LlmResponse> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model      = _model,
            max_tokens = 1024,
            system     = systemPrompt,
            messages   = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Content = content;
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            var body     = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new LlmResponse
                {
                    Success      = false,
                    ErrorMessage = $"Anthropic API returned {(int)response.StatusCode}: {body}"
                };

            var doc  = JsonNode.Parse(body);
            var text = doc?["content"]?[0]?["text"]?.GetValue<string>() ?? string.Empty;
            var inTok  = doc?["usage"]?["input_tokens"]?.GetValue<int>()  ?? 0;
            var outTok = doc?["usage"]?["output_tokens"]?.GetValue<int>() ?? 0;

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
