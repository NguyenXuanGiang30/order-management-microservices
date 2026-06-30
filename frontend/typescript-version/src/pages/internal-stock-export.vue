<script setup lang="ts">
import { computed, ref } from 'vue'

import {
  type ExportItemDto,
  type ProductDto,
  getProducts,
  internalStockExport,
} from '@/services/productInventoryApi'
import { readAuthSession } from '@/stores/authSession'

interface ExportCartItem {
  productId: string
  productCode: string
  productName: string
  unitName: string
  currentQty: number
  quantity: number
  note: string
}

const cart = ref<ExportCartItem[]>([])
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])
const searchLoading = ref(false)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const totalSku = computed(() => cart.value.length)

const totalExportQuantity = computed(() =>
  cart.value.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0),
)

const invalidItems = computed(() =>
  cart.value.filter(item =>
    !item.quantity
    || item.quantity <= 0
    || item.quantity > item.currentQty,
  ),
)

const canSubmit = computed(() =>
  cart.value.length > 0 && invalidItems.value.length === 0 && !loading.value,
)

const summaryCards = computed(() => [
  {
    label: 'SKU xuất kho',
    value: formatNumber(totalSku.value),
    helper: 'Số mặt hàng trong phiếu',
    icon: 'ri-price-tag-3-line',
    color: 'primary',
  },
  {
    label: 'Tổng số lượng xuất',
    value: formatNumber(totalExportQuantity.value),
    helper: 'Tổng đơn vị sẽ trừ khỏi tồn kho',
    icon: 'ri-upload-2-line',
    color: 'error',
  },
  {
    label: 'Dòng hợp lệ',
    value: formatNumber(cart.value.length - invalidItems.value.length),
    helper: `${formatNumber(invalidItems.value.length)} dòng cần kiểm tra`,
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
])

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []

    return
  }

  searchLoading.value = true
  errorMessage.value = ''

  try {
    const response = await getProducts({
      search: searchProductQuery.value.trim(),
      pageSize: 10,
    })

    searchedProducts.value = response.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tìm sản phẩm.'
  }
  finally {
    searchLoading.value = false
  }
}

const addToCart = (product: ProductDto) => {
  errorMessage.value = ''
  successMessage.value = ''

  if (product.quantityOnHand <= 0) {
    errorMessage.value = 'Sản phẩm đã hết hàng, không thể xuất kho.'

    return
  }

  const existing = cart.value.find(item => item.productId === product.id)

  if (existing) {
    if (existing.quantity >= product.quantityOnHand) {
      errorMessage.value = `Không thể xuất vượt quá tồn kho hiện có của ${product.name}.`

      return
    }

    existing.quantity += 1
  }
  else {
    cart.value.push({
      productId: product.id,
      productCode: product.code,
      productName: product.name,
      unitName: product.unitName,
      currentQty: product.quantityOnHand,
      quantity: 1,
      note: '',
    })
  }

  searchProductQuery.value = ''
  searchedProducts.value = []
}

const removeFromCart = (index: number) => {
  cart.value.splice(index, 1)
}

const increaseQuantity = (item: ExportCartItem) => {
  if (item.quantity >= item.currentQty) {
    errorMessage.value = `Không thể xuất vượt quá tồn kho hiện có của ${item.productName}.`

    return
  }

  item.quantity += 1
}

const decreaseQuantity = (item: ExportCartItem) => {
  item.quantity = Math.max(1, item.quantity - 1)
}

const fillMaxQuantity = (item: ExportCartItem) => {
  item.quantity = item.currentQty
}

const clearCart = () => {
  cart.value = []
  errorMessage.value = ''
  successMessage.value = ''
}

const validateBeforeSubmit = () => {
  if (cart.value.length === 0)
    return 'Vui lòng thêm ít nhất một sản phẩm vào danh sách xuất.'

  for (const item of cart.value) {
    if (item.quantity <= 0)
      return `Số lượng xuất cho ${item.productName} phải lớn hơn 0.`

    if (item.quantity > item.currentQty)
      return `Số lượng xuất cho ${item.productName} vượt quá tồn kho hiện có (${formatNumber(item.currentQty)} ${item.unitName}).`
  }

  return ''
}

const handleSubmitExport = async () => {
  const validationMessage = validateBeforeSubmit()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const session = readAuthSession()
    const userId = session?.user?.id || '00000000-0000-0000-0000-000000000000'
    const userName = session?.user?.fullName || 'Hệ thống'

    const items: ExportItemDto[] = cart.value.map(item => ({
      productId: item.productId,
      quantity: Number(item.quantity) || 0,
      note: item.note || null,
    }))

    await internalStockExport({
      createdBy: userId,
      createdByName: userName,
      items,
    } as any)

    cart.value = []
    successMessage.value = 'Xuất kho nội bộ thành công.'
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xuất kho.'
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="internal-export-page">
    <div class="export-hero">
      <div class="export-hero__title-area">
        <h1>Xuất kho nội bộ</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-upload-2-line" class="ml-2">
          Quản lý tồn kho
        </VChip>
      </div>

      <div class="export-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-delete-bin-line"
          :disabled="cart.length === 0 || loading" @click="clearCart">
          Xóa phiếu
        </VBtn>

        <VBtn color="error" prepend-icon="ri-checkbox-circle-line" class="danger-action" :loading="loading"
          :disabled="!canSubmit" @click="handleSubmitExport">
          Xác nhận xuất kho
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

    <div class="export-layout">
      <div class="export-main">
        <VCard class="export-panel search-panel">
          <VCardText>
            <div class="product-search-box">
              <VTextField v-model="searchProductQuery" label="Tìm sản phẩm xuất kho"
                placeholder="Nhập tên sản phẩm, SKU hoặc barcode..." prepend-inner-icon="ri-search-line" clearable
                density="comfortable" hide-details @keyup.enter="searchProducts" @click:clear="searchedProducts = []" />

              <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="searchLoading"
                @click="searchProducts">
                Tìm
              </VBtn>

              <div v-if="searchedProducts.length" class="product-result-list">
                <button v-for="product in searchedProducts" :key="product.id" type="button" class="product-result-item"
                  @click="addToCart(product)">
                  <span>
                    <strong>{{ product.name }}</strong>
                    <small>
                      {{ product.code }} · Tồn hiện tại:
                      {{ formatNumber(product.quantityOnHand ?? 0) }} {{ product.unitName }}
                    </small>
                  </span>

                  <VIcon icon="ri-add-line" />
                </button>
              </div>
            </div>
          </VCardText>
        </VCard>

        <VCard class="export-panel">
          <div class="panel-head">
            <div>
              <span>Phiếu xuất kho</span>
              <strong>Danh sách sản phẩm xuất</strong>
            </div>

            <VChip color="error" variant="tonal">
              {{ formatNumber(totalExportQuantity) }} sản phẩm
            </VChip>
          </div>

          <VDivider />

          <template v-if="cart.length">
            <div class="export-table-wrap">
              <VTable class="export-table">
                <thead>
                  <tr>
                    <th>Sản phẩm</th>
                    <th>Mã SKU</th>
                    <th class="text-end">Tồn kho</th>
                    <th class="text-center">Số lượng xuất</th>
                    <th>Lý do xuất</th>
                    <th class="text-center">Xóa</th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="(item, index) in cart" :key="item.productId" class="export-row">
                    <td>
                      <div class="product-cell">
                        <strong>{{ item.productName }}</strong>
                        <span>{{ item.unitName }}</span>
                      </div>
                    </td>

                    <td>
                      <div class="sku-cell">
                        {{ item.productCode }}
                      </div>
                    </td>

                    <td class="text-end font-weight-bold">
                      {{ formatNumber(item.currentQty) }} {{ item.unitName }}
                    </td>

                    <td>
                      <div class="quantity-control">
                        <VBtn icon="ri-subtract-line" size="x-small" variant="text" :disabled="item.quantity <= 1"
                          @click="decreaseQuantity(item)" />

                        <VTextField v-model.number="item.quantity" type="number" density="compact" hide-details min="1"
                          :max="item.currentQty" class="quantity-input" />

                        <VBtn icon="ri-add-line" size="x-small" variant="text"
                          :disabled="item.quantity >= item.currentQty" @click="increaseQuantity(item)" />
                      </div>

                      <button type="button" class="max-button" @click="fillMaxQuantity(item)">
                        Xuất tối đa
                      </button>
                    </td>

                    <td>
                      <VTextField v-model="item.note" placeholder="VD: Kiểm mẫu, hỏng hóc..." density="compact"
                        hide-details />
                    </td>

                    <td class="text-center">
                      <VBtn icon="ri-delete-bin-line" variant="text" color="error" size="small"
                        @click="removeFromCart(index)" />
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </div>
          </template>

          <div v-else class="export-empty">
            <VIcon size="46" icon="ri-shopping-cart-line" color="primary" />
            <strong>Chưa có sản phẩm nào được chọn</strong>
            <span>Tìm sản phẩm ở phía trên rồi thêm vào phiếu xuất kho.</span>
          </div>
        </VCard>
      </div>

      <aside class="export-aside">
        <VCard class="export-panel sticky-panel">
          <div class="aside-head">
            <span>Thông tin phiếu</span>
            <strong>Tóm tắt xuất kho</strong>
          </div>

          <div class="summary-list">
            <div>
              <span>Số lượng SKU xuất</span>
              <strong>{{ formatNumber(totalSku) }}</strong>
            </div>

            <div>
              <span>Tổng sản phẩm xuất</span>
              <strong>{{ formatNumber(totalExportQuantity) }}</strong>
            </div>

            <div>
              <span>Dòng cần kiểm tra</span>
              <strong :class="invalidItems.length ? 'text-error' : 'text-success'">
                {{ formatNumber(invalidItems.length) }}
              </strong>
            </div>
          </div>

          <VBtn block color="error" size="large" prepend-icon="ri-checkbox-circle-line" class="submit-button"
            :loading="loading" :disabled="!canSubmit" @click="handleSubmitExport">
            Xác nhận xuất kho
          </VBtn>
        </VCard>

        <VCard class="export-panel rules-card">
          <div class="aside-head">
            <span>Lưu ý nghiệp vụ</span>
            <strong>Quy định xuất kho</strong>
          </div>

          <div class="rule-list">
            <div>
              <VIcon icon="ri-checkbox-circle-line" color="success" />
              <span>Mỗi sản phẩm xuất kho nội bộ sẽ giảm trực tiếp số lượng tồn kho thực tế.</span>
            </div>

            <div>
              <VIcon icon="ri-history-line" color="primary" />
              <span>Giao dịch được ghi nhận vào lịch sử biến động kho với loại xuất kho.</span>
            </div>

            <div>
              <VIcon icon="ri-file-list-3-line" color="warning" />
              <span>Nên nhập lý do xuất kho để phục vụ báo cáo và đối soát cuối tháng.</span>
            </div>
          </div>
        </VCard>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.internal-export-page {
  position: relative;
  isolation: isolate;
}

.internal-export-page::before {
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

.danger-action {
  color: white !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-error), 0.24);
}

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
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

.export-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 360px;
}

.export-main {
  display: grid;
  gap: 1rem;
}

.export-panel {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: visible;
}

.product-search-box {
  position: relative;
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: minmax(0, 1fr) auto;
}

.product-search-box :deep(.v-field),
.export-table :deep(.v-field) {
  border-radius: 16px;
}

.product-search-box .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.product-result-list {
  position: absolute;
  inset-block-start: calc(100% + 0.45rem);
  inset-inline: 0;
  z-index: 20;
  display: grid;
  gap: 0.55rem;
  max-block-size: 260px;
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

.panel-head,
.aside-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.1rem 1.25rem;
}

.panel-head span,
.aside-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.panel-head strong,
.aside-head strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.2rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.export-table-wrap {
  overflow-x: auto;
}

.export-table {
  min-inline-size: 920px;
}

.export-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.export-table :deep(td) {
  block-size: 70px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.export-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
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

.quantity-control {
  display: grid;
  align-items: center;
  gap: 0.35rem;
  grid-template-columns: 30px 86px 30px;
  justify-content: center;
}

.quantity-input :deep(input) {
  text-align: center;
}

.max-button {
  display: block;
  border: 0;
  margin: 0.35rem auto 0;
  color: rgb(var(--v-theme-primary));
  background: transparent;
  font-size: 0.75rem;
  font-weight: 800;
  cursor: pointer;
}

.export-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 300px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.export-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.export-aside {
  display: grid;
  gap: 1rem;
}

.sticky-panel {
  position: sticky;
  inset-block-start: 84px;
}

.summary-list {
  display: grid;
  gap: 0.75rem;
  padding: 0 1.25rem 1.25rem;
}

.summary-list>div {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.1);
  padding-block: 0.7rem;
}

.summary-list span {
  color: rgba(var(--v-theme-on-surface), 0.62);
}

.summary-list strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 950;
}

.submit-button {
  margin: 0 1.25rem 1.25rem;
  border-radius: 14px;
  color: white !important;
  font-weight: 900;
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
  .export-layout {
    grid-template-columns: 1fr;
  }

  .sticky-panel {
    position: static;
  }
}@media (max-width: 900px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }

  .product-search-box {
    grid-template-columns: 1fr;
  }

  .product-search-box .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .panel-head,
  .aside-head {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.export-hero {
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

.export-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.export-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.export-hero__copy {
  display: none !important;
}

.export-hero__actions,
.export-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.export-hero__actions .v-btn,
.export-actions .v-btn,
.export-hero__actions .v-btn.primary-action,
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