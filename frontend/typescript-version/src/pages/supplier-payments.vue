<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'

import {
  type SupplierDto,
  type SupplierPaymentDto,
  createSupplierPayment,
  getSupplierPayments,
  getSuppliers,
} from '@/services/orderSalesApi'

const route = useRoute()

const payments = ref<SupplierPaymentDto[]>([])
const suppliers = ref<SupplierDto[]>([])

const loading = ref(false)
const loadingSuppliers = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(1)
const totalCount = ref(0)

const search = ref('')
const selectedSupplierId = ref<string | null>(null)

const paymentDialog = ref(false)

const paymentForm = ref({
  supplierId: null as string | null,
  amount: null as number | null,
  paymentMethod: 'Tiền mặt',
  note: '',
})

const formLoading = ref(false)
const formError = ref('')

const paymentMethods = ['Tiền mặt', 'Chuyển khoản']

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

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

const supplierItems = computed(() => [
  { name: 'Tất cả nhà cung cấp', id: null },
  ...suppliers.value,
])

const selectedSupplier = computed(() =>
  suppliers.value.find(supplier => supplier.id === paymentForm.value.supplierId) || null,
)

const selectedSupplierDebt = computed(() =>
  selectedSupplier.value?.debtAmount || 0,
)

const remainingDebtAfterPayment = computed(() => {
  const amount = Number(paymentForm.value.amount) || 0

  return Math.max(0, selectedSupplierDebt.value - amount)
})

const totalPaymentsAmount = computed(() =>
  payments.value.reduce((sum, payment) => sum + (payment.amount || 0), 0),
)

const totalSupplierDebt = computed(() =>
  suppliers.value.reduce((sum, supplier) => sum + (supplier.debtAmount || 0), 0),
)

const suppliersWithDebt = computed(() =>
  suppliers.value.filter(supplier => (supplier.debtAmount || 0) > 0).length,
)

const rangeStart = computed(() => {
  if (!payments.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalCount.value),
)

const summaryCards = computed(() => [
  {
    label: 'Đã thanh toán',
    value: formatCurrency(totalPaymentsAmount.value),
    helper: 'Tổng tiền chi trả trên trang hiện tại',
    icon: 'ri-money-dollar-circle-line',
    color: 'success',
  },
  {
    label: 'Số phiếu chi',
    value: formatNumber(totalCount.value),
    helper: 'Tổng giao dịch thanh toán công nợ NCC',
    icon: 'ri-file-list-3-line',
    color: 'info',
  },
  {
    label: 'Nhà cung cấp còn nợ',
    value: formatNumber(suppliersWithDebt.value),
    helper: 'Cần theo dõi thanh toán tiếp',
    icon: 'ri-truck-line',
    color: 'warning',
  },
  {
    label: 'Tổng phải trả NCC',
    value: formatCurrency(totalSupplierDebt.value),
    helper: 'Tổng công nợ từ danh sách nhà cung cấp',
    icon: 'ri-bank-card-line',
    color: 'error',
  },
])

const paymentMethodColor = (method: string) => {
  if (method === 'Tiền mặt')
    return 'info'

  if (method === 'Chuyển khoản')
    return 'primary'

  return 'secondary'
}

async function loadSuppliers() {
  loadingSuppliers.value = true

  try {
    const result = await getSuppliers({ pageSize: 100 })

    suppliers.value = result.items
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách nhà cung cấp.')
  }
  finally {
    loadingSuppliers.value = false
  }
}

async function loadPayments() {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getSupplierPayments({
      search: search.value.trim() || undefined,
      supplierId: selectedSupplierId.value || undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    payments.value = result.items
    totalPages.value = Math.max(1, result.totalPages)
    totalCount.value = result.totalCount
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải lịch sử thanh toán công nợ.')
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadPayments()
  else
    page.value = 1
}

const clearFilters = () => {
  search.value = ''
  selectedSupplierId.value = null
  resetAndLoad()
}

const openCreateDialog = (preselectedSupplierId?: string) => {
  paymentForm.value = {
    supplierId: preselectedSupplierId || selectedSupplierId.value || null,
    amount: null,
    paymentMethod: 'Tiền mặt',
    note: '',
  }

  formError.value = ''
  paymentDialog.value = true
}

const validatePaymentForm = () => {
  if (!paymentForm.value.supplierId)
    return 'Vui lòng chọn nhà cung cấp.'

  if (paymentForm.value.amount === null || paymentForm.value.amount <= 0)
    return 'Vui lòng nhập số tiền chi trả hợp lệ lớn hơn 0.'

  if (!paymentForm.value.paymentMethod)
    return 'Vui lòng chọn phương thức thanh toán.'

  return ''
}

async function handleSavePayment() {
  const validationMessage = validatePaymentForm()

  if (validationMessage) {
    formError.value = validationMessage

    return
  }

  formLoading.value = true
  formError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createSupplierPayment({
      supplierId: paymentForm.value.supplierId!,
      amount: Number(paymentForm.value.amount),
      paymentMethod: paymentForm.value.paymentMethod,
      note: paymentForm.value.note.trim() || null,
    })

    paymentDialog.value = false
    successMessage.value = 'Ghi nhận phiếu chi trả công nợ nhà cung cấp thành công.'

    await Promise.all([
      loadPayments(),
      loadSuppliers(),
    ])
  }
  catch (error) {
    formError.value = getErrorMessage(error, 'Lỗi khi ghi nhận thanh toán công nợ.')
  }
  finally {
    formLoading.value = false
  }
}

const reloadAll = async () => {
  await Promise.all([
    loadSuppliers(),
    loadPayments(),
  ])
}

watch(page, () => {
  void loadPayments()
})

watch(pageSize, () => {
  page.value = 1
  void loadPayments()
})

onMounted(async () => {
  await reloadAll()

  const querySupplierId = route.query.supplierId

  if (typeof querySupplierId === 'string' && querySupplierId)
    openCreateDialog(querySupplierId)
})
</script>

<template>
  <section class="supplier-payment-page">
    <div class="payment-hero">
      <div class="payment-hero__title-area">
        <h1>Thanh toán công nợ nhà cung cấp</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-secure-payment-line" class="ml-2">
          Quản lý chi trả
        </VChip>
      </div>

      <div class="payment-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading || loadingSuppliers"
          @click="reloadAll">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-add-line" class="primary-action" @click="openCreateDialog()">
          Lập phiếu chi trả NCC
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

    <VCard class="payment-panel">
      <VCardText>
        <div class="payment-toolbar">
          <VTextField v-model="search" prepend-inner-icon="ri-search-2-line" label="Tìm phiếu chi"
            placeholder="Mã phiếu thanh toán, nhà cung cấp, ghi chú..." density="comfortable" hide-details clearable
            @keyup.enter="resetAndLoad" @click:clear="search = ''; resetAndLoad()" />

          <VSelect v-model="selectedSupplierId" label="Nhà cung cấp" :items="supplierItems" item-title="name"
            item-value="id" density="comfortable" hide-details :loading="loadingSuppliers"
            @update:model-value="resetAndLoad" />

          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="resetAndLoad">
            Lọc
          </VBtn>

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line" @click="clearFilters">
            Xóa lọc
          </VBtn>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="payments.length" class="payment-table-wrap">
          <VTable class="payment-table">
            <thead>
              <tr>
                <th>Mã phiếu chi</th>
                <th>Thời gian</th>
                <th>Nhà cung cấp</th>
                <th>Mã NCC</th>
                <th>Hình thức</th>
                <th class="text-end">Số tiền chi trả</th>
                <th>Người lập</th>
                <th>Ghi chú</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="payment in payments" :key="payment.id" class="payment-row">
                <td>
                  <div class="payment-code">
                    {{ payment.paymentCode }}
                  </div>
                </td>

                <td>
                  <div class="date-cell">
                    {{ formatDate(payment.paymentDate) }}
                  </div>
                </td>

                <td>
                  <div class="supplier-cell">
                    <VAvatar size="34" color="primary" variant="tonal">
                      <VIcon icon="ri-truck-line" />
                    </VAvatar>

                    <strong>{{ payment.supplierName }}</strong>
                  </div>
                </td>

                <td>
                  <VChip size="small" variant="outlined">
                    {{ payment.supplierCode }}
                  </VChip>
                </td>

                <td>
                  <VChip size="small" :color="paymentMethodColor(payment.paymentMethod)" variant="tonal">
                    {{ payment.paymentMethod }}
                  </VChip>
                </td>

                <td class="text-end amount-cell">
                  -{{ formatCurrency(payment.amount) }}
                </td>

                <td>{{ payment.createdByName }}</td>

                <td>
                  <div class="note-cell" :title="payment.note || ''">
                    {{ payment.note || '—' }}
                  </div>
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="payment-empty">
          <VIcon icon="ri-secure-payment-line" size="42" color="primary" />
          <strong>Chưa có phiếu chi trả công nợ</strong>
          <span>Lập phiếu thanh toán để giảm công nợ cho nhà cung cấp.</span>

          <VBtn color="primary" prepend-icon="ri-add-line" @click="openCreateDialog()">
            Lập phiếu chi trả NCC
          </VBtn>
        </div>
      </template>

      <div v-if="payments.length || totalCount > 0" class="payment-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
          trên tổng số {{ formatNumber(totalCount) }} giao dịch chi trả
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="paymentDialog" max-width="620" persistent>
      <VCard class="payment-dialog">
        <div class="dialog-head">
          <div>
            <span>Phiếu chi trả</span>
            <h2>Lập phiếu chi trả NCC</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ paymentForm.paymentMethod }}
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="formError" type="error" variant="tonal" class="mb-4">
            {{ formError }}
          </VAlert>

          <VSelect v-model="paymentForm.supplierId" label="Chọn nhà cung cấp *" :items="suppliers" item-title="name"
            item-value="id" density="comfortable" :loading="loadingSuppliers" required class="mb-4" />

          <div v-if="paymentForm.supplierId" class="debt-preview">
            <div>
              <span>Công nợ hiện tại</span>
              <strong class="text-error">
                {{ formatCurrency(selectedSupplierDebt) }}
              </strong>
            </div>

            <div>
              <span>Sau thanh toán</span>
              <strong class="text-primary">
                {{ formatCurrency(remainingDebtAfterPayment) }}
              </strong>
            </div>
          </div>

          <VTextField v-model.number="paymentForm.amount" label="Số tiền chi trả *" type="number" min="1" suffix="đ"
            prepend-inner-icon="ri-money-dollar-circle-line" density="comfortable" required class="mt-4 mb-4" />

          <VSelect v-model="paymentForm.paymentMethod" label="Phương thức thanh toán *" :items="paymentMethods"
            density="comfortable" required class="mb-4" />

          <VTextarea v-model="paymentForm.note" label="Ghi chú / Lý do chi trả"
            placeholder="Ghi chú thêm thông tin như mã hóa đơn, ngày nhập hàng..." rows="3" density="comfortable" />
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="formLoading" @click="paymentDialog = false">
            Hủy bỏ
          </VBtn>

          <VBtn color="primary" :loading="formLoading" @click="handleSavePayment">
            Ghi nhận phiếu chi
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.supplier-payment-page {
  position: relative;
  isolation: isolate;
}

.supplier-payment-page::before {
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
.payment-panel,
.payment-dialog {
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
  font-size: 1.18rem;
  font-weight: 900;
  letter-spacing: -0.035em;
  margin-block: 0.2rem;
}

.summary-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
}

.payment-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(220px, 320px) auto auto;
}

.payment-toolbar :deep(.v-field),
.payment-dialog :deep(.v-field) {
  border-radius: 16px;
}

.payment-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.payment-table-wrap {
  overflow-x: auto;
}

.payment-table {
  min-inline-size: 1080px;
}

.payment-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.payment-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.payment-row {
  transition: background 160ms ease;
}

.payment-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.payment-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.date-cell {
  color: rgba(var(--v-theme-on-surface), 0.66);
  font-weight: 700;
  white-space: nowrap;
}

.supplier-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
}

.supplier-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.amount-cell {
  color: rgb(var(--v-theme-success));
  font-size: 1rem;
  font-weight: 950;
  white-space: nowrap;
}

.note-cell {
  display: -webkit-box;
  max-inline-size: 250px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.62);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.payment-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.payment-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.payment-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.payment-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.payment-pagination>div {
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

.debt-preview {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.debt-preview>div {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.debt-preview span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.debt-preview strong {
  display: block;
  margin-block-start: 0.3rem;
  font-size: 1.05rem;
  font-weight: 950;
  letter-spacing: -0.03em;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .payment-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .payment-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .payment-toolbar,
  .debt-preview {
    grid-template-columns: 1fr;
  }

  .payment-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .payment-pagination>div {
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
.payment-hero {
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

.payment-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.payment-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.payment-hero__copy {
  display: none !important;
}

.payment-hero__actions,
.payment-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.payment-hero__actions .v-btn,
.payment-actions .v-btn,
.payment-hero__actions .v-btn.primary-action,
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