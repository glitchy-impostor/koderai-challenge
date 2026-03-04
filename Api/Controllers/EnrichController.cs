using Microsoft.AspNetCore.Mvc;
using DebateScoringEngine.Api.Models;
using DebateScoringEngine.Api.Services;

namespace DebateScoringEngine.Api.Controllers;

/// <summary>
/// LLM enrichment endpoints.
///
/// POST /api/enrich        — enrich a debate's arguments via LLM, return enriched debate
/// POST /api/enrich/score  — enrich then immediately score (one round-trip for the frontend)
/// GET  /api/enrich/providers — list available LLM providers and current config
/// </summary>
[ApiController]
[Route("api/enrich")]
public class EnrichController : ControllerBase
{
    private readonly ConfigService             _configs;
    private readonly LlmEnrichmentService      _enrichment;
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly IConfiguration            _config;
    private readonly ILogger<EnrichController> _logger;

    public EnrichController(
        ConfigService                configs,
        LlmEnrichmentService         enrichment,
        IEnumerable<ILlmProvider>    providers,
        IConfiguration               config,
        ILogger<EnrichController>    logger)
    {
        _configs    = configs;
        _enrichment = enrichment;
        _providers  = providers;
        _config     = config;
        _logger     = logger;
    }

    // ── POST /api/enrich ──────────────────────────────────────────────────────

    /// <summary>
    /// Enriches all arguments in the debate that have null enrichment fields.
    /// Returns the debate with fields filled in — does NOT score.
    /// The caller can review the enriched debate, adjust values, then submit to /api/debate/score.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Enrich(
        [FromBody] EnrichDebateRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Debate == null)
            return BadRequest(new ApiError { Error = "Request body must contain a 'debate' object." });

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new ApiError { Error = "'apiKey' is required." });

        var provider = ResolveProvider(request.ProviderOverride);
        if (provider == null)
            return BadRequest(new ApiError
            {
                Error = $"Unknown provider '{request.ProviderOverride}'. " +
                        $"Available: {string.Join(", ", _providers.Select(p => p.ProviderName))}"
            });

        _logger.LogInformation(
            "Enriching debate {DebateId} via {Provider} ({ArgCount} arguments).",
            request.Debate.DebateId, provider.ProviderName,
            request.Debate.Arguments.Count);

        try
        {
            var result = await _enrichment.EnrichDebateAsync(
                request.Debate, request.ApiKey, cancellationToken);

            return Ok(new EnrichDebateResponse
            {
                EnrichedDebate    = result.EnrichedDebate,
                FieldsFilled      = result.FieldsFilled,
                SkippedArgumentIds = result.SkippedIds,
                Warning = result.SkippedIds.Count > 0
                    ? $"{result.SkippedIds.Count} argument(s) could not be enriched. " +
                      "They retain their original null values."
                    : null
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new ApiError { Error = "Request cancelled." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enriching debate {DebateId}", request.Debate?.DebateId);
            return StatusCode(500, new ApiError
            {
                Error   = "Enrichment failed.",
                Details = new() { ex.Message }
            });
        }
    }

    // ── POST /api/enrich/score ────────────────────────────────────────────────

    /// <summary>
    /// Convenience endpoint: enrich then score in one call.
    /// Returns both the enriched debate and the scoring result.
    /// Saves a round-trip for the common case where the user wants to
    /// submit raw arguments and get a full result immediately.
    /// </summary>
    [HttpPost("score")]
    public async Task<IActionResult> EnrichAndScore(
        [FromBody] EnrichDebateRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Debate == null)
            return BadRequest(new ApiError { Error = "Request body must contain a 'debate' object." });

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new ApiError { Error = "'apiKey' is required." });

        var provider = ResolveProvider(request.ProviderOverride);
        if (provider == null)
            return BadRequest(new ApiError { Error = $"Unknown provider '{request.ProviderOverride}'." });

        try
        {
            // Stage 1: Enrich
            _logger.LogInformation(
                "Enrich+Score: enriching debate {DebateId}...", request.Debate.DebateId);

            var enrichResult = await _enrichment.EnrichDebateAsync(
                request.Debate, request.ApiKey, cancellationToken);

            var enrichedDebate = enrichResult.EnrichedDebate;

            // Stage 2: Score
            _logger.LogInformation("Enrich+Score: scoring...");

            var format  = _configs.GetFormat();
            var scoring = _configs.GetScoring();
            var round   = _configs.GetRound();

            var builder = new DebateScoringEngine.Core.FlowGraph.FlowGraphBuilder(format, scoring, round);
            var flow    = builder.Build(enrichedDebate);
            var engine  = new DebateScoringEngine.Core.Scoring.ScoringEngine();
            var scoreResult = engine.Score(flow, enrichedDebate, format, scoring, round);

            var explanation = DebateScoringEngine.Core.Output.ExplanationGenerator
                .GenerateFull(scoreResult, flow, format, round);

            _logger.LogInformation(
                "Enrich+Score complete. Winner: {Winner}. Fields filled: {Fields}.",
                scoreResult.Winner, enrichResult.FieldsFilled);

            return Ok(new
            {
                enrichedDebate     = enrichResult.EnrichedDebate,
                fieldsFilled       = enrichResult.FieldsFilled,
                skippedArgumentIds = enrichResult.SkippedIds,
                scoringResult      = scoreResult,
                fullExplanation    = explanation,
                flowSummary        = flow.GetSummary()
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new ApiError { Error = "Request cancelled." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in enrich+score for debate {DebateId}", request.Debate?.DebateId);
            return StatusCode(500, new ApiError
            {
                Error   = "Enrich+score pipeline failed.",
                Details = new() { ex.Message }
            });
        }
    }

    // ── GET /api/enrich/providers ─────────────────────────────────────────────

    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var configured = _config["Llm:Provider"] ?? "Anthropic";
        return Ok(new
        {
            configured,
            available = _providers.Select(p => p.ProviderName).ToList()
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ILlmProvider? ResolveProvider(string? overrideName)
    {
        var name = overrideName ?? _config["Llm:Provider"] ?? "Anthropic";
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
