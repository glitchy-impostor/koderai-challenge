// ============================================================================
// WinnerBanner.tsx — Large winner declaration with score totals
// ============================================================================

import type { ScoringResult } from '@/types/scoring'

interface WinnerBannerProps {
  result: ScoringResult
}

function ScoreBar({ aff, neg }: { aff: number; neg: number }) {
  const total = aff + neg
  const affPct = total > 0 ? (aff / total) * 100 : 50
  const negPct = 100 - affPct

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6, width: '100%', maxWidth: 420 }}>
      {/* Bar */}
      <div style={{
        display: 'flex',
        height: 6,
        borderRadius: 99,
        overflow: 'hidden',
        background: 'var(--bg-overlay)',
      }}>
        <div style={{ width: `${affPct}%`, background: 'var(--aff)', transition: 'width 0.6s ease' }} />
        <div style={{ width: `${negPct}%`, background: 'var(--neg)', transition: 'width 0.6s ease' }} />
      </div>
      {/* Labels */}
      <div style={{ display: 'flex', justifyContent: 'space-between' }}>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--aff-text)' }}>
          AFF {aff.toFixed(2)}
        </span>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--neg-text)' }}>
          NEG {neg.toFixed(2)}
        </span>
      </div>
    </div>
  )
}

export function WinnerBanner({ result }: WinnerBannerProps) {
  const isAff = result.winner === 'AFF'
  const winColor     = isAff ? 'var(--aff)'      : 'var(--neg)'
  const winColorText = isAff ? 'var(--aff-text)'  : 'var(--neg-text)'
  const winBg        = isAff ? 'var(--aff-bg)'    : 'var(--neg-bg)'
  const winBorder    = isAff ? 'var(--aff-dim)'   : 'var(--neg-dim)'

  return (
    <div style={{
      background: winBg,
      border: `1px solid ${winBorder}`,
      borderLeft: `4px solid ${winColor}`,
      borderRadius: 'var(--radius-lg)',
      padding: '18px 24px',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      flexWrap: 'wrap',
      gap: 20,
    }}>
      {/* Left: winner declaration */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-3xl)',
            fontWeight: 700,
            color: winColorText,
            letterSpacing: '-0.03em',
            lineHeight: 1,
          }}>
            {result.winner}
          </span>
          <span style={{
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--text-lg)',
            fontWeight: 400,
            color: 'var(--text-secondary)',
            letterSpacing: '-0.01em',
          }}>
            wins
          </span>
          {result.decidedByHardGate && result.hardGateIssue && (
            <span style={{
              fontFamily: 'var(--font-mono)',
              fontSize: 'var(--text-xs)',
              color: 'var(--dropped)',
              background: 'var(--dropped-bg)',
              border: '1px solid var(--dropped-dim)',
              borderRadius: 'var(--radius-sm)',
              padding: '2px 8px',
              textTransform: 'uppercase',
              letterSpacing: '0.06em',
            }}>
              Hard Gate — {result.hardGateIssue}
            </span>
          )}
        </div>

        {/* Winner explanation — one sentence */}
        {result.winnerExplanation && (
          <p style={{
            fontSize: 'var(--text-sm)',
            color: 'var(--text-secondary)',
            lineHeight: 1.5,
            maxWidth: 560,
            margin: 0,
          }}>
            {result.winnerExplanation}
          </p>
        )}
      </div>

      {/* Right: score bar */}
      <ScoreBar aff={result.affTotalScore ?? 0} neg={result.negTotalScore ?? 0} />
    </div>
  )
}
