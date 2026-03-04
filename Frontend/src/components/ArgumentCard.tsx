// ============================================================================
// ArgumentCard.tsx — Renders one argument node in a grid cell
// ============================================================================

import type { Argument, FlowNode } from '@/types/domain'
import { Badge } from './ui'
import { fmtStrength, strengthColor } from './gridModel'

interface ArgumentCardProps {
  argument: Argument
  node: FlowNode
  isSelected: boolean
  onClick: () => void
}

type StatusVariant = 'dropped' | 'extended' | 'conceded' | 'neutral'

function statusVariant(status: string): StatusVariant {
  switch (status) {
    case 'Dropped':  return 'dropped'
    case 'Extended': return 'extended'
    case 'Conceded': return 'conceded'
    default:         return 'neutral'
  }
}

// 5-pip strength indicator
function StrengthPips({ value }: { value: number }) {
  const pips = 5
  const color = strengthColor(value)
  return (
    <div style={{ display: 'flex', gap: 2, alignItems: 'center' }}>
      {Array.from({ length: pips }, (_, i) => {
        const filled = value >= i + 1
        const partial = !filled && value > i  // fractional last pip
        const fill = partial
          ? `linear-gradient(90deg, ${color} ${Math.round((value - i) * 100)}%, var(--border-strong) 0%)`
          : filled ? color : 'var(--border-strong)'
        return (
          <div
            key={i}
            style={{
              width: 5,
              height: 5,
              borderRadius: 1,
              background: fill,
              flexShrink: 0,
            }}
          />
        )
      })}
      <span style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 10,
        color,
        marginLeft: 3,
        fontWeight: 500,
      }}>
        {fmtStrength(value)}
      </span>
    </div>
  )
}

export function ArgumentCard({ argument, node, isSelected, onClick }: ArgumentCardProps) {
  const isDropped = node.status === 'Dropped'
  const isConceded = node.status === 'Conceded'
  const isExtended = node.status === 'Extended'
  const showStatus = isDropped || isConceded || isExtended

  const sideColor = node.side === 'AFF' ? 'var(--aff)' : 'var(--neg)'
  const sideBg = node.side === 'AFF' ? 'var(--aff-bg)' : 'var(--neg-bg)'

  // Dropped overrides side background with a more urgent amber tint
  const bgColor = isDropped ? 'var(--dropped-bg)' : sideBg
  const borderLeftColor = isDropped ? 'var(--dropped)' : sideColor

  return (
    <button
      onClick={onClick}
      title={`${argument.argumentId} — click for full detail`}
      style={{
        display: 'block',
        width: '100%',
        textAlign: 'left',
        background: bgColor,
        border: `1px solid ${isSelected ? 'var(--accent)' : 'var(--border-subtle)'}`,
        borderLeft: `3px solid ${borderLeftColor}`,
        borderRadius: 'var(--radius-sm)',
        padding: '6px 8px',
        cursor: 'pointer',
        marginBottom: 4,
        boxShadow: isSelected ? `0 0 0 1px var(--accent)` : 'none',
        transition: 'all var(--transition-fast)',
        position: 'relative',
      }}
    >
      {/* Header row: side badge + status */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 4,
        marginBottom: 5,
        flexWrap: 'wrap',
      }}>
        <Badge variant={node.side === 'AFF' ? 'aff' : 'neg'}>
          {node.side}
        </Badge>
        {showStatus && (
          <Badge variant={statusVariant(node.status)}>
            {node.status}
            {node.statusIsOverridden && ' *'}
          </Badge>
        )}
        {node.resolvedEnrichment?.evidenceSource === 'Blueprint' && (
          <Badge variant="neutral" style={{ fontSize: 9, opacity: 0.6 }}>BP</Badge>
        )}
      </div>

      {/* Claim text — clamped to 3 lines */}
      <div style={{
        fontSize: 'var(--text-xs)',
        color: isDropped ? 'var(--dropped)' : 'var(--text-primary)',
        lineHeight: 1.45,
        display: '-webkit-box',
        WebkitLineClamp: 3,
        WebkitBoxOrient: 'vertical',
        overflow: 'hidden',
        marginBottom: 6,
        fontWeight: isDropped ? 400 : 400,
        textDecoration: isDropped ? 'line-through' : 'none',
        textDecorationColor: 'var(--dropped)',
      }}>
        {argument.core.claim}
      </div>

      {/* Footer: strength + issue tag */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 4,
      }}>
        <StrengthPips value={node.computedStrength} />
        <span style={{
          fontSize: 9,
          color: 'var(--text-disabled)',
          fontFamily: 'var(--font-mono)',
          letterSpacing: '0.04em',
          textTransform: 'uppercase',
        }}>
          {argument.stockIssueTag.slice(0, 4)}
        </span>
      </div>
    </button>
  )
}
