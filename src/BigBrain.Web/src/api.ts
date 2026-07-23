import type { DockerInventory, MediaOverview, ModuleDefinition, SystemOverview } from './types'

async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return response.json() as Promise<T>
}

export const getModules = (signal?: AbortSignal) =>
  getJson<ModuleDefinition[]>('/api/v1/modules', signal)

export const getSystemOverview = (signal?: AbortSignal) =>
  getJson<SystemOverview>('/api/v1/system/overview', signal)

export const getDockerContainers = (signal?: AbortSignal) =>
  getJson<DockerInventory>('/api/v1/docker/containers', signal)

export const getMediaOverview = (signal?: AbortSignal) =>
  getJson<MediaOverview>('/api/v1/modules/media', signal)
