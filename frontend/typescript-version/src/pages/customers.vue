<script setup lang="ts">
import {
  type CustomerDto,
  type CustomerGroupDto,
  type CustomerDetailDto,
  type CustomerDebtDto,
  getCustomers,
  getCustomerDetails,
  createCustomer,
  updateCustomer,
  getCustomerDebt,
  getCustomerGroups,
  createCustomerGroup,
  updateCustomerGroup,
  deleteCustomerGroup,
} from '@/services/orderSalesApi'

const customers = ref<CustomerDto[]>([])
const customerGroups = ref<CustomerGroupDto[]>([])
const loading = ref(false)
const errorMessage = ref('')

// Dialog states
const customerDialog = ref(false)
const groupDialog = ref(false)
const detailDialog = ref(false)
const isEditingCustomer = ref(false)

// Detail states
const selectedCustomerDetail = ref<CustomerDetailDto | null>(null)
const selectedCustomerDebt = ref<CustomerDebtDto | null>(null)
const loadingDetails = ref(false)

// Forms
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
  }).format(value)

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

const loadCustomers = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getCustomers({ pageSize: 100 })
    customers.value = result.items
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

const loadCustomerGroups = async () => {
  try {
    customerGroups.value = await getCustomerGroups()
  } catch (error) {
    console.error('Không thể tải nhóm khách hàng:', error)
  }
}

const openAddCustomer = () => {
  isEditingCustomer.value = false
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
  if (!customerForm.value.fullName) {
    errorMessage.value = 'Vui lòng nhập Tên khách hàng.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    const payload = {
      code: customerForm.value.code || undefined,
      fullName: customerForm.value.fullName,
      phone: customerForm.value.phone || null,
      email: customerForm.value.email || null,
      address: customerForm.value.address || null,
      taxCode: customerForm.value.taxCode || null,
      customerGroupId: customerForm.value.customerGroupId || null,
    }

    if (isEditingCustomer.value) {
      await updateCustomer(customerForm.value.id, payload)
    } else {
      await createCustomer(payload)
    }

    customerDialog.value = false
    await loadCustomers()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu thông tin khách hàng.'
  } finally {
    loading.value = false
  }
}

const openCustomerDetails = async (customer: CustomerDto) => {
  loadingDetails.value = true
  selectedCustomerDetail.value = null
  selectedCustomerDebt.value = null
  detailDialog.value = true
  try {
    selectedCustomerDetail.value = await getCustomerDetails(customer.id)
    selectedCustomerDebt.value = await getCustomerDebt(customer.id)
  } catch (error: any) {
    console.error('Không thể load chi tiết khách hàng:', error)
  } finally {
    loadingDetails.value = false
  }
}

const handleAddGroup = async () => {
  if (!groupForm.value.name) return
  loading.value = true
  try {
    await createCustomerGroup({
      name: groupForm.value.name,
      description: groupForm.value.description || null,
      discountPercent: Number(groupForm.value.discountPercent) || 0,
    })
    groupForm.value.name = ''
    groupForm.value.description = ''
    groupForm.value.discountPercent = 0
    await loadCustomerGroups()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi tạo nhóm khách hàng.'
  } finally {
    loading.value = false
  }
}

const handleDeleteGroup = async (id: string) => {
  if (!confirm('Bạn có chắc chắn muốn xóa nhóm khách hàng này?')) return
  loading.value = true
  try {
    await deleteCustomerGroup(id)
    await loadCustomerGroups()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi xóa nhóm khách hàng.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadCustomerGroups()
  await loadCustomers()
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Khách hàng"
    title="Khách hàng"
    subtitle="Quản lý thông tin khách hàng, hạng thành viên, công nợ và lịch sử mua hàng."
  >
    <template #actions>
      <div class="d-flex gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="loadCustomers"
        >
          Tải lại
        </VBtn>
        <VBtn
          variant="tonal"
          prepend-icon="ri-group-line"
          @click="groupDialog = true"
        >
          Quản lý nhóm
        </VBtn>
        <VBtn
          prepend-icon="ri-user-add-line"
          @click="openAddCustomer"
        >
          Thêm khách hàng
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

  <VCard>
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
          <th>Mã KH</th>
          <th>Tên khách</th>
          <th>Điện thoại</th>
          <th>Nhóm</th>
          <th class="text-end">
            Công nợ
          </th>
          <th>Trạng thái</th>
          <th class="text-center">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="customer in customers"
          :key="customer.id"
        >
          <td class="font-weight-bold">
            {{ customer.code }}
          </td>
          <td>
            <a
              href="javascript:void(0)"
              class="text-primary font-weight-bold"
              @click="openCustomerDetails(customer)"
            >
              {{ customer.fullName }}
            </a>
          </td>
          <td>{{ customer.phone || '—' }}</td>
          <td>{{ customer.customerGroupName || '—' }}</td>
          <td class="text-end font-weight-bold">
            {{ formatCurrency(customer.debtAmount) }}
          </td>
          <td>
            <RetailStatusChip :status="customer.isActive ? 'Đang hoạt động' : 'Tạm dừng'" />
          </td>
          <td class="text-center">
            <VBtn
              icon="ri-edit-line"
              variant="text"
              size="small"
              color="primary"
              @click="openEditCustomer(customer)"
            />
          </td>
        </tr>
        <tr v-if="!loading && !customers.length">
          <td
            colspan="7"
            class="text-center text-medium-emphasis py-8"
          >
            Chưa có dữ liệu khách hàng.
          </td>
        </tr>
      </tbody>
    </VTable>
  </VCard>

  <!-- Add/Edit Customer Dialog -->
  <VDialog
    v-model="customerDialog"
    max-width="600"
  >
    <VCard>
      <VCardTitle>{{ isEditingCustomer ? 'Cập nhật khách hàng' : 'Thêm khách hàng mới' }}</VCardTitle>
      <VCardText>
        <VRow>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="customerForm.code"
              label="Mã khách hàng (Bỏ trống để tự sinh)"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VSelect
              v-model="customerForm.customerGroupId"
              label="Nhóm khách hàng"
              :items="customerGroups"
              item-title="name"
              item-value="id"
              clearable
            />
          </VCol>
          <VCol cols="12">
            <VTextField
              v-model="customerForm.fullName"
              label="Tên khách hàng *"
              required
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="customerForm.phone"
              label="Số điện thoại"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="customerForm.email"
              label="Email"
              type="email"
            />
          </VCol>
          <VCol cols="12" sm="8">
            <VTextField
              v-model="customerForm.address"
              label="Địa chỉ"
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VTextField
              v-model="customerForm.taxCode"
              label="Mã số thuế"
            />
          </VCol>
        </VRow>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="customerDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSaveCustomer"
        >
          Lưu
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Customer Group Management Dialog -->
  <VDialog
    v-model="groupDialog"
    max-width="700"
  >
    <VCard>
      <VCardTitle>Quản lý nhóm khách hàng</VCardTitle>
      <VCardText>
        <VRow class="mb-4">
          <VCol cols="12" sm="5">
            <VTextField
              v-model="groupForm.name"
              label="Tên nhóm mới *"
              placeholder="VD: VIP"
            />
          </VCol>
          <VCol cols="12" sm="4">
            <VTextField
              v-model.number="groupForm.discountPercent"
              type="number"
              label="Chiết khấu (%)"
              suffix="%"
            />
          </VCol>
          <VCol cols="12" sm="3" class="d-flex align-center">
            <VBtn
              color="primary"
              block
              @click="handleAddGroup"
            >
              Thêm nhóm
            </VBtn>
          </VCol>
        </VRow>

        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Tên nhóm</th>
              <th class="text-end">Chiết khấu (%)</th>
              <th class="text-center">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="grp in customerGroups"
              :key="grp.id"
            >
              <td class="font-weight-bold">{{ grp.name }}</td>
              <td class="text-end font-weight-bold text-success">{{ grp.discountPercent }}%</td>
              <td class="text-center">
                <VBtn
                  icon="ri-delete-bin-line"
                  variant="text"
                  size="small"
                  color="error"
                  @click="handleDeleteGroup(grp.id)"
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
          @click="groupDialog = false"
        >
          Đóng
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>

  <!-- Customer Details & History Dialog -->
  <VDialog
    v-model="detailDialog"
    max-width="900"
  >
    <VCard>
      <VCardTitle class="d-flex justify-between align-center">
        <span>Chi tiết khách hàng: {{ selectedCustomerDetail?.fullName }}</span>
        <VChip color="primary" size="small">{{ selectedCustomerDetail?.customerGroupName || 'Vãng lai' }}</VChip>
      </VCardTitle>
      <VCardText v-if="loadingDetails" class="text-center py-8">
        <VProgressCircular indeterminate color="primary" />
        <div class="mt-2 text-medium-emphasis">Đang tải lịch sử khách hàng...</div>
      </VCardText>
      <VCardText v-else-if="selectedCustomerDetail">
        <VRow class="mb-4">
          <VCol cols="12" md="6">
            <div><strong>Điện thoại:</strong> {{ selectedCustomerDetail.phone || '—' }}</div>
            <div><strong>Email:</strong> {{ selectedCustomerDetail.email || '—' }}</div>
            <div><strong>Địa chỉ:</strong> {{ selectedCustomerDetail.address || '—' }}</div>
          </VCol>
          <VCol cols="12" md="6" class="text-right">
            <div><strong>Tổng mua hàng:</strong> <span class="text-primary font-weight-bold">{{ formatCurrency(selectedCustomerDetail.totalPurchased) }}</span></div>
            <div><strong>Công nợ hiện tại:</strong> <span class="text-error font-weight-bold">{{ formatCurrency(selectedCustomerDetail.debtAmount) }}</span></div>
            <div><strong>Ngày tạo tài khoản:</strong> {{ formatDate(selectedCustomerDetail.createdAt) }}</div>
          </VCol>
        </VRow>

        <VTabs class="mb-4">
          <VTab>Lịch sử đơn hàng ({{ selectedCustomerDetail.orders?.length || 0 }})</VTab>
          <VTab>Chi tiết công nợ ({{ selectedCustomerDebt?.payments?.length || 0 }} giao dịch)</VTab>
        </VTabs>

        <VWindow>
          <!-- Order History -->
          <VWindowItem>
            <VTable class="retail-table">
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
                <tr
                  v-for="order in selectedCustomerDetail.orders"
                  :key="order.id"
                >
                  <td class="font-weight-bold text-primary">{{ order.orderCode }}</td>
                  <td>{{ formatDate(order.createdAt) }}</td>
                  <td class="text-end">{{ formatCurrency(order.finalAmount) }}</td>
                  <td class="text-end">{{ formatCurrency(order.paidAmount) }}</td>
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
          </VWindowItem>
        </VWindow>
      </VCardText>
      <VCardActions>
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
