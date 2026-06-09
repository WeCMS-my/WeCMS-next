 import http from '../request'
 import type { ApiResult } from '../generated/types'
 import type { LoginRequest, LoginResponse, RefreshRequest, RefreshResponse, CurrentUserResponse } from '../generated/types'
 
 export async function login(data: LoginRequest): Promise<LoginResponse> {
   const res = await http.post<ApiResult<LoginResponse>>('/auth/login', data)
   return res.data.data
 }
 
 export async function refresh(data: RefreshRequest): Promise<RefreshResponse> {
   const res = await http.post<ApiResult<RefreshResponse>>('/auth/refresh', data)
   return res.data.data
 }
 
 export async function logout(): Promise<void> {
  const refreshToken = localStorage.getItem('refresh_token') || ''
  await http.post('/auth/logout', null, {
    headers: { 'X-Refresh-Token': refreshToken }
  })
}
 
 export async function getCurrentUser(): Promise<CurrentUserResponse> {
   const res = await http.get<ApiResult<CurrentUserResponse>>('/auth/me')
   return res.data.data
 }
