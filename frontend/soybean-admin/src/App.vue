 <script setup lang="ts">
 import { useAuthStore } from '@/store/modules/auth'
 import { onMounted } from 'vue'
 
 const authStore = useAuthStore()
 
 onMounted(async () => {
   const token = localStorage.getItem('access_token')
   if (token) {
     await authStore.fetchCurrentUser()
   }
 })
 </script>
 
 <template>
   <router-view />
 </template>
