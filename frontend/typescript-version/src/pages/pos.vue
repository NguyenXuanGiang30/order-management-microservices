<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import {
  type ProductDto,
  getProducts,
} from '@/services/productInventoryApi'
import {
  type CustomerDto,
  type PromotionDto,
  type CashShiftDto,
  createOrder,
  getActivePromotions,
  getCustomers,
  getCurrentShift,
  openShift,
  closeShift,
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

interface CartItem {
  product: ProductDto
  quantity: number
}

const cart = ref<CartItem[]>([])

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

type PaymentMethodValue = 'cash' | 'transfer' | 'card' | 'wallet' | 'debt'

const selectedCustomer = ref('')
const selectedPromotion = ref('')
const selectedPaymentMethod = ref<PaymentMethodValue>('cash')
const cashReceived = ref(0)

const paymentMethods: {
  value: PaymentMethodValue
  label: string
  icon: string
}[] = [
  { value: 'cash', label: 'Tiền mặt', icon: 'ri-cash-line' },
  { value: 'transfer', label: 'Chuyển khoản', icon: 'ri-bank-card-line' },
  { value: 'card', label: 'Thẻ', icon: 'ri-bank-card-2-line' },
  { value: 'wallet', label: 'Ví điện tử', icon: 'ri-wallet-3-line' },
  { value: 'debt', label: 'Ghi nợ', icon: 'ri-file-list-3-line' },
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
const isCashPayment = computed(() => selectedPaymentMethod.value === 'cash')
const isDeferredConfirmation = computed(() => ['transfer', 'card', 'wallet'].includes(selectedPaymentMethod.value))
const isDebtPayment = computed(() => selectedPaymentMethod.value === 'debt')
const paymentActionLabel = computed(() => (isDebtPayment.value ? 'Ghi nhận công nợ' : 'Thanh toán'))

const addToCart = (product: ProductDto) => {
  const existing = cart.value.find(item => item.product.id === product.id)
  if (existing)
    existing.quantity++
  else
    cart.value.push({ product, quantity: 1 })
}

const startOpenShift = () => {
  openingCashInput.value = 0
  shiftNoteInput.value = ''
  openShiftDialog.value = true
}

const confirmOpenShift = async () => {
  try {
    const shift = await openShift({
      openingCash: Number(openingCashInput.value) || 0,
      note: shiftNoteInput.value || 'Mở ca từ POS',
    })
    currentShift.value = shift
    shiftOpen.value = true
    openShiftDialog.value = false
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Không thể mở ca bán hàng.'
  }
}

const startCloseShift = async () => {
  try {
    const shift = await getCurrentShift()
    currentShift.value = shift
    actualCashInput.value = shift?.expectedCash || 0
    shiftNoteInput.value = ''
    closeShiftDialog.value = true
  } catch (error: any) {
    errorMessage.value = error.message || 'Không thể tải thông tin ca hiện tại.'
  }
}

const confirmCloseShift = async () => {
  if (!currentShift.value) return
  try {
    await closeShift(currentShift.value.id, {
      actualCash: Number(actualCashInput.value) || 0,
      note: shiftNoteInput.value || null,
    })
    currentShift.value = null
    shiftOpen.value = false
    closeShiftDialog.value = false
    alert('Đóng ca bán hàng thành công!')
  } catch (error: any) {
    alert('Lỗi đóng ca: ' + error.message)
  }
}

const handlePayment = async () => {
  if (cart.value.length === 0)
    return

  const customer = customerList.value.find(c => c.fullName === selectedCustomer.value)

  submitting.value = true
  errorMessage.value = ''

  try {
    await createOrder({
      customerId: customer?.id ?? '',
      customerName: (customer?.fullName ?? selectedCustomer.value) || 'Khách lẻ',
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
      selectedCustomer.value = customerResult.items[0].fullName
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

onMounted(loadPosData)
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
                label="Tìm sản phẩm"
                placeholder="Nhập tên, SKU hoặc quét barcode"
                prepend-inner-icon="ri-search-line"
                density="comfortable"
              />
            </VCol>
            <VCol
              cols="12"
              md="4"
            >
              <VBtn
                block
                height="48"
                prepend-icon="ri-qr-scan-2-line"
              >
                Quét barcode
              </VBtn>
            </VCol>
          </VRow>

          <div class="d-flex flex-wrap gap-2 mt-2">
            <VChip
              color="primary"
              variant="tonal"
            >
              Tất cả
            </VChip>
            <VChip variant="outlined">
              Thời trang
            </VChip>
            <VChip variant="outlined">
              Giày dép
            </VChip>
            <VChip variant="outlined">
              Phụ kiện
            </VChip>
            <VChip variant="outlined">
              Điện tử
            </VChip>
          </div>
        </VCardText>
      </VCard>

      <VRow>
        <VCol
          v-for="product in products"
          :key="product.id"
          cols="12"
          sm="6"
          xl="4"
        >
          <VCard class="h-100">
            <VCardText>
              <div class="d-flex align-start justify-space-between mb-4">
                <VAvatar
                  color="primary"
                  variant="tonal"
                  rounded="lg"
                >
                  <VIcon icon="ri-price-tag-3-line" />
                </VAvatar>
                <VChip
                  :color="product.quantityOnHand <= product.quantityReserved ? 'warning' : 'success'"
                  size="small"
                  variant="tonal"
                >
                  Tồn {{ product.quantityOnHand }}
                </VChip>
              </div>
              <div class="text-caption text-medium-emphasis mb-1">
                {{ product.code }}
              </div>
              <h3 class="text-subtitle-1 font-weight-bold mb-2">
                {{ product.name }}
              </h3>
              <div class="d-flex align-center justify-space-between">
                <span class="text-primary font-weight-bold">{{ formatCurrency(product.sellPrice) }}</span>
                <VBtn
                  size="small"
                  variant="tonal"
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
              v-for="item in cart"
              :key="item.product.id"
              class="cart-item"
            >
              <div>
                <div class="cart-item-title">
                  {{ item.product.name }}
                </div>
                <div class="text-body-2 text-medium-emphasis">
                  {{ item.quantity }} x {{ formatCurrency(item.product.sellPrice) }}
                </div>
              </div>
              <strong class="cart-item-total">{{ formatCurrency(item.product.sellPrice * item.quantity) }}</strong>
            </div>
          </VCardText>

          <VDivider />

          <VCardText class="cart-form">
            <div class="cart-section-label">
              Thông tin đơn
            </div>

            <div class="cart-field-stack">
              <VSelect
                v-model="selectedCustomer"
                label="Khách hàng"
                :items="customerList.map(c => c.fullName)"
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
        <hr class="my-4" />
        <VRow class="mb-4">
          <VCol cols="6">Tiền mặt đầu ca:</VCol>
          <VCol cols="6" class="text-right font-weight-bold">{{ formatCurrency(currentShift.openingCash) }}</VCol>

          <VCol cols="6">Doanh thu tiền mặt:</VCol>
          <VCol cols="6" class="text-right font-weight-bold text-success">{{ formatCurrency(currentShift.totalCashSales) }}</VCol>

          <VCol cols="6">Doanh thu chuyển khoản:</VCol>
          <VCol cols="6" class="text-right font-weight-bold">{{ formatCurrency(currentShift.totalTransferSales) }}</VCol>

          <VCol cols="6">Doanh thu thẻ:</VCol>
          <VCol cols="6" class="text-right font-weight-bold">{{ formatCurrency(currentShift.totalCardSales) }}</VCol>

          <VCol cols="6">Doanh thu ghi nợ:</VCol>
          <VCol cols="6" class="text-right font-weight-bold text-error">{{ formatCurrency(currentShift.totalDebtSales) }}</VCol>

          <VCol cols="6" class="text-h6">Tổng tiền mặt kỳ vọng:</VCol>
          <VCol cols="6" class="text-right text-h6 font-weight-bold text-primary">{{ formatCurrency(currentShift.expectedCash) }}</VCol>
        </VRow>

        <hr class="my-4" />

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
</template>

<style scoped>
.cart-panel {
  display: flex;
  flex-direction: column;
  max-block-size: calc(100dvh - 220px);
  position: sticky;
  inset-block-start: 88px;
  overflow: hidden;
}

.cart-scroll-area {
  flex: 1 1 auto;
  min-block-size: 0;
  overflow: auto;
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
  align-items: start;
  gap: 12px;
  grid-template-columns: minmax(0, 1fr) auto;
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
