<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  type ActivityLogDto,
  type UserDto,
  getActivityLogs,
  getUsers,
} from '@/services/adminApi'

const loading = ref(false)
const items = ref<ActivityLogDto[]>([])
const users = ref<UserDto[]>([])
const errorMessage = ref('')

const detailDialog = ref(false)
const selectedLog = ref<ActivityLogDto | null>(null)

const filterUserId = ref<string | null>(null)
const filterServiceName = ref<string | null>(null)
const filterSeverity = ref<string | null>(null)
const filterAction = ref<string | null>(null)
const filterEntityType = ref<string | null>(null)
const filterFromDate = ref('')
const filterToDate = ref('')

const page = ref(1)
const pageSize = ref(20)
const totalItems = ref(0)
const totalPages = ref(0)

const services = [
  { title: 'Tất cả dịch vụ', value: null },
  { title: 'UserReportService', value: 'UserReportService' },
  { title: 'ProductInventoryService', value: 'ProductInventoryService' },
  { title: 'OrderSalesService', value: 'OrderSalesService' },
]

const severities = [
  { title: 'Tất cả mức độ', value: null },
  { title: 'Bình thường', value: 'Info' },
  { title: 'Cần theo dõi', value: 'Warning' },
  { title: 'Nghiêm trọng', value: 'Error' },
]

const userItems = computed(() => [
  { title: 'Tất cả nhân viên', value: null },
  ...users.value.map(user => ({
    title: user.fullName,
    value: user.id,
  })),
])

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatDate = (dateStr: string) => {
  if (!dateStr)
    return '—'

  const date = new Date(dateStr)

  if (Number.isNaN(date.getTime()))
    return '—'

  return date.toLocaleString('vi-VN')
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

const getSeverityColor = (severity: string) => {
  switch (severity?.toLowerCase()) {
    case 'error':
      return 'error'
    case 'warning':
      return 'warning'
    case 'info':
      return 'success'
    default:
      return 'secondary'
  }
}

const getSeverityText = (severity: string) => {
  switch (severity?.toLowerCase()) {
    case 'error':
      return 'Nghiêm trọng'
    case 'warning':
      return 'Cần theo dõi'
    case 'info':
      return 'Bình thường'
    default:
      return severity || 'Bình thường'
  }
}

const getServiceLabel = (serviceName: string) => {
  const map: Record<string, string> = {
    UserReportService: 'Người dùng / Báo cáo',
    ProductInventoryService: 'Sản phẩm / Kho',
    OrderSalesService: 'Đơn hàng / Bán hàng',
  }

  return map[serviceName] ?? serviceName
}

const getServiceColor = (serviceName: string) => {
  if (serviceName === 'ProductInventoryService')
    return 'primary'

  if (serviceName === 'OrderSalesService')
    return 'success'

  if (serviceName === 'UserReportService')
    return 'info'

  return 'secondary'
}

const rangeStart = computed(() => {
  if (!items.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalItems.value),
)

const infoCount = computed(() =>
  items.value.filter(item => item.severity?.toLowerCase() === 'info').length,
)

const warningCount = computed(() =>
  items.value.filter(item => item.severity?.toLowerCase() === 'warning').length,
)

const errorCount = computed(() =>
  items.value.filter(item => item.severity?.toLowerCase() === 'error').length,
)

const summaryCards = computed(() => [
  {
    label: 'Tổng bản ghi',
    value: formatNumber(totalItems.value),
    helper: `Trang hiện tại: ${formatNumber(items.value.length)} bản ghi`,
    icon: 'ri-history-line',
    color: 'primary',
  },
  {
    label: 'Bình thường',
    value: formatNumber(infoCount.value),
    helper: 'Thao tác hệ thống không có cảnh báo',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Cần theo dõi',
    value: formatNumber(warningCount.value),
    helper: 'Các thao tác cần rà soát thêm',
    icon: 'ri-alert-line',
    color: 'warning',
  },
  {
    label: 'Nghiêm trọng',
    value: formatNumber(errorCount.value),
    helper: 'Lỗi hoặc hành động quan trọng',
    icon: 'ri-error-warning-line',
    color: 'error',
  },
])

const loadUsers = async () => {
  try {
    const result = await getUsers({ pageSize: 100 })

    users.value = result.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách nhân viên.'
  }
}

const loadLogs = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getActivityLogs({
      userId: filterUserId.value || undefined,
      serviceName: filterServiceName.value || undefined,
      severity: filterSeverity.value || undefined,
      action: filterAction.value?.trim() || undefined,
      entityType: filterEntityType.value?.trim() || undefined,
      from: toStartIso(filterFromDate.value),
      to: toEndIso(filterToDate.value),
      page: page.value,
      pageSize: pageSize.value,
    })

    items.value = result.items
    totalItems.value = result.totalCount
    totalPages.value = Math.max(1, result.totalPages)
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách nhật ký hoạt động.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadLogs()
  else
    page.value = 1
}

const handleResetFilters = () => {
  filterUserId.value = null
  filterServiceName.value = null
  filterSeverity.value = null
  filterAction.value = null
  filterEntityType.value = null
  filterFromDate.value = ''
  filterToDate.value = ''

  resetAndLoad()
}

const openLogDetail = (log: ActivityLogDto) => {
  selectedLog.value = log
  detailDialog.value = true
}

watch(page, () => {
  void loadLogs()
})

watch(pageSize, () => {
  page.value = 1
  void loadLogs()
})

onMounted(async () => {
  await loadUsers()
  await loadLogs()
})
</script>

<template>
  <section class="activity-page">
    <div class="activity-hero">
      <div class="activity-hero__title-area">
        <h1>Nhật ký hoạt động hệ thống</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-shield-user-line" class="ml-2">
          Quản trị
        </VChip>
      </div>

      <div class="activity-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
          @click="resetAndLoad">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-filter-3-line" class="primary-action" :loading="loading"
          @click="resetAndLoad">
          Lọc dữ liệu
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

    <VCard class="activity-filter-card">
      <VCardText>
        <div class="activity-filter-grid">
          <VSelect v-model="filterUserId" label="Nhân viên" :items="userItems" item-title="title" item-value="value"
            density="comfortable" hide-details />

          <VSelect v-model="filterServiceName" label="Dịch vụ" :items="services" item-title="title" item-value="value"
            density="comfortable" hide-details />

          <VSelect v-model="filterSeverity" label="Mức độ" :items="severities" item-title="title" item-value="value"
            density="comfortable" hide-details />

          <VTextField v-model="filterAction" label="Hành động" placeholder="Create, Update, Delete..."
            density="comfortable" hide-details clearable @keyup.enter="resetAndLoad" />

          <VTextField v-model="filterEntityType" label="Loại dữ liệu" placeholder="Order, Product, User..."
            density="comfortable" hide-details clearable @keyup.enter="resetAndLoad" />

          <VTextField v-model="filterFromDate" type="date" label="Từ ngày" density="comfortable" hide-details />

          <VTextField v-model="filterToDate" type="date" label="Đến ngày" density="comfortable" hide-details />

          <div class="filter-actions">
            <VBtn color="primary" prepend-icon="ri-filter-3-line" :loading="loading" @click="resetAndLoad">
              Lọc
            </VBtn>

            <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line" @click="handleResetFilters">
              Xóa lọc
            </VBtn>
          </div>
        </div>
      </VCardText>
    </VCard>

    <VCard class="activity-panel">
      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="items.length" class="activity-table-wrap">
          <VTable class="activity-table">
            <thead>
              <tr>
                <th>Người dùng</th>
                <th>Dịch vụ</th>
                <th>Thao tác</th>
                <th>Loại dữ liệu</th>
                <th>Mức độ</th>
                <th>Thời gian</th>
                <th class="text-center">Chi tiết</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="log in items" :key="log.id" class="activity-row" @click="openLogDetail(log)">
                <td>
                  <div class="user-cell">
                    <VAvatar size="34" color="primary" variant="tonal">
                      <VIcon icon="ri-user-line" />
                    </VAvatar>

                    <div>
                      <strong>{{ log.userName || 'Hệ thống' }}</strong>
                      <span>Người thao tác</span>
                    </div>
                  </div>
                </td>

                <td>
                  <VChip :color="getServiceColor(log.serviceName)" variant="tonal" size="small">
                    {{ getServiceLabel(log.serviceName) }}
                  </VChip>
                </td>

                <td>
                  <VChip size="small" variant="tonal" color="info">
                    {{ log.action }}
                  </VChip>
                </td>

                <td>
                  <span class="entity-cell">
                    {{ log.entityType || '—' }}
                  </span>
                </td>

                <td>
                  <VChip :color="getSeverityColor(log.severity)" variant="tonal" size="small">
                    {{ getSeverityText(log.severity) }}
                  </VChip>
                </td>

                <td>
                  <div class="time-cell">
                    {{ formatDate(log.createdAt) }}
                  </div>
                </td>

                <td class="text-center" @click.stop>
                  <VBtn icon="ri-eye-line" size="small" variant="text" color="primary" @click="openLogDetail(log)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="activity-empty">
          <VIcon icon="ri-history-line" size="42" color="primary" />
          <strong>Không tìm thấy bản ghi hoạt động nào</strong>
          <span>Hãy thử thay đổi bộ lọc hoặc khoảng thời gian tìm kiếm.</span>

          <VBtn color="primary" prepend-icon="ri-refresh-line" @click="handleResetFilters">
            Xóa lọc và tải lại
          </VBtn>
        </div>
      </template>

      <div v-if="items.length || totalItems > 0" class="activity-pagination">
        <span>
          Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
          trên tổng số {{ formatNumber(totalItems) }} bản ghi
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="detailDialog" max-width="720">
      <VCard v-if="selectedLog" class="activity-dialog">
        <div class="dialog-head">
          <div>
            <span>Chi tiết nhật ký</span>
            <h2>{{ selectedLog.action }}</h2>
          </div>

          <VChip :color="getSeverityColor(selectedLog.severity)" variant="tonal">
            {{ getSeverityText(selectedLog.severity) }}
          </VChip>
        </div>

        <VCardText>
          <div class="detail-grid">
            <div class="detail-card">
              <span>Người dùng</span>
              <strong>{{ selectedLog.userName || 'Hệ thống' }}</strong>
              <p>{{ formatDate(selectedLog.createdAt) }}</p>
            </div>

            <div class="detail-card">
              <span>Dịch vụ</span>
              <strong>{{ getServiceLabel(selectedLog.serviceName) }}</strong>
              <p>{{ selectedLog.serviceName }}</p>
            </div>

            <div class="detail-card">
              <span>Dữ liệu</span>
              <strong>{{ selectedLog.entityType || '—' }}</strong>
              <p>Loại dữ liệu bị tác động</p>
            </div>
          </div>

          <div class="log-summary-box">
            <div>
              <span>Thao tác</span>
              <strong>{{ selectedLog.action || '—' }}</strong>
            </div>

            <div>
              <span>Mức độ</span>
              <strong>{{ getSeverityText(selectedLog.severity) }}</strong>
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
.activity-page {
  position: relative;
  isolation: isolate;
}

.activity-page::before {
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

.activity-filter-card,
.activity-panel,
.activity-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.activity-filter-card {
  margin-block-end: 1rem;
}

.activity-filter-grid {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.activity-filter-grid :deep(.v-field),
.activity-dialog :deep(.v-field) {
  border-radius: 16px;
}

.filter-actions {
  display: flex;
  gap: 0.65rem;
}

.filter-actions .v-btn {
  flex: 1;
  border-radius: 14px;
  font-weight: 800;
}

.activity-table-wrap {
  overflow-x: auto;
}

.activity-table {
  min-inline-size: 980px;
}

.activity-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.activity-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.activity-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.activity-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user-cell strong,
.user-cell span {
  display: block;
}

.user-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.user-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.entity-cell {
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-weight: 800;
}

.time-cell {
  color: rgba(var(--v-theme-on-surface), 0.64);
  font-weight: 700;
  white-space: nowrap;
}

.activity-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.activity-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.activity-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.activity-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.activity-pagination>div {
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

.detail-card,
.log-summary-box {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.95rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.detail-card span,
.log-summary-box span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.detail-card strong,
.log-summary-box strong {
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

.log-summary-box {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.dialog-actions {
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {

  .summary-grid,
  .activity-filter-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .activity-filter-grid,
  .detail-grid,
  .log-summary-box {
    grid-template-columns: 1fr;
  }

  .filter-actions {
    flex-direction: column;
  }

  .activity-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .activity-pagination>div {
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
.activity-hero {
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

.activity-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.activity-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.activity-hero__copy {
  display: none !important;
}

.activity-hero__actions,
.activity-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.activity-hero__actions .v-btn,
.activity-actions .v-btn,
.activity-hero__actions .v-btn.primary-action,
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