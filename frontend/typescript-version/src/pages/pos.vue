<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'

import {
  type ProductDto,
  getProducts,
} from '@/services/productInventoryApi'
import {
  type CashShiftDto,
  type CustomerDto,
  type PromotionDto,
  closeShift,
  createOrder,
  getActivePromotions,
  getCurrentShift,
  getCustomers,
  openShift,
} from '@/services/orderSalesApi'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const products = ref<ProductDto[]>([])
const customerList = ref<CustomerDto[]>([])
const promotionList = ref<PromotionDto[]>([])
const loading = ref(false)
const submitting = ref(false)
const shiftOpen = ref(false)
const currentShift = ref<CashShiftDto | null>(null)
const openShiftDialog = ref(false)
const closeShiftDialog = ref(false)
const openingCashInput = ref(0)
const actualCashInput = ref(0)
const shiftNoteInput = ref('')
const errorMessage = ref('')
const successMessage = ref('')

interface CartItem {
  product: ProductDto
  quantity: number
}

const cart = ref<CartItem[]>([])
const searchQuery = ref('')
const selectedCategory = ref('Tất cả')
const barcodeDialog = ref(false)
const barcodeInput = ref('')
const barcodeInputRef = ref<HTMLInputElement | null>(null)
const barcodeMessage = ref('')
const barcodeMessageType = ref<'success' | 'error' | 'info'>('info')

const categories = computed(() => {
  const values = new Set(products.value.map(product => product.categoryName).filter(Boolean))

  return ['Tất cả', ...values]
})

const filteredProducts = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return products.value.filter(product => {
    const matchesCategory = selectedCategory.value === 'Tất cả' || product.categoryName === selectedCategory.value

    const matchesQuery = !query
      || product.name.toLowerCase().includes(query)
      || product.code.toLowerCase().includes(query)
      || product.barcode?.toLowerCase().includes(query)

    return matchesCategory && matchesQuery
  })
})

const getAvailableQuantity = (product: ProductDto) => Math.max(product.quantityOnHand - product.quantityReserved, 0)

const getStockColor = (product: ProductDto) => {
  const available = getAvailableQuantity(product)

  if (available <= 0)
    return 'error'

  if (available <= 5)
    return 'warning'

  return 'success'
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

type PaymentMethodValue = 'Tiền mặt' | 'Chuyển khoản' | 'Thẻ' | 'Ví điện tử' | 'Ghi nợ'

const selectedCustomerId = ref<string | null>(null)
const selectedPromotion = ref('')
const selectedPaymentMethod = ref<PaymentMethodValue>('Tiền mặt')
const cashReceived = ref(0)

const paymentMethods: {
  value: PaymentMethodValue
  label: string
  icon: string
}[] = [
  { value: 'Tiền mặt', label: 'Tiền mặt', icon: 'ri-cash-line' },
  { value: 'Chuyển khoản', label: 'Chuyển khoản', icon: 'ri-bank-card-line' },
  { value: 'Thẻ', label: 'Thẻ', icon: 'ri-bank-card-2-line' },
  { value: 'Ví điện tử', label: 'Ví điện tử', icon: 'ri-wallet-3-line' },
  { value: 'Ghi nợ', label: 'Ghi nợ', icon: 'ri-file-list-3-line' },
]

const subtotal = computed(() => cart.value.reduce((sum, item) => sum + item.product.sellPrice * item.quantity, 0))

const discount = computed(() => {
  const promo = promotionList.value.find(p => p.code === selectedPromotion.value)
  if (!promo)
    return 0
  if (promo.discountType === 'Percent')
    return Math.round(subtotal.value * promo.discountValue / 100)

  return promo.discountValue
})

const total = computed(() => Math.max(subtotal.value - discount.value, 0))
const changeDue = computed(() => Math.max(cashReceived.value - total.value, 0))
const isCashPayment = computed(() => selectedPaymentMethod.value === 'Tiền mặt')
const isDeferredConfirmation = computed(() => ['Chuyển khoản', 'Thẻ', 'Ví điện tử'].includes(selectedPaymentMethod.value))
const isDebtPayment = computed(() => selectedPaymentMethod.value === 'Ghi nợ')
const paymentActionLabel = computed(() => (isDebtPayment.value ? 'Ghi nhận công nợ' : 'Thanh toán'))

const shiftSummaryRows = computed(() => {
  if (!currentShift.value)
    return []

  return [
    {
      label: 'Tiền mặt đầu ca',
      value: formatCurrency(currentShift.value.openingCash),
    },
    {
      label: 'Tiền mặt kỳ vọng',
      value: formatCurrency(currentShift.value.expectedCash),
      class: 'text-primary',
    },
    {
      label: 'Tiền mặt thực tế',
      value: currentShift.value.actualCash === null ? 'Chưa chốt' : formatCurrency(currentShift.value.actualCash),
    },
    {
      label: 'Chênh lệch',
      value: formatCurrency(currentShift.value.variance),
      class: currentShift.value.variance === 0 ? 'text-success' : 'text-warning',
    },
  ]
})

const addToCart = (product: ProductDto) => {
  const existing = cart.value.find(item => item.product.id === product.id)
  if (existing)
    existing.quantity++
  else
    cart.value.push({ product, quantity: 1 })
}

const increaseCartItem = (item: CartItem) => {
  item.quantity++
}

const removeCartItem = (productId: string) => {
  cart.value = cart.value.filter(item => item.product.id !== productId)
}

const decreaseCartItem = (item: CartItem) => {
  if (item.quantity > 1) {
    item.quantity--

    return
  }

  removeCartItem(item.product.id)
}

const startOpenShift = () => {
  openingCashInput.value = 0
  shiftNoteInput.value = ''
  openShiftDialog.value = true
}

const confirmOpenShift = async () => {
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const shift = await openShift({
      openingCash: Number(openingCashInput.value) || 0,
      note: shiftNoteInput.value || 'Mở ca từ POS',
    })

    currentShift.value = shift
    shiftOpen.value = true
    openShiftDialog.value = false
    successMessage.value = 'Mở ca bán hàng thành công.'
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể mở ca bán hàng.'
  }
}

const startCloseShift = async () => {
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const shift = await getCurrentShift()

    currentShift.value = shift
    actualCashInput.value = shift?.expectedCash || 0
    shiftNoteInput.value = ''
    closeShiftDialog.value = true
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải thông tin ca hiện tại.'
  }
}

const confirmCloseShift = async () => {
  if (!currentShift.value)
    return

  errorMessage.value = ''
  successMessage.value = ''

  try {
    await closeShift(currentShift.value.id, {
      actualCash: Number(actualCashInput.value) || 0,
      note: shiftNoteInput.value || null,
    })

    currentShift.value = null
    shiftOpen.value = false
    closeShiftDialog.value = false
    successMessage.value = 'Đóng ca bán hàng thành công.'
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể đóng ca bán hàng.'
  }
}

const handlePayment = async () => {
  if (cart.value.length === 0)
    return

  const customer = customerList.value.find(c => c.id === selectedCustomerId.value)

  if (!customer) {
    errorMessage.value = 'Vui lòng chọn khách hàng trước khi tạo đơn.'

    return
  }

  submitting.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createOrder({
      customerId: customer.id,
      customerName: customer.fullName,
      createdBy: authStore.user?.id ?? '',
      createdByName: authStore.user?.fullName ?? 'POS',
      paymentMethod: selectedPaymentMethod.value,
      promotionCode: selectedPromotion.value || null,
      note: null,
      items: cart.value.map(item => ({
        productId: item.product.id,
        productCode: item.product.code,
        productName: item.product.name,
        unitName: item.product.unitName,
        unitPrice: item.product.sellPrice,
        quantity: item.quantity,
        discountPercent: 0,
      })),
    })

    cart.value = []
    cashReceived.value = 0
    selectedPromotion.value = ''
    successMessage.value = 'Tạo đơn hàng thành công.'
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tạo đơn hàng.'
  }
  finally {
    submitting.value = false
  }
}

const loadPosData = async () => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const [productResult, customerResult, promotionResult, shift] = await Promise.all([
      getProducts({ pageSize: 50 }),
      getCustomers({ pageSize: 50 }),
      getActivePromotions(),
      getCurrentShift().catch(() => null),
    ])

    products.value = productResult.items
    customerList.value = customerResult.items
    promotionList.value = promotionResult
    shiftOpen.value = shift?.status === 'Open'
    currentShift.value = shift

    if (customerResult.items.length > 0)
      selectedCustomerId.value = customerResult.items[0].id
    if (promotionResult.length > 0)
      selectedPromotion.value = promotionResult[0].code
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu POS.'
  }
  finally {
    loading.value = false
  }
}

const openBarcodeScanner = () => {
  barcodeInput.value = ''
  barcodeMessage.value = ''
  barcodeDialog.value = true
  nextTick(() => {
    barcodeInputRef.value?.focus()
  })
}

const handleBarcodeScan = () => {
  const code = barcodeInput.value.trim()
  if (!code) return

  const product = products.value.find(
    p => p.barcode?.toLowerCase() === code.toLowerCase()
      || p.code.toLowerCase() === code.toLowerCase(),
  )

  if (product) {
    if (getAvailableQuantity(product) <= 0) {
      barcodeMessage.value = `"${product.name}" đã hết hàng.`
      barcodeMessageType.value = 'error'
    }
    else {
      addToCart(product)
      barcodeMessage.value = `Đã thêm "${product.name}" vào giỏ hàng.`
      barcodeMessageType.value = 'success'
    }
  }
  else {
    barcodeMessage.value = `Không tìm thấy sản phẩm với mã "${code}".`
    barcodeMessageType.value = 'error'
  }

  barcodeInput.value = ''
  nextTick(() => {
    barcodeInputRef.value?.focus()
  })
}

// Global barcode listener: focus on search field when typing starts
const handleGlobalKeydown = (e: KeyboardEvent) => {
  // Ignore if user is typing in an input/textarea or dialog is open
  const tag = (e.target as HTMLElement)?.tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return
  if (barcodeDialog.value) return

  // If it's a printable character, focus the search and let the char through
  if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
    const searchEl = document.querySelector('.pos-search-field input') as HTMLInputElement
    if (searchEl) {
      searchEl.focus()
    }
  }
}

onMounted(() => {
  loadPosData()
  window.addEventListener('keydown', handleGlobalKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Không gian bán hàng"
    title="Bán hàng POS"
    subtitle="Tìm sản phẩm bằng tên, SKU hoặc barcode; quản lý giỏ hàng và thanh toán tại quầy."
  >
    <template #actions>
      <VBtn
        v-if="!shiftOpen"
        color="success"
        prepend-icon="ri-play-circle-line"
        @click="startOpenShift"
      >
        Mở ca bán hàng
      </VBtn>
      <VBtn
        v-else
        color="warning"
        prepend-icon="ri-stop-circle-line"
        @click="startCloseShift"
      >
        Đóng ca bán hàng
      </VBtn>
    </template>
  </RetailPageHeader>

  <VAlert
    v-if="errorMessage"
    type="error"
    variant="tonal"
    density="comfortable"
    class="mb-4"
  >
    {{ errorMessage }}
  </VAlert>

  <VAlert
    v-if="successMessage"
    type="success"
    variant="tonal"
    density="comfortable"
    class="mb-4"
  >
    {{ successMessage }}
  </VAlert>

  <div class="retail-pos-shell">
    <section class="d-flex flex-column gap-4">
      <VCard>
        <VCardText>
          <VRow>
            <VCol
              cols="12"
              md="8"
            >
              <VTextField
                v-model="searchQuery"
                label="Tìm sản phẩm"
                placeholder="Nhập tên, SKU hoặc barcode"
                prepend-inner-icon="ri-search-line"
                density="comfortable"
                clearable
                class="pos-search-field"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VBtn
                color="primary"
                variant="tonal"
                block
                prepend-icon="ri-qr-scan-2-line"
                class="h-100"
                style="min-height: 56px;"
                @click="openBarcodeScanner"
              >
                Quét mã barcode
              </VBtn>
            </VCol>
          </VRow>

          <div class="d-flex flex-wrap gap-2 mt-2">
            <VChip
              v-for="category in categories"
              :key="category"
              :color="selectedCategory === category ? 'primary' : undefined"
              :variant="selectedCategory === category ? 'tonal' : 'outlined'"
              @click="selectedCategory = category"
            >
              {{ category }}
            </VChip>
          </div>
        </VCardText>
      </VCard>

      <VRow>
        <VCol
          v-if="!loading && filteredProducts.length === 0"
          cols="12"
        >
          <VCard class="retail-panel-card">
            <div class="retail-empty-state">
              <div>
                <VIcon
                  icon="ri-search-line"
                  color="primary"
                  size="34"
                  class="mb-2"
                />
                <div class="font-weight-bold text-high-emphasis mb-1">
                  Không tìm thấy sản phẩm
                </div>
                <div class="text-body-2">
                  Thử đổi từ khóa hoặc chọn danh mục khác.
                </div>
              </div>
            </div>
          </VCard>
        </VCol>

        <VCol
          v-for="product in filteredProducts"
          :key="product.id"
          cols="12"
          sm="6"
          xl="4"
        >
          <VCard class="pos-product-card h-100">
            <div
              v-if="product.imageUrl"
              class="pos-product-media"
            >
              <VImg
                :src="product.imageUrl"
                height="132"
                cover
              />
            </div>
            <div
              v-else
              class="pos-product-placeholder"
            >
              <VIcon
                icon="ri-price-tag-3-line"
                size="34"
              />
            </div>

            <VCardText>
              <div class="d-flex align-center justify-space-between ga-3 mb-3">
                <VChip
                  color="primary"
                  size="small"
                  variant="tonal"
                >
                  {{ product.categoryName }}
                </VChip>
                <VChip
                  :color="getStockColor(product)"
                  size="small"
                  variant="tonal"
                >
                  Tồn {{ getAvailableQuantity(product) }}
                </VChip>
              </div>

              <div class="text-caption text-medium-emphasis mb-1">
                {{ product.code }}
              </div>
              <h3 class="text-subtitle-1 font-weight-bold mb-2">
                {{ product.name }}
              </h3>
              <div class="text-caption text-medium-emphasis mb-4">
                {{ product.unitName }} · Barcode {{ product.barcode || '—' }}
              </div>
              <div class="d-flex align-center justify-space-between ga-3">
                <span class="text-primary font-weight-bold">{{ formatCurrency(product.sellPrice) }}</span>
                <VBtn
                  size="small"
                  color="primary"
                  variant="tonal"
                  prepend-icon="ri-add-line"
                  :disabled="getAvailableQuantity(product) <= 0"
                  @click="addToCart(product)"
                >
                  Thêm
                </VBtn>
              </div>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>
    </section>

    <aside>
      <VCard class="cart-panel">
        <VCardItem>
          <VCardTitle>Giỏ hàng</VCardTitle>
          <VCardSubtitle>{{ cart.length }} mặt hàng đang chọn</VCardSubtitle>
        </VCardItem>

        <VDivider />

        <div class="cart-scroll-area">
          <VCardText class="cart-section">
            <div
              v-if="cart.length === 0"
              class="cart-empty"
            >
              <VIcon
                icon="ri-shopping-basket-2-line"
                size="34"
                color="primary"
                class="mb-2"
              />
              <div class="font-weight-bold text-high-emphasis mb-1">
                Giỏ hàng đang trống
              </div>
              <div class="text-body-2 text-medium-emphasis">
                Chọn sản phẩm ở bên trái để bắt đầu tạo đơn.
              </div>
            </div>

            <template v-else>
              <div
                v-for="item in cart"
                :key="item.product.id"
                class="cart-item"
              >
                <div class="min-w-0">
                  <div class="cart-item-title">
                    {{ item.product.name }}
                  </div>
                  <div class="text-body-2 text-medium-emphasis">
                    {{ formatCurrency(item.product.sellPrice) }} / {{ item.product.unitName }}
                  </div>
                </div>
                <div class="cart-item-actions">
                  <div class="cart-quantity-control">
                    <VBtn
                      icon="ri-subtract-line"
                      size="x-small"
                      variant="tonal"
                      @click="decreaseCartItem(item)"
                    />
                    <strong>{{ item.quantity }}</strong>
                    <VBtn
                      icon="ri-add-line"
                      size="x-small"
                      variant="tonal"
                      @click="increaseCartItem(item)"
                    />
                  </div>
                  <strong class="cart-item-total">{{ formatCurrency(item.product.sellPrice * item.quantity) }}</strong>
                  <VBtn
                    icon="ri-delete-bin-line"
                    size="x-small"
                    color="error"
                    variant="text"
                    @click="removeCartItem(item.product.id)"
                  />
                </div>
              </div>
            </template>
          </VCardText>

          <VDivider />

          <VCardText class="cart-form">
            <div class="cart-section-label">
              Thông tin đơn
            </div>

            <div class="cart-field-stack">
              <VSelect
                v-model="selectedCustomerId"
                label="Khách hàng"
                :items="customerList"
                item-title="fullName"
                item-value="id"
                density="comfortable"
                hide-details="auto"
              />
              <VSelect
                v-model="selectedPromotion"
                label="Khuyến mãi"
                :items="promotionList.map(p => p.code)"
                density="comfortable"
                hide-details="auto"
              />
            </div>
          </VCardText>
        </div>

        <VDivider />

        <VCardText class="cart-form cart-checkout-footer">
          <div class="checkout-controls">
            <VSelect
              v-model="selectedPaymentMethod"
              label="Hình thức thanh toán"
              :items="paymentMethods"
              item-title="label"
              item-value="value"
              density="comfortable"
              hide-details="auto"
            />

            <VTextField
              v-if="isCashPayment"
              v-model.number="cashReceived"
              label="Khách đưa"
              type="number"
              prefix="đ"
              density="comfortable"
              hide-details="auto"
            />

            <VAlert
              v-else-if="isDeferredConfirmation"
              color="info"
              variant="tonal"
              density="compact"
              icon="ri-time-line"
            >
              Chờ xác nhận thanh toán trước khi hoàn tất đơn.
            </VAlert>

            <VAlert
              v-else-if="isDebtPayment"
              color="warning"
              variant="tonal"
              density="compact"
              icon="ri-error-warning-line"
            >
              Ghi nợ chỉ áp dụng cho khách hàng đã có hồ sơ.
            </VAlert>
          </div>

          <div class="cart-summary">
            <div class="summary-line">
              <span>Tạm tính</span>
              <strong>{{ formatCurrency(subtotal) }}</strong>
            </div>
            <div class="summary-line text-success">
              <span>Giảm giá</span>
              <strong>-{{ formatCurrency(discount) }}</strong>
            </div>
            <div
              v-if="isCashPayment"
              class="summary-line"
            >
              <span>Tiền thừa</span>
              <strong>{{ formatCurrency(changeDue) }}</strong>
            </div>
          </div>

          <div class="d-flex justify-space-between text-h5 font-weight-bold total-line">
            <span>Cần trả</span>
            <span class="text-primary">{{ formatCurrency(total) }}</span>
          </div>

          <VBtn
            block
            size="large"
            class="payment-action"
            prepend-icon="ri-bank-card-line"
            :loading="submitting"
            :disabled="cart.length === 0"
            @click="handlePayment"
          >
            {{ paymentActionLabel }}
          </VBtn>
        </VCardText>
      </VCard>
    </aside>
  </div>

  <!-- Open Shift Dialog -->
  <VDialog
    v-model="openShiftDialog"
    max-width="500"
  >
    <VCard>
      <VCardTitle>Mở ca bán hàng mới</VCardTitle>
      <VCardText>
        <VTextField
          v-model.number="openingCashInput"
          type="number"
          label="Tiền mặt đầu ca (đ)"
          placeholder="0"
          class="mb-4"
        />
        <VTextarea
          v-model="shiftNoteInput"
          label="Ghi chú mở ca"
          rows="3"
        />
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="openShiftDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="confirmOpenShift"
        >
          Xác nhận mở ca
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Close Shift Dialog -->
  <VDialog
    v-model="closeShiftDialog"
    max-width="550"
  >
    <VCard v-if="currentShift">
      <VCardTitle>Xác nhận đóng ca làm việc</VCardTitle>
      <VCardText>
        <div class="mb-4">
          <div><strong>Mã ca:</strong> {{ currentShift.shiftCode }}</div>
          <div><strong>Thu ngân:</strong> {{ currentShift.cashierName }}</div>
          <div><strong>Giờ mở ca:</strong> {{ new Date(currentShift.openedAt).toLocaleString('vi-VN') }}</div>
        </div>
        <hr class="my-4">
        <VRow class="mb-4">
          <VCol
            v-for="row in shiftSummaryRows"
            :key="row.label"
            cols="12"
            sm="6"
          >
            <div class="text-caption text-medium-emphasis mb-1">
              {{ row.label }}
            </div>
            <div
              class="text-subtitle-1 font-weight-bold"
              :class="row.class"
            >
              {{ row.value }}
            </div>
          </VCol>
        </VRow>

        <hr class="my-4">

        <VTextField
          v-model.number="actualCashInput"
          type="number"
          label="Tiền mặt thực tế trong két (đ) *"
          placeholder="Nhập số tiền kiểm thực tế..."
          class="mb-4"
          required
        />

        <VTextarea
          v-model="shiftNoteInput"
          label="Ghi chú đóng ca (nếu có)"
          rows="3"
        />
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="closeShiftDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="warning"
          @click="confirmCloseShift"
        >
          Xác nhận đóng ca
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Barcode Scanner Dialog -->
  <VDialog
    v-model="barcodeDialog"
    max-width="500"
  >
    <VCard>
      <VCardTitle class="d-flex align-center gap-2">
        <VIcon
          icon="ri-qr-scan-2-line"
          color="primary"
        />
        Quét mã Barcode
      </VCardTitle>
      <VCardText>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Dùng máy quét barcode hoặc nhập mã tay. Nhấn Enter để tìm sản phẩm.
        </p>
        <VTextField
          ref="barcodeInputRef"
          v-model="barcodeInput"
          label="Mã Barcode / SKU"
          placeholder="Quét hoặc nhập mã barcode..."
          prepend-inner-icon="ri-barcode-line"
          density="comfortable"
          autofocus
          @keyup.enter="handleBarcodeScan"
        />
        <VAlert
          v-if="barcodeMessage"
          :type="barcodeMessageType"
          variant="tonal"
          density="compact"
          class="mt-4"
        >
          {{ barcodeMessage }}
        </VAlert>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="barcodeDialog = false"
        >
          Đóng
        </VBtn>
        <VBtn
          color="primary"
          prepend-icon="ri-search-line"
          @click="handleBarcodeScan"
        >
          Tìm sản phẩm
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>

<style scoped>
.pos-product-card {
  overflow: hidden;
  transition: transform 180ms ease, border-color 180ms ease, box-shadow 180ms ease;
}

.pos-product-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.28);
  box-shadow: 0 16px 34px rgba(15, 23, 42, 0.1) !important;
  transform: translateY(-3px);
}

.pos-product-media {
  background: rgba(var(--v-theme-primary), 0.04);
}

.pos-product-placeholder {
  display: grid;
  block-size: 132px;
  place-items: center;
  background:
    radial-gradient(circle at 50% 0, rgba(var(--v-theme-primary), 0.14), transparent 40%),
    rgba(var(--v-theme-primary), 0.05);
  color: rgb(var(--v-theme-primary));
}

.cart-panel {
  display: flex;
  flex-direction: column;
  max-block-size: calc(100dvh - 120px);
  position: sticky;
  inset-block-start: 88px;
  overflow: visible;
}

.cart-scroll-area {
  flex: 1 1 auto;
  min-block-size: 120px;
  max-block-size: 40vh;
  overflow-y: auto;
  overflow-x: hidden;
}

.cart-checkout-footer {
  flex: 0 0 auto;
  background: rgb(var(--v-theme-surface));
}

.cart-section {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.cart-item {
  display: grid;
  align-items: center;
  gap: 12px;
  grid-template-columns: minmax(0, 1fr) auto;
}

.cart-empty {
  display: grid;
  min-block-size: 180px;
  place-items: center;
  text-align: center;
}

.cart-item-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}

.cart-quantity-control {
  display: inline-flex;
  align-items: center;
  padding: 4px;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 999px;
  background: rgba(var(--v-theme-on-surface), 0.03);
  gap: 8px;
}

.cart-item-title {
  color: rgba(var(--v-theme-on-surface), 0.9);
  font-weight: 600;
  line-height: 1.35;
}

.cart-item-total {
  color: rgba(var(--v-theme-on-surface), 0.9);
  white-space: nowrap;
}

.cart-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.cart-field-stack {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.cart-section-label {
  color: rgba(var(--v-theme-on-surface), 0.72);
  font-size: 0.8125rem;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

.checkout-controls {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.cart-summary {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.summary-line {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.total-line {
  border-block-start: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  padding-block-start: 16px;
}

.payment-action {
  min-block-size: 48px;
}
</style>
