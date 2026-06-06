import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import { getDashboardReport } from './reportsApi'

describe('getDashboardReport', () => {
  it('loads the dashboard report through the shared API client', async () => {
    const calls: string[] = []
    const client = {
      get: async <T>(path: string) => {
        calls.push(path)

        return {
          today: {
            id: 'daily-1',
            reportDate: '2026-06-01T00:00:00Z',
            totalOrders: 12,
            totalRevenue: 24000000,
            totalDiscount: 120000,
            totalItemsSold: 38,
            totalNewCustomers: 4,
          },
          last7Days: [],
          currentMonth: null,
        } as T
      },
    }

    const result = await getDashboardReport(client)

    assert.deepEqual(calls, ['/api/reports/dashboard'])
    assert.equal(result.today?.totalOrders, 12)
    assert.equal(result.today?.totalRevenue, 24000000)
  })
})
