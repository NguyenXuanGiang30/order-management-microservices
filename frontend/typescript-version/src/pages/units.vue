<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import {
  type ProductDto,
  type UnitConversionDto,
  type UnitDto,
  createUnit,
  createUnitConversion,
  deleteUnitConversion,
  getProducts,
  getUnitConversions,
  getUnits,
} from '@/services/productInventoryApi'

type TabKey = 'units' | 'conversions'

const currentTab = ref<TabKey>('units')

const units = ref<UnitDto[]>([])
const conversions = ref<UnitConversionDto[]>([])

const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const createDialog = ref(false)
const unitForm = ref({
  name: '',
  abbreviation: '',
})

const unitFormError = ref('')

const conversionDialog = ref(false)
const conversionLoading = ref(false)
const conversionFormError = ref('')

const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])
const selectedProduct = ref<ProductDto | null>(null)

const conversionForm = ref({
  fromUnitId: '',
  toUnitId: '',
  factor: 1,
})

const tabItems = [
  {
    value: 'units' as TabKey,
    title: 'Đơn vị tính',
    icon: 'ri-ruler-2-line',
  },
  {
    value: 'conversions' as TabKey,
    title: 'Quy đổi đơn vị',
    icon: 'ri-shuffle-line',
  },
]

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const unitCount = computed(() => units.value.length)

const activeUnitCount = computed(() =>
  units.value.filter(unit => unit.isActive).length,
)

const inactiveUnitCount = computed(() =>
  units.value.filter(unit => !unit.isActive).length,
)

const conversionCount = computed(() => conversions.value.length)

const selectedFromUnit = computed(() =>
  units.value.find(unit => unit.id === conversionForm.value.fromUnitId) || null,
)

const selectedToUnit = computed(() =>
  units.value.find(unit => unit.id === conversionForm.value.toUnitId) || null,
)

const conversionPreview = computed(() => {
  if (!selectedFromUnit.value || !selectedToUnit.value)
    return 'Chưa chọn đủ đơn vị quy đổi'

  return `1 ${selectedFromUnit.value.name} = ${formatNumber(conversionForm.value.factor)} ${selectedToUnit.value.name}`
})

const summaryCards = computed(() => [
  {
    label: 'Đơn vị tính',
    value: formatNumber(unitCount.value),
    helper: `${formatNumber(activeUnitCount.value)} đang hoạt động · ${formatNumber(inactiveUnitCount.value)} ngừng dùng`,
    icon: 'ri-ruler-2-line',
    color: 'primary',
  },
  {
    label: 'Quy tắc quy đổi',
    value: formatNumber(conversionCount.value),
    helper: 'Quy đổi theo từng sản phẩm',
    icon: 'ri-shuffle-line',
    color: 'success',
  },
  {
    label: 'Sản phẩm đã chọn',
    value: selectedProduct.value ? selectedProduct.value.code : '—',
    helper: selectedProduct.value?.name || 'Chọn sản phẩm để tạo quy đổi',
    icon: 'ri-box-3-line',
    color: 'warning',
  },
])

const loadUnits = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    units.value = await getUnits()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách đơn vị tính.')
  }
  finally {
    loading.value = false
  }
}

const loadConversions = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    conversions.value = await getUnitConversions()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách quy đổi đơn vị.')
  }
  finally {
    loading.value = false
  }
}

const loadCurrentTab = async () => {
  if (currentTab.value === 'units') {
    await loadUnits()
    return
  }

  await Promise.all([
    loadUnits(),
    loadConversions(),
  ])
}

const openCreateDialog = () => {
  unitForm.value = {
    name: '',
    abbreviation: '',
  }

  unitFormError.value = ''
  createDialog.value = true
}

const validateUnitForm = () => {
  if (!unitForm.value.name.trim())
    return 'Vui lòng nhập tên đơn vị tính.'

  return ''
}

const handleCreateUnit = async () => {
  const validationMessage = validateUnitForm()

  if (validationMessage) {
    unitFormError.value = validationMessage
    return
  }

  loading.value = true
  unitFormError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createUnit({
      name: unitForm.value.name.trim(),
      abbreviation: unitForm.value.abbreviation.trim() || null,
    })

    createDialog.value = false
    successMessage.value = 'Tạo đơn vị tính thành công.'

    await loadUnits()
  }
  catch (error) {
    unitFormError.value = getErrorMessage(error, 'Lỗi khi tạo đơn vị tính.')
  }
  finally {
    loading.value = false
  }
}

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []
    return
  }

  conversionLoading.value = true
  conversionFormError.value = ''

  try {
    const result = await getProducts({
      search: searchProductQuery.value.trim(),
      pageSize: 10,
    })

    searchedProducts.value = result.items
  }
  catch (error) {
    conversionFormError.value = getErrorMessage(error, 'Không thể tìm sản phẩm.')
  }
  finally {
    conversionLoading.value = false
  }
}

const clearProductSearch = () => {
  searchProductQuery.value = ''
  searchedProducts.value = []
}

const selectProduct = (product: ProductDto) => {
  selectedProduct.value = product
  clearProductSearch()
}

const openConversionDialog = async () => {
  selectedProduct.value = null
  searchProductQuery.value = ''
  searchedProducts.value = []

  conversionForm.value = {
    fromUnitId: '',
    toUnitId: '',
    factor: 1,
  }

  conversionFormError.value = ''

  if (!units.value.length)
    await loadUnits()

  conversionDialog.value = true
}

const validateConversionForm = () => {
  if (!selectedProduct.value)
    return 'Vui lòng chọn sản phẩm.'

  if (!conversionForm.value.fromUnitId)
    return 'Vui lòng chọn đơn vị gốc.'

  if (!conversionForm.value.toUnitId)
    return 'Vui lòng chọn đơn vị sau quy đổi.'

  if (conversionForm.value.fromUnitId === conversionForm.value.toUnitId)
    return 'Đơn vị gốc và đơn vị sau quy đổi không được trùng nhau.'

  if (!conversionForm.value.factor || conversionForm.value.factor <= 0)
    return 'Tỷ lệ quy đổi phải lớn hơn 0.'

  return ''
}

const handleCreateConversion = async () => {
  const validationMessage = validateConversionForm()

  if (validationMessage) {
    conversionFormError.value = validationMessage
    return
  }

  if (!selectedProduct.value)
    return

  loading.value = true
  conversionFormError.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createUnitConversion({
      productId: selectedProduct.value.id,
      fromUnitId: conversionForm.value.fromUnitId,
      toUnitId: conversionForm.value.toUnitId,
      factor: Number(conversionForm.value.factor),
    })

    conversionDialog.value = false
    successMessage.value = 'Tạo quy đổi đơn vị thành công.'

    await loadConversions()
  }
  catch (error) {
    conversionFormError.value = getErrorMessage(error, 'Lỗi khi tạo quy đổi đơn vị.')
  }
  finally {
    loading.value = false
  }
}

const handleDeleteConversion = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa quy đổi đơn vị tính này?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await deleteUnitConversion(id)

    successMessage.value = 'Xóa quy đổi đơn vị thành công.'

    await loadConversions()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi khi xóa quy đổi đơn vị.')
  }
  finally {
    loading.value = false
  }
}

watch(currentTab, () => {
  void loadCurrentTab()
})

onMounted(async () => {
  await loadUnits()
})
</script>

<template>
  <section class="unit-page">
    <div class="unit-hero">
      <div class="unit-hero__title-area">
        <h1>Đơn vị & Quy đổi</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-ruler-2-line" class="ml-2">
          Danh mục hàng hóa
        </VChip>
      </div>

      <div class="unit-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadCurrentTab">
          Tải lại
        </VBtn>

        <VBtn v-if="currentTab === 'units'" color="primary" prepend-icon="ri-add-line" class="primary-action"
          @click="openCreateDialog">
          Thêm đơn vị tính
        </VBtn>

        <VBtn v-else color="primary" prepend-icon="ri-shuffle-line" class="primary-action"
          @click="openConversionDialog">
          Thêm quy đổi
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

    <VCard class="tabs-card">
      <div class="unit-tabs">
        <button v-for="tab in tabItems" :key="tab.value" type="button" class="unit-tab"
          :class="{ 'unit-tab--active': currentTab === tab.value }" @click="currentTab = tab.value">
          <VIcon :icon="tab.icon" />
          <span>{{ tab.title }}</span>
        </button>
      </div>
    </VCard>

    <VWindow v-model="currentTab">
      <VWindowItem value="units">
        <div class="unit-layout">
          <VCard class="unit-panel">
            <div class="panel-head">
              <div>
                <span>Đơn vị tính</span>
                <strong>Danh sách đơn vị tính</strong>
                <p>Đơn vị dùng cho sản phẩm, nhập kho, xuất kho và bán hàng tại POS.</p>
              </div>

              <VChip color="primary" variant="tonal">
                {{ formatNumber(unitCount) }} đơn vị
              </VChip>
            </div>

            <VDivider />

            <VCardText v-if="loading">
              <VSkeletonLoader type="table-heading, table-tbody" />
            </VCardText>

            <template v-else>
              <div v-if="units.length" class="unit-table-wrap">
                <VTable class="unit-table">
                  <thead>
                    <tr>
                      <th>Tên đơn vị tính</th>
                      <th>Ký hiệu viết tắt</th>
                      <th class="text-center">Trạng thái</th>
                    </tr>
                  </thead>

                  <tbody>
                    <tr v-for="unit in units" :key="unit.id" class="unit-row">
                      <td>
                        <div class="unit-name-cell">
                          <VAvatar size="36" color="primary" variant="tonal">
                            <VIcon icon="ri-ruler-2-line" />
                          </VAvatar>

                          <strong>{{ unit.name }}</strong>
                        </div>
                      </td>

                      <td>
                        <VChip size="small" variant="outlined">
                          {{ unit.abbreviation || '—' }}
                        </VChip>
                      </td>

                      <td class="text-center">
                        <VChip :color="unit.isActive ? 'success' : 'secondary'" variant="tonal" size="small">
                          {{ unit.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động' }}
                        </VChip>
                      </td>
                    </tr>
                  </tbody>
                </VTable>
              </div>

              <div v-else class="unit-empty">
                <VIcon icon="ri-ruler-2-line" size="42" color="primary" />

                <strong>Chưa có đơn vị tính nào</strong>
                <span>Hãy tạo đơn vị tính đầu tiên để sử dụng cho sản phẩm.</span>

                <VBtn color="primary" prepend-icon="ri-add-line" @click="openCreateDialog">
                  Thêm đơn vị tính
                </VBtn>
              </div>
            </template>
          </VCard>

          <aside class="unit-aside">
            <VCard class="unit-panel guide-card">
              <div class="aside-head">
                <span>Gợi ý</span>
                <strong>Vai trò của đơn vị tính</strong>
              </div>

              <div class="guide-list">
                <div>
                  <VIcon color="primary" icon="ri-information-line" />
                  <span>Đơn vị tính dùng để đếm sản phẩm khi nhập hàng, kiểm kho và bán hàng.</span>
                </div>

                <div>
                  <VIcon color="success" icon="ri-checkbox-circle-line" />
                  <span>Mỗi sản phẩm nên có một đơn vị cơ bản như chai, lon, hộp hoặc kg.</span>
                </div>

                <div>
                  <VIcon color="warning" icon="ri-shuffle-line" />
                  <span>Dùng quy đổi khi nhập thùng nhưng bán lẻ theo lon hoặc chai.</span>
                </div>
              </div>
            </VCard>
          </aside>
        </div>
      </VWindowItem>

      <VWindowItem value="conversions">
        <div class="unit-layout">
          <VCard class="unit-panel">
            <div class="panel-head">
              <div>
                <span>Quy đổi</span>
                <strong>Quy đổi đơn vị</strong>
                <p>Thiết lập công thức quy đổi riêng cho từng sản phẩm.</p>
              </div>

              <VChip color="success" variant="tonal">
                {{ formatNumber(conversionCount) }} quy tắc
              </VChip>
            </div>

            <VDivider />

            <VCardText v-if="loading">
              <VSkeletonLoader type="table-heading, table-tbody" />
            </VCardText>

            <template v-else>
              <div v-if="conversions.length" class="unit-table-wrap">
                <VTable class="unit-table">
                  <thead>
                    <tr>
                      <th>Sản phẩm áp dụng</th>
                      <th>Quy đổi</th>
                      <th class="text-center">Hành động</th>
                    </tr>
                  </thead>

                  <tbody>
                    <tr v-for="conversion in conversions" :key="conversion.id" class="unit-row">
                      <td>
                        <div class="unit-name-cell">
                          <VAvatar size="36" color="success" variant="tonal">
                            <VIcon icon="ri-box-3-line" />
                          </VAvatar>

                          <strong>{{ conversion.productName }}</strong>
                        </div>
                      </td>

                      <td>
                        <div class="conversion-formula">
                          1 {{ conversion.fromUnitName }}
                          =
                          {{ formatNumber(conversion.factor) }}
                          {{ conversion.toUnitName }}
                        </div>
                      </td>

                      <td class="text-center">
                        <VBtn icon="ri-delete-bin-line" variant="text" color="error" size="small" :loading="loading"
                          @click="handleDeleteConversion(conversion.id)" />
                      </td>
                    </tr>
                  </tbody>
                </VTable>
              </div>

              <div v-else class="unit-empty">
                <VIcon icon="ri-shuffle-line" size="42" color="primary" />

                <strong>Chưa có quy tắc quy đổi</strong>
                <span>Tạo quy đổi để nhập hàng theo thùng và bán lẻ theo đơn vị nhỏ hơn.</span>

                <VBtn color="primary" prepend-icon="ri-shuffle-line" @click="openConversionDialog">
                  Thêm quy đổi
                </VBtn>
              </div>
            </template>
          </VCard>

          <aside class="unit-aside">
            <VCard class="unit-panel guide-card">
              <div class="aside-head">
                <span>Lợi ích</span>
                <strong>Quy đổi đơn vị</strong>
              </div>

              <div class="guide-list">
                <div>
                  <VIcon color="primary" icon="ri-checkbox-circle-line" />
                  <span>Nhập hàng bằng thùng, tự động quy đổi sang đơn vị bán lẻ.</span>
                </div>

                <div>
                  <VIcon color="success" icon="ri-store-2-line" />
                  <span>Giúp POS bán đúng đơn vị nhỏ như lon, chai hoặc gói.</span>
                </div>

                <div>
                  <VIcon color="warning" icon="ri-error-warning-line" />
                  <span>Giảm sai lệch tồn kho khi sản phẩm có nhiều quy cách đóng gói.</span>
                </div>
              </div>
            </VCard>
          </aside>
        </div>
      </VWindowItem>
    </VWindow>

    <VDialog v-model="createDialog" max-width="520" persistent>
      <VCard class="unit-dialog">
        <div class="dialog-head">
          <div>
            <span>Thêm mới</span>
            <h2>Thêm đơn vị tính</h2>
          </div>

          <VChip color="primary" variant="tonal">
            Đơn vị
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="unitFormError" type="error" variant="tonal" class="mb-4">
            {{ unitFormError }}
          </VAlert>

          <VTextField v-model="unitForm.name" label="Tên đơn vị tính *" placeholder="Ví dụ: Chai, Lon, Hộp, Kilogam..."
            density="comfortable" required class="mb-4" />

          <VTextField v-model="unitForm.abbreviation" label="Ký hiệu viết tắt" placeholder="Ví dụ: kg, l, c..."
            density="comfortable" />
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="loading" @click="createDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleCreateUnit">
            Lưu đơn vị
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="conversionDialog" max-width="700" persistent>
      <VCard class="unit-dialog">
        <div class="dialog-head">
          <div>
            <span>Quy đổi</span>
            <h2>Thêm quy đổi đơn vị</h2>
          </div>

          <VChip color="success" variant="tonal">
            {{ selectedProduct?.code || 'Chưa chọn SKU' }}
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="conversionFormError" type="error" variant="tonal" class="mb-4">
            {{ conversionFormError }}
          </VAlert>

          <div class="product-search-box">
            <VTextField v-model="searchProductQuery" label="Tìm sản phẩm *"
              placeholder="Nhập tên, mã sản phẩm hoặc barcode..." prepend-inner-icon="ri-search-line"
              density="comfortable" hide-details clearable @keyup.enter="searchProducts"
              @click:clear="clearProductSearch" />

            <VBtn color="primary" prepend-icon="ri-search-line" :loading="conversionLoading" @click="searchProducts">
              Tìm
            </VBtn>
          </div>

          <div v-if="searchedProducts.length" class="product-result-list">
            <button v-for="product in searchedProducts" :key="product.id" type="button" class="product-result-item"
              @click="selectProduct(product)">
              <span>
                <strong>{{ product.name }}</strong>
                <small>
                  {{ product.code }} · Đơn vị cơ bản: {{ product.unitName }}
                </small>
              </span>

              <VIcon icon="ri-add-line" />
            </button>
          </div>

          <div v-if="selectedProduct" class="selected-product-box">
            <VIcon icon="ri-checkbox-circle-line" color="success" />

            <div>
              <strong>{{ selectedProduct.name }}</strong>
              <span>
                SKU: {{ selectedProduct.code }} · Đơn vị hiện tại:
                {{ selectedProduct.unitName }}
              </span>
            </div>
          </div>

          <div class="conversion-grid">
            <VSelect v-model="conversionForm.fromUnitId" :items="units" item-title="name" item-value="id"
              label="Đơn vị gốc *" placeholder="VD: Thùng" density="comfortable" />

            <VSelect v-model="conversionForm.toUnitId" :items="units" item-title="name" item-value="id"
              label="Đơn vị sau quy đổi *" placeholder="VD: Lon" density="comfortable" />

            <VTextField v-model.number="conversionForm.factor" type="number" min="0" label="Tỷ lệ quy đổi *"
              placeholder="VD: 24" density="comfortable" class="span-2" />
          </div>

          <div class="conversion-preview">
            <span>Công thức quy đổi</span>
            <strong>{{ conversionPreview }}</strong>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" :disabled="loading" @click="conversionDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleCreateConversion">
            Lưu quy đổi
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.unit-page {
  position: relative;
  isolation: isolate;
}

.unit-page::before {
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
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.summary-card,
.tabs-card,
.unit-panel,
.unit-dialog {
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

.tabs-card {
  padding: 0.65rem;
  margin-block-end: 1rem;
}

.unit-tabs {
  display: grid;
  gap: 0.5rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.unit-tab {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  min-block-size: 46px;
  border: 0;
  border-radius: 16px;
  color: rgba(var(--v-theme-on-surface), 0.66);
  background: transparent;
  font: inherit;
  font-weight: 850;
  cursor: pointer;
}

.unit-tab:hover,
.unit-tab--active {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.09);
}

.unit-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 340px;
}

.panel-head,
.aside-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
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
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.22rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.panel-head p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.unit-table-wrap {
  overflow-x: auto;
}

.unit-table {
  min-inline-size: 720px;
}

.unit-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.unit-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.unit-row {
  transition: background 160ms ease;
}

.unit-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.unit-name-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
}

.unit-name-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.conversion-formula {
  color: rgb(var(--v-theme-primary));
  font-weight: 950;
}

.unit-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.unit-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.unit-aside {
  display: grid;
  gap: 1rem;
}

.guide-card {
  position: sticky;
  inset-block-start: 84px;
}

.guide-list {
  display: grid;
  gap: 0.9rem;
  padding: 0 1.25rem 1.25rem;
}

.guide-list>div {
  display: grid;
  align-items: start;
  gap: 0.65rem;
  grid-template-columns: 22px minmax(0, 1fr);
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.5;
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

.unit-dialog :deep(.v-field) {
  border-radius: 16px;
}

.product-search-box {
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: minmax(0, 1fr) auto;
  margin-block-end: 1rem;
}

.product-search-box .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.product-result-list {
  display: grid;
  gap: 0.55rem;
  max-block-size: 220px;
  overflow-y: auto;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.65rem;
  margin-block-end: 1rem;
  background: rgb(var(--v-theme-surface));
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

.selected-product-box {
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: 28px minmax(0, 1fr);
  border: 1px solid rgba(var(--v-theme-success), 0.22);
  border-radius: 18px;
  padding: 0.9rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-success), 0.08);
}

.selected-product-box strong,
.selected-product-box span {
  display: block;
}

.selected-product-box strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.selected-product-box span {
  color: rgba(var(--v-theme-on-surface), 0.6);
  font-size: 0.86rem;
}

.conversion-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.span-2 {
  grid-column: span 2;
}

.conversion-preview {
  border: 1px solid rgba(var(--v-theme-primary), 0.18);
  border-radius: 18px;
  padding: 0.9rem;
  margin-block-start: 1rem;
  background: rgba(var(--v-theme-primary), 0.07);
}

.conversion-preview span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.conversion-preview strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-primary));
  font-size: 1rem;
  font-weight: 950;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .unit-layout {
    grid-template-columns: 1fr;
  }

  .guide-card {
    position: static;
  }

  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .unit-tabs,
  .product-search-box,
  .conversion-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: span 1;
  }

  .panel-head,
  .aside-head,
  .dialog-head {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.unit-hero {
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

.unit-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.unit-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.unit-hero__copy {
  display: none !important;
}

.unit-hero__actions,
.unit-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.unit-hero__actions .v-btn,
.unit-actions .v-btn,
.unit-hero__actions .v-btn.primary-action,
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