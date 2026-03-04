// ============================================================================
// ArgumentDetail.tsx — Slide-up detail panel for a selected argument
// ============================================================================

import type { Argument, FlowNode } from '@/types/domain'
import { Badge } from './ui'
import { fmtStrength, strengthColor } from './gridModel'

interface ArgumentDetailProps {
  argument: Argument
  node: FlowNode
  onClose: () => void
}

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', gap: 12, alignItems: 'flex-start', padding: '8px 0', borderBottom: '1px solid var(--border-subtle)' }}>
      <span style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.06em',
        width: 110,
        flexShrink: 0,
        paddingTop: 1,
      }}>
        {label}
      </span>
      <span style={{ fontSize: 'var(--text-sm)', color: 'var(--text-primary)', lineHeight: 1.55 }}>
        {value}
      </span>
    </div>
  )
}

export function ArgumentDetail({ argument, node, onClose }: ArgumentDetailProps) {
  const { core, enrichment } = argument

  const sideColor  = node.side === 'AFF' ? 'var(--aff)' : 'var(--neg)'
  const strengthC  = strengthColor(node.computedStrength)

  return (
    <div style={{
      position: 'fixed',
      bottom: 0,
      left: 0,
      right: 0,
      zIndex: 'var(--z-modal)' as React.CSSProperties['zIndex'],
      background: 'var(--bg-elevated)',
      borderTop: `2px solid ${sideColor}`,
      boxShadow: '0 -8px 32px rgba(0,0,0,0.7)',
      animation: 'slideUp 0.2s ease',
      maxHeight: '50vh',
      overflowY: 'auto',
    }}>
      <style>{`
        @keyframes slideUp {
          from { transform: translateY(100%); opacity: 0; }
          to   { transform: translateY(0);    opacity: 1; }
        }
      `}</style>

      {/* Header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '10px 20px',
        borderBottom: '1px solid var(--border-default)',
        position: 'sticky',
        top: 0,
        background: 'var(--bg-elevated)',
        zIndex: 1,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <Badge variant={node.side === 'AFF' ? 'aff' : 'neg'}>{node.side}</Badge>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            color: 'var(--text-secondary)',
          }}>
            {argument.argumentId}
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-xs)',
            color: 'var(--text-muted)',
          }}>
            {argument.speechId} · {argument.stockIssueTag}
          </span>
          {node.status !== 'Active' && (
            <Badge variant={
              node.status === 'Dropped' ? 'dropped' :
              node.status === 'Extended' ? 'extended' : 'conceded'
            }>
              {node.status}{node.statusIsOverridden ? ' (override)' : ''}
            </Badge>
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          {/* Strength display */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>Strength</span>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontWeight: 600,
              fontSize: 'var(--text-md)',
              color: strengthC,
            }}>
              {fmtStrength(node.computedStrength)}
            </span>
            <span style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>/ 5</span>
          </div>

          <button
            onClick={onClose}
            style={{
              background: 'var(--bg-overlay)',
              border: '1px solid var(--border-default)',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              padding: '4px 10px',
              fontSize: 'var(--text-sm)',
              fontFamily: 'var(--font-ui)',
            }}
          >
            ✕
          </button>
        </div>
      </div>

      {/* Content */}
      <div style={{ padding: '4px 20px 16px' }}>
        <DetailRow label="Claim" value={core.claim} />
        <DetailRow label="Reasoning" value={core.reasoning} />
        <DetailRow label="Impact" value={core.impact} />

        {core.evidenceSource && (
          <DetailRow
            label="Evidence"
            value={
              <span style={{ color: 'var(--text-secondary)', fontStyle: 'italic' }}>
                {core.evidenceSource}
              </span>
            }
          />
        )}

        {/* Enrichment */}
        <div style={{ display: 'flex', gap: 16, marginTop: 12, flexWrap: 'wrap' }}>
          {enrichment.evidenceQuality && (
            <Chip label="Evidence Quality" value={enrichment.evidenceQuality} />
          )}
          {enrichment.impactMagnitude && (
            <Chip label="Impact Magnitude" value={enrichment.impactMagnitude} />
          )}
          {enrichment.fallacies.length > 0 && (
            <Chip
              label="Fallacies"
              value={enrichment.fallacies.join(', ')}
              color="var(--neg)"
            />
          )}
          {enrichment.argumentStrength != null && (
            <Chip
              label="Manual Strength"
              value={String(enrichment.argumentStrength)}
            />
          )}
        </div>

        {/* Rebuttal targets */}
        {argument.rebuttalTargetIds.length > 0 && (
          <div style={{ marginTop: 10 }}>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--text-muted)',
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
            }}>
              Rebuts
            </span>
            <div style={{ display: 'flex', gap: 6, marginTop: 4, flexWrap: 'wrap' }}>
              {argument.rebuttalTargetIds.map(id => (
                <span key={id} style={{
                  fontFamily: 'var(--font-mono)',
                  fontSize: 'var(--text-xs)',
                  color: 'var(--accent)',
                  background: 'var(--accent-dim)',
                  padding: '2px 7px',
                  borderRadius: 'var(--radius-sm)',
                  border: '1px solid #3a2888',
                }}>
                  {id}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

function Chip({
  label,
  value,
  color = 'var(--text-secondary)',
}: {
  label: string
  value: string
  color?: string
}) {
  return (
    <div style={{
      background: 'var(--bg-overlay)',
      border: '1px solid var(--border-default)',
      borderRadius: 'var(--radius-sm)',
      padding: '5px 10px',
    }}>
      <div style={{ fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', fontFamily: 'var(--font-mono)' }}>
        {label}
      </div>
      <div style={{ fontSize: 'var(--text-sm)', color, fontWeight: 500, marginTop: 2 }}>
        {value}
      </div>
    </div>
  )
}

// Export React import for JSX
import React from 'react'
void React
