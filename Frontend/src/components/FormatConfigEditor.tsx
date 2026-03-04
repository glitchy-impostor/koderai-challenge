// ============================================================================
// FormatConfigEditor.tsx — Editor for format-config.json
// ============================================================================

import { useState, useEffect } from 'react'
import { configApi, ApiError } from '@/api/client'
import type { FormatConfig, StockIssue, SpeechDefinition, DropRule } from '@/types/config'
import { Section, SaveBar, TextInput, Toggle, NumberInput, Select, FieldLabel } from './formControls'

const SIDE_OPTIONS: { value: ObligatedSide; label: string }[] = [
  { value: 'AFF', label: 'AFF' },
  { value: 'NEG', label: 'NEG' },
  { value: 'BOTH', label: 'BOTH' },
  { value: 'NEITHER', label: 'NEITHER' },
]

const SPEECH_TYPE_OPTIONS: { value: SpeechType; label: string }[] = [
  { value: 'Constructive', label: 'Constructive' },
  { value: 'Rebuttal', label: 'Rebuttal' },
  { value: 'CrossEx', label: 'CrossEx' },
]

type ObligatedSide = 'AFF' | 'NEG' | 'BOTH' | 'NEITHER'
type SpeechSide = 'AFF' | 'NEG' | 'CX'
type SpeechType = 'Constructive' | 'Rebuttal' | 'CrossEx'

export function FormatConfigEditor() {
  const [config, setConfig] = useState<FormatConfig | null>(null)
  const [original, setOriginal] = useState<FormatConfig | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    configApi.getFormat()
      .then(c => { setConfig(c); setOriginal(c) })
      .catch(e => setError(e instanceof ApiError ? e.message : 'Failed to load format config'))
  }, [])

  const dirty = JSON.stringify(config) !== JSON.stringify(original)

  function update(updater: (c: FormatConfig) => FormatConfig) {
    setConfig(c => c ? updater(c) : c)
    setSaved(false)
  }

  async function handleSave() {
    if (!config) return
    setSaving(true); setError(null)
    try {
      await configApi.saveFormat(config)
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
      {/* Format identity */}
      <Section title="Format Identity">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
          <div>
            <FieldLabel>Format ID</FieldLabel>
            <TextInput value={config.formatId} onChange={v => update(c => ({ ...c, formatId: v }))} mono />
          </div>
          <div>
            <FieldLabel>Format Name</FieldLabel>
            <TextInput value={config.formatName} onChange={v => update(c => ({ ...c, formatName: v }))} />
          </div>
        </div>
      </Section>

      {/* Stock Issues */}
      <Section title="Stock Issues">
        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 10 }}>
          <thead>
            <tr>
              {['ID', 'Label', 'Obligated Side', 'Hard Gate', ''].map((h, i) => (
                <th key={i} style={{
                  padding: '6px 10px', textAlign: 'left',
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
            {config.stockIssues.map((issue, i) => (
              <tr key={issue.id}>
                <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <TextInput
                    value={issue.id} mono
                    onChange={v => update(c => {
                      const updated = [...c.stockIssues]
                      updated[i] = { ...updated[i], id: v }
                      // Also update hardGateIssues if this id was there
                      const hg = c.hardGateIssues.map(h => h === issue.id ? v : h)
                      return { ...c, stockIssues: updated, hardGateIssues: hg }
                    })}
                  />
                </td>
                <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <TextInput value={issue.label} onChange={v => update(c => {
                    const updated = [...c.stockIssues]; updated[i] = { ...updated[i], label: v }
                    return { ...c, stockIssues: updated }
                  })} />
                </td>
                <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <Select<ObligatedSide>
                    value={issue.obligatedSide as ObligatedSide}
                    options={SIDE_OPTIONS}
                    onChange={v => update(c => {
                      const updated = [...c.stockIssues]; updated[i] = { ...updated[i], obligatedSide: v }
                      return { ...c, stockIssues: updated }
                    })}
                    width={110}
                  />
                </td>
                <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <Toggle
                    checked={config.hardGateIssues.includes(issue.id)}
                    onChange={checked => update(c => ({
                      ...c,
                      hardGateIssues: checked
                        ? [...c.hardGateIssues, issue.id]
                        : c.hardGateIssues.filter(h => h !== issue.id),
                    }))}
                  />
                </td>
                <td style={{ padding: '6px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <button
                    onClick={() => update(c => ({
                      ...c,
                      stockIssues: c.stockIssues.filter((_, j) => j !== i),
                      hardGateIssues: c.hardGateIssues.filter(h => h !== issue.id),
                    }))}
                    style={{ background: 'none', border: 'none', color: 'var(--neg)', cursor: 'pointer', fontSize: 14 }}
                  >✕</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <button
          onClick={() => update(c => ({
            ...c, stockIssues: [...c.stockIssues, { id: 'New', label: 'New Issue', obligatedSide: 'AFF' } as StockIssue]
          }))}
          style={{
            background: 'none', border: '1px dashed var(--border-default)',
            borderRadius: 'var(--radius-md)', padding: '5px 14px',
            color: 'var(--text-muted)', cursor: 'pointer', fontSize: 'var(--text-sm)',
          }}
        >
          + Add Issue
        </button>
      </Section>

      {/* Speech Order */}
      <Section title="Speech Order">
        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 10 }}>
          <thead>
            <tr>
              {['Speech ID', 'Side', 'Type', 'Time (s)', ''].map((h, i) => (
                <th key={i} style={{ padding: '6px 10px', textAlign: 'left', fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', borderBottom: '1px solid var(--border-default)' }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {config.speechOrder.map((sp, i) => (
              <tr key={i}>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <TextInput value={sp.speechId} mono onChange={v => update(c => {
                    const o = [...c.speechOrder]; o[i] = { ...o[i], speechId: v }; return { ...c, speechOrder: o }
                  })} />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <Select<SpeechSide>
                    value={sp.side as SpeechSide}
                    options={[{ value: 'AFF', label: 'AFF' }, { value: 'NEG', label: 'NEG' }, { value: 'CX', label: 'CX' }]}
                    onChange={v => update(c => {
                      const o = [...c.speechOrder]; o[i] = { ...o[i], side: v }; return { ...c, speechOrder: o }
                    })}
                    width={80}
                  />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <Select<SpeechType>
                    value={sp.type as SpeechType}
                    options={SPEECH_TYPE_OPTIONS}
                    onChange={v => update(c => {
                      const o = [...c.speechOrder]; o[i] = { ...o[i], type: v }; return { ...c, speechOrder: o }
                    })}
                    width={130}
                  />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <NumberInput value={sp.timeSeconds} min={0} step={30} onChange={v => update(c => {
                    const o = [...c.speechOrder]; o[i] = { ...o[i], timeSeconds: v }; return { ...c, speechOrder: o }
                  })} width={80} />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <button onClick={() => update(c => ({ ...c, speechOrder: c.speechOrder.filter((_, j) => j !== i) }))}
                    style={{ background: 'none', border: 'none', color: 'var(--neg)', cursor: 'pointer', fontSize: 14 }}>✕</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <button
          onClick={() => update(c => ({ ...c, speechOrder: [...c.speechOrder, { speechId: 'NEW', side: 'AFF', type: 'Constructive', timeSeconds: 480 } as SpeechDefinition] }))}
          style={{ background: 'none', border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)', padding: '5px 14px', color: 'var(--text-muted)', cursor: 'pointer', fontSize: 'var(--text-sm)' }}
        >
          + Add Speech
        </button>
      </Section>

      {/* Drop Rules */}
      <Section title="Drop Rules">
        <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 10 }}>
          <thead>
            <tr>
              {['Introduced In', 'Must Be Answered By', ''].map((h, i) => (
                <th key={i} style={{ padding: '6px 10px', textAlign: 'left', fontFamily: 'var(--font-mono)', fontSize: 9, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.06em', borderBottom: '1px solid var(--border-default)' }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {config.dropRules.map((rule, i) => (
              <tr key={i}>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <TextInput value={rule.argumentIntroducedIn} mono onChange={v => update(c => {
                    const r = [...c.dropRules]; r[i] = { ...r[i], argumentIntroducedIn: v }; return { ...c, dropRules: r }
                  })} />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <TextInput value={rule.mustBeAnsweredBy} mono onChange={v => update(c => {
                    const r = [...c.dropRules]; r[i] = { ...r[i], mustBeAnsweredBy: v }; return { ...c, dropRules: r }
                  })} />
                </td>
                <td style={{ padding: '5px 10px', borderBottom: '1px solid var(--border-subtle)' }}>
                  <button onClick={() => update(c => ({ ...c, dropRules: c.dropRules.filter((_, j) => j !== i) }))}
                    style={{ background: 'none', border: 'none', color: 'var(--neg)', cursor: 'pointer', fontSize: 14 }}>✕</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <button
          onClick={() => update(c => ({ ...c, dropRules: [...c.dropRules, { argumentIntroducedIn: '', mustBeAnsweredBy: '' } as DropRule] }))}
          style={{ background: 'none', border: '1px dashed var(--border-default)', borderRadius: 'var(--radius-md)', padding: '5px 14px', color: 'var(--text-muted)', cursor: 'pointer', fontSize: 'var(--text-sm)' }}
        >
          + Add Rule
        </button>
      </Section>

      <SaveBar dirty={dirty} saving={saving} saved={saved} error={error} onSave={handleSave} onReset={handleReset} />
    </div>
  )
}
