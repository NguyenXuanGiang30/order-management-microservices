<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

import { useAuthStore } from '@/stores/auth'

interface NavLinkItem {
  title: string
  icon: string
  to: string
}

interface NavSection {
  heading: string
  icon: string
  links: NavLinkItem[]
}

const adminSections: NavSection[] = [
  {
    heading: 'Vận hành',
    icon: 'ri-pulse-line',
    links: [
      { title: 'Tổng quan', icon: 'ri-dashboard-line', to: '/dashboard' },
      { title: 'POS bán hàng', icon: 'ri-shopping-cart-2-line', to: '/pos' },
      { title: 'Đơn hàng', icon: 'ri-receipt-line', to: '/orders' },
      { title: 'Trả hàng', icon: 'ri-arrow-go-back-line', to: '/returns' },
      { title: 'Ca bán hàng', icon: 'ri-calendar-todo-line', to: '/shifts' },
      { title: 'Sổ quỹ', icon: 'ri-bank-card-line', to: '/cashbook' },
    ],
  },
  {
    heading: 'Kho hàng',
    icon: 'ri-archive-stack-line',
    links: [
      { title: 'Sản phẩm', icon: 'ri-price-tag-3-line', to: '/products' },
      { title: 'Tồn kho', icon: 'ri-archive-line', to: '/inventory' },
      { title: 'Báo cáo NXT', icon: 'ri-file-chart-line', to: '/nxt-report' },
      { title: 'Biến động kho', icon: 'ri-history-line', to: '/inventory-transactions' },
      { title: 'Kiểm kê kho', icon: 'ri-barcode-box-line', to: '/stock-take' },
      { title: 'Xuất kho nội bộ', icon: 'ri-download-line', to: '/internal-stock-export' },
      { title: 'Nhập hàng', icon: 'ri-truck-line', to: '/goods-receipts' },
      { title: 'Đơn vị tính', icon: 'ri-ruler-2-line', to: '/units' },
      { title: 'Nhà cung cấp', icon: 'ri-building-4-line', to: '/suppliers' },
      { title: 'Thanh toán nợ NCC', icon: 'ri-money-dollar-circle-line', to: '/supplier-payments' },
    ],
  },
  {
    heading: 'Khách hàng',
    icon: 'ri-user-heart-line',
    links: [
      { title: 'Khách hàng', icon: 'ri-user-3-line', to: '/customers' },
      { title: 'Công nợ', icon: 'ri-file-list-3-line', to: '/debts' },
      { title: 'Khuyến mãi', icon: 'ri-coupon-3-line', to: '/promotions' },
    ],
  },
  {
    heading: 'Quản trị',
    icon: 'ri-settings-3-line',
    links: [
      { title: 'Cài đặt & phân quyền', icon: 'ri-settings-3-line', to: '/settings' },
      { title: 'Nhật ký hoạt động', icon: 'ri-history-line', to: '/activity-logs' },
    ],
  },
]

const salesSections: NavSection[] = [
  {
    heading: 'Bán hàng',
    icon: 'ri-store-2-line',
    links: [
      { title: 'Tổng quan', icon: 'ri-dashboard-line', to: '/dashboard' },
      { title: 'Trang bán hàng', icon: 'ri-store-2-line', to: '/sales' },
      { title: 'POS bán hàng', icon: 'ri-shopping-cart-2-line', to: '/pos' },
      { title: 'Đơn hàng', icon: 'ri-receipt-line', to: '/orders' },
      { title: 'Trả hàng', icon: 'ri-arrow-go-back-line', to: '/returns' },
      { title: 'Ca bán hàng', icon: 'ri-calendar-todo-line', to: '/shifts' },
      { title: 'Sổ quỹ', icon: 'ri-bank-card-line', to: '/cashbook' },
    ],
  },
  {
    heading: 'Khách hàng',
    icon: 'ri-user-heart-line',
    links: [
      { title: 'Khách hàng', icon: 'ri-user-3-line', to: '/customers' },
      { title: 'Công nợ', icon: 'ri-file-list-3-line', to: '/debts' },
      { title: 'Khuyến mãi', icon: 'ri-coupon-3-line', to: '/promotions' },
    ],
  },
]

const warehouseSections: NavSection[] = [
  {
    heading: 'Kho hàng',
    icon: 'ri-archive-stack-line',
    links: [
      { title: 'Tổng quan', icon: 'ri-dashboard-line', to: '/dashboard' },
      { title: 'Trang kho', icon: 'ri-home-gear-line', to: '/warehouse' },
      { title: 'Tồn kho', icon: 'ri-archive-line', to: '/inventory' },
      { title: 'Báo cáo NXT', icon: 'ri-file-chart-line', to: '/nxt-report' },
      { title: 'Biến động kho', icon: 'ri-history-line', to: '/inventory-transactions' },
      { title: 'Kiểm kê kho', icon: 'ri-barcode-box-line', to: '/stock-take' },
      { title: 'Xuất kho nội bộ', icon: 'ri-download-line', to: '/internal-stock-export' },
      { title: 'Sản phẩm', icon: 'ri-price-tag-3-line', to: '/products' },
      { title: 'Nhập hàng', icon: 'ri-truck-line', to: '/goods-receipts' },
      { title: 'Đơn vị tính', icon: 'ri-ruler-2-line', to: '/units' },
      { title: 'Nhà cung cấp', icon: 'ri-building-4-line', to: '/suppliers' },
      { title: 'Thanh toán nợ NCC', icon: 'ri-money-dollar-circle-line', to: '/supplier-payments' },
    ],
  },
]

const route = useRoute()
const authStore = useAuthStore()

const sections = computed(() => {
  if (authStore.user?.role === 'Sales')
    return salesSections

  if (authStore.user?.role === 'Warehouse')
    return warehouseSections

  return adminSections
})

const isActiveRoute = (to: string) => {
  return route.path === to || route.path.startsWith(`${to}/`)
}
</script>

<template>
  <nav class="retail-nav">
    <section v-for="section in sections" :key="section.heading" class="retail-nav-section">
      <div class="retail-nav-heading">
        <div class="retail-nav-heading__left">
          <VIcon :icon="section.icon" size="16" />
          <span>{{ section.heading }}</span>
        </div>

        <span class="retail-nav-count">
          {{ section.links.length }}
        </span>
      </div>

      <div class="retail-nav-links">
        <RouterLink v-for="link in section.links" :key="link.to" :to="link.to" class="retail-nav-link"
          :class="{ 'retail-nav-link--active': isActiveRoute(link.to) }"
          :aria-current="isActiveRoute(link.to) ? 'page' : undefined">
          <span class="retail-nav-link__glow" />

          <span class="retail-nav-link__icon">
            <VIcon :icon="link.icon" size="20" />
          </span>

          <span class="retail-nav-link__title">
            {{ link.title }}
          </span>

          <VIcon icon="ri-arrow-right-s-line" size="18" class="retail-nav-link__arrow" />
        </RouterLink>
      </div>
    </section>
  </nav>
</template>

<style scoped>
.retail-nav {
  display: grid;
  gap: 1.25rem;
  padding: 0.35rem 0.75rem 1.25rem;
}

.retail-nav-section {
  display: grid;
  gap: 0.55rem;
}

.retail-nav-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.25rem 0.45rem 0.35rem;
}

.retail-nav-heading__left {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  min-inline-size: 0;
  color: rgba(var(--v-theme-on-surface), 0.46);
  font-size: 0.72rem;
  font-weight: 850;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.retail-nav-heading__left span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.retail-nav-count {
  display: inline-grid;
  place-items: center;
  min-inline-size: 24px;
  block-size: 22px;
  border-radius: 999px;
  color: rgba(var(--v-theme-on-surface), 0.48);
  background: rgba(var(--v-theme-on-surface), 0.06);
  font-size: 0.72rem;
  font-weight: 800;
}

.retail-nav-links {
  display: grid;
  gap: 0.28rem;
}

.retail-nav-link {
  position: relative;
  isolation: isolate;
  display: grid;
  align-items: center;
  gap: 0.72rem;
  grid-template-columns: 38px minmax(0, 1fr) 18px;
  min-block-size: 48px;
  border-radius: 16px;
  padding: 0.3rem 0.72rem 0.3rem 0.35rem;
  color: rgba(var(--v-theme-on-surface), 0.72);
  text-decoration: none;
  overflow: hidden;
  transition:
    color 180ms ease,
    background 180ms ease,
    box-shadow 180ms ease,
    transform 180ms ease;
}

.retail-nav-link::before {
  content: '';
  position: absolute;
  inset-block: 8px;
  inset-inline-start: 0;
  inline-size: 3px;
  border-radius: 999px;
  background: rgb(var(--v-theme-primary));
  opacity: 0;
  transform: scaleY(0.4);
  transition:
    opacity 180ms ease,
    transform 180ms ease;
}

.retail-nav-link__glow {
  position: absolute;
  inset: 0;
  z-index: -1;
  opacity: 0;
  background:
    radial-gradient(circle at 18% 50%, rgba(var(--v-theme-primary), 0.18), transparent 34%),
    linear-gradient(90deg, rgba(var(--v-theme-primary), 0.12), rgba(var(--v-theme-primary), 0.04));
  transition: opacity 180ms ease;
}

.retail-nav-link__icon {
  display: grid;
  place-items: center;
  inline-size: 38px;
  block-size: 38px;
  border-radius: 13px;
  color: rgba(var(--v-theme-on-surface), 0.66);
  background: rgba(var(--v-theme-on-surface), 0.055);
  transition:
    color 180ms ease,
    background 180ms ease,
    box-shadow 180ms ease,
    transform 180ms ease;
}

.retail-nav-link__title {
  overflow: hidden;
  color: inherit;
  font-size: 0.94rem;
  font-weight: 720;
  letter-spacing: -0.012em;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.retail-nav-link__arrow {
  opacity: 0;
  color: rgb(var(--v-theme-primary));
  transform: translateX(-4px);
  transition:
    opacity 180ms ease,
    transform 180ms ease;
}

.retail-nav-link:hover {
  color: rgb(var(--v-theme-on-surface));
  background: rgba(var(--v-theme-on-surface), 0.045);
  transform: translateX(3px);
}

.retail-nav-link:hover .retail-nav-link__icon {
  color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.1);
}

.retail-nav-link:hover .retail-nav-link__arrow {
  opacity: 1;
  transform: translateX(0);
}

.retail-nav-link--active {
  color: rgb(var(--v-theme-primary));
  background:
    linear-gradient(90deg, rgba(var(--v-theme-primary), 0.14), rgba(var(--v-theme-primary), 0.06)),
    rgb(var(--v-theme-surface));
  box-shadow:
    0 12px 30px rgba(var(--v-theme-primary), 0.16),
    inset 0 1px 0 rgba(255, 255, 255, 0.18);
}

.retail-nav-link--active::before {
  opacity: 1;
  transform: scaleY(1);
}

.retail-nav-link--active .retail-nav-link__glow {
  opacity: 1;
}

.retail-nav-link--active .retail-nav-link__icon {
  color: white;
  background:
    radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.42), transparent 28%),
    linear-gradient(135deg, rgb(var(--v-theme-primary)), rgb(var(--v-theme-info)));
  box-shadow: 0 12px 28px rgba(var(--v-theme-primary), 0.32);
}

.retail-nav-link--active .retail-nav-link__arrow {
  opacity: 1;
  transform: translateX(0);
}

@media (max-width: 1279px) {
  .retail-nav {
    padding-inline: 0.5rem;
  }

  .retail-nav-link {
    min-block-size: 46px;
  }
}
</style>