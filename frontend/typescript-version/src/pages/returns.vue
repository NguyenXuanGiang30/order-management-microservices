<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  getReturnOrders,
  type ReturnOrderDto,
} from '@/services/orderSalesApi'

const returns = ref<ReturnOrderDto[]>([])
const search = ref('')
const loading = ref(false)
const errorMessage = ref('')

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

const detailDialog = ref(false)
const selectedReturn = ref<ReturnOrderDto | null>(null)

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

const statusLabelMap: Record<string, string> = {
  Pending: 'Đang xử lý',
  Completed: 'Đã hoàn thành',
  Rejected: 'Đã từ chối',
}

const hasReturns = computed(() => returns.value.length > 0)

const formatCurrency = (amount: number) => moneyFormatter.format(amount)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatTime = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? 'Không xác định'
    : dateTimeFormatter.format(date)
}

const statusLabel = (status: string) => statusLabelMap[status] ?? status

const statusColor = (status: string) => {
  if (status === 'Completed')
    return 'success'

  if (status === 'Pending')
    return 'warning'

  if (status === 'Rejected')
    return 'error'

  return 'secondary'
}

const pageRefundTotal = computed(() =>
  returns.value.reduce((sum, item) => sum + item.totalRefundAmount, 0),
)

const completedCount = computed(() =>
  returns.value.filter(item => item.status === 'Completed').length,
)

const pendingCount = computed(() =>
  returns.value.filter(item => item.status === 'Pending').length,
)

const rejectedCount = computed(() =>
  returns.value.filter(item => item.status === 'Rejected').length,
)

const summaryCards = computed(() => [
  {
    label: 'Tổng phiếu trả',
    value: formatNumber(totalCount.value),
    helper: `Trang hiện tại: ${formatNumber(returns.value.length)} phiếu`,
    icon: 'ri-arrow-go-back-line',
    color: 'primary',
  },
  {
    label: 'Tiền hoàn trang này',
    value: formatCurrency(pageRefundTotal.value),
    helper: 'Tổng tiền hoàn theo danh sách đang xem',
    icon: 'ri-refund-2-line',
    color: 'error',
  },
  {
    label: 'Đã hoàn thành',
    value: formatNumber(completedCount.value),
    helper: `${formatNumber(pendingCount.value)} phiếu đang xử lý`,
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Đã từ chối',
    value: formatNumber(rejectedCount.value),
    helper: 'Phiếu không đủ điều kiện hoàn trả',
    icon: 'ri-close-circle-line',
    color: 'warning',
  },
])

async function loadReturns() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getReturnOrders({
      search: search.value.trim(),
      page: page.value,
      pageSize: pageSize.value,
    })

    returns.value = response.items
    totalPages.value = response.totalPages
    totalCount.value = response.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách phiếu trả hàng.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadReturns()
  else
    page.value = 1
}

watch(page, () => {
  void loadReturns()
})

const openReturnDetail = (ret: ReturnOrderDto) => {
  selectedReturn.value = ret
  detailDialog.value = true
}

onMounted(() => {
  void loadReturns()
})
</script>

<template>
  <section class="returns-page">
    <div class="returns-hero">
      <div class="returns-hero__title-area">
        <h1>Quản lý trả hàng</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-arrow-go-back-line" class="ml-2">
          Trả hàng
        </VChip>
      </div>

      <div class="returns-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="resetAndLoad">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-receipt-line" to="/orders" class="primary-action">
          Tạo từ đơn hàng
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

    <VCard class="returns-panel">
      <VCardText>
        <div class="returns-toolbar">
          <VTextField v-model="search" label="Tìm phiếu trả hàng" placeholder="Mã phiếu trả, mã đơn gốc, khách hàng..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable class="returns-search"
            @keyup.enter="resetAndLoad" />

          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="resetAndLoad">
            Tìm
          </VBtn>

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
            @click="resetAndLoad">
            Tải lại
          </VBtn>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="hasReturns" class="returns-table-wrap">
          <VTable class="returns-table">
            <thead>
              <tr>
                <th>Mã phiếu trả</th>
                <th>Mã đơn gốc</th>
                <th>Khách hàng</th>
                <th>Thời gian</th>
                <th class="text-end">Tiền hoàn</th>
                <th>Lý do</th>
                <th>Trạng thái</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="ret in returns" :key="ret.id" class="return-row" @click="openReturnDetail(ret)">
                <td>
                  <div class="return-code">
                    <VIcon icon="ri-arrow-go-back-line" />
                    <strong>{{ ret.returnCode }}</strong>
                  </div>
                </td>

                <td>
                  <div class="order-code">
                    {{ ret.orderCode }}
                  </div>
                </td>

                <td>
                  <div class="cell-main">
                    <strong>{{ ret.customerName }}</strong>
                    <span>Khách hàng</span>
                  </div>
                </td>

                <td>
                  {{ formatTime(ret.createdAt) }}
                </td>

                <td class="text-end refund-cell">
                  {{ formatCurrency(ret.totalRefundAmount) }}
                </td>

                <td>
                  <div class="reason-cell">
                    {{ ret.returnReason || '—' }}
                  </div>
                </td>

                <td>
                  <VChip :color="statusColor(ret.status)" variant="tonal" size="small">
                    {{ statusLabel(ret.status) }}
                  </VChip>
                </td>

                <td class="text-center" @click.stop>
                  <VBtn size="small" color="primary" variant="tonal" prepend-icon="ri-eye-line"
                    @click="openReturnDetail(ret)">
                    Chi tiết
                  </VBtn>
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="returns-empty">
          <VIcon icon="ri-arrow-go-back-line" size="42" color="primary" />
          <strong>Chưa có phiếu trả hàng</strong>
          <span>Các phiếu trả hàng được tạo từ trang Quản lý đơn hàng khi khách hoàn sản phẩm.</span>

          <VBtn color="primary" prepend-icon="ri-receipt-line" to="/orders">
            Đi tới đơn hàng
          </VBtn>
        </div>
      </template>

      <div v-if="returns.length || totalCount > 0" class="returns-pagination">
        <span>
          Hiển thị {{ formatNumber(returns.length) }} trên tổng số {{ formatNumber(totalCount) }} phiếu trả hàng
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" @update:model-value="resetAndLoad" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="detailDialog" max-width="880" scrollable>
      <VCard v-if="selectedReturn" class="return-dialog">
        <div class="dialog-head">
          <div>
            <span>Chi tiết phiếu trả</span>
            <h2>{{ selectedReturn.returnCode }}</h2>
          </div>

          <VChip :color="statusColor(selectedReturn.status)" variant="tonal">
            {{ statusLabel(selectedReturn.status) }}
          </VChip>
        </div>

        <VCardText>
          <div class="detail-grid">
            <div class="detail-card">
              <span>Mã đơn gốc</span>
              <strong>{{ selectedReturn.orderCode }}</strong>
              <p>Đơn hàng phát sinh trả hàng</p>
            </div>

            <div class="detail-card">
              <span>Khách hàng</span>
              <strong>{{ selectedReturn.customerName }}</strong>
              <p>{{ formatTime(selectedReturn.createdAt) }}</p>
            </div>

            <div class="detail-card">
              <span>Tổng hoàn tiền</span>
              <strong class="text-error">
                {{ formatCurrency(selectedReturn.totalRefundAmount) }}
              </strong>
              <p>{{ selectedReturn.returnOrderDetails.length }} sản phẩm nhận lại</p>
            </div>
          </div>

          <div class="reason-box">
            <span>Lý do trả hàng</span>
            <p>{{ selectedReturn.returnReason || 'Không có ghi chú lý do trả hàng.' }}</p>
          </div>

          <div class="dialog-section-title">
            Danh sách sản phẩm nhận lại
          </div>

          <div class="dialog-table-wrap">
            <VTable class="returns-table compact-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th>ĐVT</th>
                  <th class="text-end">Đơn giá</th>
                  <th class="text-center">SL nhận trả</th>
                  <th class="text-end">Tiền hoàn</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in selectedReturn.returnOrderDetails" :key="item.id">
                  <td class="font-weight-bold">{{ item.productName }}</td>
                  <td>{{ item.productCode }}</td>
                  <td>{{ item.unitName }}</td>
                  <td class="text-end">{{ formatCurrency(item.unitPrice) }}</td>
                  <td class="text-center quantity-cell">{{ item.returnQuantity }}</td>
                  <td class="text-end refund-cell">{{ formatCurrency(item.refundAmount) }}</td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div class="refund-summary">
            <div>
              <span>Tổng hoàn tiền</span>
              <strong>{{ formatCurrency(selectedReturn.totalRefundAmount) }}</strong>
            </div>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="outlined" @click="detailDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.returns-page {
  position: relative;
  isolation: isolate;
}

.returns-page::before {
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

.returns-panel,
.return-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.returns-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) auto auto;
}

.returns-toolbar :deep(.v-field) {
  border-radius: 16px;
}

.returns-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.returns-table-wrap,
.dialog-table-wrap {
  overflow-x: auto;
}

.returns-table {
  min-inline-size: 920px;
}

.returns-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.returns-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.return-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.return-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.return-code {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: rgb(var(--v-theme-primary));
}

.order-code {
  color: rgba(var(--v-theme-on-surface), 0.75);
  font-weight: 800;
}

.cell-main strong,
.cell-main span {
  display: block;
}

.cell-main strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 800;
}

.cell-main span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.refund-cell {
  color: rgb(var(--v-theme-error));
  font-weight: 900;
}

.reason-cell {
  display: -webkit-box;
  max-inline-size: 240px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.68);
  font-size: 0.88rem;
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.returns-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.returns-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.returns-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.returns-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.returns-pagination>div {
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

.reason-box {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 1rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-background), 0.42);
}

.reason-box span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.reason-box p {
  margin: 0.45rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.76);
  line-height: 1.55;
}

.dialog-section-title {
  margin-block-end: 0.7rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 950;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.compact-table {
  min-inline-size: 780px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 54px !important;
}

.quantity-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.refund-summary {
  display: grid;
  gap: 0.55rem;
  max-inline-size: 380px;
  margin-inline-start: auto;
  margin-block-start: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.5);
}

.refund-summary>div {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
}

.refund-summary span {
  color: rgba(var(--v-theme-on-surface), 0.62);
  font-weight: 800;
}

.refund-summary strong {
  color: rgb(var(--v-theme-error));
  font-size: 1.25rem;
  font-weight: 950;
  letter-spacing: -0.035em;
}

.dialog-actions {
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .returns-toolbar {
    grid-template-columns: 1fr;
  }

  .returns-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .returns-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .returns-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .refund-summary {
    max-inline-size: none;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.returns-hero {
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

.returns-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.returns-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.returns-hero__copy {
  display: none !important;
}

.returns-hero__actions,
.returns-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.returns-hero__actions .v-btn,
.returns-actions .v-btn,
.returns-hero__actions .v-btn.primary-action,
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