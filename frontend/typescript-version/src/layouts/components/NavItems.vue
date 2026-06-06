<script lang="ts" setup>
import VerticalNavSectionTitle from '@/@layouts/components/VerticalNavSectionTitle.vue'
import VerticalNavLink from '@layouts/components/VerticalNavLink.vue'
</script>

<template>
  <VerticalNavSectionTitle
    :item="{
      heading: 'Vận hành',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Tổng quan',
      icon: 'ri-dashboard-line',
      to: '/dashboard',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'POS bán hàng',
      icon: 'ri-shopping-cart-2-line',
      to: '/pos',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Đơn hàng',
      icon: 'ri-receipt-line',
      to: '/orders',
    }"
  />

  <VerticalNavSectionTitle
    :item="{
      heading: 'Kho hàng',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Sản phẩm',
      icon: 'ri-price-tag-3-line',
      to: '/products',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Tồn kho',
      icon: 'ri-archive-line',
      to: '/inventory',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Nhập hàng',
      icon: 'ri-truck-line',
      to: '/goods-receipts',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Nhà cung cấp',
      icon: 'ri-building-4-line',
      to: '/suppliers',
    }"
  />

  <VerticalNavSectionTitle
    :item="{
      heading: 'Khách hàng',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Khách hàng',
      icon: 'ri-user-3-line',
      to: '/customers',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Công nợ',
      icon: 'ri-file-list-3-line',
      to: '/debts',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Khuyến mãi',
      icon: 'ri-coupon-3-line',
      to: '/promotions',
    }"
  />

  <VerticalNavSectionTitle
    :item="{
      heading: 'Quản trị',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Cài đặt & phân quyền',
      icon: 'ri-settings-3-line',
      to: '/settings',
    }"
  />
</template>
