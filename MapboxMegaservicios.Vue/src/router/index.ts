import { createRouter, createWebHistory } from 'vue-router'
import authService from '@/services/auth'
import LayoutWrapper from '@/components/LayoutWrapper.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/Login.vue'),
      meta: { requiresAuth: false, noLayout: true },
    },
    {
      path: '/',
      component: LayoutWrapper,
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'dashboard',
          component: () => import('@/views/Dashboard.vue'),
        },
        {
          path: 'empleados',
          name: 'empleados',
          component: () => import('@/views/Empleados.vue'),
        },
        {
          path: 'lugares',
          name: 'lugares',
          component: () => import('@/views/Lugares.vue'),
        },

        {
          path: 'reportes',
          name: 'reportes',
          component: () => import('@/views/Reportes.vue'),
        },
      ],
    },
  ],
})

// Guard de navegación
router.beforeEach((to, from, next) => {
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isAuthenticated = authService.isAuthenticated()

  if (requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && isAuthenticated) {
    next('/')
  } else {
    next()
  }
})

export default router
