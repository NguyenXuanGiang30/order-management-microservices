<template>
  <div class="h-100 d-flex align-center justify-md-space-between justify-center">
    <span class="d-flex align-center text-medium-emphasis">
      &copy; {{ new Date().getFullYear() }} RetailOps
      <span class="mx-2">/</span>
      Quản lý bán lẻ và kho hàng
    </span>

    <span class="d-md-flex gap-x-4 text-primary d-none">
      <RouterLink to="/settings">
        Phân quyền
      </RouterLink>
      <RouterLink to="/inventory">
        Tồn kho
      </RouterLink>
      <RouterLink to="/goods-receipts">
        Nhập hàng
      </RouterLink>
    </span>
  </div>
</template>
