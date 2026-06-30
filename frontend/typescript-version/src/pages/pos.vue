<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

import {
  type ProductDto,
  getProductByBarcode,
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

import { getApiBaseUrl } from '@/services/authApi'
import { useAuthStore } from '@/stores/auth'
import { printInvoice } from '@/utils/printInvoice'

type PaymentMethodValue =
  | 'Tiền mặt'
  | 'Chuyển khoản'
  | 'Thẻ'
  | 'Ví điện tử'
  | 'Ghi nợ'
  | 'Hỗn hợp'

interface CartItem {
  product: ProductDto
  quantity: number
}

const authStore = useAuthStore()

const products = ref<ProductDto[]>([])
const customerList = ref<CustomerDto[]>([])
const promotionList = ref<PromotionDto[]>([])
const cart = ref<CartItem[]>([])

const loading = ref(false)
const submitting = ref(false)

const errorMessage = ref('')
const successMessage = ref('')

const shiftOpen = ref(false)
const currentShift = ref<CashShiftDto | null>(null)
const openShiftDialog = ref(false)
const closeShiftDialog = ref(false)
const openingCashInput = ref(0)
const actualCashInput = ref(0)
const shiftNoteInput = ref('')

const searchQuery = ref('')
const selectedCategory = ref('Tất cả')
const currentPage = ref(1)
const productsPerPage = ref(12)
const productsPerPageOptions = [8, 12, 16, 24]

const selectedCustomerId = ref<string | null>(null)
const selectedPromotion = ref('')
const selectedPaymentMethod = ref<PaymentMethodValue>('Tiền mặt')
const cashReceived = ref(0)

const lastCreatedOrderId = ref<string | null>(null)

const barcodeDialog = ref(false)
const barcodeInput = ref('')
const barcodeInputRef = ref<any>(null)
const barcodeMessage = ref('')
const barcodeMessageType = ref<'success' | 'error' | 'info'>('info')

const vietqrDialog = ref(false)
const qrImageUrl = ref('')
const qrCountdown = ref(10)

let qrTimer: ReturnType<typeof setInterval> | null = null

const splitAmounts = ref({
  cash: 0,
  transfer: 0,
  card: 0,
  ewallet: 0,
  debt: 0,
})

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
    { value: 'Hỗn hợp', label: 'Hỗn hợp', icon: 'ri-split-cells-vertical' },
  ]

const printLastInvoice = async () => {
  if (lastCreatedOrderId.value)
    await printInvoice(lastCreatedOrderId.value)
}

const resolveImageUrl = (path: string | null) => {
  if (!path)
    return ''

  if (path.startsWith('http'))
    return path

  return `${getApiBaseUrl()}${path}`
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const normalizeCategory = (value?: string | null) =>
  value?.trim() || 'Chưa phân loại'

const getAvailableQuantity = (product: ProductDto) =>
  Math.max((product.quantityOnHand ?? 0) - (product.quantityReserved ?? 0), 0)

const getCartQuantity = (productId: string) =>
  cart.value.find(item => item.product.id === productId)?.quantity ?? 0

const canAddProduct = (product: ProductDto) =>
  getCartQuantity(product.id) < getAvailableQuantity(product)

const getStockColor = (product: ProductDto) => {
  const available = getAvailableQuantity(product)

  if (available <= 0)
    return 'error'

  if (available <= 5)
    return 'warning'

  return 'success'
}

const getStockText = (product: ProductDto) => {
  const available = getAvailableQuantity(product)

  if (available <= 0)
    return 'Hết hàng'

  if (available <= 5)
    return `Sắp hết · ${available}`

  return `Còn ${available}`
}

const categoryStats = computed(() => {
  const map = new Map<string, {
    name: string
    count: number
    availableCount: number
  }>()

  products.value.forEach(product => {
    const name = normalizeCategory(product.categoryName)

    const current = map.get(name) ?? {
      name,
      count: 0,
      availableCount: 0,
    }

    current.count += 1

    if (getAvailableQuantity(product) > 0)
      current.availableCount += 1

    map.set(name, current)
  })

  const allAvailable = products.value.filter(product => getAvailableQuantity(product) > 0).length

  return [
    {
      name: 'Tất cả',
      count: products.value.length,
      availableCount: allAvailable,
    },
    ...Array.from(map.values()).sort((a, b) => a.name.localeCompare(b.name, 'vi')),
  ]
})

const filteredProducts = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return products.value.filter(product => {
    const productCategory = normalizeCategory(product.categoryName)

    const matchesCategory =
      selectedCategory.value === 'Tất cả'
      || productCategory === selectedCategory.value

    const matchesQuery =
      !query
      || product.name.toLowerCase().includes(query)
      || product.code.toLowerCase().includes(query)
      || Boolean(product.barcode?.toLowerCase().includes(query))
      || productCategory.toLowerCase().includes(query)

    return matchesCategory && matchesQuery
  })
})

const totalProductPages = computed(() =>
  Math.max(1, Math.ceil(filteredProducts.value.length / productsPerPage.value)),
)

const paginatedProducts = computed(() => {
  const start = (currentPage.value - 1) * productsPerPage.value

  return filteredProducts.value.slice(start, start + productsPerPage.value)
})

const productRangeStart = computed(() => {
  if (!filteredProducts.value.length)
    return 0

  return (currentPage.value - 1) * productsPerPage.value + 1
})

const productRangeEnd = computed(() =>
  Math.min(currentPage.value * productsPerPage.value, filteredProducts.value.length),
)

const subtotal = computed(() =>
  cart.value.reduce((sum, item) => sum + item.product.sellPrice * item.quantity, 0),
)

const discount = computed(() => {
  const promo = promotionList.value.find(p => p.code === selectedPromotion.value)

  if (!promo)
    return 0

  if (promo.promotionType === 'Order') {
    if (promo.discountType === 'Percent')
      return Math.round(subtotal.value * promo.discountValue / 100)

    return Math.min(subtotal.value, promo.discountValue)
  }

  if (promo.promotionType === 'Product') {
    const productIds = promo.items?.map((item: any) => item.productId) || []

    if (productIds.length === 0)
      return 0

    const eligibleAmount = cart.value
      .filter(item => productIds.includes(item.product.id))
      .reduce((sum, item) => sum + item.product.sellPrice * item.quantity, 0)

    if (promo.discountType === 'Percent')
      return Math.round(eligibleAmount * promo.discountValue / 100)

    return Math.min(eligibleAmount, promo.discountValue)
  }

  if (promo.promotionType === 'Combo') {
    const items = promo.items || []

    for (const requiredItem of items) {
      const cartItem = cart.value.find(c => c.product.id === requiredItem.productId)

      if (!cartItem || cartItem.quantity < requiredItem.requiredQuantity)
        return 0
    }

    const comboProductIds = items.map((item: any) => item.productId)

    const eligibleAmount = cart.value
      .filter(item => comboProductIds.includes(item.product.id))
      .reduce((sum, item) => sum + item.product.sellPrice * item.quantity, 0)

    if (promo.discountType === 'Percent')
      return Math.round(eligibleAmount * promo.discountValue / 100)

    return Math.min(eligibleAmount, promo.discountValue)
  }

  if (promo.promotionType === 'BuyXGetY') {
    const items = promo.items || []

    if (items.length < 2)
      return 0

    const buyItem = items[0]
    const getItem = items[1]

    const buyLine = cart.value.find(c => c.product.id === buyItem.productId)
    const getLine = cart.value.find(c => c.product.id === getItem.productId)

    if (!buyLine || buyLine.quantity < buyItem.requiredQuantity)
      return 0

    if (!getLine || getLine.quantity < getItem.requiredQuantity)
      return 0

    const multiplier = Math.floor(buyLine.quantity / buyItem.requiredQuantity)
    const actualGiftQty = Math.min(multiplier * getItem.requiredQuantity, getLine.quantity)
    const giftAmount = actualGiftQty * getLine.product.sellPrice

    if (promo.discountType === 'Percent')
      return Math.round(giftAmount * promo.discountValue / 100)

    return Math.min(giftAmount, promo.discountValue)
  }

  return 0
})

const total = computed(() => Math.max(subtotal.value - discount.value, 0))

const changeDue = computed(() =>
  Math.max(cashReceived.value - total.value, 0),
)

const isCashPayment = computed(() => selectedPaymentMethod.value === 'Tiền mặt')

const isDeferredConfirmation = computed(() =>
  ['Chuyển khoản', 'Thẻ', 'Ví điện tử'].includes(selectedPaymentMethod.value),
)

const isDebtPayment = computed(() => selectedPaymentMethod.value === 'Ghi nợ')

const paymentActionLabel = computed(() =>
  isDebtPayment.value ? 'Ghi nhận công nợ' : 'Thanh toán',
)

const splitRemaining = computed(() => {
  const sumPaid =
    splitAmounts.value.cash
    + splitAmounts.value.transfer
    + splitAmounts.value.card
    + splitAmounts.value.ewallet
    + splitAmounts.value.debt

  return Math.max(0, total.value - sumPaid)
})

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
      value: currentShift.value.actualCash === null
        ? 'Chưa chốt'
        : formatCurrency(currentShift.value.actualCash),
    },
    {
      label: 'Chênh lệch',
      value: formatCurrency(currentShift.value.variance),
      class: currentShift.value.variance === 0 ? 'text-success' : 'text-warning',
    },
  ]
})

const selectCategory = (category: string) => {
  selectedCategory.value = category
  currentPage.value = 1
}

const addToCart = (product: ProductDto) => {
  errorMessage.value = ''

  if (!shiftOpen.value) {
    errorMessage.value = 'Vui lòng mở ca bán hàng trước khi thêm sản phẩm.'

    return
  }

  if (!canAddProduct(product)) {
    errorMessage.value = `"${product.name}" không đủ tồn kho khả dụng.`

    return
  }

  const existing = cart.value.find(item => item.product.id === product.id)

  if (existing)
    existing.quantity += 1
  else
    cart.value.push({ product, quantity: 1 })
}

const increaseCartItem = (item: CartItem) => {
  if (!canAddProduct(item.product)) {
    errorMessage.value = `"${item.product.name}" không đủ tồn kho khả dụng.`

    return
  }

  item.quantity += 1
}

const removeCartItem = (productId: string) => {
  cart.value = cart.value.filter(item => item.product.id !== productId)
}

const decreaseCartItem = (item: CartItem) => {
  if (item.quantity > 1) {
    item.quantity -= 1

    return
  }

  removeCartItem(item.product.id)
}

const fillRemainder = (key: 'cash' | 'transfer' | 'card' | 'ewallet' | 'debt') => {
  splitAmounts.value[key] = 0

  const sumPaid =
    splitAmounts.value.cash
    + splitAmounts.value.transfer
    + splitAmounts.value.card
    + splitAmounts.value.ewallet
    + splitAmounts.value.debt

  splitAmounts.value[key] = Math.max(0, total.value - sumPaid)
}

const resetOrderState = () => {
  cart.value = []
  cashReceived.value = 0
  selectedPromotion.value = ''
  splitAmounts.value = {
    cash: 0,
    transfer: 0,
    card: 0,
    ewallet: 0,
    debt: 0,
  }
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

const startQrCountdown = () => {
  if (qrTimer)
    clearInterval(qrTimer)

  qrTimer = setInterval(() => {
    if (qrCountdown.value > 0) {
      qrCountdown.value -= 1

      return
    }

    if (qrTimer)
      clearInterval(qrTimer)

    void handleVietQrSuccess()
  }, 1000)
}

const cancelQrPayment = () => {
  if (qrTimer)
    clearInterval(qrTimer)

  vietqrDialog.value = false
  submitting.value = false
}

const handleVietQrSuccess = async () => {
  if (qrTimer)
    clearInterval(qrTimer)

  vietqrDialog.value = false
  await executeCreateOrder()
}

const buildSplitPaymentsPayload = () => {
  if (selectedPaymentMethod.value !== 'Hỗn hợp')
    return null

  const splitSum =
    splitAmounts.value.cash
    + splitAmounts.value.transfer
    + splitAmounts.value.card
    + splitAmounts.value.ewallet
    + splitAmounts.value.debt

  if (Math.abs(splitSum - total.value) > 1) {
    throw new Error(
      `Tổng số tiền chia thanh toán (${splitSum.toLocaleString('vi-VN')}đ) phải bằng tổng hóa đơn (${total.value.toLocaleString('vi-VN')}đ).`,
    )
  }

  const payments: {
    paymentMethod: PaymentMethodValue
    amount: number
    note: string
  }[] = []

  if (splitAmounts.value.cash > 0)
    payments.push({ paymentMethod: 'Tiền mặt', amount: splitAmounts.value.cash, note: 'POS chia hóa đơn' })

  if (splitAmounts.value.transfer > 0)
    payments.push({ paymentMethod: 'Chuyển khoản', amount: splitAmounts.value.transfer, note: 'POS chia hóa đơn' })

  if (splitAmounts.value.card > 0)
    payments.push({ paymentMethod: 'Thẻ', amount: splitAmounts.value.card, note: 'POS chia hóa đơn' })

  if (splitAmounts.value.ewallet > 0)
    payments.push({ paymentMethod: 'Ví điện tử', amount: splitAmounts.value.ewallet, note: 'POS chia hóa đơn' })

  if (splitAmounts.value.debt > 0)
    payments.push({ paymentMethod: 'Ghi nợ', amount: splitAmounts.value.debt, note: 'POS chia hóa đơn' })

  return payments
}

const validateBeforeCreateOrder = () => {
  if (!shiftOpen.value)
    return 'Vui lòng mở ca bán hàng trước khi tạo đơn.'

  if (cart.value.length === 0)
    return 'Giỏ hàng đang trống.'

  const customer = customerList.value.find(c => c.id === selectedCustomerId.value)

  if (!customer)
    return 'Vui lòng chọn khách hàng trước khi tạo đơn.'

  if (isCashPayment.value && cashReceived.value < total.value)
    return 'Số tiền khách đưa chưa đủ để thanh toán hóa đơn.'

  return ''
}

const executeCreateOrder = async () => {
  submitting.value = true
  errorMessage.value = ''
  successMessage.value = ''
  lastCreatedOrderId.value = null

  try {
    const validationMessage = validateBeforeCreateOrder()

    if (validationMessage) {
      errorMessage.value = validationMessage

      return
    }

    const customer = customerList.value.find(c => c.id === selectedCustomerId.value)

    if (!customer)
      return

    const paymentsPayload = buildSplitPaymentsPayload()

    const res = await createOrder({
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
      payments: paymentsPayload,
    })

    resetOrderState()
    lastCreatedOrderId.value = res.id

    await loadPosData(false)

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

const handlePayment = async () => {
  const validationMessage = validateBeforeCreateOrder()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  if (selectedPaymentMethod.value === 'Chuyển khoản') {
    const bankId = 'ICB'
    const accountNumber = '109876543210'
    const template = 'qr_only'
    const amount = total.value
    const orderCode = `DH-${Date.now()}`
    const accountName = encodeURIComponent('Cty RetailOps')
    const addInfo = encodeURIComponent(orderCode)

    qrImageUrl.value = `https://img.vietqr.io/image/${bankId}-${accountNumber}-${template}.png?amount=${amount}&addInfo=${addInfo}&accountName=${accountName}`

    vietqrDialog.value = true
    qrCountdown.value = 10
    startQrCountdown()

    return
  }

  await executeCreateOrder()
}

const handleSaveDraftOrQuote = async (status: 'Draft' | 'Quotation') => {
  if (!cart.value.length)
    return

  const customer = customerList.value.find(c => c.id === selectedCustomerId.value)

  if (!customer) {
    errorMessage.value = 'Vui lòng chọn khách hàng trước khi tạo đơn.'

    return
  }

  submitting.value = true
  errorMessage.value = ''
  successMessage.value = ''
  lastCreatedOrderId.value = null

  try {
    const res = await createOrder({
      customerId: customer.id,
      customerName: customer.fullName,
      createdBy: authStore.user?.id ?? '',
      createdByName: authStore.user?.fullName ?? 'POS',
      paymentMethod: selectedPaymentMethod.value,
      promotionCode: selectedPromotion.value || null,
      note: `Lưu dưới dạng ${status === 'Draft' ? 'Đơn nháp' : 'Báo giá'}`,
      status,
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

    resetOrderState()
    lastCreatedOrderId.value = res.id
    successMessage.value = `Lưu ${status === 'Draft' ? 'đơn hàng nháp' : 'báo giá'} thành công.`
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : `Không thể lưu ${status === 'Draft' ? 'đơn hàng nháp' : 'báo giá'}.`
  }
  finally {
    submitting.value = false
  }
}

const loadPosData = async (showLoading = true) => {
  if (showLoading) {
    loading.value = true
    errorMessage.value = ''
    successMessage.value = ''
  }

  try {
    const [productResult, customerResult, promotionResult, shift] = await Promise.all([
      getProducts({ pageSize: 300 }),
      getCustomers({ pageSize: 100 }),
      getActivePromotions(),
      getCurrentShift().catch(() => null),
    ])

    products.value = productResult.items
    customerList.value = customerResult.items
    promotionList.value = promotionResult
    shiftOpen.value = shift?.status === 'Open'
    currentShift.value = shift

    if (!selectedCustomerId.value && customerResult.items.length > 0)
      selectedCustomerId.value = customerResult.items[0].id

    if (selectedPromotion.value && !promotionResult.some(p => p.code === selectedPromotion.value))
      selectedPromotion.value = ''
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

const focusBarcodeInput = () => {
  nextTick(() => {
    const input = document.querySelector('.barcode-input-field input') as HTMLInputElement | null

    input?.focus()
  })
}

const openBarcodeScanner = () => {
  barcodeInput.value = ''
  barcodeMessage.value = ''
  barcodeDialog.value = true
  focusBarcodeInput()
}

const handleBarcodeScan = async () => {
  const code = barcodeInput.value.trim()

  if (!code)
    return

  let product = products.value.find(
    p => p.barcode?.toLowerCase() === code.toLowerCase()
      || p.code.toLowerCase() === code.toLowerCase(),
  )

  if (!product) {
    try {
      product = await getProductByBarcode(code)

      if (product && !products.value.some(p => p.id === product!.id))
        products.value.push(product)
    }
    catch (error) {
      console.error('Không tìm thấy sản phẩm từ API:', error)
    }
  }

  if (product) {
    if (!shiftOpen.value) {
      barcodeMessage.value = 'Vui lòng mở ca bán hàng trước khi thêm sản phẩm.'
      barcodeMessageType.value = 'error'
    }
    else if (!canAddProduct(product)) {
      barcodeMessage.value = `"${product.name}" không đủ tồn kho khả dụng.`
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
  focusBarcodeInput()
}

const handleGlobalKeydown = (event: KeyboardEvent) => {
  const target = event.target as HTMLElement | null
  const tag = target?.tagName

  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT')
    return

  if (barcodeDialog.value)
    return

  if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
    const searchEl = document.querySelector('.pos-search-field input') as HTMLInputElement | null

    searchEl?.focus()
  }
}

watch([searchQuery, selectedCategory, productsPerPage], () => {
  currentPage.value = 1
})

watch(filteredProducts, () => {
  if (currentPage.value > totalProductPages.value)
    currentPage.value = totalProductPages.value
})

onMounted(() => {
  loadPosData()
  window.addEventListener('keydown', handleGlobalKeydown)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleGlobalKeydown)

  if (qrTimer)
    clearInterval(qrTimer)
})
</script>
<template>
  <section class="pos-page">
    <div class="pos-hero">
      <div class="pos-hero__title-area">
        <h1>POS Bán Hàng</h1>
        <VChip :color="shiftOpen ? 'success' : 'warning'" variant="tonal" size="small"
          :prepend-icon="shiftOpen ? 'ri-checkbox-circle-line' : 'ri-error-warning-line'" class="ml-2">
          {{ shiftOpen ? 'Sẵn sàng bán hàng' : 'Cần mở ca' }}
        </VChip>
      </div>
      <div class="pos-shift-action">
        <VBtn v-if="!shiftOpen" color="success" prepend-icon="ri-play-circle-line" size="small" @click="startOpenShift">
          Mở ca </VBtn>
        <VBtn v-else color="warning" prepend-icon="ri-stop-circle-line" size="small" @click="startCloseShift">
          Đóng ca </VBtn>
      </div>
    </div>
    <VAlert v-if="errorMessage" type="error" variant="tonal" density="comfortable" class="mb-4" closable
      @click:close="errorMessage = ''"> {{ errorMessage }} </VAlert>
    <VAlert v-if="successMessage" type="success" variant="tonal" density="comfortable" class="mb-4" closable
      @click:close="successMessage = ''">
      <div class="d-flex align-center justify-space-between w-100 flex-wrap gap-2"> <span>{{ successMessage }}</span>
        <VBtn v-if="lastCreatedOrderId" color="success" size="small" prepend-icon="ri-printer-line"
          @click="printLastInvoice"> In hóa đơn K80 </VBtn>
      </div>
    </VAlert>
    <div class="pos-workspace">
      <section class="pos-main">
        <VCard class="pos-search-panel">
          <VCardText>
            <div class="pos-search-row">
              <VTextField v-model="searchQuery" label="Tìm sản phẩm" placeholder="Nhập tên, SKU, barcode hoặc danh mục"
                prepend-inner-icon="ri-search-line" density="comfortable" clearable hide-details
                class="pos-search-field" />
              <VBtn color="primary" variant="tonal" prepend-icon="ri-qr-scan-2-line" class="barcode-btn"
                @click="openBarcodeScanner"> Quét barcode </VBtn>
            </div>
          </VCardText>
        </VCard>
        <div class="catalog-layout">
          <aside class="category-panel">
            <div class="category-panel__head">
              <div> <span>Phân loại</span> <strong>Sản phẩm</strong> </div>
              <VChip color="primary" variant="tonal" size="small"> {{ products.length }} </VChip>
            </div>
            <div class="category-list"> <button v-for="category in categoryStats" :key="category.name" type="button"
                class="category-item" :class="{ 'category-item--active': selectedCategory === category.name }"
                @click="selectCategory(category.name)"> <span class="category-icon">
                  <VIcon :icon="category.name === 'Tất cả' ? 'ri-apps-2-line' : 'ri-price-tag-3-line'" size="18" />
                </span> <span class="category-text"> <strong>{{ category.name }}</strong> <small>{{
                  category.availableCount }} còn hàng</small> </span> <b>{{ category.count }}</b> </button> </div>
          </aside>
          <section class="product-catalog">
            <div class="catalog-head">
              <div> <span>Danh mục đang xem</span>
                <h2>{{ selectedCategory }}</h2>
                <p> Hiển thị {{ productRangeStart }}–{{ productRangeEnd }} trong {{
                  formatNumber(filteredProducts.length) }} sản phẩm </p>
              </div>
              <div class="catalog-controls">
                <VSelect v-model="productsPerPage" :items="productsPerPageOptions" label="Hiển thị" density="compact"
                  hide-details class="per-page-select" />
                <VBtn variant="tonal" color="primary" prepend-icon="ri-refresh-line" :loading="loading"
                  @click="loadPosData()"> Tải lại </VBtn>
              </div>
            </div>
            <VCard v-if="loading" class="pos-loading-card">
              <VCardText>
                <VSkeletonLoader type="heading, paragraph, image, image, image" />
              </VCardText>
            </VCard>
            <VCard v-else-if="filteredProducts.length === 0" class="pos-empty-card">
              <div class="pos-empty-state">
                <VIcon icon="ri-search-line" color="primary" size="38" /> <strong>Không tìm thấy sản phẩm</strong>
                <span>Thử đổi từ khóa hoặc chọn phân loại khác.</span>
              </div>
            </VCard> <template v-else>
              <div class="product-grid">
                <article v-for="product in paginatedProducts" :key="product.id" class="product-card">
                  <div class="product-media">
                    <VImg v-if="product.imageUrl" :src="resolveImageUrl(product.imageUrl)" cover height="150" />
                    <div v-else class="product-placeholder">
                      <VIcon icon="ri-price-tag-3-line" size="38" />
                    </div>
                    <VChip class="product-stock-chip" :color="getStockColor(product)" size="small" variant="tonal"> {{
                      getStockText(product) }} </VChip>
                  </div>
                  <div class="product-body">
                    <div class="product-meta"> <span>{{ product.code }}</span>
                      <VChip color="primary" size="x-small" variant="tonal"> {{ normalizeCategory(product.categoryName)
                      }} </VChip>
                    </div>
                    <h3>{{ product.name }}</h3>
                    <p> {{ product.unitName }} · Barcode {{ product.barcode || '—' }} </p>
                    <div class="product-footer"> <strong>{{ formatCurrency(product.sellPrice) }}</strong>
                      <VBtn color="primary" variant="tonal" size="small" prepend-icon="ri-add-line"
                        :disabled="!shiftOpen || !canAddProduct(product)" @click="addToCart(product)"> Thêm </VBtn>
                    </div>
                  </div>
                </article>
              </div>
              <div class="catalog-pagination">
                <div> Trang {{ currentPage }} / {{ totalProductPages }} </div>
                <VPagination v-model="currentPage" :length="totalProductPages" :total-visible="5" rounded="circle"
                  density="comfortable" />
              </div>
            </template>
          </section>
        </div>
      </section>
      <aside class="pos-cart-aside">
        <VCard class="cart-panel">
          <div class="cart-head">
            <div> <span>Đơn hàng hiện tại</span>
              <h2>Giỏ hàng</h2>
            </div>
            <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-shopping-basket-2-line"> {{ cart.length
            }}
              mặt hàng </VChip>
          </div>
          <VDivider />
          <div class="cart-scroll-area">
            <div v-if="cart.length === 0" class="cart-empty">
              <VIcon icon="ri-shopping-basket-2-line" size="42" color="primary" /> <strong>Giỏ hàng đang trống</strong>
            </div>
            <div v-else class="cart-items">
              <article v-for="item in cart" :key="item.product.id" class="cart-item">
                <div class="cart-item__info"> <strong>{{ item.product.name }}</strong> <span>{{
                  formatCurrency(item.product.sellPrice) }} / {{ item.product.unitName }}</span> </div>
                <div class="cart-item__controls">
                  <div class="cart-quantity-control">
                    <VBtn icon="ri-subtract-line" size="x-small" variant="tonal" @click="decreaseCartItem(item)" />
                    <b>{{
                      item.quantity }}</b>
                    <VBtn icon="ri-add-line" size="x-small" variant="tonal" :disabled="!canAddProduct(item.product)"
                      @click="increaseCartItem(item)" />
                  </div> <strong>{{ formatCurrency(item.product.sellPrice * item.quantity) }}</strong>
                  <VBtn icon="ri-delete-bin-line" size="x-small" color="error" variant="text"
                    @click="removeCartItem(item.product.id)" />
                </div>
              </article>
            </div>
            <VDivider />
            <div class="cart-form">
              <div class="cart-section-title"> Thông tin đơn </div>
              <VSelect v-model="selectedCustomerId" label="Khách hàng" :items="customerList" item-title="fullName"
                item-value="id" density="comfortable" hide-details="auto" />
              <VSelect v-model="selectedPromotion" label="Khuyến mãi" :items="promotionList.map(p => p.code)"
                density="comfortable" hide-details="auto" clearable />
            </div>
          </div>
          <VDivider />
          <div class="checkout-footer">
            <VSelect v-model="selectedPaymentMethod" label="Hình thức thanh toán" :items="paymentMethods"
              item-title="label" item-value="value" density="comfortable" hide-details="auto"> <template
                #item="{ props, item }">
                <VListItem v-bind="props" :prepend-icon="item.raw.icon" :title="item.raw.label" />
              </template>
            </VSelect>
            <VTextField v-if="isCashPayment" v-model.number="cashReceived" label="Khách đưa" type="number" suffix="đ"
              density="comfortable" hide-details="auto" />
            <VAlert v-else-if="isDeferredConfirmation" color="info" variant="tonal" density="compact"
              icon="ri-time-line"> Chờ
              xác nhận thanh toán trước khi hoàn tất đơn. </VAlert>
            <VAlert v-else-if="isDebtPayment" color="warning" variant="tonal" density="compact"
              icon="ri-error-warning-line">
              Ghi nợ chỉ áp dụng cho khách hàng đã có hồ sơ. </VAlert>
            <div v-else-if="selectedPaymentMethod === 'Hỗn hợp'" class="split-payment-box">
              <div class="split-payment-head"> <strong>Chia hóa đơn</strong>
                <VChip size="small" :color="splitRemaining === 0 ? 'success' : 'warning'" variant="tonal"> Còn lại: {{
                  formatCurrency(splitRemaining) }} </VChip>
              </div>
              <VTextField v-model.number="splitAmounts.cash" label="Tiền mặt" type="number" prefix="đ" density="compact"
                hide-details> <template #append-inner>
                  <VBtn size="x-small" variant="text" color="primary" @click="fillRemainder('cash')"> Điền nốt </VBtn>
                </template>
              </VTextField>
              <VTextField v-model.number="splitAmounts.transfer" label="Chuyển khoản" type="number" prefix="đ"
                density="compact" hide-details> <template #append-inner>
                  <VBtn size="x-small" variant="text" color="primary" @click="fillRemainder('transfer')"> Điền nốt
                  </VBtn>
                </template>
              </VTextField>
              <VTextField v-model.number="splitAmounts.card" label="Thẻ" type="number" prefix="đ" density="compact"
                hide-details> <template #append-inner>
                  <VBtn size="x-small" variant="text" color="primary" @click="fillRemainder('card')"> Điền nốt </VBtn>
                </template>
              </VTextField>
              <VTextField v-model.number="splitAmounts.ewallet" label="Ví điện tử" type="number" prefix="đ"
                density="compact" hide-details> <template #append-inner>
                  <VBtn size="x-small" variant="text" color="primary" @click="fillRemainder('ewallet')"> Điền nốt
                  </VBtn>
                </template>
              </VTextField>
              <VTextField v-model.number="splitAmounts.debt" label="Ghi nợ" type="number" prefix="đ" density="compact"
                hide-details> <template #append-inner>
                  <VBtn size="x-small" variant="text" color="primary" @click="fillRemainder('debt')"> Điền nốt </VBtn>
                </template>
              </VTextField>
            </div>
            <div class="cart-summary">
              <div> <span>Tạm tính</span> <strong>{{ formatCurrency(subtotal) }}</strong> </div>
              <div class="text-success"> <span>Giảm giá</span> <strong>-{{ formatCurrency(discount) }}</strong> </div>
              <div v-if="isCashPayment"> <span>Tiền thừa</span> <strong>{{ formatCurrency(changeDue) }}</strong> </div>
            </div>
            <div class="total-box"> <span>Cần trả</span> <strong>{{ formatCurrency(total) }}</strong> </div>
            <VBtn block size="large" class="payment-action" prepend-icon="ri-bank-card-line" :loading="submitting"
              :disabled="cart.length === 0 || !shiftOpen" @click="handlePayment"> {{ paymentActionLabel }} </VBtn>
            <div class="cart-secondary-actions">
              <VBtn color="secondary" variant="outlined" prepend-icon="ri-draft-line" :loading="submitting"
                :disabled="cart.length === 0" @click="handleSaveDraftOrQuote('Draft')"> Lưu nháp </VBtn>
              <VBtn color="info" variant="outlined" prepend-icon="ri-file-list-line" :loading="submitting"
                :disabled="cart.length === 0" @click="handleSaveDraftOrQuote('Quotation')"> Báo giá </VBtn>
            </div>
          </div>
        </VCard>
      </aside>
    </div>
    <VDialog v-model="openShiftDialog" max-width="500">
      <VCard class="pos-dialog-card">
        <VCardTitle>Mở ca bán hàng mới</VCardTitle>
        <VCardText>
          <VTextField v-model.number="openingCashInput" type="number" label="Tiền mặt đầu ca" prefix="đ" class="mb-4" />
          <VTextarea v-model="shiftNoteInput" label="Ghi chú mở ca" rows="3" />
        </VCardText>
        <VCardActions>
          <VSpacer />
          <VBtn color="secondary" variant="text" @click="openShiftDialog = false"> Hủy </VBtn>
          <VBtn color="primary" @click="confirmOpenShift"> Xác nhận mở ca </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
    <VDialog v-model="closeShiftDialog" max-width="560">
      <VCard v-if="currentShift" class="pos-dialog-card">
        <VCardTitle>Xác nhận đóng ca làm việc</VCardTitle>
        <VCardText>
          <div class="shift-info-box">
            <div> <span>Mã ca</span> <strong>{{ currentShift.shiftCode }}</strong> </div>
            <div> <span>Thu ngân</span> <strong>{{ currentShift.cashierName }}</strong> </div>
            <div> <span>Giờ mở ca</span> <strong>{{ new Date(currentShift.openedAt).toLocaleString('vi-VN') }}</strong>
            </div>
          </div>
          <div class="shift-summary-grid">
            <div v-for="row in shiftSummaryRows" :key="row.label"> <span>{{ row.label }}</span> <strong
                :class="row.class">{{ row.value }}</strong> </div>
          </div>
          <VTextField v-model.number="actualCashInput" type="number" label="Tiền mặt thực tế trong két" prefix="đ"
            class="mb-4" required />
          <VTextarea v-model="shiftNoteInput" label="Ghi chú đóng ca" rows="3" />
        </VCardText>
        <VCardActions>
          <VSpacer />
          <VBtn color="secondary" variant="text" @click="closeShiftDialog = false"> Hủy </VBtn>
          <VBtn color="warning" @click="confirmCloseShift"> Xác nhận đóng ca </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
    <VDialog v-model="barcodeDialog" max-width="500">
      <VCard class="pos-dialog-card">
        <VCardTitle class="d-flex align-center gap-2">
          <VIcon icon="ri-qr-scan-2-line" color="primary" /> Quét mã Barcode
        </VCardTitle>
        <VCardText>
          <p class="text-body-2 text-medium-emphasis mb-4"> Dùng máy quét barcode hoặc nhập mã tay. Nhấn Enter để tìm
            sản
            phẩm. </p>
          <VTextField ref="barcodeInputRef" v-model="barcodeInput" label="Mã Barcode / SKU"
            placeholder="Quét hoặc nhập mã barcode..." prepend-inner-icon="ri-barcode-line" density="comfortable"
            autofocus @keyup.enter="handleBarcodeScan" />
          <VAlert v-if="barcodeMessage" :type="barcodeMessageType" variant="tonal" density="compact" class="mt-4"> {{
            barcodeMessage }} </VAlert>
        </VCardText>
        <VCardActions>
          <VSpacer />
          <VBtn color="secondary" variant="text" @click="barcodeDialog = false"> Đóng </VBtn>
          <VBtn color="primary" prepend-icon="ri-search-line" @click="handleBarcodeScan"> Tìm sản phẩm </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
    <VDialog v-model="vietqrDialog" max-width="450" persistent>
      <VCard class="qr-dialog-card">
        <VCardTitle class="qr-dialog-title">
          <VIcon icon="ri-qr-code-line" color="primary" /> Thanh toán VietQR
        </VCardTitle>
        <VCardText class="qr-dialog-content">
          <p> Quét mã QR dưới đây bằng ứng dụng ngân hàng để thanh toán đơn hàng. </p>
          <VImg :src="qrImageUrl" width="260" height="260" class="qr-image" />
          <div class="qr-info"> <span>Tài khoản thụ hưởng</span> <strong>VietinBank - 109876543210</strong> <span>Chủ
              tài
              khoản</span> <strong>Cty RetailOps</strong> <span>Số tiền thanh toán</span> <b>{{ formatCurrency(total)
              }}</b>
          </div>
          <div class="qr-waiting">
            <div>
              <VProgressCircular indeterminate size="16" width="2" color="info" /> <strong>Đang chờ ngân hàng xác
                nhận...</strong>
            </div> <span> Tự động xác nhận sau <b>{{ qrCountdown }}</b> giây </span>
          </div>
        </VCardText>
        <VCardActions class="qr-actions">
          <VBtn color="secondary" variant="outlined" @click="cancelQrPayment"> Hủy bỏ </VBtn>
          <VBtn color="success" prepend-icon="ri-checkbox-circle-line" @click="handleVietQrSuccess"> Giả lập nhận tiền
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>
<style scoped>
.pos-page {
  position: relative;
  isolation: isolate;
}

.pos-page::before {
  content: '';
  position: absolute;
  inset: -2rem -2rem auto;
  z-index: -1;
  block-size: 360px;
  border-radius: 0 0 44px 44px;
  background: radial-gradient(circle at 16% 12%, rgba(var(--v-theme-primary), 0.18), transparent 34%), radial-gradient(circle at 86% 4%, rgba(var(--v-theme-info), 0.16), transparent 32%), linear-gradient(135deg, rgba(var(--v-theme-primary), 0.08), transparent 58%);
  pointer-events: none;
}

.pos-hero {
  display: flex;
  align-items: stretch;
  justify-content: space-between;
  gap: 1.25rem;
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 30px;
  padding: clamp(1.25rem, 3vw, 2rem);
  margin-block-end: 1rem;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.94), rgba(var(--v-theme-surface), 0.76)), rgba(var(--v-theme-surface), 0.78);
  box-shadow: 0 24px 70px rgba(15, 23, 42, 0.1), inset 0 1px 0 rgba(255, 255, 255, 0.16);
  backdrop-filter: blur(18px);
}

.pos-eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.22);
  border-radius: 999px;
  padding: 0.4rem 0.8rem;
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.08);
  font-size: 0.82rem;
  font-weight: 800;
  margin-block-end: 1rem;
}

.pos-hero h1 {
  margin: 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: clamp(2rem, 4vw, 3.4rem);
  font-weight: 850;
  letter-spacing: -0.045em;
  line-height: 1;
}

.pos-hero p {
  max-inline-size: 760px;
  margin: 1rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.68);
  font-size: 1rem;
  line-height: 1.7;
}

.pos-shift-card {
  display: grid;
  align-content: center;
  gap: 0.75rem;
  min-inline-size: 270px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px;
  padding: 1rem;
  background: radial-gradient(circle at 100% 0%, rgba(var(--v-theme-primary), 0.1), transparent 44%), rgba(var(--v-theme-background), 0.5);
}

.pos-shift-card span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.pos-shift-card strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.2rem;
  font-weight: 850;
  letter-spacing: -0.025em;
}

.shift-action {
  border-radius: 14px;
  font-weight: 800;
}

.pos-workspace {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) minmax(390px, 430px);
}

.pos-main {
  display: grid;
  gap: 1rem;
}

.pos-search-panel,
.cart-panel,
.pos-loading-card,
.pos-empty-card,
.pos-dialog-card,
.qr-dialog-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.96), rgba(var(--v-theme-surface), 0.84));
  box-shadow: 0 18px 52px rgba(15, 23, 42, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.pos-search-row {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: minmax(0, 1fr) auto;
}

.pos-search-field :deep(.v-field),
.pos-search-panel :deep(.v-field),
.cart-panel :deep(.v-field) {
  border-radius: 16px;
}

.barcode-btn {
  min-block-size: 48px;
  border-radius: 16px;
  font-weight: 800;
}

.catalog-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: 260px minmax(0, 1fr);
}

.category-panel {
  position: sticky;
  inset-block-start: 92px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px;
  padding: 1rem;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.96), rgba(var(--v-theme-surface), 0.84));
  box-shadow: 0 18px 52px rgba(15, 23, 42, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.category-panel__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-block-end: 1rem;
}

.category-panel__head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.category-panel__head strong {
  display: block;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.25rem;
  font-weight: 900;
  letter-spacing: -0.04em;
}

.category-list {
  display: grid;
  gap: 0.45rem;
  max-block-size: calc(100dvh - 280px);
  overflow: auto;
  padding-inline-end: 0.15rem;
}

.category-item {
  display: grid;
  align-items: center;
  gap: 0.7rem;
  grid-template-columns: 38px minmax(0, 1fr) auto;
  inline-size: 100%;
  min-block-size: 54px;
  border: 0;
  border-radius: 16px;
  padding: 0.45rem 0.65rem;
  color: rgba(var(--v-theme-on-surface), 0.72);
  background: transparent;
  font: inherit;
  text-align: start;
  cursor: pointer;
  transition: color 180ms ease, background 180ms ease, box-shadow 180ms ease, transform 180ms ease;
}

.category-item:hover {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.06);
  transform: translateX(3px);
}

.category-item--active {
  color: rgb(var(--v-theme-primary));
  background: linear-gradient(90deg, rgba(var(--v-theme-primary), 0.14), rgba(var(--v-theme-primary), 0.06)), rgb(var(--v-theme-surface));
  box-shadow: 0 12px 30px rgba(var(--v-theme-primary), 0.14), inset 0 1px 0 rgba(255, 255, 255, 0.16);
}

.category-icon {
  display: grid;
  place-items: center;
  inline-size: 38px;
  block-size: 38px;
  border-radius: 13px;
  color: rgba(var(--v-theme-on-surface), 0.65);
  background: rgba(var(--v-theme-on-surface), 0.055);
}

.category-item--active .category-icon {
  color: white;
  background: radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.42), transparent 28%), linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info)));
  box-shadow: 0 12px 28px rgba(var(--v-theme-primary), 0.26);
}

.category-text {
  min-inline-size: 0;
}

.category-text strong,
.category-text small {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.category-text strong {
  font-size: 0.92rem;
  font-weight: 850;
}

.category-text small {
  color: rgba(var(--v-theme-on-surface), 0.5);
  font-size: 0.75rem;
}

.category-item b {
  display: grid;
  place-items: center;
  min-inline-size: 28px;
  block-size: 24px;
  border-radius: 999px;
  color: rgba(var(--v-theme-on-surface), 0.58);
  background: rgba(var(--v-theme-on-surface), 0.06);
  font-size: 0.76rem;
}

.product-catalog {
  display: grid;
  gap: 1rem;
}

.catalog-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px;
  padding: 1rem;
  background: rgba(var(--v-theme-surface), 0.78);
  box-shadow: 0 14px 38px rgba(15, 23, 42, 0.06);
}

.catalog-head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.catalog-head h2 {
  margin: 0.25rem 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.55rem;
  font-weight: 900;
  letter-spacing: -0.045em;
}

.catalog-head p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.88rem;
}

.catalog-controls {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.per-page-select {
  inline-size: 116px;
}

.product-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.product-card {
  overflow: hidden;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 16px 40px rgba(15, 23, 42, 0.07), inset 0 1px 0 rgba(255, 255, 255, 0.12);
  transition: transform 180ms ease, border-color 180ms ease, box-shadow 180ms ease;
}

.product-card:hover {
  border-color: rgba(var(--v-theme-primary), 0.3);
  box-shadow: 0 24px 58px rgba(15, 23, 42, 0.12), 0 0 0 4px rgba(var(--v-theme-primary), 0.04);
  transform: translateY(-4px);
}

.product-media {
  position: relative;
  overflow: hidden;
  block-size: 150px;
  background: rgba(var(--v-theme-primary), 0.04);
}

.product-placeholder {
  display: grid;
  block-size: 100%;
  place-items: center;
  color: rgb(var(--v-theme-primary));
  background: radial-gradient(circle at 50% 0, rgba(var(--v-theme-primary), 0.16), transparent 42%), rgba(var(--v-theme-primary), 0.05);
}

.product-stock-chip {
  position: absolute;
  inset-block-start: 0.75rem;
  inset-inline-end: 0.75rem;
  backdrop-filter: blur(12px);
}

.product-body {
  padding: 1rem;
}

.product-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.7rem;
  margin-block-end: 0.75rem;
}

.product-meta span {
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
  font-weight: 800;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.product-body h3 {
  display: -webkit-box;
  min-block-size: 46px;
  margin: 0;
  overflow: hidden;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
  font-weight: 850;
  letter-spacing: -0.02em;
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.product-body p {
  overflow: hidden;
  margin: 0.55rem 0 1rem;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.product-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.8rem;
}

.product-footer strong {
  color: rgb(var(--v-theme-primary));
  font-size: 1rem;
  font-weight: 900;
  letter-spacing: -0.025em;
}

.catalog-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 22px;
  padding: 0.7rem 1rem;
  background: rgb(var(--v-theme-surface));
}

.catalog-pagination>div {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.88rem;
  font-weight: 700;
}

.pos-empty-state,
.cart-empty {
  display: grid;
  place-items: center;
  gap: 0.45rem;
  min-block-size: 240px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.6);
  text-align: center;
}

.pos-empty-state strong,
.cart-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.pos-cart-aside {
  position: sticky;
  inset-block-start: 92px;
}

.cart-panel {
  display: flex;
  flex-direction: column;
  max-block-size: calc(100dvh - 116px);
}

.cart-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem;
}

.cart-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  font-weight: 800;
  text-transform: uppercase;
}

.cart-head h2 {
  margin: 0.25rem 0 0;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.5rem;
  font-weight: 900;
  letter-spacing: -0.04em;
}

.cart-scroll-area {
  flex: 1 1 auto;
  min-block-size: 160px;
  overflow: auto;
}

.cart-items {
  display: grid;
  gap: 0.75rem;
  padding: 1rem;
}

.cart-item {
  display: grid;
  gap: 0.8rem;
  border: 1px solid rgba(var(--v-border-color), 0.1);
  border-radius: 18px;
  padding: 0.85rem;
  background: rgba(var(--v-theme-background), 0.46);
}

.cart-item__info {
  min-inline-size: 0;
}

.cart-item__info strong {
  display: block;
  overflow: hidden;
  color: rgb(var(--v-theme-on-surface));
  font-weight: 850;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cart-item__info span {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.84rem;
}

.cart-item__controls {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.65rem;
}

.cart-item__controls>strong {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
  white-space: nowrap;
}

.cart-quantity-control {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 999px;
  padding: 0.25rem;
  background: rgb(var(--v-theme-surface));
}

.cart-quantity-control b {
  min-inline-size: 24px;
  text-align: center;
}

.cart-form {
  display: grid;
  gap: 0.85rem;
  padding: 1rem;
}

.cart-section-title {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 900;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.checkout-footer {
  display: grid;
  gap: 0.9rem;
  padding: 1rem;
  background: linear-gradient(180deg, rgba(var(--v-theme-surface), 0.94), rgb(var(--v-theme-surface)));
}

.split-payment-box {
  display: grid;
  gap: 0.75rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.split-payment-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.cart-summary {
  display: grid;
  gap: 0.55rem;
}

.cart-summary>div {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  color: rgba(var(--v-theme-on-surface), 0.68);
}

.cart-summary strong {
  color: rgb(var(--v-theme-on-surface));
}

.total-box {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding-block-start: 1rem;
}

.total-box span {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.05rem;
  font-weight: 850;
}

.total-box strong {
  color: rgb(var(--v-theme-primary));
  font-size: 1.45rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.payment-action {
  min-block-size: 52px;
  border-radius: 16px;
  color: white !important;
  font-size: 1rem;
  font-weight: 900;
  background: linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info))) !important;
  box-shadow: 0 16px 36px rgba(var(--v-theme-primary), 0.3);
}

.cart-secondary-actions {
  display: grid;
  gap: 0.7rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.cart-secondary-actions .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.shift-info-box,
.shift-summary-grid {
  display: grid;
  gap: 0.75rem;
  margin-block-end: 1rem;
}

.shift-info-box {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.shift-info-box div,
.shift-summary-grid div {
  display: grid;
  gap: 0.25rem;
}

.shift-info-box span,
.shift-summary-grid span {
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 800;
}

.shift-info-box strong,
.shift-summary-grid strong {
  color: rgb(var(--v-theme-on-surface));
}

.shift-summary-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.shift-summary-grid div {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 16px;
  padding: 0.85rem;
  background: rgba(var(--v-theme-background), 0.4);
}

.qr-dialog-card {
  padding: 0.5rem;
  text-align: center;
}

.qr-dialog-title {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.55rem;
  font-size: 1.25rem;
  font-weight: 950;
}

.qr-dialog-content {
  display: grid;
  justify-items: center;
  gap: 1rem;
}

.qr-dialog-content p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.62);
}

.qr-image {
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 18px;
  box-shadow: 0 18px 44px rgba(15, 23, 42, 0.12);
}

.qr-info {
  display: grid;
  gap: 0.3rem;
}

.qr-info span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
}

.qr-info strong {
  color: rgb(var(--v-theme-on-surface));
}

.qr-info b {
  color: rgb(var(--v-theme-primary));
  font-size: 1.25rem;
}

.qr-waiting {
  inline-size: 100%;
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-info), 0.08);
}

.qr-waiting div {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: rgb(var(--v-theme-info));
}

.qr-waiting span {
  display: block;
  margin-block-start: 0.3rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.82rem;
}

.qr-actions {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.qr-actions .v-btn {
  border-radius: 14px;
}

@media (max-width: 1480px) {
  .product-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 1280px) {
  .pos-workspace {
    grid-template-columns: 1fr;
  }

  .pos-cart-aside,
  .category-panel {
    position: static;
  }

  .cart-panel {
    max-block-size: none;
  }
}

@media (max-width: 960px) {
  .catalog-layout {
    grid-template-columns: 1fr;
  }

  .category-list {
    display: flex;
    max-block-size: none;
    overflow-x: auto;
  }

  .category-item {
    min-inline-size: 220px;
  }

  .catalog-head {
    flex-direction: column;
  }

  .catalog-controls {
    inline-size: 100%;
  }

  .per-page-select {
    flex: 1;
  }
}

@media (max-width: 760px) {
  .pos-hero {
    flex-direction: column;
    border-radius: 24px;
  }

  .pos-shift-card {
    min-inline-size: 0;
  }

  .pos-search-row {
    grid-template-columns: 1fr;
  }

  .barcode-btn {
    inline-size: 100%;
  }

  .product-grid {
    grid-template-columns: 1fr;
  }

  .catalog-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .cart-secondary-actions,
  .qr-actions,
  .shift-summary-grid {
    grid-template-columns: 1fr;
  }

  .cart-item__controls {
    align-items: flex-start;
    flex-direction: column;
  }
}

/* =========================
   POS COMPACT FIX OVERRIDE
   ========================= */

/* 1. Header POS nhỏ lại */
.pos-hero {
  align-items: center;
  border-radius: 24px;
  padding: 1rem 1.25rem;
  margin-block-end: 0.85rem;
}

.pos-eyebrow {
  padding: 0.34rem 0.68rem;
  margin-block-end: 0.65rem;
  font-size: 0.78rem;
}

.pos-hero h1 {
  font-size: clamp(1.85rem, 3vw, 2.65rem);
  line-height: 1.05;
}

.pos-hero p {
  max-inline-size: 760px;
  margin-block-start: 0.65rem;
  font-size: 0.94rem;
  line-height: 1.55;
}

.pos-shift-card {
  min-inline-size: 245px;
  border-radius: 20px;
  padding: 0.85rem;
  gap: 0.55rem;
}

.pos-shift-card strong {
  font-size: 1.05rem;
}

.pos-shift-card .v-chip {
  block-size: 28px;
}

.shift-action {
  min-block-size: 40px;
  border-radius: 12px;
}

/* 2. Layout catalog rộng hơn, đỡ bóp card sản phẩm */
.pos-workspace {
  grid-template-columns: minmax(0, 1fr) minmax(360px, 400px);
}

.catalog-layout {
  grid-template-columns: 220px minmax(0, 1fr);
}

.category-panel {
  border-radius: 20px;
  padding: 0.8rem;
}

.category-panel__head {
  margin-block-end: 0.75rem;
}

.category-item {
  min-block-size: 48px;
  border-radius: 14px;
  padding: 0.38rem 0.55rem;
  grid-template-columns: 34px minmax(0, 1fr) auto;
}

.category-icon {
  inline-size: 34px;
  block-size: 34px;
  border-radius: 11px;
}

.category-text strong {
  font-size: 0.86rem;
}

.category-text small {
  font-size: 0.72rem;
}

/* 3. Card sản phẩm không bị cắt nút, không bị bó quá nhỏ */
.product-grid {
  grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
  gap: 0.9rem;
}

.product-card {
  min-inline-size: 0;
  border-radius: 22px;
}

.product-media {
  block-size: 118px;
}

.product-placeholder {
  block-size: 118px;
}

.product-stock-chip {
  inset-block-start: 0.6rem;
  inset-inline-end: 0.6rem;
}

.product-body {
  padding: 0.85rem;
}

.product-meta {
  gap: 0.5rem;
  margin-block-end: 0.55rem;
}

.product-meta span {
  max-inline-size: 70px;
  font-size: 0.72rem;
}

.product-meta .v-chip {
  max-inline-size: 118px;
}

.product-meta :deep(.v-chip__content) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.product-body h3 {
  min-block-size: 42px;
  font-size: 0.95rem;
  line-height: 1.32;
}

.product-body p {
  margin: 0.45rem 0 0.75rem;
  font-size: 0.78rem;
}

.product-footer {
  display: grid;
  gap: 0.6rem;
  grid-template-columns: 1fr;
  align-items: stretch;
}

.product-footer strong {
  font-size: 1rem;
}

.product-footer .v-btn {
  inline-size: 100%;
  min-inline-size: 0;
  border-radius: 12px;
  font-weight: 800;
}

/* 4. Header danh mục đang xem nhỏ hơn */
.catalog-head {
  border-radius: 20px;
  padding: 0.85rem 1rem;
}

.catalog-head h2 {
  font-size: 1.28rem;
  line-height: 1.25;
}

.catalog-head p {
  font-size: 0.82rem;
}

.catalog-controls {
  align-items: center;
}

.per-page-select {
  inline-size: 108px;
}

/* 5. Giỏ hàng: bỏ scroll sai vùng, chỉ scroll danh sách sản phẩm */
.pos-cart-aside {
  inset-block-start: 76px;
}

.cart-panel {
  max-block-size: calc(100dvh - 92px);
  border-radius: 22px !important;
}

.cart-head {
  padding: 0.95rem 1rem;
}

.cart-head h2 {
  font-size: 1.32rem;
}

.cart-scroll-area {
  flex: 0 0 auto;
  min-block-size: auto;
  max-block-size: none;
  overflow: visible;
}

.cart-empty {
  min-block-size: 150px;
  padding: 1.25rem;
}

.cart-items {
  max-block-size: 220px;
  overflow-y: auto;
  padding: 0.85rem;
}

.cart-items::-webkit-scrollbar {
  inline-size: 6px;
}

.cart-items::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(var(--v-theme-primary), 0.22);
}

.cart-form {
  padding: 0.85rem 1rem;
  gap: 0.75rem;
}

.checkout-footer {
  gap: 0.75rem;
  padding: 0.9rem 1rem 1rem;
}

.cart-summary {
  gap: 0.45rem;
}

.total-box {
  padding-block-start: 0.75rem;
}

.total-box span {
  font-size: 0.98rem;
}

.total-box strong {
  font-size: 1.28rem;
}

.payment-action {
  min-block-size: 48px;
  border-radius: 14px;
}

.cart-secondary-actions {
  gap: 0.55rem;
}

/* 6. Responsive */
@media (max-width: 1480px) {
  .product-grid {
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  }
}

@media (max-width: 1280px) {
  .pos-workspace {
    grid-template-columns: 1fr;
  }

  .cart-panel {
    max-block-size: none;
  }

  .cart-items {
    max-block-size: 280px;
  }
}

@media (max-width: 960px) {
  .catalog-layout {
    grid-template-columns: 1fr;
  }

  .category-list {
    display: flex;
    gap: 0.55rem;
    overflow-x: auto;
  }

  .category-item {
    min-inline-size: 205px;
  }
}

@media (max-width: 760px) {
  .pos-hero {
    padding: 1rem;
  }

  .pos-hero h1 {
    font-size: 2rem;
  }

  .catalog-head,
  .catalog-controls {
    align-items: stretch;
    inline-size: 100%;
  }

  .catalog-controls {
    flex-direction: column;
  }

  .per-page-select {
    inline-size: 100%;
  }

  .product-grid {
    grid-template-columns: 1fr;
  }
}

/* =========================
   CART COMPACT UX FIX
   ========================= */

.pos-cart-aside {
  position: sticky;
  inset-block-start: 76px;
}

.cart-panel {
  display: flex;
  flex-direction: column;
  max-block-size: calc(100dvh - 92px);
  border-radius: 22px !important;
  overflow: hidden;
}

.cart-head {
  flex: 0 0 auto;
  padding: 0.85rem 1rem;
}

.cart-head h2 {
  font-size: 1.22rem;
  line-height: 1.1;
}

.cart-head span {
  font-size: 0.72rem;
}

.cart-scroll-area {
  flex: 1 1 auto;
  min-block-size: 0;
  overflow: auto;
}

/* Danh sách sản phẩm trong giỏ gọn hơn */
.cart-items {
  display: grid;
  gap: 0.6rem;
  padding: 0.75rem;
}

.cart-item {
  gap: 0.55rem;
  border-radius: 16px;
  padding: 0.75rem;
}

.cart-item__info strong {
  font-size: 0.9rem;
  line-height: 1.35;
  white-space: normal;
}

.cart-item__info span {
  font-size: 0.78rem;
}

.cart-item__controls {
  display: grid;
  align-items: center;
  gap: 0.55rem;
  grid-template-columns: auto minmax(0, 1fr) auto;
}

.cart-quantity-control {
  padding: 0.18rem;
  gap: 0.35rem;
}

.cart-quantity-control .v-btn {
  inline-size: 30px;
  block-size: 30px;
}

.cart-quantity-control b {
  min-inline-size: 22px;
  font-size: 0.9rem;
}

.cart-item__controls>strong {
  font-size: 0.92rem;
  text-align: end;
}

/* Form thông tin đơn gọn lại */
.cart-form {
  padding: 0.75rem 1rem;
  gap: 0.65rem;
}

.cart-section-title {
  font-size: 0.72rem;
  letter-spacing: 0.05em;
}

.cart-form :deep(.v-field),
.checkout-footer :deep(.v-field) {
  border-radius: 14px;
}

.cart-form :deep(.v-field__input),
.checkout-footer :deep(.v-field__input) {
  min-block-size: 44px;
  padding-block: 0.35rem;
}

.cart-form :deep(.v-label),
.checkout-footer :deep(.v-label) {
  font-size: 0.82rem;
}

/* Footer thanh toán luôn gọn, không bị trôi */
.checkout-footer {
  flex: 0 0 auto;
  gap: 0.65rem;
  padding: 0.75rem 1rem 0.9rem;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 -12px 30px rgba(15, 23, 42, 0.06);
}

.cart-summary {
  gap: 0.35rem;
}

.cart-summary>div {
  font-size: 0.86rem;
}

.total-box {
  padding-block-start: 0.65rem;
}

.total-box span {
  font-size: 0.95rem;
}

.total-box strong {
  font-size: 1.25rem;
}

.payment-action {
  min-block-size: 46px;
  border-radius: 13px;
  font-size: 0.95rem;
}

.cart-secondary-actions {
  gap: 0.55rem;
}

.cart-secondary-actions .v-btn {
  min-block-size: 40px;
  border-radius: 12px;
  font-size: 0.84rem;
}

/* Ẩn nút tăng giảm mặc định của input number cho đẹp hơn */
.checkout-footer input[type='number']::-webkit-outer-spin-button,
.checkout-footer input[type='number']::-webkit-inner-spin-button {
  margin: 0;
  appearance: none;
}

.checkout-footer input[type='number'] {
  appearance: textfield;
}

/* Mobile/tablet */
@media (max-width: 1280px) {
  .pos-cart-aside {
    position: static;
  }

  .cart-panel {
    max-block-size: none;
  }

  .cart-scroll-area {
    max-block-size: none;
    overflow: visible;
  }
}

/* =========================
   COMPACT POS HEADER FIX
   ========================= */

.pos-hero {
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

.pos-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.pos-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.pos-hero__copy {
  display: none !important;
}

.pos-shift-card {
  display: none !important;
}

.pos-shift-action {
  display: flex;
  align-items: center;
}

.pos-shift-action .v-btn {
  min-block-size: 34px !important;
  border-radius: 10px !important;
  font-weight: 700 !important;
  font-size: 0.84rem !important;
}
</style>
