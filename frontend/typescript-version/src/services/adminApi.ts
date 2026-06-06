import { apiClient } from '@/services/apiClient'

export interface PagedResponse<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface UserDto {
  id: string
  username: string
  fullName: string
  email: string | null
  phone: string | null
  role: string
  avatarUrl: string | null
  isActive: boolean
  createdAt: string
}

export interface PermissionDto {
  code: string
  name: string
  group: string
  description: string | null
}

export interface NotificationDto {
  id: string
  title: string
  message: string
  severity: string
  isRead: boolean
  createdAt: string
}

export interface BackupRecordDto {
  id: string
  fileName: string
  createdByName: string
  note: string | null
  createdAt: string
}

export interface ActivityLogDto {
  id: string
  userName: string
  serviceName: string
  action: string
  entityType: string
  severity: string
  createdAt: string
}

export interface GetUsersParams {
  search?: string
  role?: string
  page?: number
  pageSize?: number
}

export interface GetNotificationsParams {
  isRead?: boolean
  page?: number
  pageSize?: number
}

export interface GetActivityLogsParams {
  userId?: string
  from?: string
  to?: string
  serviceName?: string
  severity?: string
  action?: string
  entityType?: string
  page?: number
  pageSize?: number
}

export interface CreateBackupPayload {
  note?: string | null
}

export interface RolePermissionsDto {
  role: string
  permissions: string[]
}

export interface UpdateUserPayload {
  fullName?: string | null
  email?: string | null
  phone?: string | null
  role?: string | null
  avatarUrl?: string | null
}

export interface CreateUserPayload {
  username: string
  fullName: string
  email?: string | null
  phone?: string | null
  role: string
  avatarUrl?: string | null
}

interface AdminApiClient {
  get<T>(path: string, options?: { query?: Record<string, unknown> }): Promise<T>
  post<T>(path: string, body?: unknown): Promise<T>
  put<T>(path: string, body?: unknown): Promise<T>
  delete<T>(path: string): Promise<T>
}

export const getUsers = (
  params: GetUsersParams = {},
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<UserDto>>('/api/users', {
    query: compactQuery({
      search: params.search,
      role: params.role,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const getUser = (
  id: string,
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<UserDto>(`/api/users/${id}`)

export const createUser = (
  payload: CreateUserPayload,
  client: Pick<AdminApiClient, 'post'> = apiClient,
) =>
  client.post<UserDto>('/api/users', payload)

export const updateUser = (
  id: string,
  payload: UpdateUserPayload,
  client: Pick<AdminApiClient, 'put'> = apiClient,
) =>
  client.put<UserDto>(`/api/users/${id}`, payload)

export const toggleUserActive = (
  id: string,
  client: Pick<AdminApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/users/${id}/toggle-active`)

export const getPermissions = (
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<PermissionDto[]>('/api/users/permissions')

export const getRolePermissions = (
  role: string,
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<RolePermissionsDto>(`/api/users/roles/${role}/permissions`)

export const updateRolePermissions = (
  role: string,
  permissions: string[],
  client: Pick<AdminApiClient, 'put'> = apiClient,
) =>
  client.put<RolePermissionsDto>(`/api/users/roles/${role}/permissions`, { permissions })

export const getNotifications = (
  params: GetNotificationsParams = {},
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<NotificationDto[]>('/api/notifications', {
    query: compactQuery({
      isRead: params.isRead,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const getUnreadNotificationCount = (
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<number>('/api/notifications/unread-count')

export const markNotificationRead = (
  id: string,
  client: Pick<AdminApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/notifications/${id}/read`)

export const markAllNotificationsRead = (
  client: Pick<AdminApiClient, 'put'> = apiClient,
) =>
  client.put<number>('/api/notifications/read-all')

export const getBackups = (
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<BackupRecordDto[]>('/api/backups')

export const createBackup = (
  payload: CreateBackupPayload,
  client: Pick<AdminApiClient, 'post'> = apiClient,
) =>
  client.post<BackupRecordDto>('/api/backups', payload)

export const restoreBackup = (
  backupId: string,
  payload: { confirm: boolean; note?: string | null },
  client: Pick<AdminApiClient, 'post'> = apiClient,
) =>
  client.post<BackupRecordDto>(`/api/backups/${backupId}/restore`, payload)

export const getActivityLogs = (
  params: GetActivityLogsParams = {},
  client: Pick<AdminApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<ActivityLogDto>>('/api/reports/activity-logs', {
    query: compactQuery({
      userId: params.userId,
      from: params.from,
      to: params.to,
      serviceName: params.serviceName,
      severity: params.severity,
      action: params.action,
      entityType: params.entityType,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

function compactQuery(query: Record<string, unknown>) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}
