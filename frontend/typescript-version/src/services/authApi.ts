export interface ApiResponse<T> {
  success: boolean
  message?: string
  data?: T
  errors?: string[]
}

export interface CurrentUser {
  id: string
  username: string
  fullName: string
  role: string
  avatarUrl: string | null
  permissions: string[]
}

export interface LoginResponse {
  isSuccess: boolean
  accessToken: string
  refreshToken: string
  expiresIn: number
  message: string
  user: CurrentUser
}

export interface RefreshTokenResponse {
  isSuccess: boolean
  accessToken: string
  refreshToken: string
  expiresIn: number
  message: string
}

export interface LoginPayload {
  username: string
  password: string
}

export interface AuthApiOptions {
  baseUrl?: string
  fetcher?: typeof fetch
}

export class AuthApiError extends Error {
  readonly status: number
  readonly errors: string[]

  constructor(message: string, status: number, errors: string[] = []) {
    super(message)
    this.name = 'AuthApiError'
    this.status = status
    this.errors = errors
  }
}

const DEFAULT_API_BASE_URL = 'http://localhost:5000'

export const getApiBaseUrl = (baseUrl = import.meta.env?.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL) =>
  baseUrl.replace(/\/+$/, '')

export async function loginWithPassword(
  payload: LoginPayload,
  options: AuthApiOptions = {},
): Promise<LoginResponse> {
  const fetcher = options.fetcher ?? fetch
  const response = await fetcher(`${getApiBaseUrl(options.baseUrl)}/api/auth/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      username: payload.username,
      password: payload.password,
    }),
  })

  const body = await readJson<ApiResponse<LoginResponse>>(response)

  if (!response.ok || !body.success || !body.data?.accessToken || !body.data.refreshToken || !body.data.user) {
    throw new AuthApiError(
      body.message || 'Không thể đăng nhập. Vui lòng thử lại.',
      response.status,
      body.errors ?? [],
    )
  }

  return {
    ...body.data,
    user: {
      ...body.data.user,
      avatarUrl: body.data.user.avatarUrl ?? null,
      permissions: body.data.user.permissions ?? [],
    },
  }
}

export async function refreshAccessToken(
  refreshToken: string,
  options: AuthApiOptions = {},
): Promise<RefreshTokenResponse> {
  const fetcher = options.fetcher ?? fetch
  const response = await fetcher(`${getApiBaseUrl(options.baseUrl)}/api/auth/refresh-token`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ refreshToken }),
  })

  const body = await readJson<ApiResponse<RefreshTokenResponse>>(response)

  if (!response.ok || !body.success || !body.data?.accessToken || !body.data.refreshToken) {
    throw new AuthApiError(
      body.message || 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
      response.status,
      body.errors ?? [],
    )
  }

  return body.data
}

export async function logoutWithRefreshToken(
  refreshToken: string,
  accessToken: string | null,
  options: AuthApiOptions = {},
): Promise<void> {
  const fetcher = options.fetcher ?? fetch
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  if (accessToken)
    headers.Authorization = `Bearer ${accessToken}`

  const response = await fetcher(`${getApiBaseUrl(options.baseUrl)}/api/auth/logout`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ refreshToken }),
  })

  if (response.ok)
    return

  const body = await readJson<ApiResponse<boolean>>(response)

  throw new AuthApiError(
    body.message || 'Không thể đăng xuất khỏi máy chủ.',
    response.status,
    body.errors ?? [],
  )
}

async function readJson<T>(response: Response): Promise<T> {
  try {
    return await response.json() as T
  }
  catch {
    return {
      success: false,
      message: 'Phản hồi từ máy chủ không hợp lệ.',
      errors: [],
    } as T
  }
}
