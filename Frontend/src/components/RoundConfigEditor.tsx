// ============================================================================
// RoundConfigEditor.tsx — Editor for round-config.json + stock case library
// ============================================================================

import { useState, useEffect } from 'react'
import { configApi, ApiError } from '@/api/client'
import type { RoundConfig, StockCase, DefaultEnrichment, BlueprintArgument } from '@/types/config'
import type { Side, EvidenceQuality, ImpactMagnitude } from '@/types/domain'
import { Section, SaveBar, TextInput, Select, FieldLabel } from './formControls'
import { Badge, Button } from './ui'

// ── Stock case card ───────────────────────────────────────────────────────────

function StockCaseCard({
  sc,
  onDelete,
}: {
  sc: StockCase
  onDelete?: () => void
}) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div style={{
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--radius-md)',
      overflow: 'hidden',
      marginBottom: 6,
    }}>
      <div
        onClick={() => setExpanded(e => !e)}
        style={{
          display: 'flex', alignItems: 'center', gap: 8,
          padding: '8px 12px', cursor: 'pointer',
          background: expanded ? 'var(--bg-elevated)' : 'var(--bg-surface)',
          userSelect: 'none',
        }}
      >
        <span style={{ color: 'var(--text-muted)', fontSize: 10, fontFamily: 'var(--font-mono)' }}>
          {expanded ? '▼' : '▶'}
        </span>
        <Badge variant={sc.side === 'AFF' ? 'aff' : 'neg'}>{sc.side}</Badge>
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)' }}>
          {sc.stockIssueTag}
        </span>
        <span style={{ fontSize: 'var(--text-sm)', color: 'var(--text-secondary)', flex: 1 }}>
          {sc.label}
        </span>
        <Badge variant="neutral" style={{ fontSize: 9, opacity: 0.6 }}>
          {sc.source}
        </Badge>
        {onDelete && (
          <button
            onClick={e => { e.stopPropagation(); onDelete() }}
            style={{ background: 'none', border: 'none', color: 'var(--neg)', cursor: 'pointer', fontSize: 13, lineHeight: 1 }}
          >✕</button>
        )}
      </div>

      {expanded && (
        <div style={{ padding: '10px 14px', borderTop: '1px solid var(--border-subtle)', background: 'var(--bg-overlay)' }}>
          {sc.blueprintArgument?.claim && (
            <div style={{ marginBottom: 6 }}>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Claim</span>
              <p style={{ margin: '3px 0 0', fontSize: 'var(--text-sm)', color: 'var(--text-secondary)' }}>{sc.blueprintArgument.claim}</p>
            </div>
          )}
          {sc.blueprintArgument?.reasoning && (
            <div style={{ marginBottom: 6 }}>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Reasoning</span>
              <p style={{ margin: '3px 0 0', fontSize: 'var(--text-sm)', color: 'var(--text-secondary)' }}>{sc.blueprintArgument.reasoning}</p>
            </div>
          )}
          {sc.blueprintArgument?.impact && (
            <div style={{ marginBottom: 6 }}>
              <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Impact</span>
              <p style={{ margin: '3px 0 0', fontSize: 'var(--text-sm)', color: 'var(--text-secondary)' }}>{sc.blueprintArgument.impact}</p>
            </div>
          )}
          {sc.defaultEnrichment && (
            <div style={{ display: 'flex', gap: 10, marginTop: 8, flexWrap: 'wrap' }}>
              {sc.defaultEnrichment.evidenceQuality && (
                <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-disabled)' }}>
                  Evidence: {sc.defaultEnrichment.evidenceQuality}
                </span>
              )}
              {sc.defaultEnrichment.impactMagnitude && (
                <span style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-disabled)' }}>
                  Impact: {sc.defaultEnrichment.impactMagnitude}
                </span>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ── Add custom stock case form ────────────────────────────────────────────────

const SIDE_OPTS: { value: Side; label: string }[] = [
  { value: 'AFF', label: 'AFF' },
  { value: 'NEG', label: 'NEG' },
]

const EQ_OPTS: { value: EvidenceQuality; label: string }[] = [
  { value: 'PeerReviewed', label: 'Peer Reviewed' },
  { value: 'ExpertOpinion', label: 'Expert Opinion' },
  { value: 'NewsSource', label: 'News Source' },
  { value: 'Anecdotal', label: 'Anecdotal' },
  { value: 'Unverified', label: 'Unverified' },
]

const IM_OPTS: { value: ImpactMagnitude; label: string }[] = [
  { value: 'Existential', label: 'Existential' },
  { value: 'Catastrophic', label: 'Catastrophic' },
  { value: 'Significant', label: 'Significant' },
  { value: 'Minor', label: 'Minor' },
  { value: 'Negligible', label: 'Negligible' },
]

const BLANK_FORM = {
  label: '', stockIssueTag: '', side: 'AFF' as Side,
  claim: '', reasoning: '', impact: '', evidenceSource: '',
  evidenceQuality: 'ExpertOpinion' as EvidenceQuality,
  impactMagnitude: 'Significant' as ImpactMagnitude,
}

function AddCaseForm({ issueOptions, onAdded }: { issueOptions: string[]; onAdded: () => void }) {
  const [form, setForm] = useState(BLANK_FORM)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const issueSelectorOpts = issueOptions.map(id => ({ value: id, label: id }))

  async function handleAdd() {
    if (!form.label.trim() || !form.stockIssueTag.trim()) {
      setError('Label and Stock Issue Tag are required.')
      return
    }
    setSaving(true); setError(null)
    try {
      const sc: Omit<StockCase, 'source'> = {
        stockCaseId: `user-${Date.now()}`,
        label: form.label,
        stockIssueTag: form.stockIssueTag,
        side: form.side,
        defaultEnrichment: {
          evidenceQuality: form.evidenceQuality,
          impactMagnitude: form.impactMagnitude,
          fallacies: [],
        } as DefaultEnrichment,
        blueprintArgument: {
          claim: form.claim || null,
          reasoning: form.reasoning || null,
          impact: form.impact || null,
          evidenceSource: form.evidenceSource || null,
        } as BlueprintArgument,
      }
      await configApi.addStockCase(sc)
      setForm(BLANK_FORM)
      onAdded()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to add stock case')
    } finally { setSaving(false) }
  }

  return (
    <div style={{
      border: '1px solid var(--border-default)',
      borderRadius: 'var(--radius-lg)',
      padding: '16px 18px',
      background: 'var(--bg-overlay)',
    }}>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 14 }}>
        Add Custom Stock Case
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 140px 100px', gap: 12, marginBottom: 12 }}>
        <div>
          <FieldLabel>Label</FieldLabel>
          <TextInput value={form.label} onChange={v => setForm(f => ({ ...f, label: v }))} placeholder="Harms — AI Safety (Custom)" />
        </div>
        <div>
          <FieldLabel>Issue Tag</FieldLabel>
          {issueSelectorOpts.length > 0 ? (
            <Select
              value={form.stockIssueTag || (issueSelectorOpts[0]?.value ?? '')}
              options={issueSelectorOpts}
              onChange={v => setForm(f => ({ ...f, stockIssueTag: v }))}
              width="100%"
            />
          ) : (
            <TextInput value={form.stockIssueTag} onChange={v => setForm(f => ({ ...f, stockIssueTag: v }))} placeholder="Harms" mono />
          )}
        </div>
        <div>
          <FieldLabel>Side</FieldLabel>
          <Select<Side> value={form.side} options={SIDE_OPTS} onChange={v => setForm(f => ({ ...f, side: v }))} width="100%" />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 }}>
        <div>
          <FieldLabel>Evidence Quality (default)</FieldLabel>
          <Select<EvidenceQuality> value={form.evidenceQuality} options={EQ_OPTS} onChange={v => setForm(f => ({ ...f, evidenceQuality: v }))} width="100%" />
        </div>
        <div>
          <FieldLabel>Impact Magnitude (default)</FieldLabel>
          <Select<ImpactMagnitude> value={form.impactMagnitude} options={IM_OPTS} onChange={v => setForm(f => ({ ...f, impactMagnitude: v }))} width="100%" />
        </div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, marginBottom: 14 }}>
        <div>
          <FieldLabel>Blueprint Claim</FieldLabel>
          <TextInput value={form.claim} onChange={v => setForm(f => ({ ...f, claim: v }))} placeholder="The plan fails to address…" multiline rows={2} />
        </div>
        <div>
          <FieldLabel>Blueprint Reasoning</FieldLabel>
          <TextInput value={form.reasoning} onChange={v => setForm(f => ({ ...f, reasoning: v }))} placeholder="Because…" multiline rows={2} />
        </div>
        <div>
          <FieldLabel>Blueprint Impact</FieldLabel>
          <TextInput value={form.impact} onChange={v => setForm(f => ({ ...f, impact: v }))} placeholder="Therefore…" multiline rows={2} />
        </div>
        <div>
          <FieldLabel>Evidence Source</FieldLabel>
          <TextInput value={form.evidenceSource} onChange={v => setForm(f => ({ ...f, evidenceSource: v }))} placeholder="Author (Year)" />
        </div>
      </div>

      {error && (
        <div style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--neg-text)', marginBottom: 10 }}>
          {error}
        </div>
      )}

      <Button variant="secondary" size="sm" onClick={handleAdd} loading={saving} disabled={saving}>
        Add Stock Case
      </Button>
    </div>
  )
}

// ── Main component ────────────────────────────────────────────────────────────

export function RoundConfigEditor() {
  const [config, setConfig] = useState<RoundConfig | null>(null)
  const [original, setOriginal] = useState<RoundConfig | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tab, setTab] = useState<'library' | 'add'>('library')

  function loadConfig() {
    configApi.getRound()
      .then(c => { setConfig(c); setOriginal(c) })
      .catch(e => setError(e instanceof ApiError ? e.message : 'Failed to load round config'))
  }

  useEffect(() => { loadConfig() }, [])

  const dirty = JSON.stringify(config) !== JSON.stringify(original)

  function update(updater: (c: RoundConfig) => RoundConfig) {
    setConfig(c => c ? updater(c) : c)
    setSaved(false)
  }

  async function handleSave() {
    if (!config) return
    setSaving(true); setError(null)
    try {
      await configApi.saveRound(config)
      setOriginal(config); setSaved(true)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Save failed')
    } finally { setSaving(false) }
  }

  async function handleDeleteCase(id: string) {
    try {
      await configApi.deleteStockCase(id)
      loadConfig()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Delete failed')
    }
  }

  function handleReset() { setConfig(original); setSaved(false); setError(null) }

  if (!config) return (
    <div style={{ padding: 'var(--space-6)', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' }}>
      {error ? `Error: ${error}` : 'Loading…'}
    </div>
  )

  const allCases = [...(config.stockCaseLibrary ?? []), ...(config.userStockCases ?? [])]
  const systemCases = allCases.filter(sc => sc.source === 'system')
  const userCases = allCases.filter(sc => sc.source === 'user')
  const issueOptions = [...new Set(allCases.map(sc => sc.stockIssueTag))]

  const TabBtn = ({ id, label }: { id: 'library' | 'add'; label: string }) => (
    <button
      onClick={() => setTab(id)}
      style={{
        background: 'none', border: 'none', padding: '5px 10px',
        fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)',
        color: tab === id ? 'var(--text-primary)' : 'var(--text-muted)',
        borderBottom: tab === id ? '2px solid var(--accent)' : '2px solid transparent',
        cursor: 'pointer', marginBottom: -1, textTransform: 'uppercase', letterSpacing: '0.05em',
      }}
    >
      {label}
    </button>
  )

  return (
    <div>
      {/* Round identity */}
      <Section title="Round">
        <div style={{ display: 'grid', gridTemplateColumns: '140px 1fr', gap: 12, marginBottom: 12 }}>
          <div>
            <FieldLabel>Round ID</FieldLabel>
            <TextInput value={config.roundId} onChange={v => update(c => ({ ...c, roundId: v }))} mono />
          </div>
          <div>
            <FieldLabel>Format ID</FieldLabel>
            <TextInput value={config.formatId} onChange={v => update(c => ({ ...c, formatId: v }))} mono />
          </div>
        </div>
        <div>
          <FieldLabel hint="Displayed in the flow sheet header">Motion</FieldLabel>
          <TextInput
            value={config.motion}
            onChange={v => update(c => ({ ...c, motion: v }))}
            placeholder="This House believes the USFG should substantially increase federal regulation of AI"
            multiline rows={2}
          />
        </div>
      </Section>

      {/* Stock Case Library */}
      <Section title="Stock Case Library">
        {/* Library/Add tabs */}
        <div style={{ display: 'flex', gap: 0, borderBottom: '1px solid var(--border-default)', marginBottom: 14 }}>
          <TabBtn id="library" label={`Library (${allCases.length})`} />
          <TabBtn id="add" label="+ Add Custom" />
        </div>

        {tab === 'library' && (
          <div>
            {allCases.length === 0 && (
              <div style={{ padding: 'var(--space-5)', color: 'var(--text-muted)', fontSize: 'var(--text-sm)', textAlign: 'center' }}>
                No stock cases loaded. Add a custom one below.
              </div>
            )}

            {systemCases.length > 0 && (
              <div style={{ marginBottom: 16 }}>
                <div style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
                  System ({systemCases.length})
                </div>
                {systemCases.map(sc => (
                  <StockCaseCard key={sc.stockCaseId} sc={sc} />
                ))}
              </div>
            )}

            {userCases.length > 0 && (
              <div>
                <div style={{ fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
                  Custom ({userCases.length})
                </div>
                {userCases.map(sc => (
                  <StockCaseCard
                    key={sc.stockCaseId}
                    sc={sc}
                    onDelete={() => handleDeleteCase(sc.stockCaseId)}
                  />
                ))}
              </div>
            )}
          </div>
        )}

        {tab === 'add' && (
          <AddCaseForm issueOptions={issueOptions} onAdded={() => { loadConfig(); setTab('library') }} />
        )}
      </Section>

      <SaveBar dirty={dirty} saving={saving} saved={saved} error={error} onSave={handleSave} onReset={handleReset} />
    </div>
  )
}
