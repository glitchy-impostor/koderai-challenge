// ============================================================================
// FlowGrid.tsx — Main flow sheet grid
// Rows: one per thread, grouped by stock issue (with rowspan issue label)
// Columns: non-CX speeches in format order
// ============================================================================

import { useState } from 'react'
import type { Debate, FlowGraphResponse } from '@/types/domain'
import type { FormatConfig } from '@/types/config'
import type { ScoringResult } from '@/types/scoring'
import { buildGridModel, getSpeechSide, type GridIssueGroup } from './gridModel'
import { ArgumentCard } from './ArgumentCard'
import { ArgumentDetail } from './ArgumentDetail'
import { Badge } from './ui'

interface FlowGridProps {
  debate: Debate
  flow: FlowGraphResponse
  format: FormatConfig
  scoreResult: ScoringResult | null
}

// ── Speech column header ──────────────────────────────────────────────────────

function SpeechHeader({ speechId, format }: { speechId: string; format: FormatConfig }) {
  const side = getSpeechSide(speechId, format)
  const sideStyle =
    side === 'AFF'
      ? { color: 'var(--aff-text)', borderBottom: '2px solid var(--aff-dim)' }
      : side === 'NEG'
      ? { color: 'var(--neg-text)', borderBottom: '2px solid var(--neg-dim)' }
      : { color: 'var(--text-muted)', borderBottom: '2px solid var(--border-subtle)' }

  return (
    <th style={{
      position: 'sticky',
      top: 0,
      zIndex: 3,
      background: 'var(--bg-surface)',
      padding: '8px 10px',
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      fontWeight: 600,
      letterSpacing: '0.08em',
      whiteSpace: 'nowrap',
      width: 165,
      minWidth: 165,
      textAlign: 'center',
      ...sideStyle,
    }}>
      {speechId}
    </th>
  )
}

// ── Issue label cell ──────────────────────────────────────────────────────────

function IssueLabelCell({ group, rowSpan }: { group: GridIssueGroup; rowSpan: number }) {
  return (
    <td
      rowSpan={rowSpan}
      style={{
        position: 'sticky',
        left: 0,
        zIndex: 2,
        background: 'var(--bg-surface)',
        padding: '10px 12px',
        borderRight: '1px solid var(--border-default)',
        borderBottom: '1px solid var(--border-default)',
        verticalAlign: 'middle',
        minWidth: 128,
        width: 128,
      }}
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-sm)',
          fontWeight: 600,
          color: 'var(--text-primary)',
          letterSpacing: '-0.01em',
        }}>
          {group.issueLabel}
        </span>
        {group.isHardGate && (
          <Badge variant="dropped" style={{ fontSize: 9, width: 'fit-content' }}>
            Hard Gate
          </Badge>
        )}
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontSize: 9,
          color: 'var(--text-disabled)',
          letterSpacing: '0.04em',
        }}>
          {group.threads.length} thread{group.threads.length !== 1 ? 's' : ''}
        </span>
      </div>
    </td>
  )
}

// ── Empty cell ────────────────────────────────────────────────────────────────

function EmptyCell() {
  return (
    <td style={{
      padding: '6px 8px',
      borderBottom: '1px solid var(--border-subtle)',
      verticalAlign: 'top',
    }} />
  )
}

// ── Summary bar shown when scoring is available ───────────────────────────────

function SummaryBar({ result }: { result: ScoringResult }) {
  const affWins = result.winner === 'AFF'
  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      gap: 16,
      padding: '8px 16px',
      background: affWins ? 'var(--aff-bg)' : 'var(--neg-bg)',
      border: `1px solid ${affWins ? 'var(--aff-dim)' : 'var(--neg-dim)'}`,
      borderRadius: 'var(--radius-md)',
      marginBottom: 12,
    }}>
      <span style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-sm)',
        fontWeight: 600,
        color: affWins ? 'var(--aff-text)' : 'var(--neg-text)',
      }}>
        {result.winner} WINS
      </span>
      <span style={{ color: 'var(--border-strong)', fontSize: 12 }}>·</span>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--aff-text)' }}>
        AFF {(result.affTotalScore ?? 0).toFixed(2)}
      </span>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>vs</span>
      <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--neg-text)' }}>
        NEG {(result.negTotalScore ?? 0).toFixed(2)}
      </span>
      {result.decidedByHardGate && result.hardGateIssue && (
        <>
          <span style={{ color: 'var(--border-strong)', fontSize: 12 }}>·</span>
          <Badge variant="dropped">Hard Gate: {result.hardGateIssue}</Badge>
        </>
      )}
    </div>
  )
}

// ── Flow stats bar ─────────────────────────────────────────────────────────────

function FlowStats({ flow }: { flow: FlowGraphResponse }) {
  const s = flow.summary
  return (
    <div style={{
      display: 'flex',
      gap: 16,
      fontSize: 'var(--text-xs)',
      fontFamily: 'var(--font-mono)',
      color: 'var(--text-muted)',
      marginBottom: 10,
      flexWrap: 'wrap',
    }}>
      <span>{s.totalArguments} args</span>
      {s.droppedArguments > 0 && (
        <span style={{ color: 'var(--dropped)' }}>{s.droppedArguments} dropped</span>
      )}
      <span style={{ color: 'var(--aff-text)' }}>{s.affArguments} AFF</span>
      <span style={{ color: 'var(--neg-text)' }}>{s.negArguments} NEG</span>
      <span>{s.cxEvents} CX events</span>
      <span style={{ color: 'var(--text-disabled)' }}>
        Issues: {(s.stockIssuesCovered ?? []).join(', ')}
      </span>
    </div>
  )
}

// ── Main FlowGrid ─────────────────────────────────────────────────────────────

export function FlowGrid({ debate, flow, format, scoreResult }: FlowGridProps) {
  const [selectedArgumentId, setSelectedArgumentId] = useState<string | null>(null)

  const grid = buildGridModel(debate, flow, format)

  const selectedArgument = selectedArgumentId
    ? debate.arguments[selectedArgumentId] ?? null
    : null
  const selectedNode = selectedArgumentId
    ? grid.nodeMap.get(selectedArgumentId) ?? null
    : null

  function toggleSelect(argumentId: string) {
    setSelectedArgumentId(prev => (prev === argumentId ? null : argumentId))
  }

  return (
    <div>
      {scoreResult && <SummaryBar result={scoreResult} />}
      <FlowStats flow={flow} />

      {/* Scrollable grid container */}
      <div style={{
        overflowX: 'auto',
        overflowY: 'auto',
        maxHeight: 'calc(100vh - 290px)',
        border: '1px solid var(--border-default)',
        borderRadius: 'var(--radius-lg)',
        // Bottom padding so the detail panel doesn't obscure last rows
        paddingBottom: selectedArgumentId ? 240 : 0,
      }}>
        <table style={{
          borderCollapse: 'collapse',
          width: 'max-content',
          minWidth: '100%',
          tableLayout: 'fixed',
        }}>
          {/* Column widths */}
          <colgroup>
            <col style={{ width: 128 }} />
            {grid.speeches.map(s => (
              <col key={s} style={{ width: 165 }} />
            ))}
          </colgroup>

          {/* Header row */}
          <thead>
            <tr>
              {/* Corner cell */}
              <th style={{
                position: 'sticky',
                top: 0,
                left: 0,
                zIndex: 4,
                background: 'var(--bg-surface)',
                borderRight: '1px solid var(--border-default)',
                borderBottom: '2px solid var(--border-default)',
                padding: '8px 12px',
                textAlign: 'left',
                width: 128,
              }}>
                <span style={{
                  fontFamily: 'var(--font-mono)',
                  fontSize: 9,
                  color: 'var(--text-disabled)',
                  textTransform: 'uppercase',
                  letterSpacing: '0.08em',
                }}>
                  Issue
                </span>
              </th>

              {grid.speeches.map(speechId => (
                <SpeechHeader key={speechId} speechId={speechId} format={format} />
              ))}
            </tr>
          </thead>

          {/* Body: one row per thread */}
          <tbody>
            {grid.issueGroups.map(group =>
              group.threads.map((gridThread, tIdx) => (
                <tr key={gridThread.thread.rootArgumentId}>
                  {/* Issue label with rowspan (only on first thread) */}
                  {tIdx === 0 && (
                    <IssueLabelCell group={group} rowSpan={group.threads.length} />
                  )}

                  {/* One cell per speech */}
                  {grid.speeches.map(speechId => {
                    const nodes = gridThread.nodesBySpeech.get(speechId)
                    if (!nodes || nodes.length === 0) {
                      return <EmptyCell key={speechId} />
                    }

                    return (
                      <td
                        key={speechId}
                        style={{
                          padding: '6px 8px',
                          borderBottom: '1px solid var(--border-subtle)',
                          verticalAlign: 'top',
                        }}
                      >
                        {nodes.map(node => {
                          const argument = debate.arguments[node.argumentId]
                          if (!argument) return null
                          return (
                            <ArgumentCard
                              key={node.argumentId}
                              argument={argument}
                              node={node}
                              isSelected={selectedArgumentId === node.argumentId}
                              onClick={() => toggleSelect(node.argumentId)}
                            />
                          )
                        })}
                      </td>
                    )
                  })}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Detail panel — rendered outside the table */}
      {selectedArgument && selectedNode && (
        <ArgumentDetail
          argument={selectedArgument}
          node={selectedNode}
          onClose={() => setSelectedArgumentId(null)}
        />
      )}
    </div>
  )
}
