<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import type { MetricItem } from '@/data/retailOps'
import {
  type CashShiftDto,
  getActivePromotions,
  getCurrentShift,
  getCustomers,
  getOrders,
} from '@/services/orderSalesApi'

const loading = ref(false)
const errorMessage = ref('')
const orderCount = ref(0)
const customerCount = ref(0)
const promotionCount = ref(0)
const currentShift = ref<CashShiftDto | null>(null)

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const metrics = computed<MetricItem[]>(() => [
  {
    label: 'Đơn hàng đang theo dõi',
    value: orderCount.value.toLocaleString('vi-VN'),
    helper: 'Tổng số đơn từ phân hệ bán hàng',
    icon: 'ri-receipt-line',
    color: 'primary',
  },
  {
    label: 'Khách hàng',
    value: customerCount.value.toLocaleString('vi-VN'),
    helper: 'Hồ sơ khách để tạo đơn và công nợ',
    icon: 'ri-user-3-line',
    color: 'success',
  },
  {
    label: 'Khuyến mãi hoạt động',
    value: promotionCount.value.toLocaleString('vi-VN'),
    helper: 'Mã có thể áp dụng tại POS',
    icon: 'ri-coupon-3-line',
    color: 'warning',
  },
  {
    label: 'Ca bán hiện tại',
    value: currentShift.value?.status === 'Open' ? 'Đang mở' : 'Chưa mở',
    helper: currentShift.value?.shiftCode ?? 'Mở ca trước khi thanh toán tại quầy',
    icon: 'ri-cash-line',
    color: currentShift.value?.status === 'Open' ? 'success' : 'secondary',
  },
])

const actions = [
  {
    title: 'Bán hàng POS',
    description: 'Tạo đơn nhanh tại quầy, áp khuyến mãi và chọn hình thức thanh toán.',
    icon: 'ri-shopping-cart-2-line',
    to: '/pos',
    color: 'primary',
  },
  {
    title: 'Quản lý đơn hàng',
    description: 'Theo dõi trạng thái, xem chi tiết và xử lý đơn sau bán.',
    icon: 'ri-receipt-line',
    to: '/orders',
    color: 'info',
  },
  {
    title: 'Khách hàng',
    description: 'Tra cứu hồ sơ khách, nhóm khách và lịch sử mua hàng.',
    icon: 'ri-user-3-line',
    to: '/customers',
    color: 'success',
  },
  {
    title: 'Công nợ',
    description: 'Theo dõi khoản phải thu và các đơn ghi nợ cần xử lý.',
    icon: 'ri-file-list-3-line',
    to: '/debts',
    color: 'warning',
  },
]

const shiftRows = computed(() => {
  if (!currentShift.value)
    return []

  return [
    {
      label: 'Thu ngân',
      value: currentShift.value.cashierName,
    },
    {
      label: 'Mã ca',
      value: currentShift.value.shiftCode,
    },
    {
      label: 'Tiền đầu ca',
      value: formatCurrency(currentShift.value.openingCash),
    },
    {
      label: 'Tiền kỳ vọng',
      value: formatCurrency(currentShift.value.expectedCash),
      class: 'text-primary',
    },
  ]
})

const shiftIsOpen = computed(() => currentShift.value?.status === 'Open')

const loadSalesWorkspace = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [orders, customers, promotions, shift] = await Promise.all([
      getOrders({ pageSize: 1 }),
      getCustomers({ pageSize: 1 }),
      getActivePromotions(),
      getCurrentShift().catch(() => null),
    ])

    orderCount.value = orders.totalCount
    customerCount.value = customers.totalCount
    promotionCount.value = promotions.length
    currentShift.value = shift
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu workspace bán hàng.'
  }
  finally {
    loading.value = false
  }
}

onMounted(loadSalesWorkspace)
</script>

<template>
  <RetailPageHeader
    eyebrow="Workspace bán hàng"
    title="Nhân viên bán hàng"
    subtitle="Không gian thao tác nhanh cho POS, đơn hàng, khách hàng, công nợ và khuyến mãi."
  >
    <template #actions>
      <VBtn
        variant="tonal"
        prepend-icon="ri-refresh-line"
        :loading="loading"
        @click="loadSalesWorkspace"
      >
        Tải lại
      </VBtn>
      <VBtn
        to="/pos"
        color="primary"
        prepend-icon="ri-shopping-cart-2-line"
      >
        Vào POS
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
            color="primary"
            variant="tonal"
            prepend-icon="ri-store-2-line"
            class="mb-4"
          >
            Quầy bán hàng
          </VChip>
          <h2 class="text-h4 font-weight-bold mb-2">
            Ưu tiên tốc độ tạo đơn và kiểm soát ca bán.
          </h2>
          <p class="text-body-1 text-medium-emphasis mb-0">
            Các thao tác POS, đơn hàng, khách hàng và công nợ được gom vào một workspace để nhân viên bán hàng xử lý nhanh tại quầy.
          </p>
        </VCol>
        <VCol
          cols="12"
          lg="4"
        >
          <div class="retail-muted-card pa-4">
            <div class="d-flex align-center justify-space-between mb-3">
              <span class="text-body-2 text-medium-emphasis">Trạng thái ca</span>
              <VChip
                :color="shiftIsOpen ? 'success' : 'secondary'"
                variant="tonal"
                size="small"
              >
                {{ shiftIsOpen ? 'Đang mở' : 'Chưa mở' }}
              </VChip>
            </div>
            <VBtn
              block
              color="primary"
              to="/pos"
              prepend-icon="ri-arrow-right-line"
            >
              Tiếp tục bán hàng
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
      <VCard class="retail-panel-card h-100">
        <VCardItem>
          <div class="retail-panel-heading">
            <div>
              <div class="retail-panel-kicker mb-1">
                Cash shift
              </div>
              <VCardTitle>Ca bán hiện tại</VCardTitle>
              <VCardSubtitle>Theo dõi tiền mặt và người phụ trách quầy</VCardSubtitle>
            </div>
            <VIcon
              icon="ri-cash-line"
              color="primary"
              size="28"
            />
          </div>
        </VCardItem>
        <VCardText v-if="shiftRows.length">
          <div class="d-flex flex-column ga-3">
            <div
              v-for="row in shiftRows"
              :key="row.label"
              class="d-flex align-center justify-space-between ga-4"
            >
              <span class="text-body-2 text-medium-emphasis">{{ row.label }}</span>
              <strong :class="row.class">{{ row.value }}</strong>
            </div>
          </div>
        </VCardText>
        <VCardText v-else>
          <div class="retail-empty-state">
            <div>
              <VIcon
                icon="ri-time-line"
                color="secondary"
                size="34"
                class="mb-2"
              />
              <div class="font-weight-bold text-high-emphasis mb-1">
                Chưa có ca mở
              </div>
              <div class="text-body-2">
                Vào POS để mở ca trước khi thanh toán.
              </div>
            </div>
          </div>
        </VCardText>
      </VCard>
    </VCol>
  </VRow>
</template>
