// ============================================================================
// DebateInput.tsx — Debate JSON input panel
// ============================================================================

import { useState, useRef } from 'react'
import type { Debate } from '@/types/domain'
import { Button } from './ui'

interface DebateInputProps {
  onLoad: (debate: Debate) => void
  loading: boolean
}

// ── Minimal validation ────────────────────────────────────────────────────────

function parseDebate(raw: string): { debate: Debate } | { error: string } {
  let parsed: unknown
  try {
    parsed = JSON.parse(raw)
  } catch (e) {
    return { error: `Invalid JSON: ${(e as Error).message}` }
  }

  // Accept either {debate: {...}} or the bare debate object
  const obj = parsed as Record<string, unknown>
  const debateObj = ('debate' in obj ? obj.debate : obj) as Record<string, unknown>

  if (!debateObj.debateId || typeof debateObj.debateId !== 'string') {
    return { error: 'Missing or invalid "debateId" field.' }
  }
  if (!debateObj.arguments || typeof debateObj.arguments !== 'object') {
    return { error: 'Missing "arguments" map.' }
  }
  if (!Array.isArray(debateObj.speeches)) {
    return { error: 'Missing "speeches" array.' }
  }

  return { debate: debateObj as unknown as Debate }
}

// ── Placeholder text ──────────────────────────────────────────────────────────

const PLACEHOLDER = `{
  "debate": {
    "debateId": "round-001",
    "roundId": "round-001",
    "teams": { ... },
    "speakers": [ ... ],
    "speeches": [ ... ],
    "arguments": { ... }
  }
}`

// ── Component ─────────────────────────────────────────────────────────────────

export function DebateInput({ onLoad, loading }: DebateInputProps) {
  const [text, setText] = useState('')
  const [parseError, setParseError] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  function handleLoad() {
    setParseError(null)
    if (!text.trim()) {
      setParseError('Paste a debate JSON first.')
      return
    }
    const result = parseDebate(text)
    if ('error' in result) {
      setParseError(result.error)
      return
    }
    onLoad(result.debate)
  }

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = ev => {
      const content = ev.target?.result as string
      setText(content)
      setParseError(null)
    }
    reader.readAsText(file)
  }

  function loadSample() {
    fetch('/sample-debate.json')
      .then(r => r.ok ? r.text() : Promise.reject(new Error('not found')))
      .then(raw => { setText(raw); setParseError(null) })
      .catch(() => {
        // Fall back to embedded minimal sample
        setText(JSON.stringify(SAMPLE_DEBATE, null, 2))
        setParseError(null)
      })
  }

  return (
    <div style={{ maxWidth: 760 }}>
      {/* Title */}
      <div style={{ marginBottom: 'var(--space-5)' }}>
        <h2 style={{
          fontSize: 'var(--text-xl)',
          fontWeight: 500,
          letterSpacing: '-0.02em',
          marginBottom: 4,
        }}>
          Load a Debate
        </h2>
        <p style={{ fontSize: 'var(--text-sm)', color: 'var(--text-muted)' }}>
          Paste the structured debate JSON, upload a file, or start with the built-in sample.
        </p>
      </div>

      {/* Action row */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
        <Button variant="ghost" size="sm" onClick={loadSample}>
          ↓ Load Sample
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => fileRef.current?.click()}
        >
          ↑ Upload JSON
        </Button>
        <input
          ref={fileRef}
          type="file"
          accept=".json"
          style={{ display: 'none' }}
          onChange={handleFile}
        />
      </div>

      {/* Textarea */}
      <textarea
        value={text}
        onChange={e => { setText(e.target.value); setParseError(null) }}
        placeholder={PLACEHOLDER}
        spellCheck={false}
        style={{
          width: '100%',
          minHeight: 280,
          padding: '12px 14px',
          fontFamily: 'var(--font-mono)',
          fontSize: 'var(--text-xs)',
          color: 'var(--text-secondary)',
          background: 'var(--bg-surface)',
          border: `1px solid ${parseError ? 'var(--neg)' : 'var(--border-default)'}`,
          borderRadius: 'var(--radius-md)',
          resize: 'vertical',
          outline: 'none',
          lineHeight: 1.6,
          transition: 'border-color var(--transition-fast)',
        }}
      />

      {/* Error */}
      {parseError && (
        <div style={{
          marginTop: 8,
          padding: '8px 12px',
          background: 'var(--neg-bg)',
          border: '1px solid var(--neg-dim)',
          borderRadius: 'var(--radius-sm)',
          color: 'var(--neg-text)',
          fontSize: 'var(--text-sm)',
          fontFamily: 'var(--font-mono)',
        }}>
          {parseError}
        </div>
      )}

      {/* Load button */}
      <div style={{ marginTop: 14 }}>
        <Button
          variant="primary"
          size="md"
          onClick={handleLoad}
          loading={loading}
          disabled={loading}
        >
          {loading ? 'Building flow graph…' : 'Build Flow Graph →'}
        </Button>
      </div>
    </div>
  )
}

// ── Embedded minimal sample (AI regulation debate) ────────────────────────────
// Full sample available at /sample-debate.json (served from public/)

const SAMPLE_DEBATE: { debate: Debate } = {
  debate: {
    debateId: 'round-001',
    roundId: 'round-001',
    teams: {
      AFF: { teamId: 'aff', side: 'AFF', speakerIds: ['spk-1', 'spk-2'] },
      NEG: { teamId: 'neg', side: 'NEG', speakerIds: ['spk-3', 'spk-4'] },
    },
    speakers: [
      { speakerId: 'spk-1', name: 'Alice Chen',    side: 'AFF' },
      { speakerId: 'spk-2', name: 'Ben Nakamura',  side: 'AFF' },
      { speakerId: 'spk-3', name: 'Carol Okafor',  side: 'NEG' },
      { speakerId: 'spk-4', name: 'David Torres',  side: 'NEG' },
    ],
    speeches: [
      { speechId: '1AC', speakerId: 'spk-1', side: 'AFF', timeAllocatedSeconds: 480, timeUsedSeconds: 471, argumentIds: ['aff-t-1', 'aff-inh-1', 'aff-h-1', 'aff-h-2', 'aff-s-1'] },
      { speechId: '1NC', speakerId: 'spk-3', side: 'NEG', timeAllocatedSeconds: 480, timeUsedSeconds: 478, argumentIds: ['neg-t-1', 'neg-h-1', 'neg-h-2', 'neg-s-1'] },
      { speechId: '2AC', speakerId: 'spk-2', side: 'AFF', timeAllocatedSeconds: 480, timeUsedSeconds: 465, argumentIds: ['aff-t-ext', 'aff-h-3', 'aff-s-2'] },
      { speechId: '2NC', speakerId: 'spk-4', side: 'NEG', timeAllocatedSeconds: 480, timeUsedSeconds: 480, argumentIds: ['neg-h-3', 'neg-s-2'] },
      { speechId: '1NR', speakerId: 'spk-3', side: 'NEG', timeAllocatedSeconds: 300, timeUsedSeconds: 298, argumentIds: ['neg-h-ext', 'neg-s-ext'] },
      { speechId: '1AR', speakerId: 'spk-1', side: 'AFF', timeAllocatedSeconds: 300, timeUsedSeconds: 295, argumentIds: ['aff-h-ext', 'aff-s-ext'] },
      { speechId: '2NR', speakerId: 'spk-4', side: 'NEG', timeAllocatedSeconds: 300, timeUsedSeconds: 285, argumentIds: ['neg-final'] },
      { speechId: '2AR', speakerId: 'spk-2', side: 'AFF', timeAllocatedSeconds: 300, timeUsedSeconds: 299, argumentIds: ['aff-final'] },
    ],
    arguments: {
      'aff-t-1':   { argumentId: 'aff-t-1',   speechId: '1AC', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Topicality', stockCaseId: null, rebuttalTargetIds: [], core: { claim: "AFF's federal AI regulatory agency meets 'substantially increase' threshold", reasoning: 'New agency with mandatory auditing = categorical increase, analogous to FDA/FAA creation', impact: 'Plan falls within resolution; topicality objections fail on textual and functional grounds', evidenceSource: 'Prior USFG regulatory precedent (FDA 1906, FAA 1958)' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-inh-1': { argumentId: 'aff-inh-1', speechId: '1AC', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Inherency', stockCaseId: 'sc-inh-regulatory-gap', rebuttalTargetIds: [], core: { claim: 'Status quo lacks unified federal AI regulatory framework', reasoning: 'Current oversight fragmented across FTC, NIST voluntary guidelines, sector-specific rules', impact: 'Structural regulatory gap means harms proliferate unchecked across industries', evidenceSource: 'GAO Report on AI Regulation, 2023' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-h-1':   { argumentId: 'aff-h-1',   speechId: '1AC', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Harms', stockCaseId: 'sc-harms-bias', rebuttalTargetIds: [], core: { claim: 'Unregulated AI causes systematic algorithmic bias in hiring, lending, criminal justice', reasoning: 'MIT/Stanford studies: 34% higher false positive rates for facial recognition on darker-skinned individuals', impact: 'Millions face discriminatory outcomes from opaque AI systems with no recourse', evidenceSource: 'Buolamwini & Gebru (2018), Stanford HAI 2023' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-h-2':   { argumentId: 'aff-h-2',   speechId: '1AC', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Harms', stockCaseId: 'sc-harms-safety', rebuttalTargetIds: [], core: { claim: 'AI without safety standards risks catastrophic failures in critical infrastructure', reasoning: 'Competitive pressure drives labs to deploy before safety benchmarks met', impact: 'Autonomous AI in power grids, financial markets, defense creates systemic cascading failure risk', evidenceSource: 'Center for AI Safety (2023); Hinton Congressional Testimony' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Catastrophic', fallacies: [], argumentStrength: null, status: null } },
      'aff-s-1':   { argumentId: 'aff-s-1',   speechId: '1AC', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Solvency', stockCaseId: 'sc-solv-agency', rebuttalTargetIds: [], core: { claim: 'Federal AI regulatory agency with mandatory auditing solves identified harms', reasoning: 'Centralized technical expertise + mandatory transparency = consistent enforcement; FDA/FAA precedent', impact: 'Structured oversight reduces systemic risks by 60–80%', evidenceSource: 'FDA drug safety outcomes; FAA aviation safety record post-1958' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'neg-t-1':   { argumentId: 'neg-t-1',   speechId: '1NC', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Topicality', stockCaseId: null, rebuttalTargetIds: ['aff-t-1'], core: { claim: "AFF's agency does not 'substantially' increase regulation — it merely consolidates existing rules", reasoning: "USFG already regulates AI via FTC, NIST, sector-specific rules — AFF reorganizes, doesn't increase", impact: 'AFF is non-topical; plan does not meet the resolution threshold', evidenceSource: "Administrative law precedent: reorganization ≠ new regulatory authority" }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: ['StrawMan'], argumentStrength: null, status: null } },
      'neg-h-1':   { argumentId: 'neg-h-1',   speechId: '1NC', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['aff-h-1'], core: { claim: 'Algorithmic bias is better solved by industry self-regulation than federal mandate', reasoning: 'Tech companies have stronger incentives (PR, liability) and faster iteration cycles than federal agencies', impact: 'Regulation ossifies good-enough solutions and blocks better ones', evidenceSource: 'Brookings Institution, "AI Governance" 2022' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'neg-h-2':   { argumentId: 'neg-h-2',   speechId: '1NC', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: [], core: { claim: 'Heavy AI regulation stifles innovation, causing economic harm exceeding AI risks', reasoning: 'EU AI Act compliance costs estimated at $10B–100B; innovation migrates to non-regulated jurisdictions', impact: 'Net harm: US cedes AI leadership to China, reducing safety globally', evidenceSource: 'European Parliament Impact Assessment (2023)' }, enrichment: { evidenceQuality: 'NewsSource', impactMagnitude: 'Catastrophic', fallacies: [], argumentStrength: null, status: null } },
      'neg-s-1':   { argumentId: 'neg-s-1',   speechId: '1NC', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['aff-s-1'], core: { claim: 'Federal agency lacks technical expertise to regulate rapidly evolving AI', reasoning: 'FDA/FAA analogy fails — drug/aviation pace is decades slower than AI development', impact: "AFF's plan generates compliance theater without substantive risk reduction", evidenceSource: 'GAO Report on Federal IT Capacity, 2022' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: ['FalseDichotomy'], argumentStrength: null, status: null } },
      'aff-t-ext': { argumentId: 'aff-t-ext', speechId: '2AC', speakerId: 'spk-2', side: 'AFF', stockIssueTag: 'Topicality', stockCaseId: null, rebuttalTargetIds: ['neg-t-1'], core: { claim: 'NEG conflates consolidation with non-topicality — both can be substantial increases', reasoning: 'FDA creation in 1906 also consolidated; that was clearly substantial regulatory increase', impact: 'NEG topicality shell fails; AFF clearly within resolution', evidenceSource: null }, enrichment: { evidenceQuality: null, impactMagnitude: null, fallacies: [], argumentStrength: null, status: null } },
      'aff-h-3':   { argumentId: 'aff-h-3',   speechId: '2AC', speakerId: 'spk-2', side: 'AFF', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['neg-h-1'], core: { claim: 'Self-regulation fails — industry bias incentives are opposite to self-correction', reasoning: 'Companies profit from biased outputs (cheaper models, discriminatory redlining)—no incentive to fix', impact: 'Only mandatory auditing creates accountability; self-regulation is proven ineffective', evidenceSource: 'FTC AI Report (2022)' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-s-2':   { argumentId: 'aff-s-2',   speechId: '2AC', speakerId: 'spk-2', side: 'AFF', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['neg-s-1'], core: { claim: 'Agency solves even without perfect technical expertise via transparency mandates', reasoning: 'Mandatory disclosure requirements create accountability without requiring regulators to understand every model', impact: 'Market actors and researchers can identify risks once disclosure is mandated', evidenceSource: 'SEC disclosure model precedent' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'neg-h-3':   { argumentId: 'neg-h-3',   speechId: '2NC', speakerId: 'spk-4', side: 'NEG', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['aff-h-3'], core: { claim: 'FTC has existing authority to address algorithmic bias — AFF is redundant', reasoning: 'FTC Act §5 already covers unfair/deceptive AI practices; three enforcement actions in 2023', impact: 'No uniqueness for AFF harms — FTC solves without new agency', evidenceSource: 'FTC v. Amazon (2023), FTC AI Policy Statement' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Minor', fallacies: [], argumentStrength: null, status: null } },
      'neg-s-2':   { argumentId: 'neg-s-2',   speechId: '2NC', speakerId: 'spk-4', side: 'NEG', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['aff-s-2'], core: { claim: 'Transparency mandates alone cannot solve safety risks in closed-source foundation models', reasoning: 'OpenAI, Anthropic operate closed models — disclosure requires access to weights, not just documentation', impact: 'AFF solvency gap: cannot mandate transparency for national security AI systems', evidenceSource: 'CSET Georgetown Report (2023)' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'neg-h-ext': { argumentId: 'neg-h-ext', speechId: '1NR', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['aff-h-3'], core: { claim: 'FTC argument is a complete solvency turn — AFF duplicates existing authority', reasoning: 'AFF has not answered that FTC already has and is using §5 authority for AI', impact: 'At best AFF is redundant; at worst AFF creates conflicting regulatory regimes', evidenceSource: null }, enrichment: { evidenceQuality: null, impactMagnitude: null, fallacies: [], argumentStrength: null, status: null } },
      'neg-s-ext': { argumentId: 'neg-s-ext', speechId: '1NR', speakerId: 'spk-3', side: 'NEG', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['aff-s-2'], core: { claim: 'Extend disclosure solvency gap — 1AR must explain how AFF accesses closed-source weights', reasoning: 'No answer to CSET argument that closed models prevent meaningful transparency auditing', impact: 'AFF cannot solve its own mechanism', evidenceSource: null }, enrichment: { evidenceQuality: null, impactMagnitude: null, fallacies: [], argumentStrength: null, status: null } },
      'aff-h-ext': { argumentId: 'aff-h-ext', speechId: '1AR', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['neg-h-ext'], core: { claim: 'FTC §5 is insufficient — no rulemaking authority, only case-by-case enforcement', reasoning: 'FTC cannot promulgate binding industry-wide rules; must litigate each case for years', impact: 'Systemic bias requires systemic solution; FTC enforcement is inadequate', evidenceSource: 'FTC v. LabMD — rulemaking limits confirmed by courts' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-s-ext': { argumentId: 'aff-s-ext', speechId: '1AR', speakerId: 'spk-1', side: 'AFF', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['neg-s-ext'], core: { claim: 'AFF agency has subpoena power to compel access to closed models for safety audits', reasoning: 'Unlike FTC, AFF creates an agency with mandatory compliance power — analogous to NRC nuclear access', impact: 'Closed-source argument fails; agency authority overcomes opacity', evidenceSource: 'NRC inspection authority model' }, enrichment: { evidenceQuality: 'ExpertOpinion', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'neg-final': { argumentId: 'neg-final', speechId: '2NR', speakerId: 'spk-4', side: 'NEG', stockIssueTag: 'Solvency', stockCaseId: null, rebuttalTargetIds: ['aff-s-ext'], core: { claim: 'AFF NRC analogy fails — nuclear is physical; AI models are software IP with First Amendment implications', reasoning: 'Compelling access to model weights = compelled disclosure of source code; courts have found this protected speech', impact: 'AFF agency cannot constitutionally force access to closed AI models', evidenceSource: 'Bernstein v. DOJ (9th Cir. 1999) — source code as speech' }, enrichment: { evidenceQuality: 'PeerReviewed', impactMagnitude: 'Significant', fallacies: [], argumentStrength: null, status: null } },
      'aff-final': { argumentId: 'aff-final', speechId: '2AR', speakerId: 'spk-2', side: 'AFF', stockIssueTag: 'Harms', stockCaseId: null, rebuttalTargetIds: ['neg-h-ext'], core: { claim: 'Vote AFF on the bias harm — FTC rulemaking limitations are uncontested and decisive', reasoning: 'NEG 2NR dropped into Solvency; the Harms/FTC flow is conceded in the 2NR', impact: 'AFF wins on systemic bias harm — FTC §5 inadequacy proves we uniquely solve', evidenceSource: null }, enrichment: { evidenceQuality: null, impactMagnitude: null, fallacies: [], argumentStrength: null, status: null } },
    },
  },
}
