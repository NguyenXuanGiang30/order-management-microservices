<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  getInventoryBalanceReport,
  type InventoryBalanceReportDto,
} from '@/services/productInventoryApi'

import { getApiBaseUrl } from '@/services/authApi'
import { readAuthSession } from '@/stores/authSession'

const reportData = ref<InventoryBalanceReportDto[]>([])
const loading = ref(false)
const errorMessage = ref('')
const search = ref('')

const toDateInputValue = (date: Date) => {
  const offset = date.getTimezoneOffset()
  const localDate = new Date(date.getTime() - offset * 60 * 1000)

  return localDate.toISOString().split('T')[0]
}

const getTodayString = () => toDateInputValue(new Date())

const get30DaysAgoString = () => {
  const date = new Date()

  date.setDate(date.getDate() - 30)

  return toDateInputValue(date)
}

const startDate = ref(get30DaysAgoString())
const endDate = ref(getTodayString())

const page = ref(1)
const pageSize = ref(10)

const totalPages = computed(() =>
  Math.max(1, Math.ceil(reportData.value.length / pageSize.value)),
)

const paginatedReportData = computed(() => {
  const start = (page.value - 1) * pageSize.value

  return reportData.value.slice(start, start + pageSize.value)
})

const rangeStart = computed(() => {
  if (!reportData.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, reportData.value.length),
)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const totalSku = computed(() => reportData.value.length)

const totalOpening = computed(() =>
  reportData.value.reduce((sum, item) => sum + item.openingStock, 0),
)

const totalReceived = computed(() =>
  reportData.value.reduce((sum, item) => sum + item.receivedQuantity, 0),
)

const totalShipped = computed(() =>
  reportData.value.reduce((sum, item) => sum + item.shippedQuantity, 0),
)

const totalClosing = computed(() =>
  reportData.value.reduce((sum, item) => sum + item.closingStock, 0),
)

const netChange = computed(() =>
  totalClosing.value - totalOpening.value,
)

const activeSkuCount = computed(() =>
  reportData.value.filter(item => item.receivedQuantity > 0 || item.shippedQuantity > 0).length,
)

const summaryCards = computed(() => [
  {
    label: 'Tổng SKU',
    value: formatNumber(totalSku.value),
    helper: `${formatNumber(activeSkuCount.value)} SKU có phát sinh trong kỳ`,
    icon: 'ri-price-tag-3-line',
    color: 'primary',
  },
  {
    label: 'Tồn đầu kỳ',
    value: formatNumber(totalOpening.value),
    helper: 'Tổng lượng tồn tại thời điểm bắt đầu',
    icon: 'ri-archive-stack-line',
    color: 'info',
  },
  {
    label: 'Nhập trong kỳ',
    value: `+${formatNumber(totalReceived.value)}`,
    helper: 'Tổng số lượng nhập kho',
    icon: 'ri-download-2-line',
    color: 'success',
  },
  {
    label: 'Xuất trong kỳ',
    value: `-${formatNumber(totalShipped.value)}`,
    helper: `Chênh lệch tồn: ${netChange.value >= 0 ? '+' : ''}${formatNumber(netChange.value)}`,
    icon: 'ri-upload-2-line',
    color: 'warning',
  },
])

const getStockColor = (item: InventoryBalanceReportDto) => {
  if (item.closingStock <= 0)
    return 'error'

  if (item.closingStock < item.openingStock)
    return 'warning'

  return 'success'
}

const getStockTrendLabel = (item: InventoryBalanceReportDto) => {
  const diff = item.closingStock - item.openingStock

  if (diff > 0)
    return `+${formatNumber(diff)}`

  if (diff < 0)
    return formatNumber(diff)

  return 'Không đổi'
}

const toStartIso = (value: string) => {
  if (!value)
    return undefined

  return new Date(`${value}T00:00:00`).toISOString()
}

const toEndIso = (value: string) => {
  if (!value)
    return undefined

  return new Date(`${value}T23:59:59`).toISOString()
}

const loadReport = async () => {
  loading.value = true
  errorMessage.value = ''
  page.value = 1

  try {
    reportData.value = await getInventoryBalanceReport({
      startDate: toStartIso(startDate.value),
      endDate: toEndIso(endDate.value),
      search: search.value.trim(),
    })
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải báo cáo Nhập Xuất Tồn.'
  }
  finally {
    loading.value = false
  }
}

const setPreset = (presetName: 'today' | '7days' | '30days' | 'thisMonth') => {
  const today = new Date()

  if (presetName === 'today') {
    startDate.value = toDateInputValue(today)
    endDate.value = toDateInputValue(today)
  }

  if (presetName === '7days') {
    const date = new Date()

    date.setDate(date.getDate() - 7)

    startDate.value = toDateInputValue(date)
    endDate.value = toDateInputValue(today)
  }

  if (presetName === '30days') {
    const date = new Date()

    date.setDate(date.getDate() - 30)

    startDate.value = toDateInputValue(date)
    endDate.value = toDateInputValue(today)
  }

  if (presetName === 'thisMonth') {
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1)

    startDate.value = toDateInputValue(firstDay)
    endDate.value = toDateInputValue(today)
  }

  void loadReport()
}

const clearSearch = () => {
  search.value = ''
  void loadReport()
}

const handleExportCsv = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const session = readAuthSession()
    const headers: Record<string, string> = {}

    if (session?.accessToken)
      headers.Authorization = `Bearer ${session.accessToken}`

    const queryParams = new URLSearchParams()

    if (startDate.value)
      queryParams.append('startDate', toStartIso(startDate.value) ?? '')

    if (endDate.value)
      queryParams.append('endDate', toEndIso(endDate.value) ?? '')

    if (search.value.trim())
      queryParams.append('search', search.value.trim())

    const response = await fetch(`${getApiBaseUrl()}/api/inventory/balance-report/export?${queryParams.toString()}`, {
      headers,
    })

    if (!response.ok)
      throw new Error('Lỗi xuất dữ liệu từ máy chủ.')

    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.setAttribute('download', `bao-cao-nxt-${startDate.value}-to-${endDate.value}.csv`)

    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)

    URL.revokeObjectURL(url)
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi xuất file CSV.'
  }
  finally {
    loading.value = false
  }
}

watch(pageSize, () => {
  page.value = 1
})

watch(page, value => {
  if (value > totalPages.value)
    page.value = totalPages.value
})

onMounted(loadReport)
</script>

<template>
  <section class="nxt-page">
    <div class="nxt-hero">
      <div class="nxt-hero__title-area">
        <h1>Báo cáo Nhập - Xuất - Tồn</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-file-chart-line" class="ml-2">
          Kho hàng
        </VChip>
      </div>

      <div class="nxt-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadReport">
          Tải lại
        </VBtn>

        <VBtn color="success" prepend-icon="ri-file-download-line" class="export-action" :loading="loading"
          @click="handleExportCsv">
          Xuất CSV
        </VBtn>
      </div>
    </div>

    <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
      {{ errorMessage }}
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

    <VCard class="nxt-panel">
      <VCardText>
        <div class="nxt-toolbar">
          <div class="date-group">
            <VTextField v-model="startDate" type="date" label="Từ ngày" density="comfortable" hide-details
              @change="loadReport" />

            <VTextField v-model="endDate" type="date" label="Đến ngày" density="comfortable" hide-details
              @change="loadReport" />
          </div>

          <VTextField v-model="search" label="Tìm sản phẩm" placeholder="Nhập tên sản phẩm hoặc mã SKU..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable @keyup.enter="loadReport"
            @click:clear="clearSearch" />

          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="loadReport">
            Lọc
          </VBtn>
        </div>

        <div class="preset-strip">
          <button type="button" class="preset-chip" @click="setPreset('today')">
            Hôm nay
          </button>

          <button type="button" class="preset-chip" @click="setPreset('7days')">
            7 ngày qua
          </button>

          <button type="button" class="preset-chip" @click="setPreset('30days')">
            30 ngày qua
          </button>

          <button type="button" class="preset-chip" @click="setPreset('thisMonth')">
            Tháng này
          </button>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="reportData.length" class="nxt-table-wrap">
          <VTable class="nxt-table">
            <thead>
              <tr>
                <th>Mã SKU</th>
                <th>Sản phẩm</th>
                <th>ĐVT</th>
                <th class="text-end">Tồn đầu kỳ</th>
                <th class="text-end">Nhập trong kỳ</th>
                <th class="text-end">Xuất trong kỳ</th>
                <th class="text-end">Tồn cuối kỳ</th>
                <th class="text-center">Biến động</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="item in paginatedReportData" :key="item.productId" class="nxt-row">
                <td>
                  <div class="sku-cell">
                    {{ item.productCode }}
                  </div>
                </td>

                <td>
                  <div class="product-cell">
                    <strong>{{ item.productName }}</strong>
                    <span>{{ item.productCode }}</span>
                  </div>
                </td>

                <td>
                  <VChip size="small" variant="tonal" color="secondary">
                    {{ item.unitName }}
                  </VChip>
                </td>

                <td class="text-end font-weight-bold">
                  {{ formatNumber(item.openingStock) }}
                </td>

                <td class="text-end received-cell">
                  +{{ formatNumber(item.receivedQuantity) }}
                </td>

                <td class="text-end shipped-cell">
                  -{{ formatNumber(item.shippedQuantity) }}
                </td>

                <td class="text-end closing-cell">
                  {{ formatNumber(item.closingStock) }}
                </td>

                <td class="text-center">
                  <VChip :color="getStockColor(item)" size="small" variant="tonal">
                    {{ getStockTrendLabel(item) }}
                  </VChip>
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="nxt-empty">
          <VIcon icon="ri-bar-chart-box-line" size="42" color="primary" />
          <strong>Không có dữ liệu báo cáo</strong>
          <span>Hãy thử thay đổi mốc thời gian hoặc từ khóa tìm kiếm.</span>

          <VBtn color="primary" prepend-icon="ri-refresh-line" @click="loadReport">
            Tải lại báo cáo
          </VBtn>
        </div>
      </template>

      <div v-if="reportData.length" class="nxt-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }} trên tổng số {{
            formatNumber(reportData.length)
          }} sản phẩm
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>
  </section>
</template>

<style scoped>
.nxt-page {
  position: relative;
  isolation: isolate;
}

.nxt-page::before {
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

.export-action {
  color: white !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-success), 0.24);
}

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-end: 1rem;
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

.summary-icon--info {
  background: rgb(var(--v-theme-info));
}

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
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

.nxt-panel {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.nxt-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 360px) minmax(260px, 1fr) auto;
}

.date-group {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.nxt-toolbar :deep(.v-field) {
  border-radius: 16px;
}

.nxt-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.preset-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
  margin-block-start: 1rem;
}

.preset-chip {
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 999px;
  padding: 0.52rem 0.85rem;
  color: rgba(var(--v-theme-on-surface), 0.72);
  background: rgba(var(--v-theme-background), 0.5);
  font: inherit;
  font-size: 0.84rem;
  font-weight: 800;
  cursor: pointer;
  transition:
    background 180ms ease,
    border-color 180ms ease,
    color 180ms ease;
}

.preset-chip:hover {
  border-color: rgba(var(--v-theme-primary), 0.28);
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.nxt-table-wrap {
  overflow-x: auto;
}

.nxt-table {
  min-inline-size: 980px;
}

.nxt-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.nxt-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.nxt-row {
  transition: background 160ms ease;
}

.nxt-row:hover {
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

.received-cell {
  color: rgb(var(--v-theme-success));
  font-weight: 900;
}

.shipped-cell {
  color: rgb(var(--v-theme-warning));
  font-weight: 900;
}

.closing-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 950;
}

.nxt-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.nxt-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.nxt-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.nxt-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.nxt-pagination>div {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.page-size-select {
  inline-size: 108px;
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .nxt-toolbar {
    grid-template-columns: 1fr;
  }

  .nxt-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .date-group {
    grid-template-columns: 1fr;
  }

  .nxt-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .nxt-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.nxt-hero {
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

.nxt-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.nxt-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.nxt-hero__copy {
  display: none !important;
}

.nxt-hero__actions,
.nxt-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.nxt-hero__actions .v-btn,
.nxt-actions .v-btn,
.nxt-hero__actions .v-btn.primary-action,
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