<script setup lang="ts">
import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import {
  type ProductDto,
  type CategoryDto,
  getProducts,
  createProduct,
  updateProduct,
  toggleProductActive,
  getCategories,
  createCategory,
  updateCategory,
  deleteCategory,
} from '@/services/productInventoryApi'

const products = ref<ProductDto[]>([])
const categories = ref<CategoryDto[]>([])
const search = ref('')
const selectedCategory = ref('Tất cả')
const totalCount = ref(0)
const loading = ref(false)
const errorMessage = ref('')

// Dialog states
const productDialog = ref(false)
const categoryDialog = ref(false)
const isEditing = ref(false)

// Forms
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

const categoryItems = computed(() => {
  const list = ['Tất cả']
  categories.value.forEach(c => {
    if (c.name) list.push(c.name)
  })
  return list
})

const visibleProducts = computed(() => {
  if (selectedCategory.value === 'Tất cả')
    return products.value

  return products.value.filter(product => product.categoryName === selectedCategory.value)
})

const stockStatus = (product: ProductDto) => {
  if (product.quantityOnHand <= 0)
    return 'Hết hàng'

  if (product.quantityOnHand <= product.quantityReserved)
    return 'Thấp'

  return 'Tốt'
}

const totalProducts = computed(() => products.value.length)
const lowStockProducts = computed(() => products.value.filter(p => p.quantityOnHand > 0 && p.quantityOnHand <= p.quantityReserved).length)
const outOfStockProducts = computed(() => products.value.filter(p => p.quantityOnHand <= 0).length)

const productActions = (product: ProductDto): ActionMenuItem[] => [
  { label: 'Chỉnh sửa', icon: 'ri-edit-line', color: 'primary', handler: () => openEditProduct(product) },
  { label: product.isActive ? 'Ngừng bán' : 'Bán lại', icon: product.isActive ? 'ri-error-warning-line' : 'ri-checkbox-circle-line', color: product.isActive ? 'error' : 'success', handler: () => handleToggleActive(product) },
]

const loadCategories = async () => {
  try {
    categories.value = await getCategories()
  } catch (error) {
    console.error('Lỗi load danh mục:', error)
  }
}

const loadProducts = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getProducts({
      search: search.value.trim(),
      pageNumber: 1,
      pageSize: 100,
    })

    products.value = result.items
    totalCount.value = result.totalCount
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

// Extracted units from products dynamically
const availableUnits = computed(() => {
  const units = products.value.map(p => ({ id: p.unitId, name: p.unitName }))
  const unique = new Map<string, string>()
  for (const u of units) {
    if (u.id && u.name) {
      unique.set(u.id, u.name)
    }
  }
  const result = Array.from(unique.entries()).map(([id, name]) => ({ id, name }))
  if (result.length === 0) {
    result.push({ id: '00000000-0000-0000-0000-000000000000', name: 'cái' })
  }
  return result
})

const openAddProduct = () => {
  isEditing.value = false
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
  }
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
  }
  productDialog.value = true
}

const handleSaveProduct = async () => {
  if (!productForm.value.name || !productForm.value.categoryId || !productForm.value.unitId) {
    errorMessage.value = 'Vui lòng nhập đầy đủ Tên, Danh mục và Đơn vị tính.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    const payload = {
      code: productForm.value.code || undefined,
      name: productForm.value.name,
      description: productForm.value.description || null,
      barcode: productForm.value.barcode || null,
      importPrice: Number(productForm.value.importPrice) || 0,
      sellPrice: Number(productForm.value.sellPrice) || 0,
      imageUrl: null,
      weight: productForm.value.weight ? Number(productForm.value.weight) : null,
      categoryId: productForm.value.categoryId,
      unitId: productForm.value.unitId,
    }

    if (isEditing.value) {
      await updateProduct(productForm.value.id, payload)
    } else {
      await createProduct(payload)
    }

    productDialog.value = false
    await loadProducts()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu sản phẩm.'
  } finally {
    loading.value = false
  }
}

const handleToggleActive = async (product: ProductDto) => {
  loading.value = true
  errorMessage.value = ''
  try {
    await toggleProductActive(product.id)
    await loadProducts()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi cập nhật trạng thái sản phẩm.'
  } finally {
    loading.value = false
  }
}

const handleAddCategory = async () => {
  if (!categoryForm.value.name) return
  loading.value = true
  errorMessage.value = ''
  try {
    await createCategory({
      name: categoryForm.value.name,
      parentId: categoryForm.value.parentId || null,
    })
    categoryForm.value.name = ''
    categoryForm.value.parentId = null
    await loadCategories()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu danh mục.'
  } finally {
    loading.value = false
  }
}

const handleDeleteCategory = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa danh mục này?')) return
  loading.value = true
  errorMessage.value = ''
  try {
    await deleteCategory(id)
    await loadCategories()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xóa danh mục.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadCategories()
  await loadProducts()
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Danh mục hàng hóa"
    title="Quản lý sản phẩm"
    subtitle="Theo dõi SKU, barcode, giá nhập, giá bán và nhà cung cấp của từng mặt hàng."
  >
    <template #actions>
      <div class="d-flex flex-wrap gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="loadProducts"
        >
          Tải lại
        </VBtn>
        <VBtn
          variant="tonal"
          prepend-icon="ri-folder-settings-line"
          @click="categoryDialog = true"
        >
          Quản lý danh mục
        </VBtn>
        <VBtn
          prepend-icon="ri-add-line"
          @click="openAddProduct"
        >
          Thêm sản phẩm
        </VBtn>
      </div>
    </template>
  </RetailPageHeader>

  <VAlert
    v-if="errorMessage"
    type="error"
    variant="tonal"
    class="mb-6"
    closable
    @click:close="errorMessage = ''"
  >
    {{ errorMessage }}
  </VAlert>

  <VRow class="mb-2">
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Tổng SKU sản phẩm', value: String(totalProducts), helper: 'Mặt hàng đang quản lý', icon: 'ri-archive-line', color: 'primary' }" />
    </VCol>
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Sản phẩm tồn thấp', value: String(lowStockProducts), helper: 'Cần lên kế hoạch nhập hàng', icon: 'ri-alert-line', color: 'warning' }" />
    </VCol>
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Sản phẩm hết hàng', value: String(outOfStockProducts), helper: 'Hết hàng trong kho', icon: 'ri-close-circle-line', color: 'error' }" />
    </VCol>
  </VRow>

  <VCard class="retail-panel-card">
    <RetailFilterBar
      v-model="search"
      search-placeholder="Tên sản phẩm, SKU, barcode..."
      :filters="[{
        key: 'category',
        label: 'Danh mục',
        items: categoryItems.map(c => ({ title: c, value: c })),
        modelValue: selectedCategory,
      }]"
      :loading="loading"
      @search="loadProducts"
      @reload="loadProducts"
      @filterChange="(_key: string, val: any) => { selectedCategory = val }"
    >
      <template #actions>
        <VBtn
          variant="tonal"
          prepend-icon="ri-folder-settings-line"
          @click="categoryDialog = true"
        >
          Quản lý danh mục
        </VBtn>
        <VBtn
          prepend-icon="ri-add-line"
          @click="openAddProduct"
        >
          Thêm sản phẩm
        </VBtn>
      </template>
    </RetailFilterBar>

    <VCardText
      v-if="loading"
      class="pt-0"
    >
      <VSkeletonLoader type="table-heading, table-tbody" />
    </VCardText>

    <VTable
      v-else
      class="retail-table"
    >
      <thead>
        <tr>
          <th>SKU</th>
          <th>Sản phẩm</th>
          <th>Barcode</th>
          <th>Danh mục</th>
          <th class="text-end">Giá nhập</th>
          <th class="text-end">Giá bán</th>
          <th>Tồn kho</th>
          <th>Trạng thái</th>
          <th class="text-center" style="width: 60px;">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="product in visibleProducts"
          :key="product.id"
          class="hover-row"
          @click="openEditProduct(product)"
        >
          <td class="font-weight-bold">
            {{ product.code }}
          </td>
          <td class="font-weight-bold text-primary">{{ product.name }}</td>
          <td>{{ product.barcode || '—' }}</td>
          <td>{{ product.categoryName }}</td>
          <td class="text-end">
            {{ formatCurrency(product.importPrice) }}
          </td>
          <td class="text-end font-weight-bold text-primary">
            {{ formatCurrency(product.sellPrice) }}
          </td>
          <td>
            <RetailStatusBadge
              :status="stockStatus(product)"
              dot
            />
          </td>
          <td>
            <RetailStatusBadge
              :status="product.isActive ? 'Đang bán' : 'Ngừng bán'"
              dot
            />
          </td>
          <td
            class="text-center"
            @click.stop
          >
            <RetailActionMenu :items="productActions(product)" />
          </td>
        </tr>
        <RetailEmptyState
          v-if="!loading && !visibleProducts.length"
          :colspan="9"
          icon="ri-barcode-line"
          title="Không tìm thấy sản phẩm phù hợp"
          subtitle="Thử nhập từ khóa khác hoặc tạo sản phẩm mới."
          action-label="Thêm sản phẩm"
        />
      </tbody>
    </VTable>
  </VCard>

  <!-- Product Add/Edit Dialog -->
  <VDialog
    v-model="productDialog"
    max-width="600"
  >
    <VCard>
      <VCardTitle>{{ isEditing ? 'Cập nhật sản phẩm' : 'Thêm sản phẩm mới' }}</VCardTitle>
      <VCardText>
        <VRow>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="productForm.code"
              label="Mã SKU (Tùy chọn)"
              placeholder="Hệ thống tự tạo nếu bỏ trống"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="productForm.barcode"
              label="Barcode"
            />
          </VCol>
          <VCol cols="12">
            <VTextField
              v-model="productForm.name"
              label="Tên sản phẩm *"
              required
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VSelect
              v-model="productForm.categoryId"
              label="Danh mục *"
              :items="categories"
              item-title="name"
              item-value="id"
              required
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VSelect
              v-model="productForm.unitId"
              label="Đơn vị tính *"
              :items="availableUnits"
              item-title="name"
              item-value="id"
              required
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VTextField
              v-model.number="productForm.importPrice"
              type="number"
              label="Giá nhập *"
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VTextField
              v-model.number="productForm.sellPrice"
              type="number"
              label="Giá bán *"
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VTextField
              v-model.number="productForm.weight"
              type="number"
              label="Cân nặng (g)"
            />
          </VCol>
          <VCol cols="12">
            <VTextarea
              v-model="productForm.description"
              label="Mô tả"
              rows="3"
            />
          </VCol>
        </VRow>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="productDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSaveProduct"
        >
          Lưu
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Category Management Dialog -->
  <VDialog
    v-model="categoryDialog"
    max-width="700"
  >
    <VCard>
      <VCardTitle>Quản lý danh mục sản phẩm</VCardTitle>
      <VCardText>
        <VRow class="mb-4">
          <VCol cols="12" sm="6">
            <VTextField
              v-model="categoryForm.name"
              label="Tên danh mục mới"
              placeholder="VD: Nước giải khát"
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VSelect
              v-model="categoryForm.parentId"
              label="Danh mục cha (Tùy chọn)"
              :items="categories"
              item-title="name"
              item-value="id"
              clearable
            />
          </VCol>
          <VCol cols="12" sm="2" class="d-flex align-center">
            <VBtn
              color="primary"
              block
              @click="handleAddCategory"
            >
              Thêm
            </VBtn>
          </VCol>
        </VRow>

        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Tên danh mục</th>
              <th>Danh mục cha</th>
              <th class="text-center">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="cat in categories"
              :key="cat.id"
            >
              <td class="font-weight-bold">{{ cat.name }}</td>
              <td>{{ cat.parentName || '—' }}</td>
              <td class="text-center">
                <VBtn
                  icon="ri-delete-bin-line"
                  variant="text"
                  size="small"
                  color="error"
                  @click="handleDeleteCategory(cat.id)"
                />
              </td>
            </tr>
          </tbody>
        </VTable>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          @click="categoryDialog = false"
        >
          Đóng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
