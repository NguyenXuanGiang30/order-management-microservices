<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import { getApiBaseUrl } from '@/services/authApi'

import {
  type ProductDto,
  type StockDto,
  type StocktakeSessionDto,
  cancelStocktake,
  confirmStocktake,
  createStocktakeSession,
  getInventoryStock,
  getProducts,
  getStocktakeSession,
  getStocktakeSessions,
} from '@/services/productInventoryApi'

const activeTab = ref(0)

const stockItems = ref<StockDto[]>([])
const sessions = ref<StocktakeSessionDto[]>([])

const search = ref('')
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const stockPage = ref(1)
const stockPageSize = ref(10)

const sessionPage = ref(1)
const sessionPageSize = ref(10)
const sessionTotalPages = ref(0)
const sessionTotalCount = ref(0)

const createSessionDialog = ref(false)
const detailSessionDialog = ref(false)

const filterStatus = ref('All')

const filterStatusItems = [
  { title: 'Tất cả', value: 'All' },
  { title: 'Bình thường / Tốt', value: 'Healthy' },
  { title: 'Chạm ngưỡng thấp', value: 'BelowMin' },
  { title: 'Quá tải tồn kho', value: 'Overstock' },
]

const sessionNote = ref('')
const sessionLines = ref<Array<{
  product: ProductDto
  actualQty: number
  note: string
}>>([])

const productSearchQuery = ref('')
const searchProductsResult = ref<ProductDto[]>([])
const searchLoading = ref(false)

const selectedSession = ref<StocktakeSessionDto | null>(null)
const loadingSessionDetail = ref(false)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatDate = (dateStr: string) => {
  if (!dateStr)
    return '—'

  const date = new Date(dateStr)

  if (Number.isNaN(date.getTime()))
    return '—'

  return date.toLocaleDateString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const stockStatus = (product: StockDto) => {
  if (product.quantityOnHand <= 0)
    return 'Hết hàng'

  if (product.alertLevel === 'Overstock')
    return 'Quá tải'

  if (product.isBelowMin)
    return 'Tồn thấp'

  if (product.alertLevel === 'High')
    return 'Tốt'

  return 'Bình thường'
}

const stockStatusColor = (product: StockDto) => {
  if (product.quantityOnHand <= 0)
    return 'error'

  if (product.alertLevel === 'Overstock')
    return 'error'

  if (product.isBelowMin)
    return 'warning'

  if (product.alertLevel === 'High')
    return 'success'

  return 'secondary'
}

const sessionStatusLabel = (status: string) => {
  if (status === 'Confirmed')
    return 'Đã xác nhận'

  if (status === 'Cancelled')
    return 'Đã hủy'

  return 'Bản nháp'
}

const sessionStatusColor = (status: string) => {
  if (status === 'Confirmed')
    return 'success'

  if (status === 'Cancelled')
    return 'error'

  return 'warning'
}

const visibleStockItems = computed(() => {
  if (filterStatus.value !== 'Healthy')
    return stockItems.value

  return stockItems.value.filter(item =>
    item.quantityOnHand > 0
    && !item.isBelowMin
    && item.alertLevel !== 'Overstock',
  )
})

const stockTotalPages = computed(() =>
  Math.max(1, Math.ceil(visibleStockItems.value.length / stockPageSize.value)),
)

const paginatedStockItems = computed(() => {
  const start = (stockPage.value - 1) * stockPageSize.value

  return visibleStockItems.value.slice(start, start + stockPageSize.value)
})

const stockRangeStart = computed(() => {
  if (!visibleStockItems.value.length)
    return 0

  return (stockPage.value - 1) * stockPageSize.value + 1
})

const stockRangeEnd = computed(() =>
  Math.min(stockPage.value * stockPageSize.value, visibleStockItems.value.length),
)

const totalStock = computed(() =>
  stockItems.value.reduce((sum, product) => sum + product.quantityOnHand, 0),
)

const reservedStock = computed(() =>
  stockItems.value.reduce((sum, product) => sum + product.quantityReserved, 0),
)

const availableStock = computed(() =>
  stockItems.value.reduce((sum, product) => sum + product.availableQuantity, 0),
)

const lowStock = computed(() =>
  stockItems.value.filter(product => product.isBelowMin).length,
)

const overStockCount = computed(() =>
  stockItems.value.filter(product => product.alertLevel === 'Overstock').length,
)

const stockSummaryCards = computed(() => [
  {
    label: 'Tổng tồn',
    value: formatNumber(totalStock.value),
    helper: 'Tổng số lượng trong kho',
    icon: 'ri-archive-line',
    color: 'primary',
  },
  {
    label: 'Khả dụng',
    value: formatNumber(availableStock.value),
    helper: 'Sau khi trừ hàng đang giữ',
    icon: 'ri-inbox-line',
    color: 'success',
  },
  {
    label: 'Đang giữ',
    value: formatNumber(reservedStock.value),
    helper: 'Hàng giữ cho đơn chưa hoàn tất',
    icon: 'ri-lock-line',
    color: 'info',
  },
  {
    label: 'Cần nhập',
    value: formatNumber(lowStock.value),
    helper: 'SKU chạm ngưỡng tồn tối thiểu',
    icon: 'ri-alert-line',
    color: 'warning',
  },
  {
    label: 'Quá tải',
    value: formatNumber(overStockCount.value),
    helper: 'SKU vượt ngưỡng tồn tối đa',
    icon: 'ri-error-warning-line',
    color: 'error',
  },
])

const draftSessions = computed(() =>
  sessions.value.filter(session => session.status === 'Draft').length,
)

const confirmedSessions = computed(() =>
  sessions.value.filter(session => session.status === 'Confirmed').length,
)

const cancelledSessions = computed(() =>
  sessions.value.filter(session => session.status === 'Cancelled').length,
)

const sessionSummaryCards = computed(() => [
  {
    label: 'Tổng phiên kiểm kê',
    value: formatNumber(sessionTotalCount.value),
    helper: `Trang hiện tại: ${formatNumber(sessions.value.length)} phiên`,
    icon: 'ri-survey-line',
    color: 'primary',
  },
  {
    label: 'Bản nháp',
    value: formatNumber(draftSessions.value),
    helper: 'Chờ xác nhận cân kho',
    icon: 'ri-draft-line',
    color: 'warning',
  },
  {
    label: 'Đã xác nhận',
    value: formatNumber(confirmedSessions.value),
    helper: 'Đã cập nhật tồn thực tế',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Đã hủy',
    value: formatNumber(cancelledSessions.value),
    helper: 'Phiên kiểm kê không áp dụng',
    icon: 'ri-close-circle-line',
    color: 'error',
  },
])

const loadStock = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    stockItems.value = await getInventoryStock({
      search: search.value.trim(),
      belowMin: filterStatus.value === 'BelowMin' ? true : undefined,
      aboveMax: filterStatus.value === 'Overstock' ? true : undefined,
    })

    stockPage.value = 1
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu tồn kho.'
  }
  finally {
    loading.value = false
  }
}

const loadSessions = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const res = await getStocktakeSessions({
      page: sessionPage.value,
      pageSize: sessionPageSize.value,
    })

    sessions.value = res.items
    sessionTotalPages.value = res.totalPages
    sessionTotalCount.value = res.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách phiên kiểm kê.'
  }
  finally {
    loading.value = false
  }
}

const refreshCurrentTab = () => {
  if (activeTab.value === 0)
    return loadStock()

  sessionPage.value = 1

  return loadSessions()
}

const searchProductsForStocktake = async () => {
  if (!productSearchQuery.value.trim()) {
    searchProductsResult.value = []

    return
  }

  searchLoading.value = true

  try {
    const res = await getProducts({
      search: productSearchQuery.value.trim(),
      pageSize: 10,
    })

    searchProductsResult.value = res.items
  }
  catch (error) {
    console.error('Không thể tìm kiếm sản phẩm:', error)
  }
  finally {
    searchLoading.value = false
  }
}

const addProductToStocktake = (product: ProductDto) => {
  const existing = sessionLines.value.find(line => line.product.id === product.id)

  if (existing)
    return

  sessionLines.value.push({
    product,
    actualQty: product.quantityOnHand ?? 0,
    note: '',
  })

  productSearchQuery.value = ''
  searchProductsResult.value = []
}

const removeProductFromStocktake = (id: string) => {
  sessionLines.value = sessionLines.value.filter(line => line.product.id !== id)
}

const openCreateStocktake = () => {
  sessionLines.value = []
  sessionNote.value = ''
  productSearchQuery.value = ''
  searchProductsResult.value = []
  createSessionDialog.value = true
}

const handleCreateSession = async () => {
  if (sessionLines.value.length === 0) {
    errorMessage.value = 'Vui lòng thêm sản phẩm cần kiểm kê.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createStocktakeSession({
      note: sessionNote.value || null,
      details: sessionLines.value.map(line => ({
        productId: line.product.id,
        actualQuantity: Number(line.actualQty) || 0,
        note: line.note || null,
      })),
    })

    createSessionDialog.value = false
    successMessage.value = 'Tạo phiên kiểm kê thành công.'

    await loadSessions()

    activeTab.value = 1
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi tạo phiên kiểm kê.'
  }
  finally {
    loading.value = false
  }
}

const openSessionDetail = async (id: string) => {
  loadingSessionDetail.value = true
  detailSessionDialog.value = true
  selectedSession.value = null
  errorMessage.value = ''

  try {
    selectedSession.value = await getStocktakeSession(id)
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải chi tiết phiên kiểm kê.'
  }
  finally {
    loadingSessionDetail.value = false
  }
}

const handleConfirmStocktake = async () => {
  if (!selectedSession.value)
    return

  if (!confirm('Bạn có chắc chắn muốn xác nhận đợt kiểm kê này không? Số lượng tồn kho thực tế sẽ được cập nhật đè lên hệ thống.'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await confirmStocktake(selectedSession.value.id)

    detailSessionDialog.value = false
    successMessage.value = 'Xác nhận kiểm kê thành công. Tồn kho đã được cập nhật.'

    await loadSessions()
    await loadStock()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi xác nhận kiểm kê.'
  }
  finally {
    loading.value = false
  }
}

const handleCancelStocktake = async () => {
  if (!selectedSession.value)
    return

  if (!confirm('Bạn có chắc chắn muốn hủy đợt kiểm kê này?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await cancelStocktake(selectedSession.value.id)

    detailSessionDialog.value = false
    successMessage.value = 'Đã hủy phiên kiểm kê.'

    await loadSessions()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi hủy kiểm kê.'
  }
  finally {
    loading.value = false
  }
}

const handleExportCsv = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const queryParams = new URLSearchParams()

    if (search.value)
      queryParams.append('search', search.value.trim())

    if (filterStatus.value === 'BelowMin')
      queryParams.append('belowMin', 'true')

    if (filterStatus.value === 'Overstock')
      queryParams.append('aboveMax', 'true')

    const token = localStorage.getItem('accessToken') || ''

    const response = await fetch(`${getApiBaseUrl()}/api/inventory/stock/export?${queryParams.toString()}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })

    if (!response.ok)
      throw new Error('Xuất CSV tồn kho thất bại.')

    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.download = `inventory-stock-${new Date().toISOString().slice(0, 10)}.csv`

    document.body.appendChild(link)
    link.click()
    link.remove()

    window.URL.revokeObjectURL(url)
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xuất CSV tồn kho.'
  }
  finally {
    loading.value = false
  }
}

watch(activeTab, value => {
  if (value === 0)
    void loadStock()
  else
    void loadSessions()
})

watch(sessionPage, () => {
  if (activeTab.value === 1)
    void loadSessions()
})

watch(sessionPageSize, () => {
  sessionPage.value = 1

  if (activeTab.value === 1)
    void loadSessions()
})

watch(stockPageSize, () => {
  stockPage.value = 1
})

watch(stockPage, value => {
  if (value > stockTotalPages.value)
    stockPage.value = stockTotalPages.value
})

onMounted(loadStock)
</script>

<template>
  <section class="inventory-page">
    <div class="inventory-hero">
      <div class="inventory-hero__title-area">
        <h1>Quản lý tồn kho</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-archive-line" class="ml-2">
          Kho hàng
        </VChip>
      </div>

      <div class="inventory-hero__actions">
        <VBtn v-if="activeTab === 0" variant="outlined" color="secondary" prepend-icon="ri-download-line"
          :loading="loading" @click="handleExportCsv">
          Xuất CSV
        </VBtn>

        <VBtn variant="tonal" color="primary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="refreshCurrentTab">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-checkbox-circle-line" class="primary-action"
          @click="openCreateStocktake">
          Tạo phiên kiểm kê
        </VBtn>
      </div>
    </div>

    <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
      {{ errorMessage }}
    </VAlert>

    <VAlert v-if="successMessage" type="success" variant="tonal" class="mb-4" closable
      @click:close="successMessage = ''">
      {{ successMessage }}
    </VAlert>

    <VCard class="inventory-tabs-card mb-4">
      <VCardText>
        <div class="inventory-tabs" role="tablist">
          <button type="button" class="inventory-tab-button"
            :class="{ 'inventory-tab-button--active': activeTab === 0 }" @click="activeTab = 0">
            <VIcon icon="ri-bar-chart-box-line" />
            <span>Báo cáo tồn kho</span>
          </button>

          <button type="button" class="inventory-tab-button"
            :class="{ 'inventory-tab-button--active': activeTab === 1 }" @click="activeTab = 1">
            <VIcon icon="ri-survey-line" />
            <span>Phiên kiểm kê điều chỉnh</span>
          </button>
        </div>
      </VCardText>
    </VCard>

    <VWindow v-model="activeTab">
      <VWindowItem :value="0">
        <div class="summary-grid inventory-summary-grid">
          <article v-for="card in stockSummaryCards" :key="card.label" class="summary-card">
            <div class="summary-icon" :class="`summary-icon--${card.color}`">
              <VIcon :icon="card.icon" />
            </div>

            <div>
              <span>{{ card.label }}</span>
              <strong>{{ card.value }}</strong>
              <p>{{ card.helper }}</p>
            </div>
          </article>
        </div>

        <VAlert v-if="lowStock > 0" type="warning" variant="tonal" class="mb-3" prepend-icon="ri-alert-line">
          Có {{ lowStock }} mặt hàng chạm ngưỡng tồn tối thiểu. Vui lòng kiểm tra và lên kế hoạch nhập hàng.
        </VAlert>

        <VAlert v-if="overStockCount > 0" type="error" variant="tonal" class="mb-4"
          prepend-icon="ri-error-warning-line">
          Có {{ overStockCount }} mặt hàng vượt ngưỡng tồn tối đa. Nên xả kho hoặc điều chỉnh chương trình bán hàng.
        </VAlert>

        <VCard class="inventory-panel">
          <VCardText>
            <div class="inventory-toolbar">
              <VTextField v-model="search" label="Tìm tồn kho" placeholder="Tên sản phẩm hoặc SKU..."
                prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable
                @keyup.enter="loadStock" @click:clear="search = ''; loadStock()" />

              <VSelect v-model="filterStatus" label="Trạng thái tồn" :items="filterStatusItems" item-title="title"
                item-value="value" density="comfortable" hide-details @update:model-value="loadStock" />

              <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="loadStock">
                Tìm
              </VBtn>

              <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
                @click="loadStock">
                Tải lại
              </VBtn>
            </div>
          </VCardText>

          <VDivider />

          <VCardText v-if="loading">
            <VSkeletonLoader type="table-heading, table-tbody" />
          </VCardText>

          <template v-else>
            <div v-if="visibleStockItems.length" class="inventory-table-wrap">
              <VTable class="inventory-table">
                <thead>
                  <tr>
                    <th>SKU</th>
                    <th>Sản phẩm</th>
                    <th class="text-end">Tồn hệ thống</th>
                    <th class="text-end">Đã giữ</th>
                    <th class="text-end">Khả dụng</th>
                    <th class="text-end">Ngưỡng tối thiểu</th>
                    <th class="text-center">Cảnh báo</th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="product in paginatedStockItems" :key="product.productId" class="inventory-row">
                    <td>
                      <div class="sku-cell">
                        {{ product.productCode }}
                      </div>
                    </td>

                    <td>
                      <div class="product-cell">
                        <strong>{{ product.productName }}</strong>
                        <span>{{ product.unitName }}</span>
                      </div>
                    </td>

                    <td class="text-end font-weight-bold">
                      {{ formatNumber(product.quantityOnHand) }} {{ product.unitName }}
                    </td>

                    <td class="text-end reserved-cell">
                      {{ formatNumber(product.quantityReserved) }} {{ product.unitName }}
                    </td>

                    <td class="text-end available-cell">
                      {{ formatNumber(product.availableQuantity) }} {{ product.unitName }}
                    </td>

                    <td class="text-end">
                      {{ formatNumber(product.minThreshold) }} {{ product.unitName }}
                    </td>

                    <td class="text-center">
                      <VChip :color="stockStatusColor(product)" variant="tonal" size="small">
                        {{ stockStatus(product) }}
                      </VChip>
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </div>

            <div v-else class="inventory-empty">
              <VIcon icon="ri-archive-line" size="42" color="primary" />
              <strong>Không tìm thấy dữ liệu tồn kho</strong>
              <span>Thử nhập từ khóa khác hoặc thay đổi bộ lọc trạng thái tồn.</span>

              <VBtn color="primary" prepend-icon="ri-refresh-line" @click="loadStock">
                Tải lại tồn kho
              </VBtn>
            </div>
          </template>

          <div v-if="visibleStockItems.length" class="inventory-pagination">
            <span>
              Hiển thị {{ formatNumber(stockRangeStart) }}–{{ formatNumber(stockRangeEnd) }}
              trên tổng số {{ formatNumber(visibleStockItems.length) }} mặt hàng
            </span>

            <div>
              <VSelect v-model="stockPageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
                variant="outlined" class="page-size-select" />

              <VPagination v-model="stockPage" :length="stockTotalPages" :total-visible="5" size="small" />
            </div>
          </div>
        </VCard>
      </VWindowItem>

      <VWindowItem :value="1">
        <div class="summary-grid">
          <article v-for="card in sessionSummaryCards" :key="card.label" class="summary-card">
            <div class="summary-icon" :class="`summary-icon--${card.color}`">
              <VIcon :icon="card.icon" />
            </div>

            <div>
              <span>{{ card.label }}</span>
              <strong>{{ card.value }}</strong>
              <p>{{ card.helper }}</p>
            </div>
          </article>
        </div>

        <VCard class="inventory-panel">
          <VCardText>
            <div class="session-panel-head">
              <div>
                <span>Kiểm kê kho</span>
                <strong>Phiên kiểm kê điều chỉnh</strong>
                <p>Theo dõi các phiên kiểm kê dùng để cân đối chênh lệch giữa tồn thực tế và tồn hệ thống.</p>
              </div>

              <VBtn color="primary" prepend-icon="ri-add-line" class="primary-action" @click="openCreateStocktake">
                Tạo phiên mới
              </VBtn>
            </div>
          </VCardText>

          <VDivider />

          <VCardText v-if="loading">
            <VSkeletonLoader type="table-heading, table-tbody" />
          </VCardText>

          <template v-else>
            <div v-if="sessions.length" class="inventory-table-wrap">
              <VTable class="inventory-table">
                <thead>
                  <tr>
                    <th>Mã kiểm kê</th>
                    <th>Người tạo</th>
                    <th>Ngày tạo</th>
                    <th>Ngày xác nhận</th>
                    <th>Ghi chú</th>
                    <th class="text-center">Trạng thái</th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="session in sessions" :key="session.id" class="inventory-row"
                    @click="openSessionDetail(session.id)">
                    <td>
                      <div class="sku-cell">
                        {{ session.code }}
                      </div>
                    </td>

                    <td>{{ session.createdByName }}</td>
                    <td>{{ formatDate(session.createdAt) }}</td>
                    <td>{{ formatDate(session.confirmedAt || '') }}</td>

                    <td>
                      <div class="note-cell">
                        {{ session.note || '—' }}
                      </div>
                    </td>

                    <td class="text-center">
                      <VChip :color="sessionStatusColor(session.status)" variant="tonal" size="small">
                        {{ sessionStatusLabel(session.status) }}
                      </VChip>
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </div>

            <div v-else class="inventory-empty">
              <VIcon icon="ri-survey-line" size="42" color="primary" />
              <strong>Chưa có phiên kiểm kê nào</strong>
              <span>Tạo phiên kiểm kê để cân đối chênh lệch hàng thực tế và hệ thống.</span>

              <VBtn color="primary" prepend-icon="ri-add-line" @click="openCreateStocktake">
                Tạo phiên kiểm kê
              </VBtn>
            </div>
          </template>

          <div v-if="sessions.length || sessionTotalCount > 0" class="inventory-pagination">
            <span>
              Hiển thị {{ formatNumber(sessions.length) }} trên tổng số {{ formatNumber(sessionTotalCount) }} phiên kiểm
              kê
            </span>

            <div>
              <VSelect v-model="sessionPageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact"
                hide-details variant="outlined" class="page-size-select" />

              <VPagination v-model="sessionPage" :length="sessionTotalPages" :total-visible="5" size="small" />
            </div>
          </div>
        </VCard>
      </VWindowItem>
    </VWindow>

    <VDialog v-model="createSessionDialog" max-width="940" persistent scrollable>
      <VCard class="stocktake-dialog">
        <div class="dialog-head">
          <div>
            <span>Tạo phiên kiểm kê</span>
            <h2>Phiên kiểm kê kho mới</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ sessionLines.length }} sản phẩm
          </VChip>
        </div>

        <VCardText>
          <VTextField v-model="sessionNote" label="Ghi chú đợt kiểm kê"
            placeholder="VD: Kiểm kê cuối tháng, kiểm kê khu A..." density="comfortable" class="mb-4" />

          <div class="stocktake-search-box">
            <VTextField v-model="productSearchQuery" label="Chọn sản phẩm muốn kiểm kê"
              placeholder="Nhập tên sản phẩm, SKU hoặc barcode..." prepend-inner-icon="ri-search-line" hide-details
              density="comfortable" @keyup.enter="searchProductsForStocktake" />

            <VBtn color="primary" prepend-icon="ri-search-line" :loading="searchLoading"
              @click="searchProductsForStocktake">
              Tìm hàng
            </VBtn>
          </div>

          <div v-if="searchProductsResult.length" class="product-result-list">
            <button v-for="product in searchProductsResult" :key="product.id" type="button" class="product-result-item"
              @click="addProductToStocktake(product)">
              <span>
                <strong>{{ product.name }}</strong>
                <small>{{ product.code }} · Tồn hệ thống: {{ formatNumber(product.quantityOnHand ?? 0) }} {{
                  product.unitName }}</small>
              </span>

              <VIcon icon="ri-add-line" />
            </button>
          </div>

          <div class="dialog-section-title">
            Danh sách sản phẩm kiểm kê
          </div>

          <div class="inventory-table-wrap">
            <VTable class="inventory-table compact-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th class="text-end">Tồn hệ thống</th>
                  <th class="text-end">Tồn thực tế</th>
                  <th class="text-end">Chênh lệch</th>
                  <th>Ghi chú</th>
                  <th class="text-center">Xóa</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="line in sessionLines" :key="line.product.id">
                  <td class="font-weight-bold">
                    {{ line.product.name }}
                  </td>

                  <td>{{ line.product.code }}</td>

                  <td class="text-end">
                    {{ formatNumber(line.product.quantityOnHand ?? 0) }}
                  </td>

                  <td>
                    <VTextField v-model.number="line.actualQty" type="number" density="compact" hide-details
                      class="quantity-field" />
                  </td>

                  <td class="text-end font-weight-bold"
                    :class="line.actualQty - (line.product.quantityOnHand ?? 0) !== 0 ? 'text-warning' : 'text-success'">
                    {{ line.actualQty - (line.product.quantityOnHand ?? 0) > 0 ? '+' : '' }}{{
                      formatNumber(line.actualQty -
                        (line.product.quantityOnHand ?? 0)) }}
                  </td>

                  <td>
                    <VTextField v-model="line.note" placeholder="Ghi chú dòng..." density="compact" hide-details />
                  </td>

                  <td class="text-center">
                    <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                      @click="removeProductFromStocktake(line.product.id)" />
                  </td>
                </tr>

                <tr v-if="!sessionLines.length">
                  <td colspan="7" class="text-center text-medium-emphasis py-6">
                    Chưa thêm sản phẩm kiểm kê nào. Vui lòng sử dụng thanh tìm kiếm.
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="createSessionDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleCreateSession">
            Lưu bản nháp kiểm kho
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="detailSessionDialog" max-width="940" scrollable>
      <VCard class="stocktake-dialog">
        <div v-if="selectedSession" class="dialog-head">
          <div>
            <span>Chi tiết phiên kiểm kê</span>
            <h2>{{ selectedSession.code }}</h2>
          </div>

          <VChip :color="sessionStatusColor(selectedSession.status)" variant="tonal">
            {{ sessionStatusLabel(selectedSession.status) }}
          </VChip>
        </div>

        <VCardText v-if="loadingSessionDetail">
          <VSkeletonLoader type="heading, paragraph, table-heading, table-tbody" />
        </VCardText>

        <template v-else-if="selectedSession">
          <VCardText>
            <div class="detail-grid">
              <div class="detail-card">
                <span>Người tạo</span>
                <strong>{{ selectedSession.createdByName }}</strong>
                <p>{{ formatDate(selectedSession.createdAt) }}</p>
              </div>

              <div class="detail-card">
                <span>Ngày xác nhận</span>
                <strong>{{ formatDate(selectedSession.confirmedAt || '') }}</strong>
                <p>{{ selectedSession.status === 'Confirmed' ? 'Đã cập nhật tồn kho' : 'Chưa cập nhật tồn kho' }}</p>
              </div>

              <div class="detail-card">
                <span>Ghi chú</span>
                <strong>{{ selectedSession.note || '—' }}</strong>
                <p>{{ selectedSession.details.length }} dòng kiểm kê</p>
              </div>
            </div>

            <div class="dialog-section-title">
              Chi tiết chênh lệch kiểm kê
            </div>

            <div class="inventory-table-wrap">
              <VTable class="inventory-table compact-table">
                <thead>
                  <tr>
                    <th>Sản phẩm</th>
                    <th>Mã hàng</th>
                    <th>ĐVT</th>
                    <th class="text-end">Tồn hệ thống</th>
                    <th class="text-end">Tồn thực tế</th>
                    <th class="text-end">Chênh lệch</th>
                    <th>Ghi chú</th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="line in selectedSession.details" :key="line.id">
                    <td class="font-weight-bold">
                      {{ line.productName }}
                    </td>

                    <td>{{ line.productCode }}</td>
                    <td>{{ line.unitName }}</td>

                    <td class="text-end">
                      {{ formatNumber(line.systemQuantity) }}
                    </td>

                    <td class="text-end font-weight-bold">
                      {{ formatNumber(line.actualQuantity) }}
                    </td>

                    <td class="text-end font-weight-bold"
                      :class="line.differenceQuantity !== 0 ? 'text-error' : 'text-success'">
                      {{ line.differenceQuantity > 0 ? '+' : '' }}{{ formatNumber(line.differenceQuantity) }}
                    </td>

                    <td>{{ line.note || '—' }}</td>
                  </tr>
                </tbody>
              </VTable>
            </div>
          </VCardText>

          <VCardActions class="dialog-actions">
            <VBtn v-if="selectedSession.status === 'Draft'" color="success" variant="tonal"
              prepend-icon="ri-checkbox-circle-line" :loading="loading" @click="handleConfirmStocktake">
              Xác nhận cân kho
            </VBtn>

            <VBtn v-if="selectedSession.status === 'Draft'" color="error" variant="tonal"
              prepend-icon="ri-close-circle-line" :loading="loading" @click="handleCancelStocktake">
              Hủy kiểm kê
            </VBtn>

            <VSpacer />

            <VBtn color="secondary" variant="outlined" @click="detailSessionDialog = false">
              Đóng
            </VBtn>
          </VCardActions>
        </template>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.inventory-page {
  position: relative;
  isolation: isolate;
}

.inventory-page::before {
  content: '';
  position: absolute;
  inset: -2rem -2rem auto;
  z-index: -1;
  block-size: 320px;
  border-radius: 0 0 44px 44px;
  background:
    radial-gradient(circle at 16% 12%, rgba(var(--v-theme-primary), 0.17), transparent 34%),
    radial-gradient(circle at 86% 4%, rgba(var(--v-theme-info), 0.14), transparent 32%),
    linear-gradient(135deg, rgba(var(--v-theme-primary), 0.08), transparent 58%);
  pointer-events: none;
}

.primary-action {
  color: white !important;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info))) !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-primary), 0.28);
}

.inventory-tabs-card,
.inventory-panel,
.stocktake-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.inventory-tabs-card :deep(.v-card-text) {
  padding: 0.9rem;
}

.inventory-tabs {
  display: grid;
  gap: 0.55rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  border: 1px solid rgba(var(--v-border-color), 0.1);
  border-radius: 18px;
  padding: 0.4rem;
  background: rgba(var(--v-theme-background), 0.72);
}

.inventory-tab-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  min-block-size: 48px;
  border: 0;
  border-radius: 15px;
  color: rgba(var(--v-theme-on-surface), 0.7);
  background: transparent;
  font: inherit;
  font-weight: 850;
  cursor: pointer;
  transition:
    color 180ms ease,
    background 180ms ease,
    box-shadow 180ms ease;
}

.inventory-tab-button:hover,
.inventory-tab-button--active {
  color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-surface));
  box-shadow:
    0 10px 28px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.16);
}

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.inventory-summary-grid {
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.summary-card {
  display: flex;
  align-items: flex-start;
  gap: 0.85rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 22px;
  padding: 1rem;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 16px 42px rgba(15, 23, 42, 0.07);
}

.summary-icon {
  display: grid;
  place-items: center;
  flex: 0 0 42px;
  inline-size: 42px;
  block-size: 42px;
  border-radius: 14px;
  color: white;
}

.summary-icon--primary {
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info)));
}

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--info {
  background: rgb(var(--v-theme-info));
}

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
}

.summary-icon--error {
  background: rgb(var(--v-theme-error));
}

.summary-card span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 800;
  text-transform: uppercase;
}

.summary-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.22rem;
  font-weight: 900;
  letter-spacing: -0.035em;
  margin-block: 0.2rem;
}

.summary-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
}

.inventory-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(190px, 260px) auto auto;
}

.inventory-toolbar :deep(.v-field),
.stocktake-dialog :deep(.v-field) {
  border-radius: 16px;
}

.inventory-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.inventory-table-wrap {
  overflow-x: auto;
}

.inventory-table {
  min-inline-size: 920px;
}

.inventory-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.inventory-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.inventory-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.inventory-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.sku-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.product-cell strong,
.product-cell span {
  display: block;
}

.product-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.product-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.reserved-cell {
  color: rgb(var(--v-theme-info));
  font-weight: 850;
}

.available-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 950;
}

.note-cell {
  display: -webkit-box;
  max-inline-size: 320px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.inventory-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.inventory-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.inventory-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.inventory-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.inventory-pagination>div {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.page-size-select {
  inline-size: 108px;
}

.session-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.session-panel-head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 900;
  text-transform: uppercase;
}

.session-panel-head strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.35rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.session-panel-head p {
  margin: 0.3rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
}

.dialog-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.12);
}

.dialog-head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 900;
  text-transform: uppercase;
}

.dialog-head h2 {
  margin: 0.25rem 0 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.45rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.stocktake-search-box {
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: minmax(0, 1fr) auto;
  margin-block-end: 1rem;
}

.stocktake-search-box .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.product-result-list {
  display: grid;
  gap: 0.55rem;
  max-block-size: 220px;
  overflow-y: auto;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.65rem;
  margin-block-end: 1rem;
  background: rgb(var(--v-theme-surface));
}

.product-result-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border: 0;
  border-radius: 14px;
  padding: 0.75rem;
  color: rgb(var(--v-theme-on-surface));
  background: rgba(var(--v-theme-background), 0.56);
  font: inherit;
  text-align: start;
  cursor: pointer;
}

.product-result-item:hover {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.product-result-item strong,
.product-result-item small {
  display: block;
}

.product-result-item strong {
  font-weight: 900;
}

.product-result-item small {
  color: rgba(var(--v-theme-on-surface), 0.56);
}

.dialog-section-title {
  margin-block: 1rem 0.7rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 950;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.compact-table {
  min-inline-size: 820px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 54px !important;
}

.quantity-field {
  max-inline-size: 130px;
  margin-inline-start: auto;
}

.detail-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.detail-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.95rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.detail-card span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.detail-card strong {
  display: block;
  margin-block: 0.3rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
  font-weight: 900;
}

.detail-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1380px) {
  .inventory-summary-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}@media (max-width: 1200px) {

  .summary-grid,
  .inventory-summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .inventory-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .inventory-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .inventory-summary-grid,
  .inventory-toolbar,
  .inventory-tabs,
  .stocktake-search-box,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .inventory-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .inventory-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .session-panel-head {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.inventory-hero {
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

.inventory-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.inventory-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.inventory-hero__copy {
  display: none !important;
}

.inventory-hero__actions,
.inventory-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.inventory-hero__actions .v-btn,
.inventory-actions .v-btn,
.inventory-hero__actions .v-btn.primary-action,
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