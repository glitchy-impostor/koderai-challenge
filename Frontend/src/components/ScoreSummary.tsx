// ============================================================================
// ScoreSummary.tsx — Per-stock-issue score breakdown table
// ============================================================================

import type { ScoringResult, StockIssueSummary } from '@/types/scoring'
import type { Side } from '@/types/domain'
import { Badge } from './ui'

interface ScoreSummaryProps {
  result: ScoringResult
}

// ── Mini horizontal score bar within a table cell ─────────────────────────────

function IssueMiniBar({ aff, neg }: { aff: number; neg: number }) {
  const total = Math.max(aff + neg, 0.01)
  const affPct = (aff / total) * 100

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 3, minWidth: 100 }}>
      <div style={{
        display: 'flex',
        height: 3,
        borderRadius: 99,
        overflow: 'hidden',
        background: 'var(--bg-overlay)',
      }}>
        <div style={{ width: `${affPct}%`, background: 'var(--aff)' }} />
        <div style={{ width: `${100 - affPct}%`, background: 'var(--neg)' }} />
      </div>
    </div>
  )
}

// ── Issue row ─────────────────────────────────────────────────────────────────

function IssueRow({ issue }: { issue: StockIssueSummary }) {
  const affLeads = (issue.affWeightedScore ?? issue.affRawScore ?? 0) >
                   (issue.negWeightedScore ?? issue.negRawScore ?? 0)
  const negLeads = !affLeads && (issue.negWeightedScore ?? issue.negRawScore ?? 0) >
                                (issue.affWeightedScore ?? issue.affRawScore ?? 0)

  const winnerSide: Side | null = issue.winner

  return (
    <tr style={{
      background: issue.isHardGate ? 'rgba(255, 154, 59, 0.04)' : undefined,
    }}>
      {/* Issue label */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        borderRight: '1px solid var(--border-subtle)',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            fontWeight: 500,
            color: 'var(--text-primary)',
          }}>
            {issue.issueLabel}
          </span>
          {issue.isHardGate && (
            <Badge variant="dropped" style={{ fontSize: 9 }}>Gate</Badge>
          )}
        </div>
      </td>

      {/* Weight */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        borderRight: '1px solid var(--border-subtle)',
        textAlign: 'center',
      }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-xs)',
          color: 'var(--text-muted)',
        }}>
          {issue.isHardGate ? '—' : `${Math.round((issue.weight ?? 0) * 100)}%`}
        </span>
      </td>

      {/* AFF raw score */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        borderRight: '1px solid var(--border-subtle)',
        textAlign: 'right',
      }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-sm)',
          fontWeight: affLeads ? 600 : 400,
          color: affLeads ? 'var(--aff-text)' : 'var(--text-secondary)',
        }}>
          {(issue.affWeightedScore ?? issue.affRawScore ?? 0).toFixed(2)}
        </span>
      </td>

      {/* NEG raw score */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        borderRight: '1px solid var(--border-subtle)',
        textAlign: 'right',
      }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-sm)',
          fontWeight: negLeads ? 600 : 400,
          color: negLeads ? 'var(--neg-text)' : 'var(--text-secondary)',
        }}>
          {(issue.negWeightedScore ?? issue.negRawScore ?? 0).toFixed(2)}
        </span>
      </td>

      {/* Visual bar */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        borderRight: '1px solid var(--border-subtle)',
        minWidth: 120,
      }}>
        <IssueMiniBar
          aff={issue.affWeightedScore ?? issue.affRawScore ?? 0}
          neg={issue.negWeightedScore ?? issue.negRawScore ?? 0}
        />
      </td>

      {/* Winner cell */}
      <td style={{
        padding: '10px 14px',
        borderBottom: '1px solid var(--border-subtle)',
        textAlign: 'center',
      }}>
        {winnerSide ? (
          <Badge variant={winnerSide === 'AFF' ? 'aff' : 'neg'}>
            {winnerSide}
          </Badge>
        ) : (
          <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-disabled)' }}>—</span>
        )}
      </td>
    </tr>
  )
}

// ── Total row ─────────────────────────────────────────────────────────────────

function TotalRow({ result }: { result: ScoringResult }) {
  const affTotal = result.affTotalScore ?? 0
  const negTotal = result.negTotalScore ?? 0
  const affWins  = result.winner === 'AFF'

  return (
    <tr style={{ background: 'var(--bg-overlay)' }}>
      <td style={{
        padding: '11px 14px',
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.06em',
        borderTop: '1px solid var(--border-default)',
        borderRight: '1px solid var(--border-subtle)',
      }}>
        Total
      </td>
      <td style={{ borderTop: '1px solid var(--border-default)', borderRight: '1px solid var(--border-subtle)' }} />
      <td style={{
        padding: '11px 14px',
        textAlign: 'right',
        borderTop: '1px solid var(--border-default)',
        borderRight: '1px solid var(--border-subtle)',
      }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-md)',
          fontWeight: 700,
          color: affWins ? 'var(--aff-text)' : 'var(--text-secondary)',
        }}>
          {affTotal.toFixed(2)}
        </span>
      </td>
      <td style={{
        padding: '11px 14px',
        textAlign: 'right',
        borderTop: '1px solid var(--border-default)',
        borderRight: '1px solid var(--border-subtle)',
      }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-md)',
          fontWeight: 700,
          color: !affWins ? 'var(--neg-text)' : 'var(--text-secondary)',
        }}>
          {negTotal.toFixed(2)}
        </span>
      </td>
      <td style={{ borderTop: '1px solid var(--border-default)', borderRight: '1px solid var(--border-subtle)' }} />
      <td style={{ padding: '11px 14px', textAlign: 'center', borderTop: '1px solid var(--border-default)' }}>
        <Badge variant={affWins ? 'aff' : 'neg'} style={{ fontWeight: 700 }}>
          {result.winner}
        </Badge>
      </td>
    </tr>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export function ScoreSummary({ result }: ScoreSummaryProps) {
  const issues = result.stockIssueSummaries ?? []

  if (issues.length === 0) {
    return (
      <div style={{
        padding: 'var(--space-6)',
        background: 'var(--bg-surface)',
        border: '1px solid var(--border-subtle)',
        borderRadius: 'var(--radius-md)',
        color: 'var(--text-muted)',
        fontSize: 'var(--text-sm)',
        fontFamily: 'var(--font-mono)',
        textAlign: 'center',
      }}>
        No stock issue breakdown available.
      </div>
    )
  }

  return (
    <div style={{
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      borderRadius: 'var(--radius-lg)',
      overflow: 'hidden',
    }}>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ background: 'var(--bg-elevated)' }}>
            {(['Issue', 'Weight', 'AFF', 'NEG', '', 'Winner'] as const).map((h, i) => (
              <th key={i} style={{
                padding: '8px 14px',
                textAlign: i === 0 ? 'left' : i <= 1 ? 'center' : i <= 3 ? 'right' : 'center',
                fontFamily: 'var(--font-mono)',
                fontSize: 'var(--text-xs)',
                fontWeight: 500,
                color: 'var(--text-muted)',
                textTransform: 'uppercase',
                letterSpacing: '0.06em',
                borderBottom: '1px solid var(--border-default)',
                borderRight: i < 5 ? '1px solid var(--border-subtle)' : undefined,
                whiteSpace: 'nowrap',
              }}>
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {issues.map(issue => <IssueRow key={issue.issueId} issue={issue} />)}
          <TotalRow result={result} />
        </tbody>
      </table>
    </div>
  )
}
