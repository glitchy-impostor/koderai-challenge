using System.Text.Json;
using System.Text.Json.Nodes;
using DebateScoringEngine.Core.Domain.Enums;
using DebateScoringEngine.Core.Domain.Models;

namespace DebateScoringEngine.Api.Services;

/// <summary>
/// Enriches argument objects with LLM-detected metadata: evidence quality,
/// impact magnitude, fallacies, and argument status.
///
/// Design:
///   - One LLM call per argument (not batched) — simpler error isolation,
///     acceptable latency for ≤30 args. If batching becomes necessary, 
///     change BatchSize below.
///   - Prompts request JSON-only responses — no markdown, no preamble.
///   - All parsing errors are soft-fails: the argument is returned with
///     its original nulls rather than crashing the whole batch.
///   - Explicit enrichment fields (non-null) are NEVER overwritten —
///     the LLM only fills gaps.
/// </summary>
public class LlmEnrichmentService
{
    private readonly ILlmProvider _provider;
    private readonly ILogger<LlmEnrichmentService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string SystemPrompt = """
        You are an expert debate analyst and policy debate judge.
        Your task is to analyze a debate argument and classify its properties.
        
        IMPORTANT: Respond ONLY with a JSON object. No explanation, no markdown, no code fences.
        
        The JSON object must have exactly these fields:
        {
          "evidenceQuality": "<one of: PeerReviewed, ExpertOpinion, NewsSource, Anecdotal, Unverified>",
          "impactMagnitude": "<one of: Existential, Catastrophic, Significant, Minor, Negligible>",
          "fallacies": ["<FallacyType>", ...],
          "argumentStatus": "<one of: Active, Dropped, Conceded, Extended, null>",
          "notes": "<brief explanation of your classification>"
        }
        
        Valid fallacy types: StrawMan, AdHominem, AppealToAuthority, FalseDichotomy,
        SlipperySlope, Repetition, CircularReasoning, HastyGeneralization, RedHerring.
        
        Use an empty array [] if no fallacies are detected.
        Use null for argumentStatus if you cannot determine it from context.
        
        Evidence quality guide:
        - PeerReviewed: academic papers, government reports, systematic reviews
        - ExpertOpinion: testimony from named experts, think tank reports
        - NewsSource: journalism, media reports
        - Anecdotal: case studies, isolated examples, personal accounts
        - Unverified: assertions without sourcing
        
        Impact magnitude guide:
        - Existential: threatens human existence or civilization
        - Catastrophic: severe, large-scale, irreversible harms
        - Significant: meaningful harms affecting many people
        - Minor: limited or easily recoverable harms
        - Negligible: trivial impact
        """;

    public LlmEnrichmentService(
        ILlmProvider provider,
        ILogger<LlmEnrichmentService> logger)
    {
        _provider = provider;
        _logger   = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public record EnrichmentResult(
        Debate EnrichedDebate,
        int    FieldsFilled,
        List<string> SkippedIds,
        int    TotalInputTokens,
        int    TotalOutputTokens);

    /// <summary>
    /// Enriches all arguments in the debate that have null enrichment fields.
    /// Returns a new Debate object with enriched arguments — original is unchanged.
    /// </summary>
    public async Task<EnrichmentResult> EnrichDebateAsync(
        Debate debate,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var enrichedArgs  = new Dictionary<string, Argument>(debate.Arguments);
        var skippedIds    = new List<string>();
        int fieldsFilled  = 0;
        int totalInputTok = 0;
        int totalOutputTok = 0;

        foreach (var (id, argument) in debate.Arguments)
        {
            // Skip if already fully enriched
            if (IsFullyEnriched(argument.Enrichment))
            {
                _logger.LogDebug("Argument {Id} already enriched, skipping.", id);
                continue;
            }

            _logger.LogDebug("Enriching argument {Id} ({Issue})...", id, argument.StockIssueTag);

            var prompt   = BuildPrompt(argument);
            var response = await _provider.CompleteAsync(
                SystemPrompt, prompt, apiKey, cancellationToken);

            totalInputTok  += response.InputTokens;
            totalOutputTok += response.OutputTokens;

            if (!response.Success)
            {
                _logger.LogWarning(
                    "LLM call failed for argument {Id}: {Error}", id, response.ErrorMessage);
                skippedIds.Add(id);
                continue;
            }

            var (enriched, filled, parseError) = ParseResponse(response.Content, argument);

            if (parseError != null)
            {
                _logger.LogWarning(
                    "Could not parse LLM response for argument {Id}: {Error}", id, parseError);
                skippedIds.Add(id);
                continue;
            }

            enrichedArgs[id] = enriched;
            fieldsFilled    += filled;
        }

        _logger.LogInformation(
            "Enrichment complete. {Filled} fields filled, {Skipped} arguments skipped. " +
            "Tokens used: {InTok} in / {OutTok} out.",
            fieldsFilled, skippedIds.Count, totalInputTok, totalOutputTok);

        // Build new Debate with enriched arguments
        // Clone the Debate with the enriched arguments dictionary
        var enrichedDebate = new Debate
        {
            DebateId            = debate.DebateId,
            RoundId             = debate.RoundId,
            Teams               = debate.Teams,
            Speakers            = debate.Speakers,
            Speeches            = debate.Speeches,
            Arguments           = enrichedArgs,
            CrossExaminations   = debate.CrossExaminations,
            PrepTimeUsedSeconds = debate.PrepTimeUsedSeconds,
        };

        return new EnrichmentResult(
            enrichedDebate, fieldsFilled, skippedIds,
            totalInputTok, totalOutputTok);
    }

    // ── Prompt builder ────────────────────────────────────────────────────────

    private static string BuildPrompt(Argument argument)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Analyze this debate argument:");
        sb.AppendLine($"  Stock Issue  : {argument.StockIssueTag}");
        sb.AppendLine($"  Side         : {argument.Side}");
        sb.AppendLine($"  Speech       : {argument.SpeechId}");
        sb.AppendLine($"  Claim        : {argument.Core.Claim}");
        sb.AppendLine($"  Reasoning    : {argument.Core.Reasoning}");
        sb.AppendLine($"  Impact       : {argument.Core.Impact}");

        if (!string.IsNullOrWhiteSpace(argument.Core.EvidenceSource))
            sb.AppendLine($"  EvidenceSource: {argument.Core.EvidenceSource}");

        // Tell the LLM which fields are already set so it doesn't override them
        var enrichment = argument.Enrichment;
        var alreadySet = new List<string>();
        if (enrichment.EvidenceQuality.HasValue) alreadySet.Add($"evidenceQuality={enrichment.EvidenceQuality}");
        if (enrichment.ImpactMagnitude.HasValue)  alreadySet.Add($"impactMagnitude={enrichment.ImpactMagnitude}");
        if (enrichment.Fallacies?.Count > 0)      alreadySet.Add($"fallacies already set");
        if (enrichment.Status.HasValue)            alreadySet.Add($"status={enrichment.Status}");

        if (alreadySet.Count > 0)
            sb.AppendLine($"\nAlready known (DO NOT OVERRIDE): {string.Join(", ", alreadySet)}");

        sb.AppendLine("\nClassify the null/unknown fields only. Return JSON.");
        return sb.ToString();
    }

    // ── Response parser ───────────────────────────────────────────────────────

    private static (Argument enriched, int fieldsFilled, string? error) ParseResponse(
        string content,
        Argument original)
    {
        // Strip possible markdown fences the model may have added despite instructions
        var json = content.Trim();
        if (json.StartsWith("```")) json = json.Split('\n').Skip(1).SkipLast(1).Aggregate((a, b) => a + "\n" + b).Trim();

        JsonNode? doc;
        try   { doc = JsonNode.Parse(json); }
        catch { return (original, 0, $"JSON parse failed. Raw response: {content[..Math.Min(200, content.Length)]}"); }

        if (doc == null)
            return (original, 0, "Empty JSON response.");

        var existing = original.Enrichment;
        int filled   = 0;

        // Only fill fields that are currently null — never overwrite explicit values
        EvidenceQuality? evidenceQuality = existing.EvidenceQuality;
        if (evidenceQuality == null)
        {
            var val = doc["evidenceQuality"]?.GetValue<string>();
            if (Enum.TryParse<EvidenceQuality>(val, ignoreCase: true, out var eq))
            {
                evidenceQuality = eq;
                filled++;
            }
        }

        ImpactMagnitude? impactMagnitude = existing.ImpactMagnitude;
        if (impactMagnitude == null)
        {
            var val = doc["impactMagnitude"]?.GetValue<string>();
            if (Enum.TryParse<ImpactMagnitude>(val, ignoreCase: true, out var im))
            {
                impactMagnitude = im;
                filled++;
            }
        }

        List<FallacyType>? fallacies = existing.Fallacies;
        if (fallacies == null || fallacies.Count == 0)
        {
            var arr = doc["fallacies"]?.AsArray();
            if (arr != null)
            {
                var parsed = arr
                    .Select(f => f?.GetValue<string>())
                    .Where(f => f != null)
                    .Select(f => Enum.TryParse<FallacyType>(f!, ignoreCase: true, out var ft)
                        ? (FallacyType?)ft : null)
                    .Where(f => f.HasValue)
                    .Select(f => f!.Value)
                    .ToList();
                fallacies = parsed;
                if (parsed.Count > 0) filled++;
            }
        }

        ArgumentStatus? status = existing.Status;
        if (status == null)
        {
            var val = doc["argumentStatus"]?.GetValue<string>();
            if (val != null && val != "null" &&
                Enum.TryParse<ArgumentStatus>(val, ignoreCase: true, out var st))
            {
                status = st;
                filled++;
            }
        }

        var enrichedEnrichment = new ArgumentEnrichment
        {
            EvidenceQuality  = evidenceQuality,
            ImpactMagnitude  = impactMagnitude,
            Fallacies        = fallacies,
            Status           = status,
            ArgumentStrength = existing.ArgumentStrength,
        };

        // Clone the argument with new enrichment (classes, not records — manual clone)
        var enriched = new Argument
        {
            ArgumentId        = original.ArgumentId,
            SpeechId          = original.SpeechId,
            SpeakerId         = original.SpeakerId,
            Side              = original.Side,
            StockIssueTag     = original.StockIssueTag,
            StockCaseId       = original.StockCaseId,
            RebuttalTargetIds = original.RebuttalTargetIds,
            Core              = original.Core,
            Enrichment        = enrichedEnrichment,
        };
        return (enriched, filled, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsFullyEnriched(ArgumentEnrichment e) =>
        e.EvidenceQuality.HasValue &&
        e.ImpactMagnitude.HasValue &&
        e.Fallacies != null;
}
