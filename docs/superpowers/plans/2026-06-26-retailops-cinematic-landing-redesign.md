# RetailOps Cinematic Landing Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the public RetailOps landing page as a dark cinematic command-center showcase with strong background motion, no old white/blue landing treatment, and a clear path into `/login`.

**Architecture:** Keep the authenticated app untouched. Replace `landing.vue` with a composition shell and move visual sections into landing-only components under `src/components/landing/`. Use native Vue, Vuetify, CSS, and SVG-style DOM effects; add no new runtime dependencies.

**Tech Stack:** Vue 3, TypeScript, Vite, Vuetify 3, Remix/Iconify icons, Playwright, CSS animations with `prefers-reduced-motion`.

---

## File Structure

- Create: `frontend/typescript-version/tests/landing.spec.ts`  
  Playwright coverage for the public landing route, CTAs, dark palette, section anchors, and reduced-motion behavior.

- Create: `frontend/typescript-version/src/components/landing/landingContent.ts`  
  Static content arrays for modules, services, roles, metrics, and hero stats. Keeps copy out of the render file.

- Create: `frontend/typescript-version/src/components/landing/CinematicBackground.vue`  
  Landing-only animated background: gradient fields, scanning light, grid layer, signal particles, and reduced-motion fallback.

- Create: `frontend/typescript-version/src/components/landing/HeroCommandPreview.vue`  
  Floating product/dashboard preview used in the hero.

- Create: `frontend/typescript-version/src/components/landing/OperationMap.vue`  
  Non-uniform module map for POS, inventory, orders, debt, reports, and permissions.

- Create: `frontend/typescript-version/src/components/landing/ServiceFlow.vue`  
  Microservices architecture flow with API Gateway, services, SQL Server, and RabbitMQ.

- Create: `frontend/typescript-version/src/components/landing/RoleWorkspaces.vue`  
  Admin, Sales, and Warehouse work modes.

- Create: `frontend/typescript-version/src/components/landing/MetricRail.vue`  
  Believable operational proof/status strip.

- Modify: `frontend/typescript-version/src/pages/landing.vue`  
  Replace the old landing page with the cinematic page shell and imported landing components.

---

### Task 1: Add Failing Playwright Coverage

**Files:**
- Create: `frontend/typescript-version/tests/landing.spec.ts`

- [ ] **Step 1: Write the failing landing tests**

Create `frontend/typescript-version/tests/landing.spec.ts`:

```ts
import { expect, test } from '@playwright/test'

test.describe('Cinematic landing page', () => {
  test('renders the rebuilt command-center landing surface', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByTestId('landing-page')).toBeVisible()
    await expect(page.getByTestId('cinematic-background')).toBeVisible()
    await expect(page.getByRole('heading', {
      name: /RetailOps biến bán hàng, kho và báo cáo thành một trung tâm vận hành sống/i,
    })).toBeVisible()
    await expect(page.getByTestId('hero-preview')).toBeVisible()
    await expect(page.getByRole('link', { name: /Đăng nhập hệ thống/i })).toHaveAttribute('href', '/login')

    const background = await page.getByTestId('landing-page').evaluate(element => getComputedStyle(element).backgroundColor)

    expect(background).not.toBe('rgb(246, 248, 251)')
    expect(background).not.toBe('rgb(255, 255, 255)')
  })

  test('exposes cinematic sections through accessible navigation', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('link', { name: /Hệ vận hành/i })).toHaveAttribute('href', '#operations')
    await expect(page.getByRole('link', { name: /Microservices/i })).toHaveAttribute('href', '#architecture')
    await expect(page.getByRole('link', { name: /Vai trò/i })).toHaveAttribute('href', '#roles')

    await page.getByRole('link', { name: /Microservices/i }).click()
    await expect(page.locator('#architecture')).toBeInViewport()
  })

  test('keeps continuous motion disabled for reduced-motion users', async ({ page }) => {
    await page.emulateMedia({ reducedMotion: 'reduce' })
    await page.goto('/')

    const animatedLayers = page.locator('[data-motion-layer="continuous"]')
    const count = await animatedLayers.count()

    expect(count).toBeGreaterThan(0)

    for (let index = 0; index < count; index++) {
      const animationName = await animatedLayers.nth(index).evaluate(element => getComputedStyle(element).animationName)
      const transitionDuration = await animatedLayers.nth(index).evaluate(element => getComputedStyle(element).transitionDuration)

      expect(animationName).toBe('none')
      expect(transitionDuration).toBe('0s')
    }
  })
})
```

- [ ] **Step 2: Run the failing test**

Run:

```bash
cd frontend/typescript-version
npm run test:e2e -- landing.spec.ts
```

Expected: FAIL because `data-testid="landing-page"`, `data-testid="cinematic-background"`, the new headline, and reduced-motion layers do not exist yet.

- [ ] **Step 3: Commit the failing test**

```bash
git add frontend/typescript-version/tests/landing.spec.ts
git commit -m "test: cover cinematic landing page"
```

---

### Task 2: Add Landing Content Data

**Files:**
- Create: `frontend/typescript-version/src/components/landing/landingContent.ts`

- [ ] **Step 1: Create typed content arrays**

Create `frontend/typescript-version/src/components/landing/landingContent.ts`:

```ts
export interface LandingModule {
  title: string
  text: string
  icon: string
  tone: 'amber' | 'red' | 'violet'
}

export interface LandingService {
  name: string
  role: string
  text: string
}

export interface LandingRole {
  name: 'Admin' | 'Sales' | 'Warehouse'
  title: string
  text: string
  signal: string
}

export interface LandingMetric {
  value: string
  label: string
}

export const landingModules: LandingModule[] = [
  {
    title: 'POS bán hàng',
    text: 'Tạo đơn, chọn khách, áp khuyến mãi và ghi nhận thanh toán ngay tại quầy.',
    icon: 'ri-shopping-cart-2-line',
    tone: 'amber',
  },
  {
    title: 'Tồn kho sống',
    text: 'Theo dõi tồn khả dụng, ngưỡng thấp, giữ hàng và biến động kho theo thời gian.',
    icon: 'ri-archive-line',
    tone: 'violet',
  },
  {
    title: 'Nhập hàng',
    text: 'Lập phiếu nhập, xác nhận hàng về và cập nhật giá vốn qua service kho.',
    icon: 'ri-truck-line',
    tone: 'amber',
  },
  {
    title: 'Đơn hàng',
    text: 'Quản lý trạng thái, thanh toán, trả hàng, hủy đơn và lịch sử xử lý.',
    icon: 'ri-receipt-line',
    tone: 'red',
  },
  {
    title: 'Công nợ',
    text: 'Theo dõi phải thu khách hàng và phải trả nhà cung cấp trong cùng một luồng.',
    icon: 'ri-file-list-3-line',
    tone: 'violet',
  },
  {
    title: 'Báo cáo & quyền',
    text: 'Dashboard, lợi nhuận, audit, thông báo, backup và phân quyền theo vai trò.',
    icon: 'ri-bar-chart-box-line',
    tone: 'amber',
  },
]

export const landingServices: LandingService[] = [
  {
    name: 'Product & Inventory',
    role: 'Stock Core',
    text: 'Sản phẩm, danh mục, tồn kho, nhập hàng và kiểm kê.',
  },
  {
    name: 'Order & Sales',
    role: 'Revenue Core',
    text: 'Đơn hàng, khách hàng, thanh toán, khuyến mãi, ca bán và công nợ.',
  },
  {
    name: 'User & Report',
    role: 'Control Core',
    text: 'JWT, người dùng, báo cáo, thông báo, audit log và backup.',
  },
]

export const landingRoles: LandingRole[] = [
  {
    name: 'Admin',
    title: 'Trạm chỉ huy',
    text: 'Dashboard, báo cáo, phân quyền, audit và cấu hình hệ thống.',
    signal: 'Toàn quyền',
  },
  {
    name: 'Sales',
    title: 'Quầy bán hàng',
    text: 'POS, đơn hàng, khách hàng, khuyến mãi và công nợ phải thu.',
    signal: 'Tốc độ',
  },
  {
    name: 'Warehouse',
    title: 'Kho vận hành',
    text: 'Tồn kho, nhập hàng, kiểm kê, nhà cung cấp và công nợ phải trả.',
    signal: 'Chính xác',
  },
]

export const landingMetrics: LandingMetric[] = [
  { value: '3', label: 'Microservices nghiệp vụ' },
  { value: '14+', label: 'Luồng vận hành bán lẻ' },
  { value: '24/7', label: 'Tín hiệu kho và báo cáo' },
  { value: 'RBAC', label: 'Phân quyền theo vai trò' },
]
```

- [ ] **Step 2: Run typecheck**

Run:

```bash
cd frontend/typescript-version
npm run typecheck
```

Expected: PASS because this file is pure typed exports and has no consumers yet.

- [ ] **Step 3: Commit content data**

```bash
git add frontend/typescript-version/src/components/landing/landingContent.ts
git commit -m "feat: add landing content model"
```

---

### Task 3: Build The Animated Cinematic Background

**Files:**
- Create: `frontend/typescript-version/src/components/landing/CinematicBackground.vue`

- [ ] **Step 1: Create the background component**

Create `frontend/typescript-version/src/components/landing/CinematicBackground.vue`:

```vue
<template>
  <div
    class="cinematic-bg"
    data-testid="cinematic-background"
    aria-hidden="true"
  >
    <div
      class="cinematic-bg__field cinematic-bg__field--amber"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-bg__field cinematic-bg__field--violet"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-bg__grid"
      data-motion-layer="continuous"
    />
    <div
      class="cinematic-bg__sweep"
      data-motion-layer="continuous"
    />
    <div class="cinematic-bg__signals">
      <span
        v-for="index in 18"
        :key="index"
        class="cinematic-bg__signal"
        :style="{ '--signal-index': index }"
        data-motion-layer="continuous"
      />
    </div>
  </div>
</template>

<style scoped>
.cinematic-bg {
  position: fixed;
  inset: 0;
  overflow: hidden;
  background:
    radial-gradient(circle at 18% 18%, rgba(245, 124, 31, 0.22), transparent 30%),
    radial-gradient(circle at 82% 12%, rgba(128, 75, 255, 0.16), transparent 34%),
    linear-gradient(135deg, #06070a 0%, #11131a 48%, #07080c 100%);
  pointer-events: none;
  z-index: 0;
}

.cinematic-bg__field,
.cinematic-bg__grid,
.cinematic-bg__sweep,
.cinematic-bg__signals {
  position: absolute;
  inset: 0;
}

.cinematic-bg__field {
  border-radius: 999px;
  filter: blur(42px);
  opacity: 0.8;
}

.cinematic-bg__field--amber {
  width: min(48vw, 620px);
  height: min(48vw, 620px);
  inset-block-start: -12%;
  inset-inline-end: 7%;
  background: rgba(245, 124, 31, 0.24);
  animation: landingFieldDrift 18s ease-in-out infinite;
}

.cinematic-bg__field--violet {
  width: min(42vw, 520px);
  height: min(42vw, 520px);
  inset-block-end: -18%;
  inset-inline-start: -5%;
  background: rgba(120, 82, 255, 0.18);
  animation: landingFieldDrift 24s ease-in-out infinite reverse;
}

.cinematic-bg__grid {
  background:
    linear-gradient(rgba(255, 182, 93, 0.06) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 182, 93, 0.045) 1px, transparent 1px);
  background-size: 72px 72px;
  mask-image: linear-gradient(to bottom, transparent 0%, black 18%, black 76%, transparent 100%);
  opacity: 0.45;
  transform: perspective(900px) rotateX(58deg) translateY(-18%);
  transform-origin: top center;
  animation: landingGridDrift 16s linear infinite;
}

.cinematic-bg__sweep {
  background: linear-gradient(115deg, transparent 28%, rgba(255, 183, 77, 0.16) 42%, transparent 58%);
  mix-blend-mode: screen;
  opacity: 0.8;
  transform: translateX(-55%);
  animation: landingLightSweep 9s cubic-bezier(0.22, 1, 0.36, 1) infinite;
}

.cinematic-bg__signal {
  position: absolute;
  width: 3px;
  height: 3px;
  border-radius: 999px;
  background: #ffb457;
  box-shadow: 0 0 18px rgba(255, 180, 87, 0.8);
  inset-block-start: calc((var(--signal-index) * 5.1%) + 3%);
  inset-inline-start: calc((var(--signal-index) * 7.3%) - 9%);
  opacity: 0;
  animation: landingSignalRun 7s linear infinite;
  animation-delay: calc(var(--signal-index) * -420ms);
}

@keyframes landingFieldDrift {
  0%,
  100% { transform: translate3d(0, 0, 0) scale(1); }
  50% { transform: translate3d(-4%, 3%, 0) scale(1.08); }
}

@keyframes landingGridDrift {
  from { background-position: 0 0, 0 0; }
  to { background-position: 0 72px, 72px 0; }
}

@keyframes landingLightSweep {
  0% { transform: translateX(-65%); opacity: 0; }
  18% { opacity: 0.82; }
  48% { opacity: 0.34; }
  100% { transform: translateX(75%); opacity: 0; }
}

@keyframes landingSignalRun {
  0% { opacity: 0; transform: translate3d(0, 0, 0); }
  12% { opacity: 0.9; }
  72% { opacity: 0.28; }
  100% { opacity: 0; transform: translate3d(56vw, -24vh, 0); }
}

@media (prefers-reduced-motion: reduce) {
  [data-motion-layer="continuous"] {
    animation: none !important;
    transition-duration: 0s !important;
  }
}
</style>
```

- [ ] **Step 2: Run typecheck**

Run:

```bash
cd frontend/typescript-version
npm run typecheck
```

Expected: PASS.

- [ ] **Step 3: Commit background component**

```bash
git add frontend/typescript-version/src/components/landing/CinematicBackground.vue
git commit -m "feat: add cinematic landing background"
```

---

### Task 4: Build Hero Preview

**Files:**
- Create: `frontend/typescript-version/src/components/landing/HeroCommandPreview.vue`

- [ ] **Step 1: Create the floating command preview**

Create `frontend/typescript-version/src/components/landing/HeroCommandPreview.vue`:

```vue
<template>
  <aside
    class="hero-preview"
    data-testid="hero-preview"
    aria-label="Bản xem trước trung tâm vận hành RetailOps"
  >
    <div class="hero-preview__topline">
      <span>RetailOps Command</span>
      <span class="hero-preview__live">Live</span>
    </div>

    <div class="hero-preview__metrics">
      <div>
        <span>Doanh thu hôm nay</span>
        <strong>12.450.800đ</strong>
      </div>
      <div>
        <span>SKU cần nhập</span>
        <strong>08</strong>
      </div>
    </div>

    <div class="hero-preview__chart" aria-hidden="true">
      <span v-for="height in [42, 64, 38, 78, 58, 92, 74, 88]" :key="height" :style="{ blockSize: `${height}%` }" />
    </div>

    <div class="hero-preview__events">
      <div>
        <span class="hero-preview__dot hero-preview__dot--amber" />
        POS tạo đơn qua Order & Sales
      </div>
      <div>
        <span class="hero-preview__dot hero-preview__dot--violet" />
        RabbitMQ đồng bộ tồn kho
      </div>
      <div>
        <span class="hero-preview__dot hero-preview__dot--red" />
        Low stock alert gửi về dashboard
      </div>
    </div>
  </aside>
</template>

<style scoped>
.hero-preview {
  position: relative;
  overflow: hidden;
  padding: clamp(1rem, 2vw, 1.35rem);
  border: 1px solid rgba(255, 183, 77, 0.22);
  border-radius: 18px;
  background:
    linear-gradient(145deg, rgba(31, 26, 22, 0.92), rgba(14, 15, 22, 0.82)),
    #0b0d12;
  box-shadow: 0 28px 90px rgba(0, 0, 0, 0.42), inset 0 1px 0 rgba(255, 255, 255, 0.06);
  color: #fff3df;
  transform: perspective(1100px) rotateY(-7deg) rotateX(4deg);
  animation: heroPreviewFloat 6s ease-in-out infinite;
}

.hero-preview::before {
  position: absolute;
  inset: 0;
  background: radial-gradient(circle at 72% 6%, rgba(255, 183, 77, 0.2), transparent 34%);
  content: "";
  pointer-events: none;
}

.hero-preview__topline,
.hero-preview__metrics,
.hero-preview__events {
  position: relative;
  z-index: 1;
}

.hero-preview__topline,
.hero-preview__metrics {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
}

.hero-preview__topline {
  margin-block-end: 1rem;
  color: rgba(255, 243, 223, 0.72);
  font-size: 0.78rem;
  font-weight: 800;
}

.hero-preview__live {
  border-radius: 999px;
  background: rgba(245, 124, 31, 0.16);
  color: #ffbd6e;
  padding: 0.25rem 0.65rem;
}

.hero-preview__metrics {
  margin-block-end: 1.25rem;
}

.hero-preview__metrics div {
  min-width: 0;
}

.hero-preview__metrics span {
  display: block;
  color: rgba(255, 243, 223, 0.58);
  font-size: 0.72rem;
}

.hero-preview__metrics strong {
  display: block;
  color: #fff8ec;
  font-size: clamp(1.25rem, 2.2vw, 1.8rem);
  line-height: 1.1;
}

.hero-preview__chart {
  position: relative;
  z-index: 1;
  display: grid;
  min-height: 150px;
  align-items: end;
  gap: 0.55rem;
  grid-template-columns: repeat(8, minmax(12px, 1fr));
  margin-block-end: 1.25rem;
}

.hero-preview__chart span {
  border-radius: 999px 999px 4px 4px;
  background: linear-gradient(180deg, #ffbd6e, rgba(188, 54, 37, 0.62));
  box-shadow: 0 0 28px rgba(255, 183, 77, 0.22);
}

.hero-preview__events {
  display: grid;
  gap: 0.7rem;
  color: rgba(255, 243, 223, 0.72);
  font-size: 0.82rem;
}

.hero-preview__events div {
  display: flex;
  align-items: center;
  gap: 0.55rem;
}

.hero-preview__dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  flex: 0 0 auto;
}

.hero-preview__dot--amber { background: #ffbd6e; }
.hero-preview__dot--violet { background: #9f7aff; }
.hero-preview__dot--red { background: #ff5d4d; }

@keyframes heroPreviewFloat {
  0%,
  100% { transform: perspective(1100px) rotateY(-7deg) rotateX(4deg) translate3d(0, 0, 0); }
  50% { transform: perspective(1100px) rotateY(-5deg) rotateX(3deg) translate3d(0, -10px, 0); }
}

@media (max-width: 900px) {
  .hero-preview {
    transform: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .hero-preview {
    animation: none !important;
    transform: none;
  }
}
</style>
```

- [ ] **Step 2: Run typecheck**

Run:

```bash
cd frontend/typescript-version
npm run typecheck
```

Expected: PASS.

- [ ] **Step 3: Commit hero preview**

```bash
git add frontend/typescript-version/src/components/landing/HeroCommandPreview.vue
git commit -m "feat: add landing command preview"
```

---

### Task 5: Build Story Sections

**Files:**
- Create: `frontend/typescript-version/src/components/landing/OperationMap.vue`
- Create: `frontend/typescript-version/src/components/landing/ServiceFlow.vue`
- Create: `frontend/typescript-version/src/components/landing/RoleWorkspaces.vue`
- Create: `frontend/typescript-version/src/components/landing/MetricRail.vue`

- [ ] **Step 1: Create `OperationMap.vue`**

Use `landingModules` and a non-uniform map layout. Include `id="operations"` on the section root.

```vue
<script setup lang="ts">
import { landingModules } from './landingContent'
</script>

<template>
  <section id="operations" class="landing-section operation-map">
    <div class="landing-section__heading">
      <span>Hệ vận hành</span>
      <h2>Mọi module chạy như một trung tâm điều phối.</h2>
      <p>RetailOps nối POS, kho, đơn hàng, công nợ và báo cáo thành một luồng thao tác có kiểm soát.</p>
    </div>

    <div class="operation-map__grid">
      <article
        v-for="module in landingModules"
        :key="module.title"
        class="operation-map__node"
        :class="`operation-map__node--${module.tone}`"
      >
        <VIcon :icon="module.icon" size="24" />
        <h3>{{ module.title }}</h3>
        <p>{{ module.text }}</p>
      </article>
    </div>
  </section>
</template>
```

- [ ] **Step 2: Create `ServiceFlow.vue`**

Use `landingServices` and include `id="architecture"` on the section root.

```vue
<script setup lang="ts">
import { landingServices } from './landingContent'
</script>

<template>
  <section id="architecture" class="landing-section service-flow">
    <div class="landing-section__heading">
      <span>Microservices</span>
      <h2>Frontend gọi Gateway, nghiệp vụ chạy qua các service độc lập.</h2>
      <p>Kiến trúc thể hiện đúng tinh thần đồ án: phân tách service, database riêng và đồng bộ qua event.</p>
    </div>

    <div class="service-flow__canvas">
      <div class="service-flow__gateway">
        <VIcon icon="ri-route-line" size="28" />
        <strong>API Gateway</strong>
        <small>YARP reverse proxy</small>
      </div>

      <article v-for="service in landingServices" :key="service.name" class="service-flow__service">
        <span>{{ service.role }}</span>
        <h3>{{ service.name }}</h3>
        <p>{{ service.text }}</p>
      </article>

      <div class="service-flow__infra">
        <span>SQL Server</span>
        <span>RabbitMQ</span>
      </div>
    </div>
  </section>
</template>
```

- [ ] **Step 3: Create `RoleWorkspaces.vue`**

Use `landingRoles` and include `id="roles"` on the section root.

```vue
<script setup lang="ts">
import { landingRoles } from './landingContent'
</script>

<template>
  <section id="roles" class="landing-section role-workspaces">
    <div class="landing-section__heading">
      <span>Vai trò</span>
      <h2>Ba workspace, ba nhịp vận hành khác nhau.</h2>
      <p>Admin kiểm soát hệ thống, Sales xử lý quầy bán, Warehouse giữ kho chính xác.</p>
    </div>

    <div class="role-workspaces__grid">
      <article v-for="role in landingRoles" :key="role.name" class="role-workspaces__panel">
        <small>{{ role.signal }}</small>
        <h3>{{ role.name }}</h3>
        <strong>{{ role.title }}</strong>
        <p>{{ role.text }}</p>
      </article>
    </div>
  </section>
</template>
```

- [ ] **Step 4: Create `MetricRail.vue`**

Use `landingMetrics`.

```vue
<script setup lang="ts">
import { landingMetrics } from './landingContent'
</script>

<template>
  <section class="metric-rail" aria-label="Tín hiệu năng lực RetailOps">
    <div v-for="metric in landingMetrics" :key="metric.label" class="metric-rail__item">
      <strong>{{ metric.value }}</strong>
      <span>{{ metric.label }}</span>
    </div>
  </section>
</template>
```

- [ ] **Step 5: Add shared section styles to each component**

Use scoped CSS in each component. Keep these constants consistent:

```css
.landing-section {
  position: relative;
  z-index: 1;
  width: min(1180px, calc(100% - 32px));
  margin-inline: auto;
  padding-block: clamp(4rem, 10vw, 7rem);
}

.landing-section__heading {
  max-width: 760px;
  margin-block-end: 2rem;
}

.landing-section__heading span {
  color: #ffb457;
  font-size: 0.78rem;
  font-weight: 800;
}

.landing-section__heading h2 {
  color: #fff3df;
  font-size: clamp(2rem, 4vw, 3.45rem);
  line-height: 1.04;
  margin-block: 0.75rem 1rem;
}

.landing-section__heading p {
  color: rgba(255, 243, 223, 0.68);
  font-size: 1rem;
  line-height: 1.7;
}
```

- [ ] **Step 6: Run typecheck**

Run:

```bash
cd frontend/typescript-version
npm run typecheck
```

Expected: PASS.

- [ ] **Step 7: Commit story sections**

```bash
git add frontend/typescript-version/src/components/landing/OperationMap.vue frontend/typescript-version/src/components/landing/ServiceFlow.vue frontend/typescript-version/src/components/landing/RoleWorkspaces.vue frontend/typescript-version/src/components/landing/MetricRail.vue
git commit -m "feat: add cinematic landing story sections"
```

---

### Task 6: Replace The Landing Page Shell

**Files:**
- Modify: `frontend/typescript-version/src/pages/landing.vue`

- [ ] **Step 1: Replace `landing.vue` with the new composition shell**

Replace the current old landing implementation with:

```vue
<script setup lang="ts">
import CinematicBackground from '@/components/landing/CinematicBackground.vue'
import HeroCommandPreview from '@/components/landing/HeroCommandPreview.vue'
import MetricRail from '@/components/landing/MetricRail.vue'
import OperationMap from '@/components/landing/OperationMap.vue'
import RoleWorkspaces from '@/components/landing/RoleWorkspaces.vue'
import ServiceFlow from '@/components/landing/ServiceFlow.vue'
</script>

<template>
  <main class="cinematic-landing" data-testid="landing-page">
    <CinematicBackground />

    <header class="cinematic-landing__nav">
      <RouterLink to="/" class="cinematic-landing__brand" aria-label="RetailOps trang chủ">
        <span class="cinematic-landing__brand-mark">
          <VIcon icon="ri-store-2-line" size="20" />
        </span>
        <span>RetailOps</span>
      </RouterLink>

      <nav class="cinematic-landing__links" aria-label="Điều hướng landing">
        <a href="#operations">Hệ vận hành</a>
        <a href="#architecture">Microservices</a>
        <a href="#roles">Vai trò</a>
      </nav>

      <VBtn to="/login" class="cinematic-landing__login" variant="flat" prepend-icon="ri-login-circle-line">
        Đăng nhập
      </VBtn>
    </header>

    <section class="cinematic-landing__hero">
      <div class="cinematic-landing__copy">
        <div class="cinematic-landing__signal">
          <VIcon icon="ri-flashlight-line" size="16" />
          Retail operations command center
        </div>

        <h1>RetailOps biến bán hàng, kho và báo cáo thành một trung tâm vận hành sống.</h1>

        <p>
          POS, đơn hàng, tồn kho, nhập hàng, công nợ, báo cáo và phân quyền được nối trong một hệ thống microservices có API Gateway, SQL Server và RabbitMQ.
        </p>

        <div class="cinematic-landing__actions">
          <VBtn to="/login" size="large" class="cinematic-landing__primary" append-icon="ri-arrow-right-line">
            Đăng nhập hệ thống
          </VBtn>
          <VBtn href="#operations" size="large" variant="outlined" class="cinematic-landing__secondary">
            Xem hệ vận hành
          </VBtn>
        </div>
      </div>

      <HeroCommandPreview />
    </section>

    <MetricRail />
    <OperationMap />
    <ServiceFlow />
    <RoleWorkspaces />

    <section class="cinematic-landing__cta">
      <div>
        <span>Ready signal</span>
        <h2>Vào workspace RetailOps và điều hành cửa hàng như một hệ thống thật.</h2>
      </div>
      <VBtn to="/login" size="large" class="cinematic-landing__primary" append-icon="ri-arrow-right-line">
        Mở RetailOps
      </VBtn>
    </section>
  </main>
</template>
```

- [ ] **Step 2: Add page shell styles in `landing.vue`**

Add scoped styles for `.cinematic-landing`, nav, hero, CTA, responsive behavior, and reduced motion. Use these key decisions:

```css
.cinematic-landing {
  position: relative;
  min-block-size: 100dvh;
  overflow: hidden;
  background-color: #07080c;
  color: #fff3df;
}

.cinematic-landing__nav,
.cinematic-landing__hero,
.cinematic-landing__cta {
  position: relative;
  z-index: 1;
  width: min(1180px, calc(100% - 32px));
  margin-inline: auto;
}

.cinematic-landing__nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 76px;
  gap: 1rem;
}

.cinematic-landing__brand,
.cinematic-landing__links a {
  color: inherit;
  text-decoration: none;
}

.cinematic-landing__brand {
  display: inline-flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 850;
}

.cinematic-landing__brand-mark {
  display: grid;
  width: 38px;
  height: 38px;
  border: 1px solid rgba(255, 180, 87, 0.28);
  border-radius: 10px;
  background: rgba(255, 180, 87, 0.1);
  color: #ffb457;
  place-items: center;
}

.cinematic-landing__links {
  display: flex;
  gap: 1.35rem;
}

.cinematic-landing__links a {
  color: rgba(255, 243, 223, 0.7);
  font-weight: 700;
}

.cinematic-landing__hero {
  display: grid;
  align-items: center;
  min-height: calc(100dvh - 76px);
  padding-block: clamp(3rem, 8vw, 6rem);
  gap: clamp(2rem, 6vw, 5rem);
  grid-template-columns: minmax(0, 1.05fr) minmax(320px, 0.78fr);
}

.cinematic-landing__signal {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  border: 1px solid rgba(255, 180, 87, 0.22);
  border-radius: 999px;
  background: rgba(255, 180, 87, 0.08);
  color: #ffb457;
  gap: 0.45rem;
  padding: 0.45rem 0.8rem;
  font-size: 0.78rem;
  font-weight: 800;
}

.cinematic-landing__copy h1 {
  max-width: 790px;
  color: #fff8ec;
  font-size: clamp(3rem, 7vw, 5.8rem);
  font-weight: 900;
  letter-spacing: 0;
  line-height: 0.96;
  margin-block: 1.2rem 1.4rem;
  text-wrap: balance;
}

.cinematic-landing__copy p {
  max-width: 650px;
  color: rgba(255, 243, 223, 0.72);
  font-size: clamp(1rem, 1.8vw, 1.18rem);
  line-height: 1.72;
}

.cinematic-landing__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.85rem;
  margin-block-start: 2rem;
}

.cinematic-landing__primary {
  background: linear-gradient(135deg, #ffb457, #bc3625) !important;
  color: #1b0d06 !important;
  font-weight: 850;
}

.cinematic-landing__secondary {
  border-color: rgba(255, 180, 87, 0.32) !important;
  color: #fff3df !important;
}

.cinematic-landing__cta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border: 1px solid rgba(255, 180, 87, 0.2);
  border-radius: 18px;
  background: linear-gradient(145deg, rgba(31, 26, 22, 0.94), rgba(12, 13, 18, 0.92));
  gap: 2rem;
  margin-block: 3rem 5rem;
  padding: clamp(1.5rem, 4vw, 2.4rem);
}

.cinematic-landing__cta span {
  color: #ffb457;
  font-weight: 850;
}

.cinematic-landing__cta h2 {
  max-width: 780px;
  color: #fff8ec;
  font-size: clamp(1.8rem, 3.4vw, 3rem);
  line-height: 1.08;
  margin-block: 0.6rem 0;
}

@media (max-width: 980px) {
  .cinematic-landing__hero {
    min-height: auto;
    grid-template-columns: 1fr;
  }

  .cinematic-landing__links {
    display: none;
  }
}

@media (max-width: 640px) {
  .cinematic-landing__nav {
    min-height: 68px;
  }

  .cinematic-landing__copy h1 {
    font-size: clamp(2.35rem, 14vw, 3.3rem);
  }

  .cinematic-landing__cta {
    align-items: flex-start;
    flex-direction: column;
  }
}
```

- [ ] **Step 3: Run typecheck and the landing test**

Run:

```bash
cd frontend/typescript-version
npm run typecheck
npm run test:e2e -- landing.spec.ts
```

Expected: typecheck PASS. Landing tests may still fail if story section styles or reduced-motion continuous layers need polish.

- [ ] **Step 4: Commit the landing shell**

```bash
git add frontend/typescript-version/src/pages/landing.vue
git commit -m "feat: rebuild landing page shell"
```

---

### Task 7: Visual Polish, Responsiveness, And Motion Hardening

**Files:**
- Modify: `frontend/typescript-version/src/components/landing/OperationMap.vue`
- Modify: `frontend/typescript-version/src/components/landing/ServiceFlow.vue`
- Modify: `frontend/typescript-version/src/components/landing/RoleWorkspaces.vue`
- Modify: `frontend/typescript-version/src/components/landing/MetricRail.vue`
- Modify: `frontend/typescript-version/src/pages/landing.vue`

- [ ] **Step 1: Finish section-specific CSS**

Add polished CSS to:

- make `OperationMap` non-uniform with a larger first node and staggered rails;
- make `ServiceFlow` show a gateway core and services in a connected canvas;
- make `RoleWorkspaces` read as three command scenes instead of simple cards;
- make `MetricRail` compact, dark, and cinematic;
- avoid nested card styling and avoid white surfaces.

Use these shared color constants directly in component CSS:

```css
--landing-bg: #07080c;
--landing-surface: rgba(20, 18, 20, 0.82);
--landing-border: rgba(255, 180, 87, 0.18);
--landing-ink: #fff8ec;
--landing-muted: rgba(255, 243, 223, 0.68);
--landing-amber: #ffb457;
--landing-red: #bc3625;
--landing-violet: #9f7aff;
```

- [ ] **Step 2: Run Playwright in desktop and inspect screenshots**

Run:

```bash
cd frontend/typescript-version
npm run test:e2e -- landing.spec.ts
```

Expected: PASS.

Open the generated Playwright report only if a failure occurs:

```bash
cd frontend/typescript-version
npx playwright show-report
```

- [ ] **Step 3: Manually inspect responsive behavior**

Run dev server:

```bash
cd frontend/typescript-version
npm run dev -- --host 127.0.0.1 --port 8080
```

Check:

- `http://127.0.0.1:8080/` at desktop width.
- `http://127.0.0.1:8080/` at mobile width around 390px.
- No text overflow.
- CTA visible in the first viewport.
- A hint of the next section visible after hero content on normal desktop.
- Background motion is visible on normal motion settings.
- Continuous motion stops when reduced motion is enabled.

- [ ] **Step 4: Commit polish**

```bash
git add frontend/typescript-version/src/components/landing frontend/typescript-version/src/pages/landing.vue
git commit -m "style: polish cinematic landing motion"
```

---

### Task 8: Final Build Verification

**Files:**
- Verify only; no planned source edits unless a verification failure identifies a specific bug.

- [ ] **Step 1: Run typecheck**

```bash
cd frontend/typescript-version
npm run typecheck
```

Expected: PASS.

- [ ] **Step 2: Run production build**

```bash
cd frontend/typescript-version
npm run build
```

Expected: PASS with Vite build output and no TypeScript errors.

- [ ] **Step 3: Run landing e2e test**

```bash
cd frontend/typescript-version
npm run test:e2e -- landing.spec.ts
```

Expected: PASS.

- [ ] **Step 4: Review changed files**

```bash
git status --short
git diff --stat
```

Expected: only landing page, landing components, and landing test files are changed for implementation. Pre-existing unrelated worktree changes may still appear and should not be reverted.

- [ ] **Step 5: Commit verification fixes if any were needed**

If source fixes were required during verification:

```bash
git add frontend/typescript-version/src/components/landing frontend/typescript-version/src/pages/landing.vue frontend/typescript-version/tests/landing.spec.ts
git commit -m "fix: stabilize cinematic landing verification"
```

If no fixes were required, do not create an empty commit.

---

## Self-Review

Spec coverage:

- Dark cinematic palette: Tasks 3, 6, and 7.
- Background motion beyond cards/popups: Task 3.
- Full landing rebuild: Task 6.
- Microservices and role storytelling: Task 5.
- No old white/blue dominant treatment: Tasks 1, 6, and 7.
- Reduced motion: Tasks 1, 3, 6, and 7.
- Verification: Task 8.

The plan intentionally avoids authenticated app shell changes and global Vuetify theme changes.
