import { apiClient } from '@/services/apiClient'

export interface PagedResponse<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface OrderDetailDto {
  id: string
  productId: string
  productCode: string
  productName: string
  unitName: string
  unitPrice: number
  quantity: number
  discountPercent: number
  subTotal: number
}

export interface OrderDto {
  id: string
  orderCode: string
  customerId: string
  customerName: string
  createdByName: string
  orderDate: string
  subTotal: number
  discountPercent: number
  discountAmount: number
  promotionId: string | null
  promotionCode: string | null
  promotionName: string | null
  promotionDiscountAmount: number
  finalAmount: number
  paidAmount: number
  debtAmount: number
  paymentMethod: string | null
  status: string
  note: string | null
  createdAt: string
  orderDetails: OrderDetailDto[]
}

export interface CustomerDto {
  id: string
  code: string
  fullName: string
  phone: string | null
  email: string | null
  address: string | null
  taxCode: string | null
  customerGroupId: string | null
  customerGroupName: string | null
  totalPurchased: number
  debtAmount: number
  isActive: boolean
  createdAt: string
}

export interface PromotionItemDto {
  id: string
  productId: string
  productCode: string
  productName: string
  requiredQuantity: number
}

export interface PromotionDto {
  id: string
  code: string
  name: string
  description: string | null
  promotionType: string
  discountType: string
  discountValue: number
  minimumOrderAmount: number
  startAt: string
  endAt: string
  isActive: boolean
  items: PromotionItemDto[]
}

export interface GetOrdersParams {
  search?: string
  status?: string
  customerId?: string
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
}

export interface GetCustomersParams {
  search?: string
  page?: number
  pageSize?: number
}

export interface CreateOrderItemPayload {
  productId: string
  productCode: string
  productName: string
  unitName: string
  unitPrice: number
  quantity: number
  discountPercent: number
}

export interface CreateOrderPayload {
  customerId: string
  customerName: string
  createdBy: string
  createdByName: string
  paymentMethod: string | null
  promotionCode: string | null
  note: string | null
  items: CreateOrderItemPayload[]
}

export interface CreateOrderResponse {
  id: string
  orderCode: string
  subTotal: number
  promotionDiscountAmount: number
  finalAmount: number
  promotionCode: string | null
  promotionName: string | null
}

export interface CustomerGroupDto {
  id: string
  name: string
  description: string | null
  discountPercent: number
  createdAt: string
}

export interface CustomerDetailDto extends CustomerDto {
  orders: OrderDto[]
}

export interface CustomerDebtDto {
  customerId: string
  customerName: string
  debtAmount: number
  orders: OrderDto[]
  payments: PaymentDto[]
}

export interface OrderStatusHistoryDto {
  id: string
  orderId: string
  fromStatus: string | null
  toStatus: string
  note: string | null
  createdByName: string
  createdAt: string
}

export interface OrderInvoiceDto {
  orderCode: string
  customerName: string
  customerPhone: string | null
  customerAddress: string | null
  createdByName: string
  orderDate: string
  subTotal: number
  discountPercent: number
  discountAmount: number
  promotionDiscountAmount: number
  finalAmount: number
  paidAmount: number
  debtAmount: number
  paymentMethod: string | null
  note: string | null
  items: Array<{
    productName: string
    unitName: string
    unitPrice: number
    quantity: number
    discountPercent: number
    subTotal: number
  }>
}

interface OrderSalesApiClient {
  get<T>(path: string, options?: { query?: Record<string, unknown> }): Promise<T>
  post<T>(path: string, body?: unknown): Promise<T>
  put<T>(path: string, body?: unknown): Promise<T>
  delete<T>(path: string): Promise<T>
}

export const getOrders = (
  params: GetOrdersParams = {},
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<OrderDto>>('/api/orders', {
    query: compactQuery({
      search: params.search,
      status: params.status,
      customerId: params.customerId,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      sortBy: params.sortBy ?? 'CreatedAt',
      sortDescending: params.sortDescending ?? true,
    }),
  })

export const getOrderDetail = (
  id: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<OrderDto>(`/api/orders/${id}`)

export const createOrder = (
  payload: CreateOrderPayload,
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<CreateOrderResponse>('/api/orders', payload)

export const cancelOrder = (
  id: string,
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/orders/${id}/cancel`)

export const returnOrderItems = (
  id: string,
  payload: { items: Array<{ productId: string; quantity: number; note?: string | null }> },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<OrderDto>(`/api/orders/${id}/return`, payload)

export const getOrderInvoice = (
  id: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<OrderInvoiceDto>(`/api/orders/${id}/invoice`)

export const getOrderStatusHistory = (
  id: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<OrderStatusHistoryDto[]>(`/api/orders/${id}/status-history`)

export const getCustomers = (
  params: GetCustomersParams = {},
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<CustomerDto>>('/api/customers', {
    query: compactQuery({
      search: params.search,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const getCustomerDetails = (
  id: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<CustomerDetailDto>(`/api/customers/${id}`)

export const createCustomer = (
  payload: { code?: string; fullName: string; phone?: string | null; email?: string | null; address?: string | null; taxCode?: string | null; customerGroupId?: string | null },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<CustomerDto>('/api/customers', payload)

export const updateCustomer = (
  id: string,
  payload: { fullName: string; phone?: string | null; email?: string | null; address?: string | null; taxCode?: string | null; customerGroupId?: string | null },
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<boolean>(`/api/customers/${id}`, payload)

export const getCustomerDebt = (
  id: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<CustomerDebtDto>(`/api/customers/${id}/debt`)

export const getCustomerGroups = (
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<CustomerGroupDto[]>('/api/customer-groups')

export const createCustomerGroup = (
  payload: { name: string; description?: string | null; discountPercent: number },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<CustomerGroupDto>('/api/customer-groups', payload)

export const updateCustomerGroup = (
  id: string,
  payload: { name: string; description?: string | null; discountPercent: number },
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<CustomerGroupDto>(`/api/customer-groups/${id}`, payload)

export const deleteCustomerGroup = (
  id: string,
  client: Pick<OrderSalesApiClient, 'delete'> = apiClient,
) =>
  client.delete<boolean>(`/api/customer-groups/${id}`)

export const getActivePromotions = (
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PromotionDto[]>('/api/promotions/active')

export const getPromotions = (
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PromotionDto[]>('/api/promotions')

export const validatePromotion = (
  code: string,
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PromotionDto>(`/api/promotions/${code}/validate`)

export const createPromotion = (
  payload: { code: string; name: string; description?: string | null; promotionType: string; discountType: string; discountValue: number; minimumOrderAmount: number; startAt: string; endAt: string; items: Array<{ productId: string; productCode: string; productName: string; requiredQuantity: number }> },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<PromotionDto>('/api/promotions', payload)

export const updatePromotion = (
  id: string,
  payload: { code: string; name: string; description?: string | null; promotionType: string; discountType: string; discountValue: number; minimumOrderAmount: number; startAt: string; endAt: string; items: Array<{ productId: string; productCode: string; productName: string; requiredQuantity: number }> },
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<PromotionDto>(`/api/promotions/${id}`, payload)

export const togglePromotionActive = (
  id: string,
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<PromotionDto>(`/api/promotions/${id}/toggle-active`)

export interface SupplierDto {
  id: string
  code: string
  name: string
  contactPerson: string | null
  contactPhone: string | null
  contactEmail: string | null
  address: string | null
  taxCode: string | null
  debtAmount: number
  note: string | null
  createdAt: string
}

export interface GetSuppliersParams {
  search?: string
  page?: number
  pageSize?: number
}

export const getSuppliers = (
  params: GetSuppliersParams = {},
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<SupplierDto>>('/api/suppliers', {
    query: compactQuery({
      search: params.search,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export const createSupplier = (
  payload: { code?: string; name: string; contactPerson?: string | null; contactPhone?: string | null; contactEmail?: string | null; address?: string | null; taxCode?: string | null; note?: string | null },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<SupplierDto>('/api/suppliers', payload)

export const updateSupplier = (
  id: string,
  payload: { name: string; contactPerson?: string | null; contactPhone?: string | null; contactEmail?: string | null; address?: string | null; taxCode?: string | null; note?: string | null },
  client: Pick<OrderSalesApiClient, 'put'> = apiClient,
) =>
  client.put<SupplierDto>(`/api/suppliers/${id}`, payload)

export interface CashShiftDto {
  id: string
  shiftCode: string
  cashierId: string
  cashierName: string
  openedAt: string
  closedAt: string | null
  openingCash: number
  actualCash: number | null
  expectedCash: number
  totalCashSales: number
  totalTransferSales: number
  totalCardSales: number
  totalDebtSales: number
  status: string
  note: string | null
}

export const getCurrentShift = (
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<CashShiftDto | null>('/api/shifts/current')

export interface OpenShiftPayload {
  openingCash: number
  note?: string | null
}

export const openShift = (
  payload: OpenShiftPayload,
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<CashShiftDto>('/api/shifts/open', payload)

export const closeShift = (
  id: string,
  payload: { actualCash: number; note?: string | null },
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<CashShiftDto>(`/api/shifts/${id}/close`, payload)

export const getShifts = (
  params: { search?: string; page?: number; pageSize?: number } = {},
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<CashShiftDto>>('/api/shifts', {
    query: compactQuery({
      search: params.search,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

export interface PaymentDto {
  id: string
  orderId: string
  customerId: string
  amount: number
  paymentMethod: string
  note: string | null
  receivedByName: string
  paymentDate: string
}

export interface CreatePaymentPayload {
  orderId: string
  customerId: string
  amount: number
  paymentMethod: string
  note: string | null
  receivedBy: string
  receivedByName: string
}

export const createPayment = (
  payload: CreatePaymentPayload,
  client: Pick<OrderSalesApiClient, 'post'> = apiClient,
) =>
  client.post<PaymentDto>('/api/payments', payload)

export const getPayments = (
  params: { customerId?: string; orderId?: string; from?: string; page?: number; pageSize?: number } = {},
  client: Pick<OrderSalesApiClient, 'get'> = apiClient,
) =>
  client.get<PagedResponse<PaymentDto>>('/api/payments', {
    query: compactQuery({
      customerId: params.customerId,
      orderId: params.orderId,
      from: params.from,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    }),
  })

function compactQuery(query: Record<string, unknown>) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}
