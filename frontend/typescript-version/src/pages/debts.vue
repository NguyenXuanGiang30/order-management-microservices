<script setup lang="ts">
import {
  type CustomerDto,
  type SupplierDto,
  getCustomers,
  getSuppliers,
} from '@/services/orderSalesApi'

const customers = ref<CustomerDto[]>([])
const suppliers = ref<SupplierDto[]>([])
const loading = ref(false)
const errorMessage = ref('')

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const receivables = computed(() => customers.value.reduce((sum, c) => sum + c.debtAmount, 0))
const payables = computed(() => suppliers.value.reduce((sum, s) => sum + s.debtAmount, 0))
const netDebt = computed(() => receivables.value - payables.value)

const customersWithDebt = computed(() => customers.value.filter(c => c.debtAmount > 0))
const suppliersWithDebt = computed(() => suppliers.value.filter(s => s.debtAmount > 0))

const loadDebts = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const [customerResult, supplierResult] = await Promise.all([
      getCustomers({ pageSize: 100 }),
      getSuppliers({ pageSize: 100 }),
    ])

    customers.value = customerResult.items
    suppliers.value = supplierResult.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải dữ liệu công nợ.'
  }
  finally {
    loading.value = false
  }
}

onMounted(loadDebts)
</script>

<template>
  <RetailPageHeader
    eyebrow="Kiểm soát công nợ"
    title="Công nợ"
    subtitle="Theo dõi công nợ khách hàng phải thu và công nợ nhà cung cấp phải trả."
  >
    <template #actions>
      <VBtn prepend-icon="ri-money-dollar-circle-line">
        Ghi nhận thanh toán
      </VBtn>
    </template>
  </RetailPageHeader>

  <VAlert
    v-if="errorMessage"
    type="error"
    variant="tonal"
    class="mb-6"
  >
    {{ errorMessage }}
  </VAlert>

  <VRow class="mb-2">
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Phải thu', value: formatCurrency(receivables), helper: 'Công nợ khách hàng', icon: 'ri-arrow-left-down-line', color: 'warning' }" />
    </VCol>
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Phải trả', value: formatCurrency(payables), helper: 'Công nợ nhà cung cấp', icon: 'ri-arrow-right-up-line', color: 'error' }" />
    </VCol>
    <VCol
      cols="12"
      md="4"
    >
      <RetailMetricCard :metric="{ label: 'Chênh lệch', value: formatCurrency(netDebt), helper: 'Phải thu trừ phải trả', icon: 'ri-scales-3-line', color: 'primary' }" />
    </VCol>
  </VRow>

  <VRow>
    <VCol
      cols="12"
      lg="6"
    >
      <VCard class="retail-panel-card">
        <VCardItem>
          <VCardTitle>Công nợ khách hàng</VCardTitle>
          <VCardSubtitle>Danh sách khách hàng còn nợ chưa thanh toán</VCardSubtitle>
        </VCardItem>
        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Khách hàng</th>
              <th>Điện thoại</th>
              <th class="text-end">Số tiền nợ</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="customer in customersWithDebt"
              :key="customer.id"
              class="hover-row"
            >
              <td class="font-weight-bold">{{ customer.fullName }}</td>
              <td>{{ customer.phone || '—' }}</td>
              <td class="text-end font-weight-bold text-warning">{{ formatCurrency(customer.debtAmount) }}</td>
            </tr>
            <RetailEmptyState
              v-if="!loading && !customersWithDebt.length"
              :colspan="3"
              icon="ri-check-double-line"
              title="Không có công nợ khách hàng"
              subtitle="Tất cả khách hàng đã thanh toán đầy đủ."
            />
          </tbody>
        </VTable>
      </VCard>
    </VCol>

    <VCol
      cols="12"
      lg="6"
    >
      <VCard class="retail-panel-card">
        <VCardItem>
          <VCardTitle>Công nợ nhà cung cấp</VCardTitle>
          <VCardSubtitle>Danh sách nhà cung cấp cần thanh toán</VCardSubtitle>
        </VCardItem>
        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Nhà cung cấp</th>
              <th>Ghi chú</th>
              <th class="text-end">Số tiền nợ</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="supplier in suppliersWithDebt"
              :key="supplier.id"
              class="hover-row"
            >
              <td class="font-weight-bold">{{ supplier.name }}</td>
              <td>{{ supplier.note || '—' }}</td>
              <td class="text-end font-weight-bold text-error">{{ formatCurrency(supplier.debtAmount) }}</td>
            </tr>
            <RetailEmptyState
              v-if="!loading && !suppliersWithDebt.length"
              :colspan="3"
              icon="ri-check-double-line"
              title="Không có công nợ nhà cung cấp"
              subtitle="Đã thanh toán đầy đủ cho tất cả nhà cung cấp."
            />
          </tbody>
        </VTable>
      </VCard>
    </VCol>
  </VRow>
</template>
