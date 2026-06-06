import { defineStore } from 'pinia'

import {
  type LoginPayload,
  type LoginResponse,
  loginWithPassword,
  logoutWithRefreshToken,
} from '@/services/authApi'
import {
  clearAuthSession,
  readAuthSession,
  writeAuthSession,
} from '@/stores/authSession'

interface LoginCredentials extends LoginPayload {
  remember: boolean
}

interface AuthState {
  session: LoginResponse | null
  loading: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    session: readAuthSession(),
    loading: false,
  }),

  getters: {
    user: state => state.session?.user ?? null,
    accessToken: state => state.session?.accessToken ?? null,
    refreshToken: state => state.session?.refreshToken ?? null,
    isAuthenticated: state => Boolean(state.session?.accessToken),
  },

  actions: {
    async login(credentials: LoginCredentials) {
      this.loading = true

      try {
        const session = await loginWithPassword({
          username: credentials.username,
          password: credentials.password,
        })

        writeAuthSession(session, credentials.remember)
        this.session = session

        return session
      }
      finally {
        this.loading = false
      }
    },

    async logout() {
      const currentSession = this.session

      try {
        if (currentSession?.refreshToken)
          await logoutWithRefreshToken(currentSession.refreshToken, currentSession.accessToken)
      }
      finally {
        clearAuthSession()
        this.session = null
      }
    },
  },
})
