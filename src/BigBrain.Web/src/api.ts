import type { ModuleDefinition, SystemHealth } from './types'

async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return response.json() as Promise<T>
}

export const getModules = (signal?: AbortSignal) =>
  getJson<ModuleDefinition[]>('/api/v1/modules', signal)

export const getSystemHealth = (endpoint: string, signal?: AbortSignal) =>
  getJson<SystemHealth>(endpoint, signal)

