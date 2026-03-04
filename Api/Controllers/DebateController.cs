using Microsoft.AspNetCore.Mvc;
using DebateScoringEngine.Api.Models;
using DebateScoringEngine.Api.Services;
using DebateScoringEngine.Core.FlowGraph;
using DebateScoringEngine.Core.Output;
using DebateScoringEngine.Core.Scoring;

namespace DebateScoringEngine.Api.Controllers;

/// <summary>
/// Core debate operations: score a debate, retrieve its flow graph.
///
/// POST /api/debate/score  — run full pipeline, return ScoringResult + explanation
/// GET  /api/debate/flow   — build and return the flow graph for the current debate
///                           (useful for the frontend to render the flow sheet before scoring)
/// </summary>
[ApiController]
[Route("api/debate")]
public class DebateController : ControllerBase
{
    private readonly ConfigService  _configs;
    private readonly ScoringEngine  _engine;
    private readonly ILogger<DebateController> _logger;

    public DebateController(
        ConfigService configs,
        ScoringEngine engine,
        ILogger<DebateController> logger)
    {
        _configs = configs;
        _engine  = engine;
        _logger  = logger;
    }

    // ── POST /api/debate/score ────────────────────────────────────────────────

    /// <summary>
    /// Runs the full three-stage scoring pipeline on the submitted debate.
    /// Returns a ScoringResult with winner, breakdown, and explanation.
    /// </summary>
    [HttpPost("score")]
    public IActionResult Score([FromBody] ScoreDebateRequest request)
    {
        if (request?.Debate == null)
            return BadRequest(new ApiError { Error = "Request body must contain a 'debate' object." });

        try
        {
            var format  = _configs.GetFormat();
            var scoring = _configs.GetScoring();
            var round   = _configs.GetRound();

            // Stage 1: Build flow graph
            var builder = new FlowGraphBuilder(format, scoring, round);
            var flow    = builder.Build(request.Debate);

            // Stage 2: Score
            var result = _engine.Score(flow, request.Debate, format, scoring, round);

            // Stage 3: Enrich explanation if requested
            string fullExplanation = request.IncludeFullExplanation
                ? ExplanationGenerator.GenerateFull(result, flow, format, round)
                : result.WinnerExplanation;

            var summary = flow.GetSummary();

            _logger.LogInformation(
                "Scored debate {DebateId}: {Winner} wins ({AFF:F2} vs {NEG:F2}). " +
                "{Args} args, {Dropped} dropped.",
                request.Debate.DebateId, result.Winner,
                result.AffTotalScore, result.NegTotalScore,
                summary.TotalArguments, summary.DroppedArguments);

            return Ok(new
            {
                result,
                fullExplanation,
                flowSummary = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scoring debate {DebateId}", request.Debate?.DebateId);
            return StatusCode(500, new ApiError
            {
                Error   = "Scoring failed.",
                Details = new() { ex.Message }
            });
        }
    }

    // ── GET /api/debate/flow ──────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns the flow graph for a debate without scoring it.
    /// Used by the frontend to render the flow sheet immediately after input.
    /// The debate is passed as a query-string-encoded JSON body (POST-as-GET pattern).
    /// </summary>
    [HttpPost("flow")]
    public IActionResult BuildFlow([FromBody] ScoreDebateRequest request)
    {
        if (request?.Debate == null)
            return BadRequest(new ApiError { Error = "Request body must contain a 'debate' object." });

        try
        {
            var format  = _configs.GetFormat();
            var scoring = _configs.GetScoring();
            var round   = _configs.GetRound();

            var builder = new FlowGraphBuilder(format, scoring, round);
            var flow    = builder.Build(request.Debate);

            // Serialize a UI-friendly representation of the flow graph
            var nodes = flow.Nodes.Values.Select(n => new
            {
                n.ArgumentId,
                n.SpeechId,
                n.Side,
                n.StockIssueTag,
                n.Status,
                n.ComputedStrength,
                n.StatusIsOverridden,
                ResolvedEnrichment = new
                {
                    n.Resolved.EvidenceQuality,
                    n.Resolved.ImpactMagnitude,
                    n.Resolved.Fallacies,
                    n.Resolved.EvidenceSource,
                    n.Resolved.ImpactSource,
                }
            });

            var edges = flow.Edges.Select(e => new
            {
                e.SourceArgumentId,
                e.SourceSpeechId,
                e.TargetArgumentId,
                e.TargetSpeechId
            });

            var speechIndex = (string id) => format.GetSpeechIndex(id);
            var threads = flow.GetThreads(speechIndex).Select(t => new
            {
                RootArgumentId = t.Root.ArgumentId,
                t.StockIssueTag,
                NodeIds = t.AllNodes.Select(n => n.ArgumentId)
            });

            return Ok(new
            {
                debateId = request.Debate.DebateId,
                summary  = flow.GetSummary(),
                nodes,
                edges,
                threads
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building flow for debate {DebateId}", request.Debate?.DebateId);
            return StatusCode(500, new ApiError
            {
                Error   = "Flow graph build failed.",
                Details = new() { ex.Message }
            });
        }
    }
}
