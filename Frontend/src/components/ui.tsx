import React from 'react'
import type { ButtonHTMLAttributes } from 'react'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  children: React.ReactNode
}

const variantStyles: Record<Variant, React.CSSProperties> = {
  primary: {
    background: 'var(--accent)',
    color: 'white',
    border: '1px solid transparent',
  },
  secondary: {
    background: 'var(--bg-elevated)',
    color: 'var(--text-primary)',
    border: '1px solid var(--border-default)',
  },
  ghost: {
    background: 'transparent',
    color: 'var(--text-secondary)',
    border: '1px solid transparent',
  },
  danger: {
    background: 'var(--neg-bg)',
    color: 'var(--neg-text)',
    border: '1px solid var(--neg-dim)',
  },
}

const sizeStyles: Record<Size, React.CSSProperties> = {
  sm: { padding: '4px 10px', fontSize: 'var(--text-sm)', borderRadius: 'var(--radius-sm)' },
  md: { padding: '7px 14px', fontSize: 'var(--text-base)', borderRadius: 'var(--radius-md)' },
  lg: { padding: '10px 20px', fontSize: 'var(--text-md)', borderRadius: 'var(--radius-md)' },
}

export function Button({
  variant = 'secondary',
  size = 'md',
  loading = false,
  disabled,
  children,
  style,
  ...props
}: ButtonProps) {
  return (
    <button
      disabled={disabled || loading}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: '6px',
        fontFamily: 'var(--font-ui)',
        fontWeight: 500,
        cursor: disabled || loading ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.5 : 1,
        transition: 'all var(--transition-fast)',
        whiteSpace: 'nowrap',
        ...variantStyles[variant],
        ...sizeStyles[size],
        ...style,
      }}
      {...props}
    >
      {loading && <Spinner size={14} />}
      {children}
    </button>
  )
}

// ── Spinner ───────────────────────────────────────────────────────────────────

interface SpinnerProps { size?: number; color?: string }

export function Spinner({ size = 16, color = 'currentColor' }: SpinnerProps) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      style={{ animation: 'spin 0.7s linear infinite', flexShrink: 0 }}
    >
      <style>{`@keyframes spin { from { transform: rotate(0deg) } to { transform: rotate(360deg) } }`}</style>
      <circle cx="12" cy="12" r="9" stroke={color} strokeWidth="2" opacity="0.2" />
      <path d="M12 3a9 9 0 0 1 9 9" stroke={color} strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

// ── Badge ─────────────────────────────────────────────────────────────────────

type BadgeVariant = 'aff' | 'neg' | 'cx' | 'dropped' | 'extended' | 'conceded' | 'neutral'

interface BadgeProps {
  variant?: BadgeVariant
  children: React.ReactNode
  style?: React.CSSProperties
}

const badgeVariantStyles: Record<BadgeVariant, React.CSSProperties> = {
  aff:      { color: 'var(--aff-text)',      background: 'var(--aff-bg)',     border: '1px solid var(--aff-dim)' },
  neg:      { color: 'var(--neg-text)',      background: 'var(--neg-bg)',     border: '1px solid var(--neg-dim)' },
  cx:       { color: 'var(--text-secondary)', background: 'var(--bg-elevated)', border: '1px solid var(--border-default)' },
  dropped:  { color: 'var(--dropped)',       background: 'var(--dropped-bg)', border: '1px solid var(--dropped-dim)' },
  extended: { color: 'var(--extended)',      background: 'var(--extended-bg)', border: '1px solid var(--extended-dim)' },
  conceded: { color: 'var(--conceded)',      background: 'var(--conceded-bg)', border: '1px solid #3d2a6e' },
  neutral:  { color: 'var(--text-muted)',    background: 'var(--bg-elevated)', border: '1px solid var(--border-subtle)' },
}

export function Badge({ variant = 'neutral', children, style }: BadgeProps) {
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        padding: '1px 6px',
        fontSize: 'var(--text-xs)',
        fontFamily: 'var(--font-mono)',
        fontWeight: 500,
        letterSpacing: '0.03em',
        borderRadius: 'var(--radius-sm)',
        whiteSpace: 'nowrap',
        textTransform: 'uppercase',
        ...badgeVariantStyles[variant],
        ...style,
      }}
    >
      {children}
    </span>
  )
}
