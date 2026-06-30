<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'

import {
  type DailyProfitDto,
  type DailySalesSummary,
  type DashboardReport,
  type MonthlyProfitDto,
  type ProductProfitDto,
  type StaffPerformanceDto,
  type TopCustomerDto,
  type TopProductDto,
  getDailyProfit,
  getDailyReports,
  getDashboardReport,
  getMonthlyProfit,
  getProductProfit,
  getStaffPerformance,
  getTopCustomers,
  getTopProducts,
} from '@/services/reportsApi'

import {
  type GoodsReceiptDto,
  type StockDto,
  getGoodsReceipts,
  getInventoryStock,
} from '@/services/productInventoryApi'

import {
  type ActivityLogDto,
  getActivityLogs,
} from '@/services/adminApi'

import { useAuthStore } from '@/stores/auth'

type MetricColor = 'success' | 'info' | 'warning' | 'error' | 'primary' | 'secondary'

interface DashboardMetric {
  label: string
  value: string
  helper: string
  icon: string
  color: MetricColor
}

const authStore = useAuthStore()

const activeTab = ref(0)
const dashboard = ref<DashboardReport | null>(null)
const stockAlerts = ref<StockDto[]>([])
const activityLogs = ref<ActivityLogDto[]>([])
const recentReceipts = ref<GoodsReceiptDto[]>([])

const dailyProfitData = ref<DailyProfitDto[]>([])
const monthlyProfitData = ref<MonthlyProfitDto[]>([])
const productProfitData = ref<ProductProfitDto[]>([])

const topProducts = ref<TopProductDto[]>([])
const topCustomers = ref<TopCustomerDto[]>([])
const dailySalesReports = ref<DailySalesSummary[]>([])
const staffPerformanceData = ref<StaffPerformanceDto[]>([])

const loading = ref(false)
const errorMessage = ref('')

const tabLoaded = ref<Record<number, boolean>>({})
const componentAlive = ref(true)
let requestVersion = 0

onBeforeUnmount(() => {
  componentAlive.value = false
  requestVersion += 1
  loading.value = false
})

const hasPermission = (code: string) => {
  return authStore.user?.permissions?.includes(code) || authStore.user?.role === 'Admin'
}

const availableTabs = computed(() => {
  const tabs = [
    { value: 0, title: 'Tổng quan', icon: 'ri-dashboard-3-line', perm: 'dashboard.read' },
    { value: 1, title: 'Báo cáo lợi nhuận', icon: 'ri-line-chart-line', perm: 'reports.profit' },
    { value: 2, title: 'Xếp hạng bán hàng', icon: 'ri-trophy-line', perm: 'reports.read' },
    { value: 3, title: 'Doanh số theo ngày', icon: 'ri-calendar-event-line', perm: 'reports.read' },
    { value: 4, title: 'Hiệu suất nhân viên', icon: 'ri-user-star-line', perm: 'reports.read' },
  ]

  return tabs.filter(tab => hasPermission(tab.perm))
})

const activeTabMeta = computed(() =>
  availableTabs.value.find(tab => tab.value === activeTab.value),
)

const roleLabel = computed(() => {
  if (authStore.user?.role === 'Warehouse')
    return 'Không gian kho hàng'

  if (authStore.user?.role === 'Sales')
    return 'Không gian bán hàng'

  return 'Không gian quản trị'
})

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatCompactCurrency = (value: number) => {
  const abs = Math.abs(value)

  if (abs >= 1_000_000_000)
    return `${(value / 1_000_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} tỷ`

  if (abs >= 1_000_000)
    return `${(value / 1_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} tr`

  if (abs >= 1_000)
    return `${Math.round(value / 1_000).toLocaleString('vi-VN')}K`

  return formatNumber(value)
}

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

const formatDateTime = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleString('vi-VN')
}

const dashboardMetrics = computed<DashboardMetric[]>(() => {
  const today = dashboard.value?.today
  const currentMonth = dashboard.value?.currentMonth
  const role = authStore.user?.role

  if (role === 'Warehouse') {
    return [
      {
        label: 'Tồn kho cảnh báo',
        value: formatNumber(stockAlerts.value.length),
        helper: 'SKU dưới tối thiểu hoặc vượt định mức',
        icon: 'ri-archive-line',
        color: 'error',
      },
      {
        label: 'Sản phẩm đã bán',
        value: formatNumber(today?.totalItemsSold ?? 0),
        helper: 'Số lượng sản phẩm bán ra hôm nay',
        icon: 'ri-price-tag-3-line',
        color: 'info',
      },
    ]
  }

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
      helper: today ? `${formatNumber(today.totalItemsSold)} sản phẩm đã bán` : 'Chờ đồng bộ bán hàng',
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

const chartBox = {
  width: 720,
  height: 300,
  top: 28,
  right: 24,
  bottom: 54,
  left: 72,
}

const chartPlotWidth = chartBox.width - chartBox.left - chartBox.right
const chartPlotHeight = chartBox.height - chartBox.top - chartBox.bottom

const revenueChartMax = computed(() => {
  const rawMax = Math.max(...revenueChartItems.value.map(item => item.value), 1)

  if (rawMax >= 1_000_000_000)
    return Math.ceil(rawMax / 100_000_000) * 100_000_000

  if (rawMax >= 100_000_000)
    return Math.ceil(rawMax / 10_000_000) * 10_000_000

  if (rawMax >= 10_000_000)
    return Math.ceil(rawMax / 1_000_000) * 1_000_000

  if (rawMax >= 1_000_000)
    return Math.ceil(rawMax / 100_000) * 100_000

  return Math.ceil(rawMax / 10_000) * 10_000
})

const revenueChartPoints = computed(() => {
  const items = revenueChartItems.value
  const lastIndex = Math.max(items.length - 1, 1)
  const maxValue = revenueChartMax.value || 1

  return items.map((item, index) => {
    const x = chartBox.left + (index / lastIndex) * chartPlotWidth
    const y = chartBox.top + (1 - item.value / maxValue) * chartPlotHeight

    return {
      ...item,
      x,
      y,
      formatted: formatCurrency(item.value),
      compact: formatCompactCurrency(item.value),
    }
  })
})

const revenueLinePath = computed(() => {
  const points = revenueChartPoints.value

  if (!points.length)
    return ''

  return points
    .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
    .join(' ')
})

const revenueAreaPath = computed(() => {
  const points = revenueChartPoints.value

  if (!points.length)
    return ''

  const bottomY = chartBox.top + chartPlotHeight
  const firstPoint = points[0]
  const lastPoint = points[points.length - 1]

  return `${revenueLinePath.value} L ${lastPoint.x.toFixed(2)} ${bottomY} L ${firstPoint.x.toFixed(2)} ${bottomY} Z`
})

const revenueYAxisTicks = computed(() => {
  const maxValue = revenueChartMax.value || 1

  return [1, 0.75, 0.5, 0.25, 0].map(ratio => ({
    value: maxValue * ratio,
    y: chartBox.top + (1 - ratio) * chartPlotHeight,
    label: ratio === 0 ? '0' : formatCompactCurrency(maxValue * ratio),
  }))
})

const revenuePeakPoint = computed(() => {
  const points = revenueChartPoints.value

  if (!points.length)
    return null

  return points.reduce((best, point) =>
    point.value > best.value ? point : best,
    points[0])
})

const revenueChartSummary = computed(() => {
  const items = revenueChartItems.value

  if (!items.length)
    return null

  const total = items.reduce((sum, item) => sum + item.value, 0)
  const average = total / items.length
  const peak = items.reduce((best, item) =>
    item.value > best.value ? item : best,
    items[0])

  return {
    total: formatCurrency(total),
    average: formatCurrency(average),
    peak: `${peak.label} · ${formatCurrency(peak.value)}`,
  }
})

const periodComparison = computed(() => {
  const last14Days = dashboard.value?.last14Days ?? []
  const currentMonth = dashboard.value?.currentMonth
  const previousMonth = dashboard.value?.previousMonth

  const sortedDays = [...last14Days].sort((a, b) =>
    new Date(b.reportDate).getTime() - new Date(a.reportDate).getTime(),
  )

  const todayRevenue = sortedDays[0]?.totalRevenue ?? 0
  const yesterdayRevenue = sortedDays[1]?.totalRevenue ?? 0

  const todayDiffPercent = yesterdayRevenue > 0
    ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100
    : 0

  const thisWeekDays = sortedDays.slice(0, 7)
  const lastWeekDays = sortedDays.slice(7, 14)

  const thisWeekRevenue = thisWeekDays.reduce((sum, item) => sum + item.totalRevenue, 0)
  const lastWeekRevenue = lastWeekDays.reduce((sum, item) => sum + item.totalRevenue, 0)

  const weekDiffPercent = lastWeekRevenue > 0
    ? ((thisWeekRevenue - lastWeekRevenue) / lastWeekRevenue) * 100
    : 0

  const thisMonthRevenue = currentMonth?.totalRevenue ?? 0
  const lastMonthRevenue = previousMonth?.totalRevenue ?? 0

  const monthDiffPercent = lastMonthRevenue > 0
    ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
    : 0

  const half = Math.floor(sortedDays.length / 2)
  const firstHalf = sortedDays.slice(0, half)
  const secondHalf = sortedDays.slice(half)

  const firstHalfRev = firstHalf.reduce((sum, item) => sum + item.totalRevenue, 0)
  const secondHalfRev = secondHalf.reduce((sum, item) => sum + item.totalRevenue, 0)

  const periodDiffPercent = secondHalfRev > 0
    ? ((firstHalfRev - secondHalfRev) / secondHalfRev) * 100
    : 0

  return {
    todayRevenue,
    yesterdayRevenue,
    todayDiffPercent,
    thisWeekRevenue,
    lastWeekRevenue,
    weekDiffPercent,
    thisMonthRevenue,
    lastMonthRevenue,
    monthDiffPercent,
    firstHalfRev,
    secondHalfRev,
    periodDiffPercent,
  }
})

const comparisonCards = computed(() => [
  {
    label: 'Hôm nay vs hôm qua',
    value: formatCurrency(periodComparison.value.todayRevenue),
    helper: `Hôm qua: ${formatCurrency(periodComparison.value.yesterdayRevenue)}`,
    percent: periodComparison.value.todayDiffPercent,
  },
  {
    label: '3 ngày gần đây',
    value: formatCurrency(periodComparison.value.firstHalfRev),
    helper: `Kỳ trước: ${formatCurrency(periodComparison.value.secondHalfRev)}`,
    percent: periodComparison.value.periodDiffPercent,
  },
  {
    label: 'Tuần này',
    value: formatCurrency(periodComparison.value.thisWeekRevenue),
    helper: `Tuần trước: ${formatCurrency(periodComparison.value.lastWeekRevenue)}`,
    percent: periodComparison.value.weekDiffPercent,
  },
  {
    label: 'Tháng này',
    value: formatCurrency(periodComparison.value.thisMonthRevenue),
    helper: `Tháng trước: ${formatCurrency(periodComparison.value.lastMonthRevenue)}`,
    percent: periodComparison.value.monthDiffPercent,
  },
])
const runDashboardRequest = async (
  tabValue: number,
  task: () => Promise<void>,
  options: { force?: boolean } = {},
) => {
  if (!options.force && tabLoaded.value[tabValue])
    return

  const currentRequest = ++requestVersion

  loading.value = true

  try {
    await task()

    if (!componentAlive.value || currentRequest !== requestVersion)
      return

    tabLoaded.value[tabValue] = true
  }
  catch (error) {
    if (!componentAlive.value || currentRequest !== requestVersion)
      return

    if (tabValue === 0) {
      errorMessage.value = error instanceof Error
        ? error.message
        : 'Không thể tải dữ liệu dashboard.'
    }
    else {
      console.error('Không thể tải dữ liệu báo cáo:', error)
    }
  }
  finally {
    if (componentAlive.value && currentRequest === requestVersion)
      loading.value = false
  }
}
const loadDashboard = async (force = false) => {
  errorMessage.value = ''

  return runDashboardRequest(0, async () => {
    dashboard.value = await getDashboardReport()

    const [lowStockResult, overStockResult, logsResult] = await Promise.all([
      getInventoryStock({ belowMin: true }).catch(() => [] as StockDto[]),
      getInventoryStock({ aboveMax: true }).catch(() => [] as StockDto[]),
      getActivityLogs({ pageSize: 10 }).catch(() => ({
        items: [] as ActivityLogDto[],
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0,
      })),
    ])

    stockAlerts.value = [...lowStockResult, ...overStockResult]
    activityLogs.value = logsResult.items

    if (authStore.user?.role === 'Warehouse') {
      const receiptsRes = await getGoodsReceipts({ pageSize: 10 }).catch(() => ({
        items: [] as GoodsReceiptDto[],
        page: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0,
      }))

      recentReceipts.value = receiptsRes.items
    }
  }, { force })
}

const loadProfitReports = async (force = false) => {
  return runDashboardRequest(1, async () => {
    const [daily, monthly, products] = await Promise.all([
      getDailyProfit(),
      getMonthlyProfit(),
      getProductProfit({ pageSize: 50 }),
    ])

    dailyProfitData.value = daily ?? []
    monthlyProfitData.value = monthly ?? []
    productProfitData.value = products ?? []
  }, { force })
}

const loadTopItems = async (force = false) => {
  return runDashboardRequest(2, async () => {
    const [products, customers] = await Promise.all([
      getTopProducts({ limit: 15 }),
      getTopCustomers({ limit: 15 }),
    ])

    topProducts.value = products ?? []
    topCustomers.value = customers ?? []
  }, { force })
}

const loadDailyReportsList = async (force = false) => {
  return runDashboardRequest(3, async () => {
    const result = await getDailyReports()

    dailySalesReports.value = result ?? []
  }, { force })
}

const loadStaffPerformance = async (force = false) => {
  return runDashboardRequest(4, async () => {
    staffPerformanceData.value = await getStaffPerformance()
  }, { force })
}

const refreshCurrentTab = () => {
  if (activeTab.value === 0)
    return loadDashboard()

  if (activeTab.value === 1)
    return loadProfitReports()

  if (activeTab.value === 2)
    return loadTopItems()

  if (activeTab.value === 3)
    return loadDailyReportsList()

  return loadStaffPerformance()
}

watch(activeTab, value => {
  if (value === 0)
    loadDashboard()
  else if (value === 1)
    loadProfitReports()
  else if (value === 2)
    loadTopItems()
  else if (value === 3)
    loadDailyReportsList()
  else if (value === 4)
    loadStaffPerformance()
})

const handleExportCsv = () => {
  let headers = ''
  let rows: string[] = []
  let filename = 'dashboard-report.csv'

  if (activeTab.value === 0) {
    const last7Days = dashboard.value?.last7Days ?? []

    headers = 'Ngày báo cáo,Tổng đơn hàng,Số sản phẩm đã bán,Doanh thu,Khách hàng mới'
    rows = last7Days.map(day =>
      `${day.reportDate.split('T')[0]},${day.totalOrders},${day.totalItemsSold},${day.totalRevenue},${day.totalNewCustomers}`,
    )
    filename = 'bao-cao-tong-quan-7-ngay.csv'
  }
  else if (activeTab.value === 1) {
    headers = 'Mã sản phẩm,Tên sản phẩm,Doanh thu,Vốn hàng bán,Lợi nhuận,Tỷ suất lợi nhuận (%)'
    rows = productProfitData.value.map(product =>
      `${product.productCode},"${product.productName.replace(/"/g, '""')}",${product.revenue},${product.costOfGoods},${product.profit},${product.marginPercent}`,
    )
    filename = 'bao-cao-loi-nhuan-san-pham.csv'
  }
  else if (activeTab.value === 2) {
    headers = 'Mã sản phẩm,Tên sản phẩm,Số lượng bán,Doanh thu'
    rows = topProducts.value.map(product =>
      `${product.productCode},"${product.productName.replace(/"/g, '""')}",${product.quantitySold},${product.totalRevenue}`,
    )
    filename = 'xep-hang-san-pham-ban-chay.csv'
  }
  else if (activeTab.value === 3) {
    headers = 'Ngày báo cáo,Tổng đơn hàng,Số sản phẩm đã bán,Doanh thu,Khách hàng mới'
    rows = dailySalesReports.value.map(day =>
      `${day.reportDate.split('T')[0]},${day.totalOrders},${day.totalItemsSold},${day.totalRevenue},${day.totalNewCustomers}`,
    )
    filename = 'bao-cao-doanh-so-theo-ngay.csv'
  }
  else if (activeTab.value === 4) {
    headers = 'ID Nhân viên,Tên nhân viên,Tổng doanh số,Số đơn hàng,Giá trị đơn trung bình'
    rows = staffPerformanceData.value.map(staff =>
      `"${staff.staffId}","${staff.staffName.replace(/"/g, '""')}",${staff.totalRevenue},${staff.totalOrders},${staff.averageOrderValue}`,
    )
    filename = 'bao-cao-hieu-suat-nhan-vien.csv'
  }

  if (!rows.length) {
    alert('Không có dữ liệu để xuất!')

    return
  }

  const csvContent = `\uFEFF${[headers, ...rows].join('\n')}`
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.setAttribute('href', url)
  link.setAttribute('download', filename)
  link.style.visibility = 'hidden'

  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

onMounted(loadDashboard)
</script>
<template>
  <section class="dashboard-page">
    <div class="dashboard-shell">
      <div class="dashboard-hero">
        <div class="dashboard-hero__title-area">
        <h1>Tổng quan</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-pulse-line" class="ml-2">
          {{ roleLabel }}
        </VChip>
      </div>
        <div class="dashboard-actions">
          <VBtn variant="outlined" class="dashboard-action-btn" prepend-icon="ri-download-line"
            @click="handleExportCsv"> Xuất CSV </VBtn>
          <VBtn variant="tonal" class="dashboard-action-btn" prepend-icon="ri-refresh-line" :loading="loading"
            @click="refreshCurrentTab"> Tải lại </VBtn>
          <VBtn class="dashboard-primary-btn" prepend-icon="ri-add-line" to="/goods-receipts"> Tạo phiếu nhập </VBtn>
        </div>
      </div>
      <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-6" closable @click:close="errorMessage = ''">
        {{ errorMessage }} </VAlert>
      <VCard class="dashboard-tabs-card mb-6">
        <VCardText>
          <div class="dashboard-tabs-head">
            <div>
              <span>Báo cáo</span>
              <strong>{{ activeTabMeta?.title ?? 'Tổng quan' }}</strong>
            </div>

            <div class="active-tab-badge">
              <VIcon :icon="activeTabMeta?.icon ?? 'ri-dashboard-3-line'" size="18" />
              <span>Đang xem: {{ activeTabMeta?.title ?? 'Tổng quan' }}</span>
            </div>
          </div>

          <div class="dashboard-tabs-scroll">
            <div class="dashboard-tabs-custom" role="tablist" :style="{ '--tab-count': availableTabs.length }">
              <button v-for="tab in availableTabs" :key="tab.value" type="button" class="dashboard-tab-button"
                :class="{ 'dashboard-tab-button--active': activeTab === tab.value }"
                :aria-selected="activeTab === tab.value" role="tab" @click="activeTab = tab.value">
                <VIcon :icon="tab.icon" size="20" class="dashboard-tab-icon" />
                <span>{{ tab.title }}</span>
              </button>
            </div>
          </div>
        </VCardText>
      </VCard>
    </div>
    <VWindow v-model="activeTab"> <!-- TAB 0 -->
      <VWindowItem :value="0">
        <VRow v-if="loading" class="match-height">
          <VCol v-for="index in 4" :key="`dashboard-metric-skeleton-${index}`" cols="12" sm="6" lg="3">
            <VCard class="dashboard-panel">
              <VCardText>
                <VSkeletonLoader type="list-item-avatar-two-line" />
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="12" lg="8">
            <VCard class="dashboard-panel">
              <VCardText>
                <VSkeletonLoader type="heading, image, list-item-two-line" />
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="12" lg="4">
            <VCard class="dashboard-panel">
              <VCardText>
                <VSkeletonLoader type="heading, paragraph" />
              </VCardText>
            </VCard>
          </VCol>
        </VRow>
        <VRow v-else class="match-height">
          <VCol v-for="metric in dashboardMetrics" :key="metric.label" cols="12" sm="6" lg="3">
            <RetailMetricCard :metric="metric" />
          </VCol>
          <VCol v-if="periodComparison && authStore.user?.role !== 'Warehouse'" cols="12">
            <VCard class="dashboard-panel comparison-panel">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Revenue insight </div>
                    <VCardTitle>Phân tích biến động doanh thu</VCardTitle>
                    <VCardSubtitle>So sánh hiệu suất giữa các kỳ gần nhất</VCardSubtitle>
                  </div>
                  <VAvatar rounded="lg" class="panel-icon">
                    <VIcon icon="ri-line-chart-line" />
                  </VAvatar>
                </div>
              </VCardItem>
              <VCardText>
                <div class="comparison-grid">
                  <article v-for="item in comparisonCards" :key="item.label" class="comparison-card">
                    <div> <span>{{ item.label }}</span> <strong>{{ item.value }}</strong>
                      <p>{{ item.helper }}</p>
                    </div>
                    <VChip :color="item.percent >= 0 ? 'success' : 'error'" variant="tonal" size="small"
                      :prepend-icon="item.percent >= 0 ? 'ri-arrow-up-line' : 'ri-arrow-down-line'"> {{ item.percent >=
                        0 ? '+' : '' }}{{ item.percent.toFixed(1) }}% </VChip>
                  </article>
                </div>
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="12" lg="8">
            <VCard v-if="authStore.user?.role !== 'Warehouse'" class="dashboard-panel">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Doanh thu </div>
                    <VCardTitle>Doanh thu 7 ngày gần nhất</VCardTitle>
                    <VCardSubtitle>Dữ liệu tổng hợp từ báo cáo doanh thu</VCardSubtitle>
                  </div>
                  <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-bar-chart-grouped-line"> 7 ngày
                  </VChip>
                </div>
              </VCardItem>
              <VCardText>
                <div v-if="revenueChartItems.length" class="revenue-visual">
                  <div v-if="revenueChartSummary" class="revenue-summary-strip">
                    <div class="summary-tile"> <span>Tổng 7 ngày</span> <strong>{{ revenueChartSummary.total }}</strong>
                    </div>
                    <div class="summary-tile"> <span>Trung bình/ngày</span> <strong>{{ revenueChartSummary.average
                    }}</strong> </div>
                    <div class="summary-tile"> <span>Cao nhất</span> <strong>{{ revenueChartSummary.peak }}</strong>
                    </div>
                  </div>
                  <div class="revenue-chart-card"> <svg class="revenue-svg" viewBox="0 0 720 300" role="img"
                      aria-label="Biểu đồ doanh thu 7 ngày gần nhất">
                      <defs>
                        <linearGradient id="revenueAreaGradient" x1="0" x2="0" y1="0" y2="1">
                          <stop offset="0%" stop-color="rgb(var(--v-theme-primary))" stop-opacity="0.28" />
                          <stop offset="72%" stop-color="rgb(var(--v-theme-info))" stop-opacity="0.06" />
                          <stop offset="100%" stop-color="rgb(var(--v-theme-info))" stop-opacity="0" />
                        </linearGradient>
                        <linearGradient id="revenueLineGradient" x1="0" x2="1" y1="0" y2="0">
                          <stop offset="0%" stop-color="rgb(var(--v-theme-primary))" />
                          <stop offset="52%" stop-color="rgb(var(--v-theme-info))" />
                          <stop offset="100%" stop-color="rgb(var(--v-theme-success))" />
                        </linearGradient>
                        <filter id="revenueGlow" x="-20%" y="-20%" width="140%" height="140%">
                          <feGaussianBlur stdDeviation="4" result="blur" />
                          <feMerge>
                            <feMergeNode in="blur" />
                            <feMergeNode in="SourceGraphic" />
                          </feMerge>
                        </filter>
                      </defs>
                      <g class="chart-grid-lines">
                        <g v-for="tick in revenueYAxisTicks" :key="tick.label">
                          <line x1="72" x2="696" :y1="tick.y" :y2="tick.y" /> <text x="20" :y="tick.y + 4"> {{
                            tick.label }} </text>
                        </g>
                      </g>
                      <path class="revenue-area" :d="revenueAreaPath" />
                      <path class="revenue-line" :d="revenueLinePath" />
                      <g v-if="revenuePeakPoint" class="peak-marker">
                        <line :x1="revenuePeakPoint.x" :x2="revenuePeakPoint.x" y1="28" :y2="revenuePeakPoint.y" />
                        <text :x="revenuePeakPoint.x" :y="Math.max(revenuePeakPoint.y - 14, 18)" text-anchor="middle">
                          {{ revenuePeakPoint.compact }} </text>
                      </g>
                      <g v-for="point in revenueChartPoints" :key="point.label" class="revenue-point">
                        <line class="revenue-hover-line" :x1="point.x" :x2="point.x" y1="28" y2="246" />
                        <circle class="revenue-point-ring" :cx="point.x" :cy="point.y" r="9" />
                        <circle class="revenue-point-core" :cx="point.x" :cy="point.y" r="4.5" />
                        <title>{{ point.label }}: {{ point.formatted }}</title>
                      </g>
                      <g class="chart-x-labels"> <text v-for="point in revenueChartPoints" :key="`label-${point.label}`"
                          :x="point.x" y="284" text-anchor="middle"> {{ point.label }} </text> </g>
                    </svg> </div>
                </div>
                <VAlert v-else type="info" variant="tonal"> Chưa có dữ liệu doanh thu 7 ngày gần nhất. </VAlert>
              </VCardText>
            </VCard>
            <VCard v-else class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Nhập kho </div>
                    <VCardTitle>Đơn nhập hàng gần đây</VCardTitle>
                    <VCardSubtitle>10 phiếu nhập hàng mới nhất được tạo trong hệ thống</VCardSubtitle>
                  </div>
                  <VChip color="info" variant="tonal" size="small" prepend-icon="ri-truck-line"> Kho hàng </VChip>
                </div>
              </VCardItem>
              <VTable v-if="recentReceipts.length" class="retail-table">
                <thead>
                  <tr>
                    <th>Mã phiếu</th>
                    <th>Nhà cung cấp</th>
                    <th>Người tạo</th>
                    <th class="text-right">Tổng tiền</th>
                    <th class="text-center">Trạng thái</th>
                    <th>Ngày nhập</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="receipt in recentReceipts" :key="receipt.id">
                    <td class="font-weight-bold text-primary"> {{ receipt.receiptCode }} </td>
                    <td>{{ receipt.supplierName }}</td>
                    <td>{{ receipt.createdByName }}</td>
                    <td class="text-right font-weight-bold text-success"> {{ formatCurrency(receipt.totalAmount) }}
                    </td>
                    <td class="text-center">
                      <VChip
                        :color="receipt.status === 'Confirmed' ? 'success' : receipt.status === 'Cancelled' ? 'error' : 'warning'"
                        size="small" variant="tonal"> {{ receipt.status === 'Confirmed' ? 'Đã nhập' : receipt.status ===
                          'Cancelled' ? 'Đã hủy' : 'Tạm' }} </VChip>
                    </td>
                    <td>{{ formatDateFull(receipt.receiptDate) }}</td>
                  </tr>
                </tbody>
              </VTable>
              <VCardText v-else>
                <div class="retail-empty-state">
                  <VIcon icon="ri-truck-line" color="info" size="34" class="mb-2" />
                  <div class="font-weight-bold text-high-emphasis mb-1"> Chưa có phiếu nhập hàng </div>
                  <div class="text-body-2"> Hệ thống chưa ghi nhận phiếu nhập hàng nào. </div>
                </div>
              </VCardText>
            </VCard>
          </VCol>
          <VCol cols="12" lg="4">
            <VCard class="dashboard-panel">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Tồn kho </div>
                    <VCardTitle>Cảnh báo tồn kho</VCardTitle>
                    <VCardSubtitle>SKU dưới ngưỡng an toàn hoặc vượt tối đa</VCardSubtitle>
                  </div>
                  <VChip class="stock-alert-chip" :color="stockAlerts.length ? 'error' : 'success'" variant="tonal"
                    size="small"> {{ stockAlerts.length }} cảnh báo </VChip>
                </div>
              </VCardItem>
              <VList v-if="stockAlerts.length" class="dashboard-list">
                <VListItem v-for="stock in stockAlerts" :key="stock.productId">
                  <VListItemTitle>{{ stock.productName }}</VListItemTitle>
                  <VListItemSubtitle>{{ stock.productCode }} · {{ stock.unitName }}</VListItemSubtitle> <template
                    #append>
                    <VChip
                      :color="stock.alertLevel === 'Overstock' ? 'error' : stock.alertLevel === 'Critical' ? 'error' : 'warning'"
                      size="small" variant="tonal"> {{ stock.alertLevel === 'Overstock' ? 'Tồn cao: ' : 'Tồn thấp: '
                      }}{{ stock.availableQuantity }} </VChip>
                  </template>
                </VListItem>
              </VList>
              <VCardText v-else>
                <div class="retail-empty-state">
                  <VIcon icon="ri-shield-check-line" color="success" size="34" class="mb-2" />
                  <div class="font-weight-bold text-high-emphasis mb-1"> Tồn kho đang an toàn </div>
                  <div class="text-body-2"> Tất cả SKU đều trên ngưỡng an toàn. </div>
                </div>
              </VCardText>
            </VCard>
          </VCol>
          <VCol v-if="authStore.user?.role !== 'Warehouse'" cols="12">
            <VCard class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Audit log </div>
                    <VCardTitle>Hoạt động gần đây</VCardTitle>
                    <VCardSubtitle>10 hoạt động gần nhất trong hệ thống</VCardSubtitle>
                  </div>
                  <VChip color="info" variant="tonal" size="small" prepend-icon="ri-history-line"> Theo thời gian thực
                  </VChip>
                </div>
              </VCardItem>
              <VTable v-if="activityLogs.length" class="retail-table">
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
                  <tr v-for="log in activityLogs" :key="log.id">
                    <td>{{ log.userName }}</td>
                    <td>{{ log.action }}</td>
                    <td>{{ log.entityType }}</td>
                    <td>{{ log.serviceName }}</td>
                    <td>{{ formatDateTime(log.createdAt) }}</td>
                  </tr>
                </tbody>
              </VTable>
              <VCardText v-else>
                <div class="retail-empty-state">
                  <VIcon icon="ri-time-line" color="info" size="34" class="mb-2" />
                  <div class="font-weight-bold text-high-emphasis mb-1"> Chưa có hoạt động </div>
                  <div class="text-body-2"> Chưa có hoạt động nào được ghi nhận. </div>
                </div>
              </VCardText>
            </VCard>
          </VCol>
        </VRow>
      </VWindowItem> <!-- TAB 1 -->
      <VWindowItem :value="1">
        <VRow align="start">
          <VCol cols="12" lg="6">
            <VCard class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Gross profit </div>
                    <VCardTitle>Lợi nhuận gộp theo ngày</VCardTitle>
                    <VCardSubtitle>So sánh doanh thu, giá vốn và lợi nhuận thực tế</VCardSubtitle>
                  </div>
                </div>
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
                  <tr v-for="item in dailyProfitData" :key="item.date">
                    <td>{{ formatDateFull(item.date) }}</td>
                    <td class="text-right font-weight-bold text-primary"> {{ formatCurrency(item.revenue) }} </td>
                    <td class="text-right text-medium-emphasis"> {{ formatCurrency(item.costOfGoods) }} </td>
                    <td class="text-right font-weight-bold text-success"> {{ formatCurrency(item.profit) }} </td>
                  </tr>
                  <tr v-if="!dailyProfitData.length">
                    <td colspan="4" class="text-center text-medium-emphasis py-6"> Chưa có dữ liệu thống kê lợi nhuận
                      ngày.
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </VCard>
          </VCol>
          <VCol cols="12" lg="6">
            <VCard class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Product margin </div>
                    <VCardTitle>Tỷ suất lợi nhuận trên từng sản phẩm</VCardTitle>
                    <VCardSubtitle>Xác định biên lợi nhuận của từng sản phẩm đã bán</VCardSubtitle>
                  </div>
                </div>
              </VCardItem>
              <VTable class="retail-table">
                <thead>
                  <tr>
                    <th>Sản phẩm</th>
                    <th class="text-right">Doanh thu</th>
                    <th class="text-right">Lợi nhuận</th>
                    <th class="text-right">Biên</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="product in productProfitData" :key="product.productId">
                    <td class="font-weight-bold"> {{ product.productName }} </td>
                    <td class="text-right"> {{ formatCurrency(product.revenue) }} </td>
                    <td class="text-right text-success font-weight-bold"> {{ formatCurrency(product.profit) }} </td>
                    <td class="text-right font-weight-bold">
                      <VChip size="small" variant="tonal"
                        :color="(product.marginPercent ?? 0) > 30 ? 'success' : (product.marginPercent ?? 0) > 15 ? 'info' : 'warning'">
                        {{
                          (product.marginPercent ?? 0).toFixed(1) }}% </VChip>
                    </td>
                  </tr>
                  <tr v-if="!productProfitData.length">
                    <td colspan="4" class="text-center text-medium-emphasis py-6"> Chưa có dữ liệu thống kê sản phẩm.
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </VCard>
          </VCol>
        </VRow>
      </VWindowItem> <!-- TAB 2 -->
      <VWindowItem :value="2">
        <VRow align="start">
          <VCol cols="12" lg="6">
            <VCard class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Product ranking </div>
                    <VCardTitle>Top sản phẩm bán chạy</VCardTitle>
                    <VCardSubtitle>Sản phẩm dẫn đầu doanh số và số lượng bán</VCardSubtitle>
                  </div>
                </div>
              </VCardItem>
              <VTable class="retail-table">
                <thead>
                  <tr>
                    <th>Sản phẩm</th>
                    <th>Mã hàng</th>
                    <th class="text-right">Đã bán</th>
                    <th class="text-right">Doanh thu</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(product, index) in topProducts" :key="product.productId">
                    <td class="font-weight-bold"> <span class="rank-number">#{{ index + 1 }}</span> {{
                      product.productName }}
                    </td>
                    <td>{{ product.productCode }}</td>
                    <td class="text-right font-weight-bold text-success"> {{ formatNumber(product.quantitySold) }} </td>
                    <td class="text-right font-weight-bold text-primary"> {{ formatCurrency(product.totalRevenue) }}
                    </td>
                  </tr>
                  <tr v-if="!topProducts.length">
                    <td colspan="4" class="text-center text-medium-emphasis py-6"> Chưa có dữ liệu xếp hạng sản phẩm.
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </VCard>
          </VCol>
          <VCol cols="12" lg="6">
            <VCard class="dashboard-panel dashboard-table-card">
              <VCardItem>
                <div class="dashboard-panel-heading">
                  <div>
                    <div class="dashboard-kicker"> Customer ranking </div>
                    <VCardTitle>Top khách hàng mua nhiều nhất</VCardTitle>
                    <VCardSubtitle>Khách hàng đóng góp nhiều doanh thu nhất</VCardSubtitle>
                  </div>
                </div>
              </VCardItem>
              <VTable class="retail-table">
                <thead>
                  <tr>
                    <th>Khách hàng</th>
                    <th class="text-center">Số đơn</th>
                    <th class="text-right">Tổng chi tiêu</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(customer, index) in topCustomers" :key="customer.customerId">
                    <td class="font-weight-bold"> <span class="rank-number">#{{ index + 1 }}</span> {{
                      customer.customerName
                    }} </td>
                    <td class="text-center"> {{ customer.ordersCount }} </td>
                    <td class="text-right font-weight-bold text-success"> {{ formatCurrency(customer.totalSpent) }}
                    </td>
                  </tr>
                  <tr v-if="!topCustomers.length">
                    <td colspan="3" class="text-center text-medium-emphasis py-6"> Chưa có dữ liệu xếp hạng khách hàng.
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </VCard>
          </VCol>
        </VRow>
      </VWindowItem> <!-- TAB 3 -->
      <VWindowItem :value="3">
        <VCard class="dashboard-panel dashboard-table-card">
          <VCardItem>
            <div class="dashboard-panel-heading">
              <div>
                <div class="dashboard-kicker"> Daily sales </div>
                <VCardTitle>Chi tiết báo cáo doanh số ngày</VCardTitle>
                <VCardSubtitle>Tổng hợp doanh thu, giảm giá, đơn hàng và khách hàng mới</VCardSubtitle>
              </div>
            </div>
          </VCardItem>
          <VTable class="retail-table">
            <thead>
              <tr>
                <th>Ngày báo cáo</th>
                <th class="text-center">Tổng đơn</th>
                <th class="text-center">Sản phẩm bán</th>
                <th class="text-right">Khách mới</th>
                <th class="text-right">Chiết khấu</th>
                <th class="text-right">Doanh thu ròng</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="report in dailySalesReports" :key="report.id">
                <td class="font-weight-bold text-primary"> {{ formatDateFull(report.reportDate) }} </td>
                <td class="text-center font-weight-bold"> {{ report.totalOrders }} </td>
                <td class="text-center"> {{ report.totalItemsSold }} </td>
                <td class="text-right"> {{ report.totalNewCustomers }} </td>
                <td class="text-right text-error"> {{ formatCurrency(report.totalDiscount) }} </td>
                <td class="text-right text-success font-weight-bold"> {{ formatCurrency(report.totalRevenue) }} </td>
              </tr>
              <tr v-if="!dailySalesReports.length">
                <td colspan="6" class="text-center text-medium-emphasis py-6"> Chưa có báo cáo doanh số ngày nào được
                  tạo.
                </td>
              </tr>
            </tbody>
          </VTable>
        </VCard>
      </VWindowItem> <!-- TAB 4 -->
      <VWindowItem :value="4">
        <VCard class="dashboard-panel dashboard-table-card">
          <VCardItem>
            <div class="dashboard-panel-heading">
              <div>
                <div class="dashboard-kicker"> Staff performance </div>
                <VCardTitle>Báo cáo hiệu suất bán hàng của nhân viên</VCardTitle>
                <VCardSubtitle>Theo dõi tổng số đơn hàng, doanh thu và giá trị đơn hàng trung bình</VCardSubtitle>
              </div>
            </div>
          </VCardItem>
          <VTable class="retail-table">
            <thead>
              <tr>
                <th>Nhân viên</th>
                <th class="text-center">Số đơn hàng</th>
                <th class="text-right">Tổng doanh số</th>
                <th class="text-right">Giá trị đơn trung bình</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="staff in staffPerformanceData" :key="staff.staffId">
                <td>
                  <div class="staff-cell">
                    <VAvatar color="primary" variant="tonal" size="34"> <span class="text-caption font-weight-bold"> {{
                      staff.staffName.substring(0, 2).toUpperCase() }} </span> </VAvatar> <strong>{{ staff.staffName
                        }}</strong>
                  </div>
                </td>
                <td class="text-center font-weight-bold"> {{ formatNumber(staff.totalOrders) }} </td>
                <td class="text-right text-success font-weight-bold"> {{ formatCurrency(staff.totalRevenue) }} </td>
                <td class="text-right font-weight-bold"> {{ formatCurrency(staff.averageOrderValue) }} </td>
              </tr>
              <tr v-if="!staffPerformanceData.length">
                <td colspan="4" class="text-center text-medium-emphasis py-6"> Chưa có dữ liệu hiệu suất của nhân viên
                  nào.
                </td>
              </tr>
            </tbody>
          </VTable>
        </VCard>
      </VWindowItem>
    </VWindow>
  </section>
</template>
<style scoped>
.dashboard-page {
  position: relative;
  isolation: isolate;
}

.dashboard-page::before {
  content: '';
  position: absolute;
  inset: -2rem -2rem auto;
  z-index: -1;
  block-size: 340px;
  border-radius: 0 0 44px 44px;
  background: radial-gradient(circle at 18% 12%, rgba(var(--v-theme-primary), 0.18), transparent 34%), radial-gradient(circle at 88% 0%, rgba(var(--v-theme-info), 0.16), transparent 32%), linear-gradient(135deg, rgba(var(--v-theme-primary), 0.08), transparent 58%);
  pointer-events: none;
}

.dashboard-shell {
  margin-block-end: 1.5rem;
}

.dashboard-action-btn,
.dashboard-primary-btn {
  border-radius: 14px;
  font-weight: 700;
}

.dashboard-primary-btn {
  color: white !important;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info))) !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-primary), 0.32);
}

.dashboard-tabs-card,
.dashboard-panel,
:deep(.retail-panel-card) {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 22px !important;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.96), rgba(var(--v-theme-surface), 0.84));
  box-shadow: 0 18px 52px rgba(15, 23, 42, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.dashboard-tabs-card {
  overflow: visible;
}

.dashboard-tabs-card :deep(.v-card-text) {
  padding: 1rem;
}

.dashboard-tabs-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-block-end: 0.75rem;
}

.dashboard-tabs-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.dashboard-tabs-head strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.1rem;
  font-weight: 800;
}

.dashboard-tabs-scroll {
  overflow-x: auto;
  padding-block-end: 2px;
  scrollbar-width: thin;
}

.dashboard-tabs-scroll::-webkit-scrollbar {
  block-size: 6px;
}

.dashboard-tabs-scroll::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(var(--v-theme-primary), 0.22);
}

.dashboard-tabs-card {
  overflow: hidden;
}

.dashboard-tabs-card :deep(.v-card-text) {
  padding: 1.1rem;
}

.dashboard-tabs-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-block-end: 1rem;
}

.dashboard-tabs-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.dashboard-tabs-head strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.25rem;
  font-weight: 850;
  letter-spacing: -0.035em;
}

.active-tab-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  min-inline-size: max-content;
  border-radius: 999px;
  padding: 0.48rem 0.82rem;
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.12);
  font-size: 0.82rem;
  font-weight: 800;
}

.active-tab-badge span {
  color: inherit;
  font-size: inherit;
  font-weight: inherit;
  letter-spacing: -0.01em;
  text-transform: none;
}

.dashboard-tabs-scroll {
  overflow-x: auto;
  overflow-y: visible;
  scrollbar-width: thin;
}

.dashboard-tabs-scroll::-webkit-scrollbar {
  block-size: 6px;
}

.dashboard-tabs-scroll::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(var(--v-theme-primary), 0.22);
}

.dashboard-tabs-custom {
  display: grid;
  grid-template-columns: repeat(var(--tab-count), minmax(0, 1fr));
  align-items: center;
  gap: 0.55rem;
  inline-size: 100%;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 0.45rem;
  background: rgba(var(--v-theme-background), 0.72);
}

.dashboard-tab-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  inline-size: 100%;
  min-inline-size: 0;
  min-block-size: 52px;
  padding: 0 0.7rem;
  border: 0;
  border-radius: 16px;
  color: rgba(var(--v-theme-on-surface), 0.72);
  background: transparent;
  font: inherit;
  font-size: 0.95rem;
  font-weight: 800;
  letter-spacing: -0.018em;
  white-space: nowrap;
  cursor: pointer;
  transition:
    color 180ms ease,
    background 180ms ease,
    box-shadow 180ms ease,
    transform 180ms ease;
}

.dashboard-tab-button span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dashboard-tab-icon {
  flex: 0 0 auto;
}

.dashboard-tab-button:hover {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.06);
}

.dashboard-tab-button--active {
  color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-surface));
  box-shadow:
    0 12px 30px rgba(15, 23, 42, 0.1),
    inset 0 1px 0 rgba(255, 255, 255, 0.2);
}

.dashboard-tab-button--active .dashboard-tab-icon {
  color: rgb(var(--v-theme-primary));
}@media (max-width: 1100px) {
  .dashboard-tabs-custom {
    inline-size: max-content;
    min-inline-size: 900px;
  }

  .dashboard-tab-button {
    min-inline-size: 160px;
  }
}@media (max-width: 760px) {
  .dashboard-tabs-head {
    align-items: flex-start;
    flex-direction: column;
  }

  .active-tab-badge {
    max-inline-size: 100%;
  }

  .dashboard-tabs-card :deep(.v-card-text) {
    padding: 0.9rem;
  }

  .dashboard-tabs-custom {
    min-inline-size: 760px;
  }

  .dashboard-tab-button {
    min-block-size: 46px;
    min-inline-size: 145px;
    font-size: 0.88rem;
  }
}

.dashboard-panel {
  block-size: auto;
}

.dashboard-table-card {
  block-size: auto !important;
  min-block-size: unset !important;
}

.dashboard-table-card :deep(.v-table__wrapper) {
  max-block-size: 520px;
  overflow: auto;
}

.dashboard-panel-heading {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) auto;
}

.dashboard-panel-heading>div {
  min-inline-size: 0;
}

.dashboard-panel-heading :deep(.v-card-title) {
  white-space: normal;
  overflow-wrap: anywhere;
  line-height: 1.25;
}

.dashboard-panel-heading :deep(.v-card-subtitle) {
  white-space: normal;
  overflow-wrap: anywhere;
}

.dashboard-kicker {
  color: rgb(var(--v-theme-primary));
  font-size: 0.74rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.panel-icon {
  color: white;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info)));
  box-shadow: 0 14px 32px rgba(var(--v-theme-primary), 0.26);
}

.stock-alert-chip {
  flex-shrink: 0;
  max-inline-size: max-content;
  white-space: nowrap;
}

.comparison-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.comparison-card {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 1rem;
  background: radial-gradient(circle at 100% 0%, rgba(var(--v-theme-primary), 0.08), transparent 44%), rgba(var(--v-theme-background), 0.64);
  transition: transform 220ms cubic-bezier(0.22, 1, 0.36, 1), box-shadow 220ms ease, border-color 220ms ease;
}

.comparison-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.24);
  box-shadow: 0 14px 34px rgba(15, 23, 42, 0.08);
  transform: translateY(-4px);
}

.comparison-card span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 700;
  margin-block-end: 0.45rem;
}

.comparison-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.15rem;
  font-weight: 850;
  letter-spacing: -0.035em;
}

.comparison-card p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.82rem;
}

/* Revenue chart */
.revenue-visual {
  display: grid;
  gap: 1rem;
}

.revenue-summary-strip {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.summary-tile {
  border: 1px solid rgba(var(--v-border-color), 0.1);
  border-radius: 18px;
  padding: 1rem;
  background: radial-gradient(circle at 100% 0%, rgba(var(--v-theme-primary), 0.08), transparent 42%), rgba(var(--v-theme-background), 0.58);
}

.summary-tile span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 700;
  margin-block-end: 0.45rem;
}

.summary-tile strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.05rem;
  font-weight: 850;
  letter-spacing: -0.03em;
}

.revenue-chart-card {
  position: relative;
  overflow: hidden;
  border: 1px solid rgba(var(--v-border-color), 0.1);
  border-radius: 24px;
  padding: 1rem;
  background: radial-gradient(circle at 72% 12%, rgba(var(--v-theme-info), 0.12), transparent 34%), linear-gradient(180deg, rgba(var(--v-theme-background), 0.62), rgba(var(--v-theme-background), 0.32));
}

.revenue-chart-card::before {
  content: '';
  position: absolute;
  inset-inline: 1.2rem;
  inset-block-start: 0;
  block-size: 3px;
  border-radius: 999px;
  background: linear-gradient(90deg, transparent, rgba(var(--v-theme-primary), 0.9), rgba(var(--v-theme-info), 0.9), transparent);
}

.revenue-svg {
  display: block;
  inline-size: 100%;
  min-block-size: 320px;
}

.chart-grid-lines line {
  stroke: rgba(var(--v-border-color), 0.16);
  stroke-width: 1;
  stroke-dasharray: 4 8;
}

.chart-grid-lines text,
.chart-x-labels text {
  fill: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 12px;
  font-weight: 700;
}

.revenue-area {
  fill: url(#revenueAreaGradient);
}

.revenue-line {
  fill: none;
  stroke: url(#revenueLineGradient);
  stroke-width: 4;
  stroke-linecap: round;
  stroke-linejoin: round;
  filter: url(#revenueGlow);
}

.revenue-point {
  cursor: pointer;
}

.revenue-hover-line {
  opacity: 0;
  stroke: rgba(var(--v-theme-primary), 0.35);
  stroke-width: 1.5;
  stroke-dasharray: 5 7;
  transition: opacity 180ms ease;
}

.revenue-point-ring {
  fill: rgba(var(--v-theme-surface), 0.96);
  stroke: rgba(var(--v-theme-info), 0.55);
  stroke-width: 2;
  transition: r 180ms ease, stroke 180ms ease, filter 180ms ease;
}

.revenue-point-core {
  fill: rgb(var(--v-theme-primary));
  transition: r 180ms ease, fill 180ms ease;
}

.revenue-point:hover .revenue-hover-line {
  opacity: 1;
}

.revenue-point:hover .revenue-point-ring {
  r: 12;
  stroke: rgb(var(--v-theme-info));
  filter: drop-shadow(0 0 12px rgba(var(--v-theme-info), 0.55));
}

.revenue-point:hover .revenue-point-core {
  r: 6;
  fill: rgb(var(--v-theme-info));
}

.peak-marker line {
  stroke: rgba(var(--v-theme-success), 0.38);
  stroke-width: 1.5;
  stroke-dasharray: 5 7;
}

.peak-marker text {
  fill: rgb(var(--v-theme-success));
  font-size: 12px;
  font-weight: 850;
  paint-order: stroke;
  stroke: rgba(var(--v-theme-surface), 0.94);
  stroke-width: 4px;
}

/* Tables */
.retail-table {
  border-radius: 0 0 22px 22px;
}

.retail-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.68) !important;
  font-size: 0.76rem;
  font-weight: 800 !important;
  letter-spacing: 0.035em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.retail-table :deep(td) {
  block-size: 58px !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
}

.retail-table :deep(tbody tr) {
  transition: background 160ms ease;
}

.retail-table :deep(tbody tr:hover) {
  background: rgba(var(--v-theme-primary), 0.045);
}

.dashboard-list {
  background: transparent;
}

.retail-empty-state {
  display: grid;
  place-items: center;
  min-block-size: 230px;
  border: 1px dashed rgba(var(--v-border-color), 0.24);
  border-radius: 18px;
  color: rgba(var(--v-theme-on-surface), 0.62);
  text-align: center;
  background: rgba(var(--v-theme-background), 0.44);
}

.rank-number {
  display: inline-flex;
  min-inline-size: 38px;
  color: rgba(var(--v-theme-on-surface), 0.48);
  font-weight: 800;
}

.staff-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
}

.staff-cell strong {
  color: rgb(var(--v-theme-on-surface));
}@media (max-width: 1200px) {
  .comparison-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {
  .dashboard-panel-heading {
    grid-template-columns: 1fr;
  }

  .stock-alert-chip {
    justify-self: start;
  }

  .revenue-summary-strip {
    grid-template-columns: 1fr;
  }

  .revenue-chart-card {
    padding: 0.75rem;
  }

  .revenue-svg {
    min-block-size: 260px;
  }
}@media (max-width: 640px) {
  .comparison-grid {
    grid-template-columns: 1fr;
  }

  .dashboard-tabs-card,
  .dashboard-panel,
  :deep(.retail-panel-card) {
    border-radius: 18px !important;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.dashboard-hero {
  display: flex !important;
  align-items: center !important;
  justify-content: space-between !important;
  border-radius: 16px !important;
  padding: 0.65rem 1rem !important;
  margin-block-end: 0.75rem !important;
  min-block-size: auto !important;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.94), rgba(var(--v-theme-surface), 0.76)), rgba(var(--v-theme-surface), 0.78) !important;
  box-shadow: 0 4px 20px rgba(15, 23, 42, 0.05) !important;
  backdrop-filter: blur(10px) !important;
}

.dashboard-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.dashboard-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.dashboard-hero__copy {
  display: none !important;
}

.dashboard-hero__actions,
.dashboard-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.dashboard-hero__actions .v-btn,
.dashboard-actions .v-btn,
.dashboard-hero__actions .v-btn.primary-action,
.primary-action,
.receipt-action,
.payment-action,
.danger-action,
.export-action,
.warning-action,
.dashboard-action-btn,
.dashboard-primary-btn {
  min-block-size: 34px !important;
  border-radius: 10px !important;
  font-weight: 700 !important;
  font-size: 0.84rem !important;
  box-shadow: none !important;
  padding: 0 1rem !important;
}
</style>
