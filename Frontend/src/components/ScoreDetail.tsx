// ============================================================================
// ScoreDetail.tsx — Per-rule breakdown with argument-level drill-down
// ============================================================================

import { useState } from 'react'
import type { ScoringResult, RuleResult, ArgumentScoreDetail } from '@/types/scoring'
import { Badge } from './ui'

interface ScoreDetailProps {
  result: ScoringResult
}

// ── Argument row in a rule's detail table ─────────────────────────────────────

function ArgDetailRow({ arg }: { arg: ArgumentScoreDetail }) {
  const [expanded, setExpanded] = useState(false)

  return (
    <>
      <tr
        onClick={() => setExpanded(e => !e)}
        style={{
          cursor: 'pointer',
          background: expanded ? 'var(--bg-overlay)' : undefined,
          transition: 'background var(--transition-fast)',
        }}
      >
        {/* Expand indicator */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)', width: 24 }}>
          <span style={{ color: 'var(--text-muted)', fontSize: 10, fontFamily: 'var(--font-mono)' }}>
            {expanded ? '▼' : '▶'}
          </span>
        </td>

        {/* argumentId */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--text-secondary)',
            }}>
              {arg.argumentId}
            </span>
            {arg.isDropped && <Badge variant="dropped" style={{ fontSize: 9 }}>Dropped</Badge>}
          </div>
        </td>

        {/* speechId */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)', textAlign: 'center' }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>
            {arg.speechId}
          </span>
        </td>

        {/* side */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)', textAlign: 'center' }}>
          <Badge variant={arg.side === 'AFF' ? 'aff' : 'neg'}>{arg.side}</Badge>
        </td>

        {/* strength */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)', textAlign: 'right' }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>
            {(arg.computedStrength ?? 0).toFixed(1)}
          </span>
        </td>

        {/* net score */}
        <td style={{ padding: '7px 10px', borderBottom: '1px solid var(--border-subtle)', textAlign: 'right' }}>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: 500,
            color: (arg.netScore ?? 0) >= 0
              ? arg.side === 'AFF' ? 'var(--aff-text)' : 'var(--neg-text)'
              : 'var(--score-low)',
          }}>
            {(arg.netScore ?? 0) >= 0 ? '+' : ''}{(arg.netScore ?? 0).toFixed(2)}
          </span>
        </td>
      </tr>

      {/* Expanded: rule breakdown for this argument */}
      {expanded && (
        <tr>
          <td colSpan={6} style={{ padding: 0, borderBottom: '1px solid var(--border-subtle)' }}>
            <div style={{
              padding: '8px 14px 12px 40px',
              background: 'var(--bg-overlay)',
              borderLeft: `3px solid ${arg.side === 'AFF' ? 'var(--aff-dim)' : 'var(--neg-dim)'}`,
            }}>
              {/* Penalties row */}
              {((arg.droppedPenalty ?? 0) > 0 || (arg.fallacyPenalty ?? 0) > 0) && (
                <div style={{ display: 'flex', gap: 16, marginBottom: 8 }}>
                  {(arg.droppedPenalty ?? 0) > 0 && (
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--dropped)' }}>
                      Dropped penalty: −{arg.droppedPenalty.toFixed(2)}
                    </span>
                  )}
                  {(arg.fallacyPenalty ?? 0) > 0 && (
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--neg-text)' }}>
                      Fallacy penalty: −{arg.fallacyPenalty.toFixed(2)}
                    </span>
                  )}
                </div>
              )}

              {/* Per-rule breakdown */}
              {(arg.ruleBreakdown ?? []).length > 0 ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                  {arg.ruleBreakdown.map((rb, i) => (
                    <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'flex-start' }}>
                      <span style={{
                        fontFamily: 'var(--font-mono)',
                        fontSize: 9,
                        color: 'var(--text-muted)',
                        width: 160,
                        flexShrink: 0,
                        paddingTop: 1,
                      }}>
                        {rb.ruleName}
                      </span>
                      <span style={{
                        fontFamily: 'var(--font-mono)',
                        fontSize: 'var(--text-xs)',
                        color: (rb.score ?? 0) >= 0 ? 'var(--score-high)' : 'var(--score-low)',
                        width: 50,
                        flexShrink: 0,
                        textAlign: 'right',
                      }}>
                        {(rb.score ?? 0) >= 0 ? '+' : ''}{(rb.score ?? 0).toFixed(2)}
                      </span>
                      {rb.notes && (
                        <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)', lineHeight: 1.4 }}>
                          {rb.notes}
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-disabled)' }}>
                  No per-rule breakdown available for this argument.
                </span>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  )
}

// ── Rule accordion panel ──────────────────────────────────────────────────────

function RulePanel({ rule }: { rule: RuleResult }) {
  const [open, setOpen] = useState(false)

  const affLeads = (rule.affScore ?? 0) > (rule.negScore ?? 0)
  const negLeads = (rule.negScore ?? 0) > (rule.affScore ?? 0)

  return (
    <div style={{
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--radius-md)',
      overflow: 'hidden',
      marginBottom: 6,
    }}>
      {/* Header row — clickable */}
      <div
        onClick={() => setOpen(o => !o)}
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '10px 14px',
          cursor: 'pointer',
          background: open ? 'var(--bg-elevated)' : 'var(--bg-surface)',
          transition: 'background var(--transition-fast)',
          userSelect: 'none',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{ color: 'var(--text-muted)', fontSize: 10, fontFamily: 'var(--font-mono)' }}>
            {open ? '▼' : '▶'}
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: 500,
            color: 'var(--text-primary)',
          }}>
            {rule.displayName ?? rule.ruleId}
          </span>
          {rule.explanation && (
            <span style={{
              fontSize: 'var(--text-xs)',
              color: 'var(--text-muted)',
              maxWidth: 360,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}>
              {rule.explanation}
            </span>
          )}
        </div>

        {/* Score pair */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexShrink: 0 }}>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: affLeads ? 600 : 400,
            color: affLeads ? 'var(--aff-text)' : 'var(--text-secondary)',
          }}>
            AFF {(rule.affScore ?? 0).toFixed(2)}
          </span>
          <span style={{ color: 'var(--border-strong)', fontSize: 12 }}>·</span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: negLeads ? 600 : 400,
            color: negLeads ? 'var(--neg-text)' : 'var(--text-secondary)',
          }}>
            NEG {(rule.negScore ?? 0).toFixed(2)}
          </span>
        </div>
      </div>

      {/* Expanded: argument detail table */}
      {open && (
        <div>
          {(rule.argumentDetails ?? []).length === 0 ? (
            <div style={{
              padding: '12px 14px',
              fontSize: 'var(--text-xs)',
              color: 'var(--text-disabled)',
              fontFamily: 'var(--font-mono)',
              borderTop: '1px solid var(--border-subtle)',
            }}>
              No per-argument details for this rule.
            </div>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ background: 'var(--bg-overlay)' }}>
                  {(['', 'Argument', 'Speech', 'Side', 'Strength', 'Score'] as const).map((h, i) => (
                    <th key={i} style={{
                      padding: '6px 10px',
                      textAlign: i === 0 ? 'center' : i <= 2 ? 'left' : i === 3 ? 'center' : 'right',
                      fontFamily: 'var(--font-mono)',
                      fontSize: 9,
                      fontWeight: 500,
                      color: 'var(--text-disabled)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.06em',
                      borderBottom: '1px solid var(--border-subtle)',
                      borderTop: '1px solid var(--border-subtle)',
                    }}>
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rule.argumentDetails.map(arg => (
                  <ArgDetailRow key={arg.argumentId} arg={arg} />
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  )
}

// ── Main ScoreDetail component ────────────────────────────────────────────────

export function ScoreDetail({ result }: ScoreDetailProps) {
  const rules = result.ruleResults ?? []

  if (rules.length === 0) {
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
        No rule-level breakdown available.
      </div>
    )
  }

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
        Rule Breakdown — {rules.length} rules
      </div>

      {rules.map(rule => (
        <RulePanel key={rule.ruleId} rule={rule} />
      ))}
    </div>
  )
}
