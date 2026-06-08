 <script setup lang="ts">
 import { useAuthStore } from '@/store/modules/auth'
 import { useRouter } from 'vue-router'
 
 const authStore = useAuthStore()
 const router = useRouter()
 
 async function handleLogout() {
   await authStore.logout()
   router.push('/login')
 }
 </script>
 
 <template>
   <div class="dashboard">
     <header>
       <h2>WeCMS Admin</h2>
       <div class="user-area">
         <span v-if="authStore.user">{{ authStore.user.displayName }}</span>
         <button @click="handleLogout">Logout</button>
       </div>
     </header>
     <main>
       <p v-if="authStore.user">
         Logged in as <strong>{{ authStore.user.username }}</strong>
       </p>
       <p>Roles: {{ authStore.user?.roles?.join(', ') }}</p>
       <p>Permissions: {{ authStore.user?.permissions?.join(', ') || 'none' }}</p>
     </main>
   </div>
 </template>
 
 <style scoped>
 .dashboard { min-height: 100vh; background: #f0f2f5; }
 header {
   display: flex; justify-content: space-between; align-items: center;
   padding: 16px 24px; background: #fff; box-shadow: 0 1px 4px rgba(0,0,0,0.08);
 }
 header h2 { margin: 0; font-size: 16px; }
 .user-area { display: flex; align-items: center; gap: 12px; }
 .user-area button {
   padding: 4px 12px; border: 1px solid #d9d9d9; border-radius: 4px;
   background: #fff; cursor: pointer; font-size: 13px;
 }
 main { padding: 24px; max-width: 800px; }
 </style>
