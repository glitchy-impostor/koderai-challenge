// ============================================================================
// scoring.ts — Scoring result types
// Mirrors DebateScoringEngine.Core.Scoring + Output namespaces exactly.
// Updated in Phase C to match actual C# plan shape.
// ============================================================================

import type { Side } from './domain'

// ── ArgumentScoreDetail — per-argument scoring detail ─────────────────────────

export interface ArgumentScoreDetail {
  argumentId: string
  speechId: string
  side: Side
  stockIssueTag: string
  speakerId: string | null
  netScore: number
  computedStrength: number
  isDropped: boolean
  droppedPenalty: number
  fallacyPenalty: number
  ruleBreakdown: RuleScore[]
}

export interface RuleScore {
  ruleName: string
  score: number
  notes: string
}

// ── SpeakerRuleContribution — one rule's score for a speaker ──────────────────

export interface SpeakerRuleContribution {
  ruleId: string
  displayName: string
  score: number
  detailCount: number
}

// ── SpeakerScoreSummary — aggregated per speaker ──────────────────────────────

export interface SpeakerScoreSummary {
  speakerId: string
  speakerName: string
  side: Side
  totalScore: number
  argumentCount: number
  droppedCount: number
  rebuttalCount: number
  averageStrength: number
  ruleContributions: SpeakerRuleContribution[]
}

// ── RuleResult — output of one IScoringRule ────────────────────────────────────

export interface RuleResult {
  ruleId: string
  displayName: string
  affScore: number
  negScore: number
  argumentDetails: ArgumentScoreDetail[]
  explanation: string
}

// ── StockIssueSummary — aggregated per stock issue ────────────────────────────

export interface StockIssueSummary {
  issueId: string
  issueLabel: string
  weight: number
  affRawScore: number
  negRawScore: number
  affWeightedScore: number
  negWeightedScore: number
  winner: Side | null     // null = tied
  isHardGate: boolean
}

// ── ScoringResult — top-level result from POST /api/debate/score ──────────────

export interface ScoringResult {
  winner: Side
  winnerExplanation: string
  decidedByHardGate: boolean
  hardGateIssue: string | null
  affTotalScore: number
  negTotalScore: number
  ruleResults: RuleResult[]
  stockIssueSummaries: StockIssueSummary[]
  argumentDetails: ArgumentScoreDetail[]
  speakerScoreSummaries: SpeakerScoreSummary[]
  explanation: string
}

// ── API response wrappers ─────────────────────────────────────────────────────

export interface ScoreResponse {
  result: ScoringResult
  fullExplanation: string
  flowSummary: import('./domain').FlowSummary
}

export interface EnrichScoreResponse {
  enrichedDebate: import('./domain').Debate
  fieldsFilled: number
  skippedArgumentIds: string[]
  scoringResult: ScoringResult
  fullExplanation: string
  flowSummary: import('./domain').FlowSummary
}

export interface EnrichResponse {
  enrichedDebate: import('./domain').Debate
  fieldsFilled: number
  skippedArgumentIds: string[]
  warning: string | null
}
