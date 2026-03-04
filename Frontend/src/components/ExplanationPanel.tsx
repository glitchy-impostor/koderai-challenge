// ============================================================================
// ExplanationPanel.tsx — Collapsible full explanation text
// ============================================================================

import { useState } from 'react'

interface ExplanationPanelProps {
  explanation: string
}

export function ExplanationPanel({ explanation }: ExplanationPanelProps) {
  const [open, setOpen] = useState(false)

  if (!explanation) return null

  // Split on paragraph breaks or double-newlines
  const paragraphs = explanation
    .split(/\n{2,}/)
    .map(p => p.trim())
    .filter(Boolean)

  return (
    <div style={{
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--radius-md)',
      overflow: 'hidden',
    }}>
      {/* Toggle header */}
      <div
        onClick={() => setOpen(o => !o)}
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '10px 14px',
          cursor: 'pointer',
          background: open ? 'var(--bg-elevated)' : 'var(--bg-surface)',
          userSelect: 'none',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: 'var(--text-muted)', fontSize: 10, fontFamily: 'var(--font-mono)' }}>
            {open ? '▼' : '▶'}
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-sm)',
            color: 'var(--text-secondary)',
            fontWeight: 500,
          }}>
            Full Explanation
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 9,
            color: 'var(--text-disabled)',
            textTransform: 'uppercase',
            letterSpacing: '0.06em',
          }}>
            {paragraphs.length} paragraphs
          </span>
        </div>

        {!open && (
          <span style={{
            fontSize: 'var(--text-xs)',
            color: 'var(--text-muted)',
            fontStyle: 'italic',
            maxWidth: 440,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}>
            {paragraphs[0]}
          </span>
        )}
      </div>

      {/* Expanded content */}
      {open && (
        <div style={{
          padding: '14px 18px',
          borderTop: '1px solid var(--border-subtle)',
          background: 'var(--bg-surface)',
          display: 'flex',
          flexDirection: 'column',
          gap: 12,
          maxHeight: 420,
          overflowY: 'auto',
        }}>
          {paragraphs.map((para, i) => (
            <p key={i} style={{
              fontSize: 'var(--text-sm)',
              lineHeight: 1.65,
              margin: 0,
              fontWeight: /^(#{1,3}\s|[A-Z]{3,}:)/.test(para) ? 500 : 400,
              color: /^(#{1,3}\s|[A-Z]{3,}:)/.test(para) ? 'var(--text-primary)' : 'var(--text-secondary)',
            }}>
              {para.replace(/^#+\s/, '')}
            </p>
          ))}
        </div>
      )}
    </div>
  )
}
