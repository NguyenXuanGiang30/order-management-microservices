<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'

import {
  type PromotionDto,
  createPromotion,
  getPromotions,
  togglePromotionActive,
  updatePromotion,
} from '@/services/orderSalesApi'

import {
  type ProductDto,
  getProducts,
} from '@/services/productInventoryApi'

const promotions = ref<PromotionDto[]>([])
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const search = ref('')
const selectedType = ref('Tất cả')
const selectedState = ref('Tất cả')

const promotionDialog = ref(false)
const editingPromotion = ref<PromotionDto | null>(null)

const promoForm = ref({
  code: '',
  name: '',
  description: '',
  promotionType: 'Order',
  discountType: 'Percent',
  discountValue: 0,
  minimumOrderAmount: 0,
  startAt: '',
  endAt: '',
  items: [] as Array<{
    productId: string
    productCode: string
    productName: string
    requiredQuantity: number
  }>,
})

const productSearchQuery = ref('')
const searchResult = ref<ProductDto[]>([])
const searchLoading = ref(false)

const promotionTypes = [
  { title: 'Theo đơn hàng', value: 'Order', icon: 'ri-shopping-bag-3-line' },
  { title: 'Theo sản phẩm', value: 'Product', icon: 'ri-price-tag-3-line' },
  { title: 'Theo combo sản phẩm', value: 'Combo', icon: 'ri-stack-line' },
  { title: 'Mua X tặng Y', value: 'BuyXGetY', icon: 'ri-gift-line' },
]

const promotionTypeFilters = [
  { title: 'Tất cả', value: 'Tất cả' },
  ...promotionTypes.map(item => ({
    title: item.title,
    value: item.value,
  })),
]

const stateFilters = [
  { title: 'Tất cả', value: 'Tất cả' },
  { title: 'Đang chạy', value: 'Active' },
  { title: 'Tạm dừng', value: 'Inactive' },
  { title: 'Hết hạn', value: 'Expired' },
  { title: 'Sắp diễn ra', value: 'Upcoming' },
]

const discountTypes = [
  { title: 'Phần trăm (%)', value: 'Percent' },
  { title: 'Số tiền cố định (đ)', value: 'FixedAmount' },
]

const moneyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const formatCurrency = (value: number) => moneyFormatter.format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const getPromotionTypeLabel = (type: string) => {
  switch (type) {
    case 'Order':
      return 'Đơn hàng'
    case 'Product':
      return 'Sản phẩm'
    case 'Combo':
      return 'Combo'
    case 'BuyXGetY':
      return 'Mua X tặng Y'
    default:
      return type
  }
}

const getPromotionTypeIcon = (type: string) =>
  promotionTypes.find(item => item.value === type)?.icon ?? 'ri-coupon-3-line'

const formatDiscountValue = (promotion: Pick<PromotionDto, 'discountType' | 'discountValue'>) => {
  if (promotion.discountType === 'Percent')
    return `${promotion.discountValue}%`

  return formatCurrency(promotion.discountValue)
}

const formatDate = (value: string) => {
  if (!value)
    return '—'

  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    }).format(date)
}

const isExpired = (promotion: PromotionDto) => {
  const end = new Date(promotion.endAt)

  if (Number.isNaN(end.getTime()))
    return false

  return end.getTime() < Date.now()
}

const isUpcoming = (promotion: PromotionDto) => {
  const start = new Date(promotion.startAt)

  if (Number.isNaN(start.getTime()))
    return false

  return start.getTime() > Date.now()
}

const getPromotionState = (promotion: PromotionDto) => {
  if (!promotion.isActive)
    return 'Inactive'

  if (isExpired(promotion))
    return 'Expired'

  if (isUpcoming(promotion))
    return 'Upcoming'

  return 'Active'
}

const getPromotionStateLabel = (promotion: PromotionDto) => {
  const state = getPromotionState(promotion)

  if (state === 'Active')
    return 'Đang chạy'

  if (state === 'Inactive')
    return 'Tạm dừng'

  if (state === 'Expired')
    return 'Hết hạn'

  if (state === 'Upcoming')
    return 'Sắp diễn ra'

  return state
}

const getPromotionStateColor = (promotion: PromotionDto) => {
  const state = getPromotionState(promotion)

  if (state === 'Active')
    return 'success'

  if (state === 'Inactive')
    return 'secondary'

  if (state === 'Expired')
    return 'error'

  if (state === 'Upcoming')
    return 'info'

  return 'secondary'
}

const filteredPromotions = computed(() => {
  const keyword = search.value.trim().toLowerCase()

  return promotions.value.filter(promotion => {
    const matchesSearch = !keyword
      || promotion.code.toLowerCase().includes(keyword)
      || promotion.name.toLowerCase().includes(keyword)
      || promotion.description?.toLowerCase().includes(keyword)

    const matchesType = selectedType.value === 'Tất cả'
      || promotion.promotionType === selectedType.value

    const matchesState = selectedState.value === 'Tất cả'
      || getPromotionState(promotion) === selectedState.value

    return matchesSearch && matchesType && matchesState
  })
})

const activeCount = computed(() =>
  promotions.value.filter(promotion => getPromotionState(promotion) === 'Active').length,
)

const expiredCount = computed(() =>
  promotions.value.filter(promotion => getPromotionState(promotion) === 'Expired').length,
)

const productPromoCount = computed(() =>
  promotions.value.filter(promotion => promotion.promotionType !== 'Order').length,
)

const summaryCards = computed(() => [
  {
    label: 'Tổng chương trình',
    value: formatNumber(promotions.value.length),
    helper: `${formatNumber(filteredPromotions.value.length)} chương trình đang hiển thị`,
    icon: 'ri-coupon-3-line',
    color: 'primary',
  },
  {
    label: 'Đang chạy',
    value: formatNumber(activeCount.value),
    helper: 'Có hiệu lực và đang kích hoạt',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Theo sản phẩm',
    value: formatNumber(productPromoCount.value),
    helper: 'Product, Combo, Mua X tặng Y',
    icon: 'ri-price-tag-3-line',
    color: 'info',
  },
  {
    label: 'Hết hạn',
    value: formatNumber(expiredCount.value),
    helper: 'Cần kiểm tra hoặc gia hạn',
    icon: 'ri-time-line',
    color: 'error',
  },
])

const promotionNeedsProducts = computed(() =>
  ['Product', 'Combo', 'BuyXGetY'].includes(promoForm.value.promotionType),
)

const loadPromotions = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    promotions.value = await getPromotions()
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách khuyến mãi.'
  }
  finally {
    loading.value = false
  }
}

const handleToggleActive = async (promo: PromotionDto) => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await togglePromotionActive(promo.id)

    successMessage.value = promo.isActive
      ? 'Đã tạm dừng chương trình khuyến mãi.'
      : 'Đã kích hoạt chương trình khuyến mãi.'

    await loadPromotions()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi đổi trạng thái khuyến mãi.'
  }
  finally {
    loading.value = false
  }
}

const searchProducts = async () => {
  if (!productSearchQuery.value.trim()) {
    searchResult.value = []

    return
  }

  searchLoading.value = true

  try {
    const result = await getProducts({
      search: productSearchQuery.value.trim(),
      pageSize: 10,
    })

    searchResult.value = result.items
  }
  catch (error) {
    console.error(error)
  }
  finally {
    searchLoading.value = false
  }
}

const addProductToPromo = (product: ProductDto) => {
  const existing = promoForm.value.items.find(item => item.productId === product.id)

  if (existing)
    return

  if (promoForm.value.promotionType === 'BuyXGetY' && promoForm.value.items.length >= 2) {
    errorMessage.value = 'Mua X tặng Y chỉ nên chọn 2 sản phẩm: sản phẩm mua và sản phẩm tặng.'

    return
  }

  promoForm.value.items.push({
    productId: product.id,
    productCode: product.code,
    productName: product.name,
    requiredQuantity: 1,
  })

  productSearchQuery.value = ''
  searchResult.value = []
}

const removeProductFromPromo = (productId: string) => {
  promoForm.value.items = promoForm.value.items.filter(item => item.productId !== productId)
}

const resetPromoForm = () => {
  const now = new Date()
  const nextMonth = new Date()

  nextMonth.setMonth(now.getMonth() + 1)

  promoForm.value = {
    code: '',
    name: '',
    description: '',
    promotionType: 'Order',
    discountType: 'Percent',
    discountValue: 0,
    minimumOrderAmount: 0,
    startAt: now.toISOString().substring(0, 10),
    endAt: nextMonth.toISOString().substring(0, 10),
    items: [],
  }

  productSearchQuery.value = ''
  searchResult.value = []
}

const openCreateDialog = () => {
  editingPromotion.value = null
  resetPromoForm()
  promotionDialog.value = true
}

const openEditDialog = (promo: PromotionDto) => {
  editingPromotion.value = promo

  promoForm.value = {
    code: promo.code,
    name: promo.name,
    description: promo.description || '',
    promotionType: promo.promotionType,
    discountType: promo.discountType,
    discountValue: promo.discountValue,
    minimumOrderAmount: promo.minimumOrderAmount || 0,
    startAt: promo.startAt.substring(0, 10),
    endAt: promo.endAt.substring(0, 10),
    items: [...(promo.items || [])],
  }

  productSearchQuery.value = ''
  searchResult.value = []
  promotionDialog.value = true
}

const validatePromotionForm = () => {
  if (!promoForm.value.code.trim())
    return 'Vui lòng nhập mã khuyến mãi.'

  if (!promoForm.value.name.trim())
    return 'Vui lòng nhập tên chương trình.'

  if (!promoForm.value.startAt || !promoForm.value.endAt)
    return 'Vui lòng chọn ngày bắt đầu và ngày kết thúc.'

  if (new Date(promoForm.value.startAt).getTime() > new Date(promoForm.value.endAt).getTime())
    return 'Ngày bắt đầu không được lớn hơn ngày kết thúc.'

  if (promoForm.value.discountValue <= 0)
    return 'Giá trị giảm giá phải lớn hơn 0.'

  if (promoForm.value.discountType === 'Percent' && promoForm.value.discountValue > 100)
    return 'Giảm giá phần trăm không được vượt quá 100%.'

  if (promoForm.value.promotionType === 'Product' && promoForm.value.items.length === 0)
    return 'Khuyến mãi theo sản phẩm cần chọn ít nhất một sản phẩm.'

  if (promoForm.value.promotionType === 'Combo' && promoForm.value.items.length < 2)
    return 'Khuyến mãi combo nên có ít nhất 2 sản phẩm.'

  if (promoForm.value.promotionType === 'BuyXGetY' && promoForm.value.items.length !== 2)
    return 'Mua X tặng Y cần chọn đúng 2 sản phẩm: sản phẩm mua và sản phẩm tặng.'

  return ''
}

const handleSavePromotion = async () => {
  const validationMessage = validatePromotionForm()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const payload = {
      code: promoForm.value.code.trim(),
      name: promoForm.value.name.trim(),
      description: promoForm.value.description || null,
      promotionType: promoForm.value.promotionType,
      discountType: promoForm.value.discountType,
      discountValue: Number(promoForm.value.discountValue) || 0,
      minimumOrderAmount: Number(promoForm.value.minimumOrderAmount) || 0,
      startAt: new Date(promoForm.value.startAt).toISOString(),
      endAt: new Date(promoForm.value.endAt).toISOString(),
      items: promoForm.value.items.map(item => ({
        productId: item.productId,
        productCode: item.productCode,
        productName: item.productName,
        requiredQuantity: Number(item.requiredQuantity) || 1,
      })),
    }

    if (editingPromotion.value) {
      await updatePromotion(editingPromotion.value.id, payload)
      successMessage.value = 'Cập nhật chương trình khuyến mãi thành công.'
    }
    else {
      await createPromotion(payload)
      successMessage.value = 'Tạo chương trình khuyến mãi thành công.'
    }

    promotionDialog.value = false
    await loadPromotions()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi lưu chương trình khuyến mãi.'
  }
  finally {
    loading.value = false
  }
}

const promoActions = (promo: PromotionDto): ActionMenuItem[] => [
  {
    label: 'Chỉnh sửa',
    icon: 'ri-edit-line',
    color: 'primary',
    handler: () => openEditDialog(promo),
  },
  {
    label: promo.isActive ? 'Tắt kích hoạt' : 'Kích hoạt',
    icon: promo.isActive ? 'ri-toggle-line' : 'ri-toggle-fill',
    color: promo.isActive ? 'warning' : 'success',
    handler: () => handleToggleActive(promo),
  },
]

watch(
  () => promoForm.value.promotionType,
  type => {
    if (type === 'Order') {
      promoForm.value.items = []
      searchResult.value = []
      productSearchQuery.value = ''
    }

    if (type === 'BuyXGetY' && promoForm.value.items.length > 2)
      promoForm.value.items = promoForm.value.items.slice(0, 2)
  },
)

onMounted(loadPromotions)
</script>

<template>
  <section class="promotions-page">
    <div class="promotions-hero">
      <div class="promotions-hero__title-area">
        <h1>Chương trình khuyến mãi</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-coupon-3-line" class="ml-2">
          Khuyến mãi
        </VChip>
      </div>

      <div class="promotions-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadPromotions">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-coupon-3-line" class="primary-action" @click="openCreateDialog">
          Tạo khuyến mãi
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

    <VCard class="promotions-panel">
      <VCardText>
        <div class="promotions-toolbar">
          <VTextField v-model="search" label="Tìm khuyến mãi" placeholder="Mã, tên chương trình, mô tả..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable />

          <VSelect v-model="selectedType" label="Phạm vi" :items="promotionTypeFilters" item-title="title"
            item-value="value" density="comfortable" hide-details />

          <VSelect v-model="selectedState" label="Trạng thái" :items="stateFilters" item-title="title"
            item-value="value" density="comfortable" hide-details />

          <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line"
            @click="() => { search = ''; selectedType = 'Tất cả'; selectedState = 'Tất cả' }">
            Xóa lọc
          </VBtn>
        </div>
      </VCardText>

      <VDivider />

      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText>

      <template v-else>
        <div v-if="filteredPromotions.length" class="promotions-table-wrap">
          <VTable class="promotions-table">
            <thead>
              <tr>
                <th>Mã khuyến mãi</th>
                <th>Tên chương trình</th>
                <th>Phạm vi</th>
                <th>Chiết khấu</th>
                <th class="text-end">Đơn tối thiểu</th>
                <th>Hiệu lực</th>
                <th class="text-center">Trạng thái</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="promotion in filteredPromotions" :key="promotion.id" class="promotion-row"
                @click="openEditDialog(promotion)">
                <td>
                  <div class="promo-code">
                    <VIcon icon="ri-coupon-3-line" />
                    <strong>{{ promotion.code }}</strong>
                  </div>
                </td>

                <td>
                  <div class="promo-name">
                    <strong>{{ promotion.name }}</strong>
                    <span>{{ promotion.description || 'Không có mô tả' }}</span>
                  </div>
                </td>

                <td>
                  <VChip color="primary" variant="tonal" size="small">
                    <VIcon :icon="getPromotionTypeIcon(promotion.promotionType)" size="16" class="me-1" />
                    {{ getPromotionTypeLabel(promotion.promotionType) }}
                  </VChip>
                </td>

                <td>
                  <div class="discount-cell">
                    <strong>{{ formatDiscountValue(promotion) }}</strong>
                    <span>{{ promotion.discountType === 'Percent' ? 'Phần trăm' : 'Số tiền cố định' }}</span>
                  </div>
                </td>

                <td class="text-end">
                  {{ formatCurrency(promotion.minimumOrderAmount || 0) }}
                </td>

                <td>
                  <div class="date-cell">
                    <strong>{{ formatDate(promotion.startAt) }}</strong>
                    <span>đến {{ formatDate(promotion.endAt) }}</span>
                  </div>
                </td>

                <td class="text-center">
                  <VChip :color="getPromotionStateColor(promotion)" variant="tonal" size="small">
                    {{ getPromotionStateLabel(promotion) }}
                  </VChip>
                </td>

                <td class="text-center" @click.stop>
                  <RetailActionMenu :items="promoActions(promotion)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="promotions-empty">
          <VIcon icon="ri-coupon-3-line" size="42" color="primary" />
          <strong>Chưa có chương trình khuyến mãi phù hợp</strong>
          <span>Tạo chương trình mới hoặc thay đổi bộ lọc hiện tại.</span>

          <VBtn color="primary" prepend-icon="ri-coupon-3-line" @click="openCreateDialog">
            Tạo khuyến mãi
          </VBtn>
        </div>
      </template>
    </VCard>

    <VDialog v-model="promotionDialog" max-width="920" persistent scrollable>
      <VCard class="promotion-dialog">
        <div class="dialog-head">
          <div>
            <span>{{ editingPromotion ? 'Cập nhật khuyến mãi' : 'Tạo khuyến mãi mới' }}</span>
            <h2>{{ promoForm.name || 'Chương trình mới' }}</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ promoForm.code || 'Chưa có mã' }}
          </VChip>
        </div>

        <VCardText>
          <div class="form-section">
            <div class="section-title">
              Thông tin chương trình
            </div>

            <VRow>
              <VCol cols="12" sm="6">
                <VTextField v-model="promoForm.code" label="Mã khuyến mãi *" placeholder="VD: KHUYENMAI20"
                  :disabled="!!editingPromotion" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VTextField v-model="promoForm.name" label="Tên chương trình *" placeholder="VD: Giảm giá mùa hè"
                  density="comfortable" />
              </VCol>

              <VCol cols="12">
                <VTextField v-model="promoForm.description" label="Mô tả chương trình" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VSelect v-model="promoForm.promotionType" label="Phạm vi áp dụng" :items="promotionTypes"
                  item-title="title" item-value="value" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VSelect v-model="promoForm.discountType" label="Hình thức chiết khấu" :items="discountTypes"
                  item-title="title" item-value="value" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VTextField v-model.number="promoForm.discountValue" type="number"
                  :label="promoForm.discountType === 'Percent' ? 'Giá trị giảm (%) *' : 'Giá trị giảm (đ) *'"
                  :suffix="promoForm.discountType === 'Percent' ? '%' : 'đ'" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VTextField v-model.number="promoForm.minimumOrderAmount" type="number" label="Đơn hàng tối thiểu"
                  suffix="đ" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VTextField v-model="promoForm.startAt" type="date" label="Ngày bắt đầu *" density="comfortable" />
              </VCol>

              <VCol cols="12" sm="6">
                <VTextField v-model="promoForm.endAt" type="date" label="Ngày kết thúc *" density="comfortable" />
              </VCol>
            </VRow>
          </div>

          <div v-if="promotionNeedsProducts" class="form-section product-section">
            <div class="section-title">
              Sản phẩm áp dụng
            </div>

            <VAlert v-if="promoForm.promotionType === 'BuyXGetY'" type="info" variant="tonal" density="compact"
              class="mb-4">
              <strong>Mua X tặng Y:</strong> chọn đúng 2 sản phẩm. Dòng đầu là sản phẩm mua, dòng thứ hai là sản phẩm
              tặng.
            </VAlert>

            <VAlert v-if="promoForm.promotionType === 'Combo'" type="info" variant="tonal" density="compact"
              class="mb-4">
              <strong>Combo:</strong> khách cần mua đủ các sản phẩm bên dưới với số lượng tối thiểu để được áp dụng
              khuyến
              mãi.
            </VAlert>

            <div class="product-search-box">
              <VTextField v-model="productSearchQuery" label="Tìm sản phẩm áp dụng"
                placeholder="Nhập tên, SKU hoặc barcode..." prepend-inner-icon="ri-search-line" hide-details
                density="comfortable" @keyup.enter="searchProducts" />

              <VBtn color="primary" :loading="searchLoading" prepend-icon="ri-search-line" @click="searchProducts">
                Tìm
              </VBtn>
            </div>

            <div v-if="searchResult.length" class="product-search-results">
              <button v-for="product in searchResult" :key="product.id" type="button" class="product-result"
                @click="addProductToPromo(product)">
                <span>
                  <strong>{{ product.name }}</strong>
                  <small>{{ product.code }} · {{ formatCurrency(product.sellPrice) }}</small>
                </span>

                <VIcon icon="ri-add-line" />
              </button>
            </div>

            <div class="selected-products-wrap">
              <VTable class="promotions-table compact-table">
                <thead>
                  <tr>
                    <th v-if="promoForm.promotionType === 'BuyXGetY'">
                      Vai trò
                    </th>
                    <th>Sản phẩm</th>
                    <th>Mã hàng</th>
                    <th class="text-center">
                      {{ promoForm.promotionType === 'BuyXGetY' ? 'Số lượng X/Y' : 'Số lượng tối thiểu' }}
                    </th>
                    <th class="text-center">Xóa</th>
                  </tr>
                </thead>

                <tbody>
                  <tr v-for="(item, index) in promoForm.items" :key="item.productId">
                    <td v-if="promoForm.promotionType === 'BuyXGetY'">
                      <VChip :color="index === 0 ? 'primary' : 'success'" size="small" variant="tonal">
                        {{ index === 0 ? 'Mua (X)' : 'Tặng (Y)' }}
                      </VChip>
                    </td>

                    <td class="font-weight-bold">
                      {{ item.productName }}
                    </td>

                    <td>
                      {{ item.productCode }}
                    </td>

                    <td class="text-center">
                      <VTextField v-model.number="item.requiredQuantity" type="number" density="compact" hide-details
                        min="1" class="quantity-input" />
                    </td>

                    <td class="text-center">
                      <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                        @click="removeProductFromPromo(item.productId)" />
                    </td>
                  </tr>

                  <tr v-if="!promoForm.items.length">
                    <td :colspan="promoForm.promotionType === 'BuyXGetY' ? 5 : 4"
                      class="text-center text-medium-emphasis py-6">
                      Chưa chọn sản phẩm nào cho chương trình này.
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </div>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="promotionDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleSavePromotion">
            Lưu lại
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.promotions-page {
  position: relative;
  isolation: isolate;
}

.promotions-page::before {
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

.summary-icon--info {
  background: rgb(var(--v-theme-info));
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

.promotions-panel,
.promotion-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.promotions-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(180px, 220px) minmax(180px, 220px) auto;
}

.promotions-toolbar :deep(.v-field),
.promotion-dialog :deep(.v-field) {
  border-radius: 16px;
}

.promotions-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.promotions-table-wrap,
.selected-products-wrap {
  overflow-x: auto;
}

.promotions-table {
  min-inline-size: 980px;
}

.promotions-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.promotions-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.promotion-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.promotion-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.promo-code {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: rgb(var(--v-theme-primary));
}

.promo-name strong,
.promo-name span,
.discount-cell strong,
.discount-cell span,
.date-cell strong,
.date-cell span {
  display: block;
}

.promo-name strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.promo-name span {
  max-inline-size: 320px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.82rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.discount-cell strong {
  color: rgb(var(--v-theme-success));
  font-weight: 900;
}

.discount-cell span,
.date-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.date-cell strong {
  font-weight: 850;
}

.promotions-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.promotions-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
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

.form-section {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.42);
}

.product-section {
  margin-block-start: 1rem;
}

.section-title {
  margin-block-end: 0.85rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 950;
  letter-spacing: 0.06em;
  text-transform: uppercase;
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

.product-search-results {
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

.product-result {
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

.product-result:hover {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
}

.product-result strong,
.product-result small {
  display: block;
}

.product-result strong {
  font-weight: 900;
}

.product-result small {
  color: rgba(var(--v-theme-on-surface), 0.56);
}

.compact-table {
  min-inline-size: 760px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 54px !important;
}

.quantity-input {
  max-inline-size: 120px;
  margin-inline: auto;
}

.dialog-actions {
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .promotions-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .promotions-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .promotions-toolbar,
  .product-search-box {
    grid-template-columns: 1fr;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.promotions-hero {
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

.promotions-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.promotions-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.promotions-hero__copy {
  display: none !important;
}

.promotions-hero__actions,
.promotions-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.promotions-hero__actions .v-btn,
.promotions-actions .v-btn,
.promotions-hero__actions .v-btn.primary-action,
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