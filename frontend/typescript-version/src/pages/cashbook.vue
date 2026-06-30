<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  type CashBookBalanceDto,
  type CashTransactionDto,
  createCashTransaction,
  getCashBookBalance,
  getCashTransactions,
} from '@/services/orderSalesApi'

const transactions = ref<CashTransactionDto[]>([])

const balance = ref<CashBookBalanceDto>({
  totalReceipts: 0,
  totalPayments: 0,
  currentBalance: 0,
})

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const search = ref('')
const selectedType = ref<string | null>(null)
const selectedCategory = ref<string | null>(null)

const txDialog = ref(false)
const txFormType = ref<'Receipt' | 'Payment'>('Receipt')

const txForm = ref({
  amount: null as number | null,
  sourceOrRecipient: '',
  category: '',
  note: '',
})

const formLoading = ref(false)
const formError = ref('')

const categoryOptions = [
  'Bán hàng',
  'Chi phí vận hành',
  'Nhập hàng',
  'Lương nhân viên',
  'Khác',
]

const typeItems = [
  { title: 'Tất cả loại', value: null },
  { title: 'Thu quỹ', value: 'Receipt' },
  { title: 'Chi quỹ', value: 'Payment' },
]

const categoryItems = computed(() => [
  { title: 'Tất cả hạng mục', value: null },
  ...categoryOptions.map(category => ({
    title: category,
    value: category,
  })),
])

const moneyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const dateTimeFormatter = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const formatCurrency = (amount: number) => moneyFormatter.format(amount || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatTime = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? 'Không xác định'
    : dateTimeFormatter.format(date)
}

const typeLabel = (type: string) => {
  if (type === 'Receipt')
    return 'Thu quỹ'

  if (type === 'Payment')
    return 'Chi quỹ'

  return type
}

const typeColor = (type: string) => {
  if (type === 'Receipt')
    return 'success'

  if (type === 'Payment')
    return 'error'

  return 'secondary'
}

const typeIcon = (type: string) => {
  if (type === 'Receipt')
    return 'ri-arrow-down-circle-line'

  if (type === 'Payment')
    return 'ri-arrow-up-circle-line'

  return 'ri-wallet-3-line'
}

const rangeStart = computed(() => {
  if (!transactions.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalCount.value),
)

const receiptCount = computed(() =>
  transactions.value.filter(item => item.type === 'Receipt').length,
)

const paymentCount = computed(() =>
  transactions.value.filter(item => item.type === 'Payment').length,
)

const pageReceiptTotal = computed(() =>
  transactions.value
    .filter(item => item.type === 'Receipt')
    .reduce((sum, item) => sum + item.amount, 0),
)

const pagePaymentTotal = computed(() =>
  transactions.value
    .filter(item => item.type === 'Payment')
    .reduce((sum, item) => sum + item.amount, 0),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng thu',
    value: formatCurrency(balance.value.totalReceipts),
    helper: `${formatNumber(receiptCount.value)} phiếu thu trên trang này`,
    icon: 'ri-arrow-down-circle-line',
    color: 'success',
  },
  {
    label: 'Tổng chi',
    value: formatCurrency(balance.value.totalPayments),
    helper: `${formatNumber(paymentCount.value)} phiếu chi trên trang này`,
    icon: 'ri-arrow-up-circle-line',
    color: 'error',
  },
  {
    label: 'Tồn quỹ hiện tại',
    value: formatCurrency(balance.value.currentBalance),
    helper: 'Số dư quỹ tiền mặt khả dụng',
    icon: 'ri-money-dollar-circle-line',
    color: balance.value.currentBalance >= 0 ? 'primary' : 'warning',
  },
  {
    label: 'Thu chi trang này',
    value: formatCurrency(pageReceiptTotal.value - pagePaymentTotal.value),
    helper: `Thu ${formatCurrency(pageReceiptTotal.value)} · Chi ${formatCurrency(pagePaymentTotal.value)}`,
    icon: 'ri-scales-3-line',
    color: pageReceiptTotal.value - pagePaymentTotal.value >= 0 ? 'success' : 'error',
  },
])

async function loadBalance() {
  try {
    balance.value = await getCashBookBalance()
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải số dư sổ quỹ.'
  }
}

async function loadTransactions() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getCashTransactions({
      search: search.value.trim(),
      type: selectedType.value || undefined,
      category: selectedCategory.value || undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    transactions.value = response.items
    totalPages.value = response.totalPages
    totalCount.value = response.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách giao dịch sổ quỹ.'
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

const loadAll = async () => {
  loading.value = true

  try {
    await Promise.all([
      loadBalance(),
      loadTransactions(),
    ])
  }
  finally {
    loading.value = false
  }
}

const clearFilters = () => {
  search.value = ''
  selectedType.value = null
  selectedCategory.value = null
  resetAndLoad()
}

const openCreateDialog = (type: 'Receipt' | 'Payment') => {
  txFormType.value = type

  txForm.value = {
    amount: null,
    sourceOrRecipient: '',
    category: type === 'Receipt' ? 'Bán hàng' : 'Chi phí vận hành',
    note: '',
  }

  formError.value = ''
  txDialog.value = true
}

async function handleSaveTransaction() {
  if (txForm.value.amount === null || txForm.value.amount <= 0) {
    formError.value = 'Vui lòng nhập số tiền hợp lệ lớn hơn 0.'

    return
  }

  if (!txForm.value.sourceOrRecipient.trim()) {
    formError.value = txFormType.value === 'Receipt'
      ? 'Vui lòng nhập tên người nộp tiền.'
      : 'Vui lòng nhập tên người nhận tiền.'

    return
  }

  if (!txForm.value.category) {
    formError.value = 'Vui lòng chọn hạng mục thu chi.'

    return
  }

  formLoading.value = true
  formError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createCashTransaction({
      type: txFormType.value,
      amount: Number(txForm.value.amount),
      sourceOrRecipient: txForm.value.sourceOrRecipient.trim(),
      category: txForm.value.category,
      note: txForm.value.note.trim() || null,
    })

    txDialog.value = false
    successMessage.value = txFormType.value === 'Receipt'
      ? 'Lập phiếu thu quỹ thành công.'
      : 'Lập phiếu chi quỹ thành công.'

    await loadBalance()
    await loadTransactions()
  }
  catch (error: any) {
    formError.value = error.message || 'Lỗi khi lưu giao dịch quỹ.'
  }
  finally {
    formLoading.value = false
  }
}

watch(page, () => {
  void loadTransactions()
})

watch(pageSize, () => {
  page.value = 1
  void loadTransactions()
})

onMounted(() => {
  void loadAll()
})
</script>

<template>
  <section class="cashbook-page">
    <div class="cashbook-hero">
      <div class="cashbook-hero__title-area">
        <h1>Quản lý thu chi ngoài</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-wallet-3-line" class="ml-2">
          Sổ quỹ
        </VChip>
      </div>

      <div class="cashbook-hero__actions">
        <VBtn color="success" prepend-icon="ri-add-line" class="receipt-action" @click="openCreateDialog('Receipt')">
          Lập phiếu thu
        </VBtn>

        <VBtn color="error" prepend-icon="ri-subtract-line" class="payment-action" @click="openCreateDialog('Payment')">
          Lập phiếu chi
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

    <VCard class="cashbook-panel">
      <VCardText>
        <div class="cashbook-toolbar">
          <VTextField v-model="search" prepend-inner-icon="ri-search-2-line" label="Tìm giao dịch"
            placeholder="Mã giao dịch, người nộp/nhận, ghi chú..." density="comfortable" hide-details clearable
            @keyup.enter="resetAndLoad" @click:clear="search = ''; resetAndLoad()" />

          <VSelect v-model="selectedType" label="Loại giao dịch" :items="typeItems" item-title="title"
            item-value="value" density="comfortable" hide-details @update:model-value="resetAndLoad" />

          <VSelect v-model="selectedCategory" label="Hạng mục" :items="categoryItems" item-title="title"
            item-value="value" density="comfortable" hide-details @update:model-value="resetAndLoad" />

          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="resetAndLoad">
            Lọc
          </VBtn>

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line" @click="clearFilters">
            Xóa lọc
          </VBtn>

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading" @click="loadAll">
            Tải lại
          </VBtn>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="transactions.length" class="cashbook-table-wrap">
          <VTable class="cashbook-table">
            <thead>
              <tr>
                <th>Mã phiếu</th>
                <th>Thời gian</th>
                <th>Loại phiếu</th>
                <th>Hạng mục</th>
                <th>Người nộp/nhận</th>
                <th class="text-end">Số tiền</th>
                <th>Người lập</th>
                <th>Ghi chú</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="transaction in transactions" :key="transaction.id" class="cashbook-row">
                <td>
                  <div class="transaction-code">
                    {{ transaction.transactionCode }}
                  </div>
                </td>

                <td>
                  <div class="time-cell">
                    {{ formatTime(transaction.createdAt) }}
                  </div>
                </td>

                <td>
                  <VChip size="small" :color="typeColor(transaction.type)" variant="tonal">
                    <VIcon :icon="typeIcon(transaction.type)" size="16" class="me-1" />
                    {{ typeLabel(transaction.type) }}
                  </VChip>
                </td>

                <td>
                  <VChip size="small" color="secondary" variant="outlined">
                    {{ transaction.category }}
                  </VChip>
                </td>

                <td>
                  <div class="person-cell">
                    <strong>{{ transaction.sourceOrRecipient }}</strong>
                    <span>{{ transaction.type === 'Receipt' ? 'Người nộp' : 'Người nhận' }}</span>
                  </div>
                </td>

                <td class="text-end amount-cell"
                  :class="transaction.type === 'Receipt' ? 'amount-cell--receipt' : 'amount-cell--payment'">
                  {{ transaction.type === 'Receipt' ? '+' : '-' }}{{ formatCurrency(transaction.amount) }}
                </td>

                <td>
                  {{ transaction.createdByName }}
                </td>

                <td>
                  <div class="note-cell" :title="transaction.note || ''">
                    {{ transaction.note || '—' }}
                  </div>
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="cashbook-empty">
          <VIcon icon="ri-bank-card-line" size="42" color="primary" />
          <strong>Chưa có giao dịch thu chi</strong>
          <span>Lập phiếu thu hoặc phiếu chi ngoài để quản lý dòng tiền mặt.</span>

          <div class="empty-actions">
            <VBtn color="success" prepend-icon="ri-add-line" @click="openCreateDialog('Receipt')">
              Lập phiếu thu
            </VBtn>

            <VBtn color="error" prepend-icon="ri-subtract-line" @click="openCreateDialog('Payment')">
              Lập phiếu chi
            </VBtn>
          </div>
        </div>
      </template>

      <div v-if="transactions.length || totalCount > 0" class="cashbook-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
          trên tổng số {{ formatNumber(totalCount) }} giao dịch sổ quỹ
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="txDialog" max-width="560" persistent>
      <VCard class="cashbook-dialog">
        <div class="dialog-head">
          <div>
            <span>{{ txFormType === 'Receipt' ? 'Phiếu thu quỹ' : 'Phiếu chi quỹ' }}</span>
            <h2>{{ txFormType === 'Receipt' ? 'Lập phiếu thu' : 'Lập phiếu chi' }}</h2>
          </div>

          <VChip :color="txFormType === 'Receipt' ? 'success' : 'error'" variant="tonal">
            {{ txFormType === 'Receipt' ? 'Thu quỹ' : 'Chi quỹ' }}
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="formError" type="error" variant="tonal" class="mb-4">
            {{ formError }}
          </VAlert>

          <VRow>
            <VCol cols="12">
              <VTextField v-model.number="txForm.amount" label="Số tiền *" type="number" min="1" suffix="đ"
                prepend-inner-icon="ri-money-dollar-circle-line" density="comfortable" required />
            </VCol>

            <VCol cols="12">
              <VTextField v-model="txForm.sourceOrRecipient"
                :label="txFormType === 'Receipt' ? 'Người nộp tiền *' : 'Người nhận tiền *'"
                placeholder="Nhập họ và tên..." density="comfortable" required />
            </VCol>

            <VCol cols="12">
              <VSelect v-model="txForm.category" label="Hạng mục *" :items="categoryOptions" density="comfortable"
                required />
            </VCol>

            <VCol cols="12">
              <VTextarea v-model="txForm.note" label="Lý do / Ghi chú" placeholder="Mô tả chi tiết lý do thu/chi..."
                rows="3" density="comfortable" />
            </VCol>
          </VRow>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="formLoading" @click="txDialog = false">
            Hủy bỏ
          </VBtn>

          <VBtn :color="txFormType === 'Receipt' ? 'success' : 'error'" :loading="formLoading"
            @click="handleSaveTransaction">
            Xác nhận ghi quỹ
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.cashbook-page {
  position: relative;
  isolation: isolate;
}

.cashbook-page::before {
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

.receipt-action {
  box-shadow: 0 14px 34px rgba(var(--v-theme-success), 0.24);
}

.payment-action {
  box-shadow: 0 14px 34px rgba(var(--v-theme-error), 0.24);
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

.cashbook-panel,
.cashbook-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.cashbook-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(160px, 210px) minmax(160px, 220px) auto auto auto;
}

.cashbook-toolbar :deep(.v-field),
.cashbook-dialog :deep(.v-field) {
  border-radius: 16px;
}

.cashbook-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.cashbook-table-wrap {
  overflow-x: auto;
}

.cashbook-table {
  min-inline-size: 1060px;
}

.cashbook-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.cashbook-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.cashbook-row {
  transition: background 160ms ease;
}

.cashbook-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.transaction-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.time-cell {
  color: rgba(var(--v-theme-on-surface), 0.68);
  font-weight: 700;
  white-space: nowrap;
}

.person-cell strong,
.person-cell span {
  display: block;
}

.person-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.person-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.amount-cell {
  font-size: 1rem;
  font-weight: 950;
  white-space: nowrap;
}

.amount-cell--receipt {
  color: rgb(var(--v-theme-success));
}

.amount-cell--payment {
  color: rgb(var(--v-theme-error));
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

.cashbook-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.cashbook-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.empty-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-block-start: 0.5rem;
}

.cashbook-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.cashbook-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.cashbook-pagination>div {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.page-size-select {
  inline-size: 108px;
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

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1400px) {
  .cashbook-toolbar {
    grid-template-columns: 1fr 1fr 1fr;
  }

  .cashbook-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .cashbook-toolbar {
    grid-template-columns: 1fr 1fr;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .cashbook-toolbar {
    grid-template-columns: 1fr;
  }

  .cashbook-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .cashbook-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .dialog-head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.cashbook-hero {
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

.cashbook-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.cashbook-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.cashbook-hero__copy {
  display: none !important;
}

.cashbook-hero__actions,
.cashbook-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.cashbook-hero__actions .v-btn,
.cashbook-actions .v-btn,
.cashbook-hero__actions .v-btn.primary-action,
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