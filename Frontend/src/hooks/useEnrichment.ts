// ============================================================================
// useEnrichment.ts — Enrichment panel state + API orchestration
// ============================================================================

import { useState, useCallback } from 'react'
import { enrichApi, ApiError } from '@/api/client'
import type { Debate, Argument } from '@/types/domain'
import type { EnrichResponse, EnrichScoreResponse, ScoringResult } from '@/types/scoring'

export type EnrichStep = 'configure' | 'enriching' | 'diff' | 'done'

// Per-argument diff: which enrichment fields were actually filled in
export interface ArgDiff {
  argumentId: string
  speechId: string
  side: 'AFF' | 'NEG' | 'CX'
  fields: FieldDiff[]
}

export interface FieldDiff {
  field: string
  before: unknown
  after: unknown
}

function diffArguments(original: Debate, enriched: Debate): ArgDiff[] {
  const diffs: ArgDiff[] = []
  const ENRICHMENT_FIELDS = ['evidenceQuality', 'impactMagnitude', 'fallacies', 'argumentStrength', 'status'] as const

  for (const id of Object.keys(enriched.arguments)) {
    const orig = original.arguments[id]
    const enr  = enriched.arguments[id]
    if (!orig || !enr) continue

    const fields: FieldDiff[] = []
    for (const field of ENRICHMENT_FIELDS) {
      const before = orig.enrichment[field as keyof typeof orig.enrichment]
      const after  = enr.enrichment[field as keyof typeof enr.enrichment]
      // Only report if it was null/empty before and now has a value
      const wasEmpty = before === null || before === undefined ||
                       (Array.isArray(before) && before.length === 0)
      const nowFilled = after !== null && after !== undefined &&
                        !(Array.isArray(after) && after.length === 0)
      if (wasEmpty && nowFilled) {
        fields.push({ field, before, after })
      }
    }
    if (fields.length > 0) {
      diffs.push({ argumentId: id, speechId: orig.speechId, side: orig.side, fields })
    }
  }
  return diffs
}

// Count arguments that have at least one null enrichment field
export function countEnrichableArgs(debate: Debate): number {
  return Object.values(debate.arguments).filter((a: Argument) =>
    a.enrichment.evidenceQuality === null ||
    a.enrichment.impactMagnitude === null ||
    (a.enrichment.fallacies?.length ?? 0) === 0
  ).length
}

export interface EnrichmentState {
  open: boolean
  step: EnrichStep
  provider: string
  apiKey: string
  diffs: ArgDiff[]
  fieldsFilled: number
  skippedIds: string[]
  error: string | null
  // enrichedDebate and optional scoring result after a successful enrich
  enrichedDebate: Debate | null
  scoreResult: ScoringResult | null
  fullExplanation: string | null
}

export interface EnrichmentActions {
  openPanel: () => void
  closePanel: () => void
  setProvider: (p: string) => void
  setApiKey: (k: string) => void
  runEnrich: (debate: Debate) => Promise<void>
  runEnrichAndScore: (debate: Debate) => Promise<void>
  clearError: () => void
  resetToConfig: () => void
}

const INITIAL: EnrichmentState = {
  open: false, step: 'configure',
  provider: 'Anthropic', apiKey: '',
  diffs: [], fieldsFilled: 0, skippedIds: [],
  error: null, enrichedDebate: null,
  scoreResult: null, fullExplanation: null,
}

export function useEnrichment(): EnrichmentState & EnrichmentActions {
  const [state, setState] = useState<EnrichmentState>(INITIAL)

  const openPanel = useCallback(() =>
    setState(s => ({ ...s, open: true, step: 'configure' })), [])

  const closePanel = useCallback(() =>
    setState(s => ({ ...s, open: false })), [])

  const setProvider = useCallback((provider: string) =>
    setState(s => ({ ...s, provider })), [])

  const setApiKey = useCallback((apiKey: string) =>
    setState(s => ({ ...s, apiKey })), [])

  const clearError = useCallback(() =>
    setState(s => ({ ...s, error: null })), [])

  const resetToConfig = useCallback(() =>
    setState(s => ({ ...s, step: 'configure', diffs: [], error: null,
                     enrichedDebate: null, scoreResult: null, fullExplanation: null })), [])

  const runEnrich = useCallback(async (debate: Debate) => {
    setState(s => ({ ...s, step: 'enriching', error: null }))
    try {
      const res: EnrichResponse = await enrichApi.enrich({
        debate,
        apiKey: state.apiKey,
        providerOverride: state.provider,
      })
      const diffs = diffArguments(debate, res.enrichedDebate)
      setState(s => ({
        ...s, step: 'diff',
        diffs, fieldsFilled: res.fieldsFilled,
        skippedIds: res.skippedArgumentIds ?? [],
        enrichedDebate: res.enrichedDebate,
        scoreResult: null, fullExplanation: null,
      }))
    } catch (err) {
      const msg = err instanceof ApiError
        ? `Enrichment failed (${err.status}): ${err.message}`
        : 'Enrichment request failed. Check your API key and backend status.'
      setState(s => ({ ...s, step: 'configure', error: msg }))
    }
  }, [state.apiKey, state.provider])

  const runEnrichAndScore = useCallback(async (debate: Debate) => {
    setState(s => ({ ...s, step: 'enriching', error: null }))
    try {
      const res: EnrichScoreResponse = await enrichApi.enrichAndScore({
        debate,
        apiKey: state.apiKey,
        providerOverride: state.provider,
      })
      const diffs = diffArguments(debate, res.enrichedDebate)
      setState(s => ({
        ...s, step: 'diff',
        diffs, fieldsFilled: res.fieldsFilled,
        skippedIds: res.skippedArgumentIds ?? [],
        enrichedDebate: res.enrichedDebate,
        scoreResult: res.scoringResult,
        fullExplanation: res.fullExplanation,
      }))
    } catch (err) {
      const msg = err instanceof ApiError
        ? `Enrich+Score failed (${err.status}): ${err.message}`
        : 'Enrich+Score failed. Check your API key and backend status.'
      setState(s => ({ ...s, step: 'configure', error: msg }))
    }
  }, [state.apiKey, state.provider])

  return { ...state, openPanel, closePanel, setProvider, setApiKey,
           runEnrich, runEnrichAndScore, clearError, resetToConfig }
}
