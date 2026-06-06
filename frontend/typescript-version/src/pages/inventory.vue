<script setup lang="ts">
import {
  type StockDto,
  getInventoryStock,
  getStocktakeSessions,
  getStocktakeSession,
  createStocktakeSession,
  confirmStocktake,
  cancelStocktake,
  getProducts,
  type ProductDto,
  type StocktakeSessionDto,
} from '@/services/productInventoryApi'

const activeTab = ref(0)
const stockItems = ref<StockDto[]>([])
const sessions = ref<StocktakeSessionDto[]>([])
const search = ref('')
const loading = ref(false)
const errorMessage = ref('')

// Dialogs
const createSessionDialog = ref(false)
const detailSessionDialog = ref(false)

// Create session states
const sessionNote = ref('')
const sessionLines = ref<Array<{ product: ProductDto; actualQty: number; note: string }>>([])
const productSearchQuery = ref('')
const searchProductsResult = ref<ProductDto[]>([])
const searchLoading = ref(false)

// Session details
const selectedSession = ref<StocktakeSessionDto | null>(null)
const loadingSessionDetail = ref(false)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value)

const formatDate = (dateStr: string) => {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleDateString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const totalStock = computed(() => stockItems.value.reduce((sum, product) => sum + product.quantityOnHand, 0))
const reservedStock = computed(() => stockItems.value.reduce((sum, product) => sum + product.quantityReserved, 0))
const lowStock = computed(() => stockItems.value.filter(product => product.isBelowMin).length)
const availableStock = computed(() => stockItems.value.reduce((sum, product) => sum + product.availableQuantity, 0))

const stockStatus = (product: StockDto) => {
  if (product.quantityOnHand <= 0)
    return 'Hết hàng'
  if (product.isBelowMin)
    return 'Thấp'
  if (product.alertLevel === 'High')
    return 'Tốt'
  return 'Bình thường'
}

const loadStock = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    stockItems.value = await getInventoryStock({
      search: search.value.trim(),
    })
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu tồn kho.'
  }
  finally {
    loading.value = false
  }
}

const loadSessions = async () => {
  loading.value = true
  try {
    const res = await getStocktakeSessions()
    sessions.value = res.items
  } catch (error) {
    console.error('Không thể tải danh sách kiểm kê:', error)
  } finally {
    loading.value = false
  }
}

const searchProductsForStocktake = async () => {
  if (!productSearchQuery.value.trim()) {
    searchProductsResult.value = []
    return
  }
  searchLoading.value = true
  try {
    const res = await getProducts({ search: productSearchQuery.value, pageSize: 10 })
    searchProductsResult.value = res.items
  } catch (error) {
    console.error('Không thể tìm kiếm sản phẩm:', error)
  } finally {
    searchLoading.value = false
  }
}

const addProductToStocktake = (prod: ProductDto) => {
  const existing = sessionLines.value.find(line => line.product.id === prod.id)
  if (existing) return
  sessionLines.value.push({
    product: prod,
    actualQty: prod.quantityOnHand,
    note: '',
  })
  productSearchQuery.value = ''
  searchProductsResult.value = []
}

const removeProductFromStocktake = (id: string) => {
  sessionLines.value = sessionLines.value.filter(l => l.product.id !== id)
}

const openCreateStocktake = () => {
  sessionLines.value = []
  sessionNote.value = ''
  createSessionDialog.value = true
}

const handleCreateSession = async () => {
  if (sessionLines.value.length === 0) {
    alert('Vui lòng thêm sản phẩm cần kiểm kê.')
    return
  }
  loading.value = true
  try {
    await createStocktakeSession({
      note: sessionNote.value || null,
      details: sessionLines.value.map(line => ({
        productId: line.product.id,
        actualQuantity: Number(line.actualQty) || 0,
        note: line.note || null,
      })),
    })
    createSessionDialog.value = false
    await loadSessions()
  } catch (error: any) {
    alert('Lỗi tạo phiên kiểm kê: ' + error.message)
  } finally {
    loading.value = false
  }
}

const openSessionDetail = async (id: string) => {
  loadingSessionDetail.value = true
  detailSessionDialog.value = true
  selectedSession.value = null
  try {
    selectedSession.value = await getStocktakeSession(id)
  } catch (error) {
    console.error(error)
  } finally {
    loadingSessionDetail.value = false
  }
}

const handleConfirmStocktake = async () => {
  if (!selectedSession.value) return
  if (!confirm('Bạn có chắc chắn muốn xác nhận đợt kiểm kê này không? Số lượng tồn kho thực tế sẽ được cập nhật đè lên hệ thống.')) return
  loading.value = true
  try {
    await confirmStocktake(selectedSession.value.id)
    detailSessionDialog.value = false
    await loadSessions()
    await loadStock()
  } catch (error: any) {
    alert('Lỗi xác nhận kiểm kê: ' + error.message)
  } finally {
    loading.value = false
  }
}

const handleCancelStocktake = async () => {
  if (!selectedSession.value) return
  if (!confirm('Bạn có chắc chắn muốn hủy đợt kiểm kê này?')) return
  loading.value = true
  try {
    await cancelStocktake(selectedSession.value.id)
    detailSessionDialog.value = false
    await loadSessions()
  } catch (error: any) {
    alert('Lỗi hủy kiểm kê: ' + error.message)
  } finally {
    loading.value = false
  }
}

watch(activeTab, (val) => {
  if (val === 0) loadStock()
  else if (val === 1) loadSessions()
})

onMounted(loadStock)
</script>

<template>
  <RetailPageHeader
    eyebrow="Kho hàng"
    title="Quản lý tồn kho"
    subtitle="Theo dõi tồn hệ thống, hàng đã giữ, ngưỡng cần nhập và đợt kiểm kê điều chỉnh tồn kho."
  >
    <template #actions>
      <div class="d-flex flex-wrap gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="activeTab === 0 ? loadStock() : loadSessions()"
        >
          Tải lại
        </VBtn>
        <VBtn
          prepend-icon="ri-checkbox-circle-line"
          @click="openCreateStocktake"
        >
          Tạo phiên kiểm kê
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

  <VTabs
    v-model="activeTab"
    class="mb-6"
  >
    <VTab :value="0">Báo cáo tồn kho</VTab>
    <VTab :value="1">Phiên kiểm kê điều chỉnh</VTab>
  </VTabs>

  <VWindow v-model="activeTab">
    <!-- Tab 0: Inventory reports & metrics -->
    <VWindowItem :value="0">
      <VRow
        v-if="loading"
        class="mb-2"
      >
        <VCol
          v-for="index in 4"
          :key="`inventory-metric-skeleton-${index}`"
          cols="12"
          sm="6"
          lg="3"
        >
          <VCard>
            <VCardText>
              <VSkeletonLoader type="list-item-avatar-two-line" />
            </VCardText>
          </VCard>
        </VCol>
      </VRow>

      <VRow
        v-else
        class="mb-2"
      >
        <VCol cols="12" sm="6" lg="3">
          <RetailMetricCard :metric="{ label: 'Tổng tồn', value: formatNumber(totalStock), helper: 'Đơn vị trong kho', icon: 'ri-archive-line', color: 'primary' }" />
        </VCol>
        <VCol cols="12" sm="6" lg="3">
          <RetailMetricCard :metric="{ label: 'Khả dụng', value: formatNumber(availableStock), helper: 'Sau khi trừ hàng giữ', icon: 'ri-inbox-line', color: 'success' }" />
        </VCol>
        <VCol cols="12" sm="6" lg="3">
          <RetailMetricCard :metric="{ label: 'Đang giữ', value: formatNumber(reservedStock), helper: 'Cho đơn chưa hoàn tất', icon: 'ri-lock-line', color: 'info' }" />
        </VCol>
        <VCol cols="12" sm="6" lg="3">
          <RetailMetricCard :metric="{ label: 'Cần nhập', value: String(lowStock), helper: 'SKU chạm ngưỡng', icon: 'ri-alert-line', color: 'warning' }" />
        </VCol>
      </VRow>

      <VCard>
        <VCardText>
          <VTextField
            v-model="search"
            label="Tìm tồn kho"
            placeholder="Tên sản phẩm hoặc SKU"
            prepend-inner-icon="ri-search-line"
            :disabled="loading"
            @keyup.enter="loadStock"
          />
        </VCardText>

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
              <th class="text-end">Tồn</th>
              <th class="text-end">Đã giữ</th>
              <th class="text-end">Khả dụng</th>
              <th class="text-end">Ngưỡng</th>
              <th>Cảnh báo</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="product in stockItems"
              :key="product.productId"
            >
              <td class="font-weight-bold">{{ product.productCode }}</td>
              <td>{{ product.productName }}</td>
              <td class="text-end">{{ product.quantityOnHand }} {{ product.unitName }}</td>
              <td class="text-end">{{ product.quantityReserved }} {{ product.unitName }}</td>
              <td class="text-end font-weight-bold">{{ product.availableQuantity }} {{ product.unitName }}</td>
              <td class="text-end">{{ product.minThreshold }} {{ product.unitName }}</td>
              <td>
                <RetailStatusChip :status="stockStatus(product)" />
              </td>
            </tr>
            <tr v-if="!loading && !stockItems.length">
              <td colspan="7" class="text-center text-medium-emphasis py-8">
                Không có dữ liệu tồn kho phù hợp.
              </td>
            </tr>
          </tbody>
        </VTable>
      </VCard>
    </VWindowItem>

    <!-- Tab 1: Stocktake Sessions -->
    <VWindowItem :value="1">
      <VCard>
        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Mã kiểm kê</th>
              <th>Người tạo</th>
              <th>Ngày tạo</th>
              <th>Ngày xác nhận</th>
              <th>Ghi chú</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="session in sessions"
              :key="session.id"
              class="cursor-pointer hover-row"
              @click="openSessionDetail(session.id)"
            >
              <td class="font-weight-bold text-primary">{{ session.code }}</td>
              <td>{{ session.createdByName }}</td>
              <td>{{ formatDate(session.createdAt) }}</td>
              <td>{{ formatDate(session.confirmedAt || '') }}</td>
              <td>{{ session.note || '—' }}</td>
              <td>
                <VChip
                  :color="session.status === 'Confirmed' ? 'success' : (session.status === 'Cancelled' ? 'error' : 'warning')"
                  size="small"
                >
                  {{ session.status === 'Confirmed' ? 'Đã cân kho' : (session.status === 'Cancelled' ? 'Đã hủy' : 'Bản nháp') }}
                </VChip>
              </td>
            </tr>
            <tr v-if="!sessions.length">
              <td colspan="6" class="text-center text-medium-emphasis py-8">
                Chưa lập đợt kiểm kê điều chỉnh nào.
              </td>
            </tr>
          </tbody>
        </VTable>
      </VCard>
    </VWindowItem>
  </VWindow>

  <!-- Create Stocktake Session Dialog -->
  <VDialog
    v-model="createSessionDialog"
    max-width="850"
    persistent
  >
    <VCard>
      <VCardTitle>Tạo phiên kiểm kê kho mới</VCardTitle>
      <VCardText>
        <VTextField
          v-model="sessionNote"
          label="Ghi chú đợt kiểm kê"
          class="mb-4"
        />

        <hr class="my-4" />

        <div class="d-flex gap-3 align-center mb-4">
          <VTextField
            v-model="productSearchQuery"
            label="Chọn sản phẩm muốn kiểm kê..."
            prepend-inner-icon="ri-search-line"
            hide-details
            @keyup.enter="searchProductsForStocktake"
          />
          <VBtn
            :loading="searchLoading"
            color="primary"
            @click="searchProductsForStocktake"
          >
            Tìm hàng
          </VBtn>
        </div>

        <VList
          v-if="searchProductsResult.length"
          class="border rounded mb-4"
          style="max-height: 200px; overflow-y: auto;"
        >
          <VListItem
            v-for="prod in searchProductsResult"
            :key="prod.id"
            class="cursor-pointer"
            @click="addProductToStocktake(prod)"
          >
            <VListItemTitle class="font-weight-bold">{{ prod.name }} ({{ prod.code }})</VListItemTitle>
            <VListItemSubtitle>Tồn hệ thống: {{ prod.quantityOnHand }} | Đơn vị: {{ prod.unitName }}</VListItemSubtitle>
          </VListItem>
        </VList>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Mã hàng</th>
              <th class="text-right">Tồn hệ thống</th>
              <th class="text-right" style="width: 180px;">Tồn thực tế</th>
              <th class="text-right">Chênh lệch</th>
              <th>Lý do / Ghi chú</th>
              <th class="text-center">Xóa</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="line in sessionLines"
              :key="line.product.id"
            >
              <td class="font-weight-bold">{{ line.product.name }}</td>
              <td>{{ line.product.code }}</td>
              <td class="text-right">{{ line.product.quantityOnHand }}</td>
              <td>
                <VTextField
                  v-model.number="line.actualQty"
                  type="number"
                  density="compact"
                  hide-details
                />
              </td>
              <td class="text-right font-weight-bold" :class="line.actualQty - line.product.quantityOnHand !== 0 ? 'text-warning' : 'text-success'">
                {{ line.actualQty - line.product.quantityOnHand }}
              </td>
              <td>
                <VTextField
                  v-model="line.note"
                  placeholder="Ghi chú dòng..."
                  density="compact"
                  hide-details
                />
              </td>
              <td class="text-center">
                <VBtn
                  icon="ri-delete-bin-line"
                  variant="text"
                  size="small"
                  color="error"
                  @click="removeProductFromStocktake(line.product.id)"
                />
              </td>
            </tr>
            <tr v-if="!sessionLines.length">
              <td colspan="7" class="text-center text-medium-emphasis py-6">
                Chưa thêm sản phẩm kiểm kê nào. Vui lòng sử dụng thanh tìm kiếm.
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
          @click="createSessionDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleCreateSession"
        >
          Lưu bản nháp kiểm kho
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Stocktake Details Dialog -->
  <VDialog
    v-model="detailSessionDialog"
    max-width="850"
  >
    <VCard v-if="selectedSession">
      <VCardTitle class="d-flex justify-between align-center">
        <span>Đợt kiểm kê: {{ selectedSession.code }}</span>
        <VChip :color="selectedSession.status === 'Confirmed' ? 'success' : (selectedSession.status === 'Cancelled' ? 'error' : 'warning')">
          {{ selectedSession.status === 'Confirmed' ? 'Đã cân kho' : (selectedSession.status === 'Cancelled' ? 'Đã hủy' : 'Bản nháp') }}
        </VChip>
      </VCardTitle>
      <VCardText>
        <VRow class="mb-4">
          <VCol cols="12" sm="6">
            <div><strong>Người tạo:</strong> {{ selectedSession.createdByName }}</div>
            <div><strong>Ngày tạo:</strong> {{ formatDate(selectedSession.createdAt) }}</div>
          </VCol>
          <VCol cols="12" sm="6">
            <div><strong>Ngày xác nhận:</strong> {{ formatDate(selectedSession.confirmedAt || '') }}</div>
            <div><strong>Ghi chú:</strong> {{ selectedSession.note || '—' }}</div>
          </VCol>
        </VRow>

        <VTable class="retail-table mb-4">
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Mã hàng</th>
              <th>ĐVT</th>
              <th class="text-right">Tồn hệ thống</th>
              <th class="text-right">Tồn thực tế</th>
              <th class="text-right">Chênh lệch</th>
              <th>Ghi chú</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="line in selectedSession.details"
              :key="line.id"
            >
              <td class="font-weight-bold">{{ line.productName }}</td>
              <td>{{ line.productCode }}</td>
              <td>{{ line.unitName }}</td>
              <td class="text-right">{{ line.systemQuantity }}</td>
              <td class="text-right font-weight-bold">{{ line.actualQuantity }}</td>
              <td class="text-right font-weight-bold" :class="line.differenceQuantity !== 0 ? 'text-error' : 'text-success'">
                {{ line.differenceQuantity > 0 ? '+' : '' }}{{ line.differenceQuantity }}
              </td>
              <td>{{ line.note || '—' }}</td>
            </tr>
          </tbody>
        </VTable>
      </VCardText>
      <VCardActions>
        <VBtn
          v-if="selectedSession.status === 'Draft'"
          color="success"
          variant="tonal"
          prepend-icon="ri-checkbox-circle-line"
          @click="handleConfirmStocktake"
        >
          Xác nhận cân kho
        </VBtn>
        <VBtn
          v-if="selectedSession.status === 'Draft'"
          color="error"
          variant="tonal"
          prepend-icon="ri-close-circle-line"
          @click="handleCancelStocktake"
        >
          Hủy kiểm kê
        </VBtn>
        <VSpacer />
        <VBtn
          color="secondary"
          @click="detailSessionDialog = false"
        >
          Đóng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
