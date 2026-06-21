<script setup lang="ts">
import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'
import {
  type SupplierDto,
  getSuppliers,
  createSupplier,
  updateSupplier,
} from '@/services/orderSalesApi'

const suppliers = ref<SupplierDto[]>([])
const loading = ref(false)
const errorMessage = ref('')

// Dialog States
const supplierDialog = ref(false)
const isEditing = ref(false)

// Form
const supplierForm = ref({
  id: '',
  code: '',
  name: '',
  contactPerson: '',
  contactPhone: '',
  contactEmail: '',
  address: '',
  taxCode: '',
  note: '',
})

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)

const loadSuppliers = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getSuppliers({ pageSize: 100 })
    suppliers.value = result.items
  }
  catch (error) {
    errorMessage.value = error instanceof Error
      ? error.message
      : 'Không thể tải danh sách nhà cung cấp.'
  }
  finally {
    loading.value = false
  }
}

const openAddSupplier = () => {
  isEditing.value = false
  supplierForm.value = {
    id: '',
    code: '',
    name: '',
    contactPerson: '',
    contactPhone: '',
    contactEmail: '',
    address: '',
    taxCode: '',
    note: '',
  }
  supplierDialog.value = true
}

const openEditSupplier = (supplier: SupplierDto) => {
  isEditing.value = true
  supplierForm.value = {
    id: supplier.id,
    code: supplier.code,
    name: supplier.name,
    contactPerson: supplier.contactPerson || '',
    contactPhone: supplier.contactPhone || '',
    contactEmail: supplier.contactEmail || '',
    address: supplier.address || '',
    taxCode: supplier.taxCode || '',
    note: supplier.note || '',
  }
  supplierDialog.value = true
}

const handleSaveSupplier = async () => {
  if (!supplierForm.value.name) {
    errorMessage.value = 'Vui lòng điền Tên nhà cung cấp.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    const payload = {
      code: supplierForm.value.code || undefined,
      name: supplierForm.value.name,
      contactPerson: supplierForm.value.contactPerson || null,
      contactPhone: supplierForm.value.contactPhone || null,
      contactEmail: supplierForm.value.contactEmail || null,
      address: supplierForm.value.address || null,
      taxCode: supplierForm.value.taxCode || null,
      note: supplierForm.value.note || null,
    }

    if (isEditing.value) {
      await updateSupplier(supplierForm.value.id, payload)
    } else {
      await createSupplier(payload)
    }

    supplierDialog.value = false
    await loadSuppliers()
  } catch (error: any) {
    errorMessage.value = error.message || 'Lỗi khi lưu thông tin nhà cung cấp.'
  } finally {
    loading.value = false
  }
}

const search = ref('')

const totalSuppliers = computed(() => suppliers.value.length)
const suppliersWithDebt = computed(() => suppliers.value.filter(s => s.debtAmount > 0).length)
const totalDebt = computed(() => suppliers.value.reduce((sum, s) => sum + s.debtAmount, 0))

const filteredSuppliers = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return suppliers.value
  return suppliers.value.filter(s =>
    s.name.toLowerCase().includes(q) ||
    s.code.toLowerCase().includes(q) ||
    (s.contactPhone && s.contactPhone.includes(q)) ||
    (s.contactPerson && s.contactPerson.toLowerCase().includes(q)),
  )
})

const supplierActions = (supplier: SupplierDto): ActionMenuItem[] => [
  { label: 'Chỉnh sửa', icon: 'ri-edit-line', color: 'primary', handler: () => openEditSupplier(supplier) },
]

onMounted(loadSuppliers)
</script>

<template>
  <RetailPageHeader
    eyebrow="Nhà cung cấp"
    title="Nhà cung cấp"
    subtitle="Quản lý nhà cung cấp, nhóm hàng, công nợ phải trả và trạng thái hợp tác."
  >
    <template #actions>
      <div class="d-flex gap-3">
        <VBtn
          variant="tonal"
          prepend-icon="ri-refresh-line"
          :loading="loading"
          @click="loadSuppliers"
        >
          Tải lại
        </VBtn>
        <VBtn
          prepend-icon="ri-building-4-line"
          @click="openAddSupplier"
        >
          Thêm nhà cung cấp
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
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Tổng nhà cung cấp', value: String(totalSuppliers), helper: 'Đối tác đã lưu', icon: 'ri-building-line', color: 'primary' }" />
    </VCol>
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Đối tác có công nợ', value: String(suppliersWithDebt), helper: 'Cần thanh toán hóa đơn', icon: 'ri-user-shared-line', color: 'warning' }" />
    </VCol>
    <VCol cols="12" md="4">
      <RetailMetricCard :metric="{ label: 'Tổng công nợ phải trả', value: formatCurrency(totalDebt), helper: 'Phải trả nhà cung cấp', icon: 'ri-money-dollar-circle-line', color: 'error' }" />
    </VCol>
  </VRow>

  <VCard class="retail-panel-card">
    <RetailFilterBar
      v-model="search"
      search-placeholder="Tên, mã nhà cung cấp, sđt..."
      :loading="loading"
      @search="loadSuppliers"
      @reload="loadSuppliers"
    >
      <template #actions>
        <VBtn
          prepend-icon="ri-building-4-line"
          @click="openAddSupplier"
        >
          Thêm nhà cung cấp
        </VBtn>
      </template>
    </RetailFilterBar>

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
          <th>Mã NCC</th>
          <th>Nhà cung cấp</th>
          <th>Người liên hệ</th>
          <th>Điện thoại</th>
          <th>Email</th>
          <th>Địa chỉ</th>
          <th class="text-end">Công nợ phải trả</th>
          <th class="text-center" style="width: 60px;">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="supplier in filteredSuppliers"
          :key="supplier.id"
          class="hover-row"
          @click="openEditSupplier(supplier)"
        >
          <td class="font-weight-bold">
            {{ supplier.code }}
          </td>
          <td class="font-weight-bold text-primary">{{ supplier.name }}</td>
          <td>{{ supplier.contactPerson || '—' }}</td>
          <td>{{ supplier.contactPhone || '—' }}</td>
          <td>{{ supplier.contactEmail || '—' }}</td>
          <td>{{ supplier.address || '—' }}</td>
          <td class="text-end font-weight-bold text-error">
            {{ formatCurrency(supplier.debtAmount) }}
          </td>
          <td
            class="text-center"
            @click.stop
          >
            <RetailActionMenu :items="supplierActions(supplier)" />
          </td>
        </tr>
        <RetailEmptyState
          v-if="!loading && !filteredSuppliers.length"
          :colspan="8"
          icon="ri-building-line"
          title="Chưa có dữ liệu nhà cung cấp"
          subtitle="Thêm nhà cung cấp mới để quản lý."
          action-label="Thêm nhà cung cấp"
        />
      </tbody>
    </VTable>
  </VCard>

  <!-- Add/Edit Supplier Dialog -->
  <VDialog
    v-model="supplierDialog"
    max-width="600"
  >
    <VCard>
      <VCardTitle>{{ isEditing ? 'Cập nhật nhà cung cấp' : 'Thêm nhà cung cấp mới' }}</VCardTitle>
      <VCardText>
        <VRow>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="supplierForm.code"
              label="Mã nhà cung cấp (Bỏ trống để tự sinh)"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="supplierForm.taxCode"
              label="Mã số thuế"
            />
          </VCol>
          <VCol cols="12">
            <VTextField
              v-model="supplierForm.name"
              label="Tên nhà cung cấp *"
              required
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="supplierForm.contactPerson"
              label="Người đại diện liên hệ"
            />
          </VCol>
          <VCol cols="12" sm="6">
            <VTextField
              v-model="supplierForm.contactPhone"
              label="Số điện thoại liên hệ"
            />
          </VCol>
          <VCol cols="12" sm="12">
            <VTextField
              v-model="supplierForm.contactEmail"
              label="Email liên hệ"
              type="email"
            />
          </VCol>
          <VCol cols="12">
            <VTextField
              v-model="supplierForm.address"
              label="Địa chỉ"
            />
          </VCol>
          <VCol cols="12">
            <VTextarea
              v-model="supplierForm.note"
              label="Ghi chú / Nhóm mặt hàng"
              rows="3"
            />
          </VCol>
        </VRow>
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="supplierDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSaveSupplier"
        >
          Lưu
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
