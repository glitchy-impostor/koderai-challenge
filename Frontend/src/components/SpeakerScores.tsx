// ============================================================================
// SpeakerScores.tsx — Per-speaker score breakdown
// ============================================================================

import { useState } from 'react'
import type { ScoringResult, SpeakerScoreSummary } from '@/types/scoring'
import { Badge } from './ui'

interface SpeakerScoresProps {
  result: ScoringResult
}

// ── Strength bar — visualises 0–5 scale ───────────────────────────────────────

function StrengthBar({ value }: { value: number }) {
  const pct = Math.min((value / 5) * 100, 100)
  const color = value >= 3.5 ? 'var(--score-high)' : value >= 2.0 ? 'var(--score-mid)' : 'var(--score-low)'

  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      gap: 6,
    }}>
      <div style={{
        width: 48,
        height: 4,
        borderRadius: 99,
        background: 'var(--bg-overlay)',
        overflow: 'hidden',
        flexShrink: 0,
      }}>
        <div style={{ width: `${pct}%`, height: '100%', background: color, borderRadius: 99 }} />
      </div>
      <span style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
      }}>
        {value.toFixed(1)}
      </span>
    </div>
  )
}

// ── Speaker card — one per speaker ────────────────────────────────────────────

function SpeakerCard({ speaker }: { speaker: SpeakerScoreSummary }) {
  const [expanded, setExpanded] = useState(false)
  const isAff = speaker.side === 'AFF'

  return (
    <div style={{
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--radius-md)',
      overflow: 'hidden',
      background: 'var(--bg-surface)',
    }}>
      {/* Header — clickable */}
      <div
        onClick={() => setExpanded(e => !e)}
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '10px 14px',
          cursor: 'pointer',
          background: expanded ? 'var(--bg-elevated)' : 'var(--bg-surface)',
          transition: 'background var(--transition-fast)',
          userSelect: 'none',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{
            color: 'var(--text-muted)',
            fontSize: 10,
            fontFamily: 'var(--font-mono)',
          }}>
            {expanded ? '▼' : '▶'}
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: 500,
            color: 'var(--text-primary)',
          }}>
            {speaker.speakerName}
          </span>
          <Badge variant={isAff ? 'aff' : 'neg'}>{speaker.side}</Badge>
        </div>

        {/* Score + stats */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          {/* Stats chips */}
          <div style={{ display: 'flex', gap: 10 }}>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--text-muted)',
            }}>
              {speaker.argumentCount} arg{speaker.argumentCount !== 1 ? 's' : ''}
            </span>
            {speaker.rebuttalCount > 0 && (
              <span style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 'var(--text-xs)',
                color: 'var(--text-muted)',
              }}>
                {speaker.rebuttalCount} rebuttal{speaker.rebuttalCount !== 1 ? 's' : ''}
              </span>
            )}
            {speaker.droppedCount > 0 && (
              <span style={{
                fontFamily: 'var(--font-mono)',
                fontSize: 'var(--text-xs)',
                color: 'var(--dropped)',
              }}>
                {speaker.droppedCount} dropped
              </span>
            )}
          </div>

          {/* Avg strength */}
          <StrengthBar value={speaker.averageStrength} />

          {/* Total score */}
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: 600,
            color: speaker.totalScore >= 0
              ? isAff ? 'var(--aff-text)' : 'var(--neg-text)'
              : 'var(--score-low)',
            minWidth: 56,
            textAlign: 'right',
          }}>
            {speaker.totalScore >= 0 ? '+' : ''}{speaker.totalScore.toFixed(2)}
          </span>
        </div>
      </div>

      {/* Expanded: per-rule contributions */}
      {expanded && (
        <div style={{
          borderTop: '1px solid var(--border-subtle)',
          padding: '10px 14px 14px 38px',
          background: 'var(--bg-overlay)',
        }}>
          {speaker.ruleContributions.length === 0 ? (
            <span style={{
              fontSize: 'var(--text-xs)',
              color: 'var(--text-disabled)',
              fontFamily: 'var(--font-mono)',
            }}>
              No rule contributions for this speaker.
            </span>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
              {speaker.ruleContributions.map(rc => (
                <div key={rc.ruleId} style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
                  <span style={{
                    fontFamily: 'var(--font-mono)',
                    fontSize: 'var(--text-xs)',
                    color: 'var(--text-muted)',
                    width: 180,
                    flexShrink: 0,
                  }}>
                    {rc.displayName}
                  </span>
                  <span style={{
                    fontFamily: 'var(--font-mono)',
                    fontSize: 'var(--text-xs)',
                    color: rc.score >= 0 ? 'var(--score-high)' : 'var(--score-low)',
                    width: 60,
                    textAlign: 'right',
                    flexShrink: 0,
                  }}>
                    {rc.score >= 0 ? '+' : ''}{rc.score.toFixed(2)}
                  </span>
                  <span style={{
                    fontFamily: 'var(--font-mono)',
                    fontSize: 10,
                    color: 'var(--text-disabled)',
                  }}>
                    ({rc.detailCount} item{rc.detailCount !== 1 ? 's' : ''})
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export function SpeakerScores({ result }: SpeakerScoresProps) {
  const speakers = result.speakerScoreSummaries ?? []

  if (speakers.length === 0) {
    return (
      <div style={{
        padding: 'var(--space-6)',
        color: 'var(--text-muted)',
        fontSize: 'var(--text-sm)',
        fontFamily: 'var(--font-mono)',
        textAlign: 'center',
        background: 'var(--bg-surface)',
        border: '1px solid var(--border-subtle)',
        borderRadius: 'var(--radius-md)',
      }}>
        No speaker score breakdown available.
      </div>
    )
  }

  // Split by side
  const affSpeakers = speakers.filter(s => s.side === 'AFF')
  const negSpeakers = speakers.filter(s => s.side === 'NEG')
  const affTotal = affSpeakers.reduce((sum, s) => sum + s.totalScore, 0)
  const negTotal = negSpeakers.reduce((sum, s) => sum + s.totalScore, 0)

  return (
    <div>
      {/* Section header */}
      <div style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.08em',
        marginBottom: 8,
      }}>
        Speaker Scores — {speakers.length} speaker{speakers.length !== 1 ? 's' : ''}
      </div>

      {/* Two-column layout */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        {/* AFF column */}
        <div>
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 6,
            padding: '0 4px',
          }}>
            <Badge variant="aff">AFF</Badge>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              fontWeight: 600,
              color: 'var(--aff-text)',
            }}>
              {affTotal.toFixed(2)}
            </span>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {affSpeakers.map(s => <SpeakerCard key={s.speakerId} speaker={s} />)}
          </div>
        </div>

        {/* NEG column */}
        <div>
          <div style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 6,
            padding: '0 4px',
          }}>
            <Badge variant="neg">NEG</Badge>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              fontWeight: 600,
              color: 'var(--neg-text)',
            }}>
              {negTotal.toFixed(2)}
            </span>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {negSpeakers.map(s => <SpeakerCard key={s.speakerId} speaker={s} />)}
          </div>
        </div>
      </div>
    </div>
  )
}
