// ============================================================================
// config.ts — Config types
// Mirrors DebateScoringEngine.Core.Config namespace.
// ============================================================================

import type { Side, SpeechType, EvidenceQuality, ImpactMagnitude, FallacyType } from './domain'

// ── format-config.json ────────────────────────────────────────────────────────

export interface StockIssue {
  id: string
  label: string
  obligatedSide: Side | 'BOTH' | 'NEITHER'
}

export interface SpeechDefinition {
  speechId: string
  side: Side | 'CX'
  type: SpeechType
  timeSeconds: number
}

export interface DropRule {
  argumentIntroducedIn: string
  mustBeAnsweredBy: string
}

export interface FormatConfig {
  formatId: string
  formatName: string
  stockIssues: StockIssue[]
  hardGateIssues: string[]
  speechOrder: SpeechDefinition[]
  dropRules: DropRule[]
  prepTimeSeconds: Record<string, number>
  coreArgumentFields: string[]
}

// ── scoring-config.json ───────────────────────────────────────────────────────

export interface ScoringConfig {
  stockIssueWeights: Record<string, number>
  tiebreakerPriority: string[]
  evidenceQualityMultipliers: Record<EvidenceQuality, number>
  impactMagnitudeScores: Record<ImpactMagnitude, number>
  fallacyPenalties: Record<FallacyType, number>
  droppedArgumentPenalty: number
  ruleWeights: RuleWeights
  crossExamination: CrossExaminationConfig
  prepTime: PrepTimeConfig
}

export interface RuleWeights {
  argumentStrength: number
  rebuttalEffectiveness: number
  droppedArgument: number
  logicalConsistency: number
  timeEfficiency: number
  crossExamination: number
  prepTime: number
}

export interface CrossExaminationConfig {
  perAdmissionScore: number
  perEvasionPenalty: number
  timeEfficiencyWeight: number
}

export interface PrepTimeConfig {
  penaltyPerSecondOver: number
  maxPenalty: number
}

// ── round-config.json + stock cases ───────────────────────────────────────────

export interface DefaultEnrichment {
  evidenceQuality?: EvidenceQuality | null
  impactMagnitude?: ImpactMagnitude | null
  fallacies?: FallacyType[]
}

export interface BlueprintArgument {
  claim?: string | null
  reasoning?: string | null
  impact?: string | null
  evidenceSource?: string | null
}

export interface StockCase {
  stockCaseId: string
  label: string
  stockIssueTag: string
  side: Side
  source: 'system' | 'user'
  defaultEnrichment?: DefaultEnrichment | null
  blueprintArgument?: BlueprintArgument | null
}

export interface RoundConfig {
  roundId: string
  motion: string
  formatId: string
  stockCaseLibrary: StockCase[]
  userStockCases: StockCase[]
}

// ── GET /api/config/stockcases response ───────────────────────────────────────

export interface StockCasesResponse {
  total: number
  system: number
  user: number
  stockCases: StockCase[]
}

// ── GET /api/enrich/providers response ───────────────────────────────────────

export interface ProvidersResponse {
  configured: string
  available: string[]
}
