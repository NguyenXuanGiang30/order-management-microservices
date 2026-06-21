<script setup lang="ts">
import {
  type UserDto,
  type BackupRecordDto,
  type ActivityLogDto,
  getUsers,
  createUser,
  updateUser,
  toggleUserActive,
  getRolePermissions,
  updateRolePermissions,
  getPermissions,
  getBackups,
  createBackup,
  restoreBackup,
  getActivityLogs,
} from '@/services/adminApi'

const activeTab = ref(0)
const loading = ref(false)
const errorMessage = ref('')

// Users states
const usersList = ref<UserDto[]>([])
const userDialog = ref(false)
const editingUser = ref<UserDto | null>(null)
const userForm = ref({
  username: '',
  fullName: '',
  email: '',
  phone: '',
  role: 'Sales',
})

// Permissions states
const selectedRole = ref('Sales')
const roles = ['Admin', 'Sales', 'Warehouse']
const rolePermissions = ref<string[]>([])
const allPermissions = ref<any[]>([])

// Activity logs states
const activityLogs = ref<ActivityLogDto[]>([])

// Backup states
const backupsList = ref<BackupRecordDto[]>([])
const backupNote = ref('')

const loadUsers = async () => {
  loading.value = true
  try {
    const res = await getUsers({ pageSize: 100 })
    usersList.value = res.items
  } catch (error: any) {
    errorMessage.value = error.message || 'Không thể tải danh sách tài khoản.'
  } finally {
    loading.value = false
  }
}

const loadRolePermissions = async () => {
  try {
    const [permsRes, rolePermsRes] = await Promise.all([
      getPermissions(),
      getRolePermissions(selectedRole.value),
    ])
    allPermissions.value = permsRes
    rolePermissions.value = rolePermsRes.permissions
  } catch (error) {
    console.error('Lỗi tải quyền:', error)
  }
}

const handleSavePermissions = async () => {
  loading.value = true
  try {
    await updateRolePermissions(selectedRole.value, rolePermissions.value)
    alert('Cập nhật phân quyền thành công!')
  } catch (error: any) {
    alert('Lỗi cập nhật quyền: ' + error.message)
  } finally {
    loading.value = false
  }
}

const loadActivityLogs = async () => {
  loading.value = true
  try {
    const res = await getActivityLogs({ pageSize: 50 })
    activityLogs.value = res.items
  } catch (error) {
    console.error('Không thể tải nhật ký hoạt động:', error)
  } finally {
    loading.value = false
  }
}

const loadBackups = async () => {
  loading.value = true
  try {
    backupsList.value = await getBackups()
  } catch (error) {
    console.error('Không thể tải danh sách sao lưu:', error)
  } finally {
    loading.value = false
  }
}

const handleCreateBackup = async () => {
  loading.value = true
  try {
    await createBackup({ note: backupNote.value || 'Sao lưu định kỳ' })
    backupNote.value = ''
    await loadBackups()
    alert('Tạo sao lưu hệ thống thành công!')
  } catch (error: any) {
    alert('Lỗi tạo sao lưu: ' + error.message)
  } finally {
    loading.value = false
  }
}

const handleRestoreBackup = async (id: string) => {
  if (!confirm('CẢNH BÁO: Phục hồi sao lưu sẽ ghi đè toàn bộ dữ liệu hiện tại của hệ thống. Bạn có chắc chắn muốn thực hiện?')) return
  loading.value = true
  try {
    await restoreBackup(id, { confirm: true, note: 'Khôi phục từ quản trị' })
    alert('Khôi phục hệ thống thành công! Vui lòng tải lại trang.')
  } catch (error: any) {
    alert('Lỗi phục hồi: ' + error.message)
  } finally {
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
    role: user.role,
  }
  userDialog.value = true
}

const handleSaveUser = async () => {
  loading.value = true
  try {
    if (editingUser.value) {
      await updateUser(editingUser.value.id, {
        fullName: userForm.value.fullName,
        email: userForm.value.email || null,
        phone: userForm.value.phone || null,
        role: userForm.value.role,
      })
    } else {
      await createUser({
        username: userForm.value.username,
        fullName: userForm.value.fullName,
        email: userForm.value.email || null,
        phone: userForm.value.phone || null,
        role: userForm.value.role,
      })
    }
    userDialog.value = false
    await loadUsers()
  } catch (error: any) {
    alert('Lỗi lưu tài khoản: ' + error.message)
  } finally {
    loading.value = false
  }
}

const handleToggleUserStatus = async (user: UserDto) => {
  try {
    await toggleUserActive(user.id)
    await loadUsers()
  } catch (error: any) {
    alert('Lỗi kích hoạt/vô hiệu hóa tài khoản: ' + error.message)
  }
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleString('vi-VN')
}

watch(activeTab, (val) => {
  if (val === 0) {
    loadUsers()
    loadRolePermissions()
  }
  else if (val === 1) loadActivityLogs()
  else if (val === 2) loadBackups()
})

onMounted(() => {
  loadUsers()
  loadRolePermissions()
})
</script>

<template>
  <RetailPageHeader
    eyebrow="Quản trị"
    title="Cài đặt & phân quyền"
    subtitle="Quản lý vai trò, quyền truy cập, nhật ký hệ thống và sao lưu/khôi phục dữ liệu."
  />

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

  <VTabs
    v-model="activeTab"
    class="mb-6"
  >
    <VTab :value="0">Tài khoản & Phân quyền</VTab>
    <VTab :value="1">Nhật ký hoạt động</VTab>
    <VTab :value="2">Sao lưu & Khôi phục</VTab>
  </VTabs>

  <VWindow v-model="activeTab">
    <!-- Tab 0: Users and permissions -->
    <VWindowItem :value="0">
      <VRow>
        <VCol cols="12" lg="7">
          <VCard class="retail-panel-card">
            <VCardItem class="d-flex justify-between align-center">
              <div>
                <VCardTitle>Tài khoản nhân viên</VCardTitle>
                <VCardSubtitle>Danh sách tài khoản và vai trò trong hệ thống</VCardSubtitle>
              </div>
              <template #append>
                <VBtn
                  prepend-icon="ri-user-add-line"
                  size="small"
                  @click="handleOpenCreateUser"
                >
                  Thêm nhân viên
                </VBtn>
              </template>
            </VCardItem>

            <VTable class="retail-table">
              <thead>
                <tr>
                  <th>Tài khoản</th>
                  <th>Họ tên</th>
                  <th>Vai trò</th>
                  <th>Trạng thái</th>
                  <th class="text-center">Hành động</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="u in usersList"
                  :key="u.id"
                  class="hover-row"
                >
                  <td class="font-weight-bold text-primary">{{ u.username }}</td>
                  <td>{{ u.fullName }}</td>
                  <td>
                    <RetailStatusBadge :status="u.role" />
                  </td>
                  <td>
                    <VSwitch
                      :model-value="u.isActive"
                      color="success"
                      density="compact"
                      hide-details
                      @change="handleToggleUserStatus(u)"
                    />
                  </td>
                  <td class="text-center">
                    <VBtn
                      icon="ri-edit-line"
                      variant="text"
                      size="small"
                      color="primary"
                      @click="handleOpenEditUser(u)"
                    />
                  </td>
                </tr>
                <RetailEmptyState
                  v-if="!usersList.length"
                  :colspan="5"
                  icon="ri-user-settings-line"
                  title="Chưa có tài khoản nhân viên nào"
                  subtitle="Tạo tài khoản mới để cấp quyền truy cập."
                />
              </tbody>
            </VTable>
          </VCard>
        </VCol>

        <VCol cols="12" lg="5">
          <VCard class="retail-panel-card">
            <VCardItem>
              <VCardTitle>Chi tiết phân quyền</VCardTitle>
              <VCardSubtitle>Quản lý các chức năng cho từng nhóm vai trò</VCardSubtitle>
            </VCardItem>
            <VCardText>
              <VSelect
                v-model="selectedRole"
                label="Chọn vai trò cấu hình"
                :items="roles"
                class="mb-4"
                @update:model-value="loadRolePermissions"
              />

              <div class="mb-4 text-subtitle-2 font-weight-bold">
                Quyền được cấp phép:
              </div>

              <div style="max-height: 350px; overflow-y: auto;">
                <VCheckbox
                  v-for="p in allPermissions"
                  :key="p.code"
                  v-model="rolePermissions"
                  :label="p.name"
                  :value="p.code"
                  density="compact"
                  hide-details
                />
              </div>

              <VBtn
                block
                color="primary"
                class="mt-4"
                prepend-icon="ri-save-3-line"
                :loading="loading"
                @click="handleSavePermissions"
              >
                Lưu phân quyền
              </VBtn>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>
    </VWindowItem>

    <!-- Tab 1: Activity logs -->
    <VWindowItem :value="1">
      <VCard class="retail-panel-card">
        <VCardItem>
          <VCardTitle>Nhật ký hệ thống</VCardTitle>
          <VCardSubtitle>Danh sách thao tác, sự kiện và mức độ nghiêm trọng</VCardSubtitle>
        </VCardItem>
        <VTable class="retail-table">
          <thead>
            <tr>
              <th>Người dùng</th>
              <th>Dịch vụ</th>
              <th>Thao tác</th>
              <th>Loại dữ liệu</th>
              <th>Mức độ</th>
              <th>Thời gian</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="log in activityLogs"
              :key="log.id"
              class="hover-row"
            >
              <td class="font-weight-bold text-primary">{{ log.userName }}</td>
              <td>{{ log.serviceName }}</td>
              <td>{{ log.action }}</td>
              <td>{{ log.entityType }}</td>
              <td>
                <RetailStatusBadge
                  :status="log.severity === 'Error' ? 'Nghiêm trọng' : (log.severity === 'Warning' ? 'Cần theo dõi' : 'Bình thường')"
                  dot
                />
              </td>
              <td>{{ formatDate(log.createdAt) }}</td>
            </tr>
            <RetailEmptyState
              v-if="!activityLogs.length"
              :colspan="6"
              icon="ri-history-line"
              title="Chưa có bản ghi hoạt động nào"
              subtitle="Nhật ký thao tác hệ thống sẽ được hiển thị tại đây."
            />
          </tbody>
        </VTable>
      </VCard>
    </VWindowItem>

    <!-- Tab 2: Backup and restore -->
    <VWindowItem :value="2">
      <VRow>
        <VCol cols="12" lg="8">
          <VCard class="retail-panel-card">
            <VCardItem>
              <VCardTitle>Lịch sử sao lưu hệ thống</VCardTitle>
              <VCardSubtitle>Khôi phục dữ liệu về các thời điểm sao lưu trước đó</VCardSubtitle>
            </VCardItem>

            <VTable class="retail-table">
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
                <tr
                  v-for="b in backupsList"
                  :key="b.id"
                  class="hover-row"
                >
                  <td class="font-weight-bold text-success">{{ b.fileName }}</td>
                  <td>{{ b.createdByName }}</td>
                  <td>{{ b.note || '—' }}</td>
                  <td>{{ formatDate(b.createdAt) }}</td>
                  <td class="text-center">
                    <VBtn
                      size="small"
                      color="warning"
                      variant="tonal"
                      prepend-icon="ri-history-line"
                      @click="handleRestoreBackup(b.id)"
                    >
                      Khôi phục
                    </VBtn>
                  </td>
                </tr>
                <RetailEmptyState
                  v-if="!backupsList.length"
                  :colspan="5"
                  icon="ri-database-line"
                  title="Chưa có dữ liệu sao lưu"
                  subtitle="Tạo sao lưu mới để phòng tránh mất mát dữ liệu."
                />
              </tbody>
            </VTable>
          </VCard>
        </VCol>

        <VCol cols="12" lg="4">
          <VCard class="retail-panel-card">
            <VCardItem>
              <VCardTitle>Tạo bản sao lưu</VCardTitle>
              <VCardSubtitle>Sao lưu cơ sở dữ liệu và lưu cấu hình trạng thái</VCardSubtitle>
            </VCardItem>
            <VCardText>
              <VTextField
                v-model="backupNote"
                label="Ghi chú bản sao lưu"
                placeholder="Ví dụ: Trước khi cập nhật danh mục..."
                class="mb-4"
              />
              <VBtn
                block
                color="primary"
                prepend-icon="ri-database-line"
                :loading="loading"
                @click="handleCreateBackup"
              >
                Tiến hành sao lưu
              </VBtn>
            </VCardText>
          </VCard>
        </VCol>
      </VRow>
    </VWindowItem>
  </VWindow>

  <!-- Create/Edit User Dialog -->
  <VDialog
    v-model="userDialog"
    max-width="500"
  >
    <VCard>
      <VCardTitle>{{ editingUser ? 'Cập nhật tài khoản nhân viên' : 'Thêm nhân viên mới' }}</VCardTitle>
      <VCardText>
        <VTextField
          v-model="userForm.username"
          label="Tên đăng nhập *"
          :disabled="!!editingUser"
          class="mb-4"
        />
        <VTextField
          v-model="userForm.fullName"
          label="Họ và tên *"
          class="mb-4"
        />
        <VTextField
          v-model="userForm.email"
          label="Email"
          class="mb-4"
        />
        <VTextField
          v-model="userForm.phone"
          label="Số điện thoại"
          class="mb-4"
        />
        <VSelect
          v-model="userForm.role"
          label="Vai trò hệ thống *"
          :items="roles"
        />
      </VCardText>
      <VCardActions>
        <VSpacer />
        <VBtn
          color="secondary"
          variant="text"
          @click="userDialog = false"
        >
          Hủy
        </VBtn>
        <VBtn
          color="primary"
          @click="handleSaveUser"
        >
          Lưu lại
        </VBtn>
      </VCardActions>
    </VCard>
  </VDialog>
</template>
