<script setup lang="ts">
import { computed } from 'vue'

interface FooterLink {
  title: string
  to: string
  icon: string
}

const currentYear = computed(() => new Date().getFullYear())

const footerLinks: FooterLink[] = [
  {
    title: 'Phân quyền',
    to: '/settings',
    icon: 'ri-shield-user-line',
  },
  {
    title: 'Tồn kho',
    to: '/inventory',
    icon: 'ri-archive-line',
  },
  {
    title: 'Nhập hàng',
    to: '/goods-receipts',
    icon: 'ri-truck-line',
  },
]
</script>

<template>
  <footer class="app-footer">
    <div class="footer-brand">
      <span class="footer-dot" />

      <span>
        &copy; {{ currentYear }} RetailOps
      </span>

      <span class="footer-divider">/</span>

      <span>
        Quản lý bán lẻ và kho hàng
      </span>
    </div>

    <nav class="footer-links">
      <RouterLink v-for="link in footerLinks" :key="link.to" :to="link.to" class="footer-link">
        <VIcon :icon="link.icon" size="16" />

        <span>{{ link.title }}</span>
      </RouterLink>
    </nav>
  </footer>
</template>

<style scoped>
.app-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  block-size: 100%;
  padding-inline: 0.5rem;
  color: rgba(var(--v-theme-on-surface), 0.62);
  font-size: 0.86rem;
}

.footer-brand {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.45rem;
  min-inline-size: 0;
  font-weight: 650;
}

.footer-dot {
  inline-size: 8px;
  block-size: 8px;
  border-radius: 999px;
  background: linear-gradient(135deg,
      rgb(var(--v-theme-primary)),
      rgb(var(--v-theme-info)));
  box-shadow: 0 0 0 4px rgba(var(--v-theme-primary), 0.12);
}

.footer-divider {
  color: rgba(var(--v-theme-on-surface), 0.32);
}

.footer-links {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.footer-link {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  border-radius: 999px;
  padding: 0.42rem 0.7rem;
  color: rgb(var(--v-theme-primary));
  font-size: 0.82rem;
  font-weight: 800;
  text-decoration: none;
  transition:
    background 160ms ease,
    color 160ms ease,
    transform 160ms ease;
}

.footer-link:hover {
  background: rgba(var(--v-theme-primary), 0.09);
  transform: translateY(-1px);
}

@media (max-width: 960px) {
  .app-footer {
    justify-content: center;
    text-align: center;
  }

  .footer-links {
    display: none;
  }
}
</style>