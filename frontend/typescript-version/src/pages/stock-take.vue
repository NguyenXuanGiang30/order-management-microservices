<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'

import {
  type ProductDto,
  type StocktakeSessionDto,
  cancelStocktake,
  confirmStocktake,
  createStocktakeSession,
  getProducts,
  getStocktakeSession,
  getStocktakeSessions,
  getStocktakeTemplate,
  importStocktakeCounts,
} from '@/services/productInventoryApi'

const sessions = ref<StocktakeSessionDto[]>([])
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const detailDialog = ref(false)
const selectedSession = ref<StocktakeSessionDto | null>(null)
const selectedSessionDetails = ref<NonNullable<StocktakeSessionDto['details']>>([])

const createDialog = ref(false)
const sessionNote = ref('')
const stocktakeItems = ref<Array<{
  product: ProductDto
  actualQuantity: number
  note: string
}>>([])

const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])
const searchLoading = ref(false)

const csvFileInput = ref<HTMLInputElement | null>(null)

const moneyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const formatCurrency = (value: number) => moneyFormatter.format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatDate = (value?: string | null) => {
  if (!value)
    return '—'

  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date)
}

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const statusLabel = (status: string) => {
  const map: Record<string, string> = {
    Draft: 'Bản nháp',
    Confirmed: 'Đã xác nhận',
    Cancelled: 'Đã hủy',
  }

  return map[status] ?? status
}

const statusColor = (status: string) => {
  const map: Record<string, string> = {
    Draft: 'warning',
    Confirmed: 'success',
    Cancelled: 'error',
  }

  return map[status] ?? 'secondary'
}

const differenceColor = (value: number) => {
  if (value > 0)
    return 'success'

  if (value < 0)
    return 'error'

  return 'secondary'
}

const totalSessions = computed(() => sessions.value.length)

const draftSessionsCount = computed(() =>
  sessions.value.filter(session => session.status === 'Draft').length,
)

const confirmedSessionsCount = computed(() =>
  sessions.value.filter(session => session.status === 'Confirmed').length,
)

const cancelledSessionsCount = computed(() =>
  sessions.value.filter(session => session.status === 'Cancelled').length,
)

const totalLinesOnPage = computed(() =>
  sessions.value.reduce((sum, session) => sum + (session.details?.length || 0), 0),
)

const stocktakeDraftQuantity = computed(() =>
  stocktakeItems.value.reduce((sum, item) => sum + (Number(item.actualQuantity) || 0), 0),
)

const selectedSessionDifference = computed(() =>
  selectedSessionDetails.value.reduce((sum, line) => sum + (line.differenceQuantity || 0), 0),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng phiếu kiểm kê',
    value: formatNumber(totalSessions.value),
    helper: `${formatNumber(totalLinesOnPage.value)} dòng hàng trong danh sách`,
    icon: 'ri-barcode-box-line',
    color: 'primary',
  },
  {
    label: 'Đã xác nhận',
    value: formatNumber(confirmedSessionsCount.value),
    helper: 'Đã đồng bộ tồn kho thực tế',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Chờ xử lý',
    value: formatNumber(draftSessionsCount.value),
    helper: 'Phiếu nháp đang đếm thực tế',
    icon: 'ri-time-line',
    color: 'warning',
  },
  {
    label: 'Đã hủy',
    value: formatNumber(cancelledSessionsCount.value),
    helper: 'Phiếu không còn hiệu lực',
    icon: 'ri-close-circle-line',
    color: 'error',
  },
])

const loadSessions = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getStocktakeSessions({ pageSize: 100 })

    sessions.value = result.items
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách phiếu kiểm kê.')
  }
  finally {
    loading.value = false
  }
}

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []

    return
  }

  searchLoading.value = true
  errorMessage.value = ''

  try {
    const result = await getProducts({
      search: searchProductQuery.value.trim(),
      pageSize: 10,
    })

    searchedProducts.value = result.items
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tìm sản phẩm kiểm kê.')
  }
  finally {
    searchLoading.value = false
  }
}

const addProductToStocktake = (product: ProductDto) => {
  const existing = stocktakeItems.value.find(item => item.product.id === product.id)

  if (existing) {
    successMessage.value = 'Sản phẩm này đã có trong phiếu kiểm kê.'

    searchProductQuery.value = ''
    searchedProducts.value = []

    return
  }

  stocktakeItems.value.push({
    product,
    actualQuantity: product.quantityOnHand || 0,
    note: '',
  })

  searchProductQuery.value = ''
  searchedProducts.value = []
}

const removeProductFromStocktake = (productId: string) => {
  stocktakeItems.value = stocktakeItems.value.filter(item => item.product.id !== productId)
}

const openCreateStocktake = () => {
  stocktakeItems.value = []
  sessionNote.value = ''
  searchProductQuery.value = ''
  searchedProducts.value = []
  createDialog.value = true
}

const validateStocktake = () => {
  if (!stocktakeItems.value.length)
    return 'Vui lòng thêm ít nhất một sản phẩm để kiểm kê.'

  for (const item of stocktakeItems.value) {
    if (item.actualQuantity < 0)
      return `Số thực tế của ${item.product.name} không được âm.`
  }

  return ''
}

const handleSaveStocktake = async () => {
  const validationMessage = validateStocktake()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createStocktakeSession({
      note: sessionNote.value || null,
      details: stocktakeItems.value.map(item => ({
        productId: item.product.id,
        actualQuantity: Number(item.actualQuantity) || 0,
        note: item.note || null,
      })),
    })

    createDialog.value = false
    successMessage.value = 'Tạo phiếu kiểm kê bản nháp thành công.'

    await loadSessions()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi lập phiếu kiểm kê.')
  }
  finally {
    loading.value = false
  }
}

const loadSessionDetails = async (id: string) => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getStocktakeSession(id)

    selectedSession.value = result
    selectedSessionDetails.value = result.details || []
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải chi tiết phiếu kiểm kê.')
  }
  finally {
    loading.value = false
  }
}

const openSessionDetails = async (session: StocktakeSessionDto) => {
  selectedSession.value = session
  selectedSessionDetails.value = []
  detailDialog.value = true

  await loadSessionDetails(session.id)
}

const downloadTemplateFile = async (sessionId: string, code: string) => {
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const csvContent = await getStocktakeTemplate(sessionId)
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.setAttribute('download', `stocktake_template_${code}.csv`)

    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)

    URL.revokeObjectURL(url)

    successMessage.value = 'Đã tải file mẫu kiểm kê.'
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi khi tải file mẫu.')
  }
}

const triggerCsvImport = () => {
  csvFileInput.value?.click()
}

const handleCsvUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]

  if (!file || !selectedSession.value)
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const updated = await importStocktakeCounts(selectedSession.value.id, file)

    if (updated) {
      successMessage.value = 'Nhập file CSV kiểm kê thành công.'

      await loadSessionDetails(selectedSession.value.id)
    }
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi khi nhập file CSV.')
  }
  finally {
    loading.value = false
    target.value = ''
  }
}

const handleConfirmStocktake = async () => {
  if (!selectedSession.value)
    return

  if (!confirm('Bạn có chắc muốn xác nhận phiếu kiểm kê này? Số lượng tồn kho thực tế sẽ được cập nhật và ghi nhận chênh lệch.'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await confirmStocktake(selectedSession.value.id)

    detailDialog.value = false
    successMessage.value = 'Xác nhận kiểm kê thành công. Tồn kho đã được cập nhật.'

    await loadSessions()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi xác nhận kiểm kê.')
  }
  finally {
    loading.value = false
  }
}

const handleCancelStocktake = async () => {
  if (!selectedSession.value)
    return

  if (!confirm('Bạn có chắc muốn hủy phiếu kiểm kê này?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await cancelStocktake(selectedSession.value.id)

    detailDialog.value = false
    successMessage.value = 'Đã hủy phiếu kiểm kê.'

    await loadSessions()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi hủy phiếu kiểm kê.')
  }
  finally {
    loading.value = false
  }
}

const sessionActions = (session: StocktakeSessionDto): ActionMenuItem[] => [
  {
    label: 'Xem chi tiết',
    icon: 'ri-eye-line',
    handler: () => {
      void openSessionDetails(session)
    },
  },
  {
    label: 'Tải file mẫu nhập',
    icon: 'ri-download-line',
    handler: () => {
      void downloadTemplateFile(session.id, session.code)
    },
    show: session.status === 'Draft',
  },
  {
    label: 'Xác nhận kiểm kê',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
    handler: () => {
      selectedSession.value = session
      void handleConfirmStocktake()
    },
    show: session.status === 'Draft',
  },
  {
    label: 'Hủy phiếu',
    icon: 'ri-close-circle-line',
    color: 'error',
    handler: () => {
      selectedSession.value = session
      void handleCancelStocktake()
    },
    show: session.status === 'Draft',
  },
]

onMounted(async () => {
  await loadSessions()
})
</script>

<template>
  <section class="stocktake-page">
    <div class="stocktake-hero">
      <div class="stocktake-hero__title-area">
        <h1>Kiểm kê kho</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-barcode-box-line" class="ml-2">
          Quản lý tồn kho
        </VChip>
      </div>

      <div class="stocktake-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadSessions">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-add-line" class="primary-action" @click="openCreateStocktake">
          Tạo phiếu kiểm kê
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

    <div class="summary-grid">
      <article v-for="card in summaryCards" :key="card.label" class="summary-card">
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

    <div class="stocktake-layout">
      <VCard class="stocktake-panel">
        <div class="panel-head">
          <div>
            <span>Danh sách</span>
            <strong>Phiếu kiểm kê kho</strong>
            <p>Quản lý phiếu nháp, phiếu đã xác nhận và phiếu đã hủy.</p>
          </div>

          <VChip color="primary" variant="tonal">
            {{ formatNumber(totalSessions) }} phiếu
          </VChip>
        </div>

        <VDivider />

        <VCardText v-if="loading">
          <VSkeletonLoader type="table-heading, table-tbody" />
        </VCardText>

        <template v-else>
          <div v-if="sessions.length" class="stocktake-table-wrap">
            <VTable class="stocktake-table">
              <thead>
                <tr>
                  <th>Mã kiểm kê</th>
                  <th>Người kiểm</th>
                  <th>Ngày tạo</th>
                  <th class="text-end">Mặt hàng</th>
                  <th>Ghi chú</th>
                  <th class="text-center">Trạng thái</th>
                  <th class="text-center">Thao tác</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="session in sessions" :key="session.id" class="stocktake-row"
                  @click="openSessionDetails(session)">
                  <td>
                    <div class="stocktake-code">
                      {{ session.code }}
                    </div>
                  </td>

                  <td>{{ session.createdByName }}</td>
                  <td>{{ formatDate(session.createdAt) }}</td>

                  <td class="text-end font-weight-bold">
                    {{ formatNumber(session.details?.length || 0) }}
                  </td>

                  <td>
                    <div class="note-cell" :title="session.note || ''">
                      {{ session.note || '—' }}
                    </div>
                  </td>

                  <td class="text-center">
                    <VChip :color="statusColor(session.status)" variant="tonal" size="small">
                      {{ statusLabel(session.status) }}
                    </VChip>
                  </td>

                  <td class="text-center" @click.stop>
                    <RetailActionMenu :items="sessionActions(session)" />
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div v-else class="stocktake-empty">
            <VIcon icon="ri-barcode-box-line" size="42" color="primary" />

            <strong>Chưa có đợt kiểm kê nào</strong>
            <span>Hãy tạo phiếu kiểm kê mới để đối soát lượng tồn thực tế.</span>

            <VBtn color="primary" prepend-icon="ri-add-line" @click="openCreateStocktake">
              Tạo phiếu kiểm kê
            </VBtn>
          </div>
        </template>
      </VCard>

      <aside class="stocktake-aside">
        <VCard class="stocktake-panel guide-card">
          <div class="aside-head">
            <span>Quy trình</span>
            <strong>Quy trình kiểm kê</strong>
          </div>

          <div class="guide-list">
            <div>
              <VIcon color="primary" icon="ri-draft-line" />
              <span>Tạo phiếu nháp và thêm các sản phẩm cần kiểm kê.</span>
            </div>

            <div>
              <VIcon color="info" icon="ri-file-excel-line" />
              <span>Cập nhật số đếm thực tế bằng tay hoặc nhập từ file CSV.</span>
            </div>

            <div>
              <VIcon color="warning" icon="ri-scales-3-line" />
              <span>Đối chiếu chênh lệch giữa tồn hệ thống và số thực tế.</span>
            </div>

            <div>
              <VIcon color="success" icon="ri-checkbox-circle-line" />
              <span>Xác nhận để cập nhật tồn kho và ghi nhận biến động.</span>
            </div>
          </div>
        </VCard>
      </aside>
    </div>

    <VDialog v-model="createDialog" max-width="980" persistent scrollable>
      <VCard class="stocktake-dialog">
        <div class="dialog-head">
          <div>
            <span>Tạo phiếu kiểm kê</span>
            <h2>Phiếu kiểm kê mới</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ formatNumber(stocktakeItems.length) }} SKU
          </VChip>
        </div>

        <VCardText>
          <VTextField v-model="sessionNote" label="Ghi chú đợt kiểm kê" placeholder="Ví dụ: Kiểm kê định kỳ tháng 6..."
            density="comfortable" class="mb-4" />

          <div class="draft-summary-strip">
            <div>
              <span>Số SKU</span>
              <strong>{{ formatNumber(stocktakeItems.length) }}</strong>
            </div>

            <div>
              <span>Tổng số thực tế</span>
              <strong>{{ formatNumber(stocktakeDraftQuantity) }}</strong>
            </div>

            <div>
              <span>Trạng thái</span>
              <strong>Bản nháp</strong>
            </div>
          </div>

          <VDivider class="my-4" />

          <div class="product-search-box">
            <VTextField v-model="searchProductQuery" label="Tìm sản phẩm kiểm kê"
              placeholder="Nhập tên sản phẩm, mã hàng hoặc barcode..." prepend-inner-icon="ri-search-line"
              density="comfortable" hide-details @keyup.enter="searchProducts" />

            <VBtn color="primary" prepend-icon="ri-search-line" :loading="searchLoading" @click="searchProducts">
              Tìm
            </VBtn>
          </div>

          <div v-if="searchedProducts.length" class="product-result-list">
            <button v-for="product in searchedProducts" :key="product.id" type="button" class="product-result-item"
              @click="addProductToStocktake(product)">
              <span>
                <strong>{{ product.name }}</strong>
                <small>
                  {{ product.code }} · Tồn hệ thống:
                  {{ formatNumber(product.quantityOnHand ?? 0) }} {{ product.unitName }}
                </small>
              </span>

              <VIcon icon="ri-add-line" />
            </button>
          </div>

          <div class="stocktake-items-wrap">
            <VTable class="stocktake-table compact-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th>ĐVT</th>
                  <th class="text-end">Tồn hệ thống</th>
                  <th class="text-end">Số thực tế</th>
                  <th>Ghi chú dòng</th>
                  <th class="text-center">Xóa</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in stocktakeItems" :key="item.product.id">
                  <td class="font-weight-bold">
                    {{ item.product.name }}
                  </td>

                  <td>
                    <span class="stocktake-code">{{ item.product.code }}</span>
                  </td>

                  <td>{{ item.product.unitName }}</td>

                  <td class="text-end font-weight-bold">
                    {{ formatNumber(item.product.quantityOnHand ?? 0) }}
                  </td>

                  <td>
                    <VTextField v-model.number="item.actualQuantity" type="number" density="compact" hide-details
                      min="0" class="number-input" />
                  </td>

                  <td>
                    <VTextField v-model="item.note" placeholder="Lý do chênh lệch nếu có" density="compact"
                      hide-details />
                  </td>

                  <td class="text-center">
                    <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                      @click="removeProductFromStocktake(item.product.id)" />
                  </td>
                </tr>

                <tr v-if="!stocktakeItems.length">
                  <td colspan="7" class="text-center text-medium-emphasis py-6">
                    Chưa chọn sản phẩm kiểm kê. Vui lòng tìm kiếm phía trên.
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="createDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleSaveStocktake">
            Tạo bản nháp
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="detailDialog" max-width="980" scrollable>
      <VCard v-if="selectedSession" class="stocktake-dialog">
        <div class="dialog-head">
          <div>
            <span>Chi tiết kiểm kê</span>
            <h2>{{ selectedSession.code }}</h2>
          </div>

          <VChip :color="statusColor(selectedSession.status)" variant="tonal">
            {{ statusLabel(selectedSession.status) }}
          </VChip>
        </div>

        <VCardText>
          <div class="detail-grid">
            <div class="detail-card">
              <span>Người lập</span>
              <strong>{{ selectedSession.createdByName }}</strong>
              <p>{{ formatDate(selectedSession.createdAt) }}</p>
            </div>

            <div class="detail-card">
              <span>Ghi chú</span>
              <strong>{{ selectedSession.note || 'Không có ghi chú' }}</strong>
              <p v-if="selectedSession.confirmedAt">
                Xác nhận: {{ formatDate(selectedSession.confirmedAt) }}
              </p>
              <p v-else>
                Chưa xác nhận
              </p>
            </div>

            <div class="detail-card">
              <span>Chênh lệch tổng</span>
              <strong :class="`text-${differenceColor(selectedSessionDifference)}`">
                {{ selectedSessionDifference > 0 ? '+' : '' }}{{ formatNumber(selectedSessionDifference) }}
              </strong>
              <p>{{ formatNumber(selectedSessionDetails.length) }} dòng kiểm kê</p>
            </div>
          </div>

          <div v-if="selectedSession.status === 'Draft'" class="csv-action-box">
            <VBtn variant="tonal" prepend-icon="ri-download-line" color="secondary"
              @click="downloadTemplateFile(selectedSession.id, selectedSession.code)">
              Tải mẫu CSV kiểm kê
            </VBtn>

            <VBtn variant="tonal" prepend-icon="ri-upload-line" color="success" :loading="loading"
              @click="triggerCsvImport">
              Nhập thực tế từ CSV
            </VBtn>

            <input ref="csvFileInput" type="file" accept=".csv" class="d-none" @change="handleCsvUpload">
          </div>

          <div class="stocktake-items-wrap">
            <VTable class="stocktake-table compact-table">
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
                <tr v-for="line in selectedSessionDetails" :key="line.id">
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

                  <td class="text-end font-weight-bold">
                    <span :class="`text-${differenceColor(line.differenceQuantity)}`">
                      {{ line.differenceQuantity > 0 ? '+' : '' }}{{ formatNumber(line.differenceQuantity) }}
                    </span>
                  </td>

                  <td>
                    <div class="note-cell" :title="line.note || ''">
                      {{ line.note || '—' }}
                    </div>
                  </td>
                </tr>

                <tr v-if="!selectedSessionDetails.length">
                  <td colspan="7" class="text-center text-medium-emphasis py-6">
                    Chưa có dòng kiểm kê nào trong phiếu này.
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn v-if="selectedSession.status === 'Draft'" color="success" variant="tonal"
            prepend-icon="ri-checkbox-circle-line" :loading="loading" @click="handleConfirmStocktake">
            Xác nhận kiểm kê
          </VBtn>

          <VBtn v-if="selectedSession.status === 'Draft'" color="error" variant="tonal"
            prepend-icon="ri-close-circle-line" :loading="loading" @click="handleCancelStocktake">
            Hủy phiếu
          </VBtn>

          <VBtn color="secondary" variant="outlined" @click="detailDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.stocktake-page {
  position: relative;
  isolation: isolate;
}

.stocktake-page::before {
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

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.summary-card,
.stocktake-panel,
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

.summary-card {
  display: flex;
  align-items: flex-start;
  gap: 0.85rem;
  padding: 1rem;
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

.stocktake-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 340px;
}

.panel-head,
.aside-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
}

.panel-head span,
.aside-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.panel-head strong,
.aside-head strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.22rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.panel-head p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.stocktake-table-wrap,
.stocktake-items-wrap {
  overflow-x: auto;
}

.stocktake-table {
  min-inline-size: 900px;
}

.stocktake-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.stocktake-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.stocktake-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.stocktake-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.stocktake-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.note-cell {
  display: -webkit-box;
  max-inline-size: 240px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.62);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.stocktake-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.stocktake-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.stocktake-aside {
  display: grid;
  gap: 1rem;
}

.guide-card {
  position: sticky;
  inset-block-start: 84px;
}

.guide-list {
  display: grid;
  gap: 0.9rem;
  padding: 0 1.25rem 1.25rem;
}

.guide-list>div {
  display: grid;
  align-items: start;
  gap: 0.65rem;
  grid-template-columns: 22px minmax(0, 1fr);
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.5;
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

.stocktake-dialog :deep(.v-field) {
  border-radius: 16px;
}

.draft-summary-strip {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.draft-summary-strip>div,
.detail-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.draft-summary-strip span,
.detail-card span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.draft-summary-strip strong,
.detail-card strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.98rem;
  font-weight: 900;
}

.product-search-box {
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: minmax(0, 1fr) auto;
  margin-block-end: 1rem;
}

.product-search-box .v-btn {
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

.compact-table {
  min-inline-size: 880px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 56px !important;
}

.number-input {
  max-inline-size: 150px;
  margin-inline-start: auto;
}

.number-input :deep(input) {
  text-align: end;
}

.detail-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.detail-card p {
  margin: 0.25rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.84rem;
}

.csv-action-box {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.csv-action-box .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .stocktake-layout {
    grid-template-columns: 1fr;
  }

  .guide-card {
    position: static;
  }

  .summary-grid,
  .detail-grid,
  .draft-summary-strip {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .detail-grid,
  .draft-summary-strip,
  .product-search-box {
    grid-template-columns: 1fr;
  }

  .panel-head,
  .aside-head,
  .dialog-head {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.stocktake-hero {
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

.stocktake-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.stocktake-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.stocktake-hero__copy {
  display: none !important;
}

.stocktake-hero__actions,
.stocktake-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.stocktake-hero__actions .v-btn,
.stocktake-actions .v-btn,
.stocktake-hero__actions .v-btn.primary-action,
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