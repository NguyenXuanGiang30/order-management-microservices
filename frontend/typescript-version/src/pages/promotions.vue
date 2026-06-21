<script setup lang="ts">
import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import {
  type PromotionDto,
  getPromotions,
  createPromotion,
  updatePromotion,
  togglePromotionActive,
} from '@/services/orderSalesApi'
import {
  type ProductDto,
  getProducts,
} from '@/services/productInventoryApi'

const promotions = ref<PromotionDto[]>([])
const loading = ref(false)
const errorMessage = ref('')

// Dialogs
const promotionDialog = ref(false)
const editingPromotion = ref<PromotionDto | null>(null)

// Form states
const promoForm = ref({
  code: '',
  name: '',
  description: '',
  promotionType: 'Order', // Order, Product
  discountType: 'Percent', // Percent, FixedAmount
  discountValue: 0,
  minimumOrderAmount: 0,
  startAt: '',
  endAt: '',
  items: [] as Array<{ productId: string; productCode: string; productName: string; requiredQuantity: number }>,
})

// Product search states
const productSearchQuery = ref('')
const searchResult = ref<ProductDto[]>([])
const searchLoading = ref(false)

const promotionTypes = [
  { title: 'Theo đơn hàng', value: 'Order' },
  { title: 'Theo sản phẩm', value: 'Product' },
]

const discountTypes = [
  { title: 'Phần trăm (%)', value: 'Percent' },
  { title: 'Số tiền cố định (đ)', value: 'FixedAmount' },
]

const formatDiscountValue = (promotion: PromotionDto) => {
  if (promotion.discountType === 'Percent')
    return `${promotion.discountValue}%`

  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(promotion.discountValue)
}

const formatDate = (value: string) => {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    }).format(date)
}

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
  try {
    await togglePromotionActive(promo.id)
    await loadPromotions()
  } catch (error: any) {
    alert('Lỗi đổi trạng thái khuyến mãi: ' + error.message)
  }
}

const searchProducts = async () => {
  if (!productSearchQuery.value.trim()) {
    searchResult.value = []
    return
  }
  searchLoading.value = true
  try {
    const res = await getProducts({ search: productSearchQuery.value, pageSize: 10 })
    searchResult.value = res.items
  } catch (error) {
    console.error(error)
  } finally {
    searchLoading.value = false
  }
}

const addProductToPromo = (prod: ProductDto) => {
  const existing = promoForm.value.items.find(item => item.productId === prod.id)
  if (existing) return
  promoForm.value.items.push({
    productId: prod.id,
    productCode: prod.code,
    productName: prod.name,
    requiredQuantity: 1,
  })
  productSearchQuery.value = ''
  searchResult.value = []
}

const removeProductFromPromo = (productId: string) => {
  promoForm.value.items = promoForm.value.items.filter(item => item.productId !== productId)
}

const openCreateDialog = () => {
  editingPromotion.value = null
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
  promotionDialog.value = true
}

const handleSavePromotion = async () => {
  if (!promoForm.value.code || !promoForm.value.name) {
    alert('Vui lòng nhập đầy đủ Mã và Tên chương trình.')
    return
  }
  loading.value = true
  try {
    const payload = {
      code: promoForm.value.code,
      name: promoForm.value.name,
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
    } else {
      await createPromotion(payload)
    }
    promotionDialog.value = false
    await loadPromotions()
  } catch (error: any) {
    alert('Lỗi lưu chương trình khuyến mãi: ' + error.message)
  } finally {
    loading.value = false
  }
}

const promoActions = (promo: PromotionDto): ActionMenuItem[] => [
  { label: 'Chỉnh sửa', icon: 'ri-edit-line', color: 'primary', handler: () => openEditDialog(promo) },
  { label: promo.isActive ? 'Tắt kích hoạt' : 'Kích hoạt', icon: promo.isActive ? 'ri-toggle-line' : 'ri-toggle-fill', color: promo.isActive ? 'warning' : 'success', handler: () => handleToggleActive(promo) },
]

onMounted(loadPromotions)
</script>

<template>
  <RetailPageHeader
    eyebrow="Khuyến mãi"
    title="Chương trình Khuyến mãi"
    subtitle="Quản lý mã giảm giá theo đơn hàng, theo sản phẩm, cấu hình chiết khấu và theo dõi trạng thái hoạt động."
  >
    <template #actions>
      <VBtn
        prepend-icon="ri-coupon-3-line"
        @click="openCreateDialog"
      >
        Tạo khuyến mãi
      </VBtn>
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

  <VCard class="retail-panel-card">
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
          <th>Mã khuyến mãi</th>
          <th>Tên chương trình</th>
          <th>Phạm vi</th>
          <th>Loại chiết khấu</th>
          <th>Giá trị giảm</th>
          <th>Hiệu lực từ</th>
          <th>Đến ngày</th>
          <th class="text-center">Trạng thái</th>
          <th class="text-center" style="width: 60px;">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="promotion in promotions"
          :key="promotion.id"
          class="hover-row"
        >
          <td class="font-weight-bold text-primary">
            {{ promotion.code }}
          </td>
          <td>{{ promotion.name }}</td>
          <td>
            <RetailStatusBadge :status="promotion.promotionType === 'Order' ? 'Đơn hàng' : 'Sản phẩm'" />
          </td>
          <td>
            {{ promotion.discountType === 'Percent' ? 'Phần trăm (%)' : 'Số tiền cố định' }}
          </td>
          <td class="font-weight-bold text-success">{{ formatDiscountValue(promotion) }}</td>
          <td>{{ formatDate(promotion.startAt) }}</td>
          <td>{{ formatDate(promotion.endAt) }}</td>
          <td class="text-center">
            <RetailStatusBadge
              :status="promotion.isActive ? 'Đang chạy' : 'Tạm dừng'"
              dot
            />
          </td>
          <td
            class="text-center"
            @click.stop
          >
            <RetailActionMenu :items="promoActions(promotion)" />
          </td>
        </tr>
        <RetailEmptyState
          v-if="!loading && !promotions.length"
          :colspan="9"
          icon="ri-coupon-3-line"
          title="Chưa có chương trình khuyến mãi nào"
          subtitle="Tạo chương trình khuyến mãi mới để bắt đầu."
          action-label="Tạo khuyến mãi"
        />
      </tbody>
    </VTable>
  </VCard>

  <!-- Create/Edit Promotion Dialog -->
  <VDialog
    v-model="promotionDialog"
    max-width="800"
    persistent
  >
    <VCard>
      <VCardTitle>{{ editingPromotion ? 'Cập nhật chương trình khuyến mãi' : 'Tạo khuyến mãi mới' }}</VCardTitle>
      <VCardText>
        <VRow>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="promoForm.code"
              label="Mã khuyến mãi *"
              placeholder="VD: KHUYENMAI20"
              :disabled="!!editingPromotion"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="promoForm.name"
              label="Tên chương trình *"
              placeholder="VD: Giảm giá mùa hè"
            />
          </VCol>
          <VCol cols="12">
            <VTextField
              v-model="promoForm.description"
              label="Mô tả chương trình"
            />
          </VCol>

          <VCol cols="12" sm="6">
            <VSelect
              v-model="promoForm.promotionType"
              label="Phạm vi áp dụng"
              :items="promotionTypes"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VSelect
              v-model="promoForm.discountType"
              label="Hình thức chiết khấu"
              :items="discountTypes"
            />
          </VCol>

          <VCol cols="12" sm="6">
            <VTextField
              v-model.number="promoForm.discountValue"
              type="number"
              label="Giá trị giảm giá *"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model.number="promoForm.minimumOrderAmount"
              type="number"
              label="Đơn hàng tối thiểu (đ)"
            />
          </VCol>

          <VCol cols="12" sm="6">
            <VTextField
              v-model="promoForm.startAt"
              type="date"
              label="Ngày bắt đầu *"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="promoForm.endAt"
              type="date"
              label="Ngày kết thúc *"
            />
          </VCol>
        </VRow>

        <!-- Product selection if PromotionType is 'Product' -->
        <div v-if="promoForm.promotionType === 'Product'" class="mt-4">
          <hr class="my-4" />
          <h4 class="text-subtitle-1 font-weight-bold mb-2">Sản phẩm áp dụng khuyến mãi</h4>

          <div class="d-flex gap-3 align-center mb-4">
            <VTextField
              v-model="productSearchQuery"
              label="Tìm sản phẩm áp dụng..."
              prepend-inner-icon="ri-search-line"
              hide-details
              @keyup.enter="searchProducts"
            />
            <VBtn :loading="searchLoading" color="primary" @click="searchProducts">Tìm</VBtn>
          </div>

          <VList
            v-if="searchResult.length"
            class="border rounded mb-4"
            style="max-height: 180px; overflow-y: auto;"
          >
            <VListItem
              v-for="prod in searchResult"
              :key="prod.id"
              class="cursor-pointer"
              @click="addProductToPromo(prod)"
            >
              <VListItemTitle class="font-weight-bold">{{ prod.name }} ({{ prod.code }})</VListItemTitle>
              <VListItemSubtitle>Giá bán: {{ formatDiscountValue({ discountType: 'FixedAmount', discountValue: prod.sellPrice } as any) }}</VListItemSubtitle>
            </VListItem>
          </VList>

          <VTable class="retail-table mb-4">
            <thead>
              <tr>
                <th>Sản phẩm</th>
                <th>Mã hàng</th>
                <th style="width: 150px;">Số lượng tối thiểu mua</th>
                <th class="text-center">Xóa</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in promoForm.items" :key="item.productId">
                <td class="font-weight-bold">{{ item.productName }}</td>
                <td>{{ item.productCode }}</td>
                <td>
                  <VTextField
                    v-model.number="item.requiredQuantity"
                    type="number"
                    density="compact"
                    hide-details
                    min="1"
                  />
                </td>
                <td class="text-center">
                  <VBtn
                    icon="ri-delete-bin-line"
                    variant="text"
                    size="small"
                    color="error"
                    @click="removeProductFromPromo(item.productId)"
                  />
                </td>
              </tr>
              <tr v-if="!promoForm.items.length">
                <td colspan="4" class="text-center text-medium-emphasis py-6">
                  Chưa chọn sản phẩm nào cho chương trình này. Mặc định sẽ không có tác dụng.
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="promotionDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSavePromotion"
        >
          Lưu lại
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
