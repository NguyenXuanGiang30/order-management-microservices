<script setup lang="ts">
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
          <th>Mã NCC</th>
          <th>Nhà cung cấp</th>
          <th>Người liên hệ</th>
          <th>Điện thoại</th>
          <th>Email</th>
          <th>Địa chỉ</th>
          <th class="text-end">
            Công nợ phải trả
          </th>
          <th class="text-center">Thao tác</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="supplier in suppliers"
          :key="supplier.id"
        >
          <td class="font-weight-bold">
            {{ supplier.code }}
          </td>
          <td>{{ supplier.name }}</td>
          <td>{{ supplier.contactPerson || '—' }}</td>
          <td>{{ supplier.contactPhone || '—' }}</td>
          <td>{{ supplier.contactEmail || '—' }}</td>
          <td>{{ supplier.address || '—' }}</td>
          <td class="text-end font-weight-bold text-error">
            {{ formatCurrency(supplier.debtAmount) }}
          </td>
          <td class="text-center">
            <VBtn
              icon="ri-edit-line"
              variant="text"
              size="small"
              color="primary"
              @click="openEditSupplier(supplier)"
            />
          </td>
        </tr>
        <tr v-if="!loading && !suppliers.length">
          <td
            colspan="8"
            class="text-center text-medium-emphasis py-8"
          >
            Chưa có dữ liệu nhà cung cấp.
          </td>
        </tr>
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
