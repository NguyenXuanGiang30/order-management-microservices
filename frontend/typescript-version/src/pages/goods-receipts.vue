<script setup lang="ts">
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

// Selected receipt for detail dialog
const detailDialog = ref(false)
const selectedReceipt = ref<GoodsReceiptDto | null>(null)

// Goods receipt creation state
const createDialog = ref(false)
const selectedSupplierId = ref<string | null>(null)
const receiptNote = ref('')
const receiptItems = ref<Array<{ product: ProductDto; quantity: number; importPrice: number }>>([])

// Right sidebar search & quick add state
const searchProductQuery = ref('')
const searchedProducts = ref<ProductDto[]>([])
const searchLoading = ref(false)

const csvFileInput = ref<HTMLInputElement | null>(null)

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
  try {
    const importedItems = await importGoodsReceiptItems(file)
    for (const item of importedItems) {
      const existing = receiptItems.value.find(x => x.product.id === item.productId)
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
          },
          quantity: item.quantity,
          importPrice: item.importPrice,
        })
      }
    }
    alert(`Nhập thành công ${importedItems.length} mặt hàng từ file CSV.`)
  }
  catch (error: any) {
    alert(error.message || 'Lỗi khi nhập file CSV.')
  }
  finally {
    loading.value = false
    target.value = ''
  }
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

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

const loadReceipts = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await getGoodsReceipts({ pageSize: 100 })

    receipts.value = result.items
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
    const res = await getSuppliers({ pageSize: 100 })

    suppliers.value = res.items
  }
  catch (error) {
    console.error('Không thể tải nhà cung cấp:', error)
  }
}

const searchProducts = async () => {
  if (!searchProductQuery.value.trim()) {
    searchedProducts.value = []

    return
  }
  searchLoading.value = true
  try {
    const res = await getProducts({ search: searchProductQuery.value, pageSize: 10 })

    searchedProducts.value = res.items
  }
  catch (error) {
    console.error('Lỗi tìm sản phẩm:', error)
  }
  finally {
    searchLoading.value = false
  }
}

const addProductToReceipt = (product: ProductDto) => {
  const existing = receiptItems.value.find(item => item.product.id === product.id)
  if (existing) {
    existing.quantity++
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
  selectedSupplierId.value = suppliers.value[0]?.id || null
  createDialog.value = true
}

const handleSaveReceipt = async () => {
  if (!selectedSupplierId.value) {
    alert('Vui lòng chọn nhà cung cấp.')

    return
  }
  if (receiptItems.value.length === 0) {
    alert('Vui lòng thêm sản phẩm vào phiếu nhập.')

    return
  }

  loading.value = true
  try {
    const payload = {
      supplierId: selectedSupplierId.value,
      note: receiptNote.value || null,
      details: receiptItems.value.map(item => ({
        productId: item.product.id,
        quantity: item.quantity,
        unitPrice: item.importPrice,
      })),
    }

    await createGoodsReceipt(payload)
    createDialog.value = false
    await loadReceipts()
  }
  catch (error: any) {
    alert(`Lỗi tạo phiếu nhập: ${error.message}`)
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
  try {
    await confirmGoodsReceipt(selectedReceipt.value.id)
    detailDialog.value = false
    await loadReceipts()
  }
  catch (error: any) {
    alert(`Lỗi xác nhận phiếu nhập: ${error.message}`)
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
  try {
    await cancelGoodsReceipt(selectedReceipt.value.id)
    detailDialog.value = false
    await loadReceipts()
  }
  catch (error: any) {
    alert(`Lỗi hủy phiếu nhập: ${error.message}`)
  }
  finally {
    loading.value = false
  }
}

const totalReceipts = computed(() => receipts.value.length)
const totalReceiptAmount = computed(() => receipts.value.reduce((sum, r) => sum + r.totalAmount, 0))
const draftReceiptsCount = computed(() => receipts.value.filter(r => r.status === 'Draft').length)

const receiptActions = (receipt: GoodsReceiptDto): ActionMenuItem[] => [
  { label: 'Xem chi tiết', icon: 'ri-eye-line', handler: () => openReceiptDetails(receipt) },
  {
    label: 'Xác nhận',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
    handler: () => {
      selectedReceipt.value = receipt
      handleConfirmReceipt()
    },
    show: receipt.status === 'Draft',
  },
  {
    label: 'Hủy phiếu',
    icon: 'ri-close-circle-line',
    color: 'error',
    handler: () => {
      selectedReceipt.value = receipt
      handleCancelReceipt()
    },
    show: receipt.status === 'Draft',
  },
]

onMounted(async () => {
  await loadSuppliersList()
  await loadReceipts()
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Mua hàng"
    title="Nhập hàng"
    subtitle="Tạo phiếu nhập, quét SKU/barcode, theo dõi giá nhập và xác nhận cập nhật tồn kho."
  >
    <template #actions>
      <div class="d-flex gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="loadReceipts"
        >
          Tải lại
        </VBtn>
        <VBtn
          prepend-icon="ri-add-line"
          @click="openCreateReceipt"
        >
          Tạo phiếu nhập
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
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Tổng phiếu nhập', value: String(totalReceipts), helper: 'Đã lập trong hệ thống', icon: 'ri-file-list-3-line', color: 'primary' }" />
    </VCol>
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Tổng giá trị nhập', value: formatCurrency(totalReceiptAmount), helper: 'Giá trị hàng hóa đã nhập', icon: 'ri-money-dollar-circle-line', color: 'success' }" />
    </VCol>
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Phiếu chờ xác nhận', value: String(draftReceiptsCount), helper: 'Đang ở trạng thái nháp', icon: 'ri-time-line', color: 'warning' }" />
    </VCol>
  </VRow>

  <VRow>
    <VCol
      cols="12"
      lg="8"
    >
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
              <th>Mã phiếu</th>
              <th>Nhà cung cấp</th>
              <th>Người tạo</th>
              <th>Ngày tạo</th>
              <th class="text-end">
                Mặt hàng
              </th>
              <th class="text-end">
                Tổng tiền
              </th>
              <th>Trạng thái</th>
              <th
                class="text-center"
                style="width: 60px;"
              >
                Thao tác
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="receipt in receipts"
              :key="receipt.id"
              class="hover-row"
              @click="openReceiptDetails(receipt)"
            >
              <td class="font-weight-bold text-primary">
                {{ receipt.receiptCode }}
              </td>
              <td>{{ receipt.supplierName }}</td>
              <td>{{ receipt.createdByName }}</td>
              <td>{{ formatDate(receipt.createdAt) }}</td>
              <td class="text-end font-weight-bold">
                {{ receipt.details?.length || 0 }}
              </td>
              <td class="text-end font-weight-bold text-success">
                {{ formatCurrency(receipt.totalAmount) }}
              </td>
              <td>
                <RetailStatusBadge
                  :status="statusLabel(receipt.status)"
                  dot
                />
              </td>
              <td
                class="text-center"
                @click.stop
              >
                <RetailActionMenu :items="receiptActions(receipt)" />
              </td>
            </tr>
            <RetailEmptyState
              v-if="!loading && !receipts.length"
              :colspan="8"
              icon="ri-file-list-3-line"
              title="Chưa có phiếu nhập hàng nào"
              subtitle="Hãy tạo phiếu nhập kho mới để cập nhật tồn kho."
              action-label="Tạo phiếu nhập"
            />
          </tbody>
        </VTable>
      </VCard>
    </VCol>

    <!-- Right Sidebar Quick Search and Info -->
    <VCol
      cols="12"
      lg="4"
    >
      <VCard class="mb-6">
        <VCardItem>
          <VCardTitle>Hướng dẫn quy trình nhập</VCardTitle>
        </VCardItem>
        <VCardText class="d-flex flex-column gap-3">
          <div class="d-flex align-start gap-2">
            <VIcon
              color="primary"
              icon="ri-checkbox-circle-line"
              class="mt-1"
            />
            <div>
              <strong>Bấm tạo phiếu nhập:</strong> Điền thông tin nhà cung cấp và danh sách hàng hóa.
            </div>
          </div>
          <div class="d-flex align-start gap-2">
            <VIcon
              color="primary"
              icon="ri-checkbox-circle-line"
              class="mt-1"
            />
            <div>
              <strong>Lưu bản nháp:</strong> Phiếu nhập ban đầu sẽ ở trạng thái Bản nháp (Draft), chưa cập nhật kho.
            </div>
          </div>
          <div class="d-flex align-start gap-2">
            <VIcon
              color="primary"
              icon="ri-checkbox-circle-line"
              class="mt-1"
            />
            <div>
              <strong>Xác nhận:</strong> Bấm chi tiết phiếu nháp, chọn <strong>Xác nhận</strong> để tăng số lượng tồn kho của sản phẩm tương ứng và cập nhật giá nhập mới.
            </div>
          </div>
        </VCardText>
      </VCard>
    </VCol>
  </VRow>

  <!-- Create Goods Receipt Dialog -->
  <VDialog
    v-model="createDialog"
    max-width="900"
    persistent
  >
    <VCard>
      <VCardTitle>Lập phiếu nhập kho mới</VCardTitle>
      <VCardText>
        <VRow>
          <VCol
            cols="12"
            sm="6"
          >
            <VSelect
              v-model="selectedSupplierId"
              label="Chọn nhà cung cấp *"
              :items="suppliers"
              item-title="name"
              item-value="id"
            />
          </VCol>
          <VCol
            cols="12"
            sm="6"
          >
            <VTextField
              v-model="receiptNote"
              label="Ghi chú phiếu nhập"
            />
          </VCol>
        </VRow>

        <hr class="my-4">

        <div class="d-flex gap-3 align-center mb-4">
          <VTextField
            v-model="searchProductQuery"
            label="Tìm kiếm sản phẩm để nhập hàng..."
            placeholder="Nhập tên sản phẩm, mã hàng, hoặc barcode..."
            prepend-inner-icon="ri-search-line"
            hide-details
            @keyup.enter="searchProducts"
          />
          <VBtn
            :loading="searchLoading"
            color="primary"
            @click="searchProducts"
          >
            Tìm
          </VBtn>
          <VBtn
            variant="tonal"
            prepend-icon="ri-file-excel-line"
            color="success"
            @click="triggerCsvImport"
          >
            Nhập CSV
          </VBtn>
          <VBtn
            variant="text"
            density="comfortable"
            prepend-icon="ri-download-line"
            @click="downloadTemplate"
          >
            Mẫu
          </VBtn>
          <input
            ref="csvFileInput"
            type="file"
            accept=".csv"
            style="display: none"
            @change="handleCsvUpload"
          >
        </div>

        <!-- Seached Products Dropdown Mock -->
        <VList
          v-if="searchedProducts.length"
          class="border rounded mb-4"
          style="max-height: 200px; overflow-y: auto;"
        >
          <VListItem
            v-for="prod in searchedProducts"
            :key="prod.id"
            class="cursor-pointer"
            @click="addProductToReceipt(prod)"
          >
            <VListItemTitle class="font-weight-bold">
              {{ prod.name }} ({{ prod.code }})
            </VListItemTitle>
            <VListItemSubtitle>Tồn kho hiện tại: {{ prod.quantityOnHand }} {{ prod.unitName }} | Giá nhập hiện tại: {{ formatCurrency(prod.importPrice) }}</VListItemSubtitle>
          </VListItem>
        </VList>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Mã hàng</th>
              <th>ĐVT</th>
              <th
                class="text-right"
                style="width: 150px;"
              >
                Số lượng nhập
              </th>
              <th
                class="text-right"
                style="width: 200px;"
              >
                Giá nhập (đ)
              </th>
              <th class="text-right">
                Thành tiền
              </th>
              <th class="text-center">
                Thao tác
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in receiptItems"
              :key="item.product.id"
            >
              <td class="font-weight-bold">
                {{ item.product.name }}
              </td>
              <td>{{ item.product.code }}</td>
              <td>{{ item.product.unitName }}</td>
              <td>
                <VTextField
                  v-model.number="item.quantity"
                  type="number"
                  density="compact"
                  hide-details
                  min="1"
                />
              </td>
              <td>
                <VTextField
                  v-model.number="item.importPrice"
                  type="number"
                  density="compact"
                  hide-details
                  min="0"
                />
              </td>
              <td class="text-right font-weight-bold">
                {{ formatCurrency(item.quantity * item.importPrice) }}
              </td>
              <td class="text-center">
                <VBtn
                  icon="ri-delete-bin-line"
                  variant="text"
                  size="small"
                  color="error"
                  @click="removeProductFromReceipt(item.product.id)"
                />
              </td>
            </tr>
            <tr v-if="!receiptItems.length">
              <td
                colspan="7"
                class="text-center text-medium-emphasis py-6"
              >
                Chưa có sản phẩm nào được chọn để nhập. Vui lòng tìm kiếm phía trên.
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
          @click="createDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSaveReceipt"
        >
          Lưu bản nháp
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Goods Receipt Detail Dialog -->
  <VDialog
    v-model="detailDialog"
    max-width="850"
  >
    <VCard v-if="selectedReceipt">
      <VCardTitle class="d-flex justify-between align-center">
        <span>Phiếu nhập hàng: {{ selectedReceipt.receiptCode }}</span>
        <RetailStatusBadge
          :status="statusLabel(selectedReceipt.status)"
          dot
        />
      </VCardTitle>
      <VCardText>
        <VRow class="mb-4">
          <VCol
            cols="12"
            sm="6"
          >
            <div><strong>Nhà cung cấp:</strong> {{ selectedReceipt.supplierName }}</div>
            <div><strong>Ngày tạo:</strong> {{ formatDate(selectedReceipt.createdAt) }}</div>
          </VCol>
          <VCol
            cols="12"
            sm="6"
          >
            <div><strong>Người tạo:</strong> {{ selectedReceipt.createdByName }}</div>
            <div><strong>Ghi chú:</strong> {{ selectedReceipt.note || '—' }}</div>
          </VCol>
        </VRow>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Mã hàng</th>
              <th>ĐVT</th>
              <th class="text-right">
                Đơn giá nhập
              </th>
              <th class="text-center">
                Số lượng
              </th>
              <th class="text-right">
                Thành tiền
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in selectedReceipt.details"
              :key="item.id"
            >
              <td class="font-weight-bold">
                {{ item.productName }}
              </td>
              <td>{{ item.productCode }}</td>
              <td>{{ item.unitName }}</td>
              <td class="text-right">
                {{ formatCurrency(item.unitPrice) }}
              </td>
              <td class="text-center">
                {{ item.quantity }}
              </td>
              <td class="text-right font-weight-bold">
                {{ formatCurrency(item.totalPrice) }}
              </td>
            </tr>
          </tbody>
        </VTable>

        <div class="text-right text-h6 font-weight-bold">
          Tổng tiền nhập hàng: <span class="text-success">{{ formatCurrency(selectedReceipt.totalAmount) }}</span>
        </div>
      </VCardText>
      <VCardActions>
        <VBtn
          v-if="selectedReceipt.status === 'Draft'"
          color="success"
          variant="tonal"
          prepend-icon="ri-checkbox-circle-line"
          @click="handleConfirmReceipt"
        >
          Xác nhận nhập kho
        </VBtn>
        <VBtn
          v-if="selectedReceipt.status === 'Draft'"
          color="error"
          variant="tonal"
          prepend-icon="ri-close-circle-line"
          @click="handleCancelReceipt"
        >
          Hủy phiếu nhập
        </VBtn>
        <VSpacer />
        <VBtn
          color="secondary"
          @click="detailDialog = false"
        >
          Đóng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
