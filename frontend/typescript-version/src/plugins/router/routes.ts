const ADMIN_ROLES = ['Admin']
const SALES_ROLES = ['Admin', 'Sales']
const WAREHOUSE_ROLES = ['Admin', 'Warehouse']

export const routes = [
  {
    path: '/',
    component: () => import('@/layouts/blank.vue'),
    children: [
      {
        path: '',
        component: () => import('@/pages/landing.vue'),
        meta: { public: true },
      },
      {
        path: 'login',
        component: () => import('@/pages/login.vue'),
        meta: { public: true, unauthenticatedOnly: true },
      },
      {
        path: 'register',
        redirect: '/login',
        meta: { public: true },
      },
      {
        path: '/:pathMatch(.*)*',
        component: () => import('@/pages/[...error].vue'),
        meta: { public: true },
      },
    ],
  },
  {
    path: '/',
    component: () => import('@/layouts/default.vue'),
    children: [
      {
        path: 'dashboard',
        component: () => import('@/pages/dashboard.vue'),
        meta: { allowedRoles: ADMIN_ROLES },
      },
      {
        path: 'sales',
        component: () => import('@/pages/sales.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'warehouse',
        component: () => import('@/pages/warehouse.vue'),
        meta: { allowedRoles: WAREHOUSE_ROLES },
      },
      {
        path: 'pos',
        component: () => import('@/pages/pos.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'orders',
        component: () => import('@/pages/orders.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'products',
        component: () => import('@/pages/products.vue'),
        meta: { allowedRoles: WAREHOUSE_ROLES },
      },
      {
        path: 'inventory',
        component: () => import('@/pages/inventory.vue'),
        meta: { allowedRoles: WAREHOUSE_ROLES },
      },
      {
        path: 'goods-receipts',
        component: () => import('@/pages/goods-receipts.vue'),
        meta: { allowedRoles: WAREHOUSE_ROLES },
      },
      {
        path: 'customers',
        component: () => import('@/pages/customers.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'suppliers',
        component: () => import('@/pages/suppliers.vue'),
        meta: { allowedRoles: WAREHOUSE_ROLES },
      },
      {
        path: 'debts',
        component: () => import('@/pages/debts.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'promotions',
        component: () => import('@/pages/promotions.vue'),
        meta: { allowedRoles: SALES_ROLES },
      },
      {
        path: 'settings',
        component: () => import('@/pages/settings.vue'),
        meta: { allowedRoles: ADMIN_ROLES },
      },
      {
        path: 'tables',
        redirect: '/products',
      },
      {
        path: 'account-settings',
        redirect: '/settings',
      },
    ],
  },
]
