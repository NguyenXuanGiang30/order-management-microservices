<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import {
  type CashShiftDto,
  getActivePromotions,
  getCurrentShift,
  getCustomers,
  getOrders,
} from '@/services/orderSalesApi'

const loading = ref(false)
const errorMessage = ref('')

const orderCount = ref(0)
const customerCount = ref(0)
const promotionCount = ref(0)
const currentShift = ref<CashShiftDto | null>(null)

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const shiftIsOpen = computed(() => currentShift.value?.status === 'Open')

const metrics = computed(() => [
  {
    label: 'Đơn hàng',
    value: formatNumber(orderCount.value),
    helper: 'Tổng số đơn từ phân hệ bán hàng',
    icon: 'ri-receipt-line',
    color: 'primary',
  },
  {
    label: 'Khách hàng',
    value: formatNumber(customerCount.value),
    helper: 'Hồ sơ khách dùng cho POS và công nợ',
    icon: 'ri-user-3-line',
    color: 'success',
  },
  {
    label: 'Khuyến mãi',
    value: formatNumber(promotionCount.value),
    helper: 'Mã đang hoạt động có thể áp dụng tại POS',
    icon: 'ri-coupon-3-line',
    color: 'warning',
  },
  {
    label: 'Ca bán hiện tại',
    value: shiftIsOpen.value ? 'Đang mở' : 'Chưa mở',
    helper: currentShift.value?.shiftCode || 'Mở ca trước khi thanh toán',
    icon: 'ri-cash-line',
    color: shiftIsOpen.value ? 'success' : 'secondary',
  },
])

const actions = [
  {
    title: 'Bán hàng POS',
    description: 'Tạo đơn nhanh tại quầy, áp khuyến mãi, nhận thanh toán và in hóa đơn.',
    icon: 'ri-shopping-cart-2-line',
    to: '/pos',
    color: 'primary',
    badge: 'Ưu tiên',
  },
  {
    title: 'Quản lý đơn hàng',
    description: 'Theo dõi trạng thái, xem chi tiết, xác nhận, hủy hoặc xử lý đơn sau bán.',
    icon: 'ri-receipt-line',
    to: '/orders',
    color: 'info',
    badge: 'Đơn hàng',
  },
  {
    title: 'Khách hàng',
    description: 'Tra cứu hồ sơ khách, nhóm khách, lịch sử mua hàng và công nợ.',
    icon: 'ri-user-3-line',
    to: '/customers',
    color: 'success',
    badge: 'CRM',
  },
  {
    title: 'Công nợ',
    description: 'Theo dõi khoản phải thu, đơn ghi nợ và khách hàng cần thanh toán.',
    icon: 'ri-file-list-3-line',
    to: '/debts',
    color: 'warning',
    badge: 'Theo dõi',
  },
]

const shiftRows = computed(() => {
  if (!currentShift.value)
    return []

  return [
    {
      label: 'Thu ngân',
      value: currentShift.value.cashierName || '—',
      icon: 'ri-user-line',
    },
    {
      label: 'Mã ca',
      value: currentShift.value.shiftCode || '—',
      icon: 'ri-qr-code-line',
    },
    {
      label: 'Tiền đầu ca',
      value: formatCurrency(currentShift.value.openingCash),
      icon: 'ri-wallet-3-line',
    },
    {
      label: 'Tiền kỳ vọng',
      value: formatCurrency(currentShift.value.expectedCash),
      icon: 'ri-money-dollar-circle-line',
      class: 'text-primary',
    },
  ]
})

const loadSalesWorkspace = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [orders, customers, promotions, shift] = await Promise.all([
      getOrders({ pageSize: 1 }),
      getCustomers({ pageSize: 1 }),
      getActivePromotions(),
      getCurrentShift().catch(() => null),
    ])

    orderCount.value = orders.totalCount
    customerCount.value = customers.totalCount
    promotionCount.value = promotions.length
    currentShift.value = shift
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu workspace bán hàng.'
  }
  finally {
    loading.value = false
  }
}

onMounted(loadSalesWorkspace)
</script>

<template>
  <section class="sales-workspace-page">
    <div class="sales-hero">
      <div class="sales-hero__title-area">
        <h1>Nhân viên bán hàng</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-store-2-line" class="ml-2">
          Workspace bán hàng
        </VChip>
      </div>

      <div class="sales-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadSalesWorkspace">
          Tải lại
        </VBtn>

        <VBtn to="/pos" color="primary" prepend-icon="ri-shopping-cart-2-line" class="primary-action">
          Vào POS
        </VBtn>
      </div>
    </div>

    <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
      {{ errorMessage }}
    </VAlert>

    <VCard class="workspace-focus-card">
      <VCardText>
        <div class="focus-layout">
          <div>
            <VChip color="primary" variant="tonal" prepend-icon="ri-store-2-line" class="mb-4">
              Quầy bán hàng
            </VChip>

            <h2>Ưu tiên tốc độ tạo đơn và kiểm soát ca bán.</h2>

            <p>
              Các thao tác POS, đơn hàng, khách hàng và công nợ được gom vào một workspace để nhân viên bán hàng xử lý
              nhanh tại quầy.
            </p>
          </div>

          <div class="shift-status-card">
            <div class="shift-status-card__head">
              <span>Trạng thái ca</span>

              <VChip :color="shiftIsOpen ? 'success' : 'secondary'" variant="tonal" size="small">
                {{ shiftIsOpen ? 'Đang mở' : 'Chưa mở' }}
              </VChip>
            </div>

            <strong>
              {{ shiftIsOpen ? currentShift?.shiftCode : 'Chưa có ca bán' }}
            </strong>

            <p>
              {{ shiftIsOpen ? 'Bạn có thể tiếp tục bán hàng tại POS.' : 'Vào POS để mở ca trước khi thanh toán.' }}
            </p>

            <VBtn block color="primary" to="/pos" prepend-icon="ri-arrow-right-line">
              Tiếp tục bán hàng
            </VBtn>
          </div>
        </div>
      </VCardText>
    </VCard>

    <div class="summary-grid">
      <article v-for="metric in metrics" :key="metric.label" class="summary-card">
        <template v-if="loading">
          <VSkeletonLoader type="list-item-avatar-two-line" />
        </template>

        <template v-else>
          <div class="summary-icon" :class="`summary-icon--${metric.color}`">
            <VIcon :icon="metric.icon" />
          </div>

          <div>
            <span>{{ metric.label }}</span>
            <strong>{{ metric.value }}</strong>
            <p>{{ metric.helper }}</p>
          </div>
        </template>
      </article>
    </div>

    <div class="workspace-layout">
      <div class="action-grid">
        <VCard v-for="action in actions" :key="action.to" class="workspace-action-card" :to="action.to" link>
          <VCardText>
            <div class="action-card-head">
              <VAvatar :color="action.color" variant="tonal" rounded="lg">
                <VIcon :icon="action.icon" />
              </VAvatar>

              <VChip :color="action.color" variant="tonal" size="small">
                {{ action.badge }}
              </VChip>
            </div>

            <h3>{{ action.title }}</h3>
            <p>{{ action.description }}</p>

            <div class="action-card-footer">
              <span>Mở chức năng</span>
              <VIcon icon="ri-arrow-right-line" />
            </div>
          </VCardText>
        </VCard>
      </div>

      <aside class="workspace-aside">
        <VCard class="workspace-panel shift-panel">
          <div class="panel-head">
            <div>
              <span>Cash shift</span>
              <strong>Ca bán hiện tại</strong>
              <p>Theo dõi tiền mặt và người phụ trách quầy.</p>
            </div>

            <VIcon icon="ri-cash-line" color="primary" size="28" />
          </div>

          <VDivider />

          <VCardText v-if="loading">
            <VSkeletonLoader type="list-item-avatar-three-line" />
          </VCardText>

          <VCardText v-else-if="shiftRows.length">
            <div class="shift-row-list">
              <div v-for="row in shiftRows" :key="row.label" class="shift-row">
                <div>
                  <VIcon :icon="row.icon" />
                  <span>{{ row.label }}</span>
                </div>

                <strong :class="row.class">
                  {{ row.value }}
                </strong>
              </div>
            </div>
          </VCardText>

          <VCardText v-else>
            <div class="workspace-empty">
              <VIcon icon="ri-time-line" color="secondary" size="36" />
              <strong>Chưa có ca mở</strong>
              <span>Vào POS để mở ca trước khi thanh toán.</span>

              <VBtn color="primary" to="/pos" prepend-icon="ri-cash-line">
                Mở ca bán
              </VBtn>
            </div>
          </VCardText>
        </VCard>

        <VCard class="workspace-panel rules-panel">
          <div class="panel-head compact">
            <div>
              <span>Gợi ý thao tác</span>
              <strong>Quy trình tại quầy</strong>
            </div>
          </div>

          <div class="rule-list">
            <div>
              <VIcon icon="ri-cash-line" color="primary" />
              <span>Mở ca bán trước khi nhận thanh toán.</span>
            </div>

            <div>
              <VIcon icon="ri-coupon-3-line" color="warning" />
              <span>Kiểm tra khuyến mãi đang hoạt động trước khi chốt đơn.</span>
            </div>

            <div>
              <VIcon icon="ri-user-3-line" color="success" />
              <span>Gắn khách hàng vào đơn để theo dõi lịch sử mua và công nợ.</span>
            </div>
          </div>
        </VCard>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.sales-workspace-page {
  position: relative;
  isolation: isolate;
}

.sales-workspace-page::before {
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

.workspace-focus-card,
.workspace-panel,
.workspace-action-card,
.summary-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.workspace-focus-card {
  margin-block-end: 1rem;
}

.focus-layout {
  display: grid;
  align-items: center;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 340px;
}

.focus-layout h2 {
  margin: 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: clamp(1.5rem, 3vw, 2.2rem);
  font-weight: 950;
  letter-spacing: -0.05em;
}

.focus-layout p {
  max-inline-size: 760px;
  margin: 0.75rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.62);
  line-height: 1.6;
}

.shift-status-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.52);
}

.shift-status-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-block-end: 0.75rem;
}

.shift-status-card__head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
  font-weight: 800;
}

.shift-status-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.1rem;
  font-weight: 950;
}

.shift-status-card p {
  margin: 0.4rem 0 1rem;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.88rem;
}

.shift-status-card .v-btn {
  border-radius: 14px;
  font-weight: 850;
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

.workspace-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 360px;
}

.action-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.workspace-action-card {
  block-size: 100%;
  transition:
    transform 180ms ease,
    box-shadow 180ms ease,
    border-color 180ms ease;
}

.workspace-action-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.28);
  transform: translateY(-2px);
  box-shadow: 0 24px 64px rgba(15, 23, 42, 0.12);
}

.action-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-block-end: 1rem;
}

.workspace-action-card h3 {
  margin: 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.15rem;
  font-weight: 950;
  letter-spacing: -0.03em;
}

.workspace-action-card p {
  min-block-size: 48px;
  margin: 0.5rem 0 1rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.9rem;
  line-height: 1.55;
}

.action-card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: rgb(var(--v-theme-primary));
  font-size: 0.85rem;
  font-weight: 850;
}

.workspace-aside {
  display: grid;
  gap: 1rem;
}

.shift-panel {
  position: sticky;
  inset-block-start: 84px;
}

.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
}

.panel-head.compact {
  padding-block-end: 0.7rem;
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

.shift-row-list {
  display: grid;
  gap: 0.65rem;
}

.shift-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.1);
  padding-block: 0.65rem;
}

.shift-row>div {
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  color: rgba(var(--v-theme-on-surface), 0.62);
  font-size: 0.9rem;
}

.shift-row strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 950;
  text-align: end;
}

.workspace-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 210px;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.workspace-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.workspace-empty span {
  font-size: 0.9rem;
}

.workspace-empty .v-btn {
  margin-block-start: 0.5rem;
  border-radius: 14px;
  font-weight: 850;
}

.rule-list {
  display: grid;
  gap: 0.9rem;
  padding: 0 1.25rem 1.25rem;
}

.rule-list>div {
  display: grid;
  align-items: start;
  gap: 0.65rem;
  grid-template-columns: 22px minmax(0, 1fr);
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.5;
}@media (max-width: 1200px) {

  .focus-layout,
  .workspace-layout {
    grid-template-columns: 1fr;
  }

  .shift-panel {
    position: static;
  }

  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .action-grid {
    grid-template-columns: 1fr;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.sales-hero {
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

.sales-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.sales-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.sales-hero__copy {
  display: none !important;
}

.sales-hero__actions,
.sales-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.sales-hero__actions .v-btn,
.sales-actions .v-btn,
.sales-hero__actions .v-btn.primary-action,
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