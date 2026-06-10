import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { ApiResult } from '../generated/types'

const http: AxiosInstance = axios.create({
  baseURL: '/api/v1',
  timeout: 10_000,
  headers: { 'Content-Type': 'application/json' }
})

// Request interceptor — attach access token
http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('access_token')
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor — unwrap ApiResult, handle 401/403
http.interceptors.response.use(
  (response) => {
    const body = response.data as ApiResult<unknown>
    if (body.code !== 0) {
      return Promise.reject(new ApiError(body.code, body.msg))
    }
    return response
  },
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Try refresh
      const refreshed = await tryRefreshToken()
      if (refreshed && error.config) {
        return http.request(error.config)
      }
      // Refresh failed — redirect to login
      clearAuthState()
      window.location.href = '/login'
      return Promise.reject(error)
    }
    if (error.response?.status === 403) {
      window.location.href = '/403'
      return Promise.reject(error)
    }
    return Promise.reject(error)
  }
)

let isRefreshing = false
let refreshSubscribers: Array<(token: string) => void> = []

async function tryRefreshToken(): Promise<boolean> {
  const refreshToken = localStorage.getItem('refresh_token')
  if (!refreshToken) return false

  if (isRefreshing) {
    return new Promise((resolve) => {
      refreshSubscribers.push((token) => resolve(!!token))
    })
  }

  isRefreshing = true
  try {
    const res = await axios.post<ApiResult<{ accessToken: string; refreshToken: string; expiresIn: number }>>(
      '/api/v1/auth/refresh',
      { refreshToken }
    )
    if (res.data.code === 0) {
      const d = res.data.data
      localStorage.setItem('access_token', d.accessToken)
      localStorage.setItem('refresh_token', d.refreshToken)
      refreshSubscribers.forEach((cb) => cb(d.accessToken))
      return true
    }
    return false
  } catch {
    // Notify waiting subscribers of failure (empty token = retry will fail)
    refreshSubscribers.forEach((cb) => cb(''))
    return false
  } finally {
    isRefreshing = false
    refreshSubscribers = []
  }
}

export function clearAuthState() {
  localStorage.removeItem('access_token')
  localStorage.removeItem('refresh_token')
}

export class ApiError extends Error {
  constructor(public code: number, message: string) {
    super(message)
    this.name = 'ApiError'
  }
}

export default http
