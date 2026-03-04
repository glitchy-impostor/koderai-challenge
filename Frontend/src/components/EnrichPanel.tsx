// ============================================================================
// EnrichPanel.tsx — LLM enrichment panel (modal overlay)
// ============================================================================

import { useEffect } from 'react'
import type { Debate } from '@/types/domain'
import type { EnrichmentState, EnrichmentActions } from '@/hooks/useEnrichment'
import { countEnrichableArgs } from '@/hooks/useEnrichment'
import { EnrichDiff } from './EnrichDiff'
import { Button, Badge, Spinner } from './ui'

type Props = EnrichmentState & EnrichmentActions & {
  debate: Debate
  onApply: (enrichedDebate: Debate) => void
  onApplyWithScore: (enrichedDebate: Debate, scoreResult: import('@/types/scoring').ScoringResult, explanation: string | null) => void
}

const PROVIDERS = ['Anthropic', 'OpenAI']

// ── Shared input style ─────────────────────────────────────────────────────────

function inputStyle(error = false): React.CSSProperties {
  return {
    width: '100%',
    padding: '8px 12px',
    fontFamily: 'var(--font-mono)',
    fontSize: 'var(--text-sm)',
    color: 'var(--text-primary)',
    background: 'var(--bg-overlay)',
    border: `1px solid ${error ? 'var(--neg)' : 'var(--border-default)'}`,
    borderRadius: 'var(--radius-md)',
    outline: 'none',
    transition: 'border-color var(--transition-fast)',
  }
}

export function EnrichPanel(props: Props) {
  const {
    open, step, provider, apiKey, diffs, fieldsFilled, skippedIds,
    error, enrichedDebate, scoreResult, fullExplanation,
    closePanel, setProvider, setApiKey,
    runEnrich, runEnrichAndScore, clearError, resetToConfig,
    onApply, onApplyWithScore, debate,
  } = props

  // Close on Escape
  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') closePanel() }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, closePanel])

  if (!open) return null

  const enrichableCount = countEnrichableArgs(debate)
  const isBusy = step === 'enriching'
  const hasDiff = step === 'diff'

  function handleApply() {
    if (!enrichedDebate) return
    onApply(enrichedDebate)
    closePanel()
  }

  function handleApplyWithScore() {
    if (!enrichedDebate || !scoreResult) return
    onApplyWithScore(enrichedDebate, scoreResult, fullExplanation)
    closePanel()
  }

  return (
    <>
      {/* Backdrop */}
      <div
        onClick={closePanel}
        style={{
          position: 'fixed', inset: 0,
          background: 'rgba(0,0,0,0.6)',
          zIndex: 'var(--z-modal)' as React.CSSProperties['zIndex'],
          backdropFilter: 'blur(2px)',
        }}
      />

      {/* Panel */}
      <div style={{
        position: 'fixed',
        top: '50%', left: '50%',
        transform: 'translate(-50%, -50%)',
        zIndex: 201,
        width: 'min(620px, 95vw)',
        maxHeight: '88vh',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--bg-elevated)',
        border: '1px solid var(--border-default)',
        borderRadius: 'var(--radius-lg)',
        boxShadow: 'var(--shadow-lg)',
        animation: 'fadeIn 0.15s ease',
      }}>
        <style>{`@keyframes fadeIn { from { opacity:0; transform:translate(-50%,-48%); } to { opacity:1; transform:translate(-50%,-50%); } }`}</style>

        {/* Header */}
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '14px 20px',
          borderBottom: '1px solid var(--border-default)',
          flexShrink: 0,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <span style={{ fontSize: 'var(--text-md)', fontWeight: 500 }}>Enrich with LLM</span>
            {enrichableCount > 0 && (
              <Badge variant="neutral">{enrichableCount} args with null fields</Badge>
            )}
            {enrichableCount === 0 && (
              <Badge variant="extended">All fields filled</Badge>
            )}
          </div>
          <button onClick={closePanel} style={{
            background: 'none', border: 'none', color: 'var(--text-muted)',
            cursor: 'pointer', fontSize: 18, lineHeight: 1,
          }}>✕</button>
        </div>

        {/* Body — scrollable */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '16px 20px', display: 'flex', flexDirection: 'column', gap: 16 }}>

          {/* Error banner */}
          {error && (
            <div style={{
              padding: '10px 14px',
              background: 'var(--neg-bg)', border: '1px solid var(--neg-dim)',
              borderRadius: 'var(--radius-md)',
              display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              gap: 12,
            }}>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: 'var(--neg-text)' }}>
                {error}
              </span>
              <button onClick={clearError} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>✕</button>
            </div>
          )}

          {/* Configure step */}
          {!hasDiff && (
            <>
              {/* Provider */}
              <div>
                <label style={{ display: 'block', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>
                  Provider
                </label>
                <div style={{ display: 'flex', gap: 8 }}>
                  {PROVIDERS.map(p => (
                    <button
                      key={p}
                      onClick={() => setProvider(p)}
                      style={{
                        padding: '6px 16px',
                        fontFamily: 'var(--font-mono)',
                        fontSize: 'var(--text-sm)',
                        borderRadius: 'var(--radius-md)',
                        cursor: 'pointer',
                        border: provider === p ? '1px solid var(--accent)' : '1px solid var(--border-default)',
                        background: provider === p ? 'var(--accent-dim)' : 'var(--bg-overlay)',
                        color: provider === p ? 'var(--text-primary)' : 'var(--text-muted)',
                        transition: 'all var(--transition-fast)',
                      }}
                    >
                      {p}
                    </button>
                  ))}
                </div>
              </div>

              {/* API Key */}
              <div>
                <label style={{ display: 'block', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>
                  API Key
                  <span style={{ marginLeft: 8, fontSize: 9, color: 'var(--text-disabled)', textTransform: 'none' }}>
                    never stored — used for this request only
                  </span>
                </label>
                <input
                  type="password"
                  value={apiKey}
                  onChange={e => setApiKey(e.target.value)}
                  placeholder={provider === 'Anthropic' ? 'sk-ant-...' : 'sk-...'}
                  style={inputStyle(!apiKey && error !== null)}
                />
              </div>

              {/* What will be enriched */}
              <div style={{
                padding: '10px 14px',
                background: 'var(--bg-overlay)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-md)',
                fontSize: 'var(--text-sm)',
                color: 'var(--text-muted)',
              }}>
                {enrichableCount > 0 ? (
                  <>
                    <span style={{ color: 'var(--text-secondary)' }}>
                      The LLM will fill in null fields for{' '}
                      <strong style={{ color: 'var(--text-primary)' }}>{enrichableCount} argument{enrichableCount !== 1 ? 's' : ''}</strong>:
                    </span>
                    {' '}evidenceQuality, impactMagnitude, fallacies.
                    Explicit values (already set) are never overwritten.
                  </>
                ) : (
                  'All arguments already have enrichment data. Running enrichment will have no effect.'
                )}
              </div>

              {/* Loading indicator */}
              {isBusy && (
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, color: 'var(--text-muted)', fontSize: 'var(--text-sm)' }}>
                  <Spinner size={14} />
                  Enriching {enrichableCount} arguments via {provider}…
                  <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-disabled)' }}>
                    (one API call per argument)
                  </span>
                </div>
              )}
            </>
          )}

          {/* Diff step */}
          {hasDiff && (
            <>
              <EnrichDiff diffs={diffs} fieldsFilled={fieldsFilled} skippedIds={skippedIds} />

              {/* Score result notice */}
              {scoreResult && (
                <div style={{
                  padding: '10px 14px',
                  background: scoreResult.winner === 'AFF' ? 'var(--aff-bg)' : 'var(--neg-bg)',
                  border: `1px solid ${scoreResult.winner === 'AFF' ? 'var(--aff-dim)' : 'var(--neg-dim)'}`,
                  borderRadius: 'var(--radius-md)',
                  display: 'flex', alignItems: 'center', gap: 10,
                }}>
                  <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: scoreResult.winner === 'AFF' ? 'var(--aff-text)' : 'var(--neg-text)', fontWeight: 600 }}>
                    {scoreResult.winner} wins
                  </span>
                  <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>
                    AFF {(scoreResult.affTotalScore ?? 0).toFixed(2)} vs NEG {(scoreResult.negTotalScore ?? 0).toFixed(2)}
                  </span>
                  <Badge variant="neutral" style={{ marginLeft: 'auto' }}>score ready</Badge>
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer actions */}
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'flex-end',
          gap: 8, padding: '12px 20px',
          borderTop: '1px solid var(--border-default)',
          flexShrink: 0,
        }}>
          {hasDiff ? (
            <>
              <Button variant="ghost" size="sm" onClick={resetToConfig}>
                ← Back
              </Button>
              <Button variant="ghost" size="sm" onClick={closePanel}>
                Discard
              </Button>
              {scoreResult ? (
                <Button variant="primary" size="sm" onClick={handleApplyWithScore}>
                  Apply enrichment + score
                </Button>
              ) : (
                <>
                  <Button variant="secondary" size="sm" onClick={handleApply}>
                    Apply enrichment
                  </Button>
                  <Button
                    variant="primary" size="sm"
                    loading={isBusy} disabled={isBusy || !apiKey.trim()}
                    onClick={() => enrichedDebate && runEnrichAndScore(enrichedDebate)}
                  >
                    Apply + Score
                  </Button>
                </>
              )}
            </>
          ) : (
            <>
              <Button variant="ghost" size="sm" onClick={closePanel}>Cancel</Button>
              <Button
                variant="secondary" size="sm"
                loading={isBusy} disabled={isBusy || !apiKey.trim()}
                onClick={() => runEnrich(debate)}
              >
                Enrich only
              </Button>
              <Button
                variant="primary" size="sm"
                loading={isBusy} disabled={isBusy || !apiKey.trim()}
                onClick={() => runEnrichAndScore(debate)}
              >
                Enrich + Score
              </Button>
            </>
          )}
        </div>
      </div>
    </>
  )
}
