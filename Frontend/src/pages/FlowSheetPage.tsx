// ============================================================================
// FlowSheetPage.tsx — Phase D: EnrichPanel wired in
// ============================================================================

import { useFlowSheet } from '@/hooks/useFlowSheet'
import { useEnrichment } from '@/hooks/useEnrichment'
import { DebateInput } from '@/components/DebateInput'
import { FlowGrid } from '@/components/FlowGrid'
import { ScorePanel } from '@/components/ScorePanel'
import { EnrichPanel } from '@/components/EnrichPanel'
import { Button, Badge, Spinner } from '@/components/ui'

export function FlowSheetPage() {
  const flow = useFlowSheet()
  const enrich = useEnrichment()

  const {
    debate, flow: flowGraph, format,
    scoreResult, fullExplanation, scorePanelVisible,
    loading, error,
    loadDebate, scoreDebate, clearDebate, clearError,
    collapseScorePanel, showScorePanel,
    applyEnrichedDebate, applyEnrichedWithScore,
  } = flow

  const isBuilding = loading === 'building-flow'
  const isScoring  = loading === 'scoring'
  const busy       = isBuilding || isScoring

  const errorBanner = error ? (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '10px 16px',
      background: 'var(--neg-bg)', border: '1px solid var(--neg-dim)',
      borderRadius: 'var(--radius-md)', marginBottom: 'var(--space-5)',
    }}>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: 'var(--neg-text)' }}>
        {error}
      </span>
      <button onClick={clearError} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer', fontSize: 16 }}>✕</button>
    </div>
  ) : null

  if (!debate || !flowGraph || !format) {
    return (
      <div style={{ padding: 'var(--space-8)' }}>
        {errorBanner}
        <DebateInput onLoad={loadDebate} loading={isBuilding} />
      </div>
    )
  }

  return (
    <div style={{ padding: 'var(--space-5) var(--space-6)' }}>
      {errorBanner}

      {/* Action bar */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        flexWrap: 'wrap', gap: 10, marginBottom: 14,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', fontWeight: 600, color: 'var(--text-primary)' }}>
            {debate.debateId}
          </span>
          <Badge variant="neutral">{flowGraph.summary.totalArguments} args</Badge>
          {flowGraph.summary.droppedArguments > 0 && (
            <Badge variant="dropped">{flowGraph.summary.droppedArguments} dropped</Badge>
          )}
          {scoreResult && !scorePanelVisible && (
            <Badge variant={scoreResult.winner === 'AFF' ? 'aff' : 'neg'}>
              {scoreResult.winner} wins · {(scoreResult.affTotalScore ?? 0).toFixed(1)} vs {(scoreResult.negTotalScore ?? 0).toFixed(1)}
            </Badge>
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          {isScoring && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--text-muted)', fontSize: 'var(--text-sm)' }}>
              <Spinner size={12} /> Scoring…
            </div>
          )}
          {scoreResult && !scorePanelVisible && (
            <Button variant="ghost" size="sm" onClick={showScorePanel}>Show Score ▲</Button>
          )}
          <Button variant="secondary" size="sm" onClick={scoreDebate} loading={isScoring} disabled={busy}>
            {scoreResult ? 'Re-score' : 'Score Round'}
          </Button>
          <Button variant="ghost" size="sm" onClick={enrich.openPanel} disabled={busy}>
            Enrich with LLM ▾
          </Button>
          <Button variant="ghost" size="sm" onClick={clearDebate} disabled={busy}>
            ← Change Debate
          </Button>
        </div>
      </div>

      {/* Score panel */}
      {scoreResult && scorePanelVisible && (
        <ScorePanel result={scoreResult} fullExplanation={fullExplanation} onCollapse={collapseScorePanel} />
      )}

      {/* Flow grid */}
      <FlowGrid debate={debate} flow={flowGraph} format={format} scoreResult={scoreResult} />

      {/* Enrichment panel (modal) */}
      <EnrichPanel
        {...enrich}
        debate={debate}
        onApply={applyEnrichedDebate}
        onApplyWithScore={applyEnrichedWithScore}
      />
    </div>
  )
}
