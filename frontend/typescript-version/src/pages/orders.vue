<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import {
  getOrders,
  getOrderDetail,
  cancelOrder,
  returnOrderItems,
  getOrderInvoice,
  getOrderStatusHistory,
  type OrderDto,
  type OrderInvoiceDto,
  type OrderStatusHistoryDto,
} from '@/services/orderSalesApi'

const orders = ref<OrderDto[]>([])
const search = ref('')
const selectedStatus = ref<string | null>(null)
const loading = ref(false)
const errorMessage = ref('')

// Dialog States
const detailDialog = ref(false)
const returnDialog = ref(false)
const statusHistoryDialog = ref(false)
const selectedOrder = ref<OrderDto | null>(null)
const statusHistory = ref<OrderStatusHistoryDto[]>([])
const loadingHistory = ref(false)

// Return Form
const returnItemsList = ref<Array<{ productId: string; productName: string; maxQty: number; returnQty: number; note: string }>>([])
const returnErrorMessage = ref('')

const statusOptions = [
  { title: 'Tất cả', value: null },
  { title: 'Đã thanh toán', value: 'Paid' },
  { title: 'Đang chờ', value: 'Pending' },
  { title: 'Ghi nợ', value: 'PartialPaid' },
  { title: 'Đã trả', value: 'Returned' },
  { title: 'Đã hủy', value: 'Cancelled' },
]

const statusLabelMap: Record<string, string> = {
  Paid: 'Đã thanh toán',
  Pending: 'Đang chờ',
  PartialPaid: 'Ghi nợ',
  Debt: 'Ghi nợ',
  Returned: 'Đã trả',
  Cancelled: 'Đã hủy',
  Draft: 'Bản nháp',
}

const paymentMethodLabelMap: Record<string, string> = {
  cash: 'Tiền mặt',
  transfer: 'Chuyển khoản',
  card: 'Thẻ',
  wallet: 'Ví điện tử',
  debt: 'Ghi nợ',
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

const formatOrderTime = (value: string) => {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Không xác định' : dateTimeFormatter.format(date)
}

const statusLabel = (status: string) => statusLabelMap[status] ?? status

const paymentMethodLabel = (method: string | null) => {
  if (!method)
    return 'Chưa ghi nhận'
  return paymentMethodLabelMap[method] ?? method
}

async function loadOrders() {
  loading.value = true
  errorMessage.value = ''

  try {
    const response = await getOrders({
      search: search.value.trim(),
      status: selectedStatus.value ?? undefined,
      page: 1,
      pageSize: 50,
    })

    orders.value = response.items
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

const openOrderDetail = async (order: OrderDto) => {
  loading.value = true
  errorMessage.value = ''
  try {
    selectedOrder.value = await getOrderDetail(order.id)
    detailDialog.value = true
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi tải chi tiết đơn hàng.'
  } finally {
    loading.value = false
  }
}

const handleCancelOrder = async () => {
  if (!selectedOrder.value) return
  if (!confirm('Bạn có chắc muốn hủy đơn hàng này không?')) return

  loading.value = true
  errorMessage.value = ''
  try {
    await cancelOrder(selectedOrder.value.id)
    detailDialog.value = false
    await loadOrders()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi hủy đơn hàng.'
  } finally {
    loading.value = false
  }
}

const openReturnDialog = () => {
  if (!selectedOrder.value) return
  returnItemsList.value = selectedOrder.value.orderDetails.map(item => ({
    productId: item.productId,
    productName: item.productName,
    maxQty: item.quantity,
    returnQty: 0,
    note: '',
  }))
  returnDialog.value = true
}

const handleReturnOrder = async () => {
  if (!selectedOrder.value) return
  const returnPayload = returnItemsList.value
    .filter(item => item.returnQty > 0)
    .map(item => ({
      productId: item.productId,
      quantity: item.returnQty,
      note: item.note || null,
    }))

  if (returnPayload.length === 0) {
    returnErrorMessage.value = 'Vui lòng nhập số lượng trả lớn hơn 0 cho ít nhất một mặt hàng.'
    return
  }

  loading.value = true
  returnErrorMessage.value = ''
  try {
    await returnOrderItems(selectedOrder.value.id, { items: returnPayload })
    returnDialog.value = false
    detailDialog.value = false
    await loadOrders()
  } catch (error: any) {
    returnErrorMessage.value = error.message || 'Lỗi khi trả hàng.'
  } finally {
    loading.value = false
  }
}

const handlePrintInvoice = async () => {
  if (!selectedOrder.value) return
  try {
    const invoice = await getOrderInvoice(selectedOrder.value.id)
    const printWindow = window.open('', '_blank')
    if (printWindow) {
      let itemsHtml = ''
      for (const item of invoice.items) {
        itemsHtml += '<tr>' +
          '<td>' + item.productName + '</td>' +
          '<td>' + item.unitName + '</td>' +
          '<td>' + item.unitPrice.toLocaleString('vi-VN') + ' đ</td>' +
          '<td style="text-align: center;">' + item.quantity + '</td>' +
          '<td>' + item.subTotal.toLocaleString('vi-VN') + ' đ</td>' +
          '</tr>'
      }

      printWindow.document.write('<html>' +
        '<head>' +
        '<title>Hóa đơn ' + invoice.orderCode + '</title>' +
        '<style>' +
        'body { font-family: "Arial", sans-serif; padding: 20px; line-height: 1.5; color: #333; }' +
        '.header { text-align: center; margin-bottom: 20px; }' +
        '.details { margin-bottom: 20px; }' +
        '.table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }' +
        '.table th, .table td { border: 1px solid #ddd; padding: 8px; text-align: left; }' +
        '.table th { background-color: #f5f5f5; }' +
        '.total-section { text-align: right; font-weight: bold; }' +
        '</style>' +
        '</head>' +
        '<body>' +
        '<div class="header">' +
        '<h2>HÓA ĐƠN BÁN HÀNG</h2>' +
        '<p>Mã đơn: <strong>' + invoice.orderCode + '</strong></p>' +
        '<p>Ngày tạo: ' + new Date(invoice.orderDate).toLocaleString('vi-VN') + '</p>' +
        '</div>' +
        '<div class="details">' +
        '<p>Khách hàng: <strong>' + invoice.customerName + '</strong></p>' +
        '<p>Số điện thoại: ' + (invoice.customerPhone || '—') + '</p>' +
        '<p>Địa chỉ: ' + (invoice.customerAddress || '—') + '</p>' +
        '<p>Thu ngân: ' + invoice.createdByName + '</p>' +
        '</div>' +
        '<table class="table">' +
        '<thead>' +
        '<tr>' +
        '<th>Sản phẩm</th>' +
        '<th>ĐVT</th>' +
        '<th>Đơn giá</th>' +
        '<th style="text-align: center;">Số lượng</th>' +
        '<th>Thành tiền</th>' +
        '</tr>' +
        '</thead>' +
        '<tbody>' +
        itemsHtml +
        '</tbody>' +
        '</table>' +
        '<div class="total-section">' +
        '<p>Tổng tiền hàng: ' + invoice.subTotal.toLocaleString('vi-VN') + ' đ</p>' +
        '<p>Chiết khấu: -' + invoice.discountAmount.toLocaleString('vi-VN') + ' đ</p>' +
        '<p>Thành tiền: ' + invoice.finalAmount.toLocaleString('vi-VN') + ' đ</p>' +
        '<p>Đã trả: ' + invoice.paidAmount.toLocaleString('vi-VN') + ' đ</p>' +
        '<p>Còn nợ: ' + invoice.debtAmount.toLocaleString('vi-VN') + ' đ</p>' +
        '</div>' +
        '<script>' +
        'window.onload = function() { window.print(); window.close(); }' +
        '</' + 'script>' +
        '</body>' +
        '</html>')
      printWindow.document.close()
    }
  } catch (error: any) {
    alert('Lỗi in hóa đơn: ' + error.message)
  }
}

const showHistory = async () => {
  if (!selectedOrder.value) return
  loadingHistory.value = true
  statusHistoryDialog.value = true
  try {
    statusHistory.value = await getOrderStatusHistory(selectedOrder.value.id)
  } catch (error: any) {
    console.error('Không thể load lịch sử trạng thái:', error)
  } finally {
    loadingHistory.value = false
  }
}

const orderActions = (order: OrderDto): ActionMenuItem[] => [
  { label: 'Xem chi tiết', icon: 'ri-eye-line', handler: () => openOrderDetail(order) },
  { label: 'In hóa đơn', icon: 'ri-printer-line', color: 'success', handler: () => { selectedOrder.value = order; handlePrintInvoice() } },
  { label: 'Trả hàng', icon: 'ri-arrow-go-back-line', color: 'warning', handler: () => { openOrderDetail(order).then(() => openReturnDialog()) }, show: order.status !== 'Cancelled' && order.status !== 'Returned' },
  { label: 'Hủy đơn', icon: 'ri-close-circle-line', color: 'error', handler: () => { selectedOrder.value = order; handleCancelOrder() }, show: order.status !== 'Cancelled' },
]

watch(selectedStatus, () => {
  void loadOrders()
})

onMounted(() => {
  void loadOrders()
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Đơn hàng"
    title="Quản lý đơn hàng"
    subtitle="Theo dõi hóa đơn POS, trạng thái thanh toán, đơn ghi nợ và giao dịch trả hàng."
  >
    <template #actions>
      <VBtn
        prepend-icon="ri-add-line"
        to="/pos"
      >
        Tạo đơn POS
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
    <RetailFilterBar
      v-model="search"
      search-placeholder="Mã đơn, khách hàng, thu ngân..."
      :filters="[{
        key: 'status',
        label: 'Trạng thái',
        items: statusOptions,
        modelValue: selectedStatus,
      }]"
      :loading="loading"
      @search="loadOrders"
      @reload="loadOrders"
      @filterChange="(_key: string, val: any) => { selectedStatus = val }"
    />

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
          <th>Mã đơn</th>
          <th>Khách hàng</th>
          <th>Thu ngân</th>
          <th>Giờ</th>
          <th>Thanh toán</th>
          <th class="text-end">Tổng tiền</th>
          <th>Trạng thái</th>
          <th class="text-center" style="width: 60px;">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="order in orders"
          :key="order.id"
          class="hover-row"
          @click="openOrderDetail(order)"
        >
          <td class="font-weight-bold text-primary">
            {{ order.orderCode }}
          </td>
          <td>{{ order.customerName }}</td>
          <td>{{ order.createdByName }}</td>
          <td>{{ formatOrderTime(order.orderDate) }}</td>
          <td>{{ paymentMethodLabel(order.paymentMethod) }}</td>
          <td class="text-end font-weight-bold">
            {{ formatCurrency(order.finalAmount) }}
          </td>
          <td>
            <RetailStatusBadge
              :status="statusLabel(order.status)"
              dot
            />
          </td>
          <td
            class="text-center"
            @click.stop
          >
            <RetailActionMenu :items="orderActions(order)" />
          </td>
        </tr>
        <RetailEmptyState
          v-if="!loading && !hasOrders"
          :colspan="8"
          icon="ri-shopping-bag-line"
          title="Chưa có đơn hàng phù hợp"
          subtitle="Thử thay đổi bộ lọc hoặc tạo đơn hàng mới từ POS."
          action-label="Tạo đơn POS"
          action-to="/pos"
        />
      </tbody>
    </VTable>
  </VCard>

  <!-- Order Detail Dialog -->
  <VDialog
    v-model="detailDialog"
    max-width="850"
  >
    <VCard v-if="selectedOrder">
      <VCardTitle class="d-flex justify-between align-center">
        <span>Chi tiết đơn hàng: {{ selectedOrder.orderCode }}</span>
        <RetailStatusChip :status="statusLabel(selectedOrder.status)" />
      </VCardTitle>
      <VCardText>
        <VRow class="mb-4">
          <VCol cols="12" sm="6">
            <div><strong>Khách hàng:</strong> {{ selectedOrder.customerName }}</div>
            <div><strong>Thanh toán:</strong> {{ paymentMethodLabel(selectedOrder.paymentMethod) }}</div>
            <div><strong>Ngày tạo:</strong> {{ formatOrderTime(selectedOrder.orderDate) }}</div>
          </VCol>
          <VCol cols="12" sm="6">
            <div><strong>Thu ngân:</strong> {{ selectedOrder.createdByName }}</div>
            <div><strong>Ghi chú:</strong> {{ selectedOrder.note || '—' }}</div>
            <div v-if="selectedOrder.promotionCode">
              <strong>Khuyến mãi:</strong> {{ selectedOrder.promotionCode }} - {{ selectedOrder.promotionName }}
            </div>
          </VCol>
        </VRow>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Tên sản phẩm</th>
              <th>Mã hàng</th>
              <th>ĐVT</th>
              <th class="text-end">Đơn giá</th>
              <th class="text-center">SL</th>
              <th class="text-end">Chiết khấu (%)</th>
              <th class="text-end">Thành tiền</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in selectedOrder.orderDetails"
              :key="item.id"
            >
              <td class="font-weight-bold">{{ item.productName }}</td>
              <td>{{ item.productCode }}</td>
              <td>{{ item.unitName }}</td>
              <td class="text-end">{{ formatCurrency(item.unitPrice) }}</td>
              <td class="text-center">{{ item.quantity }}</td>
              <td class="text-end">{{ item.discountPercent }}%</td>
              <td class="text-end font-weight-bold">{{ formatCurrency(item.subTotal) }}</td>
            </tr>
          </tbody>
        </VTable>

        <VRow justify="end">
          <VCol cols="12" sm="6" md="4" class="text-right">
            <div>Tổng tiền hàng: {{ formatCurrency(selectedOrder.subTotal) }}</div>
            <div v-if="selectedOrder.discountAmount > 0">Giảm giá trực tiếp: -{{ formatCurrency(selectedOrder.discountAmount) }}</div>
            <div v-if="selectedOrder.promotionDiscountAmount > 0">Giảm giá KM: -{{ formatCurrency(selectedOrder.promotionDiscountAmount) }}</div>
            <hr class="my-2" />
            <div class="text-h6 font-weight-bold">Thành tiền: <span class="text-primary">{{ formatCurrency(selectedOrder.finalAmount) }}</span></div>
            <div>Đã thanh toán: {{ formatCurrency(selectedOrder.paidAmount) }}</div>
            <div class="text-error font-weight-bold">Còn nợ: {{ formatCurrency(selectedOrder.debtAmount) }}</div>
          </VCol>
        </VRow>
      </VCardText>
      <VCardActions class="flex-wrap gap-2">
        <VBtn
          color="info"
          variant="tonal"
          prepend-icon="ri-history-line"
          @click="showHistory"
        >
          Lịch sử trạng thái
        </VBtn>
        <VBtn
          color="success"
          variant="tonal"
          prepend-icon="ri-printer-line"
          @click="handlePrintInvoice"
        >
          In hóa đơn
        </VBtn>
        <VSpacer />
        <VBtn
          v-if="selectedOrder.status !== 'Cancelled' && selectedOrder.status !== 'Returned'"
          color="warning"
          variant="tonal"
          prepend-icon="ri-arrow-go-back-line"
          @click="openReturnDialog"
        >
          Trả hàng
        </VBtn>
        <VBtn
          v-if="selectedOrder.status !== 'Cancelled'"
          color="error"
          variant="tonal"
          prepend-icon="ri-close-circle-line"
          @click="handleCancelOrder"
        >
          Hủy đơn
        </VBtn>
        <VBtn
          color="secondary"
          @click="detailDialog = false"
        >
          Đóng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Return Items Dialog -->
  <VDialog
    v-model="returnDialog"
    max-width="600"
  >
    <VCard>
      <VCardTitle>Yêu cầu trả hàng</VCardTitle>
      <VCardText>
        <VAlert
          v-if="returnErrorMessage"
          type="error"
          variant="tonal"
          class="mb-4"
        >
          {{ returnErrorMessage }}
        </VAlert>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th class="text-center">Đã mua</th>
              <th class="text-center" style="width: 120px;">SL trả</th>
              <th>Lý do trả</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in returnItemsList"
              :key="item.productId"
            >
              <td>{{ item.productName }}</td>
              <td class="text-center font-weight-bold">{{ item.maxQty }}</td>
              <td>
                <VTextField
                  v-model.number="item.returnQty"
                  type="number"
                  min="0"
                  :max="item.maxQty"
                  density="compact"
                  hide-details
                />
              </td>
              <td>
                <VTextField
                  v-model="item.note"
                  placeholder="Lý do..."
                  density="compact"
                  hide-details
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
          variant="text"
          @click="returnDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleReturnOrder"
        >
          Xác nhận trả hàng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Status History Dialog -->
  <VDialog
    v-model="statusHistoryDialog"
    max-width="500"
  >
    <VCard>
      <VCardTitle>Lịch sử trạng thái đơn hàng</VCardTitle>
      <VCardText>
        <VProgressCircular v-if="loadingHistory" indeterminate class="d-block mx-auto my-4" />
        <VList v-else-if="statusHistory.length">
          <VListItem
            v-for="log in statusHistory"
            :key="log.id"
            class="py-3 border-bottom"
          >
            <VListItemTitle class="font-weight-bold">
              Trạng thái: {{ statusLabel(log.toStatus) }}
            </VListItemTitle>
            <VListItemSubtitle>
              <div>Người thực hiện: {{ log.createdByName }}</div>
              <div>Thời gian: {{ formatOrderTime(log.createdAt) }}</div>
              <div v-if="log.note">Ghi chú: {{ log.note }}</div>
            </VListItemSubtitle>
          </VListItem>
        </VList>
        <div v-else class="text-center text-medium-emphasis py-4">Chưa ghi nhận lịch sử thay đổi nào.</div>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn color="secondary" @click="statusHistoryDialog = false">Đóng</VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
