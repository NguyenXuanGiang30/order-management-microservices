<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

interface ProfileMenuItem {
  title: string
  icon: string
  to: string
}

const router = useRouter()
const authStore = useAuthStore()

const userInitials = computed(() => {
  const source = authStore.user?.fullName || authStore.user?.username || 'AD'

  return source
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase())
    .join('') || 'AD'
})

const displayName = computed(() => authStore.user?.fullName || authStore.user?.username || 'Admin RetailOps')

const displayRole = computed(() => {
  if (authStore.user?.role === 'Admin')
    return 'Quản trị viên'

  if (authStore.user?.role === 'Sales')
    return 'Nhân viên bán hàng'

  if (authStore.user?.role === 'Warehouse')
    return 'Thủ kho'

  return authStore.user?.role || 'Quản trị viên'
})

const menuItems = computed<ProfileMenuItem[]>(() => {
  if (authStore.user?.role === 'Sales') {
    return [
      { title: 'Workspace bán hàng', icon: 'ri-store-2-line', to: '/sales' },
      { title: 'Công nợ cần xử lý', icon: 'ri-file-list-3-line', to: '/debts' },
    ]
  }

  if (authStore.user?.role === 'Warehouse') {
    return [
      { title: 'Workspace kho', icon: 'ri-home-gear-line', to: '/warehouse' },
      { title: 'Tồn kho cần xử lý', icon: 'ri-archive-line', to: '/inventory' },
    ]
  }

  return [
    { title: 'Cài đặt hệ thống', icon: 'ri-settings-3-line', to: '/settings' },
    { title: 'Công nợ cần xử lý', icon: 'ri-file-list-3-line', to: '/debts' },
  ]
})

const logout = async () => {
  await authStore.logout()
  await router.push('/login')
}
</script>

<template>
  <VBadge
    dot
    location="bottom right"
    offset-x="3"
    offset-y="3"
    color="success"
    bordered
  >
    <VAvatar
      class="cursor-pointer"
      color="primary"
      variant="tonal"
    >
      {{ userInitials }}

      <VMenu
        activator="parent"
        width="260"
        location="bottom end"
        offset="14px"
      >
        <VList>
          <VListItem>
            <template #prepend>
              <VAvatar
                color="primary"
                variant="tonal"
                class="me-2"
              >
                {{ userInitials }}
              </VAvatar>
            </template>

            <VListItemTitle class="font-weight-semibold">
              {{ displayName }}
            </VListItemTitle>
            <VListItemSubtitle>{{ displayRole }}</VListItemSubtitle>
          </VListItem>

          <VDivider class="my-2" />

          <VListItem
            v-for="item in menuItems"
            :key="item.to"
            :to="item.to"
          >
            <template #prepend>
              <VIcon
                class="me-2"
                :icon="item.icon"
                size="22"
              />
            </template>
            <VListItemTitle>{{ item.title }}</VListItemTitle>
          </VListItem>

          <VDivider class="my-2" />

          <VListItem @click="logout">
            <template #prepend>
              <VIcon
                class="me-2"
                icon="ri-logout-box-r-line"
                size="22"
              />
            </template>
            <VListItemTitle>Đăng xuất</VListItemTitle>
          </VListItem>
        </VList>
      </VMenu>
    </VAvatar>
  </VBadge>
</template>
