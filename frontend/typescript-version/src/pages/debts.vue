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
      <VCard>
        <VCardItem>
          <VCardTitle>Công nợ khách hàng</VCardTitle>
        </VCardItem>
        <VList>
          <VListItem
            v-for="customer in customersWithDebt"
            :key="customer.id"
          >
            <VListItemTitle>{{ customer.fullName }}</VListItemTitle>
            <VListItemSubtitle>{{ customer.phone || '—' }}</VListItemSubtitle>
            <template #append>
              <strong>{{ formatCurrency(customer.debtAmount) }}</strong>
            </template>
          </VListItem>
          <VListItem v-if="!loading && !customersWithDebt.length">
            <VListItemTitle class="text-medium-emphasis">
              Không có công nợ khách hàng.
            </VListItemTitle>
          </VListItem>
        </VList>
      </VCard>
    </VCol>

    <VCol
      cols="12"
      lg="6"
    >
      <VCard>
        <VCardItem>
          <VCardTitle>Công nợ nhà cung cấp</VCardTitle>
        </VCardItem>
        <VList>
          <VListItem
            v-for="supplier in suppliersWithDebt"
            :key="supplier.id"
          >
            <VListItemTitle>{{ supplier.name }}</VListItemTitle>
            <VListItemSubtitle>{{ supplier.note || '—' }}</VListItemSubtitle>
            <template #append>
              <strong>{{ formatCurrency(supplier.debtAmount) }}</strong>
            </template>
          </VListItem>
          <VListItem v-if="!loading && !suppliersWithDebt.length">
            <VListItemTitle class="text-medium-emphasis">
              Không có công nợ nhà cung cấp.
            </VListItemTitle>
          </VListItem>
        </VList>
      </VCard>
    </VCol>
  </VRow>
</template>
