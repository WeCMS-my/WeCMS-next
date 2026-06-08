 import { defineStore } from 'pinia'
 import { ref } from 'vue'
 import { login as apiLogin, logout as apiLogout, getCurrentUser } from '@/service/api/auth'
 import { clearAuthState } from '@/service/request'
 import type { CurrentUserResponse } from '@/service/generated/types'
 
 export const useAuthStore = defineStore('auth', () => {
   const user = ref<CurrentUserResponse | null>(null)
   const isAuthenticated = ref(false)
 
   async function login(username: string, password: string) {
     const res = await apiLogin({ username, password })
     localStorage.setItem('access_token', res.accessToken)
     localStorage.setItem('refresh_token', res.refreshToken)
     isAuthenticated.value = true
     await fetchCurrentUser()
     return res
   }
 
   async function fetchCurrentUser() {
     try {
       user.value = await getCurrentUser()
       isAuthenticated.value = true
     } catch {
       user.value = null
       isAuthenticated.value = false
     }
   }
 
   async function logout() {
     try {
       await apiLogout()
     } finally {
       clearAuthState()
       user.value = null
       isAuthenticated.value = false
     }
   }
 
   function hasPermission(code: string): boolean {
     if (!user.value) return false
     return user.value.permissions.includes(code)
   }
 
   return { user, isAuthenticated, login, fetchCurrentUser, logout, hasPermission }
 })
