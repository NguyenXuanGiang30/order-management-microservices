<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'

import {
  type GoodsReceiptDto,
  type ProductDto,
  cancelGoodsReceipt,
  confirmGoodsReceipt,
  createGoodsReceipt,
  getGoodsReceipts,
  getProducts,
  importGoodsReceiptItems,
} from '@/services/productInventoryApi'

import {
  type SupplierDto,
  getSuppliers,
} from '@/services/orderSalesApi'

const receipts = ref<GoodsReceiptDto[]>([])
const suppliers = ref<SupplierDto[]>([])

const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

const detailDialog = ref(false)
const selectedReceipt = ref<GoodsReceiptDto | null>(null)

const createDialog = ref(false)
const selectedSupplierId = ref<string | null>(null)
const receiptNote = ref('')
const receiptItems = ref<Array<{
  product: ProductDto
  quantity: number
  importPrice: number
}>>([])

const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])
const searchLoading = ref(false)

const csvFileInput = ref<HTMLInputElement | null>(null)

const moneyFormatter = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
})

const formatCurrency = (value: number) => moneyFormatter.format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatDate = (value: string) => {
  const date = new Date(value)

  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date)
}

const statusLabel = (status: string) => {
  const map: Record<string, string> = {
    Draft: 'Bản nháp',
    Confirmed: 'Đã xác nhận',
    Cancelled: 'Đã hủy',
  }

  return map[status] ?? status
}

const statusColor = (status: string) => {
  const map: Record<string, string> = {
    Draft: 'warning',
    Confirmed: 'success',
    Cancelled: 'error',
  }

  return map[status] ?? 'secondary'
}

const selectedSupplierName = computed(() =>
  suppliers.value.find(supplier => supplier.id === selectedSupplierId.value)?.name || 'Chưa chọn nhà cung cấp',
)

const receiptDraftTotal = computed(() =>
  receiptItems.value.reduce((sum, item) => sum + ((Number(item.quantity) || 0) * (Number(item.importPrice) || 0)), 0),
)

const receiptDraftQuantity = computed(() =>
  receiptItems.value.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0),
)

const draftReceiptsCount = computed(() =>
  receipts.value.filter(receipt => receipt.status === 'Draft').length,
)

const confirmedReceiptsCount = computed(() =>
  receipts.value.filter(receipt => receipt.status === 'Confirmed').length,
)

const cancelledReceiptsCount = computed(() =>
  receipts.value.filter(receipt => receipt.status === 'Cancelled').length,
)

const totalReceiptAmount = computed(() =>
  receipts.value.reduce((sum, receipt) => sum + (receipt.totalAmount || 0), 0),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng phiếu nhập',
    value: formatNumber(totalCount.value),
    helper: `${formatNumber(receipts.value.length)} phiếu trên trang hiện tại`,
    icon: 'ri-file-list-3-line',
    color: 'primary',
  },
  {
    label: 'Giá trị nhập trang này',
    value: formatCurrency(totalReceiptAmount.value),
    helper: 'Tổng giá trị các phiếu đang hiển thị',
    icon: 'ri-money-dollar-circle-line',
    color: 'success',
  },
  {
    label: 'Chờ xác nhận',
    value: formatNumber(draftReceiptsCount.value),
    helper: 'Phiếu nháp chưa cập nhật tồn kho',
    icon: 'ri-time-line',
    color: 'warning',
  },
  {
    label: 'Đã xác nhận',
    value: formatNumber(confirmedReceiptsCount.value),
    helper: `${formatNumber(cancelledReceiptsCount.value)} phiếu đã hủy`,
    icon: 'ri-checkbox-circle-line',
    color: 'info',
  },
])

const rangeStart = computed(() => {
  if (!receipts.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalCount.value),
)

const loadReceipts = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getGoodsReceipts({
      page: page.value,
      pageSize: pageSize.value,
    })

    receipts.value = result.items
    totalPages.value = Math.max(1, result.totalPages)
    totalCount.value = result.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách phiếu nhập.'
  }
  finally {
    loading.value = false
  }
}

const loadSuppliersList = async () => {
  try {
    const result = await getSuppliers({ pageSize: 100 })

    suppliers.value = result.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách nhà cung cấp.'
  }
}

const refreshReceipts = () => {
  if (page.value === 1)
    void loadReceipts()
  else
    page.value = 1
}

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []

    return
  }

  searchLoading.value = true
  errorMessage.value = ''

  try {
    const result = await getProducts({
      search: searchProductQuery.value.trim(),
      pageSize: 10,
    })

    searchedProducts.value = result.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tìm sản phẩm.'
  }
  finally {
    searchLoading.value = false
  }
}

const addProductToReceipt = (product: ProductDto) => {
  const existing = receiptItems.value.find(item => item.product.id === product.id)

  if (existing) {
    existing.quantity += 1
  }
  else {
    receiptItems.value.push({
      product,
      quantity: 1,
      importPrice: product.importPrice || 0,
    })
  }

  searchProductQuery.value = ''
  searchedProducts.value = []
}

const removeProductFromReceipt = (productId: string) => {
  receiptItems.value = receiptItems.value.filter(item => item.product.id !== productId)
}

const openCreateReceipt = () => {
  receiptItems.value = []
  receiptNote.value = ''
  searchProductQuery.value = ''
  searchedProducts.value = []
  selectedSupplierId.value = suppliers.value[0]?.id || null
  createDialog.value = true
}

const validateReceipt = () => {
  if (!selectedSupplierId.value)
    return 'Vui lòng chọn nhà cung cấp.'

  if (receiptItems.value.length === 0)
    return 'Vui lòng thêm sản phẩm vào phiếu nhập.'

  for (const item of receiptItems.value) {
    if (!item.quantity || item.quantity <= 0)
      return `Số lượng nhập của ${item.product.name} phải lớn hơn 0.`

    if (item.importPrice < 0)
      return `Giá nhập của ${item.product.name} không được âm.`
  }

  return ''
}

const handleSaveReceipt = async () => {
  const validationMessage = validateReceipt()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createGoodsReceipt({
      supplierId: selectedSupplierId.value!,
      note: receiptNote.value || null,
      details: receiptItems.value.map(item => ({
        productId: item.product.id,
        quantity: Number(item.quantity) || 0,
        unitPrice: Number(item.importPrice) || 0,
      })),
    })

    createDialog.value = false
    successMessage.value = 'Tạo phiếu nhập bản nháp thành công.'

    await loadReceipts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi tạo phiếu nhập.'
  }
  finally {
    loading.value = false
  }
}

const openReceiptDetails = (receipt: GoodsReceiptDto) => {
  selectedReceipt.value = receipt
  detailDialog.value = true
}

const handleConfirmReceipt = async () => {
  if (!selectedReceipt.value)
    return

  if (!confirm('Bạn có chắc chắn muốn xác nhận phiếu nhập này không? Thao tác này sẽ tăng tồn kho và cập nhật giá nhập.'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await confirmGoodsReceipt(selectedReceipt.value.id)

    detailDialog.value = false
    successMessage.value = 'Xác nhận nhập kho thành công. Tồn kho đã được cập nhật.'

    await loadReceipts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi xác nhận phiếu nhập.'
  }
  finally {
    loading.value = false
  }
}

const handleCancelReceipt = async () => {
  if (!selectedReceipt.value)
    return

  if (!confirm('Bạn có chắc chắn muốn hủy phiếu nhập này không?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await cancelGoodsReceipt(selectedReceipt.value.id)

    detailDialog.value = false
    successMessage.value = 'Đã hủy phiếu nhập.'

    await loadReceipts()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi hủy phiếu nhập.'
  }
  finally {
    loading.value = false
  }
}

const receiptActions = (receipt: GoodsReceiptDto): ActionMenuItem[] => [
  {
    label: 'Xem chi tiết',
    icon: 'ri-eye-line',
    handler: () => openReceiptDetails(receipt),
  },
  {
    label: 'Xác nhận',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
    handler: () => {
      selectedReceipt.value = receipt
      void handleConfirmReceipt()
    },
    show: receipt.status === 'Draft',
  },
  {
    label: 'Hủy phiếu',
    icon: 'ri-close-circle-line',
    color: 'error',
    handler: () => {
      selectedReceipt.value = receipt
      void handleCancelReceipt()
    },
    show: receipt.status === 'Draft',
  },
]

const downloadTemplate = () => {
  const csvContent = 'ProductCode,Quantity,ImportPrice\nSP000001,10,15000\nSP000002,5,25000'
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.setAttribute('download', 'mau_nhap_hang.csv')

  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)

  URL.revokeObjectURL(url)
}

const triggerCsvImport = () => {
  csvFileInput.value?.click()
}

const handleCsvUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]

  if (!file)
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const importedItems = await importGoodsReceiptItems(file)

    for (const item of importedItems) {
      const existing = receiptItems.value.find(row => row.product.id === item.productId)

      if (existing) {
        existing.quantity += item.quantity
        existing.importPrice = item.importPrice
      }
      else {
        receiptItems.value.push({
          product: {
            id: item.productId,
            code: item.productCode,
            name: item.productName,
            unitName: item.unitName,
            importPrice: item.importPrice,
            description: null,
            barcode: null,
            sellPrice: 0,
            imageUrl: null,
            weight: null,
            categoryId: '',
            categoryName: '',
            unitId: '',
            quantityOnHand: 0,
            quantityReserved: 0,
            isActive: true,
            createdAt: '',
          } as ProductDto,
          quantity: item.quantity,
          importPrice: item.importPrice,
        })
      }
    }

    successMessage.value = `Nhập thành công ${formatNumber(importedItems.length)} mặt hàng từ file CSV.`
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi nhập file CSV.'
  }
  finally {
    loading.value = false
    target.value = ''
  }
}

watch(page, () => {
  void loadReceipts()
})

watch(pageSize, () => {
  page.value = 1
  void loadReceipts()
})

onMounted(async () => {
  await loadSuppliersList()
  await loadReceipts()
})
</script>

<template>
  <section class="goods-receipt-page">
    <div class="receipt-hero">
      <div class="receipt-hero__title-area">
        <h1>Nhập hàng</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-inbox-archive-line" class="ml-2">
          Mua hàng
        </VChip>
      </div>

      <div class="receipt-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="refreshReceipts">
          Tải lại
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-add-line" class="primary-action" @click="openCreateReceipt">
          Tạo phiếu nhập
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

    <div class="receipt-layout">
      <VCard class="receipt-panel">
        <div class="panel-head">
          <div>
            <span>Danh sách phiếu nhập</span>
            <strong>Phiếu nhập hàng</strong>
          </div>

          <VChip color="primary" variant="tonal">
            {{ formatNumber(totalCount) }} phiếu
          </VChip>
        </div>

        <VDivider />

        <VCardText v-if="loading">
          <VSkeletonLoader type="table-heading, table-tbody" />
        </VCardText>

        <template v-else>
          <div v-if="receipts.length" class="receipt-table-wrap">
            <VTable class="receipt-table">
              <thead>
                <tr>
                  <th>Mã phiếu</th>
                  <th>Nhà cung cấp</th>
                  <th>Người tạo</th>
                  <th>Ngày tạo</th>
                  <th class="text-end">Mặt hàng</th>
                  <th class="text-end">Tổng tiền</th>
                  <th class="text-center">Trạng thái</th>
                  <th class="text-center">Thao tác</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="receipt in receipts" :key="receipt.id" class="receipt-row"
                  @click="openReceiptDetails(receipt)">
                  <td>
                    <div class="receipt-code">
                      {{ receipt.receiptCode }}
                    </div>
                  </td>

                  <td>{{ receipt.supplierName }}</td>
                  <td>{{ receipt.createdByName }}</td>
                  <td>{{ formatDate(receipt.createdAt) }}</td>

                  <td class="text-end font-weight-bold">
                    {{ formatNumber(receipt.details?.length || 0) }}
                  </td>

                  <td class="text-end total-cell">
                    {{ formatCurrency(receipt.totalAmount) }}
                  </td>

                  <td class="text-center">
                    <VChip :color="statusColor(receipt.status)" variant="tonal" size="small">
                      {{ statusLabel(receipt.status) }}
                    </VChip>
                  </td>

                  <td class="text-center" @click.stop>
                    <RetailActionMenu :items="receiptActions(receipt)" />
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>

          <div v-else class="receipt-empty">
            <VIcon icon="ri-file-list-3-line" size="42" color="primary" />
            <strong>Chưa có phiếu nhập hàng nào</strong>
            <span>Hãy tạo phiếu nhập kho mới để cập nhật tồn kho.</span>

            <VBtn color="primary" prepend-icon="ri-add-line" @click="openCreateReceipt">
              Tạo phiếu nhập
            </VBtn>
          </div>
        </template>

        <div v-if="receipts.length || totalCount > 0" class="receipt-pagination">
          <span>
            Hiển thị {{ formatNumber(rangeStart) }}–{{ formatNumber(rangeEnd) }}
            trên tổng số {{ formatNumber(totalCount) }} phiếu nhập
          </span>

          <div>
            <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
              variant="outlined" class="page-size-select" />

            <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
          </div>
        </div>
      </VCard>

      <aside class="receipt-aside">
        <VCard class="receipt-panel guide-card">
          <div class="aside-head">
            <span>Quy trình</span>
            <strong>Hướng dẫn nhập hàng</strong>
          </div>

          <div class="guide-list">
            <div>
              <VIcon icon="ri-add-circle-line" color="primary" />
              <span>Tạo phiếu nhập và chọn nhà cung cấp.</span>
            </div>

            <div>
              <VIcon icon="ri-file-list-3-line" color="info" />
              <span>Thêm sản phẩm thủ công hoặc nhập nhanh bằng file CSV.</span>
            </div>

            <div>
              <VIcon icon="ri-draft-line" color="warning" />
              <span>Lưu bản nháp để kiểm tra lại trước khi cập nhật kho.</span>
            </div>

            <div>
              <VIcon icon="ri-checkbox-circle-line" color="success" />
              <span>Xác nhận phiếu nháp để tăng tồn kho và cập nhật giá nhập.</span>
            </div>
          </div>
        </VCard>
      </aside>
    </div>

    <VDialog v-model="createDialog" max-width="980" persistent scrollable>
      <VCard class="receipt-dialog">
        <div class="dialog-head">
          <div>
            <span>Lập phiếu nhập</span>
            <h2>Phiếu nhập kho mới</h2>
          </div>

          <VChip color="success" variant="tonal">
            {{ formatCurrency(receiptDraftTotal) }}
          </VChip>
        </div>

        <VCardText>
          <div class="receipt-form-grid">
            <VSelect v-model="selectedSupplierId" label="Nhà cung cấp *" :items="suppliers" item-title="name"
              item-value="id" density="comfortable" />

            <VTextField v-model="receiptNote" label="Ghi chú phiếu nhập" density="comfortable" />
          </div>

          <div class="draft-summary-strip">
            <div>
              <span>Nhà cung cấp</span>
              <strong>{{ selectedSupplierName }}</strong>
            </div>

            <div>
              <span>Số SKU</span>
              <strong>{{ formatNumber(receiptItems.length) }}</strong>
            </div>

            <div>
              <span>Tổng số lượng</span>
              <strong>{{ formatNumber(receiptDraftQuantity) }}</strong>
            </div>

            <div>
              <span>Tổng tiền</span>
              <strong>{{ formatCurrency(receiptDraftTotal) }}</strong>
            </div>
          </div>

          <VDivider class="my-4" />

          <div class="product-search-box">
            <VTextField v-model="searchProductQuery" label="Tìm sản phẩm để nhập hàng"
              placeholder="Nhập tên sản phẩm, mã hàng hoặc barcode..." prepend-inner-icon="ri-search-line" hide-details
              density="comfortable" @keyup.enter="searchProducts" />

            <VBtn color="primary" prepend-icon="ri-search-line" :loading="searchLoading" @click="searchProducts">
              Tìm
            </VBtn>

            <VBtn color="success" variant="tonal" prepend-icon="ri-file-excel-line" :loading="loading"
              @click="triggerCsvImport">
              Nhập CSV
            </VBtn>

            <VBtn variant="outlined" prepend-icon="ri-download-line" @click="downloadTemplate">
              Mẫu
            </VBtn>

            <input ref="csvFileInput" type="file" accept=".csv" class="d-none" @change="handleCsvUpload">
          </div>

          <div v-if="searchedProducts.length" class="product-result-list">
            <button v-for="product in searchedProducts" :key="product.id" type="button" class="product-result-item"
              @click="addProductToReceipt(product)">
              <span>
                <strong>{{ product.name }}</strong>
                <small>
                  {{ product.code }} · Tồn hiện tại:
                  {{ formatNumber(product.quantityOnHand ?? 0) }} {{ product.unitName }}
                  · Giá nhập: {{ formatCurrency(product.importPrice) }}
                </small>
              </span>

              <VIcon icon="ri-add-line" />
            </button>
          </div>

          <div class="receipt-items-wrap">
            <VTable class="receipt-table compact-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th>ĐVT</th>
                  <th class="text-end">Số lượng nhập</th>
                  <th class="text-end">Giá nhập</th>
                  <th class="text-end">Thành tiền</th>
                  <th class="text-center">Xóa</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in receiptItems" :key="item.product.id">
                  <td class="font-weight-bold">
                    {{ item.product.name }}
                  </td>

                  <td>
                    <span class="receipt-code">{{ item.product.code }}</span>
                  </td>

                  <td>{{ item.product.unitName }}</td>

                  <td>
                    <VTextField v-model.number="item.quantity" type="number" density="compact" hide-details min="1"
                      class="number-input" />
                  </td>

                  <td>
                    <VTextField v-model.number="item.importPrice" type="number" density="compact" hide-details min="0"
                      suffix="đ" class="price-input" />
                  </td>

                  <td class="text-end total-cell">
                    {{ formatCurrency(item.quantity * item.importPrice) }}
                  </td>

                  <td class="text-center">
                    <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                      @click="removeProductFromReceipt(item.product.id)" />
                  </td>
                </tr>

                <tr v-if="!receiptItems.length">
                  <td colspan="7" class="text-center text-medium-emphasis py-6">
                    Chưa có sản phẩm nào được chọn để nhập. Vui lòng tìm kiếm hoặc nhập CSV.
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="createDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleSaveReceipt">
            Lưu bản nháp
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="detailDialog" max-width="920" scrollable>
      <VCard v-if="selectedReceipt" class="receipt-dialog">
        <div class="dialog-head">
          <div>
            <span>Chi tiết phiếu nhập</span>
            <h2>{{ selectedReceipt.receiptCode }}</h2>
          </div>

          <VChip :color="statusColor(selectedReceipt.status)" variant="tonal">
            {{ statusLabel(selectedReceipt.status) }}
          </VChip>
        </div>

        <VCardText>
          <div class="detail-grid">
            <div class="detail-card">
              <span>Nhà cung cấp</span>
              <strong>{{ selectedReceipt.supplierName }}</strong>
              <p>{{ formatDate(selectedReceipt.createdAt) }}</p>
            </div>

            <div class="detail-card">
              <span>Người tạo</span>
              <strong>{{ selectedReceipt.createdByName }}</strong>
              <p>{{ selectedReceipt.note || 'Không có ghi chú' }}</p>
            </div>

            <div class="detail-card">
              <span>Tổng tiền nhập</span>
              <strong class="text-success">
                {{ formatCurrency(selectedReceipt.totalAmount) }}
              </strong>
              <p>{{ formatNumber(selectedReceipt.details?.length || 0) }} mặt hàng</p>
            </div>
          </div>

          <div class="receipt-items-wrap">
            <VTable class="receipt-table compact-table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Mã hàng</th>
                  <th>ĐVT</th>
                  <th class="text-end">Đơn giá nhập</th>
                  <th class="text-center">Số lượng</th>
                  <th class="text-end">Thành tiền</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="item in selectedReceipt.details" :key="item.id">
                  <td class="font-weight-bold">
                    {{ item.productName }}
                  </td>

                  <td>{{ item.productCode }}</td>
                  <td>{{ item.unitName }}</td>

                  <td class="text-end">
                    {{ formatCurrency(item.unitPrice) }}
                  </td>

                  <td class="text-center font-weight-bold">
                    {{ formatNumber(item.quantity) }}
                  </td>

                  <td class="text-end total-cell">
                    {{ formatCurrency(item.totalPrice) }}
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VBtn v-if="selectedReceipt.status === 'Draft'" color="success" variant="tonal"
            prepend-icon="ri-checkbox-circle-line" :loading="loading" @click="handleConfirmReceipt">
            Xác nhận nhập kho
          </VBtn>

          <VBtn v-if="selectedReceipt.status === 'Draft'" color="error" variant="tonal"
            prepend-icon="ri-close-circle-line" :loading="loading" @click="handleCancelReceipt">
            Hủy phiếu nhập
          </VBtn>

          <VSpacer />

          <VBtn color="secondary" variant="outlined" @click="detailDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.goods-receipt-page {
  position: relative;
  isolation: isolate;
}

.goods-receipt-page::before {
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

.summary-icon--info {
  background: rgb(var(--v-theme-info));
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

.receipt-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
  grid-template-columns: minmax(0, 1fr) 340px;
}

.receipt-panel,
.receipt-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.panel-head,
.aside-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.1rem 1.25rem;
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
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.2rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.receipt-table-wrap,
.receipt-items-wrap {
  overflow-x: auto;
}

.receipt-table {
  min-inline-size: 980px;
}

.receipt-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.receipt-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.receipt-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.receipt-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.receipt-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.total-cell {
  color: rgb(var(--v-theme-success));
  font-weight: 950;
}

.receipt-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.receipt-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.receipt-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.receipt-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.receipt-pagination>div {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.page-size-select {
  inline-size: 108px;
}

.receipt-aside {
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

.receipt-form-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: minmax(220px, 320px) minmax(0, 1fr);
}

.receipt-dialog :deep(.v-field) {
  border-radius: 16px;
}

.draft-summary-strip {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-start: 1rem;
}

.draft-summary-strip>div {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.9rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.draft-summary-strip span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.draft-summary-strip strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.95rem;
  font-weight: 900;
}

.product-search-box {
  display: grid;
  align-items: center;
  gap: 0.75rem;
  grid-template-columns: minmax(0, 1fr) auto auto auto;
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

.compact-table {
  min-inline-size: 880px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 56px !important;
}

.number-input,
.price-input {
  max-inline-size: 160px;
  margin-inline-start: auto;
}

.number-input :deep(input),
.price-input :deep(input) {
  text-align: end;
}

.detail-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  margin-block-end: 1rem;
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

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .receipt-layout {
    grid-template-columns: 1fr;
  }

  .guide-card {
    position: static;
  }

  .summary-grid,
  .draft-summary-strip {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .product-search-box {
    grid-template-columns: 1fr 1fr;
  }

  .product-search-box .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .receipt-form-grid,
  .draft-summary-strip,
  .product-search-box,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .receipt-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .receipt-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
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
.receipt-hero {
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

.receipt-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.receipt-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.receipt-hero__copy {
  display: none !important;
}

.receipt-hero__actions,
.receipt-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.receipt-hero__actions .v-btn,
.receipt-actions .v-btn,
.receipt-hero__actions .v-btn.primary-action,
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