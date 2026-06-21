<script lang="ts" setup>
import { computed } from 'vue'

import VerticalNavSectionTitle from '@/@layouts/components/VerticalNavSectionTitle.vue'
import { useAuthStore } from '@/stores/auth'
import VerticalNavLink from '@layouts/components/VerticalNavLink.vue'

interface NavLinkItem {
  title: string
  icon: string
  to: string
}

interface NavSection {
  heading: string
  links: NavLinkItem[]
}

const adminSections: NavSection[] = [
  {
    heading: 'Vận hành',
    links: [
      { title: 'Tổng quan', icon: 'ri-dashboard-line', to: '/dashboard' },
      { title: 'POS bán hàng', icon: 'ri-shopping-cart-2-line', to: '/pos' },
      { title: 'Đơn hàng', icon: 'ri-receipt-line', to: '/orders' },
    ],
  },
  {
    heading: 'Kho hàng',
    links: [
      { title: 'Sản phẩm', icon: 'ri-price-tag-3-line', to: '/products' },
      { title: 'Tồn kho', icon: 'ri-archive-line', to: '/inventory' },
      { title: 'Nhập hàng', icon: 'ri-truck-line', to: '/goods-receipts' },
      { title: 'Nhà cung cấp', icon: 'ri-building-4-line', to: '/suppliers' },
    ],
  },
  {
    heading: 'Khách hàng',
    links: [
      { title: 'Khách hàng', icon: 'ri-user-3-line', to: '/customers' },
      { title: 'Công nợ', icon: 'ri-file-list-3-line', to: '/debts' },
      { title: 'Khuyến mãi', icon: 'ri-coupon-3-line', to: '/promotions' },
    ],
  },
  {
    heading: 'Quản trị',
    links: [
      { title: 'Cài đặt & phân quyền', icon: 'ri-settings-3-line', to: '/settings' },
    ],
  },
]

const salesSections: NavSection[] = [
  {
    heading: 'Bán hàng',
    links: [
      { title: 'Trang bán hàng', icon: 'ri-store-2-line', to: '/sales' },
      { title: 'POS bán hàng', icon: 'ri-shopping-cart-2-line', to: '/pos' },
      { title: 'Đơn hàng', icon: 'ri-receipt-line', to: '/orders' },
    ],
  },
  {
    heading: 'Khách hàng',
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
    links: [
      { title: 'Trang kho', icon: 'ri-home-gear-line', to: '/warehouse' },
      { title: 'Tồn kho', icon: 'ri-archive-line', to: '/inventory' },
      { title: 'Sản phẩm', icon: 'ri-price-tag-3-line', to: '/products' },
      { title: 'Nhập hàng', icon: 'ri-truck-line', to: '/goods-receipts' },
      { title: 'Nhà cung cấp', icon: 'ri-building-4-line', to: '/suppliers' },
    ],
  },
]

const authStore = useAuthStore()

const sections = computed(() => {
  if (authStore.user?.role === 'Sales')
    return salesSections

  if (authStore.user?.role === 'Warehouse')
    return warehouseSections

  return adminSections
})
</script>

<template>
  <template
    v-for="section in sections"
    :key="section.heading"
  >
    <VerticalNavSectionTitle
      :item="{
        heading: section.heading,
      }"
    />
    <VerticalNavLink
      v-for="link in section.links"
      :key="link.to"
      :item="link"
    />
  </template>
</template>
