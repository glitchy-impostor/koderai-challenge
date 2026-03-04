// ============================================================================
// ScoringConfigEditor.tsx — Editor for scoring-config.json
// ============================================================================

import { useState, useEffect } from 'react'
import { configApi, ApiError } from '@/api/client'
import type { ScoringConfig } from '@/types/config'
import { Section, SaveBar, NumberInput } from './formControls'

// ── Generic key-value number table ───────────────────────────────────────────

function KVNumberTable({
  data = {},
  onChange,
  keyLabel = 'Key',
  valueLabel = 'Value',
  step = 0.01,
  min = 0,
  max,
}: {
  data?: Record<string, number>
  onChange: (updated: Record<string, number>) => void
  keyLabel?: string
  valueLabel?: string
  step?: number
  min?: number
  max?: number
}) {
  // ensure data is an object (defensive)
  const safeData: Record<string, number> = data ?? {}
  const entries = Object.entries(safeData).sort((a, b) => a[0].localeCompare(b[0]))
  return (
    <table style={{ borderCollapse: 'collapse', width: '100%' }}>
      <thead>
        <tr>
          {[keyLabel, valueLabel].map((h, i) => (
            <th key={i} style={{
              padding: '5px 10px', textAlign: i === 0 ? 'left' : 'right',
              fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)',
              textTransform: 'uppercase', letterSpacing: '0.06em',
              borderBottom: '1px solid var(--border-default)',
            }}>
              {h}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {entries.map(([key, val]) => (
          <tr key={key}>
            <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-secondary)' }}>
              {key}
            </td>
            <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)', textAlign: 'right' }}>
              <NumberInput
                value={val}
                onChange={v => onChange({ ...data, [key]: v })}
                step={step} min={min} max={max}
                width={90}
              />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

// ── Single labelled number row ────────────────────────────────────────────────

function ScalarRow({ label, value, onChange, step = 0.01, min = 0, hint }: {
  label: string
  value: number
  onChange: (v: number) => void
  step?: number
  min?: number
  hint?: string
}) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '7px 0', borderBottom: '1px solid var(--border-subtle)',
    }}>
      <div>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: 'var(--text-secondary)' }}>
          {label}
        </span>
        {hint && (
          <div style={{ fontSize: 10, color: 'var(--text-disabled)', marginTop: 1 }}>{hint}</div>
        )}
      </div>
      <NumberInput value={value} onChange={onChange} step={step} min={min} width={90} />
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export function ScoringConfigEditor() {
  const [config, setConfig] = useState<ScoringConfig | null>(null)
  const [original, setOriginal] = useState<ScoringConfig | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
  configApi.getScoring()
    .then(c => {
      const normalized: ScoringConfig = {
        ...c,
        crossExamination: {
          perAdmissionScore: c.crossExamination?.perAdmissionScore ?? 0,
          perEvasionPenalty: c.crossExamination?.perEvasionPenalty ?? 0,
          timeEfficiencyWeight: c.crossExamination?.timeEfficiencyWeight ?? 0,
        },
        prepTime: {
          penaltyPerSecondOver: c.prepTime?.penaltyPerSecondOver ?? 0,
          maxPenalty: c.prepTime?.maxPenalty ?? 0,
        }
      }

      setConfig(normalized)
      setOriginal(normalized)
    })
    .catch(e =>
      setError(e instanceof ApiError ? e.message : 'Failed to load scoring config')
    )
}, [])

  const dirty = JSON.stringify(config) !== JSON.stringify(original)

  function update(updater: (c: ScoringConfig) => ScoringConfig) {
    setConfig(c => c ? updater(c) : c)
    setSaved(false)
  }

  async function handleSave() {
    if (!config) return
    setSaving(true); setError(null)
    try {
      await configApi.saveScoring(config)
      setOriginal(config); setSaved(true)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Save failed')
    } finally { setSaving(false) }
  }

  function handleReset() { setConfig(original); setSaved(false); setError(null) }

  if (!config) return (
    <div style={{ padding: 'var(--space-6)', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}>
      {error ? `Error: ${error}` : 'Loading…'}
    </div>
  )

  return (
    <div>
      {/* Rule Weights */}
      <Section title="Rule Weights">
        <KVNumberTable
          data={config.ruleWeights as unknown as Record<string, number>}
          onChange={v => update(c => ({ ...c, ruleWeights: v as unknown as typeof c.ruleWeights }))}
          keyLabel="Rule"
          valueLabel="Weight"
          step={0.05}
        />
      </Section>

      {/* Stock Issue Weights */}
      <Section title="Stock Issue Weights">
        <KVNumberTable
          data={config.stockIssueWeights}
          onChange={v => update(c => ({ ...c, stockIssueWeights: v }))}
          keyLabel="Issue"
          valueLabel="Weight"
          step={0.05}
          max={1}
        />
        <p style={{ fontSize: 10, color: 'var(--text-disabled)', marginTop: 8, fontFamily: 'var(--font-ui)' }}>
          Weights should sum to 1.0 for proportional scoring.
        </p>
      </Section>

      {/* Evidence Quality Multipliers */}
      <Section title="Evidence Quality Multipliers">
        <KVNumberTable
          data={config.evidenceQualityMultipliers as Record<string, number>}
          onChange={v => update(c => ({ ...c, evidenceQualityMultipliers: v as typeof c.evidenceQualityMultipliers }))}
          keyLabel="Quality"
          valueLabel="Multiplier"
          step={0.05}
        />
      </Section>

      {/* Impact Magnitude Base Scores */}
      <Section title="Impact Magnitude Base Scores">
        <KVNumberTable
          data={config.impactMagnitudeScores as Record<string, number>}
          onChange={v => update(c => ({ ...c, impactMagnitudeScores: v as typeof c.impactMagnitudeScores }))}
          keyLabel="Magnitude"
          valueLabel="Base Score"
          step={0.1}
          max={5}
        />
      </Section>

      {/* Fallacy Penalties */}
      <Section title="Fallacy Penalties">
        <KVNumberTable
          data={config.fallacyPenalties as Record<string, number>}
          onChange={v => update(c => ({ ...c, fallacyPenalties: v as typeof c.fallacyPenalties }))}
          keyLabel="Fallacy"
          valueLabel="Penalty"
          step={0.05}
        />
      </Section>

      {/* Scalars */}
      <Section title="Argument Scoring">
        <ScalarRow
          label="Dropped Argument Penalty"
          value={config.droppedArgumentPenalty}
          onChange={v => update(c => ({ ...c, droppedArgumentPenalty: v }))}
          hint="Applied as a multiplier on the dropped argument's score"
        />
      </Section>

      {/* Cross-Examination */}
      <Section title="Cross-Examination">
        <ScalarRow
          label="Per-Admission Score"
          value={config.crossExamination.perAdmissionScore}
          onChange={v => update(c => ({ ...c, crossExamination: { ...c.crossExamination, perAdmissionScore: v } }))}
        />
        <ScalarRow
          label="Per-Evasion Penalty"
          value={config.crossExamination.perEvasionPenalty}
          onChange={v => update(c => ({ ...c, crossExamination: { ...c.crossExamination, perEvasionPenalty: v } }))}
        />
        <ScalarRow
          label="Time Efficiency Weight"
          value={config.crossExamination.timeEfficiencyWeight}
          onChange={v => update(c => ({ ...c, crossExamination: { ...c.crossExamination, timeEfficiencyWeight: v } }))}
        />
      </Section>

      {/* Prep Time */}
      <Section title="Prep Time">
        <ScalarRow
          label="Penalty Per Second Over"
          value={config.prepTime.penaltyPerSecondOver}
          onChange={v => update(c => ({ ...c, prepTime: { ...c.prepTime, penaltyPerSecondOver: v } }))}
          step={0.001}
        />
        <ScalarRow
          label="Maximum Penalty"
          value={config.prepTime.maxPenalty}
          onChange={v => update(c => ({ ...c, prepTime: { ...c.prepTime, maxPenalty: v } }))}
        />
      </Section>

      {/* Tiebreaker priority (ordered list) */}
      <Section title="Tiebreaker Priority">
        <p style={{ fontSize: 'var(--text-xs)', color: 'var(--text-muted)', marginBottom: 10 }}>
          Issues are checked in order. First issue where one side leads breaks a tie.
        </p>
        {config.tiebreakerPriority.map((issue, i) => (
          <div key={i} style={{
            display: 'flex', alignItems: 'center', gap: 8,
            padding: '5px 0', borderBottom: '1px solid var(--border-subtle)',
          }}>
            <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', width: 20 }}>
              {i + 1}.
            </span>
            <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)', color: 'var(--text-secondary)', flex: 1 }}>
              {issue}
            </span>
            <button
              onClick={() => update(c => {
                const arr = [...c.tiebreakerPriority]
                if (i > 0) { [arr[i - 1], arr[i]] = [arr[i], arr[i - 1]] }
                return { ...c, tiebreakerPriority: arr }
              })}
              disabled={i === 0}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: i === 0 ? 'default' : 'pointer', opacity: i === 0 ? 0.3 : 1, fontSize: 14 }}
            >↑</button>
            <button
              onClick={() => update(c => {
                const arr = [...c.tiebreakerPriority]
                if (i < arr.length - 1) { [arr[i], arr[i + 1]] = [arr[i + 1], arr[i]] }
                return { ...c, tiebreakerPriority: arr }
              })}
              disabled={i === config.tiebreakerPriority.length - 1}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: i === config.tiebreakerPriority.length - 1 ? 'default' : 'pointer', opacity: i === config.tiebreakerPriority.length - 1 ? 0.3 : 1, fontSize: 14 }}
            >↓</button>
          </div>
        ))}
      </Section>

      <SaveBar dirty={dirty} saving={saving} saved={saved} error={error} onSave={handleSave} onReset={handleReset} />
    </div>
  )
}
