import type { App } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'

import { routes } from './routes'
import { useAuthStore } from '@/stores/auth'

type UserRole = 'Admin' | 'Sales' | 'Warehouse'

const roleHomePaths: Record<UserRole, string> = {
  Admin: '/dashboard',
  Sales: '/sales',
  Warehouse: '/warehouse',
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

router.beforeEach(to => {
  const authStore = useAuthStore()
  const isPublicRoute = to.matched.some(route => route.meta.public)
  const userRole = normalizeRole(authStore.user?.role)

  if (!isPublicRoute && !authStore.isAuthenticated) {
    return {
      path: '/login',
      query: { redirect: to.fullPath },
    }
  }

  if (to.meta.unauthenticatedOnly && authStore.isAuthenticated)
    return roleHomePaths[userRole]

  const allowedRoles = to.matched.flatMap(route => {
    const roles = route.meta.allowedRoles

    return Array.isArray(roles) ? roles : []
  })

  if (allowedRoles.length > 0 && !allowedRoles.includes(userRole))
    return roleHomePaths[userRole]
})

function normalizeRole(role: string | null | undefined): UserRole {
  if (role === 'Sales' || role === 'Warehouse')
    return role

  return 'Admin'
}

export default function (app: App) {
  app.use(router)
}

export { router }
