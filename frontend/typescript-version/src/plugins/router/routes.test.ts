import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import { createMemoryHistory, createRouter } from 'vue-router'

import { routes } from './routes'

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes,
  })
}

describe('routes', () => {
  it('serves the landing page as a public root route', () => {
    const router = createTestRouter()
    const resolved = router.resolve('/')

    assert.equal(resolved.fullPath, '/')
    assert.equal(resolved.matched.some(route => route.meta.public), true)
  })

  it('keeps application pages private', () => {
    const router = createTestRouter()
    const resolved = router.resolve('/dashboard')

    assert.equal(resolved.matched.some(route => route.meta.public), false)
  })

  it('assigns separate default routes for sales and warehouse workspaces', () => {
    const router = createTestRouter()
    const salesRoute = router.resolve('/sales')
    const warehouseRoute = router.resolve('/warehouse')

    assert.deepEqual(salesRoute.meta.allowedRoles, ['Admin', 'Sales'])
    assert.deepEqual(warehouseRoute.meta.allowedRoles, ['Admin', 'Warehouse'])
  })

  it('keeps admin-only routes restricted to admin users', () => {
    const router = createTestRouter()
    const dashboardRoute = router.resolve('/dashboard')
    const settingsRoute = router.resolve('/settings')

    assert.deepEqual(dashboardRoute.meta.allowedRoles, ['Admin'])
    assert.deepEqual(settingsRoute.meta.allowedRoles, ['Admin'])
  })
})
