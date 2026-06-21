import { expect, test, type Page } from '@playwright/test'

async function mockLogin(page: Page, role: 'Admin' | 'Sales' | 'Warehouse') {
  await page.route('**/api/auth/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      data: {
        isSuccess: true,
        accessToken: `${role.toLowerCase()}-access-token`,
        refreshToken: `${role.toLowerCase()}-refresh-token`,
        expiresIn: 3600,
        message: 'Đăng nhập thành công.',
        user: {
          id: `${role.toLowerCase()}-user-id`,
          username: role.toLowerCase(),
          fullName: `${role} RetailOps`,
          role,
          avatarUrl: null,
          permissions: [],
        },
      },
    }),
  }))
}

async function mockWorkspaceApis(page: Page) {
  await Promise.all([
    mockApi(page, '**/api/reports/dashboard', {
      today: null,
      currentMonth: null,
      last7Days: [],
    }),
    mockApi(page, '**/api/reports/activity-logs**', pagedResponse()),
    mockApi(page, '**/api/inventory/stock**', []),
    mockApi(page, '**/api/orders**', pagedResponse()),
    mockApi(page, '**/api/customers**', pagedResponse()),
    mockApi(page, '**/api/promotions/active', []),
    mockApi(page, '**/api/shifts/current', null),
    mockApi(page, '**/api/products**', pagedResponse()),
    mockApi(page, '**/api/inventory/receipts**', pagedResponse()),
    mockApi(page, '**/api/suppliers**', pagedResponse()),
  ])
}

async function mockApi(page: Page, url: string, data: unknown) {
  await page.route(url, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data }),
  }))
}

function pagedResponse() {
  return {
    items: [],
    pageNumber: 1,
    pageSize: 1,
    totalCount: 0,
    totalPages: 0,
  }
}

test.describe('Authentication flows', () => {
  test('redirects Admin users to dashboard', async ({ page }) => {
    await mockLogin(page, 'Admin')
    await mockWorkspaceApis(page)
    await page.goto('/login')

    await page.getByLabel('Tên tài khoản').fill('admin')
    await page.locator('input[type="password"]').fill('SuperStrong@Password123')
    await page.getByRole('button', { name: 'Đăng nhập hệ thống' }).click()

    await expect(page).toHaveURL(/\/dashboard$/, { timeout: 15000 })
  })

  test('redirects Sales users to sales workspace', async ({ page }) => {
    await mockLogin(page, 'Sales')
    await mockWorkspaceApis(page)
    await page.goto('/login')
    await expect(page).toHaveURL(/\/login$/)

    await page.getByLabel('Tên tài khoản').fill('sales')
    await page.locator('input[type="password"]').fill('SuperStrong@Password123')
    await page.getByRole('button', { name: 'Đăng nhập hệ thống' }).click()

    await expect.poll(async () => page.evaluate(() => JSON.parse(localStorage.getItem('retailops.auth.session') ?? 'null')?.user?.role)).toBe('Sales')
    await expect(page).toHaveURL(/\/sales$/, { timeout: 15000 })
  })

  test('redirects Warehouse users to warehouse workspace', async ({ page }) => {
    await mockLogin(page, 'Warehouse')
    await mockWorkspaceApis(page)
    await page.goto('/login')

    await page.getByLabel('Tên tài khoản').fill('warehouse')
    await page.locator('input[type="password"]').fill('SuperStrong@Password123')
    await page.getByRole('button', { name: 'Đăng nhập hệ thống' }).click()

    await expect(page).toHaveURL(/\/warehouse$/, { timeout: 15000 })
  })

  test('displays an error message on invalid credentials', async ({ page }) => {
    await page.route('**/api/auth/login', route => route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({
        success: false,
        message: 'Tên đăng nhập hoặc mật khẩu không đúng.',
        errors: [],
      }),
    }))

    await page.goto('/login')
    await page.getByLabel('Tên tài khoản').fill('invalid_user')
    await page.locator('input[type="password"]').fill('WrongPassword123')
    await page.getByRole('button', { name: 'Đăng nhập hệ thống' }).click()

    const alert = page.locator('.v-alert')

    await expect(alert).toBeVisible()
    await expect(alert).toContainText('Tên đăng nhập hoặc mật khẩu không đúng.')
  })
})
