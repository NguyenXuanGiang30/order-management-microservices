import { apiClient } from '@/services/apiClient'
import type { PagedResponse } from './productInventoryApi'

export interface DailySalesSummary {
  id: string
  reportDate: string
  totalOrders: number
  totalRevenue: number
  totalDiscount: number
  totalItemsSold: number
  totalNewCustomers: number
}

export interface MonthlySalesSummary {
  id: string
  year: number
  month: number
  totalOrders: number
  totalRevenue: number
  totalDiscount: number
  totalItemsSold: number
  totalNewCustomers: number
}

export interface DashboardReport {
  today: DailySalesSummary | null
  last7Days: DailySalesSummary[]
  currentMonth: MonthlySalesSummary | null
  previousMonth?: MonthlySalesSummary | null
  last14Days?: DailySalesSummary[] | null
}

export interface TopProductDto {
  productId: string
  productCode: string
  productName: string
  quantitySold: number
  totalRevenue: number
}

export interface TopCustomerDto {
  customerId: string
  customerName: string
  ordersCount: number
  totalSpent: number
}

export interface MonthRevenueDto {
  month: number
  revenue: number
}

export interface DailyProfitDto {
  date: string
  revenue: number
  costOfGoods: number
  profit: number
}

export interface MonthlyProfitDto {
  month: number
  revenue: number
  costOfGoods: number
  profit: number
}

export interface ProductProfitDto {
  productId: string
  productCode: string
  productName: string
  revenue: number
  costOfGoods: number
  profit: number
  marginPercent: number
}

interface ReportsApiClient {
  get<T>(path: string, options?: { query?: Record<string, unknown> }): Promise<T>
}

export const getDashboardReport = (client: ReportsApiClient = apiClient) =>
  client.get<DashboardReport>('/api/reports/dashboard')

export const getTopProducts = (
  params: { limit?: number } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<TopProductDto[]>('/api/reports/top-products', {
    query: compactQuery({
      limit: params.limit ?? 10,
    }),
  })

export const getTopCustomers = (
  params: { limit?: number } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<TopCustomerDto[]>('/api/reports/top-customers', {
    query: compactQuery({
      limit: params.limit ?? 10,
    }),
  })

export const getRevenueByMonth = (
  year: number,
  client: ReportsApiClient = apiClient,
) =>
  client.get<MonthRevenueDto[]>('/api/reports/revenue-by-month', {
    query: { year },
  })

export const getDailyProfit = (
  params: { from?: string; to?: string } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<DailyProfitDto[]>('/api/reports/profit/daily', {
    query: compactQuery({
      from: params.from,
      to: params.to,
    }),
  })

export const getMonthlyProfit = (
  year?: number,
  client: ReportsApiClient = apiClient,
) =>
  client.get<MonthlyProfitDto[]>('/api/reports/profit/monthly', {
    query: compactQuery({ year }),
  })
export const getProductProfit = (
  params: { year?: number; month?: number; limit?: number; pageSize?: number } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<ProductProfitDto[]>('/api/reports/profit/products', {
    query: compactQuery({
      year: params.year,
      month: params.month,
      limit: params.limit ?? params.pageSize ?? 20,
    }),
  })

export const getDailyReports = (
  params: { from?: string; to?: string } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<DailySalesSummary[]>('/api/reports/daily', {
    query: compactQuery({
      from: params.from,
      to: params.to,
    }),
  })

export const getMonthlyReports = (
  year?: number,
  client: ReportsApiClient = apiClient,
) =>
  client.get<MonthlySalesSummary[]>('/api/reports/monthly', {
    query: compactQuery({
      year,
    }),
  })
export interface StaffPerformanceDto {
  staffId: string
  staffName: string
  totalOrders: number
  totalRevenue: number
  averageOrderValue: number
}

export const getStaffPerformance = (
  params: { from?: string; to?: string } = {},
  client: ReportsApiClient = apiClient,
) =>
  client.get<StaffPerformanceDto[]>('/api/orders/reports/staff-performance', {
    query: compactQuery({
      from: params.from,
      to: params.to,
    }),
  })

function compactQuery(query: Record<string, unknown>) {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== ''),
  )
}

