<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import type { ActionMenuItem } from '@/components/RetailActionMenu.vue'

import {
  type SupplierDto,
  createSupplier,
  getSuppliers,
  updateSupplier,
} from '@/services/orderSalesApi'

const router = useRouter()

const suppliers = ref<SupplierDto[]>([])
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalPages = ref(1)
const totalCount = ref(0)

const supplierDialog = ref(false)
const isEditing = ref(false)

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

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value || 0)

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const totalSuppliers = computed(() => totalCount.value)

const suppliersWithDebt = computed(() =>
  suppliers.value.filter(supplier => (supplier.debtAmount || 0) > 0).length,
)

const suppliersWithoutDebt = computed(() =>
  suppliers.value.filter(supplier => (supplier.debtAmount || 0) <= 0).length,
)

const totalDebt = computed(() =>
  suppliers.value.reduce(
    (sum, supplier) => sum + (supplier.debtAmount || 0),
    0,
  ),
)

const rangeStart = computed(() => {
  if (!suppliers.value.length)
    return 0

  return (page.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() =>
  Math.min(page.value * pageSize.value, totalCount.value),
)

const summaryCards = computed(() => [
  {
    label: 'Tổng nhà cung cấp',
    value: formatNumber(totalSuppliers.value),
    helper: `${formatNumber(suppliers.value.length)} đối tác trên trang hiện tại`,
    icon: 'ri-building-line',
    color: 'primary',
  },
  {
    label: 'Có công nợ',
    value: formatNumber(suppliersWithDebt.value),
    helper: 'Đối tác cần thanh toán hóa đơn',
    icon: 'ri-user-shared-line',
    color: 'warning',
  },
  {
    label: 'Không còn nợ',
    value: formatNumber(suppliersWithoutDebt.value),
    helper: 'Đối tác đã cân bằng công nợ',
    icon: 'ri-checkbox-circle-line',
    color: 'success',
  },
  {
    label: 'Công nợ phải trả',
    value: formatCurrency(totalDebt.value),
    helper: 'Tổng phải trả trên trang hiện tại',
    icon: 'ri-money-dollar-circle-line',
    color: 'error',
  },
])

const loadSuppliers = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getSuppliers({
      search: search.value.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value,
    })

    suppliers.value = result.items
    totalPages.value = Math.max(1, result.totalPages)
    totalCount.value = result.totalCount
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Không thể tải danh sách nhà cung cấp.',
    )
  }
  finally {
    loading.value = false
  }
}

const resetAndLoad = () => {
  if (page.value === 1)
    void loadSuppliers()
  else
    page.value = 1
}

const clearFilters = () => {
  search.value = ''
  resetAndLoad()
}

const resetSupplierForm = () => {
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
}

const openAddSupplier = () => {
  isEditing.value = false
  resetSupplierForm()
  supplierDialog.value = true
}

const openEditSupplier = (supplier: SupplierDto) => {
  isEditing.value = true

  supplierForm.value = {
    id: supplier.id,
    code: supplier.code || '',
    name: supplier.name || '',
    contactPerson: supplier.contactPerson || '',
    contactPhone: supplier.contactPhone || '',
    contactEmail: supplier.contactEmail || '',
    address: supplier.address || '',
    taxCode: supplier.taxCode || '',
    note: supplier.note || '',
  }

  supplierDialog.value = true
}

const validateSupplierForm = () => {
  if (!supplierForm.value.name.trim())
    return 'Vui lòng nhập tên nhà cung cấp.'

  return ''
}

const handleSaveSupplier = async () => {
  const validationMessage = validateSupplierForm()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const payload = {
      code: supplierForm.value.code.trim() || undefined,
      name: supplierForm.value.name.trim(),
      contactPerson: supplierForm.value.contactPerson.trim() || null,
      contactPhone: supplierForm.value.contactPhone.trim() || null,
      contactEmail: supplierForm.value.contactEmail.trim() || null,
      address: supplierForm.value.address.trim() || null,
      taxCode: supplierForm.value.taxCode.trim() || null,
      note: supplierForm.value.note.trim() || null,
    } as Parameters<typeof createSupplier>[0]

    if (isEditing.value) {
      await updateSupplier(
        supplierForm.value.id,
        payload as Parameters<typeof updateSupplier>[1],
      )

      successMessage.value = 'Cập nhật nhà cung cấp thành công.'
    }
    else {
      await createSupplier(payload)

      successMessage.value = 'Thêm nhà cung cấp mới thành công.'
    }

    supplierDialog.value = false

    await loadSuppliers()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Lỗi khi lưu thông tin nhà cung cấp.',
    )
  }
  finally {
    loading.value = false
  }
}

const goToSupplierPayment = async (supplier: SupplierDto) => {
  await router.push({
    path: '/supplier-payments',
    query: {
      supplierId: supplier.id,
    },
  })
}

const supplierActions = (supplier: SupplierDto): ActionMenuItem[] => [
  {
    label: 'Chỉnh sửa',
    icon: 'ri-edit-line',
    color: 'primary',
    handler: () => openEditSupplier(supplier),
  },
  {
    label: 'Thanh toán nợ',
    icon: 'ri-money-dollar-circle-line',
    color: 'success',
    show: (supplier.debtAmount || 0) > 0,
    handler: () => {
      void goToSupplierPayment(supplier)
    },
  },
]

watch(page, () => {
  void loadSuppliers()
})

watch(pageSize, () => {
  resetAndLoad()
})

onMounted(() => {
  void loadSuppliers()
})
</script>
<template>
  <section class="supplier-page">
    <div class="supplier-hero">
      <div class="supplier-hero__title-area">
        <h1>Nhà cung cấp</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-building-4-line" class="ml-2">
          Nhà cung cấp
        </VChip>
      </div>
      <div class="supplier-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
          @click="loadSuppliers"> Tải lại </VBtn>
        <VBtn color="primary" prepend-icon="ri-building-4-line" class="primary-action" @click="openAddSupplier"> Thêm
          nhà cung cấp </VBtn>
      </div>
    </div>
    <VAlert v-if="errorMessage" type="error" variant="tonal" class="mb-4" closable @click:close="errorMessage = ''"> {{
      errorMessage }} </VAlert>
    <VAlert v-if="successMessage" type="success" variant="tonal" class="mb-4" closable
      @click:close="successMessage = ''"> {{ successMessage }} </VAlert>
    <div class="summary-grid">
      <article v-for="card in summaryCards" :key="card.label" class="summary-card">
        <div class="summary-icon" :class="`summary-icon--${card.color}`">
          <VIcon :icon="card.icon" />
        </div>
        <div> <span>{{ card.label }}</span> <strong>{{ card.value }}</strong>
          <p>{{ card.helper }}</p>
        </div>
      </article>
    </div>
    <VCard class="supplier-panel">
      <VCardText>
        <div class="supplier-toolbar">
          <VTextField v-model="search" prepend-inner-icon="ri-search-2-line" label="Tìm nhà cung cấp"
            placeholder="Tên, mã nhà cung cấp, số điện thoại..." density="comfortable" hide-details clearable
            @keyup.enter="resetAndLoad" @click:clear="search = ''; resetAndLoad()" />
          <VBtn color="primary" variant="tonal" prepend-icon="ri-search-line" :loading="loading" @click="resetAndLoad">
            Tìm kiếm </VBtn>
          <VBtn color="secondary" variant="outlined" prepend-icon="ri-filter-off-line" @click="clearFilters"> Xóa lọc
          </VBtn>
        </div>
      </VCardText>
      <VDivider />
      <VCardText v-if="loading">
        <VSkeletonLoader type="table-heading, table-tbody" />
      </VCardText> <template v-else>
        <div v-if="suppliers.length" class="supplier-table-wrap">
          <VTable class="supplier-table">
            <thead>
              <tr>
                <th>Mã NCC</th>
                <th>Nhà cung cấp</th>
                <th>Người liên hệ</th>
                <th>Điện thoại</th>
                <th>Email</th>
                <th>Địa chỉ</th>
                <th class="text-end">Công nợ phải trả</th>
                <th class="text-center">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="supplier in suppliers" :key="supplier.id" class="supplier-row"
                @click="openEditSupplier(supplier)">
                <td>
                  <div class="supplier-code"> {{ supplier.code }} </div>
                </td>
                <td>
                  <div class="supplier-name-cell">
                    <VAvatar size="36" color="primary" variant="tonal">
                      <VIcon icon="ri-building-line" />
                    </VAvatar>
                    <div> <strong>{{ supplier.name }}</strong> <span>{{ supplier.taxCode || 'Chưa có MST' }}</span>
                    </div>
                  </div>
                </td>
                <td>{{ supplier.contactPerson || '—' }}</td>
                <td>{{ supplier.contactPhone || '—' }}</td>
                <td>{{ supplier.contactEmail || '—' }}</td>
                <td>
                  <div class="address-cell" :title="supplier.address || ''"> {{ supplier.address || '—' }} </div>
                </td>
                <td class="text-end debt-cell" :class="(supplier.debtAmount || 0) > 0 ? 'text-error' : 'text-success'">
                  {{ formatCurrency(supplier.debtAmount || 0) }} </td>
                <td class="text-center" @click.stop>
                  <RetailActionMenu :items="supplierActions(supplier)" />
                </td>
              </tr>
            </tbody>
          </VTable>
        </div>
        <div v-else class="supplier-empty">
          <VIcon icon="ri-building-line" size="42" color="primary" /> <strong>Chưa có dữ liệu nhà cung cấp</strong>
          <span>Thêm nhà cung cấp mới để quản lý thông tin đối tác và công nợ.</span>
          <VBtn color="primary" prepend-icon="ri-building-4-line" @click="openAddSupplier"> Thêm nhà cung cấp </VBtn>
        </div>
      </template>
      <div v-if="suppliers.length || totalCount > 0" class="supplier-pagination"> <span> Hiển thị {{
        formatNumber(rangeStart)
          }}–{{ formatNumber(rangeEnd) }} trên tổng số {{ formatNumber(totalCount) }} nhà cung cấp </span>
        <div>
          <VSelect v-model="pageSize" :items="[10, 20, 50, 100]" label="Số dòng" density="compact" hide-details
            variant="outlined" class="page-size-select" />
          <VPagination v-model="page" :length="totalPages" :total-visible="5" size="small" />
        </div>
      </div>
    </VCard>
    <VDialog v-model="supplierDialog" max-width="680" persistent>
      <VCard class="supplier-dialog">
        <div class="dialog-head">
          <div> <span>{{ isEditing ? 'Cập nhật' : 'Thêm mới' }}</span>
            <h2>{{ isEditing ? 'Cập nhật nhà cung cấp' : 'Thêm nhà cung cấp mới' }}</h2>
          </div>
          <VChip color="primary" variant="tonal"> {{ supplierForm.code || 'Tự sinh mã' }} </VChip>
        </div>
        <VCardText>
          <div class="supplier-form-grid">
            <VTextField v-model="supplierForm.code" label="Mã nhà cung cấp" placeholder="Bỏ trống để tự sinh"
              density="comfortable" />
            <VTextField v-model="supplierForm.taxCode" label="Mã số thuế" density="comfortable" />
            <VTextField v-model="supplierForm.name" label="Tên nhà cung cấp *" density="comfortable" required
              class="span-2" />
            <VTextField v-model="supplierForm.contactPerson" label="Người đại diện liên hệ" density="comfortable" />
            <VTextField v-model="supplierForm.contactPhone" label="Số điện thoại liên hệ" density="comfortable" />
            <VTextField v-model="supplierForm.contactEmail" label="Email liên hệ" type="email" density="comfortable"
              class="span-2" />
            <VTextField v-model="supplierForm.address" label="Địa chỉ" density="comfortable" class="span-2" />
            <VTextarea v-model="supplierForm.note" label="Ghi chú / Nhóm mặt hàng" rows="3" density="comfortable"
              class="span-2" />
          </div>
        </VCardText>
        <VCardActions class="dialog-actions">
          <VSpacer />
          <VBtn color="secondary" variant="text" :disabled="loading" @click="supplierDialog = false"> Hủy </VBtn>
          <VBtn color="primary" :loading="loading" @click="handleSaveSupplier"> Lưu nhà cung cấp </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>
<style scoped>
.supplier-page {
  position: relative;
  isolation: isolate;
}

.supplier-page::before {
  content: '';
  position: absolute;
  inset: -2rem -2rem auto;
  z-index: -1;
  block-size: 320px;
  border-radius: 0 0 44px 44px;
  background: radial-gradient(circle at 16% 12%, rgba(var(--v-theme-primary), 0.17), transparent 34%), radial-gradient(circle at 86% 4%, rgba(var(--v-theme-info), 0.14), transparent 32%), linear-gradient(135deg, rgba(var(--v-theme-primary), 0.08), transparent 58%);
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

.summary-card,
.supplier-panel,
.supplier-dialog {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 24px !important;
  background: linear-gradient(145deg, rgba(var(--v-theme-surface), 0.98), rgba(var(--v-theme-surface), 0.88));
  box-shadow: 0 18px 52px rgba(15, 23, 42, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.summary-card {
  display: flex;
  align-items: flex-start;
  gap: 0.85rem;
  padding: 1rem;
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

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--error {
  background: rgb(var(--v-theme-error));
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
  font-size: 1.18rem;
  font-weight: 900;
  letter-spacing: -0.035em;
  margin-block: 0.2rem;
}

.summary-card p {
  margin: 0;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.82rem;
}

.supplier-toolbar {
  display: grid;
  align-items: center;
  gap: 0.85rem;
  grid-template-columns: minmax(260px, 1fr) auto auto;
}

.supplier-toolbar :deep(.v-field),
.supplier-dialog :deep(.v-field) {
  border-radius: 16px;
}

.supplier-toolbar .v-btn {
  border-radius: 14px;
  font-weight: 800;
}

.supplier-table-wrap {
  overflow-x: auto;
}

.supplier-table {
  min-inline-size: 1080px;
}

.supplier-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.supplier-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.supplier-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.supplier-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.supplier-code {
  color: rgb(var(--v-theme-primary));
  font-weight: 900;
}

.supplier-name-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
}

.supplier-name-cell strong,
.supplier-name-cell span {
  display: block;
}

.supplier-name-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.supplier-name-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.address-cell {
  display: -webkit-box;
  max-inline-size: 260px;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.62);
  line-height: 1.35;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.debt-cell {
  font-weight: 950;
  white-space: nowrap;
}

.supplier-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 280px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.supplier-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.supplier-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
  padding: 0.9rem 1.2rem;
}

.supplier-pagination>span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.86rem;
  font-weight: 700;
}

.supplier-pagination>div {
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

.supplier-form-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.span-2 {
  grid-column: span 2;
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

  .supplier-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .supplier-toolbar .v-btn {
    inline-size: 100%;
  }
}@media (max-width: 760px) {

  .summary-grid,
  .supplier-toolbar,
  .supplier-form-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: span 1;
  }

  .supplier-pagination {
    align-items: flex-start;
    flex-direction: column;
  }

  .supplier-pagination>div {
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
.supplier-hero {
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

.supplier-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.supplier-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.supplier-hero__copy {
  display: none !important;
}

.supplier-hero__actions,
.supplier-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.supplier-hero__actions .v-btn,
.supplier-actions .v-btn,
.supplier-hero__actions .v-btn.primary-action,
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
