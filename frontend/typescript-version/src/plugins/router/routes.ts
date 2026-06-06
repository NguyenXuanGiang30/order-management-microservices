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
      },
      {
        path: 'pos',
        component: () => import('@/pages/pos.vue'),
      },
      {
        path: 'orders',
        component: () => import('@/pages/orders.vue'),
      },
      {
        path: 'products',
        component: () => import('@/pages/products.vue'),
      },
      {
        path: 'inventory',
        component: () => import('@/pages/inventory.vue'),
      },
      {
        path: 'goods-receipts',
        component: () => import('@/pages/goods-receipts.vue'),
      },
      {
        path: 'customers',
        component: () => import('@/pages/customers.vue'),
      },
      {
        path: 'suppliers',
        component: () => import('@/pages/suppliers.vue'),
      },
      {
        path: 'debts',
        component: () => import('@/pages/debts.vue'),
      },
      {
        path: 'promotions',
        component: () => import('@/pages/promotions.vue'),
      },
      {
        path: 'settings',
        component: () => import('@/pages/settings.vue'),
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
