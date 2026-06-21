<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import type { MetricItem } from '@/data/retailOps'
import { getSuppliers } from '@/services/orderSalesApi'
import {
  type StockDto,
  getGoodsReceipts,
  getInventoryStock,
  getProducts,
} from '@/services/productInventoryApi'

const loading = ref(false)
const errorMessage = ref('')
const productCount = ref(0)
const lowStockCount = ref(0)
const receiptCount = ref(0)
const supplierCount = ref(0)
const lowStockItems = ref<StockDto[]>([])

const metrics = computed<MetricItem[]>(() => [
  {
    label: 'Sản phẩm đang quản lý',
    value: productCount.value.toLocaleString('vi-VN'),
    helper: 'Danh mục hàng hóa đang hoạt động',
    icon: 'ri-price-tag-3-line',
    color: 'primary',
  },
  {
    label: 'Cảnh báo tồn thấp',
    value: lowStockCount.value.toLocaleString('vi-VN'),
    helper: 'SKU dưới ngưỡng cần kiểm tra',
    icon: 'ri-alert-line',
    color: lowStockCount.value > 0 ? 'warning' : 'success',
  },
  {
    label: 'Phiếu nhập',
    value: receiptCount.value.toLocaleString('vi-VN'),
    helper: 'Theo dõi nhập hàng từ nhà cung cấp',
    icon: 'ri-truck-line',
    color: 'info',
  },
  {
    label: 'Nhà cung cấp',
    value: supplierCount.value.toLocaleString('vi-VN'),
    helper: 'Đối tác cung ứng và công nợ liên quan',
    icon: 'ri-building-4-line',
    color: 'success',
  },
])

const actions = [
  {
    title: 'Kiểm soát tồn kho',
    description: 'Xem tồn khả dụng, cảnh báo thấp kho và lịch sử biến động.',
    icon: 'ri-archive-line',
    to: '/inventory',
    color: 'primary',
  },
  {
    title: 'Sản phẩm',
    description: 'Quản lý SKU, danh mục, đơn vị tính, giá nhập và giá bán.',
    icon: 'ri-price-tag-3-line',
    to: '/products',
    color: 'success',
  },
  {
    title: 'Nhập hàng',
    description: 'Tạo phiếu nhập, xác nhận hàng về kho và cập nhật tồn.',
    icon: 'ri-truck-line',
    to: '/goods-receipts',
    color: 'info',
  },
  {
    title: 'Nhà cung cấp',
    description: 'Theo dõi thông tin NCC và công nợ phải trả.',
    icon: 'ri-building-4-line',
    to: '/suppliers',
    color: 'warning',
  },
]

const warehouseFlow = [
  {
    title: 'Kiểm tra cảnh báo',
    description: 'Ưu tiên SKU dưới ngưỡng tồn tối thiểu.',
    icon: 'ri-alarm-warning-line',
  },
  {
    title: 'Xác nhận nhập hàng',
    description: 'Phiếu nhập đã xác nhận sẽ cập nhật tồn kho.',
    icon: 'ri-truck-line',
  },
  {
    title: 'Đối soát sản phẩm',
    description: 'Kiểm tra SKU, đơn vị tính và giá bán trước khi bán.',
    icon: 'ri-checkbox-circle-line',
  },
]

const loadWarehouseWorkspace = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [products, lowStock, receipts, suppliers] = await Promise.all([
      getProducts({ pageSize: 1 }),
      getInventoryStock({ belowMin: true }),
      getGoodsReceipts({ pageSize: 1 }),
      getSuppliers({ pageSize: 1 }),
    ])

    productCount.value = products.totalCount
    lowStockCount.value = lowStock.length
    lowStockItems.value = lowStock.slice(0, 5)
    receiptCount.value = receipts.totalCount
    supplierCount.value = suppliers.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu workspace kho.'
  }
  finally {
    loading.value = false
  }
}

onMounted(loadWarehouseWorkspace)
</script>

<template>
  <RetailPageHeader
    eyebrow="Workspace kho hàng"
    title="Thủ kho"
    subtitle="Không gian kiểm soát tồn kho, sản phẩm, nhập hàng và nhà cung cấp."
  >
    <template #actions>
      <VBtn
        variant="tonal"
        prepend-icon="ri-refresh-line"
        :loading="loading"
        @click="loadWarehouseWorkspace"
      >
        Tải lại
      </VBtn>
      <VBtn
        to="/inventory"
        color="primary"
        prepend-icon="ri-archive-line"
      >
        Xem tồn kho
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

  <VCard class="workspace-hero-card mb-6">
    <VCardText>
      <VRow align="center">
        <VCol
          cols="12"
          lg="8"
        >
          <VChip
            :color="lowStockCount > 0 ? 'warning' : 'success'"
            variant="tonal"
            prepend-icon="ri-archive-line"
            class="mb-4"
          >
            Điều phối kho
          </VChip>
          <h2 class="text-h4 font-weight-bold mb-2">
            Theo dõi tồn kho và nhập hàng từ một màn hình.
          </h2>
          <p class="text-body-1 text-medium-emphasis mb-0">
            Các cảnh báo tồn thấp, phiếu nhập, sản phẩm và nhà cung cấp được gom vào workspace để thủ kho xử lý nhanh các việc ưu tiên.
          </p>
        </VCol>
        <VCol
          cols="12"
          lg="4"
        >
          <div class="retail-muted-card pa-4">
            <div class="d-flex align-center justify-space-between mb-3">
              <span class="text-body-2 text-medium-emphasis">SKU cần kiểm tra</span>
              <strong class="text-h5">{{ lowStockCount.toLocaleString('vi-VN') }}</strong>
            </div>
            <VBtn
              block
              color="primary"
              to="/inventory"
              prepend-icon="ri-arrow-right-line"
            >
              Mở tồn kho
            </VBtn>
          </div>
        </VCol>
      </VRow>
    </VCardText>
  </VCard>

  <VRow class="workspace-metric-grid mb-6">
    <VCol
      v-for="metric in metrics"
      :key="metric.label"
      cols="12"
      sm="6"
      lg="3"
    >
      <VSkeletonLoader
        v-if="loading"
        type="article"
      />
      <RetailMetricCard
        v-else
        :metric="metric"
      />
    </VCol>
  </VRow>

  <VRow>
    <VCol
      cols="12"
      lg="8"
    >
      <VRow class="workspace-action-grid">
        <VCol
          v-for="action in actions"
          :key="action.to"
          cols="12"
          md="6"
        >
          <VCard
            class="workspace-action-card h-100"
            :to="action.to"
            link
          >
            <VCardText>
              <div class="d-flex align-start justify-space-between ga-4">
                <div>
                  <div class="text-h6 font-weight-bold mb-2">
                    {{ action.title }}
                  </div>
                  <p class="text-body-2 text-medium-emphasis mb-0">
                    {{ action.description }}
                  </p>
                </div>
                <VAvatar
                  :color="action.color"
                  variant="tonal"
                  rounded="lg"
                >
                  <VIcon :icon="action.icon" />
                </VAvatar>
              </div>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>
    </VCol>

    <VCol
      cols="12"
      lg="4"
    >
      <VCard class="retail-panel-card mb-4">
        <VCardItem>
          <div class="retail-panel-heading">
            <div>
              <div class="retail-panel-kicker mb-1">
                Stock alert
              </div>
              <VCardTitle>Cảnh báo tồn thấp</VCardTitle>
              <VCardSubtitle>Danh sách SKU cần ưu tiên kiểm tra</VCardSubtitle>
            </div>
            <VIcon
              icon="ri-alert-line"
              :color="lowStockCount > 0 ? 'warning' : 'success'"
              size="28"
            />
          </div>
        </VCardItem>
        <VList v-if="lowStockItems.length">
          <VListItem
            v-for="stock in lowStockItems"
            :key="stock.productId"
          >
            <VListItemTitle>{{ stock.productName }}</VListItemTitle>
            <VListItemSubtitle>{{ stock.productCode }} · {{ stock.unitName }}</VListItemSubtitle>
            <template #append>
              <VChip
                :color="stock.alertLevel === 'Critical' ? 'error' : 'warning'"
                size="small"
                variant="tonal"
              >
                Tồn {{ stock.availableQuantity }}
              </VChip>
            </template>
          </VListItem>
        </VList>
        <VCardText v-else>
          <div class="retail-empty-state">
            <div>
              <VIcon
                icon="ri-shield-check-line"
                color="success"
                size="34"
                class="mb-2"
              />
              <div class="font-weight-bold text-high-emphasis mb-1">
                Tồn kho đang an toàn
              </div>
              <div class="text-body-2">
                Chưa có SKU nào dưới ngưỡng tối thiểu.
              </div>
            </div>
          </div>
        </VCardText>
      </VCard>

      <VCard class="retail-panel-card">
        <VCardItem>
          <VCardTitle>Quy trình ưu tiên</VCardTitle>
          <VCardSubtitle>Các bước vận hành kho hằng ngày</VCardSubtitle>
        </VCardItem>
        <VCardText>
          <div class="d-flex flex-column ga-4">
            <div
              v-for="item in warehouseFlow"
              :key="item.title"
              class="d-flex ga-3"
            >
              <VAvatar
                color="primary"
                variant="tonal"
                size="36"
              >
                <VIcon :icon="item.icon" />
              </VAvatar>
              <div>
                <div class="font-weight-bold">
                  {{ item.title }}
                </div>
                <div class="text-body-2 text-medium-emphasis">
                  {{ item.description }}
                </div>
              </div>
            </div>
          </div>
        </VCardText>
      </VCard>
    </VCol>
  </VRow>
</template>
