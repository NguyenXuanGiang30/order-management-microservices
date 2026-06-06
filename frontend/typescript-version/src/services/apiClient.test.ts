import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import { createApiClient } from './apiClient'
import type { LoginResponse } from './authApi'

const session: LoginResponse = {
  isSuccess: true,
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  expiresIn: 3600,
  message: 'Đăng nhập thành công.',
  user: {
    id: 'user-1',
    username: 'admin',
    fullName: 'Admin RetailOps',
    role: 'Admin',
    avatarUrl: null,
    permissions: ['reports.view'],
  },
}

describe('apiClient', () => {
  it('adds the bearer token and unwraps ApiResponse data', async () => {
    const calls: Array<{ input: string | URL | Request, init?: RequestInit }> = []
    const fetcher: typeof fetch = async (input, init) => {
      calls.push({ input, init })

      return new Response(JSON.stringify({
        success: true,
        data: [{ id: 'product-1' }],
        message: 'Thành công',
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }

    const client = createApiClient({
      baseUrl: 'http://localhost:5000',
      fetcher,
      getSession: () => session,
    })

    const result = await client.get<Array<{ id: string }>>('/api/products')

    assert.deepEqual(result, [{ id: 'product-1' }])
    assert.equal(String(calls[0].input), 'http://localhost:5000/api/products')
    assert.equal((calls[0].init?.headers as Record<string, string>).Authorization, 'Bearer access-token')
  })

  it('refreshes the token once on 401 and retries the original request', async () => {
    const calls: Array<{ input: string | URL | Request, init?: RequestInit }> = []
    let currentSession = session
    const fetcher: typeof fetch = async (input, init) => {
      calls.push({ input, init })
      const url = String(input)

      if (url.endsWith('/api/reports/dashboard') && calls.length === 1) {
        return new Response(JSON.stringify({
          success: false,
          message: 'Access token has expired. Please refresh token.',
        }), {
          status: 401,
          headers: { 'Content-Type': 'application/json' },
        })
      }

      if (url.endsWith('/api/auth/refresh-token')) {
        return new Response(JSON.stringify({
          success: true,
          data: {
            isSuccess: true,
            accessToken: 'new-access-token',
            refreshToken: 'new-refresh-token',
            expiresIn: 3600,
            message: 'Cấp lại token thành công.',
          },
        }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })
      }

      return new Response(JSON.stringify({
        success: true,
        data: { today: { totalOrders: 2 } },
        message: 'Thành công',
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }

    const client = createApiClient({
      baseUrl: 'http://localhost:5000',
      fetcher,
      getSession: () => currentSession,
      setSession: nextSession => {
        currentSession = nextSession
      },
    })

    const result = await client.get<{ today: { totalOrders: number } }>('/api/reports/dashboard')

    assert.equal(result.today.totalOrders, 2)
    assert.equal(calls.length, 3)
    assert.equal(String(calls[1].input), 'http://localhost:5000/api/auth/refresh-token')
    assert.equal((calls[2].init?.headers as Record<string, string>).Authorization, 'Bearer new-access-token')
    assert.equal(currentSession.accessToken, 'new-access-token')
    assert.equal(currentSession.refreshToken, 'new-refresh-token')
    assert.equal(currentSession.user.username, 'admin')
  })
})
