 // Auto-generated from OpenAPI — DO NOT EDIT BY HAND
 // Source: artifacts/openapi/wecms-api-v1.json
 
 export interface ApiResult<T> {
   code: number
   msg: string
   data: T
 }
 
 export interface PagedResult<T> {
   records: T[]
   page: number
   pageSize: number
   total: number
 }
 
 export interface LoginRequest {
   username: string
   password: string
 }
 
 export interface LoginResponse {
  accessToken: string | null
  refreshToken: string | null
  expiresIn: number
  requiresTwoFactor: boolean
  twoFactorTicket?: string | null
}
 
 export interface RefreshRequest {
   refreshToken: string
 }
 
 export interface RefreshResponse {
   accessToken: string
   refreshToken: string
   expiresIn: number
 }
 
 export interface CurrentUserResponse {
   id: number
   username: string
   displayName: string
   roles: string[]
   permissions: string[]
   menus: unknown[]
 }
