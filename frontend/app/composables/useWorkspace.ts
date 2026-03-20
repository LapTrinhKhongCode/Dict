// composables/useWorkspace.ts
import { useRuntimeConfig } from '#app'
import { useJwt } from './useJwt'
import { useRouter } from 'vue-router'

export interface WorkspaceDto {
  id: number
  name: string
  description: string
  createdAt: string
  myRole: string
  memberCount: number
}

export interface WorkspaceMemberDto {
  userId: number
  userName: string
  email: string
  avatarUrl: string
  role: string
}

export const useWorkspace = () => {
  const config = useRuntimeConfig()
  const { jwt, isAuthenticated } = useJwt()
  const router = useRouter()
  const base = config.public.apiBaseUrl

  // Lấy token mới nhất mỗi lần gọi
  const headers = () => ({
    Authorization: `Bearer ${jwt.value ?? ''}`
  })

  // Guard: chưa đăng nhập thì redirect /login
  const guard = () => {
    if (!isAuthenticated.value) {
      router.push('/login')
      throw new Error('Chưa đăng nhập')
    }
  }

  // ── Workspace ────────────────────────────────────────────────
  const getMyWorkspaces = () => {
    guard()
    return $fetch<WorkspaceDto[]>(`${base}/api/workspaces`, { headers: headers() })
  }

  const getWorkspace = (id: number) => {
    guard()
    return $fetch<WorkspaceDto>(`${base}/api/workspaces/${id}`, { headers: headers() })
  }

  const createWorkspace = (body: { name: string; description: string }) => {
    guard()
    return $fetch<WorkspaceDto>(`${base}/api/workspaces`, {
      method: 'POST', body, headers: headers()
    })
  }

  const updateWorkspace = (id: number, body: { name?: string; description?: string }) => {
    guard()
    return $fetch<WorkspaceDto>(`${base}/api/workspaces/${id}`, {
      method: 'PUT', body, headers: headers()
    })
  }

  const deleteWorkspace = (id: number) => {
    guard()
    return $fetch(`${base}/api/workspaces/${id}`, { method: 'DELETE', headers: headers() })
  }

  // ── Members ──────────────────────────────────────────────────
  const getMembers = (workspaceId: number) => {
    guard()
    return $fetch<WorkspaceMemberDto[]>(`${base}/api/workspaces/${workspaceId}/members`, {
      headers: headers()
    })
  }

  const inviteMember = (workspaceId: number, email: string, role = 'Member') => {
    guard()
    return $fetch(`${base}/api/workspaces/${workspaceId}/members`, {
      method: 'POST', body: { email, role }, headers: headers()
    })
  }

  const updateMemberRole = (workspaceId: number, userId: number, role: string) => {
    guard()
    return $fetch(`${base}/api/workspaces/${workspaceId}/members/${userId}/role`, {
      method: 'PUT', body: { role }, headers: headers()
    })
  }

  const removeMember = (workspaceId: number, userId: number) => {
    guard()
    return $fetch(`${base}/api/workspaces/${workspaceId}/members/${userId}`, {
      method: 'DELETE', headers: headers()
    })
  }

  const leaveWorkspace = (workspaceId: number) => {
    guard()
    return $fetch(`${base}/api/workspaces/${workspaceId}/leave`, {
      method: 'POST', headers: headers()
    })
  }

  return {
    getMyWorkspaces, getWorkspace,
    createWorkspace, updateWorkspace, deleteWorkspace,
    getMembers, inviteMember, updateMemberRole, removeMember, leaveWorkspace
  }
}