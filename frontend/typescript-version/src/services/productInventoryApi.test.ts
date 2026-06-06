import assert from 'node:assert/strict'
import { describe, it } from 'node:test'

import {
  cancelGoodsReceipt,
  confirmGoodsReceipt,
  getGoodsReceipts,
  getInventoryStock,
  getProducts,
} from './productInventoryApi'

describe('product inventory API', () => {
  it('loads paged products with filters', async () => {
    const calls: Array<{ path: string, query?: Record<string, unknown> }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'product-1',
              code: 'SKU-001',
              name: 'Áo thun',
              description: null,
              barcode: '893',
              importPrice: 80000,
              sellPrice: 159000,
              imageUrl: null,
              weight: null,
              categoryId: 'category-1',
              categoryName: 'Thời trang',
              unitId: 'unit-1',
              unitName: 'cái',
              quantityOnHand: 10,
              quantityReserved: 2,
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

    const result = await getProducts({ search: 'áo', pageNumber: 1 }, client)

    assert.equal(calls[0].path, '/api/products')
    assert.deepEqual(calls[0].query, {
      search: 'áo',
      pageNumber: 1,
      pageSize: 20,
      isActive: true,
    })
    assert.equal(result.items[0].code, 'SKU-001')
  })

  it('loads inventory stock with optional filters', async () => {
    const calls: Array<{ path: string, query?: Record<string, unknown> }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return [
          {
            productId: 'product-1',
            productCode: 'SKU-001',
            productName: 'Áo thun',
            unitName: 'cái',
            quantityOnHand: 10,
            quantityReserved: 2,
            availableQuantity: 8,
            minThreshold: 5,
            maxThreshold: 50,
            isBelowMin: false,
            alertLevel: 'Normal',
            reorderQuantity: 0,
            recommendedOrderQuantity: 0,
            stockCoverageLabel: 'Đủ hàng',
          },
        ] as T
      },
    }

    const result = await getInventoryStock({ belowMin: true, search: 'áo' }, client)

    assert.equal(calls[0].path, '/api/inventory/stock')
    assert.deepEqual(calls[0].query, {
      belowMin: true,
      search: 'áo',
    })
    assert.equal(result[0].availableQuantity, 8)
  })
  it('loads paged goods receipts with filters', async () => {
    const calls: Array<{ path: string, query?: Record<string, unknown> }> = []
    const client = {
      get: async <T>(path: string, options?: { query?: Record<string, unknown> }) => {
        calls.push({ path, query: options?.query })

        return {
          items: [
            {
              id: 'receipt-1',
              receiptCode: 'GRN-001',
              supplierId: 'supplier-1',
              supplierName: 'Metro Supply',
              createdByName: 'Admin RetailOps',
              receiptDate: '2026-06-01T08:00:00Z',
              note: null,
              totalAmount: 1000000,
              status: 'Draft',
              createdAt: '2026-06-01T08:00:00Z',
              details: [],
            },
          ],
          pageNumber: 2,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        } as T
      },
    }

    const result = await getGoodsReceipts({ status: 'Draft', page: 2 }, client)

    assert.equal(calls[0].path, '/api/inventory/receipts')
    assert.deepEqual(calls[0].query, {
      status: 'Draft',
      page: 2,
      pageSize: 20,
    })
    assert.equal(result.items[0].receiptCode, 'GRN-001')
  })

  it('confirms goods receipts', async () => {
    const calls: Array<{ path: string, body?: unknown }> = []
    const client = {
      put: async <T>(path: string, body?: unknown) => {
        calls.push({ path, body })

        return true as T
      },
    }

    const result = await confirmGoodsReceipt('receipt-1', client)

    assert.equal(calls[0].path, '/api/inventory/receipts/receipt-1/confirm')
    assert.equal(calls[0].body, undefined)
    assert.equal(result, true)
  })

  it('cancels goods receipts', async () => {
    const calls: Array<{ path: string, body?: unknown }> = []
    const client = {
      put: async <T>(path: string, body?: unknown) => {
        calls.push({ path, body })

        return true as T
      },
    }

    const result = await cancelGoodsReceipt('receipt-1', client)

    assert.equal(calls[0].path, '/api/inventory/receipts/receipt-1/cancel')
    assert.equal(calls[0].body, undefined)
    assert.equal(result, true)
  })
})
