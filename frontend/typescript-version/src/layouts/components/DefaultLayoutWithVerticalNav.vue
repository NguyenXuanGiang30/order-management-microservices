<script lang="ts" setup>
import NavItems from '@/layouts/components/NavItems.vue'
import Footer from '@/layouts/components/Footer.vue'
import NavbarThemeSwitcher from '@/layouts/components/NavbarThemeSwitcher.vue'
import UserProfile from '@/layouts/components/UserProfile.vue'
import VerticalNavLayout from '@layouts/components/VerticalNavLayout.vue'
</script>

<template>
  <VerticalNavLayout>
    <template #navbar="{ toggleVerticalOverlayNavActive }">
      <div class="d-flex h-100 align-center">
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
            placeholder="Tìm sản phẩm, đơn hàng, khách hàng..."
            hide-details
          />
        </div>

        <VSpacer />

        <VChip
          color="success"
          variant="tonal"
          size="small"
          class="me-3 d-none d-md-inline-flex"
        >
          Hệ thống online
        </VChip>

        <IconBtn>
          <VBadge
            dot
            color="error"
          >
            <VIcon icon="ri-notification-line" />
          </VBadge>
        </IconBtn>

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

        <h1 class="font-weight-medium leading-normal text-xl text-uppercase">
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
    font-size: 1.25rem;
    font-weight: 500;
    line-height: 1.75rem;
    text-transform: uppercase;
  }
}

.navbar-search {
  inline-size: min(440px, 52vw);

  :deep(.v-field) {
    border-radius: 8px;
  }
}
</style>
