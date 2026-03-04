// ============================================================================
// useFlowSheet.ts — State + API orchestration for the flow sheet page
// ============================================================================

import { useState, useCallback, useRef } from 'react'
import { debateApi, configApi, ApiError } from '@/api/client'
import type { Debate, FlowGraphResponse } from '@/types/domain'
import type { ScoringResult } from '@/types/scoring'
import type { FormatConfig } from '@/types/config'

export type LoadingState = 'idle' | 'building-flow' | 'scoring' | 'loading-config'

export interface FlowSheetState {
  debate: Debate | null
  flow: FlowGraphResponse | null
  format: FormatConfig | null
  scoreResult: ScoringResult | null
  fullExplanation: string | null
  scorePanelVisible: boolean
  loading: LoadingState
  error: string | null
}

export interface FlowSheetActions {
  loadDebate: (debate: Debate) => Promise<void>
  scoreDebate: () => Promise<void>
  clearDebate: () => void
  clearError: () => void
  collapseScorePanel: () => void
  showScorePanel: () => void
  applyEnrichedDebate: (enrichedDebate: Debate) => void
  applyEnrichedWithScore: (enrichedDebate: Debate, scoreResult: import('@/types/scoring').ScoringResult, explanation: string | null) => void
}

const INITIAL: FlowSheetState = {
  debate: null, flow: null, format: null,
  scoreResult: null, fullExplanation: null,
  scorePanelVisible: false,
  loading: 'idle', error: null,
}

export function useFlowSheet(): FlowSheetState & FlowSheetActions {
  const [state, setState] = useState<FlowSheetState>(INITIAL)

  // Keep debate in a ref so scoreDebate can read current value without stale closure
  const debateRef = useRef<Debate | null>(null)

  const loadDebate = useCallback(async (debate: Debate) => {
    setState(s => ({ ...s, loading: 'building-flow', error: null }))
    debateRef.current = debate

    try {
      const [flow, format] = await Promise.all([
        debateApi.buildFlow({ debate }),
        configApi.getFormat(),
      ])
      setState(s => ({
        ...s,
        debate, flow, format,
        scoreResult: null, fullExplanation: null,
        loading: 'idle', error: null,
      }))
    } catch (err) {
      const message = err instanceof ApiError
        ? `API error ${err.status}: ${err.message}`
        : 'Failed to build flow graph. Is the backend running on :5000?'
      setState(s => ({ ...s, loading: 'idle', error: message }))
    }
  }, [])

  const scoreDebate = useCallback(async () => {
    const debate = debateRef.current
    if (!debate) return

    setState(s => ({ ...s, loading: 'scoring', error: null }))
    try {
      const res = await debateApi.score({ debate, includeFullExplanation: true })
      setState(s => ({
        ...s,
        scoreResult: res.result,
        fullExplanation: res.fullExplanation,
        scorePanelVisible: true,
        loading: 'idle',
      }))
    } catch (err) {
      const message = err instanceof ApiError
        ? `Scoring failed: ${err.message}`
        : 'Scoring failed. Check the backend logs.'
      setState(s => ({ ...s, loading: 'idle', error: message }))
    }
  }, [])

  const collapseScorePanel = useCallback(() => {
    setState(s => ({ ...s, scorePanelVisible: false }))
  }, [])

  const showScorePanel = useCallback(() => {
    setState(s => ({ ...s, scorePanelVisible: true }))
  }, [])

  // Called when user clicks "Apply enrichment" in the EnrichPanel
  const applyEnrichedDebate = useCallback((enrichedDebate: Debate) => {
    debateRef.current = enrichedDebate
    setState(s => ({ ...s, debate: enrichedDebate, scoreResult: null, fullExplanation: null }))
  }, [])

  // Called when user clicks "Apply enrichment + score" in the EnrichPanel
  const applyEnrichedWithScore = useCallback((
    enrichedDebate: Debate,
    scoreResult: import('@/types/scoring').ScoringResult,
    explanation: string | null,
  ) => {
    debateRef.current = enrichedDebate
    setState(s => ({
      ...s,
      debate: enrichedDebate,
      scoreResult,
      fullExplanation: explanation,
      scorePanelVisible: true,
    }))
  }, [])

  const clearDebate = useCallback(() => {
    debateRef.current = null
    setState(INITIAL)
  }, [])

  const clearError = useCallback(() => {
    setState(s => ({ ...s, error: null }))
  }, [])

  return { ...state, loadDebate, scoreDebate, clearDebate, clearError, collapseScorePanel, showScorePanel, applyEnrichedDebate, applyEnrichedWithScore }
}
