import { defineStore } from 'pinia'
import { ref } from 'vue'
import { login as apiLogin, logout as apiLogout, getCurrentUser } from '@/service/api/auth'
import { clearAuthState } from '@/service/request'
import type { CurrentUserResponse } from '@/service/generated/types'
import router from '@/router'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUserResponse | null>(null)
  const isAuthenticated = ref(false)

  function setAuth(accessToken: string, refreshToken: string) {
    if (accessToken) {
      localStorage.setItem('access_token', accessToken)
    }
    if (refreshToken) {
      localStorage.setItem('refresh_token', refreshToken)
    }
    isAuthenticated.value = !!accessToken
  }

  async function login(username: string, password: string) {
    const res = await apiLogin({ username, password })
    setAuth(res.accessToken || '', res.refreshToken || '')
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
      router.getRoutes().forEach(r => {
        if (r.name && r.name !== 'login' && r.name !== 'dashboard') {
          router.removeRoute(r.name)
        }
      })
      clearAuthState()
      user.value = null
      isAuthenticated.value = false
    }
  }

  function hasPermission(code: string): boolean {
    if (!user.value) return false
    return user.value.permissions.includes(code)
  }

  return { user, isAuthenticated, setAuth, login, fetchCurrentUser, logout, hasPermission }
})
