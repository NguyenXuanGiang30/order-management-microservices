import { apiClient } from '@/services/apiClient'

export interface PagedResponse<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ProductDto {
  id: string
  code: string
  name: string
  description: string | null
  barcode: string | null
  importPrice: number
  sellPrice: number
  imageUrl: string | null
  weight: number | null
  categoryId: string
  categoryName: string
  unitId: string
  unitName: string
  quantityOnHand: number
  quantityReserved: number
  isActive: boolean
  createdAt: string
}

export interface StockDto {
  productId: string
  productCode: string
  productName: string
  unitName: string
  quantityOnHand: number
  quantityReserved: number
  availableQuantity: number
  minThreshold: number
  maxThreshold: number
  isBelowMin: boolean
  alertLevel: string
  reorderQuantity: number
  recommendedOrderQuantity: number
  stockCoverageLabel: string
}

export interface GetProductsParams {
  search?: string
  categoryId?: string
  isActive?: boolean
  pageNumber?: number
  pageSize?: number
}

export interface GetInventoryStockParams {
  belowMin?: boolean
  search?: string
  aboveMax?: boolean
}

export interface CategoryDto {
  id: string
  name: string
  parentId: string | null
  parentName: string | null
}

export interface StocktakeLineDto {
  id: string
  productId: string
  productCode: string
  productName: string
  unitName: string
  systemQuantity: number
  actualQuantity: number
  differenceQuantity: number
  note: string | null
}

export interface StocktakeSessionDto {
  id: string
  code: string
  createdByName: string
  status: string
  note: string | null
  createdAt: string
  confirmedAt: string | null
  details: StocktakeLineDto[]
}

export interface InventoryTransactionDto {
  id: string
  productId: string
  productCode: string
  productName: string
  transactionType: string
  referenceId: string
  referenceCode: string
  changeQuantity: number
  balanceQuantity: number
  createdByName: string
  createdAt: string
}

interface ProductInventoryApiClient {
  get<T>(path: string, options?: { query?: Record<string, unknown> }): Promise<T>
  post<T>(path: string, body?: unknown): Promise<T>
  put<T>(path: string, body?: unknown): Promise<T>
  delete<T>(path: string): Promise<T>
}

export const getProducts = (
  params: GetProductsParams = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<ProductDto>>('/api/products', {
    query: compactQuery({
      search: params.search,
      categoryId: params.categoryId,
      pageNumber: params.pageNumber ?? 1,
      pageSize: params.pageSize ?? 20,
      isActive: params.isActive ?? true,
    }),
  })

export const createProduct = (
  payload: { code?: string; name: string; description?: string | null; barcode?: string | null; importPrice: number; sellPrice: number; imageUrl?: string | null; weight?: number | null; categoryId: string; unitId: string },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<ProductDto>('/api/products', payload)

export const updateProduct = (
  id: string,
  payload: { name: string; description?: string | null; barcode?: string | null; importPrice: number; sellPrice: number; imageUrl?: string | null; weight?: number | null; categoryId: string; unitId: string },
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/products/${id}`, payload)

export const toggleProductActive = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/products/${id}/toggle-active`)

export const getCategories = (
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<CategoryDto[]>('/api/categories')

export const createCategory = (
  payload: { name: string; parentId?: string | null },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<CategoryDto>('/api/categories', payload)

export const updateCategory = (
  id: string,
  payload: { name: string; parentId?: string | null },
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<CategoryDto>(`/api/categories/${id}`, payload)

export const deleteCategory = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'delete'> = apiClient,
) =>
  client.delete<boolean>(`/api/categories/${id}`)

export const getInventoryStock = (
  params: GetInventoryStockParams = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<StockDto[]>('/api/inventory/stock', {
    query: compactQuery({
      belowMin: params.belowMin,
      search: params.search,
      aboveMax: params.aboveMax,
    }),
  })

export interface GoodsReceiptDetailDto {
  id: string
  productId: string
  productCode: string
  productName: string
  unitName: string
  quantity: number
  unitPrice: number
  totalPrice: number
}

export interface GoodsReceiptDto {
  id: string
  receiptCode: string
  supplierId: string
  supplierName: string
  createdByName: string
  receiptDate: string
  note: string | null
  totalAmount: number
  status: string
  createdAt: string
  details: GoodsReceiptDetailDto[]
}

export interface GetGoodsReceiptsParams {
  status?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

export const getGoodsReceipts = (
  params: GetGoodsReceiptsParams = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<GoodsReceiptDto>>('/api/inventory/receipts', {
    query: compactQuery({
      status: params.status,
      from: params.from,
      to: params.to,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const createGoodsReceipt = (
  payload: { supplierId: string; note?: string | null; details: Array<{ productId: string; quantity: number; unitPrice: number }> },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<GoodsReceiptDto>('/api/inventory/receipts', payload)

export const confirmGoodsReceipt = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/inventory/receipts/${id}/confirm`)

export const cancelGoodsReceipt = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/inventory/receipts/${id}/cancel`)

export const getStocktakeSessions = (
  params: { status?: string; page?: number; pageSize?: number } = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<StocktakeSessionDto>>('/api/inventory/stocktakes', {
    query: compactQuery({
      status: params.status,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const getStocktakeSession = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<StocktakeSessionDto>(`/api/inventory/stocktakes/${id}`)

export const createStocktakeSession = (
  payload: { note?: string | null; details: Array<{ productId: string; actualQuantity: number; note?: string | null }> },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<StocktakeSessionDto>('/api/inventory/stocktakes', payload)

export const updateStocktakeLines = (
  id: string,
  lines: Array<{ productId: string; actualQuantity: number; note?: string | null }>,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/inventory/stocktakes/${id}/lines`, { lines })

export const confirmStocktake = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/inventory/stocktakes/${id}/confirm`)

export const cancelStocktake = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/inventory/stocktakes/${id}/cancel`)

export const getStocktakeTemplate = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<string>(`/api/inventory/stocktakes/${id}/template`)

export const importStocktakeCounts = (
  id: string,
  file: File,
  client: { post<T>(path: string, body?: unknown): Promise<T> } = apiClient,
) => {
  const formData = new FormData()

  formData.append('file', file)

  // We can bypass standard JSON headers in API client by letting fetch handle it with FormData
  return client.post<boolean>(`/api/inventory/stocktakes/${id}/import`, formData)
}

export interface ImportedReceiptItemResultDto {
  productId: string
  productCode: string
  productName: string
  unitName: string
  quantity: number
  importPrice: number
}

export const importGoodsReceiptItems = (
  file: File,
  client: { post<T>(path: string, body?: unknown): Promise<T> } = apiClient,
) => {
  const formData = new FormData()

  formData.append('file', file)

  return client.post<ImportedReceiptItemResultDto[]>('/api/inventory/receipts/import-items', formData)
}

export const getInventoryTransactions = (
  params: { productId?: string; search?: string; page?: number; pageSize?: number } = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<InventoryTransactionDto>>('/api/inventory/transactions', {
    query: compactQuery({
      productId: params.productId,
      search: params.search,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

function compactQuery(query: Record<string, unknown>) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}

export interface UnitDto {
  id: string
  name: string
  abbreviation: string | null
  isActive: boolean
}

export const getUnits = (
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<UnitDto[]>('/api/units')

export const createUnit = (
  payload: { name: string; abbreviation?: string | null },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<UnitDto>('/api/units', payload)

export interface ExportItemDto {
  productId: string
  quantity: number
  note: string | null
}

export interface InternalStockExportCommand {
  createdBy: string
  createdByName: string
  items: ExportItemDto[]
}

export const internalStockExport = (
  payload: InternalStockExportCommand,
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<boolean>('/api/inventory/export', payload)

export interface UnitConversionDto {
  id: string
  productId: string
  productName: string
  fromUnitId: string
  fromUnitName: string
  toUnitId: string
  toUnitName: string
  factor: number
}

export const getUnitConversions = (
  params: { productId?: string } = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<UnitConversionDto[]>('/api/unitconversions', {
    query: compactQuery({
      productId: params.productId,
    }),
  })

export const createUnitConversion = (
  payload: { productId: string; fromUnitId: string; toUnitId: string; factor: number },
  client: Pick<ProductInventoryApiClient, 'post'> = apiClient,
) =>
  client.post<UnitConversionDto>('/api/unitconversions', payload)

export const deleteUnitConversion = (
  id: string,
  client: Pick<ProductInventoryApiClient, 'delete'> = apiClient,
) =>
  client.delete<boolean>(`/api/unitconversions/${id}`)

export const getProductByBarcode = (
  barcode: string,
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<ProductDto>(`/api/products/barcode/${barcode}`)

export const uploadProductImage = (
  productId: string,
  file: File,
  client: { post: <T>(path: string, body?: unknown) => Promise<T> } = apiClient,
) => {
  const formData = new FormData()
  formData.append('file', file)
  return client.post<string>(`/api/products/${productId}/image`, formData)
}

export interface ProductPriceHistoryDto {
  id: string
  productId: string
  oldImportPrice: number
  newImportPrice: number
  oldSellPrice: number
  newSellPrice: number
  changedBy: string
  createdAt: string
}

export const getProductPriceHistory = (
  productId: string,
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<ProductPriceHistoryDto[]>(`/api/products/${productId}/price-history`)

export interface GetInventoryBalanceReportParams {
  startDate?: string
  endDate?: string
  search?: string
}

export interface InventoryBalanceReportDto {
  productId: string
  productCode: string
  productName: string
  unitName: string
  openingStock: number
  receivedQuantity: number
  shippedQuantity: number
  closingStock: number
}

export const getInventoryBalanceReport = (
  params: GetInventoryBalanceReportParams = {},
  client: Pick<ProductInventoryApiClient, 'get'> = apiClient,
) =>
  client.get<InventoryBalanceReportDto[]>('/api/inventory/balance-report', {
    query: compactQuery({
      startDate: params.startDate,
      endDate: params.endDate,
      search: params.search,
    }),
  })





