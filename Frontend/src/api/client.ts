// ============================================================================
// client.ts — Typed API client
// All calls go through the Vite proxy to http://localhost:5000
// ============================================================================

import type { Debate, FlowGraphResponse } from '@/types/domain'
import type {
  ScoreResponse,
  EnrichResponse,
  EnrichScoreResponse,
} from '@/types/scoring'
import type {
  FormatConfig,
  ScoringConfig,
  RoundConfig,
  StockCase,
  StockCasesResponse,
  ProvidersResponse,
} from '@/types/config'

// ── Error handling ────────────────────────────────────────────────────────────

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly details: string[] = []
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(path, {
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  })

  if (!res.ok) {
    let errorMessage = `HTTP ${res.status}`
    let details: string[] = []
    try {
      const body = await res.json()
      errorMessage = body.error ?? errorMessage
      details = body.details ?? []
    } catch {
      // body wasn't JSON
    }
    throw new ApiError(res.status, errorMessage, details)
  }

  return res.json() as Promise<T>
}

// ── Health ────────────────────────────────────────────────────────────────────

export interface HealthResponse {
  status: string
  version: string
  configs: 'loaded' | 'missing'
}

export const healthApi = {
  check: (): Promise<HealthResponse> =>
    request('/health'),
}

// ── Debate ────────────────────────────────────────────────────────────────────

export interface ScoreDebateRequest {
  debate: Debate
  includeFullExplanation?: boolean
}

export interface BuildFlowRequest {
  debate: Debate
}

export const debateApi = {
  /** POST /api/debate/score — run full scoring pipeline */
  score: (body: ScoreDebateRequest): Promise<ScoreResponse> =>
    request('http://localhost:5200/api/debate/score', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** POST /api/debate/flow — build flow graph without scoring */
  buildFlow: (body: BuildFlowRequest): Promise<FlowGraphResponse> =>
    request('http://localhost:5200/api/debate/flow', {
      method: 'POST',
      body: JSON.stringify(body),
    }),
}

// ── Enrichment ────────────────────────────────────────────────────────────────

export interface EnrichRequest {
  debate: Debate
  apiKey: string
  providerOverride?: string
}

export const enrichApi = {
  /** POST /api/enrich — enrich arguments via LLM, return enriched debate */
  enrich: (body: EnrichRequest): Promise<EnrichResponse> =>
    request('http://localhost:5200/api/enrich', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** POST /api/enrich/score — enrich then score in one round-trip */
  enrichAndScore: (body: EnrichRequest): Promise<EnrichScoreResponse> =>
    request('http://localhost:5200/api/enrich/score', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  /** GET /api/enrich/providers — list available LLM providers */
  getProviders: (): Promise<ProvidersResponse> =>
    request('http://localhost:5200/api/enrich/providers'),
}

// ── Config ────────────────────────────────────────────────────────────────────

export const configApi = {
  // Format config
  getFormat: (): Promise<FormatConfig> =>
    request('http://localhost:5200/api/config/format'),
  saveFormat: (config: FormatConfig): Promise<{ saved: boolean }> =>
    request('http://localhost:5200/api/config/format', {
      method: 'PUT',
      body: JSON.stringify(config),
    }),

  // Scoring config
  getScoring: (): Promise<ScoringConfig> =>
    request('http://localhost:5200/api/config/scoring'),
  saveScoring: (config: ScoringConfig): Promise<{ saved: boolean }> =>
    request('http://localhost:5200/api/config/scoring', {
      method: 'PUT',
      body: JSON.stringify(config),
    }),

  // Round config
  getRound: (): Promise<RoundConfig> =>
    request('http://localhost:5200/api/config/round'),
  saveRound: (config: RoundConfig): Promise<{ saved: boolean }> =>
    request('http://localhost:5200/api/config/round', {
      method: 'PUT',
      body: JSON.stringify(config),
    }),

  // Stock cases
  getStockCases: (): Promise<StockCasesResponse> =>
    request('http://localhost:5200/api/config/stockcases'),
  addStockCase: (sc: Omit<StockCase, 'source'>): Promise<{ added: boolean; stockCaseId: string }> =>
    request('http://localhost:5200/api/config/stockcases', {
      method: 'POST',
      body: JSON.stringify(sc),
    }),
  deleteStockCase: (id: string): Promise<{ deleted: boolean; stockCaseId: string }> =>
    request(`http://localhost:5200/api/config/stockcases/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    }),
}
