export const API_BASE_URL = process.env.NUXT_PUBLIC_API_BASE_URL || 'https://localhost:7084';

export function apiUrl(path: string) {
  return `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`
}
