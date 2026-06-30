<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import { getApiBaseUrl } from '@/services/authApi'
import {
  type InventoryTransactionDto,
  type ProductDto,
  getInventoryTransactions,
  getProducts,
} from '@/services/productInventoryApi'
import { readAuthSession } from '@/stores/authSession'

const transactions = ref<InventoryTransactionDto[]>([])
const loading = ref(false)
const searchLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const selectedProductId = ref<string | null>(null)
const selectedProductName = ref('')
const selectedType = ref<string | null>(null)
const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])

const page = ref(1)
const totalCount = ref(0)
const totalPages = ref(1)
const pageSize = ref(20)

const transactionTypes = [
  { title: 'Tất cả loại biến động', value: null, icon: 'ri-stack-line' },
  { title: 'Nhập kho', value: 'Import', icon: 'ri-download-2-line' },
  { title: 'Xuất kho', value: 'Export', icon: 'ri-upload-2-line' },
  { title: 'Kiểm kê', value: 'Adjust', icon: 'ri-survey-line' },
  { title: 'Trả hàng', value: 'Return', icon: 'ri-arrow-go-back-line' },
]

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatQuantity = (value: number) =>
  value > 0 ? `+${formatNumber(value)}` : formatNumber(value)

const formatDate = (value: string) => {
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

const typeLabel = (type: string) => {
  const map: Record<string, string> = {
    Import: 'Nhập kho',
    Export: 'Xuất kho',
    Adjust: 'Điều chỉnh kiểm kê',
    Return: 'Khách trả hàng',
  }

  return map[type] ?? type
}

const typeColor = (type: string) => {
  const map: Record<string, string> = {
    Import: 'success',
    Export: 'error',
    Adjust: 'warning',
    Return: 'info',
  }

  return map[type] ?? 'secondary'
}

const typeIcon = (type: string) => {
  const map: Record<string, string> = {
    Import: 'ri-download-2-line',
    Export: 'ri-upload-2-line',
    Adjust: 'ri-survey-line',
    Return: 'ri-arrow-go-back-line',
  }

  return map[type] ?? 'ri-stack-line'
}

const rangeStart = computed(() => {
  if (!transactions.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalCount.value),
)

const importCount = computed(() =>
  transactions.value.filter(item => item.transactionType === 'Import').length,
)

const exportCount = computed(() =>
  transactions.value.filter(item => item.transactionType === 'Export').length,
)

const adjustCount = computed(() =>
  transactions.value.filter(item => item.transactionType === 'Adjust').length,
)

const returnCount = computed(() =>
  transactions.value.filter(item => item.transactionType === 'Return').length,
)

const totalInQuantity = computed(() =>
  transactions.value
    .filter(item => item.changeQuantity > 0)
    .reduce((sum, item) => sum + item.changeQuantity, 0),
)

const totalOutQuantity = computed(() =>
  transactions.value
    .filter(item => item.changeQuantity < 0)
    .reduce((sum, item) => sum + Math.abs(item.changeQuantity), 0),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng biến động',
    value: formatNumber(totalCount.value),
    helper: `Trang hiện tại: ${formatNumber(transactions.value.length)} dòng`,
    icon: 'ri-history-line',
    color: 'primary',
  },
  {
    label: 'Tăng tồn',
    value: `+${formatNumber(totalInQuantity.value)}`,
    helper: `${formatNumber(importCount.value)} dòng nhập kho, ${formatNumber(returnCount.value)} dòng trả hàng`,
    icon: 'ri-download-2-line',
    color: 'success',
  },
  {
    label: 'Giảm tồn',
    value: `-${formatNumber(totalOutQuantity.value)}`,
    helper: `${formatNumber(exportCount.value)} dòng xuất kho`,
    icon: 'ri-upload-2-line',
    color: 'error',
  },
  {
    label: 'Điều chỉnh',
    value: formatNumber(adjustCount.value),
    helper: 'Biến động từ kiểm kê/cân kho',
    icon: 'ri-survey-line',
    color: 'warning',
  },
])

const loadTransactions = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const params: Record<string, unknown> = {
      productId: selectedProductId.value || undefined,
      page: page.value,
      pageSize: pageSize.value,
    }

    if (selectedType.value)
      params.type = selectedType.value

    const res = await getInventoryTransactions(params as any)

    transactions.value = res.items
    totalCount.value = res.totalCount
    totalPages.value = Math.max(1, res.totalPages)
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải lịch sử biến động kho.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadTransactions()
  else
    page.value = 1
}

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []

    return
  }

  searchLoading.value = true

  try {
    const res = await getProducts({
      search: searchProductQuery.value.trim(),
      pageSize: 10,
    })

    searchedProducts.value = res.items
  }
  catch (error) {
    console.error('Lỗi tìm sản phẩm:', error)
  }
  finally {
    searchLoading.value = false
  }
}

const selectProductFilter = (product: ProductDto) => {
  selectedProductId.value = product.id
  selectedProductName.value = product.name
  searchProductQuery.value = product.name
  searchedProducts.value = []
  resetAndLoad()
}

const clearProductFilter = () => {
  selectedProductId.value = null
  selectedProductName.value = ''
  searchProductQuery.value = ''
  searchedProducts.value = []
  resetAndLoad()
}

const clearAllFilters = () => {
  selectedProductId.value = null
  selectedProductName.value = ''
  selectedType.value = null
  searchProductQuery.value = ''
  searchedProducts.value = []
  resetAndLoad()
}

const handleExport = async () => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const session = readAuthSession()
    const token = session?.accessToken

    const queryParams = new URLSearchParams()

    if (selectedProductId.value)
      queryParams.append('productId', selectedProductId.value)

    if (selectedType.value)
      queryParams.append('type', selectedType.value)

    const response = await fetch(`${getApiBaseUrl()}/api/inventory/transactions/export?${queryParams.toString()}`, {
      headers: token
        ? {
          Authorization: `Bearer ${token}`,
        }
        : {},
    })

    if (!response.ok)
      throw new Error('Không thể tải file xuất.')

    const blob = await response.blob()
    const blobUrl = URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = blobUrl
    link.setAttribute('download', `lich-su-kho-${new Date().toISOString().slice(0, 10)}.csv`)

    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)

    URL.revokeObjectURL(blobUrl)

    successMessage.value = 'Xuất file CSV thành công.'
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xuất file.'
  }
  finally {
    loading.value = false
  }
}

watch(page, () => {
  void loadTransactions()
})

watch(pageSize, () => {
  page.value = 1
  void loadTransactions()
})

watch(selectedType, () => {
  resetAndLoad()
})

onMounted(() => {
  void loadTransactions()
})
</script>

<template>
  <section class="transactions-page">
    <div class="transactions-hero">
      <div class="transactions-hero__title-area">
        <h1>Biến động kho</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-history-line" class="ml-2">
          Quản lý tồn kho
        </VChip>
      </div>

      <div class="transactions-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="resetAndLoad">
          Tải lại
        </VBtn>

        <VBtn color="success" prepend-icon="ri-file-excel-line" class="export-action" :loading="loading"
          @click="handleExport">
          Xuất CSV
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

    <VCard class="transactions-filter-card">
      <VCardText>
        <div class="transactions-toolbar">
          <div class="product-search-wrap">
            <VTextField v-model="searchProductQuery" label="Lọc theo sản phẩm"
              placeholder="Nhập tên sản phẩm, SKU hoặc barcode..." prepend-inner-icon="ri-search-line" clearable
              density="comfortable" hide-details @click:clear="clearProductFilter" @keyup.enter="searchProducts" />

            <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="searchLoading"
              @click="searchProducts">
              Tìm
            </VBtn>

            <div v-if="searchedProducts.length" class="product-result-list">
              <button v-for="product in searchedProducts" :key="product.id" type="button" class="product-result-item"
                @click="selectProductFilter(product)">
                <span>
                  <strong>{{ product.name }}</strong>
                  <small>{{ product.code }} · Tồn hiện tại: {{ formatNumber(product.quantityOnHand ?? 0) }} {{
                    product.unitName }}</small>
                </span>

                <VIcon icon="ri-arrow-right-s-line" />
              </button>
            </div>
          </div>

          <VSelect v-model="selectedType" label="Loại biến động" :items="transactionTypes" item-title="title"
            item-value="value" density="comfortable" hide-details class="type-filter" />

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line" @click="clearAllFilters">
            Xóa lọc
          </VBtn>
        </div>

        <div v-if="selectedProductId || selectedType" class="active-filter-strip">
          <VChip v-if="selectedProductId" color="primary" variant="tonal" closable @click:close="clearProductFilter">
            Sản phẩm: {{ selectedProductName || searchProductQuery }}
          </VChip>

          <VChip v-if="selectedType" :color="typeColor(selectedType)" variant="tonal" closable
            @click:close="selectedType = null">
            Loại: {{ typeLabel(selectedType) }}
          </VChip>
        </div>
      </VCardText>
    </VCard>

    <VCard class="transactions-panel">
      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="transactions.length" class="transactions-table-wrap">
          <VTable class="transactions-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Sản phẩm</th>
                <th>Mã sản phẩm</th>
                <th>Loại biến động</th>
                <th class="text-end">Thay đổi</th>
                <th class="text-end">Tồn cuối</th>
                <th>Mã tham chiếu</th>
                <th>Người thực hiện</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="transaction in transactions" :key="transaction.id" class="transaction-row">
                <td>
                  <div class="date-cell">
                    {{ formatDate(transaction.createdAt) }}
                  </div>
                </td>

                <td>
                  <div class="product-cell">
                    <strong>{{ transaction.productName }}</strong>
                    <span>{{ transaction.productCode }}</span>
                  </div>
                </td>

                <td>
                  <div class="sku-cell">
                    {{ transaction.productCode }}
                  </div>
                </td>

                <td>
                  <VChip :color="typeColor(transaction.transactionType)" size="small" variant="tonal">
                    <VIcon :icon="typeIcon(transaction.transactionType)" size="16" class="me-1" />
                    {{ typeLabel(transaction.transactionType) }}
                  </VChip>
                </td>

                <td class="text-end quantity-cell"
                  :class="transaction.changeQuantity > 0 ? 'quantity-cell--plus' : 'quantity-cell--minus'">
                  {{ formatQuantity(transaction.changeQuantity) }}
                </td>

                <td class="text-end balance-cell">
                  {{ formatNumber(transaction.balanceQuantity) }}
                </td>

                <td>
                  <span class="reference-cell">
                    {{ transaction.referenceCode || '—' }}
                  </span>
                </td>

                <td>
                  {{ transaction.createdByName || 'Hệ thống' }}
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="transactions-empty">
          <VIcon icon="ri-history-line" size="42" color="primary" />
          <strong>Chưa có dữ liệu biến động</strong>
          <span>Các thay đổi về kho như nhập, xuất, kiểm kê hoặc trả hàng sẽ hiển thị tại đây.</span>

          <VBtn color="primary" prepend-icon="ri-refresh-line" @click="resetAndLoad">
            Tải lại
          </VBtn>
        </div>
      </template>

      <div v-if="transactions.length || totalCount > 0" class="transactions-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
          trên tổng số {{ formatNumber(totalCount) }} biến động kho
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
.transactions-page {
  position: relative;
  isolation: isolate;
}

.transactions-page::before {
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

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--error {
  background: rgb(var(--v-theme-error));
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

.transactions-filter-card,
.transactions-panel {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: visible;
}

.transactions-filter-card {
  margin-block-end: 1rem;
}

.transactions-toolbar {
  display: grid;
  align-items: start;
  gap: 0.85rem;
  grid-template-columns: minmax(320px, 1fr) minmax(190px, 260px) auto;
}

.transactions-toolbar :deep(.v-field) {
  border-radius: 16px;
}

.transactions-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.product-search-wrap {
  position: relative;
  display: grid;
  gap: 0.65rem;
  grid-template-columns: minmax(0, 1fr) auto;
}

.product-result-list {
  position: absolute;
  inset-block-start: calc(100% + 0.45rem);
  inset-inline: 0;
  z-index: 20;
  display: grid;
  gap: 0.55rem;
  max-block-size: 240px;
  overflow-y: auto;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.65rem;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 20px 46px rgba(15, 23, 42, 0.16);
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

.active-filter-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
  margin-block-start: 0.85rem;
}

.transactions-table-wrap {
  overflow-x: auto;
}

.transactions-table {
  min-inline-size: 1020px;
}

.transactions-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.transactions-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.transaction-row {
  transition: background 160ms ease;
}

.transaction-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.date-cell {
  color: rgba(var(--v-theme-on-surface), 0.68);
  font-weight: 700;
  white-space: nowrap;
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

.sku-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.quantity-cell {
  font-weight: 950;
}

.quantity-cell--plus {
  color: rgb(var(--v-theme-success));
}

.quantity-cell--minus {
  color: rgb(var(--v-theme-error));
}

.balance-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 950;
}

.reference-cell {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-weight: 750;
}

.transactions-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.transactions-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.transactions-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.transactions-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.transactions-pagination>div {
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

  .transactions-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .product-search-wrap {
    grid-column: 1 / -1;
  }

  .transactions-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .transactions-toolbar,
  .product-search-wrap {
    grid-template-columns: 1fr;
  }

  .transactions-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .transactions-pagination>div {
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
.transactions-hero {
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

.transactions-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.transactions-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.transactions-hero__copy {
  display: none !important;
}

.transactions-hero__actions,
.transactions-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.transactions-hero__actions .v-btn,
.transactions-actions .v-btn,
.transactions-hero__actions .v-btn.primary-action,
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