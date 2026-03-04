using Microsoft.AspNetCore.Mvc;
using DebateScoringEngine.Api.Models;
using DebateScoringEngine.Api.Services;
using DebateScoringEngine.Core.Config;

namespace DebateScoringEngine.Api.Controllers;

/// <summary>
/// CRUD endpoints for all three config files + stock case library.
/// These back the Settings page in the frontend.
///
/// GET  /api/config/format      → read format-config.json
/// PUT  /api/config/format      → write format-config.json
/// GET  /api/config/scoring     → read scoring-config.json
/// PUT  /api/config/scoring     → write scoring-config.json
/// GET  /api/config/round       → read round-config.json
/// PUT  /api/config/round       → write round-config.json
/// GET  /api/config/stockcases  → list all stock cases (system + user)
/// POST /api/config/stockcases  → add a user-defined stock case
/// DELETE /api/config/stockcases/{id} → remove a user-defined stock case
/// </summary>
[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConfigService _configs;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(ConfigService configs, ILogger<ConfigController> logger)
    {
        _configs = configs;
        _logger  = logger;
    }

    // ── Format config ─────────────────────────────────────────────────────────

    [HttpGet("format")]
    public IActionResult GetFormat()
    {
        try   { return Ok(_configs.GetFormat()); }
        catch (Exception ex) { return ConfigReadError("format-config.json", ex); }
    }

    [HttpPut("format")]
    public IActionResult PutFormat([FromBody] FormatConfig config)
    {
        if (config == null)
            return BadRequest(new ApiError { Error = "Request body must be a FormatConfig object." });
        try
        {
            _configs.SaveFormat(config);
            _logger.LogInformation("format-config.json updated via Settings.");
            return Ok(new { saved = true });
        }
        catch (Exception ex) { return ConfigWriteError("format-config.json", ex); }
    }

    // ── Scoring config ────────────────────────────────────────────────────────

    [HttpGet("scoring")]
    public IActionResult GetScoring()
    {
        try   { return Ok(_configs.GetScoring()); }
        catch (Exception ex) { return ConfigReadError("scoring-config.json", ex); }
    }

    [HttpPut("scoring")]
    public IActionResult PutScoring([FromBody] ScoringConfig config)
    {
        if (config == null)
            return BadRequest(new ApiError { Error = "Request body must be a ScoringConfig object." });
        try
        {
            _configs.SaveScoring(config);
            _logger.LogInformation("scoring-config.json updated via Settings.");
            return Ok(new { saved = true });
        }
        catch (Exception ex) { return ConfigWriteError("scoring-config.json", ex); }
    }

    // ── Round config ──────────────────────────────────────────────────────────

    [HttpGet("round")]
    public IActionResult GetRound()
    {
        try   { return Ok(_configs.GetRound()); }
        catch (Exception ex) { return ConfigReadError("round-config.json", ex); }
    }

    [HttpPut("round")]
    public IActionResult PutRound([FromBody] RoundConfig config)
    {
        if (config == null)
            return BadRequest(new ApiError { Error = "Request body must be a RoundConfig object." });
        try
        {
            _configs.SaveRound(config);
            _logger.LogInformation("round-config.json updated via Settings.");
            return Ok(new { saved = true });
        }
        catch (Exception ex) { return ConfigWriteError("round-config.json", ex); }
    }

    // ── Stock cases ───────────────────────────────────────────────────────────

    [HttpGet("stockcases")]
    public IActionResult GetStockCases()
    {
        try
        {
            var cases = _configs.GetAllStockCases();
            return Ok(new
            {
                total      = cases.Count,
                system     = cases.Count(sc => sc.Source == "system"),
                user       = cases.Count(sc => sc.Source != "system"),
                stockCases = cases
            });
        }
        catch (Exception ex) { return ConfigReadError("stock cases", ex); }
    }

    [HttpPost("stockcases")]
    public IActionResult AddStockCase([FromBody] StockCase stockCase)
    {
        if (stockCase == null)
            return BadRequest(new ApiError { Error = "Request body must be a StockCase object." });

        if (string.IsNullOrWhiteSpace(stockCase.StockCaseId))
            return BadRequest(new ApiError { Error = "StockCase.stockCaseId is required." });

        if (string.IsNullOrWhiteSpace(stockCase.Label))
            return BadRequest(new ApiError { Error = "StockCase.label is required." });

        // Force source to "user" — cannot add system cases via API
        var toAdd = new StockCase
        {
            StockCaseId       = stockCase.StockCaseId,
            Label             = stockCase.Label,
            StockIssueTag     = stockCase.StockIssueTag,
            Side              = stockCase.Side,
            Source            = "user",
            DefaultEnrichment = stockCase.DefaultEnrichment,
            BlueprintArgument = stockCase.BlueprintArgument,
        };

        var (success, error) = _configs.AddUserStockCase(toAdd);
        if (!success)
            return Conflict(new ApiError { Error = error ?? "Could not add stock case." });

        _logger.LogInformation("User stock case '{Id}' added.", toAdd.StockCaseId);
        return CreatedAtAction(nameof(GetStockCases), new { }, new { added = true, stockCaseId = toAdd.StockCaseId });
    }

    [HttpDelete("stockcases/{stockCaseId}")]
    public IActionResult DeleteStockCase(string stockCaseId)
    {
        var (success, error) = _configs.DeleteUserStockCase(stockCaseId);
        if (!success)
            return NotFound(new ApiError { Error = error ?? $"Stock case '{stockCaseId}' not found." });

        _logger.LogInformation("User stock case '{Id}' deleted.", stockCaseId);
        return Ok(new { deleted = true, stockCaseId });
    }

    // ── Error helpers ─────────────────────────────────────────────────────────

    private ObjectResult ConfigReadError(string file, Exception ex)
    {
        _logger.LogError(ex, "Error reading {File}", file);
        return StatusCode(500, new ApiError
        {
            Error   = $"Failed to read {file}.",
            Details = new() { ex.Message }
        });
    }

    private ObjectResult ConfigWriteError(string file, Exception ex)
    {
        _logger.LogError(ex, "Error writing {File}", file);
        return StatusCode(500, new ApiError
        {
            Error   = $"Failed to write {file}.",
            Details = new() { ex.Message }
        });
    }
}
