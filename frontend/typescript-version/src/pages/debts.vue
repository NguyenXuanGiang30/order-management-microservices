<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import {
  type CustomerDto,
  type SupplierDto,
  getCustomers,
  getSuppliers,
} from '@/services/orderSalesApi'

const customers = ref<CustomerDto[]>([])
const suppliers = ref<SupplierDto[]>([])

const loading = ref(false)
const errorMessage = ref('')

const customerSearch = ref('')
const supplierSearch = ref('')

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const receivables = computed(() =>
  customers.value.reduce((sum, customer) => sum + (customer.debtAmount || 0), 0),
)

const payables = computed(() =>
  suppliers.value.reduce((sum, supplier) => sum + (supplier.debtAmount || 0), 0),
)

const netDebt = computed(() => receivables.value - payables.value)

const customersWithDebt = computed(() =>
  customers.value.filter(customer => (customer.debtAmount || 0) > 0),
)

const suppliersWithDebt = computed(() =>
  suppliers.value.filter(supplier => (supplier.debtAmount || 0) > 0),
)

const filteredCustomersWithDebt = computed(() => {
  const keyword = customerSearch.value.trim().toLowerCase()

  if (!keyword)
    return customersWithDebt.value

  return customersWithDebt.value.filter(customer => {
    const name = customer.fullName?.toLowerCase() || ''
    const phone = customer.phone?.toLowerCase() || ''

    return name.includes(keyword) || phone.includes(keyword)
  })
})

const filteredSuppliersWithDebt = computed(() => {
  const keyword = supplierSearch.value.trim().toLowerCase()

  if (!keyword)
    return suppliersWithDebt.value

  return suppliersWithDebt.value.filter(supplier => {
    const name = supplier.name?.toLowerCase() || ''
    const note = supplier.note?.toLowerCase() || ''

    return name.includes(keyword) || note.includes(keyword)
  })
})

const debtStatusLabel = computed(() => {
  if (netDebt.value > 0)
    return 'Thu ròng'

  if (netDebt.value < 0)
    return 'Trả ròng'

  return 'Cân bằng'
})

const debtStatusColor = computed(() => {
  if (netDebt.value > 0)
    return 'primary'

  if (netDebt.value < 0)
    return 'error'

  return 'success'
})

const summaryCards = computed(() => [
  {
    label: 'Phải thu',
    value: formatCurrency(receivables.value),
    helper: `${formatNumber(customersWithDebt.value.length)} khách hàng còn nợ`,
    icon: 'ri-arrow-left-down-line',
    color: 'warning',
  },
  {
    label: 'Phải trả',
    value: formatCurrency(payables.value),
    helper: `${formatNumber(suppliersWithDebt.value.length)} nhà cung cấp cần thanh toán`,
    icon: 'ri-arrow-right-up-line',
    color: 'error',
  },
  {
    label: 'Chênh lệch',
    value: formatCurrency(netDebt.value),
    helper: `${debtStatusLabel.value}: phải thu trừ phải trả`,
    icon: 'ri-scales-3-line',
    color: debtStatusColor.value,
  },
  {
    label: 'Đối tượng công nợ',
    value: formatNumber(customersWithDebt.value.length + suppliersWithDebt.value.length),
    helper: 'Tổng khách hàng và nhà cung cấp còn công nợ',
    icon: 'ri-group-line',
    color: 'success',
  },
])

const loadDebts = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [customerResult, supplierResult] = await Promise.all([
      getCustomers({ pageSize: 100 }),
      getSuppliers({ pageSize: 100 }),
    ])

    customers.value = customerResult.items
    suppliers.value = supplierResult.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu công nợ.'
  }
  finally {
    loading.value = false
  }
}

onMounted(loadDebts)
</script>

<template>
  <section class="debts-page">
    <div class="debts-hero">
      <div class="debts-hero__title-area">
        <h1>Công nợ</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-scales-3-line" class="ml-2">
          Kiểm soát công nợ
        </VChip>
      </div>

      <div class="debts-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading" @click="loadDebts">
          Tải lại
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

    <div class="debt-layout">
      <VCard class="debt-panel">
        <div class="panel-head">
          <div>
            <span>Công nợ khách hàng</span>
            <strong>Phải thu</strong>
            <p>Danh sách khách hàng còn nợ chưa thanh toán.</p>
          </div>

          <VChip color="warning" variant="tonal">
            {{ formatCurrency(receivables) }}
          </VChip>
        </div>

        <VDivider />

        <VCardText>
          <VTextField v-model="customerSearch" label="Tìm khách hàng" placeholder="Tên khách hàng hoặc số điện thoại..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable />
        </VCardText>

        <VDivider />

        <VCardText v-if="loading">
          <VSkeletonLoader type="table-heading, table-tbody" />
        </VCardText>

        <template v-else>
          <div v-if="filteredCustomersWithDebt.length" class="debt-table-wrap">
            <VTable class="debt-table">
              <thead>
                <tr>
                  <th>Khách hàng</th>
                  <th>Điện thoại</th>
                  <th class="text-end">Số tiền nợ</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="customer in filteredCustomersWithDebt" :key="customer.id" class="debt-row">
                  <td>
                    <div class="entity-cell">
                      <VAvatar size="34" color="warning" variant="tonal">
                        <VIcon icon="ri-user-line" />
                      </VAvatar>

                      <div>
                        <strong>{{ customer.fullName }}</strong>
                        <span>Khách hàng</span>
                      </div>
                    </div>
                  </td>

                  <td>{{ customer.phone || '—' }}</td>

                  <td class="text-end debt-amount debt-amount--warning">
                    {{ formatCurrency(customer.debtAmount) }}
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div v-else class="debt-empty">
            <VIcon icon="ri-check-double-line" size="42" color="success" />
            <strong>Không có công nợ khách hàng</strong>
            <span>Tất cả khách hàng đã thanh toán đầy đủ hoặc không khớp bộ lọc.</span>
          </div>
        </template>
      </VCard>

      <VCard class="debt-panel">
        <div class="panel-head">
          <div>
            <span>Công nợ nhà cung cấp</span>
            <strong>Phải trả</strong>
            <p>Danh sách nhà cung cấp còn khoản cần thanh toán.</p>
          </div>

          <VChip color="error" variant="tonal">
            {{ formatCurrency(payables) }}
          </VChip>
        </div>

        <VDivider />

        <VCardText>
          <VTextField v-model="supplierSearch" label="Tìm nhà cung cấp" placeholder="Tên nhà cung cấp hoặc ghi chú..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable />
        </VCardText>

        <VDivider />

        <VCardText v-if="loading">
          <VSkeletonLoader type="table-heading, table-tbody" />
        </VCardText>

        <template v-else>
          <div v-if="filteredSuppliersWithDebt.length" class="debt-table-wrap">
            <VTable class="debt-table">
              <thead>
                <tr>
                  <th>Nhà cung cấp</th>
                  <th>Ghi chú</th>
                  <th class="text-end">Số tiền nợ</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="supplier in filteredSuppliersWithDebt" :key="supplier.id" class="debt-row">
                  <td>
                    <div class="entity-cell">
                      <VAvatar size="34" color="error" variant="tonal">
                        <VIcon icon="ri-building-4-line" />
                      </VAvatar>

                      <div>
                        <strong>{{ supplier.name }}</strong>
                        <span>Nhà cung cấp</span>
                      </div>
                    </div>
                  </td>

                  <td>
                    <div class="note-cell">
                      {{ supplier.note || '—' }}
                    </div>
                  </td>

                  <td class="text-end debt-amount debt-amount--error">
                    {{ formatCurrency(supplier.debtAmount) }}
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div v-else class="debt-empty">
            <VIcon icon="ri-check-double-line" size="42" color="success" />
            <strong>Không có công nợ nhà cung cấp</strong>
            <span>Đã thanh toán đầy đủ cho tất cả nhà cung cấp hoặc không khớp bộ lọc.</span>
          </div>
        </template>
      </VCard>
    </div>
  </section>
</template>

<style scoped>
.debts-page {
  position: relative;
  isolation: isolate;
}

.debts-page::before {
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

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
}

.summary-icon--error {
  background: rgb(var(--v-theme-error));
}

.summary-icon--success {
  background: rgb(var(--v-theme-success));
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

.debt-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.debt-panel {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
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
  font-size: 1.35rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.panel-head p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.9rem;
}

.debt-panel :deep(.v-field) {
  border-radius: 16px;
}

.debt-table-wrap {
  overflow-x: auto;
}

.debt-table {
  min-inline-size: 620px;
}

.debt-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.debt-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.debt-row {
  transition: background 160ms ease;
}

.debt-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.entity-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.entity-cell strong,
.entity-cell span {
  display: block;
}

.entity-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.entity-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.note-cell {
  display: -webkit-box;
  max-inline-size: 260px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.debt-amount {
  font-weight: 950;
  white-space: nowrap;
}

.debt-amount--warning {
  color: rgb(var(--v-theme-warning));
}

.debt-amount--error {
  color: rgb(var(--v-theme-error));
}

.debt-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.debt-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}@media (max-width: 1200px) {

  .summary-grid,
  .debt-layout {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .debt-layout {
    grid-template-columns: 1fr;
  }
}@media (max-width: 760px) {

  .summary-grid {
    grid-template-columns: 1fr;
  }

  .panel-head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.debts-hero {
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

.debts-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.debts-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.debts-hero__copy {
  display: none !important;
}

.debts-hero__actions,
.debts-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.debts-hero__actions .v-btn,
.debts-actions .v-btn,
.debts-hero__actions .v-btn.primary-action,
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