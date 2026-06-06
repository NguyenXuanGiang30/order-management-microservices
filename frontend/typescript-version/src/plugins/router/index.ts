import type { App } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

import { routes } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

router.beforeEach(to => {
  const authStore = useAuthStore()
  const isPublicRoute = to.matched.some(route => route.meta.public)

  if (!isPublicRoute && !authStore.isAuthenticated) {
    return {
      path: '/login',
      query: { redirect: to.fullPath },
    }
  }

  if (to.meta.unauthenticatedOnly && authStore.isAuthenticated)
    return '/dashboard'
})

export default function (app: App) {
  app.use(router)
}

export { router }
