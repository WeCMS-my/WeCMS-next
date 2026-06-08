 import { createRouter, createWebHistory } from 'vue-router'
 
 const router = createRouter({
   history: createWebHistory(),
   routes: [
     {
       path: '/login',
       name: 'Login',
       component: () => import('@/views/login/index.vue'),
       meta: { requiresAuth: false }
     },
     {
       path: '/',
       name: 'Dashboard',
       component: () => import('@/views/dashboard/index.vue'),
       meta: { requiresAuth: true }
     },
     {
       path: '/403',
       name: 'Forbidden',
       component: () => import('@/views/dashboard/index.vue')
     }
   ]
 })
 
 router.beforeEach((to, _from, next) => {
   const token = localStorage.getItem('access_token')
   if (to.meta.requiresAuth && !token) {
     next('/login')
   } else if (to.path === '/login' && token) {
     next('/')
   } else {
     next()
   }
 })
 
 export default router
