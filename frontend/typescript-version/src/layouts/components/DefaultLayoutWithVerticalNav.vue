<script lang="ts" setup>
import { computed } from 'vue'

import NavItems from '@/layouts/components/NavItems.vue'
import Footer from '@/layouts/components/Footer.vue'
import NavbarThemeSwitcher from '@/layouts/components/NavbarThemeSwitcher.vue'
import UserProfile from '@/layouts/components/UserProfile.vue'
import NotificationMenu from '@/layouts/components/NotificationMenu.vue'
import ServiceStatus from '@/layouts/components/ServiceStatus.vue'
import { useAuthStore } from '@/stores/auth'
import VerticalNavLayout from '@layouts/components/VerticalNavLayout.vue'

const authStore = useAuthStore()

const workspaceLabel = computed(() => {
  if (authStore.user?.role === 'Sales')
    return 'Workspace bán hàng'

  if (authStore.user?.role === 'Warehouse')
    return 'Workspace kho'

  return 'Workspace quản trị'
})

const searchPlaceholder = computed(() => {
  if (authStore.user?.role === 'Sales')
    return 'Tìm đơn hàng, khách hàng, công nợ...'

  if (authStore.user?.role === 'Warehouse')
    return 'Tìm sản phẩm, tồn kho, phiếu nhập...'

  return 'Tìm sản phẩm, đơn hàng, khách hàng...'
})
</script>

<template>
  <VerticalNavLayout>
    <template #navbar="{ toggleVerticalOverlayNavActive }">
      <div class="navbar-shell d-flex h-100 align-center">
        <IconBtn
          class="ms-n3 d-lg-none"
          @click="toggleVerticalOverlayNavActive(true)"
        >
          <VIcon icon="ri-menu-line" />
        </IconBtn>

        <div class="navbar-search d-none d-sm-flex align-center">
          <VTextField
            density="compact"
            variant="outlined"
            prepend-inner-icon="ri-search-line"
            :placeholder="searchPlaceholder"
            hide-details
          />
        </div>

        <VSpacer />

        <VChip
          color="primary"
          variant="tonal"
          size="small"
          class="me-3 d-none d-md-inline-flex"
          prepend-icon="ri-layout-grid-line"
        >
          {{ workspaceLabel }}
        </VChip>

        <ServiceStatus />
        <NotificationMenu />

        <NavbarThemeSwitcher class="me-2" />
        <UserProfile />
      </div>
    </template>

    <template #vertical-nav-header="{ toggleIsOverlayNavActive }">
      <RouterLink
        to="/"
        class="app-logo app-title-wrapper"
      >
        <VAvatar
          color="primary"
          rounded="lg"
          size="38"
        >
          <VIcon
            icon="ri-store-2-line"
            size="22"
          />
        </VAvatar>

        <h1 class="app-logo-title font-weight-bold leading-normal text-xl text-uppercase">
          RetailOps
        </h1>
      </RouterLink>

      <IconBtn
        class="d-block d-lg-none"
        @click="toggleIsOverlayNavActive(false)"
      >
        <VIcon icon="ri-close-line" />
      </IconBtn>
    </template>

    <template #vertical-nav-content>
      <NavItems />
    </template>

    <slot />

    <template #footer>
      <Footer />
    </template>
  </VerticalNavLayout>
</template>

<style lang="scss" scoped>
.app-logo {
  display: flex;
  align-items: center;
  column-gap: 0.75rem;

  .app-logo-title {
    color: rgba(var(--v-theme-on-surface), 0.92);
    font-size: 1.15rem;
    letter-spacing: 0.04em;
    line-height: 1.75rem;
    text-transform: uppercase;
  }
}

.navbar-shell {
  inline-size: 100%;
}

.navbar-search {
  inline-size: min(460px, 52vw);

  :deep(.v-field) {
    border-radius: 999px;
    background: rgba(var(--v-theme-surface), 0.72);
    box-shadow: inset 0 0 0 1px rgba(var(--v-theme-primary), 0.04);
  }
}

.navbar-icon-btn {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 999px;
  background: rgba(var(--v-theme-surface), 0.74);
}
</style>
