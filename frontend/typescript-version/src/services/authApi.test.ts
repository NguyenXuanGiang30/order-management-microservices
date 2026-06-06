import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import {
  loginWithPassword,
  logoutWithRefreshToken,
  refreshAccessToken,
} from './authApi'

describe('loginWithPassword', () => {
  it('posts username and password to the gateway auth endpoint', async () => {
    const calls: Array<{ input: string | URL | Request, init?: RequestInit }> = []
    const fetcher: typeof fetch = async (input, init) => {
      calls.push({ input, init })

      return new Response(JSON.stringify({
        success: true,
        message: 'Đăng nhập thành công.',
        data: {
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
        },
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }

    const result = await loginWithPassword(
      { username: 'admin', password: 'password123' },
      { baseUrl: 'http://localhost:5000/', fetcher },
    )

    assert.equal(calls.length, 1)
    assert.equal(String(calls[0].input), 'http://localhost:5000/api/auth/login')
    assert.equal(calls[0].init?.method, 'POST')
    assert.deepEqual(JSON.parse(String(calls[0].init?.body)), {
      username: 'admin',
      password: 'password123',
    })
    assert.equal(result.accessToken, 'access-token')
    assert.equal(result.refreshToken, 'refresh-token')
    assert.equal(result.user.role, 'Admin')
  })

  it('throws the API message when login fails', async () => {
    const fetcher: typeof fetch = async () => new Response(JSON.stringify({
      success: false,
      message: 'Tên đăng nhập hoặc mật khẩu không đúng.',
      errors: ['INVALID_LOGIN'],
    }), {
      status: 401,
      headers: { 'Content-Type': 'application/json' },
    })

    await assert.rejects(
      loginWithPassword(
        { username: 'admin', password: 'wrong' },
        { baseUrl: 'http://localhost:5000', fetcher },
      ),
      /Tên đăng nhập hoặc mật khẩu không đúng\./,
    )
  })
})

describe('refreshAccessToken', () => {
  it('posts the refresh token to the gateway refresh endpoint', async () => {
    const calls: Array<{ input: string | URL | Request, init?: RequestInit }> = []
    const fetcher: typeof fetch = async (input, init) => {
      calls.push({ input, init })

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

    const result = await refreshAccessToken('old-refresh-token', {
      baseUrl: 'http://localhost:5000',
      fetcher,
    })

    assert.equal(calls.length, 1)
    assert.equal(String(calls[0].input), 'http://localhost:5000/api/auth/refresh-token')
    assert.equal(calls[0].init?.method, 'POST')
    assert.deepEqual(JSON.parse(String(calls[0].init?.body)), {
      refreshToken: 'old-refresh-token',
    })
    assert.equal(result.accessToken, 'new-access-token')
    assert.equal(result.refreshToken, 'new-refresh-token')
  })
})

describe('logoutWithRefreshToken', () => {
  it('posts the refresh token with the access token authorization header', async () => {
    const calls: Array<{ input: string | URL | Request, init?: RequestInit }> = []
    const fetcher: typeof fetch = async (input, init) => {
      calls.push({ input, init })

      return new Response(JSON.stringify({
        success: true,
        data: true,
        message: 'Đăng xuất thành công.',
      }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }

    await logoutWithRefreshToken('refresh-token', 'access-token', {
      baseUrl: 'http://localhost:5000',
      fetcher,
    })

    assert.equal(calls.length, 1)
    assert.equal(String(calls[0].input), 'http://localhost:5000/api/auth/logout')
    assert.equal(calls[0].init?.method, 'POST')
    assert.equal((calls[0].init?.headers as Record<string, string>).Authorization, 'Bearer access-token')
    assert.deepEqual(JSON.parse(String(calls[0].init?.body)), {
      refreshToken: 'refresh-token',
    })
  })
})
