<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { getSuppliers } from '@/services/orderSalesApi'

import {
  type StockDto,
  getGoodsReceipts,
  getInventoryStock,
  getProducts,
} from '@/services/productInventoryApi'

const loading = ref(false)
const errorMessage = ref('')

const productCount = ref(0)
const lowStockCount = ref(0)
const receiptCount = ref(0)
const supplierCount = ref(0)
const lowStockItems = ref<StockDto[]>([])

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const stockStatusColor = (stock: StockDto) => {
  if (stock.alertLevel === 'Critical')
    return 'error'

  return 'warning'
}

const stockStatusText = (stock: StockDto) => {
  if (stock.alertLevel === 'Critical')
    return 'Rất thấp'

  return 'Sắp hết'
}

const summaryCards = computed(() => [
  {
    label: 'Sản phẩm',
    value: formatNumber(productCount.value),
    helper: 'Danh mục hàng hóa đang quản lý',
    icon: 'ri-price-tag-3-line',
    color: 'primary',
  },
  {
    label: 'Tồn thấp',
    value: formatNumber(lowStockCount.value),
    helper: 'SKU dưới ngưỡng cần kiểm tra',
    icon: 'ri-alert-line',
    color: lowStockCount.value > 0 ? 'warning' : 'success',
  },
  {
    label: 'Phiếu nhập',
    value: formatNumber(receiptCount.value),
    helper: 'Theo dõi nhập hàng từ nhà cung cấp',
    icon: 'ri-truck-line',
    color: 'info',
  },
  {
    label: 'Nhà cung cấp',
    value: formatNumber(supplierCount.value),
    helper: 'Đối tác cung ứng và công nợ liên quan',
    icon: 'ri-building-4-line',
    color: 'success',
  },
])

const actions = [
  {
    title: 'Kiểm soát tồn kho',
    description: 'Xem tồn khả dụng, cảnh báo tồn thấp và lịch sử biến động kho.',
    icon: 'ri-archive-line',
    to: '/inventory',
    color: 'primary',
    badge: 'Tồn kho',
  },
  {
    title: 'Sản phẩm',
    description: 'Quản lý SKU, danh mục, đơn vị tính, giá nhập và giá bán.',
    icon: 'ri-price-tag-3-line',
    to: '/products',
    color: 'success',
    badge: 'SKU',
  },
  {
    title: 'Nhập hàng',
    description: 'Tạo phiếu nhập, xác nhận hàng về kho và cập nhật tồn.',
    icon: 'ri-truck-line',
    to: '/goods-receipts',
    color: 'info',
    badge: 'Nhập kho',
  },
  {
    title: 'Nhà cung cấp',
    description: 'Theo dõi thông tin nhà cung cấp và công nợ phải trả.',
    icon: 'ri-building-4-line',
    to: '/suppliers',
    color: 'warning',
    badge: 'NCC',
  },
]

const warehouseFlow = [
  {
    title: 'Kiểm tra cảnh báo',
    description: 'Ưu tiên SKU dưới ngưỡng tồn tối thiểu.',
    icon: 'ri-alarm-warning-line',
    color: 'warning',
  },
  {
    title: 'Xác nhận nhập hàng',
    description: 'Phiếu nhập đã xác nhận sẽ cập nhật tồn kho.',
    icon: 'ri-truck-line',
    color: 'info',
  },
  {
    title: 'Đối soát sản phẩm',
    description: 'Kiểm tra SKU, đơn vị tính và giá bán trước khi bán.',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
]

const loadWarehouseWorkspace = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [
      products,
      lowStock,
      receipts,
      suppliers,
    ] = await Promise.all([
      getProducts({ pageSize: 1 }),
      getInventoryStock({ belowMin: true }),
      getGoodsReceipts({ pageSize: 1 }),
      getSuppliers({ pageSize: 1 }),
    ])

    productCount.value = products.totalCount
    lowStockCount.value = lowStock.length
    lowStockItems.value = lowStock.slice(0, 5)
    receiptCount.value = receipts.totalCount
    supplierCount.value = suppliers.totalCount
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Không thể tải dữ liệu workspace kho.',
    )
  }
  finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadWarehouseWorkspace()
})
</script>

<template>
  <section class="warehouse-workspace-page">
    <div class="warehouse-hero">
      <div class="warehouse-hero__title-area">
        <h1>Thủ kho</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-archive-line" class="ml-2">
          Workspace kho hàng
        </VChip>
      </div>

      <div class="warehouse-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadWarehouseWorkspace">
          Tải lại
        </VBtn>

        <VBtn to="/inventory" color="primary" prepend-icon="ri-archive-line" class="primary-action">
          Xem tồn kho
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
            <VChip :color="lowStockCount > 0 ? 'warning' : 'success'" variant="tonal" prepend-icon="ri-store-2-line"
              class="mb-4">
              Điều phối kho
            </VChip>

            <h2>Theo dõi tồn kho và nhập hàng từ một màn hình.</h2>

            <p>
              Các cảnh báo tồn thấp, phiếu nhập, sản phẩm và nhà cung cấp được gom vào workspace để thủ kho xử lý nhanh
              các việc ưu tiên.
            </p>
          </div>

          <div class="stock-priority-card">
            <div class="stock-priority-card__head">
              <span>SKU cần kiểm tra</span>

              <VChip :color="lowStockCount > 0 ? 'warning' : 'success'" variant="tonal" size="small">
                {{ lowStockCount > 0 ? 'Cần xử lý' : 'An toàn' }}
              </VChip>
            </div>

            <strong>{{ formatNumber(lowStockCount) }}</strong>

            <p>
              {{ lowStockCount > 0 ? 'Có SKU dưới ngưỡng tồn tối thiểu.' : 'Chưa có SKU nào dưới ngưỡng tối thiểu.' }}
            </p>

            <VBtn block color="primary" to="/inventory" prepend-icon="ri-arrow-right-line">
              Mở tồn kho
            </VBtn>
          </div>
        </div>
      </VCardText>
    </VCard>

    <div class="summary-grid">
      <article v-for="card in summaryCards" :key="card.label" class="summary-card">
        <template v-if="loading">
          <VSkeletonLoader type="list-item-avatar-two-line" />
        </template>

        <template v-else>
          <div class="summary-icon" :class="`summary-icon--${card.color}`">
            <VIcon :icon="card.icon" />
          </div>

          <div>
            <span>{{ card.label }}</span>
            <strong>{{ card.value }}</strong>
            <p>{{ card.helper }}</p>
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
        <VCard class="workspace-panel">
          <div class="panel-head">
            <div>
              <span>Stock alert</span>
              <strong>Cảnh báo tồn thấp</strong>
              <p>Danh sách SKU cần ưu tiên kiểm tra.</p>
            </div>

            <VIcon icon="ri-alert-line" :color="lowStockCount > 0 ? 'warning' : 'success'" size="28" />
          </div>

          <VDivider />

          <VCardText v-if="loading">
            <VSkeletonLoader type="list-item-avatar-three-line" />
          </VCardText>

          <template v-else>
            <VList v-if="lowStockItems.length" class="stock-alert-list">
              <VListItem v-for="stock in lowStockItems" :key="stock.productId">
                <template #prepend>
                  <VAvatar :color="stockStatusColor(stock)" variant="tonal" size="38">
                    <VIcon icon="ri-box-3-line" />
                  </VAvatar>
                </template>

                <VListItemTitle>
                  {{ stock.productName }}
                </VListItemTitle>

                <VListItemSubtitle>
                  {{ stock.productCode }} · {{ stock.unitName }}
                </VListItemSubtitle>

                <template #append>
                  <VChip :color="stockStatusColor(stock)" size="small" variant="tonal">
                    {{ stockStatusText(stock) }} · {{ formatNumber(stock.availableQuantity) }}
                  </VChip>
                </template>
              </VListItem>
            </VList>

            <div v-else class="workspace-empty">
              <VIcon icon="ri-shield-check-line" color="success" size="38" />

              <strong>Tồn kho đang an toàn</strong>

              <span>
                Chưa có SKU nào dưới ngưỡng tối thiểu.
              </span>
            </div>
          </template>
        </VCard>

        <VCard class="workspace-panel">
          <div class="panel-head compact">
            <div>
              <span>Quy trình</span>
              <strong>Quy trình ưu tiên</strong>
              <p>Các bước vận hành kho hằng ngày.</p>
            </div>
          </div>

          <div class="flow-list">
            <div v-for="item in warehouseFlow" :key="item.title">
              <VAvatar :color="item.color" variant="tonal" size="36">
                <VIcon :icon="item.icon" />
              </VAvatar>

              <div>
                <strong>{{ item.title }}</strong>
                <span>{{ item.description }}</span>
              </div>
            </div>
          </div>
        </VCard>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.warehouse-workspace-page {
  position: relative;
  isolation: isolate;
}

.warehouse-workspace-page::before {
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

.stock-priority-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.52);
}

.stock-priority-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-block-end: 0.75rem;
}

.stock-priority-card__head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
  font-weight: 800;
}

.stock-priority-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 2rem;
  font-weight: 950;
  letter-spacing: -0.05em;
}

.stock-priority-card p {
  margin: 0.4rem 0 1rem;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.88rem;
}

.stock-priority-card .v-btn {
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

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
}

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--info {
  background: rgb(var(--v-theme-info));
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
  grid-template-columns: minmax(0, 1fr) 380px;
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

.stock-alert-list {
  padding-block: 0.4rem;
}

.stock-alert-list :deep(.v-list-item) {
  min-block-size: 72px;
}

.stock-alert-list :deep(.v-list-item-title) {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.stock-alert-list :deep(.v-list-item-subtitle) {
  color: rgba(var(--v-theme-on-surface), 0.56);
}

.workspace-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 220px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.workspace-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.flow-list {
  display: grid;
  gap: 0.95rem;
  padding: 0 1.25rem 1.25rem;
}

.flow-list>div {
  display: grid;
  align-items: start;
  gap: 0.75rem;
  grid-template-columns: 36px minmax(0, 1fr);
}

.flow-list strong,
.flow-list span {
  display: block;
}

.flow-list strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.flow-list span {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
  line-height: 1.45;
}@media (max-width: 1200px) {

  .focus-layout,
  .workspace-layout {
    grid-template-columns: 1fr;
  }

  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .action-grid {
    grid-template-columns: 1fr;
  }

  .panel-head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.warehouse-hero {
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

.warehouse-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.warehouse-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.warehouse-hero__copy {
  display: none !important;
}

.warehouse-hero__actions,
.warehouse-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.warehouse-hero__actions .v-btn,
.warehouse-actions .v-btn,
.warehouse-hero__actions .v-btn.primary-action,
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