# Debate Scoring Engine

A deterministic, config-driven debate judging system built in C# (.NET 8). Given a structured debate transcript, it produces a winner, a multi-dimensional score breakdown at the team, issue, rule, argument, and speaker level, and a human-readable ballot explanation.

Includes a React frontend with a flow-sheet visualizer, score panel, enrichment tools, and live config editing.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Project Structure](#project-structure)
3. [Running the CLI](#running-the-cli)
4. [Running the API + Frontend](#running-the-api--frontend)
5. [Input Format](#input-format)
6. [Configuration](#configuration)
7. [Sample Debates](#sample-debates)
8. [Running Tests](#running-tests)
9. [Design Decisions](#design-decisions)
10. [Trade-offs and Limitations](#trade-offs-and-limitations)
11. [Self-Critique](#self-critique)

---

## Quick Start

**Prerequisites:** .NET 8 SDK, Node.js 18+

```bash
# Clone and build
git clone <repo>
cd DebateScoringEngine
dotnet build DebateScoringEngine.sln

# Score a sample debate via CLI
dotnet run --project Cli -- --input Samples/sample-debate.json --brief

# Run all tests
dotnet test Tests/DebateScoringEngine.Tests.csproj

# Start the API
dotnet run --project Api

# Start the frontend (separate terminal)
cd Frontend && npm install && npm run dev
```

---

## Project Structure

```
DebateScoringEngine/
├── Core/                        # All scoring logic — no I/O, no HTTP
│   ├── Domain/Models/           # Debate, Argument, Speech, Speaker, Team, CX
│   ├── Domain/Enums/            # Side, EvidenceQuality, ImpactMagnitude, FallacyType
│   ├── Config/                  # FormatConfig, ScoringConfig, RoundConfig loaders
│   ├── FlowGraph/               # FlowGraphBuilder — builds the rebuttal graph
│   │   ├── ArgumentNode.cs      # Node with resolved enrichment + computed strength
│   │   ├── RebuttalEdge.cs      # Directed edge: source rebuts target
│   │   ├── FlowGraph.cs         # Graph container with thread extraction
│   │   └── FlowGraphBuilder.cs  # 6-step build: nodes → edges → enrich → strength → drops → overrides
│   ├── Scoring/
│   │   ├── IScoringRule.cs      # One-method interface — implement to add a rule
│   │   ├── ScoringEngine.cs     # Orchestrates rules, aggregates, applies weights
│   │   ├── ScoringResult.cs     # Full output: winner, breakdown, explanation
│   │   ├── ScoringContext.cs    # Read-only context passed to every rule
│   │   ├── ArgumentScoreDetail.cs  # Per-argument detail with speaker attribution
│   │   ├── StockIssueSummary.cs    # Per-issue aggregation (raw + weighted)
│   │   ├── SpeakerScoreSummary.cs  # Per-speaker aggregation across all rules
│   │   └── Rules/               # 7 rule implementations (one file each)
│   │       ├── ArgumentStrengthRule.cs
│   │       ├── RebuttalEffectivenessRule.cs
│   │       ├── DroppedArgumentRule.cs
│   │       ├── LogicalConsistencyRule.cs
│   │       ├── TimeEfficiencyRule.cs
│   │       ├── CrossExaminationRule.cs
│   │       └── PrepTimeRule.cs
│   └── Output/
│       └── ExplanationGenerator.cs  # Structured → human-readable ballot
├── Cli/                         # Console runner — reads JSON, writes ballot
├── Api/                         # ASP.NET Core REST API
│   ├── Controllers/
│   │   ├── DebateController.cs  # POST /api/debate/score, /api/debate/flow
│   │   ├── ConfigController.cs  # GET/PUT format, scoring, round configs
│   │   └── EnrichController.cs  # POST /api/enrich — LLM enrichment
│   └── Services/
│       ├── ConfigService.cs     # Config file I/O (live reload)
│       ├── LlmEnrichmentService.cs  # Fill-gap enrichment via LLM
│       ├── ILlmProvider.cs      # Provider abstraction
│       ├── AnthropicProvider.cs # /v1/messages
│       └── OpenAiProvider.cs    # /v1/chat/completions
├── Frontend/                    # React + Vite + TypeScript
│   └── src/
│       ├── api/client.ts        # Typed API client
│       ├── types/               # Domain, scoring, config type mirrors
│       ├── pages/
│       │   ├── FlowSheetPage.tsx   # Main debate view
│       │   └── SettingsPage.tsx    # Config editors
│       ├── components/
│       │   ├── FlowGrid.tsx        # Rebuttal flow visualization
│       │   ├── ArgumentCard.tsx    # Argument cell in grid
│       │   ├── ArgumentDetail.tsx  # Expanded argument inspector
│       │   ├── ScorePanel.tsx      # Summary / Rule Breakdown / Speakers tabs
│       │   ├── ScoreSummary.tsx    # Stock issue breakdown table
│       │   ├── ScoreDetail.tsx     # Per-rule accordion with argument drill-down
│       │   ├── SpeakerScores.tsx   # Per-speaker score cards
│       │   ├── WinnerBanner.tsx    # Winner declaration banner
│       │   ├── ExplanationPanel.tsx # Full explanation viewer
│       │   ├── EnrichPanel.tsx     # LLM enrichment trigger + diff preview
│       │   ├── EnrichDiff.tsx      # Before/after enrichment comparison
│       │   ├── DebateInput.tsx     # JSON input/paste area
│       │   ├── FormatConfigEditor.tsx
│       │   ├── ScoringConfigEditor.tsx
│       │   └── RoundConfigEditor.tsx
│       └── hooks/
│           ├── useFlowSheet.ts     # Debate + scoring state management
│           └── useEnrichment.ts    # Enrichment diffing logic
├── Tests/                       # xUnit tests
│   ├── ScoringRuleTests.cs      # Unit tests for each of the 7 rules
│   ├── ScoringEngineIntegrationTests.cs  # End-to-end with verified outputs
│   ├── FlowGraphBuilderTests.cs # Graph construction + rebuttal edges
│   ├── DropDetectionTests.cs    # Drop obligation matching
│   └── Helpers/DebateFactory.cs # Test fixture builder
├── Config/                      # format-config.json, scoring-config.json, round-config.json
├── StockCaseLibrary/            # Reusable argument blueprints (4 files)
└── Samples/                     # Sample debates for testing and demos
```

---

## Running the CLI

```bash
dotnet run --project Cli -- --input <debate.json> [options]
```

| Flag | Description |
|---|---|
| `--input <path>` | Path to debate JSON file **(required)** |
| `--config <dir>` | Config directory — default: `./Config` |
| `--output <path>` | Write result to file instead of stdout |
| `--explain` | Full human-readable explanation **(default)** |
| `--brief` | Short summary: winner, decisive issue, notable drops |
| `--json` | Raw `ScoringResult` as JSON |
| `--validate` | Validate debate JSON against format config, then exit |

### Sample output (`--brief`)

```
Motion: This House believes the USFG should substantially increase federal regulation of AI

Winner: AFF (AFF 17.01 — NEG 7.17).
AFF wins on weighted score (17.01 vs 7.17). AFF won the following issues: Inherency, Harms, Solvency.
The round turned primarily on Harms (AFF 8.94 vs NEG 3.01).
Notable: 9 argument(s) dropped by AFF; 3 argument(s) dropped by NEG.
```

---

## Running the API + Frontend

```bash
# Terminal 1: API (port 5200)
dotnet run --project Api

# Terminal 2: Frontend (port 5173)
cd Frontend && npm run dev
```

### API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/debate/score` | Score a debate → winner + full breakdown |
| `POST` | `/api/debate/flow` | Build flow graph without scoring |
| `GET/PUT` | `/api/config/format` | Read or update format config |
| `GET/PUT` | `/api/config/scoring` | Read or update scoring weights |
| `GET/PUT` | `/api/config/round` | Read or update round config |
| `GET` | `/api/config/stockcases` | List all stock case blueprints |
| `POST/DELETE` | `/api/config/stockcases` | Add or remove a stock case |
| `POST` | `/api/enrich` | Enrich arguments via LLM |
| `POST` | `/api/enrich/score` | Enrich then score in one call |
| `GET` | `/api/enrich/providers` | List available LLM providers |

---

## Input Format

A debate is a single JSON object with five sections: `teams`, `speakers`, `speeches`, `arguments`, and `crossExaminations`.

```json
{
  "debateId": "round-001",
  "roundId": "round-001",
  "teams": {
    "AFF": { "teamId": "aff", "side": "AFF", "speakerIds": ["spk-1", "spk-2"] },
    "NEG": { "teamId": "neg", "side": "NEG", "speakerIds": ["spk-3", "spk-4"] }
  },
  "speakers": [
    { "speakerId": "spk-1", "name": "Alice Chen", "side": "AFF" }
  ],
  "speeches": [
    {
      "speechId": "1AC", "speakerId": "spk-1", "side": "AFF",
      "timeAllocatedSeconds": 480, "timeUsedSeconds": 471,
      "argumentIds": ["arg-1", "arg-2"]
    }
  ],
  "arguments": {
    "arg-1": {
      "argumentId": "arg-1", "speechId": "1AC", "speakerId": "spk-1",
      "side": "AFF", "stockIssueTag": "Harms", "rebuttalTargetIds": [],
      "core": { "claim": "...", "reasoning": "...", "impact": "...", "evidenceSource": "..." },
      "enrichment": {
        "evidenceQuality": "PeerReviewed", "impactMagnitude": "Catastrophic",
        "fallacies": [], "argumentStrength": null, "status": null
      }
    }
  },
  "crossExaminations": [ ... ],
  "prepTimeUsedSeconds": { "AFF": 310, "NEG": 405 }
}
```

All enrichment fields are optional. The engine applies defaults when absent (`Unverified` evidence, `Minor` impact, no fallacies). The engine works with a bare-minimum transcript — enrichment improves score fidelity but isn't required.

### Enrichment fields

| Field | Type | Values |
|---|---|---|
| `evidenceQuality` | enum or null | `PeerReviewed`, `ExpertOpinion`, `NewsSource`, `Anecdotal`, `Unverified` |
| `impactMagnitude` | enum or null | `Existential`, `Catastrophic`, `Significant`, `Minor`, `Negligible` |
| `fallacies` | array or null | `StrawMan`, `AdHominem`, `FalseDichotomy`, `SlipperySlope`, `AppealToAuthority`, `Repetition` |
| `argumentStrength` | float or null | Pre-computed 0–5 override (skips rule calculation if set) |
| `status` | string or null | `Extended`, `Conceded`, `Dropped` — overrides flow graph inference |

---

## Configuration

Three JSON files in `./Config/`. Changes apply immediately — no rebuild needed.

### `scoring-config.json` — all numeric weights

Every number in the scoring math comes from here. Key levers include `stockIssueWeights` (shift weight between Harms/Solvency/Inherency to change what wins close rounds), `droppedArgumentConcessionMultiplier` (raise to make drops more punishing), `fallacyPenalties` (add new fallacy types with zero code changes), and `evidenceQualityMultipliers` (lower `ExpertOpinion` to require peer-reviewed sources to compete).

### `format-config.json` — debate structure

Controls what a valid debate looks like: stock issues, hard gate issues, speech order, time allocations, and drop obligations (which speech must answer which).

### `round-config.json` — round-specific data

Motion text, format ID reference, and the stock case library with blueprint argument defaults.

---

## Running Tests

```bash
dotnet test Tests/DebateScoringEngine.Tests.csproj
```

Test coverage spans: unit tests for each of the 7 scoring rules in isolation, end-to-end scoring integration tests with verified outputs, flow graph construction and rebuttal edge detection, and drop obligation matching across speech positions. All tests are self-contained with in-memory config — no file I/O required.

---

## Design Decisions

### Structured input, not raw transcript parsing

The engine takes pre-structured JSON rather than parsing raw text. Natural language parsing is probabilistic; the requirement demands determinism. The structured schema captures everything a judge cares about — claims, evidence, rebuttal links, timing, CX admissions — without sacrificing exactness. The LLM enrichment layer bridges the gap: a raw transcript can be processed by an LLM to produce structured JSON, at which point the scoring engine takes over deterministically.

### Flow graph as the central data structure

Arguments are not scored in isolation — their meaning depends on whether they were rebutted, extended, or dropped. `FlowGraphBuilder` turns the flat argument list into a directed graph of rebuttal chains. Every scoring rule operates on this graph, not the raw debate. This is what makes the engine evaluate debate flow rather than just averaging argument quality scores.

### Rule isolation via `IScoringRule`

Each of the seven scoring rules is a self-contained class that sees a read-only `ScoringContext` and emits a `RuleResult`. Rules cannot mutate state or communicate with each other. Adding a new rule is one file with no changes elsewhere — register it in `ScoringEngine.BuildRules()` and it participates in scoring immediately.

### Two scoring paths: hard gate and weighted score

In real policy debate, Topicality is a "prior question" — if AFF can't defend the plan as topical, substance is irrelevant. The hard gate mechanic models this: if AFF's raw score on a designated issue is zero, the round ends there. This is configurable — any issue can be a hard gate, or none. The substantive score always runs for informational output.

### Config-first, code-last

Every numeric value that influences scoring lives in `scoring-config.json`. Every structural rule lives in `format-config.json`. The goal is that a debate coach, not a programmer, should be able to tune the system by editing JSON.

### Speaker-level scoring as post-aggregation

Every `ArgumentScoreDetail` carries a `SpeakerId`, which lets the engine aggregate scores per speaker across all rules after the fact. This is a post-processing step rather than a separate scoring dimension — speakers don't have their own rules, they inherit scores from the arguments they made and the CX exchanges they participated in. Prep time (which is per-side, not per-speaker) is excluded from individual speaker totals to avoid misleading attribution.

### Three-tier enrichment fallback

Enrichment values resolve through: explicit argument enrichment → stock case blueprint defaults → global config defaults. This means the engine always has values to work with even if the input has zero enrichment metadata, while still respecting explicit overrides when present.

---

## Trade-offs and Limitations

### Fallacy detection is input-tagged, not automatic

`LogicalConsistencyRule` applies penalties for tagged fallacies, but the engine doesn't detect fallacies autonomously. Detection requires semantic understanding — an NLP problem that's probabilistic. The intended workflow: LLM enrichment detects fallacies and tags them; the engine scores them deterministically.

### Rebuttal linking is explicit, not inferred

Arguments must declare what they rebut via `rebuttalTargetIds`. The engine doesn't infer rebuttal relationships from argument content. This keeps the engine deterministic and the graph auditable, but the quality of the flow graph depends on how carefully the input is structured.

### Single debate format shipped

The current config ships with eight-speech policy debate. The architecture supports alternative formats (British Parliamentary, Lincoln-Douglas) through `format-config.json`, but no alternative configs are provided. A BP format would require different stock issues, speech definitions, and no hard gate equivalent.

### No persistence

Results are returned in-memory and not stored. Each API request is self-contained. Adding persistence (round history, cross-round comparisons) would be a natural next step.

### Double-counting of fallacy penalties

Fallacies are subtracted both from `ComputedStrength` (via the strength formula) and reported by `LogicalConsistencyRule` as a standalone penalty. This is intentional — a fallacious argument is both weaker and reflects poor reasoning, which are distinct dimensions. But it means the effective penalty is larger than the config value alone suggests. The weights should account for this.

### Speaker scoring doesn't capture delivery

Speaker scores aggregate from argument strength, drops, rebuttals, CX performance, and time efficiency — all derived from structured metadata. They cannot capture delivery quality, persuasiveness, or strategic judgment that would be visible in an actual oral performance. This is a structural limitation of scoring from structured input rather than watching the round.

---

## Self-Critique

**What works well:** The flow graph abstraction is the right foundational choice — it mirrors how human judges actually evaluate debates by tracking argument chains across speeches. The config-driven approach genuinely delivers: you can meaningfully change how rounds are judged without touching code. The `IScoringRule` interface makes extensibility real rather than theoretical — adding a rule is one class with no wiring changes.

**What I'd improve with more time:** The enrichment fallback hierarchy could be more sophisticated — right now it's a simple three-tier cascade, but real debates often have partial enrichment where some fields should fall back and others shouldn't. The rebuttal effectiveness formula is reasonable but simplistic — it doesn't account for how central an argument is to the overall case structure (a rebuttal of a key solvency mechanism should matter more than a rebuttal of a minor impact claim, regardless of raw strength scores). The explanation generator produces functional but plain output — a more polished version would use templates and natural language generation to produce genuinely readable ballot prose rather than formatted data dumps. Finally, the frontend's flow sheet is functional but doesn't yet support drag-and-drop editing of rebuttal links or inline argument strength adjustment, which would make the enrichment workflow much more intuitive.
