import { useRuntimeConfig } from '#app'
import { useJwt } from './useJwt'

export const useFileComment = () => {
  const config = useRuntimeConfig()
  const { jwt } = useJwt()
  const base = config.public.apiBaseUrl

  // Đọc từ localStorage làm primary source (giống CollabCursorOverlay)
  // Tránh trường hợp jwt.value null do SSR hydration timing
  const getToken = () =>
    (process.client ? localStorage.getItem('jwt_token') : null) ?? jwt.value ?? ''

  const headers = () => ({ Authorization: `Bearer ${getToken()}` })

  const getComments = async (fileId: number) => {
    return await $fetch(`${base}/api/FileComment/file/${fileId}`, { headers: headers() })
  }

  const addComment = async (body: any) => {
    return await $fetch(`${base}/api/FileComment`, { method: 'POST', body, headers: headers() })
  }

  const deleteComment = async (commentId: number) => {
    return await $fetch(`${base}/api/FileComment/${commentId}`, { method: 'DELETE', headers: headers() })
  }

  return { getComments, addComment, deleteComment }
}