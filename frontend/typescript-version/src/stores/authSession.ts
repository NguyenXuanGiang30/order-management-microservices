import type { LoginResponse } from '@/services/authApi'

export type AuthSession = LoginResponse

export interface AuthStorage {
  localStorage: Storage
  sessionStorage: Storage
}

export interface AuthSessionState {
  session: AuthSession
  remember: boolean
}

const AUTH_SESSION_KEY = 'retailops.auth.session'

export function readAuthSession(storage = getBrowserStorage()): AuthSession | null {
  return readAuthSessionState(storage)?.session ?? null
}

export function readAuthSessionState(storage = getBrowserStorage()): AuthSessionState | null {
  if (!storage)
    return null

  const rememberedSession = readSessionFromStorage(storage.localStorage)

  if (rememberedSession) {
    return {
      session: rememberedSession,
      remember: true,
    }
  }

  const browserSession = readSessionFromStorage(storage.sessionStorage)

  if (browserSession) {
    return {
      session: browserSession,
      remember: false,
    }
  }

  return null
}

export function writeAuthSession(session: AuthSession, remember: boolean, storage = getBrowserStorage()) {
  if (!storage)
    return

  const targetStorage = remember ? storage.localStorage : storage.sessionStorage
  const staleStorage = remember ? storage.sessionStorage : storage.localStorage

  staleStorage.removeItem(AUTH_SESSION_KEY)
  targetStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session))
}

export function replaceAuthSession(session: AuthSession, storage = getBrowserStorage()) {
  const currentState = readAuthSessionState(storage)

  writeAuthSession(session, currentState?.remember ?? true, storage)
}

export function clearAuthSession(storage = getBrowserStorage()) {
  if (!storage)
    return

  storage.localStorage.removeItem(AUTH_SESSION_KEY)
  storage.sessionStorage.removeItem(AUTH_SESSION_KEY)
}

function readSessionFromStorage(storage: Storage): AuthSession | null {
  const rawSession = storage.getItem(AUTH_SESSION_KEY)

  if (!rawSession)
    return null

  try {
    return JSON.parse(rawSession) as AuthSession
  }
  catch {
    storage.removeItem(AUTH_SESSION_KEY)

    return null
  }
}

function getBrowserStorage(): AuthStorage | null {
  if (typeof window === 'undefined')
    return null

  return {
    localStorage: window.localStorage,
    sessionStorage: window.sessionStorage,
  }
}
