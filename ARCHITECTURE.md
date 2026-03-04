# Architecture

A technical walkthrough of how the Debate Scoring Engine works. Covers the Core library, CLI runner, ASP.NET Core API, and React frontend. Explains the data structures, pipeline sequence, and rationale behind key design decisions.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Project Layers](#project-layers)
3. [The Domain Model](#the-domain-model)
4. [The Config System](#the-config-system)
5. [The Flow Graph](#the-flow-graph)
6. [The Scoring Pipeline](#the-scoring-pipeline)
7. [The Seven Scoring Rules](#the-seven-scoring-rules)
8. [Post-Processing: Speaker and Cross-Rule Aggregation](#post-processing-speaker-and-cross-rule-aggregation)
9. [Output: Explanation Generation](#output-explanation-generation)
10. [The API Layer](#the-api-layer)
11. [LLM Enrichment](#llm-enrichment)
12. [The Frontend](#the-frontend)
13. [Data Flow End-to-End](#data-flow-end-to-end)

---

## System Overview

The engine is a deterministic scoring pipeline with three stages:

```
Input JSON
    │
    ▼
[1] FlowGraphBuilder
    Parses the debate, resolves enrichment via three-tier fallback,
    computes argument strengths, and detects dropped arguments.
    → produces: FlowGraph
    │
    ▼
[2] ScoringEngine
    Runs 7 independent scoring rules against the FlowGraph.
    Aggregates into per-issue and per-speaker summaries,
    checks hard gates, determines winner.
    → produces: ScoringResult
    │
    ▼
[3] ExplanationGenerator
    Formats ScoringResult into human-readable ballot text.
    → produces: plain-text explanation
```

The three stages are strictly sequential and share no mutable state. The same input always produces the same output.

---

## Project Layers

```
Core/           Pure C# class library — no I/O, no HTTP, no dependencies.
                All scoring logic lives here.

Cli/            Console runner. Reads JSON from disk, loads configs,
                runs the pipeline, prints to stdout.

Api/            ASP.NET Core 8 Web API. Wraps Core behind HTTP endpoints.
                Adds config editing, LLM enrichment, and the Settings UI backend.

Frontend/       React + Vite + TypeScript. Flow sheet visualizer,
                score panel with three views (Summary / Rule Breakdown / Speakers),
                enrichment tools, and live config editors.

Tests/          xUnit test project. Tests Core directly — no HTTP, no disk I/O.
```

**Key constraint:** Core has no knowledge of how it's being called. The same `ScoringEngine` instance is used by both the CLI and the API. This makes testing straightforward and ensures both produce identical results for identical inputs.

---

## The Domain Model

The domain lives in `Core/Domain/`. Everything in the pipeline flows from these types.

### Debate

The root input object. Contains five sections:

```
Debate
├── Teams          Dictionary<"AFF"|"NEG", Team>
├── Speakers       List<Speaker>           (speakerId, name, side)
├── Speeches       List<Speech>            (speechId, speakerId, side, time, argumentIds)
├── Arguments      Dictionary<string, Argument>   (keyed by argumentId)
└── CrossExaminations  List<CrossExamination>
```

`Arguments` is a dictionary rather than a list because arguments are referenced by ID from multiple places — speeches, rebuttal target lists, CX questions. Keying by ID avoids O(n) scans at every lookup.

### Argument

The atomic unit of scoring. Two sections:

```
Argument
├── Identity       argumentId, speechId, speakerId, side, stockIssueTag
├── Core           claim, reasoning, impact, evidenceSource
│                  (the substantive content)
└── Enrichment     evidenceQuality?, impactMagnitude?, fallacies?, argumentStrength?, status?
                   (metadata — all nullable; engine applies defaults when absent)
```

`rebuttalTargetIds` is on the Argument itself (not the Speech) because rebuttals are argument-to-argument relationships, not speech-level ones. A single speech can contain both new arguments and rebuttals to different targets.

### Speaker

Speakers are referenced by ID from three places: `Argument.SpeakerId`, `Speech.SpeakerId`, and `CrossExamination.ExaminerId`/`RespondentId`. This tracing is what enables per-speaker score aggregation — every score contribution in the system can be attributed back to the person who made it.

### Enums

Four enums drive the scoring math:

- **`EvidenceQuality`**: `PeerReviewed`, `ExpertOpinion`, `NewsSource`, `Anecdotal`, `Unverified` — maps to a multiplier in scoring config (1.0, 0.85, 0.7, 0.4, 0.25)
- **`ImpactMagnitude`**: `Existential`, `Catastrophic`, `Significant`, `Minor`, `Negligible` — maps to a base score (5.0, 4.0, 3.0, 2.0, 1.0)
- **`FallacyType`**: six types, each with a configurable subtraction penalty
- **`ArgumentStatus`**: `Active`, `Dropped`, `Conceded`, `Extended` — controls which rules apply to an argument

---

## The Config System

Three config files, loaded at startup, drive all behavior:

### `format-config.json` — debate structure

Controls what a valid debate looks like: `stockIssues` (id, label, obligatedSide), `hardGateIssues` (prior questions), `speechOrder` (for drop detection timing), `speeches` (per-speech metadata), and `dropObligations` (which speech must answer which).

### `scoring-config.json` — all numeric weights

Every number in the scoring math: `stockIssueWeights`, `evidenceQualityMultipliers`, `impactMagnitudeScores`, `fallacyPenalties`, `droppedArgumentConcessionMultiplier`, `tiebreakerPriority`, `rebuttalEffectivenessWeight`, and defaults for evidence quality and impact magnitude.

### `round-config.json` — round-specific data

Motion text, format ID, and the stock case library with blueprint argument defaults.

`ConfigService` reads these files fresh on every request so edits take effect immediately without restart.

---

## The Flow Graph

`FlowGraphBuilder.Build(debate)` produces a `FlowGraph`. This is the most important data structure — scoring rules operate on the graph, not the raw debate.

### What it contains

```
FlowGraph
├── Nodes    Dictionary<argumentId, ArgumentNode>
└── Edges    List<RebuttalEdge>
```

**`ArgumentNode`** wraps a raw `Argument` and adds derived state:

```
ArgumentNode
├── ArgumentId, SpeechId, SpeakerId, Side, StockIssueTag
├── Status             ArgumentStatus — engine-derived or explicit override
├── StatusIsOverridden bool — true if status came from enrichment
├── Resolved           ResolvedEnrichment — non-nullable resolved values
└── ComputedStrength   double (0–5) — final strength after resolution
```

### Build sequence (6 steps)

**Step 1 — Build nodes:** One `ArgumentNode` per argument.

**Step 2 — Build edges:** `RebuttalEdge` per `rebuttalTargetId`. Dangling references silently skipped.

**Step 3 — Resolve enrichment (three-tier fallback):** For each field: explicit argument enrichment → stock case blueprint defaults → global config defaults. Resolved values land in `node.Resolved` — a fully non-nullable struct that all rules read from.

**Step 4 — Compute strength:**

```
if enrichment.ArgumentStrength != null:
    ComputedStrength = clamp(enrichment.ArgumentStrength, 0, 5)
else:
    raw = ImpactScore × EvidenceMultiplier − sum(FallacyPenalties)
    ComputedStrength = clamp(raw, 0, 5)
```

**Step 5 — Drop detection:** For each node, find the `DropObligation` for its speech, check if any `RebuttalEdge` targets it from a speech at or before the cutoff position. If no timely response and no explicit status → mark `Dropped`.

**Step 6 — Apply status overrides:** If `argument.Enrichment.Status` is non-null, it overwrites drop detection. Runs last so human/LLM judgments override engine inference.

---

## The Scoring Pipeline

`ScoringEngine.Score(flow, debate, format, scoring, round)` runs these steps:

### Step 1 — Run all rules

Each rule sees a read-only `ScoringContext` and produces a `RuleResult` (AFF score, NEG score, per-argument details, explanation). Rules are completely independent.

### Step 2 — Post-processing

Two aggregation passes run after all rules complete:

**Cross-rule breakdown:** For each argument that appears in any rule, builds a `RuleBreakdown` list showing every rule's contribution, plus aggregated `DroppedPenalty` and `FallacyPenalty` values. This powers the expanded argument detail view in the UI.

**Speaker summaries:** Groups all `ArgumentScoreDetail` entries by `SpeakerId` and aggregates: total score, argument count, dropped count, rebuttal count, average strength, and per-rule contributions. See [Post-Processing](#post-processing-speaker-and-cross-rule-aggregation) for details.

### Step 3 — Build issue summaries

Each `StockIssueSummary` collects `ArgumentScoreDetail` entries tagged to its issue across all rules, sums AFF and NEG scores separately, and applies the issue's weight from config.

### Step 4 — Hard gate check

For each issue in `format.HardGateIssues`: if the obligated side's raw score ≤ 0, the other side wins immediately. Uses raw score (not weighted) because a zero-weight issue would always score 0 weighted.

### Step 5 — Determine winner

Weighted total comparison → tiebreaker list → AFF default if all tiebreakers also tie.

### Step 6 — Assemble result

```
ScoringResult
├── Winner, WinnerExplanation
├── DecidedByHardGate, HardGateIssue
├── AffTotalScore, NegTotalScore
├── RuleResults              (one per rule, with per-argument details)
├── StockIssueSummaries      (one per issue, with raw + weighted scores)
├── ArgumentDetails          (flattened across all rules, with speaker attribution)
├── SpeakerScoreSummaries    (one per speaker, with per-rule breakdown)
└── Explanation
```

---

## The Seven Scoring Rules

All rules implement `IScoringRule` — `string RuleId`, `string DisplayName`, `RuleResult Evaluate(ScoringContext)`. Rules are additive: their scores are summed by issue, then weighted.

Every rule populates `SpeakerId` on its `ArgumentScoreDetail` entries, enabling per-speaker aggregation downstream.

### 1. ArgumentStrengthRule

Reads `node.ComputedStrength` for every active (not dropped/conceded) argument and attributes it to the argument's side. The strength was already computed by the flow graph builder from `(ImpactScore × EvidenceMultiplier) − FallacyPenalties`. Dropped arguments are excluded here because `DroppedArgumentRule` handles them with a different multiplier.

### 2. DroppedArgumentRule

For every dropped or conceded argument: `bonus = node.ComputedStrength × droppedArgumentConcessionMultiplier`. The bonus goes to the side that introduced the argument (silence concedes it). Default multiplier is 1.5 — an unanswered argument is worth more than a contested one.

### 3. LogicalConsistencyRule

Applies fallacy penalties. Since fallacies were already subtracted from `ComputedStrength` during graph construction, this rule's primary purpose is transparency — it explicitly surfaces which arguments had fallacies and what the penalty was. See the "double-counting" note in [Trade-offs](#trade-offs-and-limitations) in the README.

### 4. RebuttalEffectivenessRule

For each rebuttal edge:
```
effectiveness = clamp(source.Strength / max(target.Strength, 0.01), 0, 1)
score = effectiveness × target.Strength × rebuttalEffectivenessWeight
```
Score goes to the rebuttal author's side. Beating a strong argument is worth more than beating a weak one.

### 5. TimeEfficiencyRule

Penalties for over-time (per-second penalty) and severe under-time (flat penalty below threshold). Applied per speech, attributed to the speech's speaker.

### 6. CrossExaminationRule

Three dimensions: admission extraction (bonus to examiner), evasive answers (penalty to respondent), and CX time underuse (penalty to examiner). Each detail is attributed to the specific examiner or respondent speaker.

### 7. PrepTimeRule

Unused prep time earns a tiny per-second bonus; over-prep earns a flat penalty. This is per-side (not per-speaker) — `SpeakerId` is null on these entries, so they're excluded from individual speaker totals.

---

## Post-Processing: Speaker and Cross-Rule Aggregation

After all seven rules produce their results, `ScoringEngine` runs two post-processing passes:

### Cross-rule breakdown

For every unique argument that appears in any rule's output, a `RuleBreakdown` list is built showing all rules' scores for that argument. This is injected into every `ArgumentScoreDetail` so the frontend can show a per-argument drill-down without making separate API calls. Aggregated `DroppedPenalty` and `FallacyPenalty` values are also propagated.

### Speaker score summaries

`BuildSpeakerSummaries()` groups all `ArgumentScoreDetail` entries by `SpeakerId` (excluding null-speaker entries like prep time). For each speaker it computes:

- **TotalScore**: sum of all score contributions across all rules
- **ArgumentCount**: unique argument IDs (excluding synthetic entries like CX and speech-time)
- **DroppedCount**: unique dropped arguments introduced by this speaker
- **RebuttalCount**: unique rebuttal contributions
- **AverageStrength**: mean `ComputedStrength` from the argument-strength rule entries
- **RuleContributions**: per-rule score and detail count

The result is a `List<SpeakerScoreSummary>` on the `ScoringResult`, sorted AFF-first then by total score descending.

---

## Output: Explanation Generation

`ExplanationGenerator.GenerateFull()` produces the full ballot text. It's called at the CLI/API layer, not inside `ScoringEngine`, so the engine remains pure — it produces numbers, not text.

Four sections: header (motion, winner, scores), stock issue table (weight / AFF / NEG / winner per issue), rule-by-rule breakdown (each rule's explanation + top argument callouts), and flow graph summary (total arguments, rebuttals, drops).

---

## The API Layer

`Api/` is a thin wrapper. Its job: receive HTTP, resolve configs, call Core, return JSON. No scoring logic lives in the API.

### ConfigService

Reads config files fresh on every request (live reload). Writes back atomically. Thread safety is last-write-wins — acceptable for a single-user local tool.

### Request/Response shapes

**`POST /api/debate/score`** → `{ result: ScoringResult, fullExplanation: string, flowSummary: {...} }`

The `ScoringResult` now includes `speakerScoreSummaries` alongside `ruleResults`, `stockIssueSummaries`, and `argumentDetails`.

**`POST /api/debate/flow`** → UI-friendly flow graph serialization with nodes, edges, and threads.

**`GET|PUT /api/config/*`** → Read/write for all three config files with validation.

---

## LLM Enrichment

`LlmEnrichmentService` fills null enrichment fields via LLM — the bridge between raw transcripts and structured input.

### Architecture

```
EnrichController → LlmEnrichmentService → ILlmProvider
                                          ├── AnthropicProvider (/v1/messages)
                                          └── OpenAiProvider (/v1/chat/completions)
```

One API call per argument. System prompt instructs JSON-only response. Fill-gap-only policy: explicit enrichment is never overwritten. Soft-fail semantics: a bad LLM response logs a warning and returns the original argument unchanged.

---

## The Frontend

React + Vite + TypeScript. Communicates with the API via a typed client (`api/client.ts`).

### Pages

**FlowSheetPage** — The main view. Three sections: debate input (paste JSON), flow grid (visual rebuttal chains), and score panel (appears after scoring). The flow grid renders arguments as cards positioned by speech column and stock issue row, with colored edges showing rebuttal links.

**SettingsPage** — Live editors for all three config files. Changes are saved via the API and take effect on the next scoring request.

### Score Panel

Three tabs:

- **Summary** — Stock issue breakdown table showing weight, AFF score, NEG score, visual bar, and winner per issue. Total row at bottom.
- **Rule Breakdown** — Accordion per rule. Each rule shows AFF vs NEG score. Expanding reveals a per-argument table with speech, side, computed strength, and net score. Expanding an argument shows its cross-rule breakdown (all rules' contributions to that argument) plus penalty details.
- **Speakers** — Two-column layout (AFF / NEG). Each speaker is a collapsible card showing argument count, rebuttal count, dropped count, average strength bar (0–5), and total score. Expanding shows per-rule contribution breakdown.

### Enrichment

The `EnrichPanel` triggers LLM enrichment and shows a diff preview (`EnrichDiff`) of what changed before the user commits. The diff shows per-argument field changes (before/after) and a summary of total fields filled.

---

## Data Flow End-to-End

```
POST /api/debate/score
    { debate: {...}, includeFullExplanation: true }
          │
          ▼
    DebateController.Score()
          │
          ├─ ConfigService.GetFormat()    → format-config.json
          ├─ ConfigService.GetScoring()   → scoring-config.json
          └─ ConfigService.GetRound()     → round-config.json
          │
          ▼
    FlowGraphBuilder.Build(debate)
          ├─ BuildNodes()          → 1 ArgumentNode per argument
          ├─ BuildEdges()          → RebuttalEdge per rebuttalTargetId
          ├─ ResolveEnrichment()   → 3-tier: explicit → blueprint → default
          ├─ ComputeStrengths()    → (impact × evidence − fallacies), clamped 0–5
          ├─ DetectDrops()         → check drop obligations vs edge timing
          └─ ApplyStatusOverrides()→ explicit overrides engine inference
          │
          ▼
    ScoringEngine.Score(flow, debate, format, scoring, round)
          ├─ RunAllRules()
          │     ├─ ArgumentStrengthRule      (7 rules, each independent,
          │     ├─ DroppedArgumentRule         each producing AFF/NEG scores
          │     ├─ LogicalConsistencyRule      with per-argument details
          │     ├─ RebuttalEffectivenessRule   and speaker attribution)
          │     ├─ TimeEfficiencyRule
          │     ├─ CrossExaminationRule
          │     └─ PrepTimeRule
          │
          ├─ PopulateCrossRuleBreakdown()  → inject per-argument rule breakdown
          ├─ BuildSpeakerSummaries()       → aggregate by speaker across all rules
          ├─ BuildIssueSummaries()         → aggregate by stock issue, apply weights
          ├─ CheckHardGates()              → if obligated side score ≤ 0 → other side wins
          └─ DetermineWinner()             → weighted total → tiebreaker → AFF default
          │
          ▼
    ScoringResult
    { winner, ruleResults, stockIssueSummaries,
      argumentDetails, speakerScoreSummaries, explanation }
          │
          ▼
    ExplanationGenerator.GenerateFull()
          → header + issue table + rule breakdown + flow summary
          │
          ▼
    HTTP 200
    { result: ScoringResult, fullExplanation: "...", flowSummary: {...} }
```

Every box in this diagram is stateless. The same input submitted twice produces the same response twice.
