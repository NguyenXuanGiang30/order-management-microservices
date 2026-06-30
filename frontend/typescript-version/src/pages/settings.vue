<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import { apiClient } from '@/services/apiClient'
import {
  type ActivityLogDto,
  type BackupRecordDto,
  type UserDto,
  changePassword,
  createBackup,
  createUser,
  getActivityLogs,
  getBackups,
  getPermissions,
  getRolePermissions,
  getUsers,
  restoreBackup,
  toggleUserActive,
  updateRolePermissions,
  updateUser,
} from '@/services/adminApi'

interface PermissionItem {
  code: string
  name: string
  description?: string | null
  group?: string | null
}

type RoleCode = 'Admin' | 'Sales' | 'Warehouse'

const activeTab = ref(0)
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const usersList = ref<UserDto[]>([])
const userDialog = ref(false)
const editingUser = ref<UserDto | null>(null)

const userForm = ref({
  username: '',
  fullName: '',
  email: '',
  phone: '',
  role: 'Sales' as RoleCode,
})

const selectedRole = ref<RoleCode>('Sales')
const roles: RoleCode[] = ['Admin', 'Sales', 'Warehouse']

const rolePermissions = ref<string[]>([])
const allPermissions = ref<PermissionItem[]>([])

const activityLogs = ref<ActivityLogDto[]>([])
const selectedLog = ref<ActivityLogDto | null>(null)
const logDialog = ref(false)

const backupsList = ref<BackupRecordDto[]>([])
const backupNote = ref('')

const oldPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const securityLoading = ref(false)

const seedingOrders = ref(false)
const seedingInventory = ref(false)

const tabItems = [
  {
    value: 0,
    title: 'Tài khoản & Phân quyền',
    icon: 'ri-user-settings-line',
  },
  {
    value: 1,
    title: 'Nhật ký hoạt động',
    icon: 'ri-history-line',
  },
  {
    value: 2,
    title: 'Sao lưu & Khôi phục',
    icon: 'ri-database-2-line',
  },
  {
    value: 3,
    title: 'Bảo mật',
    icon: 'ri-lock-password-line',
  },
  {
    value: 4,
    title: 'Dữ liệu mẫu',
    icon: 'ri-seedling-line',
  },
]

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0)

const formatDate = (dateStr: string) => {
  if (!dateStr)
    return '—'

  const date = new Date(dateStr)

  return Number.isNaN(date.getTime())
    ? '—'
    : date.toLocaleString('vi-VN')
}

const roleLabel = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'Quản trị',
    Sales: 'Bán hàng',
    Warehouse: 'Kho hàng',
  }

  return map[role] ?? role
}

const roleColor = (role: string) => {
  const map: Record<string, string> = {
    Admin: 'primary',
    Sales: 'success',
    Warehouse: 'warning',
  }

  return map[role] ?? 'secondary'
}

const severityText = (severity: string) => {
  if (severity === 'Error')
    return 'Nghiêm trọng'

  if (severity === 'Warning')
    return 'Cần theo dõi'

  return 'Bình thường'
}

const severityColor = (severity: string) => {
  if (severity === 'Error')
    return 'error'

  if (severity === 'Warning')
    return 'warning'

  return 'success'
}

const activeUsersCount = computed(() =>
  usersList.value.filter(user => user.isActive).length,
)

const inactiveUsersCount = computed(() =>
  usersList.value.filter(user => !user.isActive).length,
)

const errorLogsCount = computed(() =>
  activityLogs.value.filter(log => log.severity === 'Error').length,
)

const summaryCards = computed(() => [
  {
    label: 'Tài khoản',
    value: formatNumber(usersList.value.length),
    helper: `${formatNumber(activeUsersCount.value)} đang hoạt động · ${formatNumber(inactiveUsersCount.value)} tạm dừng`,
    icon: 'ri-user-settings-line',
    color: 'primary',
  },
  {
    label: 'Quyền hệ thống',
    value: formatNumber(allPermissions.value.length),
    helper: `${formatNumber(rolePermissions.value.length)} quyền đang cấp cho ${roleLabel(selectedRole.value)}`,
    icon: 'ri-shield-keyhole-line',
    color: 'success',
  },
  {
    label: 'Nhật ký gần đây',
    value: formatNumber(activityLogs.value.length),
    helper: `${formatNumber(errorLogsCount.value)} bản ghi nghiêm trọng`,
    icon: 'ri-history-line',
    color: 'warning',
  },
  {
    label: 'Bản sao lưu',
    value: formatNumber(backupsList.value.length),
    helper: 'Điểm khôi phục dữ liệu hệ thống',
    icon: 'ri-database-2-line',
    color: 'info',
  },
])

const groupedPermissions = computed(() => {
  const groups: Record<string, PermissionItem[]> = {}

  allPermissions.value.forEach(permission => {
    const groupName = permission.group || 'Khác'

    if (!groups[groupName])
      groups[groupName] = []

    groups[groupName].push(permission)
  })

  return groups
})

const isGroupSelected = (permissions: PermissionItem[]) =>
  permissions.length > 0
  && permissions.every(permission => rolePermissions.value.includes(permission.code))

const toggleGroupPermissions = (permissions: PermissionItem[]) => {
  const codes = permissions.map(permission => permission.code)
  const allSelected = codes.every(code => rolePermissions.value.includes(code))

  if (allSelected) {
    rolePermissions.value = rolePermissions.value.filter(code => !codes.includes(code))

    return
  }

  codes.forEach(code => {
    if (!rolePermissions.value.includes(code))
      rolePermissions.value.push(code)
  })
}

const loadUsers = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getUsers({ pageSize: 100 })

    usersList.value = result.items
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách tài khoản.')
  }
  finally {
    loading.value = false
  }
}

const loadRolePermissions = async () => {
  errorMessage.value = ''

  try {
    const [permissionsResult, rolePermissionsResult] = await Promise.all([
      getPermissions(),
      getRolePermissions(selectedRole.value),
    ])

    allPermissions.value = Array.isArray(permissionsResult)
      ? permissionsResult as PermissionItem[]
      : []

    rolePermissions.value = Array.isArray(rolePermissionsResult)
      ? rolePermissionsResult as string[]
      : rolePermissionsResult.permissions || []
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách quyền.')
  }
}

const handleSavePermissions = async () => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await updateRolePermissions(selectedRole.value, rolePermissions.value)

    successMessage.value = `Cập nhật phân quyền cho vai trò ${roleLabel(selectedRole.value)} thành công.`
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi cập nhật quyền.')
  }
  finally {
    loading.value = false
  }
}

const loadActivityLogs = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    const result = await getActivityLogs({ pageSize: 50 })

    activityLogs.value = result.items
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải nhật ký hoạt động.')
  }
  finally {
    loading.value = false
  }
}

const loadBackups = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    backupsList.value = await getBackups()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Không thể tải danh sách sao lưu.')
  }
  finally {
    loading.value = false
  }
}

const handleCreateBackup = async () => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await createBackup({
      note: backupNote.value || 'Sao lưu định kỳ',
    })

    backupNote.value = ''
    successMessage.value = 'Tạo bản sao lưu hệ thống thành công.'

    await loadBackups()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi tạo sao lưu.')
  }
  finally {
    loading.value = false
  }
}

const handleRestoreBackup = async (id: string) => {
  if (!confirm('CẢNH BÁO: Phục hồi sao lưu sẽ ghi đè toàn bộ dữ liệu hiện tại của hệ thống. Bạn có chắc chắn muốn thực hiện?'))
    return

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await restoreBackup(id, {
      confirm: true,
      note: 'Khôi phục từ quản trị',
    })

    successMessage.value = 'Khôi phục hệ thống thành công. Vui lòng tải lại trang.'
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi phục hồi dữ liệu.')
  }
  finally {
    loading.value = false
  }
}

const handleOpenCreateUser = () => {
  editingUser.value = null

  userForm.value = {
    username: '',
    fullName: '',
    email: '',
    phone: '',
    role: 'Sales',
  }

  userDialog.value = true
}

const handleOpenEditUser = (user: UserDto) => {
  editingUser.value = user

  userForm.value = {
    username: user.username,
    fullName: user.fullName,
    email: user.email || '',
    phone: user.phone || '',
    role: user.role as RoleCode,
  }

  userDialog.value = true
}

const validateUserForm = () => {
  if (!editingUser.value && !userForm.value.username.trim())
    return 'Vui lòng nhập tên đăng nhập.'

  if (!userForm.value.fullName.trim())
    return 'Vui lòng nhập họ tên nhân viên.'

  if (!userForm.value.role)
    return 'Vui lòng chọn vai trò hệ thống.'

  return ''
}

const handleSaveUser = async () => {
  const validationMessage = validateUserForm()

  if (validationMessage) {
    errorMessage.value = validationMessage

    return
  }

  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    if (editingUser.value) {
      await updateUser(editingUser.value.id, {
        fullName: userForm.value.fullName.trim(),
        email: userForm.value.email || null,
        phone: userForm.value.phone || null,
        role: userForm.value.role,
      })

      successMessage.value = 'Cập nhật tài khoản nhân viên thành công.'
    }
    else {
      await createUser({
        username: userForm.value.username.trim(),
        fullName: userForm.value.fullName.trim(),
        email: userForm.value.email || null,
        phone: userForm.value.phone || null,
        role: userForm.value.role,
      })

      successMessage.value = 'Tạo tài khoản nhân viên thành công.'
    }

    userDialog.value = false

    await loadUsers()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi lưu tài khoản.')
  }
  finally {
    loading.value = false
  }
}

const handleToggleUserStatus = async (user: UserDto) => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await toggleUserActive(user.id)

    successMessage.value = user.isActive
      ? `Đã vô hiệu hóa tài khoản ${user.username}.`
      : `Đã kích hoạt tài khoản ${user.username}.`

    await loadUsers()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi kích hoạt hoặc vô hiệu hóa tài khoản.')
  }
  finally {
    loading.value = false
  }
}

const handleChangePassword = async () => {
  if (!oldPassword.value || !newPassword.value) {
    errorMessage.value = 'Vui lòng điền đầy đủ mật khẩu cũ và mật khẩu mới.'

    return
  }

  if (newPassword.value !== confirmPassword.value) {
    errorMessage.value = 'Mật khẩu xác nhận không khớp.'

    return
  }

  securityLoading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await changePassword({
      oldPassword: oldPassword.value,
      newPassword: newPassword.value,
    })

    oldPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''

    successMessage.value = 'Đổi mật khẩu thành công.'
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi đổi mật khẩu.')
  }
  finally {
    securityLoading.value = false
  }
}

const handleSeedOrders = async () => {
  if (!confirm('CẢNH BÁO: Hành động này sẽ xóa toàn bộ đơn hàng và giao dịch bán lẻ hiện tại. Bạn có chắc muốn tiếp tục?'))
    return

  seedingOrders.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await apiClient.post('/api/orders/database/seed')

    successMessage.value = 'Nạp dữ liệu mẫu đơn hàng thành công.'
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi nạp dữ liệu mẫu đơn hàng.')
  }
  finally {
    seedingOrders.value = false
  }
}

const handleSeedInventory = async () => {
  if (!confirm('CẢNH BÁO: Hành động này sẽ xóa toàn bộ phiếu nhập và lịch sử tồn kho hiện tại. Bạn có chắc muốn tiếp tục?'))
    return

  seedingInventory.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await apiClient.post('/api/products/database/seed')

    successMessage.value = 'Nạp dữ liệu mẫu tồn kho thành công.'
  }
  catch (error) {
    errorMessage.value = getErrorMessage(error, 'Lỗi nạp dữ liệu mẫu tồn kho.')
  }
  finally {
    seedingInventory.value = false
  }
}

const openLogDetail = (log: ActivityLogDto) => {
  selectedLog.value = log
  logDialog.value = true
}

const reloadCurrentTab = () => {
  if (activeTab.value === 0) {
    void loadUsers()
    void loadRolePermissions()
  }

  if (activeTab.value === 1)
    void loadActivityLogs()

  if (activeTab.value === 2)
    void loadBackups()
}

watch(activeTab, value => {
  if (value === 0) {
    void loadUsers()
    void loadRolePermissions()
  }

  if (value === 1)
    void loadActivityLogs()

  if (value === 2)
    void loadBackups()
})

watch(selectedRole, () => {
  void loadRolePermissions()
})

onMounted(async () => {
  await Promise.all([
    loadUsers(),
    loadRolePermissions(),
    loadActivityLogs(),
    loadBackups(),
  ])
})
</script>
<template>
  <section class="admin-settings-page">
    <div class="settings-hero">
      <div class="settings-hero__title-area">
        <h1>Cài đặt & phân quyền</h1>
        <VChip color="primary" variant="tonal" size="small" prepend-icon="ri-settings-3-line" class="ml-2">
          Quản trị
        </VChip>
      </div>
      <div class="settings-hero__actions">
        <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
          @click="activeTab === 0 ? (loadUsers(), loadRolePermissions()) : activeTab === 1 ? loadActivityLogs() : activeTab === 2 ? loadBackups() : null">
          Tải lại </VBtn>
        <VBtn color="primary" prepend-icon="ri-user-add-line" class="primary-action" @click="handleOpenCreateUser"> Thêm
          nhân viên </VBtn>
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
    <VCard class="tabs-card">
      <div class="settings-tabs"> <button v-for="tab in tabItems" :key="tab.value" type="button" class="settings-tab"
          :class="{ 'settings-tab--active': activeTab === tab.value }" @click="activeTab = tab.value">
          <VIcon :icon="tab.icon" /> <span>{{ tab.title }}</span>
        </button> </div>
    </VCard>
    <VWindow v-model="activeTab">
      <VWindowItem :value="0">
        <div class="users-permissions-layout">
          <VCard class="settings-panel">
            <div class="panel-head">
              <div> <span>Tài khoản</span> <strong>Tài khoản nhân viên</strong>
                <p>Danh sách tài khoản và vai trò trong hệ thống.</p>
              </div>
              <VBtn color="primary" prepend-icon="ri-user-add-line" class="primary-action"
                @click="handleOpenCreateUser"> Thêm nhân viên </VBtn>
            </div>
            <VDivider />
            <VCardText v-if="loading">
              <VSkeletonLoader type="table-heading, table-tbody" />
            </VCardText> <template v-else>
              <div v-if="usersList.length" class="settings-table-wrap">
                <VTable class="settings-table">
                  <thead>
                    <tr>
                      <th>Tài khoản</th>
                      <th>Họ tên</th>
                      <th>Vai trò</th>
                      <th class="text-center">Trạng thái</th>
                      <th class="text-center">Hành động</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="user in usersList" :key="user.id" class="settings-row">
                      <td>
                        <div class="user-cell">
                          <VAvatar size="36" color="primary" variant="tonal">
                            <VIcon icon="ri-user-line" />
                          </VAvatar>
                          <div> <strong>{{ user.username }}</strong> <span>{{ user.email || 'Chưa có email' }}</span>
                          </div>
                        </div>
                      </td>
                      <td>{{ user.fullName }}</td>
                      <td>
                        <VChip :color="roleColor(user.role)" variant="tonal" size="small"> {{ roleLabel(user.role) }}
                        </VChip>
                      </td>
                      <td class="text-center">
                        <VSwitch :model-value="user.isActive" color="success" density="compact" hide-details
                          @update:model-value="handleToggleUserStatus(user)" />
                      </td>
                      <td class="text-center">
                        <VBtn icon="ri-edit-line" variant="text" size="small" color="primary"
                          @click="handleOpenEditUser(user)" />
                      </td>
                    </tr>
                  </tbody>
                </VTable>
              </div>
              <div v-else class="settings-empty">
                <VIcon icon="ri-user-settings-line" size="42" color="primary" /> <strong>Chưa có tài khoản nhân viên
                  nào</strong> <span>Tạo tài khoản mới để cấp quyền truy cập.</span>
                <VBtn color="primary" prepend-icon="ri-user-add-line" @click="handleOpenCreateUser"> Thêm nhân viên
                </VBtn>
              </div>
            </template>
          </VCard>
          <VCard class="settings-panel permission-panel">
            <div class="panel-head">
              <div> <span>Phân quyền</span> <strong>Chi tiết phân quyền</strong>
                <p>Quản lý chức năng cho từng nhóm vai trò.</p>
              </div>
            </div>
            <VDivider />
            <VCardText>
              <VSelect v-model="selectedRole" label="Chọn vai trò cấu hình" :items="roles" density="comfortable"
                class="mb-4" />
              <div class="permission-list">
                <div v-for="(permissions, groupName) in groupedPermissions" :key="groupName" class="permission-group">
                  <div class="permission-group__head">
                    <div> <strong>{{ groupName }}</strong> <span>{{ permissions.length }} quyền</span> </div>
                    <VBtn variant="text" density="comfortable" color="primary"
                      @click="toggleGroupPermissions(permissions)"> {{
                        isGroupSelected(permissions) ? 'Tắt nhóm' : 'Bật nhóm' }} </VBtn>
                  </div>
                  <div class="permission-items">
                    <div v-for="permission in permissions" :key="permission.code" class="permission-item">
                      <VCheckbox v-model="rolePermissions" :label="permission.name" :value="permission.code"
                        density="compact" hide-details />
                      <p>{{ permission.description || 'Không có mô tả' }}</p>
                    </div>
                  </div>
                </div>
              </div>
              <VBtn block color="primary" class="save-permission-btn" prepend-icon="ri-save-3-line" :loading="loading"
                @click="handleSavePermissions"> Lưu phân quyền </VBtn>
            </VCardText>
          </VCard>
        </div>
      </VWindowItem>
      <VWindowItem :value="1">
        <VCard class="settings-panel">
          <div class="panel-head">
            <div> <span>Audit log</span> <strong>Nhật ký hệ thống</strong>
              <p>Danh sách thao tác, sự kiện và mức độ nghiêm trọng.</p>
            </div>
            <VBtn color="secondary" variant="outlined" prepend-icon="ri-refresh-line" :loading="loading"
              @click="loadActivityLogs"> Tải lại </VBtn>
          </div>
          <VDivider />
          <VCardText v-if="loading">
            <VSkeletonLoader type="table-heading, table-tbody" />
          </VCardText> <template v-else>
            <div v-if="activityLogs.length" class="settings-table-wrap">
              <VTable class="settings-table">
                <thead>
                  <tr>
                    <th>Người dùng</th>
                    <th>Dịch vụ</th>
                    <th>Thao tác</th>
                    <th>Loại dữ liệu</th>
                    <th>Mức độ</th>
                    <th>Thời gian</th>
                    <th class="text-center">Chi tiết</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="log in activityLogs" :key="log.id" class="settings-row" @click="openLogDetail(log)">
                    <td class="font-weight-bold"> {{ log.userName || 'Hệ thống' }} </td>
                    <td>{{ log.serviceName }}</td>
                    <td>{{ log.action }}</td>
                    <td>{{ log.entityType || '—' }}</td>
                    <td>
                      <VChip :color="severityColor(log.severity)" variant="tonal" size="small"> {{
                        severityText(log.severity) }} </VChip>
                    </td>
                    <td>{{ formatDate(log.createdAt) }}</td>
                    <td class="text-center" @click.stop>
                      <VBtn icon="ri-eye-line" variant="text" size="small" color="primary"
                        @click="openLogDetail(log)" />
                    </td>
                  </tr>
                </tbody>
              </VTable>
            </div>
            <div v-else class="settings-empty">
              <VIcon icon="ri-history-line" size="42" color="primary" /> <strong>Chưa có bản ghi hoạt động nào</strong>
              <span>Nhật ký thao tác hệ thống sẽ được hiển thị tại đây.</span>
            </div>
          </template>
        </VCard>
      </VWindowItem>
      <VWindowItem :value="2">
        <div class="backup-layout">
          <VCard class="settings-panel">
            <div class="panel-head">
              <div> <span>Backup</span> <strong>Lịch sử sao lưu hệ thống</strong>
                <p>Khôi phục dữ liệu về các thời điểm sao lưu trước đó.</p>
              </div>
            </div>
            <VDivider /> <template v-if="backupsList.length">
              <div class="settings-table-wrap">
                <VTable class="settings-table">
                  <thead>
                    <tr>
                      <th>Tên file sao lưu</th>
                      <th>Người tạo</th>
                      <th>Ghi chú</th>
                      <th>Thời gian</th>
                      <th class="text-center">Hành động</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="backup in backupsList" :key="backup.id" class="settings-row">
                      <td class="backup-file"> {{ backup.fileName }} </td>
                      <td>{{ backup.createdByName }}</td>
                      <td>{{ backup.note || '—' }}</td>
                      <td>{{ formatDate(backup.createdAt) }}</td>
                      <td class="text-center">
                        <VBtn size="small" color="warning" variant="tonal" prepend-icon="ri-history-line"
                          @click="handleRestoreBackup(backup.id)"> Khôi phục </VBtn>
                      </td>
                    </tr>
                  </tbody>
                </VTable>
              </div>
            </template>
            <div v-else class="settings-empty">
              <VIcon icon="ri-database-line" size="42" color="primary" /> <strong>Chưa có dữ liệu sao lưu</strong>
              <span>Tạo
                sao lưu mới để phòng tránh mất mát dữ liệu.</span>
            </div>
          </VCard>
          <VCard class="settings-panel create-backup-card">
            <div class="panel-head">
              <div> <span>Tạo sao lưu</span> <strong>Bản sao lưu mới</strong>
                <p>Sao lưu cơ sở dữ liệu và cấu hình trạng thái.</p>
              </div>
            </div>
            <VCardText>
              <VTextField v-model="backupNote" label="Ghi chú bản sao lưu"
                placeholder="Ví dụ: Trước khi cập nhật danh mục..." density="comfortable" class="mb-4" />
              <VAlert type="info" variant="tonal" class="mb-4"> Nên tạo bản sao lưu trước khi seed dữ liệu, cập nhật lớn
                hoặc
                khôi phục hệ thống. </VAlert>
              <VBtn block color="primary" prepend-icon="ri-database-line" class="primary-action" :loading="loading"
                @click="handleCreateBackup"> Tiến hành sao lưu </VBtn>
            </VCardText>
          </VCard>
        </div>
      </VWindowItem>
      <VWindowItem :value="3">
        <div class="security-layout">
          <VCard class="settings-panel">
            <div class="panel-head">
              <div> <span>Bảo mật</span> <strong>Đổi mật khẩu tài khoản</strong>
                <p>Cập nhật mật khẩu định kỳ để nâng cao bảo mật.</p>
              </div>
            </div>
            <VCardText>
              <VTextField v-model="oldPassword" type="password" label="Mật khẩu hiện tại"
                placeholder="Nhập mật khẩu đang sử dụng" density="comfortable" class="mb-4" />
              <VTextField v-model="newPassword" type="password" label="Mật khẩu mới" placeholder="Nhập mật khẩu mới"
                density="comfortable" class="mb-4" />
              <VTextField v-model="confirmPassword" type="password" label="Xác nhận mật khẩu mới"
                placeholder="Nhập lại mật khẩu mới" density="comfortable" class="mb-4"
                :error="newPassword !== confirmPassword && !!confirmPassword"
                error-messages="Mật khẩu xác nhận không khớp" />
              <VBtn block color="primary" prepend-icon="ri-lock-password-line" class="primary-action"
                :loading="securityLoading" @click="handleChangePassword"> Cập nhật mật khẩu </VBtn>
            </VCardText>
          </VCard>
          <VCard class="settings-panel">
            <div class="panel-head">
              <div> <span>Khuyến nghị</span> <strong>Hướng dẫn bảo mật</strong>
                <p>Các quy định an toàn thông tin khi truy cập hệ thống.</p>
              </div>
            </div>
            <VCardText>
              <VAlert type="info" variant="tonal" class="mb-4"> Mật khẩu nên chứa ít nhất 8 ký tự, bao gồm chữ hoa, chữ
                thường, chữ số và ký tự đặc biệt. </VAlert>
              <div class="rule-list">
                <div>
                  <VIcon icon="ri-shield-check-line" color="success" /> <span>Không chia sẻ mật khẩu tài khoản với bất
                    kỳ
                    ai.</span>
                </div>
                <div>
                  <VIcon icon="ri-logout-box-r-line" color="primary" /> <span>Đăng xuất khỏi thiết bị sau khi hoàn thành
                    phiên
                    làm việc.</span>
                </div>
                <div>
                  <VIcon icon="ri-refresh-line" color="warning" /> <span>Thay đổi mật khẩu định kỳ để đảm bảo an
                    toàn.</span>
                </div>
              </div>
            </VCardText>
          </VCard>
        </div>
      </VWindowItem>
      <VWindowItem :value="4">
        <div class="seed-layout">
          <VCard class="settings-panel danger-panel">
            <div class="panel-head">
              <div> <span>Seed dữ liệu</span> <strong>Dữ liệu mẫu Đơn hàng & Bán lẻ</strong>
                <p>Reset và nạp lại dữ liệu đơn hàng cho OrderSalesService.</p>
              </div>
              <VIcon icon="ri-shopping-bag-3-line" color="primary" size="30" />
            </div>
            <VCardText>
              <VAlert type="warning" variant="tonal" class="mb-4"> Thao tác này sẽ xóa toàn bộ đơn hàng, chi tiết đơn
                hàng,
                lịch sử trạng thái, thanh toán và trả hàng hiện tại. </VAlert>
              <VBtn color="primary" prepend-icon="ri-refresh-line" :loading="seedingOrders" block class="primary-action"
                @click="handleSeedOrders"> Reset & Nạp lại Đơn hàng </VBtn>
            </VCardText>
          </VCard>
          <VCard class="settings-panel danger-panel">
            <div class="panel-head">
              <div> <span>Seed dữ liệu</span> <strong>Dữ liệu mẫu Sản phẩm & Tồn kho</strong>
                <p>Reset và nạp lại dữ liệu tồn kho cho ProductInventoryService.</p>
              </div>
              <VIcon icon="ri-store-3-line" color="success" size="30" />
            </div>
            <VCardText>
              <VAlert type="warning" variant="tonal" class="mb-4"> Thao tác này sẽ xóa lịch sử biến động kho, phiếu
                nhập, chi
                tiết phiếu nhập và các lượt kiểm kho. </VAlert>
              <VBtn color="success" prepend-icon="ri-refresh-line" :loading="seedingInventory" block
                class="seed-success-action" @click="handleSeedInventory"> Reset & Nạp lại Tồn kho </VBtn>
            </VCardText>
          </VCard>
        </div>
      </VWindowItem>
    </VWindow>
    <VDialog v-model="userDialog" max-width="560" persistent>
      <VCard class="settings-dialog">
        <div class="dialog-head">
          <div> <span>{{ editingUser ? 'Cập nhật tài khoản' : 'Thêm nhân viên' }}</span>
            <h2>{{ editingUser ? editingUser.username : 'Nhân viên mới' }}</h2>
          </div>
          <VChip :color="roleColor(userForm.role)" variant="tonal"> {{ roleLabel(userForm.role) }} </VChip>
        </div>
        <VCardText>
          <VTextField v-model="userForm.username" label="Tên đăng nhập *" :disabled="!!editingUser"
            density="comfortable" class="mb-4" />
          <VTextField v-model="userForm.fullName" label="Họ và tên *" density="comfortable" class="mb-4" />
          <VTextField v-model="userForm.email" label="Email" density="comfortable" class="mb-4" />
          <VTextField v-model="userForm.phone" label="Số điện thoại" density="comfortable" class="mb-4" />
          <VSelect v-model="userForm.role" label="Vai trò hệ thống *" :items="roles" density="comfortable" />
        </VCardText>
        <VCardActions class="dialog-actions">
          <VSpacer />
          <VBtn color="secondary" variant="text" @click="userDialog = false"> Hủy </VBtn>
          <VBtn color="primary" :loading="loading" @click="handleSaveUser"> Lưu lại </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
    <VDialog v-model="logDialog" max-width="680">
      <VCard v-if="selectedLog" class="settings-dialog">
        <div class="dialog-head">
          <div> <span>Chi tiết nhật ký</span>
            <h2>{{ selectedLog.action }}</h2>
          </div>
          <VChip :color="severityColor(selectedLog.severity)" variant="tonal"> {{ severityText(selectedLog.severity) }}
          </VChip>
        </div>
        <VCardText>
          <div class="detail-grid">
            <div class="detail-card"> <span>Người dùng</span> <strong>{{ selectedLog.userName || 'Hệ thống' }}</strong>
              <p>{{ formatDate(selectedLog.createdAt) }}</p>
            </div>
            <div class="detail-card"> <span>Dịch vụ</span> <strong>{{ selectedLog.serviceName }}</strong>
              <p>Dịch vụ ghi nhận thao tác</p>
            </div>
            <div class="detail-card"> <span>Dữ liệu</span> <strong>{{ selectedLog.entityType || '—' }}</strong>
              <p>Loại dữ liệu bị tác động</p>
            </div>
          </div>
        </VCardText>
        <VCardActions class="dialog-actions">
          <VSpacer />
          <VBtn color="secondary" variant="outlined" @click="logDialog = false"> Đóng </VBtn>
        </VCardActions>
      </VCard>
    </VDialog>
  </section>
</template>
<style scoped>
.admin-settings-page {
  position: relative;
  isolation: isolate;
}

.admin-settings-page::before {
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

.seed-success-action {
  color: white !important;
  box-shadow: 0 14px 34px rgba(var(--v-theme-success), 0.24);
}

.summary-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  margin-block-end: 1rem;
}

.summary-card,
.tabs-card,
.settings-panel,
.settings-dialog {
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

.summary-icon--success {
  background: rgb(var(--v-theme-success));
}

.summary-icon--warning {
  background: rgb(var(--v-theme-warning));
}

.summary-icon--info {
  background: rgb(var(--v-theme-info));
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

.tabs-card {
  padding: 0.65rem;
  margin-block-end: 1rem;
}

.settings-tabs {
  display: grid;
  gap: 0.5rem;
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.settings-tab {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  min-block-size: 46px;
  border: 0;
  border-radius: 16px;
  color: rgba(var(--v-theme-on-surface), 0.66);
  background: transparent;
  font: inherit;
  font-weight: 850;
  cursor: pointer;
}

.settings-tab:hover,
.settings-tab--active {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.09);
}

.users-permissions-layout,
.backup-layout,
.security-layout,
.seed-layout {
  display: grid;
  align-items: start;
  gap: 1rem;
}

.users-permissions-layout {
  grid-template-columns: minmax(0, 1.25fr) minmax(360px, 0.75fr);
}

.backup-layout {
  grid-template-columns: minmax(0, 1fr) 360px;
}

.security-layout,
.seed-layout {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.15rem 1.25rem;
}

.panel-head span {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.75rem;
  font-weight: 900;
  text-transform: uppercase;
}

.panel-head strong {
  display: block;
  margin-block-start: 0.25rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1.25rem;
  font-weight: 950;
  letter-spacing: -0.04em;
}

.panel-head p {
  margin: 0.35rem 0 0;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.86rem;
}

.settings-table-wrap {
  overflow-x: auto;
}

.settings-table {
  min-inline-size: 760px;
}

.settings-table :deep(th) {
  block-size: 54px !important;
  color: rgba(var(--v-theme-on-surface), 0.62) !important;
  font-size: 0.74rem;
  font-weight: 900 !important;
  letter-spacing: 0.045em;
  text-transform: uppercase;
  background: rgba(var(--v-theme-background), 0.72);
}

.settings-table :deep(td) {
  block-size: 64px !important;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08) !important;
  color: rgba(var(--v-theme-on-surface), 0.82);
}

.settings-row {
  cursor: pointer;
  transition: background 160ms ease;
}

.settings-row:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user-cell strong,
.user-cell span {
  display: block;
}

.user-cell strong {
  color: rgb(var(--v-theme-on-surface));
  font-weight: 900;
}

.user-cell span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.backup-file {
  color: rgb(var(--v-theme-success));
  font-weight: 900;
}

.settings-empty {
  display: grid;
  place-items: center;
  gap: 0.55rem;
  min-block-size: 260px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.settings-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
}

.permission-list {
  display: grid;
  gap: 0.85rem;
  max-block-size: 520px;
  overflow-y: auto;
  padding-inline-end: 0.35rem;
}

.permission-group {
  border: 1px solid rgba(var(--v-border-color), 0.12);
  border-radius: 18px;
  padding: 0.8rem;
  background: rgba(var(--v-theme-background), 0.48);
}

.permission-group__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08);
  padding-block-end: 0.5rem;
  margin-block-end: 0.5rem;
}

.permission-group__head strong,
.permission-group__head span {
  display: block;
}

.permission-group__head strong {
  color: rgb(var(--v-theme-primary));
  font-weight: 950;
}

.permission-group__head span {
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
}

.permission-items {
  display: grid;
  gap: 0.45rem;
}

.permission-item p {
  margin: -0.2rem 0 0 2.1rem;
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.78rem;
  line-height: 1.35;
}

.save-permission-btn {
  margin-block-start: 1rem;
  border-radius: 14px;
  font-weight: 850;
}

.create-backup-card {
  position: sticky;
  inset-block-start: 84px;
}

.rule-list {
  display: grid;
  gap: 0.9rem;
}

.rule-list>div {
  display: grid;
  align-items: start;
  gap: 0.65rem;
  grid-template-columns: 22px minmax(0, 1fr);
  color: rgba(var(--v-theme-on-surface), 0.68);
  line-height: 1.5;
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

.settings-dialog :deep(.v-field),
.settings-panel :deep(.v-field) {
  border-radius: 16px;
}

.dialog-actions {
  flex-wrap: wrap;
  gap: 0.55rem;
  padding: 1rem 1.25rem;
  border-block-start: 1px solid rgba(var(--v-border-color), 0.12);
}

.detail-grid {
  display: grid;
  gap: 0.85rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
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
}@media (max-width: 1280px) {
  .settings-tabs {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .users-permissions-layout,
  .backup-layout,
  .security-layout,
  .seed-layout {
    grid-template-columns: 1fr;
  }

  .create-backup-card {
    position: static;
  }
}@media (max-width: 900px) {
  .summary-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .settings-tabs {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}@media (max-width: 760px) {

  .summary-grid,
  .settings-tabs,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .panel-head,
  .dialog-head,
  .permission-group__head {
    flex-direction: column;
  }
}

/* =========================
   COMPACT HERO STYLE
   ========================= */
.settings-hero {
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

.settings-hero__title-area {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.settings-hero h1 {
  font-size: 1.25rem !important;
  font-weight: 800 !important;
  margin: 0 !important;
  line-height: 1 !important;
}

.settings-hero__copy {
  display: none !important;
}

.settings-hero__actions,
.settings-actions {
  display: flex !important;
  align-items: center !important;
  gap: 0.5rem !important;
}

.settings-hero__actions .v-btn,
.settings-actions .v-btn,
.settings-hero__actions .v-btn.primary-action,
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
