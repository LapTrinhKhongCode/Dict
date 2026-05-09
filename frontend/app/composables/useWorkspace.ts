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

// Thêm Interface cho Lời mời
export interface WorkspaceInvitationDto {
  id: number
  workspaceId: number
  workspaceName: string
  inviterName: string
  expectedRole: string
  status: string
  createdAt: string
}

export const useWorkspace = () => {
  const config = useRuntimeConfig()
  const { jwt, isAuthenticated } = useJwt()
  const router = useRouter()
  // Lấy API base URL từ nuxt.config.ts
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

  // ── Workspace Invitations (Lời mời) ──────────────────────────
  
// Thay vì URL cũ: /api/workspaces/${workspaceId}/members
const inviteMember = (workspaceId: number, email: string, role = 'MEMBER') => {
  guard()
  // GỌI ĐÚNG VÀO API LỜI MỜI MỚI VÀ TRUYỀN ĐÚNG BIẾN inviteeEmail
  return $fetch(`${base}/api/WorkspaceInvitation/invite`, {
    method: 'POST', 
    body: { 
      workspaceId: workspaceId,
      inviteeIdentifier: email, 
      expectedRole: role 
    }, 
    headers: headers()
  })
}
  // Lấy danh sách lời mời đang chờ duyệt của User hiện tại
  const getMyPendingInvitations = () => {
    guard()
    // Lưu ý: Tùy theo ResponseDTO bên Backend bạn trả về, có thể bạn sẽ cần .result ở nơi gọi hàm này
    return $fetch<WorkspaceInvitationDto[]>(`${base}/api/WorkspaceInvitation/my-pending`, {
      headers: headers()
    })
  }

  // Đồng ý hoặc Từ chối lời mời
  const respondToInvitation = (invitationId: number, isAccepted: boolean) => {
    guard()
    return $fetch(`${base}/api/WorkspaceInvitation/${invitationId}/respond?accept=${isAccepted}`, {
      method: 'POST',
      headers: headers()
    })
  }

  return {
    getMyWorkspaces, getWorkspace,
    createWorkspace, updateWorkspace, deleteWorkspace,
    getMembers, updateMemberRole, removeMember, leaveWorkspace,
    // Export các hàm Invitation mới
    inviteMember, getMyPendingInvitations, respondToInvitation
  }
}