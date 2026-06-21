import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import {
  createOrder,
  createPayment,
  getActivePromotions,
  getCurrentShift,
  getCustomers,
  getOrders,
  getSuppliers,
  openShift,
} from './orderSalesApi'

describe('order sales API', () => {
  it('loads paged orders with filters', async () => {
    const calls: Array<{ path: string; query?: Record<string, unknown> }> = []

    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'order-1',
              orderCode: 'ORD-001',
              customerId: 'customer-1',
              customerName: 'Nguyen Van A',
              createdByName: 'Admin RetailOps',
              orderDate: '2026-06-01T09:30:00Z',
              subTotal: 200000,
              discountPercent: 0,
              discountAmount: 0,
              promotionId: null,
              promotionCode: null,
              promotionName: null,
              promotionDiscountAmount: 0,
              finalAmount: 200000,
              paidAmount: 200000,
              debtAmount: 0,
              paymentMethod: 'Tiền mặt',
              status: 'Paid',
              note: null,
              createdAt: '2026-06-01T09:30:00Z',
              orderDetails: [],
            },
          ],
          pageNumber: 2,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        } as T
      },
    }

    const result = await getOrders({ search: 'ORD', status: 'Paid', page: 2 }, client)

    assert.equal(calls[0].path, '/api/orders')
    assert.deepEqual(calls[0].query, {
      search: 'ORD',
      status: 'Paid',
      page: 2,
      pageSize: 20,
      sortBy: 'CreatedAt',
      sortDescending: true,
    })
    assert.equal(result.items[0].orderCode, 'ORD-001')
  })

  it('posts create order payload to the orders endpoint', async () => {
    const calls: Array<{ path: string; body?: unknown }> = []

    const client = {
      post: async <T>(path: string, body?: unknown) => {
        calls.push({ path, body })

        return {
          id: 'order-1',
          orderCode: 'ORD-001',
          subTotal: 200000,
          promotionDiscountAmount: 0,
          finalAmount: 200000,
          promotionCode: null,
          promotionName: null,
        } as T
      },
    }

    const payload = {
      customerId: 'customer-1',
      customerName: 'Nguyen Van A',
      createdBy: 'user-1',
      createdByName: 'Admin RetailOps',
      paymentMethod: 'Tiền mặt',
      promotionCode: null,
      note: null,
      items: [
        {
          productId: 'product-1',
          productCode: 'SKU-001',
          productName: 'Ao thun',
          unitName: 'cai',
          unitPrice: 200000,
          quantity: 1,
          discountPercent: 0,
        },
      ],
    }

    const result = await createOrder(payload, client)

    assert.equal(calls[0].path, '/api/orders')
    assert.deepEqual(calls[0].body, payload)
    assert.equal(result.orderCode, 'ORD-001')
  })

  it('loads customers for POS selection', async () => {
    const calls: Array<{ path: string; query?: Record<string, unknown> }> = []

    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'customer-1',
              code: 'CUS-001',
              fullName: 'Nguyen Van A',
              phone: '0900000000',
              email: null,
              address: null,
              taxCode: null,
              customerGroupId: null,
              customerGroupName: null,
              totalPurchased: 0,
              debtAmount: 0,
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

    const result = await getCustomers({ search: 'Nguyen' }, client)

    assert.equal(calls[0].path, '/api/customers')
    assert.deepEqual(calls[0].query, {
      search: 'Nguyen',
      page: 1,
      pageSize: 20,
    })
    assert.equal(result.items[0].fullName, 'Nguyen Van A')
  })

  it('loads active promotions for POS selection', async () => {
    const calls: string[] = []

    const client = {
      get: async <T>(path: string) => {
        calls.push(path)

        return [
          {
            id: 'promotion-1',
            code: 'SALE10',
            name: 'Sale 10%',
            description: null,
            promotionType: 'Order',
            discountType: 'Percent',
            discountValue: 10,
            minimumOrderAmount: 100000,
            startAt: '2026-06-01T00:00:00Z',
            endAt: '2026-06-30T00:00:00Z',
            isActive: true,
            items: [],
          },
        ] as T
      },
    }

    const result = await getActivePromotions(client)

    assert.equal(calls[0], '/api/promotions/active')
    assert.equal(result[0].code, 'SALE10')
  })

  it('loads suppliers for supplier and debt pages', async () => {
    const calls: Array<{ path: string; query?: Record<string, unknown> }> = []

    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'supplier-1',
              code: 'SUP-001',
              name: 'Metro Supply',
              contactPerson: 'Nguyen Van B',
              contactPhone: '0900000001',
              contactEmail: null,
              address: null,
              taxCode: null,
              debtAmount: 4200000,
              note: 'Phu kien',
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

    const result = await getSuppliers({ search: 'Metro', page: 2 }, client)

    assert.equal(calls[0].path, '/api/suppliers')
    assert.deepEqual(calls[0].query, {
      search: 'Metro',
      page: 2,
      pageSize: 20,
    })
    assert.equal(result.items[0].name, 'Metro Supply')
  })

  it('loads the current POS shift', async () => {
    const calls: string[] = []

    const client = {
      get: async <T>(path: string) => {
        calls.push(path)

        return {
          id: 'shift-1',
          shiftCode: 'SHIFT-001',
          cashierId: 'user-1',
          cashierName: 'Admin RetailOps',
          openedAt: '2026-06-01T08:00:00Z',
          closedAt: null,
          openingCash: 0,
          actualCash: null,
          expectedCash: 0,
          variance: 0,
          status: 'Open',
          note: null,
        } as T
      },
    }

    const result = await getCurrentShift(client)

    assert.equal(calls[0], '/api/shifts/current')
    assert.equal(result?.shiftCode, 'SHIFT-001')
  })

  it('opens a POS shift', async () => {
    const calls: Array<{ path: string; body?: unknown }> = []

    const client = {
      post: async <T>(path: string, body?: unknown) => {
        calls.push({ path, body })

        return {
          id: 'shift-1',
          shiftCode: 'SHIFT-001',
          cashierId: 'user-1',
          cashierName: 'Admin RetailOps',
          openedAt: '2026-06-01T08:00:00Z',
          closedAt: null,
          openingCash: 0,
          actualCash: null,
          expectedCash: 0,
          variance: 0,
          status: 'Open',
          note: 'Open from POS',
        } as T
      },
    }

    const result = await openShift({ openingCash: 0, note: 'Open from POS' }, client)

    assert.equal(calls[0].path, '/api/shifts/open')
    assert.deepEqual(calls[0].body, { openingCash: 0, note: 'Open from POS' })
    assert.equal(result.status, 'Open')
  })

  it('posts customer payments to the payments endpoint', async () => {
    const calls: Array<{ path: string; body?: unknown }> = []

    const client = {
      post: async <T>(path: string, body?: unknown) => {
        calls.push({ path, body })

        return {
          id: 'payment-1',
          orderId: 'order-1',
          customerId: 'customer-1',
          amount: 200000,
          paymentMethod: 'Tiền mặt',
          note: null,
          receivedByName: 'Admin RetailOps',
          paymentDate: '2026-06-01T09:00:00Z',
        } as T
      },
    }

    const payload = {
      orderId: 'order-1',
      customerId: 'customer-1',
      amount: 200000,
      paymentMethod: 'Tiền mặt',
      note: null,
      receivedBy: 'user-1',
      receivedByName: 'Admin RetailOps',
    }

    const result = await createPayment(payload, client)

    assert.equal(calls[0].path, '/api/payments')
    assert.deepEqual(calls[0].body, payload)
    assert.equal(result.amount, 200000)
  })
})
