<script setup lang="ts">
import {
  type DashboardReport,
  type DailyProfitDto,
  type MonthlyProfitDto,
  type ProductProfitDto,
  type TopProductDto,
  type TopCustomerDto,
  type DailySalesSummary,
  getDashboardReport,
  getDailyProfit,
  getMonthlyProfit,
  getProductProfit,
  getTopProducts,
  getTopCustomers,
  getDailyReports,
} from '@/services/reportsApi'
import {
  type StockDto,
  getInventoryStock,
} from '@/services/productInventoryApi'
import {
  type ActivityLogDto,
  getActivityLogs,
} from '@/services/adminApi'

type MetricColor = 'success' | 'info' | 'warning' | 'error' | 'primary' | 'secondary'

interface DashboardMetric {
  label: string
  value: string
  helper: string
  icon: string
  color: MetricColor
}

const activeTab = ref(0)
const dashboard = ref<DashboardReport | null>(null)
const stockAlerts = ref<StockDto[]>([])
const activityLogs = ref<ActivityLogDto[]>([])
const loading = ref(false)
const errorMessage = ref('')

// Profit report states
const dailyProfitData = ref<DailyProfitDto[]>([])
const monthlyProfitData = ref<MonthlyProfitDto[]>([])
const productProfitData = ref<ProductProfitDto[]>([])

// Top items states
const topProducts = ref<TopProductDto[]>([])
const topCustomers = ref<TopCustomerDto[]>([])

// Daily sales report states
const dailySalesReports = ref<DailySalesSummary[]>([])

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatDateLabel = (value: string) => {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
    }).format(date)
}

const formatDateFull = (value: string) => {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    }).format(date)
}

const dashboardMetrics = computed<DashboardMetric[]>(() => {
  const today = dashboard.value?.today
  const currentMonth = dashboard.value?.currentMonth

  return [
    {
      label: 'Doanh thu hôm nay',
      value: formatCurrency(today?.totalRevenue ?? 0),
      helper: today ? `${formatNumber(today.totalOrders)} đơn trong ngày` : 'Chưa có dữ liệu hôm nay',
      icon: 'ri-cash-line',
      color: 'primary',
    },
    {
      label: 'Đơn hàng hôm nay',
      value: formatNumber(today?.totalOrders ?? 0),
      helper: today ? `${formatNumber(today.totalItemsSold)} sản phẩm đã bán` : 'Chờ đồng bộ dữ liệu bán hàng',
      icon: 'ri-shopping-cart-2-line',
      color: 'info',
    },
    {
      label: 'Doanh thu tháng',
      value: formatCurrency(currentMonth?.totalRevenue ?? 0),
      helper: currentMonth ? `${formatNumber(currentMonth.totalOrders)} đơn trong tháng` : 'Chưa có tổng hợp tháng',
      icon: 'ri-calendar-check-line',
      color: 'success',
    },
    {
      label: 'Khách mới tháng',
      value: formatNumber(currentMonth?.totalNewCustomers ?? 0),
      helper: currentMonth ? `${formatNumber(currentMonth.totalItemsSold)} sản phẩm đã bán` : 'Chưa có dữ liệu khách hàng',
      icon: 'ri-user-add-line',
      color: 'warning',
    },
  ]
})

const revenueChartItems = computed(() =>
  [...(dashboard.value?.last7Days ?? [])]
    .sort((a, b) => new Date(a.reportDate).getTime() - new Date(b.reportDate).getTime())
    .map(item => ({
      label: formatDateLabel(item.reportDate),
      value: item.totalRevenue,
    })),
)

const maxSales = computed(() => Math.max(...revenueChartItems.value.map(item => item.value), 1))

const loadDashboard = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    dashboard.value = await getDashboardReport()
    const [stockResult, logsResult] = await Promise.all([
      getInventoryStock({ belowMin: true }).catch(() => [] as StockDto[]),
      getActivityLogs({ pageSize: 10 }).catch(() => ({ items: [] as ActivityLogDto[], pageNumber: 1, pageSize: 10, totalCount: 0, totalPages: 0 })),
    ])
    stockAlerts.value = stockResult
    activityLogs.value = logsResult.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu dashboard.'
  }
  finally {
    loading.value = false
  }
}

const loadProfitReports = async () => {
  loading.value = true
  try {
    const [daily, monthly, products] = await Promise.all([
      getDailyProfit(),
      getMonthlyProfit(),
      getProductProfit({ pageSize: 50 }),
    ])
    dailyProfitData.value = daily
    monthlyProfitData.value = monthly
    productProfitData.value = products.items
  } catch (error) {
    console.error('Không thể tải báo cáo lợi nhuận:', error)
  } finally {
    loading.value = false
  }
}

const loadTopItems = async () => {
  loading.value = true
  try {
    const [prods, custs] = await Promise.all([
      getTopProducts({ limit: 15 }),
      getTopCustomers({ limit: 15 }),
    ])
    topProducts.value = prods
    topCustomers.value = custs
  } catch (error) {
    console.error('Không thể tải top items:', error)
  } finally {
    loading.value = false
  }
}

const loadDailyReportsList = async () => {
  loading.value = true
  try {
    const res = await getDailyReports({ pageSize: 50 })
    dailySalesReports.value = res.items
  } catch (error) {
    console.error('Không thể tải báo cáo doanh số ngày:', error)
  } finally {
    loading.value = false
  }
}

watch(activeTab, (val) => {
  if (val === 0) loadDashboard()
  else if (val === 1) loadProfitReports()
  else if (val === 2) loadTopItems()
  else if (val === 3) loadDailyReportsList()
})

onMounted(loadDashboard)
</script>

<template>
  <RetailPageHeader
    eyebrow="Trung tâm RetailOps"
    title="Tổng quan vận hành"
    subtitle="Báo cáo phân tích doanh thu, lợi nhuận, sản phẩm bán chạy và khách hàng mua nhiều nhất."
  >
    <template #actions>
      <div class="d-flex flex-wrap gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="activeTab === 0 ? loadDashboard() : (activeTab === 1 ? loadProfitReports() : (activeTab === 2 ? loadTopItems() : loadDailyReportsList()))"
        >
          Tải lại
        </VBtn>
        <VBtn
          prepend-icon="ri-add-line"
          to="/goods-receipts"
        >
          Tạo phiếu nhập
        </VBtn>
      </div>
    </template>
  </RetailPageHeader>

  <VAlert
    v-if="errorMessage"
    type="error"
    variant="tonal"
    class="mb-6"
    closable
    @click:close="errorMessage = ''"
  >
    {{ errorMessage }}
  </VAlert>

  <VTabs
    v-model="activeTab"
    class="mb-6"
  >
    <VTab :value="0">Tổng quan</VTab>
    <VTab :value="1">Báo cáo Lợi nhuận</VTab>
    <VTab :value="2">Xếp hạng bán hàng</VTab>
    <VTab :value="3">Doanh số theo ngày</VTab>
  </VTabs>

  <VWindow v-model="activeTab">
    <!-- Tab 0: Overview and standard metrics -->
    <VWindowItem :value="0">
      <VRow
        v-if="loading"
        class="match-height"
      >
        <VCol
          v-for="index in 4"
          :key="`dashboard-metric-skeleton-${index}`"
          cols="12"
          sm="6"
          lg="3"
        >
          <VCard>
            <VCardText>
              <VSkeletonLoader type="list-item-avatar-two-line" />
            </VCardText>
          </VCard>
        </VCol>

        <VCol
          cols="12"
          lg="8"
        >
          <VCard>
            <VCardText>
              <VSkeletonLoader type="heading, image, list-item-two-line" />
            </VCardText>
          </VCard>
        </VCol>

        <VCol
          cols="12"
          lg="4"
        >
          <VCard>
            <VCardText>
              <VSkeletonLoader type="heading, paragraph" />
            </VCardText>
          </VCard>
        </VCol>

        <VCol cols="12">
          <VCard>
            <VCardText>
              <VSkeletonLoader type="heading, table-tbody" />
            </VCardText>
          </VCard>
        </VCol>
      </VRow>

      <VRow
        v-else
        class="match-height"
      >
        <VCol
          v-for="metric in dashboardMetrics"
          :key="metric.label"
          cols="12"
          sm="6"
          lg="3"
        >
          <RetailMetricCard :metric="metric" />
        </VCol>

        <VCol
          cols="12"
          lg="8"
        >
          <VCard>
            <VCardItem>
              <VCardTitle>Doanh thu 7 ngày gần nhất</VCardTitle>
              <VCardSubtitle>Dữ liệu tổng hợp từ báo cáo doanh thu</VCardSubtitle>
            </VCardItem>

            <VCardText>
              <div
                v-if="revenueChartItems.length"
                class="retail-chart"
              >
                <div
                  v-for="item in revenueChartItems"
                  :key="item.label"
                  class="d-flex flex-column align-center justify-end h-100"
                >
                  <div
                    class="retail-chart-bar w-100"
                    :style="{ blockSize: `${Math.max(18, (item.value / maxSales) * 100)}%` }"
                  />
                  <div class="text-caption text-medium-emphasis mt-2">
                    {{ item.label }}
                  </div>
                </div>
              </div>

              <VAlert
                v-else
                type="info"
                variant="tonal"
              >
                Chưa có dữ liệu doanh thu 7 ngày gần nhất.
              </VAlert>
            </VCardText>
          </VCard>
        </VCol>

        <VCol
          cols="12"
          lg="4"
        >
          <VCard class="h-100">
            <VCardItem>
              <VCardTitle>Cảnh báo tồn kho</VCardTitle>
              <VCardSubtitle>SKU dưới ngưỡng an toàn</VCardSubtitle>
            </VCardItem>

            <VList v-if="stockAlerts.length">
              <VListItem
                v-for="stock in stockAlerts"
                :key="stock.productId"
              >
                <VListItemTitle>{{ stock.productName }}</VListItemTitle>
                <VListItemSubtitle>{{ stock.productCode }} · {{ stock.unitName }}</VListItemSubtitle>
                <template #append>
                  <VChip
                    :color="stock.alertLevel === 'Critical' ? 'error' : 'warning'"
                    size="small"
                    variant="tonal"
                  >
                    Tồn {{ stock.availableQuantity }}
                  </VChip>
                </template>
              </VListItem>
            </VList>

            <VCardText v-else>
              <VAlert
                type="success"
                variant="tonal"
              >
                Tất cả SKU đều trên ngưỡng an toàn.
              </VAlert>
            </VCardText>
          </VCard>
        </VCol>

        <VCol cols="12">
          <VCard>
            <VCardItem>
              <VCardTitle>Hoạt động gần đây</VCardTitle>
              <VCardSubtitle>10 hoạt động gần nhất trong hệ thống</VCardSubtitle>
            </VCardItem>

            <VTable
              v-if="activityLogs.length"
              class="retail-table"
            >
              <thead>
                <tr>
                  <th>Người dùng</th>
                  <th>Hành động</th>
                  <th>Đối tượng</th>
                  <th>Dịch vụ</th>
                  <th>Thời gian</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="log in activityLogs"
                  :key="log.id"
                >
                  <td>{{ log.userName }}</td>
                  <td>{{ log.action }}</td>
                  <td>{{ log.entityType }}</td>
                  <td>{{ log.serviceName }}</td>
                  <td>{{ new Date(log.createdAt).toLocaleString('vi-VN') }}</td>
                </tr>
              </tbody>
            </VTable>

            <VCardText v-else>
              <VAlert
                type="info"
                variant="tonal"
              >
                Chưa có hoạt động nào được ghi nhận.
              </VAlert>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>
    </VWindowItem>

    <!-- Tab 1: Profit report -->
    <VWindowItem :value="1">
      <VRow>
        <VCol cols="12" lg="6">
          <VCard>
            <VCardItem>
              <VCardTitle>Lợi nhuận gộp theo ngày</VCardTitle>
              <VCardSubtitle>So sánh Doanh thu - Giá vốn - Lợi nhuận thực tế</VCardSubtitle>
            </VCardItem>
            <VTable class="retail-table">
              <thead>
                <tr>
                  <th>Ngày</th>
                  <th class="text-right">Doanh thu</th>
                  <th class="text-right">Giá vốn</th>
                  <th class="text-right">Lợi nhuận</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="d in dailyProfitData" :key="d.date">
                  <td>{{ formatDateFull(d.date) }}</td>
                  <td class="text-right font-weight-bold text-primary">{{ formatCurrency(d.revenue) }}</td>
                  <td class="text-right text-medium-emphasis">{{ formatCurrency(d.costOfGoods) }}</td>
                  <td class="text-right font-weight-bold text-success">{{ formatCurrency(d.profit) }}</td>
                </tr>
                <tr v-if="!dailyProfitData.length">
                  <td colspan="4" class="text-center text-medium-emphasis py-6">Chưa có dữ liệu thống kê lợi nhuận ngày.</td>
                </tr>
              </tbody>
            </VTable>
          </VCard>
        </VCol>

        <VCol cols="12" lg="6">
          <VCard>
            <VCardItem>
              <VCardTitle>Tỷ suất lợi nhuận trên từng sản phẩm</VCardTitle>
              <VCardSubtitle>Xác định biên lợi nhuận % của từng sản phẩm đã bán</VCardSubtitle>
            </VCardItem>
            <VTable class="retail-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th class="text-right">Doanh thu</th>
                  <th class="text-right">Lợi nhuận</th>
                  <th class="text-right">Biên (%)</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="p in productProfitData" :key="p.productId">
                  <td class="font-weight-bold">{{ p.productName }}</td>
                  <td class="text-right">{{ formatCurrency(p.revenue) }}</td>
                  <td class="text-right text-success font-weight-bold">{{ formatCurrency(p.profit) }}</td>
                  <td class="text-right font-weight-bold">
                    <VChip size="small" :color="p.marginPercent > 30 ? 'success' : (p.marginPercent > 15 ? 'info' : 'warning')">
                      {{ p.marginPercent.toFixed(1) }}%
                    </VChip>
                  </td>
                </tr>
                <tr v-if="!productProfitData.length">
                  <td colspan="4" class="text-center text-medium-emphasis py-6">Chưa có dữ liệu thống kê sản phẩm.</td>
                </tr>
              </tbody>
            </VTable>
          </VCard>
        </VCol>
      </VRow>
    </VWindowItem>

    <!-- Tab 2: Ranking (Top products & customers) -->
    <VWindowItem :value="2">
      <VRow>
        <VCol cols="12" lg="6">
          <VCard>
            <VCardItem>
              <VCardTitle>Top 15 Sản phẩm bán chạy nhất</VCardTitle>
              <VCardSubtitle>Sản phẩm dẫn đầu doanh số và số lượng bán</VCardSubtitle>
            </VCardItem>
            <VTable class="retail-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th class="text-right">Đã bán</th>
                  <th class="text-right">Doanh thu mang lại</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(prod, i) in topProducts" :key="prod.productId">
                  <td class="font-weight-bold">
                    <span class="text-medium-emphasis mr-2">#{{ i + 1 }}</span>
                    {{ prod.productName }}
                  </td>
                  <td>{{ prod.productCode }}</td>
                  <td class="text-right font-weight-bold text-success">{{ formatNumber(prod.quantitySold) }}</td>
                  <td class="text-right font-weight-bold text-primary">{{ formatCurrency(prod.totalRevenue) }}</td>
                </tr>
                <tr v-if="!topProducts.length">
                  <td colspan="4" class="text-center text-medium-emphasis py-6">Chưa có dữ liệu xếp hạng sản phẩm.</td>
                </tr>
              </tbody>
            </VTable>
          </VCard>
        </VCol>

        <VCol cols="12" lg="6">
          <VCard>
            <VCardItem>
              <VCardTitle>Top 15 Khách hàng mua nhiều nhất</VCardTitle>
              <VCardSubtitle>Khách hàng VIP đóng góp nhiều doanh thu nhất</VCardSubtitle>
            </VCardItem>
            <VTable class="retail-table">
              <thead>
                <tr>
                  <th>Khách hàng</th>
                  <th class="text-center">Số đơn hàng</th>
                  <th class="text-right">Tổng chi tiêu</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(cust, i) in topCustomers" :key="cust.customerId">
                  <td class="font-weight-bold">
                    <span class="text-medium-emphasis mr-2">#{{ i + 1 }}</span>
                    {{ cust.customerName }}
                  </td>
                  <td class="text-center">{{ cust.ordersCount }}</td>
                  <td class="text-right font-weight-bold text-success">{{ formatCurrency(cust.totalSpent) }}</td>
                </tr>
                <tr v-if="!topCustomers.length">
                  <td colspan="3" class="text-center text-medium-emphasis py-6">Chưa có dữ liệu xếp hạng khách hàng.</td>
                </tr>
              </tbody>
            </VTable>
          </VCard>
        </VCol>
      </VRow>
    </VWindowItem>

    <!-- Tab 3: Daily Reports List -->
    <VWindowItem :value="3">
      <VCard>
        <VCardItem>
          <VCardTitle>Chi tiết báo cáo doanh số ngày</VCardTitle>
          <VCardSubtitle>Tổng hợp doanh thu, lượng giảm giá, số lượng đơn hàng hàng ngày</VCardSubtitle>
        </VCardItem>
        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Ngày báo cáo</th>
              <th class="text-center">Tổng số đơn hàng</th>
              <th class="text-center">Sản phẩm đã bán</th>
              <th class="text-right">Khách hàng mới</th>
              <th class="text-right">Tổng chiết khấu</th>
              <th class="text-right">Doanh thu ròng</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="rep in dailySalesReports" :key="rep.id">
              <td class="font-weight-bold text-primary">{{ formatDateFull(rep.reportDate) }}</td>
              <td class="text-center font-weight-bold">{{ rep.totalOrders }}</td>
              <td class="text-center">{{ rep.totalItemsSold }}</td>
              <td class="text-right">{{ rep.totalNewCustomers }}</td>
              <td class="text-right text-error">{{ formatCurrency(rep.totalDiscount) }}</td>
              <td class="text-right text-success font-weight-bold">{{ formatCurrency(rep.totalRevenue) }}</td>
            </tr>
            <tr v-if="!dailySalesReports.length">
              <td colspan="6" class="text-center text-medium-emphasis py-6">Chưa có báo cáo doanh số ngày nào được tạo.</td>
            </tr>
          </tbody>
        </VTable>
      </VCard>
    </VWindowItem>
  </VWindow>
</template>
