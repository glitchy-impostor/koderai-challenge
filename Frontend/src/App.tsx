import React from 'react'
import { BrowserRouter, Routes, Route, NavLink, Navigate } from 'react-router-dom'
import { FlowSheetPage } from '@/pages/FlowSheetPage'
import { SettingsPage } from '@/pages/SettingsPage'

// ── Top nav ───────────────────────────────────────────────────────────────────

function TopNav() {
  const navLinkStyle = ({ isActive }: { isActive: boolean }): React.CSSProperties => ({
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    padding: '6px 12px',
    fontSize: 'var(--text-sm)',
    fontFamily: 'var(--font-ui)',
    fontWeight: 500,
    color: isActive ? 'var(--text-primary)' : 'var(--text-muted)',
    background: isActive ? 'var(--bg-elevated)' : 'transparent',
    border: isActive ? '1px solid var(--border-default)' : '1px solid transparent',
    borderRadius: 'var(--radius-md)',
    textDecoration: 'none',
    transition: 'all var(--transition-fast)',
    cursor: 'pointer',
  })

  return (
    <header style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '0 var(--space-6)',
      height: 52,
      background: 'var(--bg-surface)',
      borderBottom: '1px solid var(--border-subtle)',
      position: 'sticky',
      top: 0,
      zIndex: 'var(--z-dropdown)',
      backdropFilter: 'blur(8px)',
    }}>
      {/* Wordmark */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)' }}>
        <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
          <rect x="1" y="1" width="18" height="18" rx="3" stroke="var(--accent)" strokeWidth="1.5" />
          <line x1="5" y1="7" x2="15" y2="7" stroke="var(--aff)" strokeWidth="1.5" strokeLinecap="round" />
          <line x1="5" y1="10" x2="12" y2="10" stroke="var(--neg)" strokeWidth="1.5" strokeLinecap="round" />
          <line x1="5" y1="13" x2="10" y2="13" stroke="var(--dropped)" strokeWidth="1.5" strokeLinecap="round" />
        </svg>
        <span style={{
          fontFamily: 'var(--font-mono)',
          fontWeight: 600,
          fontSize: 'var(--text-sm)',
          letterSpacing: '-0.01em',
          color: 'var(--text-primary)',
        }}>
          FLOW<span style={{ color: 'var(--accent)' }}>.</span>JUDGE
        </span>
      </div>

      {/* Nav links */}
      <nav style={{ display: 'flex', gap: 'var(--space-2)' }}>
        <NavLink to="/flow" style={navLinkStyle}>
          <FlowIcon /> Flow Sheet
        </NavLink>
        <NavLink to="/settings" style={navLinkStyle}>
          <SettingsIcon /> Settings
        </NavLink>
      </nav>
    </header>
  )
}

// ── App ───────────────────────────────────────────────────────────────────────

export function App() {
  return (
    <BrowserRouter>
      <TopNav />
      <main style={{ flex: 1 }}>
        <Routes>
          <Route path="/" element={<Navigate to="/flow" replace />} />
          <Route path="/flow" element={<FlowSheetPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}

// ── Icon helpers ──────────────────────────────────────────────────────────────

function FlowIcon() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <rect x="0.5" y="0.5" width="12" height="12" rx="2" stroke="currentColor" strokeWidth="1" />
      <line x1="3" y1="4" x2="10" y2="4" stroke="currentColor" strokeWidth="1" strokeLinecap="round" />
      <line x1="3" y1="6.5" x2="8" y2="6.5" stroke="currentColor" strokeWidth="1" strokeLinecap="round" />
      <line x1="3" y1="9" x2="6" y2="9" stroke="currentColor" strokeWidth="1" strokeLinecap="round" />
    </svg>
  )
}

function SettingsIcon() {
  return (
    <svg width="13" height="13" viewBox="0 0 13 13" fill="none">
      <circle cx="6.5" cy="6.5" r="1.5" stroke="currentColor" strokeWidth="1" />
      <circle cx="6.5" cy="6.5" r="5.5" stroke="currentColor" strokeWidth="1" strokeDasharray="2 1.5" />
    </svg>
  )
}
