// ============================================================================
// ScorePanel.tsx — Full score display with Summary / Detail tabs
// ============================================================================

import { useState } from 'react'
import type { ScoringResult } from '@/types/scoring'
import { WinnerBanner } from './WinnerBanner'
import { ScoreSummary } from './ScoreSummary'
import { ScoreDetail } from './ScoreDetail'
import { SpeakerScores } from './SpeakerScores'
import { ExplanationPanel } from './ExplanationPanel'

interface ScorePanelProps {
  result: ScoringResult
  fullExplanation: string | null
  onCollapse: () => void
}

type Tab = 'summary' | 'detail' | 'speakers'

function TabBtn({
  active,
  onClick,
  children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      onClick={onClick}
      style={{
        background: 'none',
        border: 'none',
        padding: '6px 12px',
        fontFamily: 'var(--font-ui)',
        fontSize: 'var(--text-sm)',
        fontWeight: active ? 500 : 400,
        color: active ? 'var(--text-primary)' : 'var(--text-muted)',
        borderBottom: active ? '2px solid var(--accent)' : '2px solid transparent',
        cursor: 'pointer',
        transition: 'all var(--transition-fast)',
        marginBottom: -1,
      }}
    >
      {children}
    </button>
  )
}

export function ScorePanel({ result, fullExplanation, onCollapse }: ScorePanelProps) {
  const [tab, setTab] = useState<Tab>('summary')

  return (
    <div style={{
      background: 'var(--bg-surface)',
      border: '1px solid var(--border-default)',
      borderRadius: 'var(--radius-lg)',
      overflow: 'hidden',
      marginBottom: 16,
    }}>
      {/* Panel header */}
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 16px',
        borderBottom: '1px solid var(--border-default)',
        background: 'var(--bg-elevated)',
      }}>
        {/* Tabs */}
        <div style={{ display: 'flex', gap: 2 }}>
          <TabBtn active={tab === 'summary'} onClick={() => setTab('summary')}>
            Summary
          </TabBtn>
          <TabBtn active={tab === 'detail'} onClick={() => setTab('detail')}>
            Rule Breakdown
          </TabBtn>
          <TabBtn active={tab === 'speakers'} onClick={() => setTab('speakers')}>
            Speakers
          </TabBtn>
        </div>

        {/* Collapse */}
        <button
          onClick={onCollapse}
          style={{
            background: 'none',
            border: '1px solid var(--border-default)',
            borderRadius: 'var(--radius-sm)',
            color: 'var(--text-muted)',
            cursor: 'pointer',
            padding: '3px 10px',
            fontSize: 'var(--text-xs)',
            fontFamily: 'var(--font-mono)',
          }}
        >
          ✕ hide
        </button>
      </div>

      {/* Panel body */}
      <div style={{ padding: '14px 16px', display: 'flex', flexDirection: 'column', gap: 14 }}>
        {/* Winner banner always visible */}
        <WinnerBanner result={result} />

        {/* Tab content */}
        {tab === 'summary' ? (
          <ScoreSummary result={result} />
        ) : tab === 'detail' ? (
          <ScoreDetail result={result} />
        ) : (
          <SpeakerScores result={result} />
        )}

        {/* Full explanation always at bottom, collapsed by default */}
        {fullExplanation && (
          <ExplanationPanel explanation={fullExplanation} />
        )}
      </div>
    </div>
  )
}
