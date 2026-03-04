// ============================================================================
// EnrichDiff.tsx — Per-argument enrichment diff view
// ============================================================================

import type { ArgDiff } from '@/hooks/useEnrichment'
import { Badge } from './ui'

interface EnrichDiffProps {
  diffs: ArgDiff[]
  fieldsFilled: number
  skippedIds: string[]
}

const FIELD_LABELS: Record<string, string> = {
  evidenceQuality: 'Evidence Quality',
  impactMagnitude: 'Impact Magnitude',
  fallacies: 'Fallacies',
  argumentStrength: 'Strength',
  status: 'Status',
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (Array.isArray(value)) return value.length === 0 ? '[]' : value.join(', ')
  return String(value)
}

function DiffRow({ diff }: { diff: ArgDiff }) {
  return (
    <div style={{
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--radius-md)',
      overflow: 'hidden',
      marginBottom: 6,
    }}>
      {/* Arg header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        padding: '7px 12px',
        background: 'var(--bg-elevated)',
        borderBottom: '1px solid var(--border-subtle)',
      }}>
        <Badge variant={diff.side === 'AFF' ? 'aff' : 'neg'}>{diff.side}</Badge>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-secondary)' }}>
          {diff.argumentId}
        </span>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)' }}>
          {diff.speechId}
        </span>
        <span style={{ marginLeft: 'auto', fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--extended)' }}>
          +{diff.fields.length} field{diff.fields.length !== 1 ? 's' : ''} filled
        </span>
      </div>

      {/* Field diffs */}
      <div style={{ padding: '6px 12px 8px' }}>
        {diff.fields.map(f => (
          <div key={f.field} style={{
            display: 'grid',
            gridTemplateColumns: '130px 1fr auto 1fr',
            alignItems: 'center',
            gap: 8,
            padding: '4px 0',
            borderBottom: '1px solid var(--border-subtle)',
          }}>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 9,
              color: 'var(--text-muted)',
              textTransform: 'uppercase',
              letterSpacing: '0.05em',
            }}>
              {FIELD_LABELS[f.field] ?? f.field}
            </span>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--text-disabled)',
              textDecoration: 'line-through',
            }}>
              {formatValue(f.before)}
            </span>
            <span style={{ color: 'var(--text-muted)', fontSize: 12 }}>→</span>
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--extended)',
              fontWeight: 500,
            }}>
              {formatValue(f.after)}
            </span>
          </div>
        ))}
      </div>
    </div>
  )
}

export function EnrichDiff({ diffs, fieldsFilled, skippedIds }: EnrichDiffProps) {
  if (diffs.length === 0) {
    return (
      <div style={{
        padding: 'var(--space-6)',
        textAlign: 'center',
        color: 'var(--text-muted)',
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-sm)',
        background: 'var(--bg-surface)',
        border: '1px solid var(--border-subtle)',
        borderRadius: 'var(--radius-md)',
      }}>
        No fields were enriched — all arguments already had enrichment data.
      </div>
    )
  }

  return (
    <div>
      {/* Summary line */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 12,
        padding: '8px 12px',
        background: 'var(--extended-bg)',
        border: '1px solid var(--extended-dim)',
        borderRadius: 'var(--radius-md)',
        marginBottom: 10,
      }}>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: 'var(--extended)' }}>
          ✓ {fieldsFilled} field{fieldsFilled !== 1 ? 's' : ''} filled across {diffs.length} argument{diffs.length !== 1 ? 's' : ''}
        </span>
        {skippedIds.length > 0 && (
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--dropped)' }}>
            {skippedIds.length} skipped
          </span>
        )}
      </div>

      {/* Skipped IDs */}
      {skippedIds.length > 0 && (
        <div style={{ marginBottom: 8, fontSize: 'var(--text-xs)', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)' }}>
          Skipped: {skippedIds.join(', ')}
        </div>
      )}

      {/* Diff rows */}
      <div style={{ maxHeight: 340, overflowY: 'auto' }}>
        {diffs.map(d => <DiffRow key={d.argumentId} diff={d} />)}
      </div>
    </div>
  )
}
