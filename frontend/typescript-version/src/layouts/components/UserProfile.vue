<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

interface ProfileMenuItem {
  title: string
  subtitle?: string
  icon: string
  to: string
}

const router = useRouter()
const authStore = useAuthStore()

const roleLabels: Record<string, string> = {
  Admin: 'Quản trị viên',
  Sales: 'Nhân viên bán hàng',
  Warehouse: 'Thủ kho',
}

const roleColors: Record<string, string> = {
  Admin: 'primary',
  Sales: 'success',
  Warehouse: 'warning',
}

const displayName = computed(() =>
  authStore.user?.fullName
  || authStore.user?.username
  || 'Admin RetailOps',
)

const displayRole = computed(() => {
  const role = authStore.user?.role || 'Admin'

  return roleLabels[role] || role
})

const displayRoleColor = computed(() => {
  const role = authStore.user?.role || 'Admin'

  return roleColors[role] || 'primary'
})

const userInitials = computed(() => {
  const source = displayName.value || 'AD'

  const initials = source
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase())
    .join('')

  return initials || 'AD'
})

const menuItems = computed<ProfileMenuItem[]>(() => {
  const role = authStore.user?.role

  if (role === 'Sales') {
    return [
      {
        title: 'Workspace bán hàng',
        subtitle: 'Mở màn hình vận hành bán hàng',
        icon: 'ri-store-2-line',
        to: '/sales',
      },
      {
        title: 'Công nợ cần xử lý',
        subtitle: 'Theo dõi công nợ khách hàng',
        icon: 'ri-file-list-3-line',
        to: '/debts',
      },
    ]
  }

  if (role === 'Warehouse') {
    return [
      {
        title: 'Workspace kho',
        subtitle: 'Mở màn hình vận hành kho',
        icon: 'ri-home-gear-line',
        to: '/warehouse',
      },
      {
        title: 'Tồn kho cần xử lý',
        subtitle: 'Kiểm tra tồn thấp và biến động kho',
        icon: 'ri-archive-line',
        to: '/inventory',
      },
    ]
  }

  return [
    {
      title: 'Cài đặt hệ thống',
      subtitle: 'Tài khoản, phân quyền và bảo mật',
      icon: 'ri-settings-3-line',
      to: '/settings',
    },
    {
      title: 'Công nợ cần xử lý',
      subtitle: 'Theo dõi công nợ tổng hợp',
      icon: 'ri-file-list-3-line',
      to: '/debts',
    },
  ]
})

const logout = async () => {
  await authStore.logout()
  await router.push('/login')
}
</script>

<template>
  <VMenu width="320" location="bottom end" offset="14" transition="scale-transition">
    <template #activator="{ props }">
      <VBtn v-bind="props" icon variant="text" class="profile-trigger">
        <VBadge dot location="bottom right" offset-x="4" offset-y="4" color="success" bordered>
          <VAvatar color="primary" variant="tonal" size="40" class="profile-avatar">
            {{ userInitials }}
          </VAvatar>
        </VBadge>
      </VBtn>
    </template>

    <VCard class="profile-menu-card">
      <div class="profile-head">
        <VAvatar color="primary" variant="tonal" size="48" class="profile-head__avatar">
          {{ userInitials }}
        </VAvatar>

        <div class="profile-head__info">
          <strong>{{ displayName }}</strong>

          <VChip :color="displayRoleColor" variant="tonal" size="small">
            {{ displayRole }}
          </VChip>
        </div>
      </div>

      <VDivider />

      <VList class="profile-list">
        <VListItem v-for="item in menuItems" :key="item.to" :to="item.to" rounded="lg" class="profile-list-item">
          <template #prepend>
            <VAvatar color="primary" variant="tonal" size="36" rounded="lg">
              <VIcon :icon="item.icon" size="20" />
            </VAvatar>
          </template>

          <VListItemTitle>
            {{ item.title }}
          </VListItemTitle>

          <VListItemSubtitle v-if="item.subtitle">
            {{ item.subtitle }}
          </VListItemSubtitle>
        </VListItem>
      </VList>

      <VDivider />

      <div class="profile-footer">
        <VBtn block color="error" variant="tonal" prepend-icon="ri-logout-box-r-line" class="logout-btn"
          @click="logout">
          Đăng xuất
        </VBtn>
      </div>
    </VCard>
  </VMenu>
</template>

<style scoped>
.profile-trigger {
  inline-size: 46px;
  block-size: 46px;
  border-radius: 16px;
}

.profile-avatar {
  font-size: 0.9rem;
  font-weight: 900;
  letter-spacing: -0.02em;
}

.profile-menu-card {
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 22px !important;
  background:
    linear-gradient(145deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.92));
  box-shadow:
    0 22px 60px rgba(15, 23, 42, 0.16),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.profile-head {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 1rem;
}

.profile-head__avatar {
  flex: 0 0 auto;
  font-weight: 950;
}

.profile-head__info {
  display: grid;
  gap: 0.35rem;
  min-inline-size: 0;
}

.profile-head__info strong {
  overflow: hidden;
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.98rem;
  font-weight: 900;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.profile-list {
  padding: 0.55rem;
}

.profile-list-item {
  margin-block: 0.2rem;
  padding-inline: 0.65rem !important;
}

.profile-list-item :deep(.v-list-item-title) {
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.9rem;
  font-weight: 850;
}

.profile-list-item :deep(.v-list-item-subtitle) {
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.78rem;
  line-height: 1.35;
}

.profile-footer {
  padding: 0.8rem;
}

.logout-btn {
  border-radius: 14px;
  font-weight: 850;
}
</style>