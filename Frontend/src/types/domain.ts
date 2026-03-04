// ============================================================================
// domain.ts — Core debate domain types
// Mirrors DebateScoringEngine.Core.Domain.Models exactly.
// ============================================================================

// ── Enums ────────────────────────────────────────────────────────────────────

export type Side = 'AFF' | 'NEG' | 'CX'

export type SpeechType = 'Constructive' | 'Rebuttal' | 'CrossEx'

export type EvidenceQuality =
  | 'PeerReviewed'
  | 'ExpertOpinion'
  | 'NewsSource'
  | 'Anecdotal'
  | 'Unverified'

export type ImpactMagnitude =
  | 'Existential'
  | 'Catastrophic'
  | 'Significant'
  | 'Minor'
  | 'Negligible'

export type FallacyType =
  | 'StrawMan'
  | 'AdHominem'
  | 'FalseDichotomy'
  | 'AppealToAuthority'
  | 'SlipperySlope'
  | 'CircularReasoning'
  | 'HastyGeneralization'

export type ArgumentStatus = 'Active' | 'Dropped' | 'Conceded' | 'Extended'

// ── Argument ─────────────────────────────────────────────────────────────────

/** Core fields — always required, never null */
export interface ArgumentCore {
  claim: string
  reasoning: string
  impact: string
  evidenceSource?: string | null
}

/** Enrichment fields — optional, filled by human or LLM */
export interface ArgumentEnrichment {
  evidenceQuality: EvidenceQuality | null
  impactMagnitude: ImpactMagnitude | null
  fallacies: FallacyType[]
  argumentStrength: number | null  // 0–5 float, if null engine computes
  status: ArgumentStatus | null    // null = engine derives from flow
}

/** An individual argument node in the flow */
export interface Argument {
  argumentId: string
  speechId: string
  speakerId: string
  side: Side
  stockIssueTag: string
  stockCaseId: string | null
  rebuttalTargetIds: string[]
  core: ArgumentCore
  enrichment: ArgumentEnrichment
}

// ── CX Event ─────────────────────────────────────────────────────────────────

export interface CxEvent {
  cxEventId: string
  speechId: string           // e.g. "CX-1"
  questionerId: string
  respondentId: string
  questionsAsked: number
  admissionsObtained: number
  evasions: number
  timeUsedSeconds: number
  timeAllocatedSeconds: number
}

// ── Speech ───────────────────────────────────────────────────────────────────

export interface Speech {
  speechId: string
  speakerId: string
  side: Side
  timeAllocatedSeconds: number
  timeUsedSeconds: number
  argumentIds: string[]
  prepTimeUsedSeconds?: number
  cxEvent?: CxEvent | null
}

// ── Team / Speaker ───────────────────────────────────────────────────────────

export interface Speaker {
  speakerId: string
  name: string
  side: Side
}

export interface Team {
  teamId: string
  side: Side
  speakerIds: string[]
}

// ── Debate (top-level input to the engine) ───────────────────────────────────

export interface Debate {
  debateId: string
  roundId: string
  teams: Record<'AFF' | 'NEG', Team>
  speakers: Speaker[]
  speeches: Speech[]
  /** Map of argumentId → Argument */
  arguments: Record<string, Argument>
}

// ── Flow Graph (output of FlowGraphBuilder) ───────────────────────────────────

export interface ResolvedEnrichment {
  evidenceQuality: EvidenceQuality
  impactMagnitude: ImpactMagnitude
  fallacies: FallacyType[]
  evidenceSource: string
  impactSource: string
}

export type NodeStatus = 'Active' | 'Dropped' | 'Conceded' | 'Extended'

export interface FlowNode {
  argumentId: string
  speechId: string
  side: Side
  stockIssueTag: string
  status: NodeStatus
  computedStrength: number
  statusIsOverridden: boolean
  resolvedEnrichment: ResolvedEnrichment
}

export interface FlowEdge {
  sourceArgumentId: string
  sourceSpeechId: string
  targetArgumentId: string
  targetSpeechId: string
}

export interface FlowThread {
  rootArgumentId: string
  stockIssueTag: string
  nodeIds: string[]
}

export interface FlowSummary {
  totalArguments: number
  droppedArguments: number
  affArguments: number
  negArguments: number
  cxEvents: number
  stockIssuesCovered: string[]
}

export interface FlowGraphResponse {
  debateId: string
  summary: FlowSummary
  nodes: FlowNode[]
  edges: FlowEdge[]
  threads: FlowThread[]
}
