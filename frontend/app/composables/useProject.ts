// composables/useProject.ts
import { useRuntimeConfig } from '#app'
import { useJwt } from './useJwt'
import { useRouter } from 'vue-router'

export interface ProjectDto {
  id: number
  name: string
  description: string
  workspaceId: number
  createdByUserId: number
  createdByUserName: string
  createdAt: string
  mediaCount: number
  vocabularyCount: number
}

export interface MediaDto {
  id: number
  fileName: string
  mimeType: string
  sizeBytes: number | null
  storageUrl: string
  ownerId: number
  ownerName: string
  createdAt: string | null
}

export const useProject = () => {
  const config = useRuntimeConfig()
  const { jwt } = useJwt()
  const router = useRouter()
  const base = config.public.apiBaseUrl

  // Lấy token trực tiếp — ưu tiên ref, fallback localStorage
  // Tránh race condition khi refresh trang (ref chưa kịp hydrate)
  const getToken = (): string => {
    if (jwt.value) return jwt.value
    if (process.client) return localStorage.getItem('jwt_token') ?? ''
    return ''
  }

  const headers = () => ({ Authorization: `Bearer ${getToken()}` })

  const guard = () => {
    const token = getToken()
    if (!token) {
      router.push('/login')
      throw new Error('Chưa đăng nhập')
    }
  }

  // ── Project ──────────────────────────────────────────────────
  const getProjects = (workspaceId: number) => {
    guard()
    return $fetch<ProjectDto[]>(`${base}/api/workspaces/${workspaceId}/projects`, { headers: headers() })
  }

  const getProject = (projectid: number) => {
    guard()
    return $fetch<ProjectDto>(`${base}/api/projects/${projectid}`, { headers: headers() })
  }

  const createProject = (workspaceId: number, body: { name: string; description: string }) => {
    guard()
    return $fetch<ProjectDto>(`${base}/api/workspaces/${workspaceId}/projects`, {
      method: 'POST', body, headers: headers()
    })
  }

  const updateProject = (projectid: number, body: { name?: string; description?: string }) => {
    guard()
    return $fetch<ProjectDto>(`${base}/api/projects/${projectid}`, {
      method: 'PUT', body, headers: headers()
    })
  }

  const deleteProject = (projectid: number) => {
    guard()
    return $fetch(`${base}/api/projects/${projectid}`, { method: 'DELETE', headers: headers() })
  }

  // ── Media ────────────────────────────────────────────────────
  const getMedia = (projectid: number) => {
    guard()
    return $fetch<MediaDto[]>(`${base}/api/projects/${projectid}/media`, { headers: headers() })
  }

  const uploadMedia = (projectid: number, file: File) => {
    guard()
    const form = new FormData()
    form.append('file', file)
    return $fetch<MediaDto>(`${base}/api/projects/${projectid}/media`, {
      method: 'POST',
      body: form,
      headers: { Authorization: `Bearer ${getToken()}` }
    })
  }

  const deleteMedia = (mediaId: number) => {
    guard()
    return $fetch(`${base}/api/media/${mediaId}`, { method: 'DELETE', headers: headers() })
  }

  return {
    getProjects, getProject, createProject, updateProject, deleteProject,
    getMedia, uploadMedia, deleteMedia
  }
}