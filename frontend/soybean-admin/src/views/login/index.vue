<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/store/modules/auth'
import { login as apiLogin } from '@/service/api/auth'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function handleLogin() {
  if (!username.value || !password.value) {
    error.value = 'Please enter username and password'
    return
  }
  loading.value = true
  error.value = ''
  try {
    const res = await apiLogin({ username: username.value, password: password.value })
    if (res.requiresTwoFactor) {
      localStorage.setItem('two_factor_ticket', res.twoFactorTicket || '')
      localStorage.setItem('pending_username', username.value)
      router.push('/login/2fa')
      return
    }
    authStore.setAuth(res.accessToken || '', res.refreshToken || '')
    router.push('/')
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Login failed'
    error.value = msg
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-form" @submit.prevent="handleLogin">
      <h1>WeCMS Admin</h1>
      <div v-if="error" class="error-msg">{{ error }}</div>
      <label>
        <span>Username</span>
        <input v-model="username" type="text" autocomplete="username" :disabled="loading" />
      </label>
      <label>
        <span>Password</span>
        <input v-model="password" type="password" autocomplete="current-password" :disabled="loading" />
      </label>
      <button type="submit" :disabled="loading">
        {{ loading ? 'Logging in...' : 'Login' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f0f2f5;
}
.login-form {
  width: 360px;
  padding: 32px;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
}
.login-form h1 {
  text-align: center;
  margin: 0 0 24px;
  font-size: 20px;
}
.login-form label {
  display: block;
  margin-bottom: 16px;
}
.login-form label span {
  display: block;
  margin-bottom: 4px;
  font-size: 13px;
  color: #666;
}
.login-form input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  font-size: 14px;
  box-sizing: border-box;
}
.login-form button {
  width: 100%;
  padding: 10px;
  border: none;
  border-radius: 4px;
  background: #1677ff;
  color: #fff;
  font-size: 14px;
  cursor: pointer;
}
.login-form button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.error-msg {
  padding: 8px 12px;
  margin-bottom: 16px;
  background: #fff2f0;
  border: 1px solid #ffccc7;
  border-radius: 4px;
  color: #ff4d4f;
  font-size: 13px;
}
</style>
