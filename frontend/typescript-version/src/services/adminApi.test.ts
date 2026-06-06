import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import {
  createBackup,
  getActivityLogs,
  getBackups,
  getNotifications,
  getPermissions,
  getUsers,
  markNotificationRead,
} from './adminApi'

describe('admin API', () => {
  it('loads users with filters', async () => {
    const calls: Array<{ path: string, query?: Record<string, unknown> }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'user-1',
              username: 'admin',
              fullName: 'Admin RetailOps',
              email: null,
              phone: null,
              role: 'Admin',
              avatarUrl: null,
              isActive: true,
              createdAt: '2026-06-01T00:00:00Z',
            },
          ],
          pageNumber: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        } as T
      },
    }

    const result = await getUsers({ search: 'adm', role: 'Admin' }, client)

    assert.equal(calls[0].path, '/api/users')
    assert.deepEqual(calls[0].query, {
      search: 'adm',
      role: 'Admin',
      page: 1,
      pageSize: 20,
    })
    assert.equal(result.items[0].username, 'admin')
  })

  it('loads permission catalog', async () => {
    const calls: string[] = []
    const client = {
      get: async <T>(path: string) => {
        calls.push(path)

        return [
          {
            code: 'permissions.manage',
            name: 'Manage permissions',
            group: 'Admin',
            description: null,
          },
        ] as T
      },
    }

    const result = await getPermissions(client)

    assert.equal(calls[0], '/api/users/permissions')
    assert.equal(result[0].code, 'permissions.manage')
  })

  it('loads notifications and can mark one as read', async () => {
    const calls: Array<{ method: string, path: string, query?: Record<string, unknown>, body?: unknown }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ method: 'GET', path, query: options?.query })

        return [
          {
            id: 'notification-1',
            title: 'Low stock',
            message: 'SKU-001 is low',
            severity: 'Warning',
            isRead: false,
            createdAt: '2026-06-01T00:00:00Z',
          },
        ] as T
      },
      put: async <T>(path: string, body?: unknown) => {
        calls.push({ method: 'PUT', path, body })

        return true as T
      },
    }

    const notifications = await getNotifications({ isRead: false }, client)
    const marked = await markNotificationRead('notification-1', client)

    assert.equal(calls[0].path, '/api/notifications')
    assert.deepEqual(calls[0].query, {
      isRead: false,
      page: 1,
      pageSize: 20,
    })
    assert.equal(calls[1].path, '/api/notifications/notification-1/read')
    assert.equal(notifications[0].title, 'Low stock')
    assert.equal(marked, true)
  })

  it('loads and creates backups', async () => {
    const calls: Array<{ method: string, path: string, body?: unknown }> = []
    const client = {
      get: async <T>(path: string) => {
        calls.push({ method: 'GET', path })

        return [
          {
            id: 'backup-1',
            fileName: 'backup.json',
            createdByName: 'Admin RetailOps',
            note: null,
            createdAt: '2026-06-01T00:00:00Z',
          },
        ] as T
      },
      post: async <T>(path: string, body?: unknown) => {
        calls.push({ method: 'POST', path, body })

        return {
          id: 'backup-2',
          fileName: 'backup-2.json',
          createdByName: 'Admin RetailOps',
          note: 'manual',
          createdAt: '2026-06-01T01:00:00Z',
        } as T
      },
    }

    const backups = await getBackups(client)
    const backup = await createBackup({ note: 'manual' }, client)

    assert.equal(calls[0].path, '/api/backups')
    assert.equal(calls[1].path, '/api/backups')
    assert.deepEqual(calls[1].body, { note: 'manual' })
    assert.equal(backups[0].id, 'backup-1')
    assert.equal(backup.id, 'backup-2')
  })

  it('loads activity logs', async () => {
    const calls: Array<{ path: string, query?: Record<string, unknown> }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'log-1',
              userName: 'admin',
              serviceName: 'UserReport',
              action: 'Login',
              entityType: 'User',
              severity: 'Info',
              createdAt: '2026-06-01T00:00:00Z',
            },
          ],
          pageNumber: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
        } as T
      },
    }

    const result = await getActivityLogs({ pageSize: 10 }, client)

    assert.equal(calls[0].path, '/api/reports/activity-logs')
    assert.deepEqual(calls[0].query, {
      page: 1,
      pageSize: 10,
    })
    assert.equal(result.items[0].action, 'Login')
  })
})
