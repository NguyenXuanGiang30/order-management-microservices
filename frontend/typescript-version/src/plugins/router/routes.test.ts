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
})
