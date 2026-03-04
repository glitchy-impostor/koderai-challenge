// ============================================================================
// SettingsPage.tsx — Phase E: three-tab settings with full config editors
// ============================================================================

import { useState } from 'react'
import { FormatConfigEditor } from '@/components/FormatConfigEditor'
import { ScoringConfigEditor } from '@/components/ScoringConfigEditor'
import { RoundConfigEditor } from '@/components/RoundConfigEditor'

type Tab = 'format' | 'scoring' | 'round'

const TABS: { id: Tab; label: string; desc: string }[] = [
  { id: 'format',  label: 'Format',  desc: 'Speech order, stock issues, hard gates, drop rules' },
  { id: 'scoring', label: 'Scoring', desc: 'Rule weights, penalties, multipliers, tiebreaker priority' },
  { id: 'round',   label: 'Round',   desc: 'Motion, stock case library, custom blueprints' },
]

export function SettingsPage() {
  const [tab, setTab] = useState<Tab>('format')

  return (
    <div style={{ padding: 'var(--space-8) var(--space-8) var(--space-8)' }}>
      {/* Page header */}
      <div style={{ marginBottom: 'var(--space-6)' }}>
        <h1 style={{
          fontSize: 'var(--text-xl)',
          fontWeight: 500,
          letterSpacing: '-0.02em',
          marginBottom: 4,
          color: 'var(--text-primary)',
        }}>
          Settings
        </h1>
        <p style={{ fontSize: 'var(--text-sm)', color: 'var(--text-muted)' }}>
          Configure the engine before a round. Changes are written to disk and take effect on the next score request.
        </p>
      </div>

      {/* Tab bar */}
      <div style={{
        display: 'flex',
        gap: 0,
        borderBottom: '1px solid var(--border-default)',
        marginBottom: 'var(--space-6)',
      }}>
        {TABS.map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            style={{
              background: 'none',
              border: 'none',
              padding: '10px 18px',
              fontFamily: 'var(--font-ui)',
              fontSize: 'var(--text-sm)',
              fontWeight: tab === t.id ? 500 : 400,
              color: tab === t.id ? 'var(--text-primary)' : 'var(--text-muted)',
              borderBottom: tab === t.id ? '2px solid var(--accent)' : '2px solid transparent',
              cursor: 'pointer',
              transition: 'color var(--transition-fast)',
              marginBottom: -1,
            }}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Active tab description */}
      <div style={{
        marginBottom: 20,
        fontSize: 'var(--text-sm)',
        color: 'var(--text-muted)',
        fontStyle: 'italic',
      }}>
        {TABS.find(t => t.id === tab)?.desc}
      </div>

      {/* Editor panels — mounted eagerly so edits aren't lost on tab switch */}
      <div style={{ display: tab === 'format' ? 'block' : 'none' }}>
        <FormatConfigEditor />
      </div>
      <div style={{ display: tab === 'scoring' ? 'block' : 'none' }}>
        <ScoringConfigEditor />
      </div>
      <div style={{ display: tab === 'round' ? 'block' : 'none' }}>
        <RoundConfigEditor />
      </div>
    </div>
  )
}
