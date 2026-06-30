<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'

import {
  type CustomerDebtDto,
  type CustomerDetailDto,
  type CustomerDto,
  type CustomerGroupDto,
  createCustomer,
  createCustomerGroup,
  deleteCustomerGroup,
  getCustomerDebt,
  getCustomerDetails,
  getCustomerGroups,
  getCustomers,
  updateCustomer,
  updateCustomerGroup,
} from '@/services/orderSalesApi'

const customers = ref<CustomerDto[]>([])
const customerGroups = ref<CustomerGroupDto[]>([])

const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(0)
const totalCount = ref(0)

const customerDialog = ref(false)
const groupDialog = ref(false)
const detailDialog = ref(false)

const isEditingCustomer = ref(false)
const isEditingGroup = ref(false)

const selectedCustomerDetail = ref<CustomerDetailDto | null>(null)
const selectedCustomerDebt = ref<CustomerDebtDto | null>(null)
const loadingDetails = ref(false)
const customerDetailTab = ref('orders')

const customerForm = ref({
  id: '',
  code: '',
  fullName: '',
  phone: '',
  email: '',
  address: '',
  taxCode: '',
  customerGroupId: null as string | null,
})

const groupForm = ref({
  id: '',
  name: '',
  description: '',
  discountPercent: 0,
})

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatDate = (dateStr: string) => {
  if (!dateStr)
    return '—'

  const date = new Date(dateStr)

  if (Number.isNaN(date.getTime()))
    return '—'

  return date.toLocaleDateString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const totalCustomers = computed(() => totalCount.value)

const customersWithDebt = computed(() =>
  customers.value.filter(customer => (customer.debtAmount || 0) > 0).length,
)

const totalDebt = computed(() =>
  customers.value.reduce((sum, customer) => sum + (customer.debtAmount || 0), 0),
)

const activeCustomers = computed(() =>
  customers.value.filter(customer => customer.isActive).length,
)

const filteredCustomers = computed(() => customers.value)

const summaryCards = computed(() => [
  {
    label: 'Tổng khách hàng',
    value: formatNumber(totalCustomers.value),
    helper: `${formatNumber(activeCustomers.value)} khách đang hoạt động trên trang này`,
    icon: 'ri-group-line',
    color: 'primary',
  },
  {
    label: 'Khách có công nợ',
    value: formatNumber(customersWithDebt.value),
    helper: 'Cần theo dõi thanh toán',
    icon: 'ri-user-follow-line',
    color: 'warning',
  },
  {
    label: 'Công nợ trang này',
    value: formatCurrency(totalDebt.value),
    helper: 'Phải thu từ khách hàng đang hiển thị',
    icon: 'ri-money-dollar-circle-line',
    color: 'error',
  },
  {
    label: 'Nhóm khách hàng',
    value: formatNumber(customerGroups.value.length),
    helper: 'Phân hạng và chiết khấu khách hàng',
    icon: 'ri-vip-crown-line',
    color: 'success',
  },
])

const customerActions = (customer: CustomerDto): ActionMenuItem[] => [
  {
    label: 'Xem chi tiết',
    icon: 'ri-eye-line',
    handler: () => openCustomerDetails(customer),
  },
  {
    label: 'Chỉnh sửa',
    icon: 'ri-edit-line',
    color: 'primary',
    handler: () => openEditCustomer(customer),
  },
]

const loadCustomers = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getCustomers({
      search: search.value.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    customers.value = result.items
    totalPages.value = result.totalPages
    totalCount.value = result.totalCount
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách khách hàng.'
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadCustomers()
  else
    page.value = 1
}

const loadCustomerGroups = async () => {
  try {
    customerGroups.value = await getCustomerGroups()
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải nhóm khách hàng.'
  }
}

const resetCustomerForm = () => {
  customerForm.value = {
    id: '',
    code: '',
    fullName: '',
    phone: '',
    email: '',
    address: '',
    taxCode: '',
    customerGroupId: customerGroups.value[0]?.id || null,
  }
}

const openAddCustomer = () => {
  isEditingCustomer.value = false
  resetCustomerForm()
  customerDialog.value = true
}

const openEditCustomer = (customer: CustomerDto) => {
  isEditingCustomer.value = true

  customerForm.value = {
    id: customer.id,
    code: customer.code,
    fullName: customer.fullName,
    phone: customer.phone || '',
    email: customer.email || '',
    address: customer.address || '',
    taxCode: customer.taxCode || '',
    customerGroupId: customer.customerGroupId,
  }

  customerDialog.value = true
}

const handleSaveCustomer = async () => {
  if (!customerForm.value.fullName.trim()) {
    errorMessage.value = 'Vui lòng nhập tên khách hàng.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const payload = {
      code: customerForm.value.code || undefined,
      fullName: customerForm.value.fullName.trim(),
      phone: customerForm.value.phone || null,
      email: customerForm.value.email || null,
      address: customerForm.value.address || null,
      taxCode: customerForm.value.taxCode || null,
      customerGroupId: customerForm.value.customerGroupId || null,
    }

    if (isEditingCustomer.value) {
      await updateCustomer(customerForm.value.id, payload)
      successMessage.value = 'Cập nhật khách hàng thành công.'
    }
    else {
      await createCustomer(payload)
      successMessage.value = 'Thêm khách hàng mới thành công.'
    }

    customerDialog.value = false
    await loadCustomers()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu thông tin khách hàng.'
  }
  finally {
    loading.value = false
  }
}

const openCustomerDetails = async (customer: CustomerDto) => {
  loadingDetails.value = true
  selectedCustomerDetail.value = null
  selectedCustomerDebt.value = null
  customerDetailTab.value = 'orders'
  detailDialog.value = true

  try {
    const [detail, debt] = await Promise.all([
      getCustomerDetails(customer.id),
      getCustomerDebt(customer.id),
    ])

    selectedCustomerDetail.value = detail
    selectedCustomerDebt.value = debt
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Không thể tải chi tiết khách hàng.'
  }
  finally {
    loadingDetails.value = false
  }
}

const resetGroupForm = () => {
  isEditingGroup.value = false

  groupForm.value = {
    id: '',
    name: '',
    description: '',
    discountPercent: 0,
  }
}

const openGroupDialog = () => {
  resetGroupForm()
  groupDialog.value = true
}

const openEditGroup = (group: CustomerGroupDto) => {
  isEditingGroup.value = true

  groupForm.value = {
    id: group.id,
    name: group.name,
    description: (group as any).note || (group as any).description || '',
    discountPercent: group.defaultDiscountPercent || 0,
  }
}

const handleSaveGroup = async () => {
  if (!groupForm.value.name.trim()) {
    errorMessage.value = 'Vui lòng nhập tên nhóm khách hàng.'

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const payload = {
      name: groupForm.value.name.trim(),
      note: groupForm.value.description || null,
      defaultDiscountPercent: Number(groupForm.value.discountPercent) || 0,
    }

    if (isEditingGroup.value) {
      await updateCustomerGroup(groupForm.value.id, payload)
      successMessage.value = 'Cập nhật nhóm khách hàng thành công.'
    }
    else {
      await createCustomerGroup(payload)
      successMessage.value = 'Tạo nhóm khách hàng thành công.'
    }

    resetGroupForm()
    await loadCustomerGroups()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu nhóm khách hàng.'
  }
  finally {
    loading.value = false
  }
}

const handleDeleteGroup = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa nhóm khách hàng này?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await deleteCustomerGroup(id)
    successMessage.value = 'Xóa nhóm khách hàng thành công.'

    await loadCustomerGroups()
  }
  catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xóa nhóm khách hàng.'
  }
  finally {
    loading.value = false
  }
}

watch(page, () => {
  void loadCustomers()
})

watch(pageSize, () => {
  page.value = 1
  void loadCustomers()
})

onMounted(async () => {
  await loadCustomerGroups()
  await loadCustomers()
})
</script>

<template>
  <section class="customers-page">
    <div class="customers-hero">
      <div class="customers-hero__title-area">
        <h1>Quản lý khách hàng</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-user-heart-line" class="ml-2">
          Khách hàng
        </VChip>
      </div>

      <div class="customers-hero__actions">
        <VBtn variant="outlined" color="secondary" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadCustomers">
          Tải lại
        </VBtn>

        <VBtn variant="tonal" color="primary" prepend-icon="ri-group-line" @click="openGroupDialog">
          Quản lý nhóm
        </VBtn>

        <VBtn color="primary" prepend-icon="ri-user-add-line" class="primary-action" @click="openAddCustomer">
          Thêm khách hàng
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

    <VCard class="customers-panel">
      <VCardText>
        <div class="customers-toolbar">
          <VTextField v-model="search" label="Tìm khách hàng" placeholder="Tên, mã khách, số điện thoại..."
            prepend-inner-icon="ri-search-line" density="comfortable" hide-details clearable @keyup.enter="resetAndLoad"
            @click:clear="search = ''; resetAndLoad()" />

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
        <div v-if="filteredCustomers.length" class="customers-table-wrap">
          <VTable class="customers-table">
            <thead>
              <tr>
                <th>Mã KH</th>
                <th>Tên khách</th>
                <th>Điện thoại</th>
                <th>Nhóm</th>
                <th class="text-end">Công nợ</th>
                <th class="text-center">Trạng thái</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>

            <tbody>
              <tr v-for="customer in filteredCustomers" :key="customer.id" class="customer-row"
                @click="openCustomerDetails(customer)">
                <td>
                  <div class="customer-code">
                    {{ customer.code }}
                  </div>
                </td>

                <td>
                  <div class="customer-cell">
                    <VAvatar size="38" color="primary" variant="tonal">
                      <VIcon icon="ri-user-line" />
                    </VAvatar>

                    <div>
                      <strong>{{ customer.fullName }}</strong>
                      <span>{{ customer.email || 'Chưa có email' }}</span>
                    </div>
                  </div>
                </td>

                <td>{{ customer.phone || '—' }}</td>

                <td>
                  <VChip color="secondary" variant="tonal" size="small">
                    {{ customer.customerGroupName || 'Vãng lai' }}
                  </VChip>
                </td>

                <td class="text-end debt-cell" :class="{ 'debt-cell--danger': customer.debtAmount > 0 }">
                  {{ formatCurrency(customer.debtAmount) }}
                </td>

                <td class="text-center">
                  <VChip :color="customer.isActive ? 'success' : 'secondary'" variant="tonal" size="small">
                    {{ customer.isActive ? 'Đang hoạt động' : 'Tạm dừng' }}
                  </VChip>
                </td>

                <td class="text-center" @click.stop>
                  <RetailActionMenu :items="customerActions(customer)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>

        <div v-else class="customers-empty">
          <VIcon icon="ri-user-line" size="42" color="primary" />
          <strong>Chưa có dữ liệu khách hàng</strong>
          <span>Thêm khách hàng mới để bắt đầu quản lý.</span>

          <VBtn color="primary" prepend-icon="ri-user-add-line" @click="openAddCustomer">
            Thêm khách hàng
          </VBtn>
        </div>
      </template>

      <div v-if="customers.length || totalCount > 0" class="customers-pagination">
        <span>
          Hiển thị {{ formatNumber(customers.length) }} trên tổng số {{ formatNumber(totalCount) }} khách hàng
        </span>

        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />

          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>

    <VDialog v-model="customerDialog" max-width="760" scrollable>
      <VCard class="customer-dialog">
        <div class="dialog-head">
          <div>
            <span>{{ isEditingCustomer ? 'Cập nhật khách hàng' : 'Thêm khách hàng mới' }}</span>
            <h2>{{ isEditingCustomer ? customerForm.fullName : 'Khách hàng mới' }}</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ customerForm.code || 'Tự sinh mã' }}
          </VChip>
        </div>

        <VCardText>
          <VRow>
            <VCol cols="12" sm="6">
              <VTextField v-model="customerForm.code" label="Mã khách hàng" placeholder="Bỏ trống để hệ thống tự sinh"
                density="comfortable" />
            </VCol>

            <VCol cols="12" sm="6">
              <VSelect v-model="customerForm.customerGroupId" label="Nhóm khách hàng" :items="customerGroups"
                item-title="name" item-value="id" clearable density="comfortable" />
            </VCol>

            <VCol cols="12">
              <VTextField v-model="customerForm.fullName" label="Tên khách hàng *" density="comfortable" required />
            </VCol>

            <VCol cols="12" sm="6">
              <VTextField v-model="customerForm.phone" label="Số điện thoại" density="comfortable" />
            </VCol>

            <VCol cols="12" sm="6">
              <VTextField v-model="customerForm.email" label="Email" type="email" density="comfortable" />
            </VCol>

            <VCol cols="12" sm="8">
              <VTextField v-model="customerForm.address" label="Địa chỉ" density="comfortable" />
            </VCol>

            <VCol cols="12" sm="4">
              <VTextField v-model="customerForm.taxCode" label="Mã số thuế" density="comfortable" />
            </VCol>
          </VRow>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="text" @click="customerDialog = false">
            Hủy
          </VBtn>

          <VBtn color="primary" :loading="loading" @click="handleSaveCustomer">
            Lưu khách hàng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="groupDialog" max-width="820" scrollable>
      <VCard class="customer-dialog">
        <div class="dialog-head">
          <div>
            <span>Nhóm khách hàng</span>
            <h2>Quản lý nhóm</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ customerGroups.length }} nhóm
          </VChip>
        </div>

        <VCardText>
          <div class="group-form-card">
            <VTextField v-model="groupForm.name" label="Tên nhóm *" placeholder="VD: VIP, Đại lý, Khách thân thiết..."
              density="comfortable" hide-details="auto" />

            <VTextField v-model.number="groupForm.discountPercent" type="number" label="Chiết khấu" suffix="%"
              density="comfortable" hide-details="auto" />

            <VTextField v-model="groupForm.description" label="Ghi chú" density="comfortable" hide-details="auto" />

            <VBtn color="primary" prepend-icon="ri-save-line" :loading="loading" @click="handleSaveGroup">
              {{ isEditingGroup ? 'Cập nhật' : 'Thêm nhóm' }}
            </VBtn>

            <VBtn v-if="isEditingGroup" color="secondary" variant="outlined" @click="resetGroupForm">
              Hủy sửa
            </VBtn>
          </div>

          <div class="group-table-wrap">
            <VTable class="customers-table compact-table">
              <thead>
                <tr>
                  <th>Tên nhóm</th>
                  <th class="text-end">Chiết khấu</th>
                  <th class="text-center">Thao tác</th>
                </tr>
              </thead>

              <tbody>
                <tr v-for="group in customerGroups" :key="group.id">
                  <td class="font-weight-bold">
                    {{ group.name }}
                  </td>

                  <td class="text-end text-success font-weight-bold">
                    {{ group.defaultDiscountPercent }}%
                  </td>

                  <td class="text-center">
                    <VBtn icon="ri-edit-line" variant="text" size="small" color="primary"
                      @click="openEditGroup(group)" />

                    <VBtn icon="ri-delete-bin-line" variant="text" size="small" color="error"
                      @click="handleDeleteGroup(group.id)" />
                  </td>
                </tr>

                <tr v-if="!customerGroups.length">
                  <td colspan="3" class="text-center text-medium-emphasis py-6">
                    Chưa có nhóm khách hàng nào.
                  </td>
                </tr>
              </tbody>
            </VTable>
          </div>
        </VCardText>

        <VCardActions class="dialog-actions">
          <VSpacer />

          <VBtn color="secondary" variant="outlined" @click="groupDialog = false">
            Đóng
          </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>

    <VDialog v-model="detailDialog" max-width="980" scrollable>
      <VCard class="customer-dialog">
        <div v-if="selectedCustomerDetail" class="dialog-head">
          <div>
            <span>Chi tiết khách hàng</span>
            <h2>{{ selectedCustomerDetail.fullName }}</h2>
          </div>

          <VChip color="primary" variant="tonal">
            {{ selectedCustomerDetail.customerGroupName || 'Vãng lai' }}
          </VChip>
        </div>

        <VCardText v-if="loadingDetails">
          <VSkeletonLoader type="heading, paragraph, table-heading, table-tbody" />
        </VCardText>

        <template v-else-if="selectedCustomerDetail">
          <VCardText>
            <div class="detail-grid">
              <div class="detail-card">
                <span>Liên hệ</span>
                <strong>{{ selectedCustomerDetail.phone || '—' }}</strong>
                <p>{{ selectedCustomerDetail.email || 'Chưa có email' }}</p>
              </div>

              <div class="detail-card">
                <span>Tổng mua hàng</span>
                <strong class="text-primary">
                  {{ formatCurrency(selectedCustomerDetail.totalPurchased) }}
                </strong>
                <p>{{ selectedCustomerDetail.address || 'Chưa có địa chỉ' }}</p>
              </div>

              <div class="detail-card">
                <span>Công nợ hiện tại</span>
                <strong class="text-error">
                  {{ formatCurrency(selectedCustomerDetail.debtAmount) }}
                </strong>
                <p>Ngày tạo: {{ formatDate(selectedCustomerDetail.createdAt) }}</p>
              </div>
            </div>

            <VTabs v-model="customerDetailTab" color="primary" class="detail-tabs">
              <VTab value="orders">
                Lịch sử đơn hàng
              </VTab>

              <VTab value="debt">
                Chi tiết công nợ
              </VTab>
            </VTabs>

            <VDivider />

            <VWindow v-model="customerDetailTab">
              <VWindowItem value="orders">
                <div class="detail-table-wrap">
                  <VTable class="customers-table compact-table">
                    <thead>
                      <tr>
                        <th>Mã đơn</th>
                        <th>Ngày tạo</th>
                        <th class="text-end">Tổng tiền</th>
                        <th class="text-end">Thanh toán</th>
                        <th>Trạng thái</th>
                      </tr>
                    </thead>

                    <tbody>
                      <tr v-for="order in selectedCustomerDetail.orders" :key="order.id">
                        <td class="font-weight-bold text-primary">
                          {{ order.orderCode }}
                        </td>

                        <td>
                          {{ formatDate(order.createdAt) }}
                        </td>

                        <td class="text-end">
                          {{ formatCurrency(order.finalAmount) }}
                        </td>

                        <td class="text-end">
                          {{ formatCurrency(order.paidAmount) }}
                        </td>

                        <td>
                          <RetailStatusChip :status="order.status" />
                        </td>
                      </tr>

                      <tr v-if="!selectedCustomerDetail.orders?.length">
                        <td colspan="5" class="text-center text-medium-emphasis py-6">
                          Chưa có đơn hàng nào từ khách hàng này.
                        </td>
                      </tr>
                    </tbody>
                  </VTable>
                </div>
              </VWindowItem>

              <VWindowItem value="debt">
                <div class="debt-detail-box">
                  <div>
                    <span>Công nợ hiện tại</span>
                    <strong>{{ formatCurrency(selectedCustomerDetail.debtAmount) }}</strong>
                  </div>

                  <div>
                    <span>Số giao dịch công nợ</span>
                    <strong>{{ selectedCustomerDebt?.payments?.length || 0 }}</strong>
                  </div>
                </div>

                <div class="detail-table-wrap">
                  <VTable class="customers-table compact-table">
                    <thead>
                      <tr>
                        <th>Thời gian</th>
                        <th>Phương thức</th>
                        <th class="text-end">Số tiền</th>
                        <th>Ghi chú</th>
                      </tr>
                    </thead>

                    <tbody>
                      <tr v-for="payment in selectedCustomerDebt?.payments || []" :key="payment.id">
                        <td>{{ formatDate(payment.paymentDate) }}</td>
                        <td>{{ payment.paymentMethod || '—' }}</td>
                        <td class="text-end font-weight-bold text-primary">
                          {{ formatCurrency(payment.amount) }}
                        </td>
                        <td>{{ payment.note || '—' }}</td>
                      </tr>

                      <tr v-if="!(selectedCustomerDebt?.payments || []).length">
                        <td colspan="4" class="text-center text-medium-emphasis py-6">
                          Chưa có giao dịch công nợ nào.
                        </td>
                      </tr>
                    </tbody>
                  </VTable>
                </div>
              </VWindowItem>
            </VWindow>
          </VCardText>

          <VCardActions class="dialog-actions">
            <VSpacer />

            <VBtn color="secondary" variant="outlined" @click="detailDialog = false">
              Đóng
            </VBtn>
          </VCardActions>
        </template>
      </VCard>
    </VDialog>
  </section>
</template>

<style scoped>
.customers-page {
  position: relative;
  isolation: isolate;
}

.customers-page::before {
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

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
}

.summary-icon--error {
  background: rgb(var(--v-theme-error));
}

.summary-icon--success {
  background: rgb(var(--v-theme-success));
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

.customers-panel,
.customer-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background:
    linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow:
    0 18px 52px rgba(15, 23, 42, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.customers-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) auto auto;
}

.customers-toolbar :deep(.v-field),
.customer-dialog :deep(.v-field) {
  border-radius: 16px;
}

.customers-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.customers-table-wrap,
.group-table-wrap,
.detail-table-wrap {
  overflow-x: auto;
}

.customers-table {
  min-inline-size: 920px;
}

.customers-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.customers-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.customer-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.customer-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.customer-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.customer-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.customer-cell strong,
.customer-cell span {
  display: block;
}

.customer-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.customer-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.debt-cell {
  color: rgba(var(--v-theme-on-surface), 0.82);
  font-weight: 950;
}

.debt-cell--danger {
  color: rgb(var(--v-theme-error));
}

.customers-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.customers-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.customers-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.customers-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.customers-pagination>div {
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

.group-form-card {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(180px, 1fr) minmax(120px, 160px) minmax(180px, 1fr) auto auto;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 20px;
  padding: 1rem;
  margin-block-end: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.compact-table {
  min-inline-size: 720px;
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  overflow: hidden;
}

.compact-table :deep(td) {
  block-size: 54px !important;
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

.detail-tabs {
  padding-inline: 0.25rem;
}

.debt-detail-box {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-block: 1rem;
}

.debt-detail-box>div {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 1rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.debt-detail-box span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.debt-detail-box strong {
  display: block;
  margin-block-start: 0.3rem;
  color: rgb(var(--v-theme-primary));
  font-size: 1.25rem;
  font-weight: 950;
  letter-spacing: -0.035em;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}@media (max-width: 1200px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .customers-toolbar,
  .group-form-card {
    grid-template-columns: 1fr 1fr;
  }

  .customers-toolbar .v-btn,
  .group-form-card .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .customers-toolbar,
  .group-form-card,
  .detail-grid,
  .debt-detail-box {
    grid-template-columns: 1fr;
  }

  .customers-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .customers-pagination>div {
    align-items: flex-start;
    flex-direction: column;
    inline-size: 100%;
  }

  .page-size-select {
    inline-size: 100%;
  }

  .dialog-head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.customers-hero {
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

.customers-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.customers-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.customers-hero__copy {
  display: none !important;
}

.customers-hero__actions,
.customers-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.customers-hero__actions .v-btn,
.customers-actions .v-btn,
.customers-hero__actions .v-btn.primary-action,
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