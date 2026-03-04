// ============================================================================
// gridModel.ts — Derives the grid data model from API responses
// ============================================================================

import type { Debate, FlowGraphResponse, FlowNode, FlowThread } from '@/types/domain'
import type { FormatConfig } from '@/types/config'

// ── Types ─────────────────────────────────────────────────────────────────────

/** All nodes in a thread keyed by speechId (multiple possible per speech) */
export type NodesBySpeech = Map<string, FlowNode[]>

export interface GridThread {
  thread: FlowThread
  /** speechId → nodes in this thread that belong to that speech */
  nodesBySpeech: NodesBySpeech
}

export interface GridIssueGroup {
  issueId: string
  issueLabel: string
  isHardGate: boolean
  threads: GridThread[]
}

export interface GridModel {
  /** Speeches to render as columns, in order, non-CX only */
  speeches: string[]
  /** Stock issue groups with their threads, in format order */
  issueGroups: GridIssueGroup[]
  /** All nodes keyed by argumentId for quick lookup */
  nodeMap: Map<string, FlowNode>
}

// ── Build grid model ──────────────────────────────────────────────────────────

export function buildGridModel(
  debate: Debate,
  flow: FlowGraphResponse,
  format: FormatConfig
): GridModel {
  // 1. Speech columns: use format order, filter to speeches in this debate
  const debateSpeechIds = new Set(debate.speeches.map(s => s.speechId))
  const speeches = format.speechOrder
    .filter(s => s.type !== 'CrossEx' && debateSpeechIds.has(s.speechId))
    .map(s => s.speechId)

  // 2. Node map: argumentId → FlowNode
  const nodeMap = new Map<string, FlowNode>(flow.nodes.map(n => [n.argumentId, n]))

  // 3. For each thread, build nodesBySpeech
  const gridThreads: GridThread[] = flow.threads.map(thread => {
    const nodesBySpeech = new Map<string, FlowNode[]>()
    for (const argumentId of thread.nodeIds) {
      const node = nodeMap.get(argumentId)
      if (!node) continue
      const existing = nodesBySpeech.get(node.speechId) ?? []
      nodesBySpeech.set(node.speechId, [...existing, node])
    }
    return { thread, nodesBySpeech }
  })

  // 4. Group threads by stock issue, in format order
  const hardGateSet = new Set(format.hardGateIssues)

  const issueGroups: GridIssueGroup[] = format.stockIssues
    .map(issue => {
      const threads = gridThreads.filter(
        gt => gt.thread.stockIssueTag === issue.id
      )
      return {
        issueId: issue.id,
        issueLabel: issue.label,
        isHardGate: hardGateSet.has(issue.id),
        threads,
      }
    })
    .filter(g => g.threads.length > 0)

  // 5. Catch any threads with issue tags not in format (shouldn't happen, but safe)
  const coveredIssues = new Set(format.stockIssues.map(i => i.id))
  const orphanThreads = gridThreads.filter(
    gt => !coveredIssues.has(gt.thread.stockIssueTag)
  )
  if (orphanThreads.length > 0) {
    const orphanGroups = new Map<string, GridThread[]>()
    for (const gt of orphanThreads) {
      const tag = gt.thread.stockIssueTag
      orphanGroups.set(tag, [...(orphanGroups.get(tag) ?? []), gt])
    }
    orphanGroups.forEach((threads, issueId) => {
      issueGroups.push({ issueId, issueLabel: issueId, isHardGate: false, threads })
    })
  }

  return { speeches, issueGroups, nodeMap }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Returns the side-relative column position label (e.g. "1AC", "1NC") */
export function getSpeechSide(speechId: string, format: FormatConfig): 'AFF' | 'NEG' | 'CX' {
  const def = format.speechOrder.find(s => s.speechId === speechId)
  if (!def) return 'CX'
  if (def.side === 'AFF') return 'AFF'
  if (def.side === 'NEG') return 'NEG'
  return 'CX'
}

/** Strength value 0–5 → color string */
export function strengthColor(strength: number): string {
  if (strength >= 3.5) return 'var(--score-high)'
  if (strength >= 2.0) return 'var(--score-mid)'
  return 'var(--score-low)'
}

/** Clamp + round to 1 decimal */
export function fmtStrength(strength: number): string {
  return Math.max(0, Math.min(5, strength)).toFixed(1)
}
