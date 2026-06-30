<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import { getApiBaseUrl } from '@/services/authApi'

import {
  type CategoryDto,
  type ProductDto,
  type ProductPriceHistoryDto,
  type UnitDto,
  createCategory,
  createProduct,
  deleteCategory,
  getCategories,
  getProductPriceHistory,
  getProducts,
  getUnits,
  toggleProductActive,
  updateProduct,
  uploadProductImage,
} from '@/services/productInventoryApi'

const products = ref<ProductDto[]>([])
const categories = ref<CategoryDto[]>([])
const availableUnits = ref<UnitDto[]>([])

const search = ref('')
const selectedCategory = ref('Tất cả')
const totalCount = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)

const productDialog = ref(false)
const categoryDialog = ref(false)
const isEditing = ref(false)
const activeTab = ref('general')

const imageFileInput = ref<HTMLInputElement | null>(null)
const priceHistory = ref<ProductPriceHistoryDto[]>([])

const productForm = ref({
  id: '',
  code: '',
  name: '',
  description: '',
  barcode: '',
  importPrice: 0,
  sellPrice: 0,
  weight: 0,
  categoryId: '',
  unitId: '',
  imageUrl: '',
})

const categoryForm = ref({
  id: '',
  name: '',
  parentId: null as string | null,
})

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatDateTime = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? 'Không xác định'
    : date.toLocaleString('vi-VN')
}

const resolveImageUrl = (path: string | null) => {
  if (!path)
    return ''

  if (path.startsWith('http'))
    return path

  return `${getApiBaseUrl()}${path}`
}

const categoryItems = computed(() => [
  { title: 'Tất cả', value: 'Tất cả' },
  ...categories.value
    .filter(category => category.name)
    .map(category => ({
      title: category.name,
      value: category.name,
    })),
])

const selectedCategoryId = computed(() => {
  if (selectedCategory.value === 'Tất cả')
    return undefined

  return categories.value.find(category => category.name === selectedCategory.value)?.id
})

const getAvailableQuantity = (product: ProductDto) =>
  Math.max((product.quantityOnHand ?? 0) - (product.quantityReserved ?? 0), 0)

const stockStatus = (product: ProductDto) => {
  const available = getAvailableQuantity(product)

  if (available <= 0)
    return 'Hết hàng'

  if (available <= 5)
    return 'Tồn thấp'

  return 'Tốt'
}

const stockColor = (product: ProductDto) => {
  const available = getAvailableQuantity(product)

  if (available <= 0)
    return 'error'

  if (available <= 5)
    return 'warning'

  return 'success'
}

const activeProducts = computed(() =>
  products.value.filter(product => product.isActive).length,
)

const lowStockProducts = computed(() =>
  products.value.filter(product => getAvailableQuantity(product) > 0 && getAvailableQuantity(product) <= 5).length,
)

const outOfStockProducts = computed(() =>
  products.value.filter(product => getAvailableQuantity(product) <= 0).length,
)

const inventoryValue = computed(() =>
  products.value.reduce((sum, product) => sum + ((product.sellPrice ?? 0) * getAvailableQuantity(product)), 0),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng SKU',
    value: formatNumber(totalCount.value),
    helper: `${formatNumber(activeProducts.value)} sản phẩm đang bán trên trang này`,
    icon: 'ri-archive-line',
    color: 'primary',
  },
  {
    label: 'Tồn thấp',
    value: formatNumber(lowStockProducts.value),
    helper: 'Cần theo dõi để nhập thêm hàng',
    icon: 'ri-alert-line',
    color: 'warning',
  },
  {
    label: 'Hết hàng',
    value: formatNumber(outOfStockProducts.value),
    helper: 'Không còn số lượng khả dụng',
    icon: 'ri-close-circle-line',
    color: 'error',
  },
  {
    label: 'Giá trị tồn trang này',
    value: formatCurrency(inventoryValue.value),
    helper: 'Tính theo giá bán và tồn khả dụng',
    icon: 'ri-money-dollar-circle-line',
    color: 'success',
  },
])

const productActions = (product: ProductDto): ActionMenuItem[] => [
  {
    label: 'Chỉnh sửa',
    icon: 'ri-edit-line',
    color: 'primary',
    handler: () => openEditProduct(product),
  },
  {
    label: product.isActive ? 'Ngừng bán' : 'Bán lại',
    icon: product.isActive ? 'ri-error-warning-line' : 'ri-checkbox-circle-line',
    color: product.isActive ? 'error' : 'success',
    handler: () => handleToggleActive(product),
  },
]

const loadCategories = async () => {
  try {
    categories.value = await getCategories()
  }
  catch (error) {
    console.error('Lỗi load danh mục:', error)
  }
}

const loadUnits = async () => {
  try {
    availableUnits.value = await getUnits()
  }
  catch (error) {
    console.error('Lỗi load đơn vị tính:', error)
  }
}

const loadProducts = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getProducts({
      search: search.value.trim(),
      categoryId: selectedCategoryId.value,
      pageNumber: page.value,
      pageSize: pageSize.value,
    })

    products.value = result.items
    totalCount.value = result.totalCount
    totalPages.value = result.totalPages
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách sản phẩm.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadProducts()
  else
    page.value = 1
}

const resetProductForm = () => {
  productForm.value = {
    id: '',
    code: '',
    name: '',
    description: '',
    barcode: '',
    importPrice: 0,
    sellPrice: 0,
    weight: 0,
    categoryId: categories.value[0]?.id || '',
    unitId: availableUnits.value[0]?.id || '',
    imageUrl: '',
  }
}

const openAddProduct = () => {
  isEditing.value = false
  resetProductForm()
  activeTab.value = 'general'
  priceHistory.value = []
  productDialog.value = true
}

const openEditProduct = (product: ProductDto) => {
  isEditing.value = true

  productForm.value = {
    id: product.id,
    code: product.code,
    name: product.name,
    description: product.description || '',
    barcode: product.barcode || '',
    importPrice: product.importPrice,
    sellPrice: product.sellPrice,
    weight: product.weight || 0,
    categoryId: product.categoryId,
    unitId: product.unitId,
    imageUrl: product.imageUrl || '',
  }

  activeTab.value = 'general'
  priceHistory.value = []

  void loadPriceHistory(product.id)

  productDialog.value = true
}

const handleSaveProduct = async () => {
  if (!productForm.value.name || !productForm.value.categoryId || !productForm.value.unitId) {
    errorMessage.value = 'Vui lòng nhập đầy đủ Tên sản phẩm, Danh mục và Đơn vị tính.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const payload = {
      code: productForm.value.code || undefined,
      name: productForm.value.name,
      description: productForm.value.description || null,
      barcode: productForm.value.barcode || null,
      importPrice: Number(productForm.value.importPrice) || 0,
      sellPrice: Number(productForm.value.sellPrice) || 0,
      imageUrl: productForm.value.imageUrl || null,
      weight: productForm.value.weight ? Number(productForm.value.weight) : null,
      categoryId: productForm.value.categoryId,
      unitId: productForm.value.unitId,
    }

    if (isEditing.value) {
      await updateProduct(productForm.value.id, payload)
      successMessage.value = 'Cập nhật sản phẩm thành công.'
    }
    else {
      await createProduct(payload)
      successMessage.value = 'Thêm sản phẩm mới thành công.'
    }

    productDialog.value = false
    await loadProducts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu sản phẩm.'
  }
  finally {
    loading.value = false
  }
}

const handleToggleActive = async (product: ProductDto) => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await toggleProductActive(product.id)
    successMessage.value = product.isActive
      ? 'Đã ngừng bán sản phẩm.'
      : 'Đã bật bán lại sản phẩm.'

    await loadProducts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi cập nhật trạng thái sản phẩm.'
  }
  finally {
    loading.value = false
  }
}

const handleAddCategory = async () => {
  if (!categoryForm.value.name.trim()) {
    errorMessage.value = 'Vui lòng nhập tên danh mục.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createCategory({
      name: categoryForm.value.name.trim(),
      parentId: categoryForm.value.parentId || null,
    })

    categoryForm.value.name = ''
    categoryForm.value.parentId = null
    successMessage.value = 'Thêm danh mục thành công.'

    await loadCategories()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu danh mục.'
  }
  finally {
    loading.value = false
  }
}

const handleDeleteCategory = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa danh mục này?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await deleteCategory(id)
    successMessage.value = 'Xóa danh mục thành công.'
    await loadCategories()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xóa danh mục.'
  }
  finally {
    loading.value = false
  }
}

const loadPriceHistory = async (productId: string) => {
  try {
    priceHistory.value = await getProductPriceHistory(productId)
  }
  catch (error) {
    console.error('Lỗi load lịch sử giá:', error)
  }
}

const triggerImageUpload = () => {
  imageFileInput.value?.click()
}

const onImageSelected = async (event: Event) => {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]

  if (!file)
    return

  if (!productForm.value.id) {
    errorMessage.value = 'Vui lòng lưu sản phẩm trước khi tải ảnh.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const newUrl = await uploadProductImage(productForm.value.id, file)

    productForm.value.imageUrl = newUrl
    successMessage.value = 'Tải ảnh sản phẩm thành công.'

    await loadProducts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi tải ảnh lên.'
  }
  finally {
    loading.value = false
    input.value = ''
  }
}

watch(page, () => {
  void loadProducts()
})

onMounted(async () => {
  await loadCategories()
  await loadUnits()
  await loadProducts()
})
</script>

<template>
  <section class="products-page">
    <div class="products-hero">
      <div class="products-hero__title-area">
        <h1>Quản lý sản phẩm</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-price-tag-3-line" class="ml-2">
          Danh mục hàng hóa
        </VChip>
      </div>

      <div class="products-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadProducts">
          Tải lại
        </VBtn>

        <VBtn variant="tonal" color="primary" prepend-icon="ri-folder-settings-line" @click="categoryDialog = true">
          Quản lý danh mục
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-add-line" class="primary-action" @click="openAddProduct">
          Thêm sản phẩm
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

    <VCard class="products-panel">
      <VCardText>
        <div class="products-toolbar">
          <VTextField v-model="search" label="Tìm sản phẩm" placeholder="Tên sản phẩm, SKU, barcode..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable class="products-search"
            @keyup.enter="resetAndLoad" />

          <VSelect v-model="selectedCategory" label="Danh mục" :items="categoryItems" item-title="title"
            item-value="value" density="comfortable" hide-details class="category-filter"
            @update:model-value="resetAndLoad" />

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
        <div v-if="products.length" class="products-table-wrap">
          <VTable class="products-table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Sản phẩm</th>
                <th>Barcode</th>
                <th>Danh mục</th>
                <th class="text-end">Giá nhập</th>
                <th class="text-end">Giá bán</th>
                <th class="text-center">Tồn khả dụng</th>
                <th>Trạng thái kho</th>
                <th>Kinh doanh</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="product in products" :key="product.id" class="product-row" @click="openEditProduct(product)">
                <td>
                  <div class="sku-cell">
                    {{ product.code }}
                  </div>
                </td>

                <td>
                  <div class="product-name-cell">
                    <VAvatar size="38" rounded="lg" class="product-avatar">
                      <VImg v-if="product.imageUrl" :src="resolveImageUrl(product.imageUrl)" cover />
                      <VIcon v-else icon="ri-price-tag-3-line" size="20" />
                    </VAvatar>

                    <div>
                      <strong>{{ product.name }}</strong>
                      <span>{{ product.unitName }} · {{ product.weight || 0 }}g</span>
                    </div>
                  </div>
                </td>

                <td>{{ product.barcode || '—' }}</td>
                <td>{{ product.categoryName || '—' }}</td>

                <td class="text-end">
                  {{ formatCurrency(product.importPrice) }}
                </td>

                <td class="text-end price-cell">
                  {{ formatCurrency(product.sellPrice) }}
                </td>

                <td class="text-center">
                  <strong>{{ formatNumber(getAvailableQuantity(product)) }}</strong>
                </td>

                <td>
                  <VChip :color="stockColor(product)" variant="tonal" size="small">
                    {{ stockStatus(product) }}
                  </VChip>
                </td>

                <td>
                  <VChip :color="product.isActive ? 'success' : 'secondary'" variant="tonal" size="small">
                    {{ product.isActive ? 'Đang bán' : 'Ngừng bán' }}
                  </VChip>
                </td>

                <td class="text-center" @click.stop>
                  <RetailActionMenu :items="productActions(product)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="products-empty">
          <VIcon icon="ri-barcode-line" size="42" color="primary" />
          <strong>Không tìm thấy sản phẩm phù hợp</strong>
          <span>Thử nhập từ khóa khác hoặc tạo sản phẩm mới.</span>

          <VBtn color="primary" prepend-icon="ri-add-line" @click="openAddProduct">
            Thêm sản phẩm
          </VBtn>
        </div>
      </template>

      <div v-if="products.length || totalCount > 0" class="products-pagination">
        <span>
          Hiển thị {{ formatNumber(products.length) }} trên tổng số {{ formatNumber(totalCount) }} sản phẩm
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" @update:model-value="resetAndLoad" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="productDialog" max-width="860" scrollable>
      <VCard class="product-dialog">
        <div class="dialog-head">
          <div>
            <span>{{ isEditing ? 'Cập nhật sản phẩm' : 'Thêm sản phẩm mới' }}</span>
            <h2>{{ isEditing ? productForm.name : 'Sản phẩm mới' }}</h2>
          </div>

          <VChip v-if="isEditing" color="primary" variant="tonal">
            {{ productForm.code || 'Chưa có SKU' }}
          </VChip>
        </div>

        <VTabs v-if="isEditing" v-model="activeTab" color="primary" class="dialog-tabs">
          <VTab value="general">
            Thông tin chung
          </VTab>
          <VTab value="history">
            Lịch sử giá
          </VTab>
        </VTabs>

        <VDivider v-if="isEditing" />

        <VWindow v-model="activeTab">
          <VWindowItem value="general">
            <VCardText>
              <div v-if="isEditing" class="image-upload-card">
                <VAvatar size="82" rounded="lg" class="image-preview">
                  <VImg v-if="productForm.imageUrl" :src="resolveImageUrl(productForm.imageUrl)" cover />
                  <VIcon v-else icon="ri-image-add-line" size="30" />
                </VAvatar>

                <div>
                  <strong>Ảnh sản phẩm</strong>
                  <p>Hỗ trợ JPG, PNG, WEBP. Nên dùng ảnh vuông để hiển thị đẹp hơn.</p>

                  <input ref="imageFileInput" type="file" accept="image/*" class="d-none" @change="onImageSelected">

                  <VBtn size="small" color="primary" variant="tonal" prepend-icon="ri-upload-cloud-2-line"
                    :loading="loading" @click="triggerImageUpload">
                    Tải ảnh lên
                  </VBtn>
                </div>
              </div>

              <VRow>
                <VCol cols="12" sm="6">
                  <VTextField v-model="productForm.code" label="Mã SKU" placeholder="Bỏ trống để hệ thống tự tạo"
                    density="comfortable" />
                </VCol>

                <VCol cols="12" sm="6">
                  <VTextField v-model="productForm.barcode" label="Barcode" density="comfortable" />
                </VCol>

                <VCol cols="12">
                  <VTextField v-model="productForm.name" label="Tên sản phẩm *" density="comfortable" required />
                </VCol>

                <VCol cols="12" sm="6">
                  <VSelect v-model="productForm.categoryId" label="Danh mục *" :items="categories" item-title="name"
                    item-value="id" density="comfortable" required />
                </VCol>

                <VCol cols="12" sm="6">
                  <VSelect v-model="productForm.unitId" label="Đơn vị tính *" :items="availableUnits" item-title="name"
                    item-value="id" density="comfortable" required />
                </VCol>

                <VCol cols="12" sm="4">
                  <VTextField v-model.number="productForm.importPrice" type="number" label="Giá nhập *" suffix="đ"
                    density="comfortable" />
                </VCol>

                <VCol cols="12" sm="4">
                  <VTextField v-model.number="productForm.sellPrice" type="number" label="Giá bán *" suffix="đ"
                    density="comfortable" />
                </VCol>

                <VCol cols="12" sm="4">
                  <VTextField v-model.number="productForm.weight" type="number" label="Cân nặng" suffix="g"
                    density="comfortable" />
                </VCol>

                <VCol cols="12">
                  <VTextarea v-model="productForm.description" label="Mô tả" rows="3" density="comfortable" />
                </VCol>
              </VRow>
            </VCardText>
          </VWindowItem>

          <VWindowItem v-if="isEditing" value="history">
            <VCardText class="history-body">
              <div v-if="priceHistory.length === 0" class="history-empty">
                <VIcon icon="ri-history-line" size="38" color="primary" />
                <strong>Chưa có lịch sử giá</strong>
                <span>Sản phẩm này chưa ghi nhận thay đổi giá.</span>
              </div>

              <VTimeline v-else side="end" align="start" density="compact" truncate-line="both">
                <VTimelineItem v-for="item in priceHistory" :key="item.id" dot-color="primary" size="x-small">
                  <div class="price-history-item">
                    <div class="price-history-head">
                      <strong>{{ item.changedBy }}</strong>
                      <span>{{ formatDateTime(item.createdAt) }}</span>
                    </div>

                    <div v-if="item.oldImportPrice !== item.newImportPrice" class="price-change-line">
                      <span>Giá nhập:</span>
                      <del>{{ formatCurrency(item.oldImportPrice) }}</del>
                      <VIcon icon="ri-arrow-right-line" size="14" />
                      <strong class="text-success">{{ formatCurrency(item.newImportPrice) }}</strong>
                    </div>

                    <div v-if="item.oldSellPrice !== item.newSellPrice" class="price-change-line">
                      <span>Giá bán:</span>
                      <del>{{ formatCurrency(item.oldSellPrice) }}</del>
                      <VIcon icon="ri-arrow-right-line" size="14" />
                      <strong class="text-primary">{{ formatCurrency(item.newSellPrice) }}</strong>
                    </div>
                  </div>
                </VTimelineItem>
              </VTimeline>
            </VCardText>
          </VWindowItem>
        </VWindow>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="productDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleSaveProduct">
            Lưu sản phẩm
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="categoryDialog" max-width="780" scrollable>
      <VCard class="category-dialog">
        <div class="dialog-head">
          <div>
            <span>Danh mục sản phẩm</span>
            <h2>Quản lý danh mục</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ categories.length }} danh mục
          </VChip>
        </div>

        <VCardText>
          <div class="category-form-card">
            <VTextField v-model="categoryForm.name" label="Tên danh mục mới" placeholder="VD: Nước giải khát"
              density="comfortable" hide-details="auto" />

            <VSelect v-model="categoryForm.parentId" label="Danh mục cha" :items="categories" item-title="name"
              item-value="id" clearable density="comfortable" hide-details="auto" />

            <VBtn color="primary" prepend-icon="ri-add-line" :loading="loading" @click="handleAddCategory">
              Thêm
            </VBtn>
          </div>

          <div class="category-table-wrap">
            <VTable class="products-table compact-table">
              <thead>
                <tr>
                  <th>Tên danh mục</th>
                  <th>Danh mục cha</th>
                  <th class="text-center">Thao tác</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="category in categories" :key="category.id">
                  <td class="font-weight-bold">
                    {{ category.name }}
                  </td>

                  <td>
                    {{ category.parentName || '—' }}
                  </td>

                  <td class="text-center">
                    <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                      @click="handleDeleteCategory(category.id)" />
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="outlined" @click="categoryDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.products-page {
  position: relative;
  isolation: isolate;
}

.products-page::before {
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

.products-panel,
.product-dialog,
.category-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.products-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(180px, 240px) auto auto;
}

.products-toolbar :deep(.v-field),
.product-dialog :deep(.v-field),
.category-dialog :deep(.v-field) {
  border-radius: 16px;
}

.products-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.products-table-wrap,
.category-table-wrap {
  overflow-x: auto;
}

.products-table {
  min-inline-size: 1040px;
}

.products-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.products-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.product-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.product-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.sku-cell {
  color: rgba(var(--v-theme-on-surface), 0.8);
  font-weight: 900;
}

.product-name-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-inline-size: 260px;
}

.product-avatar {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.product-name-cell strong,
.product-name-cell span {
  display: block;
}

.product-name-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.product-name-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.8rem;
}

.price-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.products-empty,
.history-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.products-empty strong,
.history-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.products-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.products-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.products-pagination>div {
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

.dialog-tabs {
  padding-inline: 1rem;
}

.image-upload-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.image-preview {
  flex: 0 0 auto;
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.image-upload-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.image-upload-card p {
  margin: 0.25rem 0 0.65rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.history-body {
  max-block-size: 420px;
  overflow-y: auto;
}

.price-history-item {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 16px;
  padding: 0.85rem;
  background: rgba(var(--v-theme-background), 0.44);
}

.price-history-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-block-end: 0.5rem;
}

.price-history-head strong {
  color: rgb(var(--v-theme-primary));
}

.price-history-head span {
  color: rgba(var(--v-theme-on-surface), 0.55);
  font-size: 0.82rem;
}

.price-change-line {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.4rem;
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 0.9rem;
}

.category-form-card {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(0, 1fr) minmax(180px, 240px) auto;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.compact-table {
  min-inline-size: 640px;
}

.compact-table :deep(td) {
  block-size: 54px !important;
}

.dialog-actions {
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .products-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .products-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .products-toolbar,
  .category-form-card {
    grid-template-columns: 1fr;
  }

  .products-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .products-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .image-upload-card {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.products-hero {
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

.products-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.products-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.products-hero__copy {
  display: none !important;
}

.products-hero__actions,
.products-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.products-hero__actions .v-btn,
.products-actions .v-btn,
.products-hero__actions .v-btn.primary-action,
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