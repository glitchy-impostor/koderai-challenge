// ============================================================================
// formControls.tsx — Shared form primitives for settings editors
// ============================================================================

import { type ChangeEvent } from 'react'

// ── Label ─────────────────────────────────────────────────────────────────────

export function FieldLabel({ children, hint }: { children: React.ReactNode; hint?: string }) {
  return (
    <div style={{ marginBottom: 6 }}>
      <label style={{
        display: 'block',
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.06em',
      }}>
        {children}
      </label>
      {hint && (
        <span style={{ fontSize: 10, color: 'var(--text-disabled)', fontFamily: 'var(--font-ui)' }}>
          {hint}
        </span>
      )}
    </div>
  )
}

// ── TextInput ─────────────────────────────────────────────────────────────────

interface TextInputProps {
  value: string
  onChange: (v: string) => void
  placeholder?: string
  mono?: boolean
  multiline?: boolean
  rows?: number
  disabled?: boolean
}

const baseInputStyle: React.CSSProperties = {
  width: '100%',
  padding: '7px 10px',
  fontSize: 'var(--text-sm)',
  color: 'var(--text-primary)',
  background: 'var(--bg-overlay)',
  border: '1px solid var(--border-default)',
  borderRadius: 'var(--radius-md)',
  outline: 'none',
  transition: 'border-color var(--transition-fast)',
}

export function TextInput({ value, onChange, placeholder, mono, multiline, rows = 3, disabled }: TextInputProps) {
  const style = { ...baseInputStyle, fontFamily: mono ? 'var(--font-mono)' : 'var(--font-ui)' }
  if (multiline) {
    return (
      <textarea
        value={value}
        onChange={(e: ChangeEvent<HTMLTextAreaElement>) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={rows}
        disabled={disabled}
        style={{ ...style, resize: 'vertical', lineHeight: 1.6 }}
      />
    )
  }
  return (
    <input
      type="text"
      value={value}
      onChange={(e: ChangeEvent<HTMLInputElement>) => onChange(e.target.value)}
      placeholder={placeholder}
      disabled={disabled}
      style={style}
    />
  )
}

// ── NumberInput ───────────────────────────────────────────────────────────────

interface NumberInputProps {
  value: number
  onChange: (v: number) => void
  min?: number
  max?: number
  step?: number
  disabled?: boolean
  width?: number | string
}

export function NumberInput({ value, onChange, min = 0, max, step = 0.01, disabled, width = 90 }: NumberInputProps) {
  return (
    <input
      type="number"
      value={value}
      onChange={e => {
        const n = parseFloat(e.target.value)
        if (!isNaN(n)) onChange(n)
      }}
      min={min}
      max={max}
      step={step}
      disabled={disabled}
      style={{
        ...baseInputStyle,
        width,
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-sm)',
        textAlign: 'right',
      }}
    />
  )
}

// ── Select ────────────────────────────────────────────────────────────────────

interface SelectProps<T extends string> {
  value: T
  onChange: (v: T) => void
  options: { value: T; label: string }[]
  disabled?: boolean
  width?: number | string
}

export function Select<T extends string>({ value, onChange, options, disabled, width = 160 }: SelectProps<T>) {
  return (
    <select
      value={value}
      onChange={e => onChange(e.target.value as T)}
      disabled={disabled}
      style={{
        ...baseInputStyle,
        width,
        cursor: disabled ? 'not-allowed' : 'pointer',
        fontFamily: 'var(--font-mono)',
      }}
    >
      {options.map(o => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  )
}

// ── Toggle ────────────────────────────────────────────────────────────────────

interface ToggleProps {
  checked: boolean
  onChange: (v: boolean) => void
  label?: string
  disabled?: boolean
}

export function Toggle({ checked, onChange, label, disabled }: ToggleProps) {
  return (
    <label style={{
      display: 'inline-flex', alignItems: 'center', gap: 8,
      cursor: disabled ? 'not-allowed' : 'pointer', userSelect: 'none',
    }}>
      <div
        onClick={() => !disabled && onChange(!checked)}
        style={{
          width: 36, height: 20,
          borderRadius: 99,
          background: checked ? 'var(--accent)' : 'var(--bg-overlay)',
          border: `1px solid ${checked ? 'var(--accent)' : 'var(--border-default)'}`,
          position: 'relative',
          transition: 'all var(--transition-base)',
          flexShrink: 0,
        }}
      >
        <div style={{
          position: 'absolute',
          top: 2, left: checked ? 18 : 2,
          width: 14, height: 14,
          borderRadius: '50%',
          background: 'white',
          transition: 'left var(--transition-base)',
        }} />
      </div>
      {label && (
        <span style={{ fontSize: 'var(--text-sm)', color: 'var(--text-secondary)' }}>{label}</span>
      )}
    </label>
  )
}

// ── Section container ──────────────────────────────────────────────────────────

export function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: 28 }}>
      <div style={{
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-xs)',
        color: 'var(--text-muted)',
        textTransform: 'uppercase',
        letterSpacing: '0.08em',
        marginBottom: 12,
        paddingBottom: 6,
        borderBottom: '1px solid var(--border-subtle)',
      }}>
        {title}
      </div>
      {children}
    </div>
  )
}

// ── SaveBar — sticky save/reset footer for each editor ────────────────────────

interface SaveBarProps {
  dirty: boolean
  saving: boolean
  saved: boolean
  error: string | null
  onSave: () => void
  onReset: () => void
}

export function SaveBar({ dirty, saving, saved, error, onSave, onReset }: SaveBarProps) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 10,
      padding: '10px 0',
      borderTop: '1px solid var(--border-subtle)',
      marginTop: 16,
    }}>
      {error && (
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--neg-text)', flex: 1 }}>
          {error}
        </span>
      )}
      {saved && !dirty && !error && (
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 'var(--text-xs)', color: 'var(--extended)', flex: 1 }}>
          ✓ Saved
        </span>
      )}
      {!saved && !error && <span style={{ flex: 1 }} />}

      <button
        onClick={onReset}
        disabled={!dirty || saving}
        style={{
          background: 'none',
          border: '1px solid var(--border-default)',
          borderRadius: 'var(--radius-md)',
          padding: '6px 14px',
          fontFamily: 'var(--font-ui)',
          fontSize: 'var(--text-sm)',
          color: 'var(--text-muted)',
          cursor: !dirty || saving ? 'not-allowed' : 'pointer',
          opacity: !dirty ? 0.4 : 1,
        }}
      >
        Reset
      </button>

      <button
        onClick={onSave}
        disabled={!dirty || saving}
        style={{
          background: dirty ? 'var(--accent)' : 'var(--bg-overlay)',
          border: '1px solid transparent',
          borderRadius: 'var(--radius-md)',
          padding: '6px 20px',
          fontFamily: 'var(--font-ui)',
          fontSize: 'var(--text-sm)',
          fontWeight: 500,
          color: dirty ? 'white' : 'var(--text-muted)',
          cursor: !dirty || saving ? 'not-allowed' : 'pointer',
          opacity: !dirty ? 0.5 : 1,
          transition: 'all var(--transition-fast)',
        }}
      >
        {saving ? 'Saving…' : 'Save'}
      </button>
    </div>
  )
}

// Re-export React for consuming files
import React from 'react'
void React
