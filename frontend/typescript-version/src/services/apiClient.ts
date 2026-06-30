import {
  type ApiResponse,
  AuthApiError,
  type LoginResponse,
  getApiBaseUrl,
  refreshAccessToken,
} from '@/services/authApi'
import {
  clearAuthSession,
  readAuthSession,
  replaceAuthSession,
} from '@/stores/authSession'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

export interface ApiClientOptions {
  baseUrl?: string
  fetcher?: typeof fetch
  getSession?: () => LoginResponse | null
  setSession?: (session: LoginResponse) => void
  clearSession?: () => void
  onUnauthorized?: () => void
}

export interface ApiRequestOptions {
  headers?: Record<string, string>
  query?: Record<string, string | number | boolean | null | undefined>
}

export class ApiClientError extends Error {
  readonly status: number
  readonly errors: string[]

  constructor(message: string, status: number, errors: string[] = []) {
    super(message)
    this.name = 'ApiClientError'
    this.status = status
    this.errors = errors
  }
}

export function createApiClient(options: ApiClientOptions = {}) {
  const fetcher = options.fetcher ?? fetch
  const getSession = options.getSession ?? readAuthSession
  const setSession = options.setSession ?? replaceAuthSession
  const clearSession = options.clearSession ?? clearAuthSession
  const onUnauthorized = options.onUnauthorized ?? redirectToLogin

  const request = async <T>(
    method: HttpMethod,
    path: string,
    body?: unknown,
    requestOptions: ApiRequestOptions = {},
    hasRetried = false,
  ): Promise<T> => {
    const session = getSession()
    const headers: Record<string, string> = {
      Accept: 'application/json',
      ...requestOptions.headers,
    }

    if (body !== undefined && !(body instanceof FormData))
      headers['Content-Type'] = 'application/json'

    if (session?.accessToken)
      headers.Authorization = `Bearer ${session.accessToken}`

    const response = await fetcher(buildUrl(path, options.baseUrl, requestOptions.query), {
      method,
      headers,
      body: body === undefined ? undefined : (body instanceof FormData ? body : JSON.stringify(body)),
    })

    const responseBody = await readResponse<any>(response)

    if (response.status === 401 && session?.refreshToken && !hasRetried) {
      try {
        const refreshedToken = await refreshAccessToken(session.refreshToken, {
          baseUrl: options.baseUrl,
          fetcher,
        })
        const nextSession: LoginResponse = {
          ...session,
          isSuccess: refreshedToken.isSuccess,
          accessToken: refreshedToken.accessToken,
          refreshToken: refreshedToken.refreshToken,
          expiresIn: refreshedToken.expiresIn,
          message: refreshedToken.message,
        }

        setSession(nextSession)

        return request<T>(method, path, body, requestOptions, true)
      }
      catch {
        clearSession()
        onUnauthorized()
      }
    }

    const isSuccess = responseBody?.success ?? responseBody?.Success ?? false
    const msg = responseBody?.message ?? responseBody?.Message
    const errs = responseBody?.errors ?? responseBody?.Errors ?? []
    const data = responseBody?.data ?? responseBody?.Data

    if (!response.ok || !isSuccess) {
      let detailedMsg = msg
      if (Array.isArray(errs) && errs.length > 0) {
        const errorStrings = errs.map((e: any) => {
          if (typeof e === 'string') return e
          const errMsg = e.errorMessage ?? e.ErrorMessage ?? e.message ?? e.Message
          const propName = e.propertyName ?? e.PropertyName
          return propName ? `${propName}: ${errMsg}` : errMsg
        }).filter(Boolean)
        if (errorStrings.length > 0) {
          detailedMsg = errorStrings.join('\n')
        }
      }

      throw new ApiClientError(
        detailedMsg || 'Không thể tải dữ liệu từ máy chủ.',
        response.status,
        errs,
      )
    }

    return data as T
  }

  return {
    get: <T>(path: string, requestOptions?: ApiRequestOptions) =>
      request<T>('GET', path, undefined, requestOptions),
    post: <T>(path: string, body?: unknown, requestOptions?: ApiRequestOptions) =>
      request<T>('POST', path, body, requestOptions),
    put: <T>(path: string, body?: unknown, requestOptions?: ApiRequestOptions) =>
      request<T>('PUT', path, body, requestOptions),
    patch: <T>(path: string, body?: unknown, requestOptions?: ApiRequestOptions) =>
      request<T>('PATCH', path, body, requestOptions),
    delete: <T>(path: string, requestOptions?: ApiRequestOptions) =>
      request<T>('DELETE', path, undefined, requestOptions),
  }
}

export const apiClient = createApiClient()

function buildUrl(
  path: string,
  baseUrl?: string,
  query?: ApiRequestOptions['query'],
) {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = new URL(`${getApiBaseUrl(baseUrl)}${normalizedPath}`)

  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== null && value !== undefined)
      url.searchParams.set(key, String(value))
  })

  return url.toString()
}

async function readResponse<T>(response: Response): Promise<T> {
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

function redirectToLogin() {
  if (typeof window === 'undefined')
    return

  if (window.location.pathname !== '/login')
    window.location.assign('/login')
}
