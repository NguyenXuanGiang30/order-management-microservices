<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  type CashShiftDto,
  closeShift,
  getCurrentShift,
  getShifts,
  openShift,
} from '@/services/orderSalesApi'

const currentActiveShift = ref<CashShiftDto | null>(null)
const shiftsList = ref<CashShiftDto[]>([])

const loadingActive = ref(false)
const loadingHistory = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const searchCashier = ref('')
const currentPage = ref(1)
const itemsPerPage = ref(15)
const totalItems = ref(0)
const totalPages = ref(1)

const openShiftDialog = ref(false)
const closeShiftDialog = ref(false)

const openOpeningCash = ref<number | null>(null)
const openNote = ref('')
const openLoading = ref(false)
const openError = ref('')

const closeActualCash = ref<number | null>(null)
const closeNote = ref('')
const closeLoading = ref(false)
const closeError = ref('')

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

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatCurrency = (amount: number) => moneyFormatter.format(amount || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatTime = (value?: string | null) => {
  if (!value)
    return '—'

  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? 'Không xác định'
    : dateTimeFormatter.format(date)
}

const statusLabel = (status: string) => {
  const map: Record<string, string> = {
    Open: 'Đang mở',
    Closed: 'Đã đóng',
  }

  return map[status] ?? status
}

const statusColor = (status: string) => {
  const map: Record<string, string> = {
    Open: 'success',
    Closed: 'secondary',
  }

  return map[status] ?? 'primary'
}

const varianceColor = (value: number) => {
  if (value < 0)
    return 'error'

  if (value > 0)
    return 'success'

  return 'secondary'
}

const varianceClass = (value: number) => {
  if (value < 0)
    return 'text-error'

  if (value > 0)
    return 'text-success'

  return 'text-medium-emphasis'
}

const shiftIsOpen = computed(() => currentActiveShift.value?.status === 'Open')

const activeExpectedCash = computed(() =>
  currentActiveShift.value?.expectedCash ?? 0,
)

const closeVariance = computed(() => {
  if (closeActualCash.value === null)
    return 0

  return closeActualCash.value - activeExpectedCash.value
})

const rangeStart = computed(() => {
  if (!shiftsList.value.length)
    return 0

  return (currentPage.value - 1) * itemsPerPage.value + 1
})

const rangeEnd = computed(() =>
  Math.min(currentPage.value * itemsPerPage.value, totalItems.value),
)

const closedShiftCount = computed(() =>
  shiftsList.value.filter(shift => shift.status === 'Closed').length,
)

const openShiftCount = computed(() =>
  shiftsList.value.filter(shift => shift.status === 'Open').length,
)

const totalExpectedCashOnPage = computed(() =>
  shiftsList.value.reduce((sum, shift) => sum + (shift.expectedCash || 0), 0),
)

const totalVarianceOnPage = computed(() =>
  shiftsList.value
    .filter(shift => shift.closedAt)
    .reduce((sum, shift) => sum + (shift.variance || 0), 0),
)

const summaryCards = computed(() => [
  {
    label: 'Trạng thái ca',
    value: shiftIsOpen.value ? 'Đang mở' : 'Chưa mở',
    helper: currentActiveShift.value?.shiftCode || 'Mở ca để bắt đầu bán hàng',
    icon: 'ri-store-2-line',
    color: shiftIsOpen.value ? 'success' : 'secondary',
  },
  {
    label: 'Két tiền dự kiến',
    value: formatCurrency(activeExpectedCash.value),
    helper: 'Tiền đầu ca cộng giao dịch tiền mặt',
    icon: 'ri-money-dollar-circle-line',
    color: 'primary',
  },
  {
    label: 'Lịch sử ca',
    value: formatNumber(totalItems.value),
    helper: `${formatNumber(closedShiftCount.value)} ca đã đóng trên trang này`,
    icon: 'ri-history-line',
    color: 'warning',
  },
  {
    label: 'Chênh lệch trang này',
    value: formatCurrency(totalVarianceOnPage.value),
    helper: `Tổng két dự kiến: ${formatCurrency(totalExpectedCashOnPage.value)}`,
    icon: 'ri-scales-3-line',
    color: varianceColor(totalVarianceOnPage.value),
  },
])

const resetOpenForm = () => {
  openOpeningCash.value = null
  openNote.value = ''
  openError.value = ''
}

const resetCloseForm = () => {
  closeActualCash.value = currentActiveShift.value?.expectedCash ?? null
  closeNote.value = ''
  closeError.value = ''
}

const openOpenShiftDialog = () => {
  resetOpenForm()
  openShiftDialog.value = true
}

const openCloseShiftDialog = () => {
  resetCloseForm()
  closeShiftDialog.value = true
}

async function loadActiveShift() {
  loadingActive.value = true

  try {
    currentActiveShift.value = await getCurrentShift()
  }
  catch {
    currentActiveShift.value = null
  }
  finally {
    loadingActive.value = false
  }
}

async function loadShiftsHistory() {
  loadingHistory.value = true
  errorMessage.value = ''

  try {
    const response = await getShifts({
      search: searchCashier.value.trim() || undefined,
      page: currentPage.value,
      pageSize: itemsPerPage.value,
    })

    shiftsList.value = response.items
    totalItems.value = response.totalCount
    totalPages.value = Math.max(1, response.totalPages)
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải lịch sử ca làm việc.')
  }
  finally {
    loadingHistory.value = false
  }
}

const refreshAll = async () => {
  await Promise.all([
    loadActiveShift(),
    loadShiftsHistory(),
  ])
}

const resetAndLoadHistory = () => {
  if (currentPage.value === 1)
    void loadShiftsHistory()
  else
    currentPage.value = 1
}

const clearSearch = () => {
  searchCashier.value = ''
  resetAndLoadHistory()
}

async function handleOpenShift() {
  if (openOpeningCash.value === null || openOpeningCash.value < 0) {
    openError.value = 'Vui lòng nhập tiền bàn giao hợp lệ lớn hơn hoặc bằng 0.'

    return
  }

  openLoading.value = true
  openError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const result = await openShift({
      openingCash: Number(openOpeningCash.value),
      note: openNote.value.trim() || null,
    })

    currentActiveShift.value = result
    openShiftDialog.value = false
    resetOpenForm()

    successMessage.value = 'Mở ca bán hàng thành công.'

    await loadShiftsHistory()
  }
  catch (error) {
    openError.value = getErrorMessage(error, 'Không thể mở ca bán hàng.')
  }
  finally {
    openLoading.value = false
  }
}

async function handleCloseShift() {
  if (!currentActiveShift.value)
    return

  if (closeActualCash.value === null || closeActualCash.value < 0) {
    closeError.value = 'Vui lòng nhập tiền mặt kiểm kê thực tế hợp lệ.'

    return
  }

  closeLoading.value = true
  closeError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await closeShift(currentActiveShift.value.id, {
      actualCash: Number(closeActualCash.value),
      note: closeNote.value.trim() || null,
    })

    currentActiveShift.value = null
    closeShiftDialog.value = false
    resetCloseForm()

    successMessage.value = 'Đóng ca và bàn giao két thành công.'

    await loadShiftsHistory()
  }
  catch (error) {
    closeError.value = getErrorMessage(error, 'Không thể đóng ca bán hàng.')
  }
  finally {
    closeLoading.value = false
  }
}

watch(currentPage, () => {
  void loadShiftsHistory()
})

watch(itemsPerPage, () => {
  currentPage.value = 1
  void loadShiftsHistory()
})

onMounted(() => {
  void refreshAll()
})
</script>

<template>
  <section class="cash-shift-page">
    <div class="shift-hero">
      <div class="shift-hero__title-area">
        <h1>Quản lý ca làm việc</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-store-2-line" class="ml-2">
          Ca bán hàng
        </VChip>
      </div>

      <div class="shift-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line"
          :loading="loadingActive || loadingHistory" @click="refreshAll">
          Tải lại
        </VBtn>

        <VBtn v-if="!currentActiveShift" color="primary" prepend-icon="ri-key-line" class="primary-action"
          @click="openOpenShiftDialog">
          Mở ca bán hàng
        </VBtn>

        <VBtn v-else color="warning" prepend-icon="ri-lock-line" class="warning-action" @click="openCloseShiftDialog">
          Đóng ca hiện tại
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

    <VCard class="shift-panel active-shift-panel">
      <div class="panel-head">
        <div>
          <span>Ca hiện tại</span>
          <strong>Ca làm việc hiện tại</strong>
          <p>Kiểm soát thu ngân, tiền đầu ca và két tiền dự kiến.</p>
        </div>

        <VChip :color="shiftIsOpen ? 'success' : 'secondary'" variant="tonal">
          {{ shiftIsOpen ? 'Đang mở' : 'Chưa mở ca' }}
        </VChip>
      </div>

      <VDivider />

      <VCardText v-if="loadingActive">
        <VSkeletonLoader type="article" />
      </VCardText>

      <VCardText v-else-if="currentActiveShift">
        <div class="active-shift-grid">
          <div class="shift-info-card">
            <span>Mã số ca</span>
            <strong class="text-primary">{{ currentActiveShift.shiftCode }}</strong>
            <p>Định danh ca bán hàng</p>
          </div>

          <div class="shift-info-card">
            <span>Thu ngân phụ trách</span>
            <strong>{{ currentActiveShift.cashierName }}</strong>
            <p>Người mở và chịu trách nhiệm ca</p>
          </div>

          <div class="shift-info-card">
            <span>Giờ mở ca</span>
            <strong>{{ formatTime(currentActiveShift.openedAt) }}</strong>
            <p>Thời điểm bắt đầu bán hàng</p>
          </div>

          <div class="shift-info-card">
            <span>Tiền đầu ca</span>
            <strong class="text-success">{{ formatCurrency(currentActiveShift.openingCash) }}</strong>
            <p>Tiền bàn giao ban đầu</p>
          </div>
        </div>

        <div class="cash-summary-box">
          <div>
            <span>Doanh thu dự kiến trong két</span>
            <strong>{{ formatCurrency(currentActiveShift.expectedCash) }}</strong>
            <p>Tiền đầu ca cộng các khoản thanh toán tiền mặt.</p>
          </div>

          <VBtn color="warning" prepend-icon="ri-lock-line" class="warning-action" @click="openCloseShiftDialog">
            Kết ca & kiểm két
          </VBtn>
        </div>
      </VCardText>

      <VCardText v-else>
        <div class="shift-empty">
          <VIcon icon="ri-door-lock-line" size="46" color="secondary" />

          <strong>Chưa có ca làm việc nào đang mở</strong>

          <span>
            Mở ca làm việc mới để bắt đầu bán hàng và thanh toán trên POS.
          </span>

          <VBtn color="primary" prepend-icon="ri-key-line" class="primary-action" @click="openOpenShiftDialog">
            Mở ca ngay
          </VBtn>
        </div>
      </VCardText>
    </VCard>

    <VCard class="shift-panel">
      <div class="panel-head">
        <div>
          <span>Lịch sử</span>
          <strong>Lịch sử ca làm việc</strong>
          <p>Tra cứu các ca đã mở, đóng và chênh lệch bàn giao két.</p>
        </div>

        <VChip color="primary" variant="tonal">
          {{ formatNumber(totalItems) }} ca
        </VChip>
      </div>

      <VDivider />

      <VCardText>
        <div class="history-toolbar">
          <VTextField v-model="searchCashier" label="Tìm theo thu ngân" placeholder="Nhập tên thu ngân..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable
            @keyup.enter="resetAndLoadHistory" @click:clear="clearSearch" />

          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loadingHistory"
            @click="resetAndLoadHistory">
            Tìm
          </VBtn>

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loadingHistory"
            @click="loadShiftsHistory">
            Tải lại
          </VBtn>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loadingHistory">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="shiftsList.length" class="shift-table-wrap">
          <VTable class="shift-table">
            <thead>
              <tr>
                <th>Mã ca</th>
                <th>Thu ngân</th>
                <th>Giờ mở</th>
                <th>Giờ đóng</th>
                <th class="text-end">Tiền đầu ca</th>
                <th class="text-end">Két dự kiến</th>
                <th class="text-end">Tiền thực tế</th>
                <th class="text-end">Chênh lệch</th>
                <th class="text-center">Trạng thái</th>
                <th>Ghi chú</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="shift in shiftsList" :key="shift.id" class="shift-row">
                <td>
                  <div class="shift-code">
                    {{ shift.shiftCode }}
                  </div>
                </td>

                <td>
                  <div class="cashier-cell">
                    <VAvatar size="34" color="primary" variant="tonal">
                      <VIcon icon="ri-user-line" />
                    </VAvatar>

                    <strong>{{ shift.cashierName }}</strong>
                  </div>
                </td>

                <td>{{ formatTime(shift.openedAt) }}</td>
                <td>{{ formatTime(shift.closedAt) }}</td>

                <td class="text-end amount-cell text-success">
                  {{ formatCurrency(shift.openingCash) }}
                </td>

                <td class="text-end amount-cell">
                  {{ formatCurrency(shift.expectedCash) }}
                </td>

                <td class="text-end amount-cell text-info">
                  {{ shift.actualCash !== null ? formatCurrency(shift.actualCash) : '—' }}
                </td>

                <td class="text-end amount-cell" :class="varianceClass(shift.variance || 0)">
                  {{ shift.closedAt ? formatCurrency(shift.variance || 0) : '—' }}
                </td>

                <td class="text-center">
                  <VChip :color="statusColor(shift.status)" variant="tonal" size="small">
                    {{ statusLabel(shift.status) }}
                  </VChip>
                </td>

                <td>
                  <div class="note-cell" :title="shift.note || ''">
                    {{ shift.note || '—' }}
                  </div>
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="shift-empty">
          <VIcon icon="ri-history-line" size="42" color="primary" />

          <strong>Chưa có ca làm việc nào được lưu</strong>

          <span>
            Các ca làm việc đã đóng sẽ xuất hiện tại danh sách lịch sử.
          </span>
        </div>
      </template>

      <div v-if="shiftsList.length || totalItems > 0" class="shift-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
          trên tổng số {{ formatNumber(totalItems) }} ca làm việc
        </span>

        <div>
          <VSelect v-model="itemsPerPage" :items="[10, 15, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="currentPage" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="openShiftDialog" max-width="560" persistent>
      <VCard class="shift-dialog">
        <div class="dialog-head">
          <div>
            <span>Mở ca</span>
            <h2>Mở ca bán hàng mới</h2>
          </div>

          <VChip color="primary" variant="tonal">
            Bắt đầu ca
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="openError" type="error" variant="tonal" class="mb-4">
            {{ openError }}
          </VAlert>

          <VTextField v-model.number="openOpeningCash" label="Tiền mặt bàn giao đầu ca" placeholder="Nhập số tiền..."
            type="number" min="0" suffix="đ" prepend-inner-icon="ri-money-dollar-circle-line" density="comfortable"
            class="mb-4" required />

          <VTextarea v-model="openNote" label="Ghi chú mở ca" placeholder="Nhập ghi chú đầu ca nếu có..." rows="3"
            density="comfortable" />
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="openLoading" @click="openShiftDialog = false">
            Hủy bỏ
          </VBtn>

          <VBtn color="primary" :loading="openLoading" @click="handleOpenShift">
            Xác nhận mở ca
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="closeShiftDialog" max-width="620" persistent>
      <VCard v-if="currentActiveShift" class="shift-dialog">
        <div class="dialog-head">
          <div>
            <span>Kết ca</span>
            <h2>Kết ca & bàn giao két</h2>
          </div>

          <VChip color="warning" variant="tonal">
            {{ currentActiveShift.shiftCode }}
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="closeError" type="error" variant="tonal" class="mb-4">
            {{ closeError }}
          </VAlert>

          <div class="close-summary">
            <div>
              <span>Thu ngân</span>
              <strong>{{ currentActiveShift.cashierName }}</strong>
            </div>

            <div>
              <span>Giờ mở ca</span>
              <strong>{{ formatTime(currentActiveShift.openedAt) }}</strong>
            </div>

            <div>
              <span>Tiền đầu ca</span>
              <strong class="text-success">{{ formatCurrency(currentActiveShift.openingCash) }}</strong>
            </div>

            <div>
              <span>Két tiền dự kiến</span>
              <strong class="text-primary">{{ formatCurrency(currentActiveShift.expectedCash) }}</strong>
            </div>
          </div>

          <VTextField v-model.number="closeActualCash" label="Tiền mặt kiểm kê thực tế"
            placeholder="Nhập số tiền thực tế trong két..." type="number" min="0" suffix="đ"
            prepend-inner-icon="ri-money-dollar-circle-line" density="comfortable" class="mt-4" required />

          <div v-if="closeActualCash !== null" class="variance-box" :class="{
            'variance-box--error': closeVariance < 0,
            'variance-box--success': closeVariance > 0,
          }">
            <span>Chênh lệch chốt ca</span>
            <strong>{{ formatCurrency(closeVariance) }}</strong>
          </div>

          <VTextarea v-model="closeNote" label="Ghi chú bàn giao chốt ca"
            placeholder="Giải trình lý do chênh lệch hoặc tình hình két tiền..." rows="3" density="comfortable"
            class="mt-4" />
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="closeLoading" @click="closeShiftDialog = false">
            Hủy bỏ
          </VBtn>

          <VBtn color="warning" :loading="closeLoading" @click="handleCloseShift">
            Xác nhận đóng ca
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.cash-shift-page {
  position: relative;
  isolation: isolate;
}

.cash-shift-page::before {
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

.warning-action {
  color: white !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-warning), 0.24);
}

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.summary-card,
.shift-panel,
.shift-dialog {
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

.summary-icon--secondary {
  background: rgb(var(--v-theme-secondary));
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

.active-shift-panel {
  margin-block-end: 1rem;
}

.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
}

.panel-head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.panel-head strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.25rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.panel-head p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.active-shift-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.shift-info-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.95rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.shift-info-card span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.shift-info-card strong {
  display: block;
  margin-block: 0.3rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
  font-weight: 900;
}

.shift-info-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.55);
  font-size: 0.82rem;
}

.cash-summary-box {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.16);
  border-radius: 22px;
  padding: 1.1rem;
  margin-block-start: 1rem;
  background:
    radial-gradient(circle at 10% 20%, rgba(var(--v-theme-primary), 0.12), transparent 34%),
    rgba(var(--v-theme-primary), 0.06);
}

.cash-summary-box span {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 900;
  text-transform: uppercase;
}

.cash-summary-box strong {
  display: block;
  margin-block: 0.25rem;
  color: rgb(var(--v-theme-primary));
  font-size: clamp(1.5rem, 3vw, 2rem);
  font-weight: 950;
  letter-spacing: -0.05em;
}

.cash-summary-box p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
}

.history-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) auto auto;
}

.history-toolbar :deep(.v-field),
.shift-dialog :deep(.v-field) {
  border-radius: 16px;
}

.history-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.shift-table-wrap {
  overflow-x: auto;
}

.shift-table {
  min-inline-size: 1160px;
}

.shift-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.shift-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.shift-row {
  transition: background 160ms ease;
}

.shift-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.shift-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.cashier-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.65rem;
}

.cashier-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.amount-cell {
  font-weight: 950;
  white-space: nowrap;
}

.note-cell {
  display: -webkit-box;
  max-inline-size: 180px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.62);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.shift-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.shift-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.shift-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.shift-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.shift-pagination>div {
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

.close-summary {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.close-summary>div,
.variance-box {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.85rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.close-summary span,
.variance-box span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.close-summary strong,
.variance-box strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-weight: 950;
}

.variance-box {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.variance-box strong {
  color: rgba(var(--v-theme-on-surface), 0.82);
  font-size: 1.05rem;
}

.variance-box--error {
  border-color: rgba(var(--v-theme-error), 0.22);
  background: rgba(var(--v-theme-error), 0.08);
}

.variance-box--error strong {
  color: rgb(var(--v-theme-error));
}

.variance-box--success {
  border-color: rgba(var(--v-theme-success), 0.22);
  background: rgba(var(--v-theme-success), 0.08);
}

.variance-box--success strong {
  color: rgb(var(--v-theme-success));
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {

  .summary-grid,
  .active-shift-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .cash-summary-box {
    align-items: flex-start;
    flex-direction: column;
  }

  .cash-summary-box .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .active-shift-grid,
  .history-toolbar,
  .close-summary {
    grid-template-columns: 1fr;
  }

  .shift-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .shift-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .panel-head,
  .dialog-head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.shift-hero {
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

.shift-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.shift-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.shift-hero__copy {
  display: none !important;
}

.shift-hero__actions,
.shift-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.shift-hero__actions .v-btn,
.shift-actions .v-btn,
.shift-hero__actions .v-btn.primary-action,
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