<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import { getApiBaseUrl } from '@/services/authApi'
import {
  cancelOrder,
  confirmOrder,
  getOrderDetail,
  getOrderInvoice,
  getOrders,
  getOrderStatusHistory,
  returnOrderItems,
  type OrderDto,
  type OrderStatusHistoryDto,
} from '@/services/orderSalesApi'
import { triggerPrint } from '@/utils/printInvoice'

const orders = ref<OrderDto[]>([])
const search = ref('')
const selectedStatus = ref<string | null>(null)
const loading = ref(false)
const errorMessage = ref('')

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

const detailDialog = ref(false)
const returnDialog = ref(false)
const statusHistoryDialog = ref(false)
const confirmDraftDialog = ref(false)

const selectedOrder = ref<OrderDto | null>(null)
const statusHistory = ref<OrderStatusHistoryDto[]>([])
const loadingHistory = ref(false)

const confirmPaymentMethod = ref('Tiền mặt')
const confirmPaidAmount = ref(0)
const confirmErrorMessage = ref('')
const confirmSubmitting = ref(false)

const confirmSplitAmounts = ref({
  cash: 0,
  transfer: 0,
  card: 0,
  ewallet: 0,
  debt: 0,
})

const returnItemsList = ref<Array<{
  orderDetailId: string
  productName: string
  maxQty: number
  returnQty: number
  note: string
}>>([])

const returnErrorMessage = ref('')

const statusOptions = [
  { title: 'Tất cả', value: null },
  { title: 'Đã thanh toán', value: 'Paid' },
  { title: 'Đang chờ', value: 'Pending' },
  { title: 'Ghi nợ', value: 'PartialPaid' },
  { title: 'Đơn nháp', value: 'Draft' },
  { title: 'Báo giá', value: 'Quotation' },
  { title: 'Đã trả', value: 'Returned' },
  { title: 'Đã hủy', value: 'Cancelled' },
]

const confirmPaymentMethods = [
  { title: 'Tiền mặt', value: 'Tiền mặt' },
  { title: 'Chuyển khoản', value: 'Chuyển khoản' },
  { title: 'Thẻ', value: 'Thẻ' },
  { title: 'Ví điện tử', value: 'Ví điện tử' },
  { title: 'Ghi nợ', value: 'Ghi nợ' },
  { title: 'Hỗn hợp', value: 'Hỗn hợp' },
]

const statusLabelMap: Record<string, string> = {
  Paid: 'Đã thanh toán',
  Pending: 'Đang chờ',
  PartialPaid: 'Ghi nợ',
  Debt: 'Ghi nợ',
  Returned: 'Đã trả',
  Cancelled: 'Đã hủy',
  Draft: 'Đơn hàng nháp',
  Quotation: 'Báo giá',
}

const paymentMethodLabelMap: Record<string, string> = {
  cash: 'Tiền mặt',
  transfer: 'Chuyển khoản',
  card: 'Thẻ',
  wallet: 'Ví điện tử',
  ewallet: 'Ví điện tử',
  debt: 'Ghi nợ',
  'Tiền mặt': 'Tiền mặt',
  'Chuyển khoản': 'Chuyển khoản',
  'Thẻ': 'Thẻ',
  'Ví điện tử': 'Ví điện tử',
  'Ghi nợ': 'Ghi nợ',
  'Hỗn hợp': 'Hỗn hợp',
}

const hasOrders = computed(() => orders.value.length > 0)

const moneyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const dateTimeFormatter = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

const formatCurrency = (amount: number) => moneyFormatter.format(amount)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatOrderTime = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? 'Không xác định'
    : dateTimeFormatter.format(date)
}

const statusLabel = (status: string) => statusLabelMap[status] ?? status

const paymentMethodLabel = (method: string | null) => {
  if (!method)
    return 'Chưa ghi nhận'

  return paymentMethodLabelMap[method] ?? method
}

const statusColor = (status: string) => {
  if (status === 'Paid')
    return 'success'

  if (status === 'Pending')
    return 'warning'

  if (status === 'PartialPaid' || status === 'Debt')
    return 'error'

  if (status === 'Draft')
    return 'secondary'

  if (status === 'Quotation')
    return 'info'

  if (status === 'Returned')
    return 'primary'

  if (status === 'Cancelled')
    return 'error'

  return 'secondary'
}

const pageRevenue = computed(() =>
  orders.value.reduce((sum, order) => sum + order.finalAmount, 0),
)

const draftCount = computed(() =>
  orders.value.filter(order => order.status === 'Draft' || order.status === 'Quotation').length,
)

const debtCount = computed(() =>
  orders.value.filter(order => order.status === 'PartialPaid' || order.status === 'Debt').length,
)

const paidCount = computed(() =>
  orders.value.filter(order => order.status === 'Paid').length,
)

const summaryCards = computed(() => [
  {
    label: 'Tổng hóa đơn',
    value: formatNumber(totalCount.value),
    helper: `Trang hiện tại: ${formatNumber(orders.value.length)} đơn`,
    icon: 'ri-shopping-bag-3-line',
    color: 'primary',
  },
  {
    label: 'Doanh thu trang này',
    value: formatCurrency(pageRevenue.value),
    helper: 'Tính theo danh sách đang hiển thị',
    icon: 'ri-money-dollar-circle-line',
    color: 'success',
  },
  {
    label: 'Đơn cần xử lý',
    value: formatNumber(draftCount.value),
    helper: 'Đơn nháp và báo giá',
    icon: 'ri-draft-line',
    color: 'warning',
  },
  {
    label: 'Đơn ghi nợ',
    value: formatNumber(debtCount.value),
    helper: `${formatNumber(paidCount.value)} đơn đã thanh toán`,
    icon: 'ri-file-list-3-line',
    color: 'error',
  },
])

const confirmSplitRemaining = computed(() => {
  if (!selectedOrder.value)
    return 0

  const sumPaid = confirmSplitAmounts.value.cash
    + confirmSplitAmounts.value.transfer
    + confirmSplitAmounts.value.card
    + confirmSplitAmounts.value.ewallet
    + confirmSplitAmounts.value.debt

  return Math.max(0, selectedOrder.value.finalAmount - sumPaid)
})

const canConfirmSelectedOrder = computed(() =>
  selectedOrder.value?.status === 'Draft' || selectedOrder.value?.status === 'Quotation',
)

const canReturnSelectedOrder = computed(() =>
  !!selectedOrder.value
  && selectedOrder.value.status !== 'Cancelled'
  && selectedOrder.value.status !== 'Returned'
  && selectedOrder.value.status !== 'Draft'
  && selectedOrder.value.status !== 'Quotation',
)

const canCancelSelectedOrder = computed(() =>
  !!selectedOrder.value && selectedOrder.value.status !== 'Cancelled',
)

const fillConfirmRemainder = (key: 'cash' | 'transfer' | 'card' | 'ewallet' | 'debt') => {
  if (!selectedOrder.value)
    return

  confirmSplitAmounts.value[key] = 0

  const sumPaid = confirmSplitAmounts.value.cash
    + confirmSplitAmounts.value.transfer
    + confirmSplitAmounts.value.card
    + confirmSplitAmounts.value.ewallet
    + confirmSplitAmounts.value.debt

  confirmSplitAmounts.value[key] = Math.max(0, selectedOrder.value.finalAmount - sumPaid)
}

async function loadOrders() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getOrders({
      search: search.value.trim(),
      status: selectedStatus.value ?? undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    orders.value = response.items
    totalPages.value = response.totalPages
    totalCount.value = response.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách đơn hàng.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = async () => {
  page.value = 1
  await loadOrders()
}

const openOrderDetail = async (order: OrderDto) => {
  loading.value = true
  errorMessage.value = ''

  try {
    selectedOrder.value = await getOrderDetail(order.id)
    detailDialog.value = true
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi tải chi tiết đơn hàng.'
  }
  finally {
    loading.value = false
  }
}

const handleCancelOrder = async () => {
  if (!selectedOrder.value)
    return

  if (!confirm('Bạn có chắc muốn hủy đơn hàng này không?'))
    return

  loading.value = true
  errorMessage.value = ''

  try {
    await cancelOrder(selectedOrder.value.id)
    detailDialog.value = false
    await loadOrders()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi hủy đơn hàng.'
  }
  finally {
    loading.value = false
  }
}

const openReturnDialog = () => {
  if (!selectedOrder.value)
    return

  returnErrorMessage.value = ''

  returnItemsList.value = selectedOrder.value.orderDetails.map(item => ({
    orderDetailId: item.id,
    productName: item.productName,
    maxQty: item.quantity,
    returnQty: 0,
    note: '',
  }))

  returnDialog.value = true
}

const handleReturnOrder = async () => {
  if (!selectedOrder.value)
    return

  const returnPayload = returnItemsList.value
    .filter(item => item.returnQty > 0)
    .map(item => ({
      orderDetailId: item.orderDetailId,
      returnQuantity: item.returnQty,
    }))

  if (returnPayload.length === 0) {
    returnErrorMessage.value = 'Vui lòng nhập số lượng trả lớn hơn 0 cho ít nhất một mặt hàng.'

    return
  }

  loading.value = true
  returnErrorMessage.value = ''

  const reasons = returnItemsList.value
    .filter(item => item.returnQty > 0 && item.note)
    .map(item => `${item.productName}: ${item.note}`)
    .join(', ') || 'Khách hàng trả hàng'

  try {
    await returnOrderItems(selectedOrder.value.id, {
      returnReason: reasons,
      items: returnPayload,
    })

    returnDialog.value = false
    detailDialog.value = false
    await loadOrders()
  }
  catch (error: any) {
    returnErrorMessage.value = error.message || 'Lỗi khi trả hàng.'
  }
  finally {
    loading.value = false
  }
}

const openConfirmDraftDialog = () => {
  if (!selectedOrder.value)
    return

  confirmPaymentMethod.value = 'Tiền mặt'
  confirmPaidAmount.value = selectedOrder.value.finalAmount
  confirmErrorMessage.value = ''
  confirmSplitAmounts.value = {
    cash: 0,
    transfer: 0,
    card: 0,
    ewallet: 0,
    debt: 0,
  }

  confirmDraftDialog.value = true
}

const handleConfirmDraft = async () => {
  if (!selectedOrder.value)
    return

  confirmSubmitting.value = true
  confirmErrorMessage.value = ''

  try {
    let paymentsPayload = null

    if (confirmPaymentMethod.value === 'Hỗn hợp') {
      const splitSum = confirmSplitAmounts.value.cash
        + confirmSplitAmounts.value.transfer
        + confirmSplitAmounts.value.card
        + confirmSplitAmounts.value.ewallet
        + confirmSplitAmounts.value.debt

      if (Math.abs(splitSum - selectedOrder.value.finalAmount) > 1) {
        confirmErrorMessage.value = `Tổng số tiền chia thanh toán (${splitSum.toLocaleString('vi-VN')}đ) phải bằng tổng hóa đơn (${selectedOrder.value.finalAmount.toLocaleString('vi-VN')}đ).`
        confirmSubmitting.value = false

        return
      }

      paymentsPayload = []

      if (confirmSplitAmounts.value.cash > 0)
        paymentsPayload.push({ paymentMethod: 'Tiền mặt', amount: confirmSplitAmounts.value.cash, note: 'Xác nhận chia hóa đơn' })

      if (confirmSplitAmounts.value.transfer > 0)
        paymentsPayload.push({ paymentMethod: 'Chuyển khoản', amount: confirmSplitAmounts.value.transfer, note: 'Xác nhận chia hóa đơn' })

      if (confirmSplitAmounts.value.card > 0)
        paymentsPayload.push({ paymentMethod: 'Thẻ', amount: confirmSplitAmounts.value.card, note: 'Xác nhận chia hóa đơn' })

      if (confirmSplitAmounts.value.ewallet > 0)
        paymentsPayload.push({ paymentMethod: 'Ví điện tử', amount: confirmSplitAmounts.value.ewallet, note: 'Xác nhận chia hóa đơn' })

      if (confirmSplitAmounts.value.debt > 0)
        paymentsPayload.push({ paymentMethod: 'Ghi nợ', amount: confirmSplitAmounts.value.debt, note: 'Xác nhận chia hóa đơn' })
    }

    await confirmOrder(selectedOrder.value.id, {
      paymentMethod: confirmPaymentMethod.value,
      paidAmount: Number(confirmPaidAmount.value) || 0,
      payments: paymentsPayload,
    })

    confirmDraftDialog.value = false
    detailDialog.value = false
    await loadOrders()
  }
  catch (error: any) {
    confirmErrorMessage.value = error.message || 'Lỗi khi xác nhận đơn hàng.'
  }
  finally {
    confirmSubmitting.value = false
  }
}

const handlePrintInvoice = async () => {
  if (!selectedOrder.value)
    return

  try {
    const invoice = await getOrderInvoice(selectedOrder.value.id)

    triggerPrint(invoice)
  }
  catch (error: any) {
    alert(`Lỗi in hóa đơn: ${error.message}`)
  }
}

const showHistory = async () => {
  if (!selectedOrder.value)
    return

  loadingHistory.value = true
  statusHistoryDialog.value = true

  try {
    statusHistory.value = await getOrderStatusHistory(selectedOrder.value.id)
  }
  catch (error: any) {
    console.error('Không thể load lịch sử trạng thái:', error)
  }
  finally {
    loadingHistory.value = false
  }
}

const orderActions = (order: OrderDto): ActionMenuItem[] => [
  {
    label: 'Xem chi tiết',
    icon: 'ri-eye-line',
    handler: () => openOrderDetail(order),
  },
  {
    label: 'In hóa đơn',
    icon: 'ri-printer-line',
    color: 'success',
    handler: () => {
      selectedOrder.value = order
      void handlePrintInvoice()
    },
  },
  {
    label: 'Xác nhận đơn',
    icon: 'ri-checkbox-circle-line',
    color: 'primary',
    handler: () => {
      selectedOrder.value = order
      openConfirmDraftDialog()
    },
    show: order.status === 'Draft' || order.status === 'Quotation',
  },
  {
    label: 'Trả hàng',
    icon: 'ri-arrow-go-back-line',
    color: 'warning',
    handler: () => {
      void openOrderDetail(order).then(() => openReturnDialog())
    },
    show: order.status !== 'Cancelled'
      && order.status !== 'Returned'
      && order.status !== 'Draft'
      && order.status !== 'Quotation',
  },
  {
    label: 'Hủy đơn',
    icon: 'ri-close-circle-line',
    color: 'error',
    handler: () => {
      selectedOrder.value = order
      void handleCancelOrder()
    },
    show: order.status !== 'Cancelled',
  },
]

const handleExportCsv = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const queryParams = new URLSearchParams()

    if (search.value)
      queryParams.append('search', search.value.trim())

    if (selectedStatus.value)
      queryParams.append('status', selectedStatus.value)

    const token = localStorage.getItem('accessToken') || ''

    const response = await fetch(`${getApiBaseUrl()}/api/orders/export?${queryParams.toString()}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })

    if (!response.ok)
      throw new Error('Xuất CSV thất bại')

    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')

    link.href = url
    link.download = `orders-${new Date().toISOString().slice(0, 10)}.csv`

    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xuất CSV.'
  }
  finally {
    loading.value = false
  }
}

watch(page, () => {
  void loadOrders()
})

watch(selectedStatus, () => {
  void resetAndLoad()
})

onMounted(() => {
  void loadOrders()
})
</script>

<template>
  <section class="orders-page">
    <div class="orders-hero">
      <div class="orders-hero__title-area">
        <h1>Quản lý đơn hàng</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-receipt-line" class="ml-2">
          Đơn hàng
        </VChip>
      </div>

      <div class="orders-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-download-line" :loading="loading"
          @click="handleExportCsv">
          Xuất CSV
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-add-line" to="/pos" class="primary-action">
          Tạo đơn POS
        </VBtn>
      </div>
    </div>

    <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''">
      {{ errorMessage }}
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

    <VCard class="orders-panel">
      <VCardText>
        <div class="orders-toolbar">
          <VTextField v-model="search" label="Tìm kiếm đơn hàng" placeholder="Mã đơn, khách hàng, thu ngân..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable class="orders-search"
            @keyup.enter="resetAndLoad" />

          <VSelect v-model="selectedStatus" label="Trạng thái" :items="statusOptions" item-title="title"
            item-value="value" density="comfortable" hide-details class="orders-status-filter" />

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
        <div v-if="hasOrders" class="orders-table-wrap">
          <VTable class="orders-table">
            <thead>
              <tr>
                <th>Mã đơn</th>
                <th>Khách hàng</th>
                <th>Thu ngân</th>
                <th>Thời gian</th>
                <th>Thanh toán</th>
                <th class="text-end">Tổng tiền</th>
                <th>Trạng thái</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="order in orders" :key="order.id" class="order-row" @click="openOrderDetail(order)">
                <td>
                  <div class="order-code">
                    <VIcon icon="ri-receipt-line" />
                    <strong>{{ order.orderCode }}</strong>
                  </div>
                </td>

                <td>
                  <div class="cell-main">
                    <strong>{{ order.customerName }}</strong>
                    <span>Khách hàng</span>
                  </div>
                </td>

                <td>
                  <div class="cell-main">
                    <strong>{{ order.createdByName }}</strong>
                    <span>Thu ngân</span>
                  </div>
                </td>

                <td>{{ formatOrderTime(order.orderDate) }}</td>

                <td>
                  <VChip color="secondary" variant="tonal" size="small">
                    {{ paymentMethodLabel(order.paymentMethod) }}
                  </VChip>
                </td>

                <td class="text-end amount-cell">
                  {{ formatCurrency(order.finalAmount) }}
                </td>

                <td>
                  <VChip :color="statusColor(order.status)" variant="tonal" size="small">
                    {{ statusLabel(order.status) }}
                  </VChip>
                </td>

                <td class="text-center" @click.stop>
                  <RetailActionMenu :items="orderActions(order)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="orders-empty">
          <VIcon icon="ri-shopping-bag-line" size="42" color="primary" />
          <strong>Chưa có đơn hàng phù hợp</strong>
          <span>Thử thay đổi bộ lọc hoặc tạo đơn hàng mới từ POS.</span>

          <VBtn color="primary" prepend-icon="ri-add-line" to="/pos">
            Tạo đơn POS
          </VBtn>
        </div>
      </template>

      <div v-if="orders.length || totalCount > 0" class="orders-pagination">
        <span>
          Hiển thị {{ formatNumber(orders.length) }} trên tổng số {{ formatNumber(totalCount) }} hóa đơn
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" @update:model-value="resetAndLoad" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="detailDialog" max-width="980" scrollable>
      <VCard v-if="selectedOrder" class="order-dialog">
        <div class="dialog-head">
          <div>
            <span>Chi tiết đơn hàng</span>
            <h2>{{ selectedOrder.orderCode }}</h2>
          </div>

          <VChip :color="statusColor(selectedOrder.status)" variant="tonal">
            {{ statusLabel(selectedOrder.status) }}
          </VChip>
        </div>

        <VCardText>
          <div class="detail-grid">
            <div class="detail-card">
              <span>Khách hàng</span>
              <strong>{{ selectedOrder.customerName }}</strong>
              <p>{{ paymentMethodLabel(selectedOrder.paymentMethod) }}</p>
            </div>

            <div class="detail-card">
              <span>Thu ngân</span>
              <strong>{{ selectedOrder.createdByName }}</strong>
              <p>{{ formatOrderTime(selectedOrder.orderDate) }}</p>
            </div>

            <div class="detail-card">
              <span>Khuyến mãi</span>
              <strong>{{ selectedOrder.promotionCode || 'Không áp dụng' }}</strong>
              <p>{{ selectedOrder.promotionName || selectedOrder.note || '—' }}</p>
            </div>
          </div>

          <div class="dialog-section-title">
            Sản phẩm trong đơn
          </div>

          <div class="dialog-table-wrap">
            <VTable class="orders-table compact-table">
              <thead>
                <tr>
                  <th>Tên sản phẩm</th>
                  <th>Mã hàng</th>
                  <th>ĐVT</th>
                  <th class="text-end">Đơn giá</th>
                  <th class="text-center">SL</th>
                  <th class="text-end">CK</th>
                  <th class="text-end">Thành tiền</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in selectedOrder.orderDetails" :key="item.id">
                  <td class="font-weight-bold">{{ item.productName }}</td>
                  <td>{{ item.productCode }}</td>
                  <td>{{ item.unitName }}</td>
                  <td class="text-end">{{ formatCurrency(item.unitPrice) }}</td>
                  <td class="text-center">{{ item.quantity }}</td>
                  <td class="text-end">{{ item.discountPercent }}%</td>
                  <td class="text-end amount-cell">{{ formatCurrency(item.subTotal) }}</td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div class="invoice-summary">
            <div>
              <span>Tổng tiền hàng</span>
              <strong>{{ formatCurrency(selectedOrder.subTotal) }}</strong>
            </div>

            <div v-if="selectedOrder.discountAmount > 0">
              <span>Giảm giá trực tiếp</span>
              <strong>-{{ formatCurrency(selectedOrder.discountAmount) }}</strong>
            </div>

            <div v-if="selectedOrder.promotionDiscountAmount > 0">
              <span>Giảm giá khuyến mãi</span>
              <strong>-{{ formatCurrency(selectedOrder.promotionDiscountAmount) }}</strong>
            </div>

            <div class="invoice-total">
              <span>Thành tiền</span>
              <strong>{{ formatCurrency(selectedOrder.finalAmount) }}</strong>
            </div>

            <div>
              <span>Đã thanh toán</span>
              <strong>{{ formatCurrency(selectedOrder.paidAmount) }}</strong>
            </div>

            <div class="text-error">
              <span>Còn nợ</span>
              <strong>{{ formatCurrency(selectedOrder.debtAmount) }}</strong>
            </div>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VBtn color="info" variant="tonal" prepend-icon="ri-history-line" @click="showHistory">
            Lịch sử
          </VBtn>

          <VBtn color="success" variant="tonal" prepend-icon="ri-printer-line" @click="handlePrintInvoice">
            In hóa đơn
          </VBtn>

          <VSpacer />

          <VBtn v-if="canConfirmSelectedOrder" color="primary" prepend-icon="ri-checkbox-circle-line"
            @click="openConfirmDraftDialog">
            Xác nhận đơn
          </VBtn>

          <VBtn v-if="canReturnSelectedOrder" color="warning" variant="tonal" prepend-icon="ri-arrow-go-back-line"
            @click="openReturnDialog">
            Trả hàng
          </VBtn>

          <VBtn v-if="canCancelSelectedOrder" color="error" variant="tonal" prepend-icon="ri-close-circle-line"
            @click="handleCancelOrder">
            Hủy đơn
          </VBtn>

          <VBtn color="secondary" variant="outlined" @click="detailDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="returnDialog" max-width="840">
      <VCard class="order-dialog">
        <div class="dialog-head">
          <div>
            <span>Yêu cầu trả hàng</span>
            <h2>{{ selectedOrder?.orderCode }}</h2>
          </div>
        </div>

        <VCardText>
          <VAlert v-if="returnErrorMessage" type="error" variant="tonal" class="mb-4">
            {{ returnErrorMessage }}
          </VAlert>

          <div class="dialog-table-wrap">
            <VTable class="orders-table compact-table return-table">
              <thead>
                <tr>
                  <th style="min-width: 250px;">Sản phẩm</th>
                  <th class="text-center" style="width: 100px;">Đã mua</th>
                  <th class="text-center" style="width: 120px;">SL trả</th>
                  <th style="min-width: 200px;">Lý do trả</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in returnItemsList" :key="item.orderDetailId">
                  <td class="font-weight-bold">{{ item.productName }}</td>
                  <td class="text-center font-weight-bold">{{ item.maxQty }}</td>
                  <td>
                    <VTextField v-model.number="item.returnQty" type="number" min="0" :max="item.maxQty"
                      density="compact" variant="outlined" hide-details style="max-width: 80px; margin: 0 auto;" />
                  </td>
                  <td>
                    <VTextField v-model="item.note" placeholder="Lý do..." density="compact" variant="outlined"
                      hide-details />
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />
          <VBtn color="secondary" variant="outlined" @click="returnDialog = false">
            Hủy
          </VBtn>
          <VBtn color="primary" variant="elevated" prepend-icon="ri-check-line" @click="handleReturnOrder">
            Xác nhận trả hàng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="statusHistoryDialog" max-width="560">
      <VCard class="order-dialog">
        <div class="dialog-head">
          <div>
            <span>Lịch sử trạng thái</span>
            <h2>{{ selectedOrder?.orderCode }}</h2>
          </div>
        </div>

        <VCardText>
          <div v-if="loadingHistory" class="history-loading">
            <VProgressCircular indeterminate />
          </div>

          <div v-else-if="statusHistory.length" class="history-list">
            <article v-for="log in statusHistory" :key="log.id" class="history-item">
              <div class="history-dot" />

              <div>
                <strong>{{ statusLabel(log.toStatus) }}</strong>
                <span>{{ formatOrderTime(log.createdAt) }} · {{ log.createdByName }}</span>
                <p v-if="log.note">{{ log.note }}</p>
              </div>
            </article>
          </div>

          <div v-else class="orders-empty small-empty">
            <VIcon icon="ri-history-line" />
            <strong>Chưa có lịch sử</strong>
            <span>Đơn hàng chưa ghi nhận thay đổi trạng thái.</span>
          </div>
        </VCardText>

        <VCardActions>
          <VSpacer />
          <VBtn color="secondary" @click="statusHistoryDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="confirmDraftDialog" max-width="560">
      <VCard v-if="selectedOrder" class="order-dialog">
        <div class="dialog-head">
          <div>
            <span>Xác nhận đơn hàng</span>
            <h2>{{ selectedOrder.orderCode }}</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ formatCurrency(selectedOrder.finalAmount) }}
          </VChip>
        </div>

        <VCardText>
          <VAlert v-if="confirmErrorMessage" type="error" variant="tonal" class="mb-4">
            {{ confirmErrorMessage }}
          </VAlert>

          <p class="confirm-text">
            Xác nhận đơn hàng của khách <strong>{{ selectedOrder.customerName }}</strong>.
          </p>

          <VSelect v-model="confirmPaymentMethod" label="Phương thức thanh toán" :items="confirmPaymentMethods"
            item-title="title" item-value="value" density="comfortable" class="mb-4" />

          <VTextField v-if="confirmPaymentMethod !== 'Hỗn hợp'" v-model.number="confirmPaidAmount" type="number"
            label="Số tiền khách trả" suffix="đ" density="comfortable" class="mb-4" />

          <div v-else class="split-box">
            <div class="split-head">
              <strong>Chia hóa đơn</strong>

              <VChip size="small" :color="confirmSplitRemaining === 0 ? 'success' : 'warning'" variant="tonal">
                Còn lại: {{ formatCurrency(confirmSplitRemaining) }}
              </VChip>
            </div>

            <VTextField v-model.number="confirmSplitAmounts.cash" label="Tiền mặt" type="number" suffix="đ"
              density="compact" hide-details>
              <template #append-inner>
                <VBtn size="x-small" variant="text" color="primary" @click="fillConfirmRemainder('cash')">
                  Điền nốt
                </VBtn>
              </template>
            </VTextField>

            <VTextField v-model.number="confirmSplitAmounts.transfer" label="Chuyển khoản" type="number" suffix="đ"
              density="compact" hide-details>
              <template #append-inner>
                <VBtn size="x-small" variant="text" color="primary" @click="fillConfirmRemainder('transfer')">
                  Điền nốt
                </VBtn>
              </template>
            </VTextField>

            <VTextField v-model.number="confirmSplitAmounts.card" label="Thẻ" type="number" suffix="đ" density="compact"
              hide-details>
              <template #append-inner>
                <VBtn size="x-small" variant="text" color="primary" @click="fillConfirmRemainder('card')">
                  Điền nốt
                </VBtn>
              </template>
            </VTextField>

            <VTextField v-model.number="confirmSplitAmounts.ewallet" label="Ví điện tử" type="number" suffix="đ"
              density="compact" hide-details>
              <template #append-inner>
                <VBtn size="x-small" variant="text" color="primary" @click="fillConfirmRemainder('ewallet')">
                  Điền nốt
                </VBtn>
              </template>
            </VTextField>

            <VTextField v-model.number="confirmSplitAmounts.debt" label="Ghi nợ" type="number" suffix="đ"
              density="compact" hide-details>
              <template #append-inner>
                <VBtn size="x-small" variant="text" color="primary" @click="fillConfirmRemainder('debt')">
                  Điền nốt
                </VBtn>
              </template>
            </VTextField>
          </div>
        </VCardText>

        <VCardActions>
          <VSpacer />
          <VBtn color="secondary" variant="text" @click="confirmDraftDialog = false">
            Hủy
          </VBtn>
          <VBtn color="primary" :loading="confirmSubmitting" @click="handleConfirmDraft">
            Xác nhận
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.orders-page {
  position: relative;
  isolation: isolate;
}

.orders-page::before {
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
  gap: 0.85rem;
  align-items: flex-start;
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

.orders-panel,
.order-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.orders-toolbar {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) minmax(190px, 240px) auto auto;
  align-items: center;
}

.orders-toolbar :deep(.v-field) {
  border-radius: 16px;
}

.orders-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.orders-table-wrap,
.dialog-table-wrap {
  overflow-x: auto;
}

.orders-table {
  min-inline-size: 880px;
}

.orders-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.orders-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.order-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.order-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.order-code {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: rgb(var(--v-theme-primary));
}

.cell-main strong,
.cell-main span {
  display: block;
}

.cell-main strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 800;
}

.cell-main span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.amount-cell {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.orders-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.orders-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.small-empty {
  min-block-size: 180px;
}

.orders-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.orders-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.orders-pagination>div {
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

.detail-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1.2rem;
}

.detail-card {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.95rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.detail-card span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.detail-card strong {
  display: block;
  margin-block: 0.3rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
  font-weight: 900;
}

.detail-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.dialog-section-title {
  margin-block-end: 0.7rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.78rem;
  font-weight: 950;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.compact-table {
  min-inline-size: 820px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 54px !important;
}

.invoice-summary {
  display: grid;
  gap: 0.55rem;
  max-inline-size: 420px;
  margin-inline-start: auto;
  margin-block-start: 1rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.5);
}

.invoice-summary>div {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
}

.invoice-summary span {
  color: rgba(var(--v-theme-on-surface), 0.62);
}

.invoice-summary strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.invoice-total {
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding-block-start: 0.65rem;
}

.invoice-total strong {
  color: rgb(var(--v-theme-primary));
  font-size: 1.2rem;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}

.split-box {
  display: grid;
  gap: 0.75rem;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.split-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.confirm-text {
  color: rgba(var(--v-theme-on-surface), 0.68);
}

.history-loading {
  display: grid;
  place-items: center;
  min-block-size: 160px;
}

.history-list {
  display: grid;
  gap: 0.8rem;
}

.history-item {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: 14px minmax(0, 1fr);
  border: 1px solid rgba(var(--v-border-color), 0.1);
  border-radius: 16px;
  padding: 0.85rem;
  background: rgba(var(--v-theme-background), 0.44);
}

.history-dot {
  inline-size: 12px;
  block-size: 12px;
  border-radius: 999px;
  margin-block-start: 0.3rem;
  background: rgb(var(--v-theme-primary));
  box-shadow: 0 0 0 5px rgba(var(--v-theme-primary), 0.1);
}

.history-item strong,
.history-item span,
.history-item p {
  display: block;
}

.history-item strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.history-item span {
  margin-block-start: 0.2rem;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.84rem;
}

.history-item p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.68);
}

.return-table {
  min-inline-size: 100% !important;
}

.return-table :deep(input) {
  text-align: center;
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .orders-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .orders-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .orders-toolbar,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .orders-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .orders-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .invoice-summary {
    max-inline-size: none;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.orders-hero {
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

.orders-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.orders-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.orders-hero__copy {
  display: none !important;
}

.orders-hero__actions,
.orders-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.orders-hero__actions .v-btn,
.orders-actions .v-btn,
.orders-hero__actions .v-btn.primary-action,
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