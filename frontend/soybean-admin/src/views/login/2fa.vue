<template>
  <div class="login-container">
    <h1>Two-Factor Authentication</h1>
    <p>Enter the 6-digit code from your authenticator app</p>
    <form @submit.prevent="handleVerify">
      <input v-model="code" placeholder="000000" maxlength="6" />
      <button type="submit" :disabled="loading">Verify</button>
    </form>
    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/store/modules/auth'
import axios from 'axios'

const router = useRouter()
const authStore = useAuthStore()
const code = ref('')
const loading = ref(false)
const error = ref('')

async function handleVerify() {
  loading.value = true
  error.value = ''
  try {
    const ticket = localStorage.getItem('two_factor_ticket') || ''
    const username = localStorage.getItem('pending_username') || ''
    const res = await axios.post('/api/v1/auth/2fa/verify', {
      twoFactorTicket: ticket,
      username,
      code: code.value
    })
    if (res.data.code === 0) {
      authStore.setAuth(res.data.data.accessToken, res.data.data.refreshToken)
      localStorage.removeItem('two_factor_ticket')
      localStorage.removeItem('pending_username')
      router.push('/')
    } else {
      error.value = res.data.msg || 'Verification failed'
    }
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : 'Verification failed'
    error.value = msg
  } finally {
    loading.value = false
  }
}
</script>
