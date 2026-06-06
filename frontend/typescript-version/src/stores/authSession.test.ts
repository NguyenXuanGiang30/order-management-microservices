import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import {
  clearAuthSession,
  replaceAuthSession,
  readAuthSession,
  readAuthSessionState,
  writeAuthSession,
} from './authSession'

class MemoryStorage implements Storage {
  private readonly data = new Map<string, string>()

  get length() {
    return this.data.size
  }

  clear() {
    this.data.clear()
  }

  getItem(key: string) {
    return this.data.get(key) ?? null
  }

  key(index: number) {
    return Array.from(this.data.keys())[index] ?? null
  }

  removeItem(key: string) {
    this.data.delete(key)
  }

  setItem(key: string, value: string) {
    this.data.set(key, value)
  }
}

const session = {
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

describe('auth session storage', () => {
  it('persists remembered sessions in local storage', () => {
    const localStorage = new MemoryStorage()
    const sessionStorage = new MemoryStorage()

    writeAuthSession(session, true, { localStorage, sessionStorage })

    assert.deepEqual(readAuthSession({ localStorage, sessionStorage }), session)
    assert.equal(sessionStorage.length, 0)
  })

  it('persists non-remembered sessions in session storage', () => {
    const localStorage = new MemoryStorage()
    const sessionStorage = new MemoryStorage()

    writeAuthSession(session, false, { localStorage, sessionStorage })

    assert.deepEqual(readAuthSession({ localStorage, sessionStorage }), session)
    assert.equal(localStorage.length, 0)
  })

  it('clears both session locations', () => {
    const localStorage = new MemoryStorage()
    const sessionStorage = new MemoryStorage()

    writeAuthSession(session, true, { localStorage, sessionStorage })
    writeAuthSession(session, false, { localStorage, sessionStorage })
    clearAuthSession({ localStorage, sessionStorage })

    assert.equal(readAuthSession({ localStorage, sessionStorage }), null)
  })

  it('replaces a remembered session in the same storage location', () => {
    const localStorage = new MemoryStorage()
    const sessionStorage = new MemoryStorage()
    const nextSession = {
      ...session,
      accessToken: 'new-access-token',
      refreshToken: 'new-refresh-token',
    }

    writeAuthSession(session, true, { localStorage, sessionStorage })
    replaceAuthSession(nextSession, { localStorage, sessionStorage })

    assert.deepEqual(readAuthSessionState({ localStorage, sessionStorage }), {
      session: nextSession,
      remember: true,
    })
    assert.equal(sessionStorage.length, 0)
  })
})
