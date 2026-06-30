<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import bgVideo from '@/assets/images/video/retailops-bg.mp4'
import { AuthApiError } from '@/services/authApi'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const form = ref({
  username: '',
  password: '',
  remember: true,
})

const isPasswordVisible = ref(false)
const errorMessage = ref('')

const roleHomePath = (role: string) => {
  if (role === 'Sales')
    return '/sales'

  if (role === 'Warehouse')
    return '/warehouse'

  return '/dashboard'
}

const submitLogin = async () => {
  errorMessage.value = ''

  const username = form.value.username.trim()
  const password = form.value.password

  if (!username || !password) {
    errorMessage.value = 'Vui lòng nhập tên tài khoản và mật khẩu.'

    return
  }

  try {
    const session = await authStore.login({
      username,
      password,
      remember: form.value.remember,
    })

    const redirectPath
      = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/')
        ? route.query.redirect
        : roleHomePath(session.user.role)

    await router.push(redirectPath)
  }
  catch (error) {
    errorMessage.value
      = error instanceof AuthApiError
        ? error.message
        : 'Không thể đăng nhập. Vui lòng kiểm tra kết nối API và thử lại.'
  }
}

const topProducts = [
  { name: 'Tai nghe Bluetooth', value: 88, total: '1.250' },
  { name: 'Loa di động', value: 72, total: '980' },
  { name: 'Sạc dự phòng', value: 58, total: '870' },
  { name: 'Đồng hồ thông minh', value: 46, total: '650' },
]

const staff = [
  { name: 'Nguyễn Minh', score: '98%', value: 98 },
  { name: 'Trần Thu Hà', score: '95%', value: 95 },
  { name: 'Lê Anh Tuấn', score: '92%', value: 92 },
]
</script>

<template>
  <main class="cinema-auth">
    <!-- Background video -->
    <video :src="bgVideo" 
      class="cinema-auth__video"
      autoplay
      muted
      loop
      playsinline
    />

    <!-- Cinematic overlays -->
    <div class="cinema-auth__shade" />
    <div class="cinema-auth__grain" />
    <div class="cinema-auth__aurora cinema-auth__aurora--one" />
    <div class="cinema-auth__aurora cinema-auth__aurora--two" />

    <!-- Floating system dashboard decoration -->
    <section class="floating-dashboard d-none d-md-block">
      <div class="dash-line dash-line--one" />
      <div class="dash-line dash-line--two" />

      <div class="holo-card metric-card metric-card--revenue">
        <div>
          <p>Doanh thu hôm nay</p>
          <strong>2.028.125 đ</strong>
          <span class="positive">↑ 12.5% so với hôm qua</span>
        </div>
        <div class="metric-icon">
          <VIcon icon="ri-money-dollar-circle-line" />
        </div>
      </div>

      <div class="holo-card metric-card metric-card--orders">
        <div>
          <p>Đơn hàng hôm nay</p>
          <strong>5</strong>
          <span class="positive">↑ 2 đơn mới</span>
        </div>
        <div class="metric-icon cyan">
          <VIcon icon="ri-shopping-cart-2-line" />
        </div>
      </div>

      <div class="holo-card metric-card metric-card--customers">
        <div>
          <p>Khách hàng mới</p>
          <strong>24</strong>
          <span class="positive">↑ 8 khách mới</span>
        </div>
        <div class="metric-icon mint">
          <VIcon icon="ri-user-add-line" />
        </div>
      </div>

      <div class="holo-card chart-card chart-card--line">
        <div class="dash-card-head">
          <span>Doanh thu theo ngày</span>
          <VIcon icon="ri-line-chart-line" />
        </div>

        <div class="fake-chart">
          <span style="--h: 32%" />
          <span style="--h: 48%" />
          <span style="--h: 42%" />
          <span style="--h: 60%" />
          <span style="--h: 52%" />
          <span style="--h: 78%" />
          <span style="--h: 65%" />
          <span style="--h: 88%" />
        </div>
      </div>

      <div class="holo-card donut-card">
        <div class="dash-card-head">
          <span>Tỷ lệ kênh bán hàng</span>
          <VIcon icon="ri-pie-chart-2-line" />
        </div>

        <div class="donut-content">
          <div class="donut-ring">
            <span>68%</span>
          </div>

          <div class="donut-list">
            <p><i class="dot online" /> Online <b>68%</b></p>
            <p><i class="dot pos" /> POS <b>24%</b></p>
            <p><i class="dot other" /> Khác <b>8%</b></p>
          </div>
        </div>
      </div>

      <div class="holo-card product-card">
        <div class="dash-card-head">
          <span>Top sản phẩm bán chạy</span>
          <VIcon icon="ri-trophy-line" />
        </div>

        <div
          v-for="(item, index) in topProducts"
          :key="item.name"
          class="product-row"
        >
          <span>{{ index + 1 }}</span>
          <p>{{ item.name }}</p>
          <div class="mini-bar">
            <i :style="{ inlineSize: `${item.value}%` }" />
          </div>
          <b>{{ item.total }}</b>
        </div>
      </div>

      <div class="holo-card inventory-card">
        <VIcon icon="ri-archive-line" />
        <p>Tồn kho</p>
        <strong>1.248</strong>
        <span>36 sản phẩm sắp hết hàng</span>
      </div>

      <div class="holo-card staff-card">
        <div class="dash-card-head">
          <span>Hiệu suất nhân viên</span>
          <VIcon icon="ri-team-line" />
        </div>

        <div
          v-for="person in staff"
          :key="person.name"
          class="staff-row"
        >
          <div class="avatar-dot">
            {{ person.name.charAt(0) }}
          </div>
          <div>
            <p>{{ person.name }}</p>
            <div class="staff-progress">
              <i :style="{ inlineSize: `${person.value}%` }" />
            </div>
          </div>
          <b>{{ person.score }}</b>
        </div>
      </div>

      <div class="kpi-strip">
        <div class="kpi-chip">
          <span>Lợi nhuận</span>
          <strong>+15.6%</strong>
        </div>
        <div class="kpi-chip">
          <span>Chuyển đổi</span>
          <strong>3.24%</strong>
        </div>
        <div class="kpi-chip">
          <span>Đơn TB</span>
          <strong>402.000 đ</strong>
        </div>
      </div>
    </section>

    <!-- Login form -->
    <section class="login-stage">
      <VCard class="login-card" theme="dark">
        <div class="login-brand">
          <VAvatar
            rounded="lg"
            size="54"
            class="login-brand__icon"
          >
            <VIcon
              icon="ri-store-2-line"
              size="28"
            />
          </VAvatar>

          <div>
            <h1>RETAIL<span>OPS</span></h1>
            <p>Nền tảng quản lý bán lẻ</p>
          </div>
        </div>

        <div class="login-heading">
          <h2>Đăng nhập</h2>
          <p>Truy cập không gian vận hành của bạn</p>
        </div>

        <VForm @submit.prevent="submitLogin">
          <Transition name="error-slide">
            <VAlert
              v-if="errorMessage"
              type="error"
              variant="tonal"
              density="comfortable"
              class="mb-5"
            >
              {{ errorMessage }}
            </VAlert>
          </Transition>

          <VTextField
            v-model="form.username"
            label="Tên tài khoản"
            prepend-inner-icon="ri-user-line"
            autocomplete="username"
            variant="outlined"
            density="comfortable"
            :disabled="authStore.loading"
            class="cinema-field mb-4"
          />

          <VTextField
            v-model="form.password"
            label="Mật khẩu"
            :type="isPasswordVisible ? 'text' : 'password'"
            autocomplete="current-password"
            prepend-inner-icon="ri-lock-line"
            :append-inner-icon="isPasswordVisible ? 'ri-eye-off-line' : 'ri-eye-line'"
            variant="outlined"
            density="comfortable"
            :disabled="authStore.loading"
            class="cinema-field mb-4"
            @click:append-inner="isPasswordVisible = !isPasswordVisible"
          />

          <div class="login-options">
            <VCheckbox
              v-model="form.remember"
              label="Ghi nhớ đăng nhập"
              density="compact"
              hide-details
              :disabled="authStore.loading"
            />

            <a>Quên mật khẩu?</a>
          </div>

          <VBtn
            block
            type="submit"
            size="x-large"
            class="login-button"
            :loading="authStore.loading"
            :disabled="authStore.loading"
            append-icon="ri-arrow-right-line"
          >
            Đăng nhập
          </VBtn>
        </VForm>

        <div class="secure-line">
          <span />
          <VIcon icon="ri-shield-check-line" />
          <span />
        </div>

        <p class="login-footer">
          RetailOps — trung tâm vận hành bán lẻ trong thời gian thực
        </p>
      </VCard>
    </section>
  </main>
</template>

<style scoped>
.cinema-auth {
  position: relative;
  isolation: isolate;
  min-block-size: 100dvh;
  overflow: hidden;
  background: #020617;
  color: white;
}

.cinema-auth__video {
  position: absolute;
  inset: 0;
  z-index: -5;
  inline-size: 100%;
  block-size: 100%;
  object-fit: cover;
  opacity: 0.95;
}

.cinema-auth__shade {
  position: absolute;
  inset: 0;
  z-index: -4;
  background:
    radial-gradient(circle at 78% 16%, rgba(134, 255, 221, 0.34), transparent 30%),
    radial-gradient(circle at 30% 72%, rgba(37, 99, 235, 0.24), transparent 38%),
    linear-gradient(90deg, rgba(2, 6, 23, 0.88) 0%, rgba(2, 6, 23, 0.52) 48%, rgba(2, 6, 23, 0.22) 100%);
}

.cinema-auth__grain {
  position: absolute;
  inset: 0;
  z-index: -3;
  opacity: 0.22;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.035) 1px, transparent 1px);
  background-size: 42px 42px;
  mask-image: radial-gradient(circle at center, black 0%, transparent 78%);
}

.cinema-auth__aurora {
  position: absolute;
  z-index: -2;
  border-radius: 999px;
  filter: blur(42px);
  opacity: 0.5;
  pointer-events: none;
}

.cinema-auth__aurora--one {
  inline-size: 420px;
  block-size: 420px;
  inset-block-start: 8%;
  inset-inline-end: 8%;
  background: rgba(45, 212, 191, 0.34);
  animation: auroraFloat 12s ease-in-out infinite;
}

.cinema-auth__aurora--two {
  inline-size: 340px;
  block-size: 340px;
  inset-block-end: -8%;
  inset-inline-start: 28%;
  background: rgba(37, 99, 235, 0.28);
  animation: auroraFloat 16s ease-in-out infinite reverse;
}

.login-stage {
  position: relative;
  z-index: 3;
  display: flex;
  align-items: center;
  min-block-size: 100dvh;
  padding: clamp(1.5rem, 4vw, 5rem);
}

.login-card {
  inline-size: min(100%, 560px);
  padding: clamp(1.5rem, 3vw, 2.4rem);
  border: 1px solid rgba(125, 211, 252, 0.32);
  border-radius: 32px;
  background:
    linear-gradient(145deg, rgba(15, 23, 42, 0.78), rgba(8, 13, 28, 0.58)),
    rgba(2, 6, 23, 0.56);
  box-shadow:
    0 28px 90px rgba(0, 0, 0, 0.55),
    inset 0 1px 0 rgba(255, 255, 255, 0.12),
    0 0 80px rgba(34, 211, 238, 0.16);
  backdrop-filter: blur(28px);
  animation: loginEnter 0.8s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.login-brand {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-block-end: 3.2rem;
}

.login-brand__icon {
  color: white;
  background:
    radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.45), transparent 26%),
    linear-gradient(135deg, #2f7bff, #0759e6);
  box-shadow:
    0 16px 42px rgba(37, 99, 235, 0.44),
    inset 0 1px 0 rgba(255, 255, 255, 0.28);
}

.login-brand h1 {
  margin: 0;
  font-size: 1.6rem;
  font-weight: 900;
  letter-spacing: 0.05em;
  line-height: 1;
}

.login-brand h1 span {
  color: #3b82f6;
}

.login-brand p {
  margin: 6px 0 0;
  color: rgba(226, 232, 240, 0.56);
  font-size: 0.9rem;
}

.login-heading {
  margin-block-end: 2rem;
  text-align: center;
}

.login-heading h2 {
  margin: 0;
  color: #fff;
  font-size: clamp(2rem, 4vw, 2.7rem);
  font-weight: 900;
  letter-spacing: -0.045em;
  text-shadow: 0 0 28px rgba(255, 255, 255, 0.22);
}

.login-heading p {
  margin: 0.7rem 0 0;
  color: rgba(226, 232, 240, 0.74);
  font-size: 1.05rem;
}

.cinema-field :deep(.v-field) {
  border-radius: 16px;
  background: rgba(2, 6, 23, 0.36);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
}

.cinema-field :deep(.v-field__outline) {
  color: rgba(148, 163, 184, 0.7);
}

.cinema-field :deep(.v-label),
.cinema-field :deep(.v-field__input),
.cinema-field :deep(.v-icon) {
  color: rgba(241, 245, 249, 0.82);
}

.cinema-field :deep(input:-webkit-autofill),
.cinema-field :deep(input:-webkit-autofill:hover),
.cinema-field :deep(input:-webkit-autofill:focus),
.cinema-field :deep(input:-webkit-autofill:active) {
  -webkit-text-fill-color: rgba(241, 245, 249, 0.82) !important;
  transition: background-color 5000000s ease-in-out 0s !important;
  caret-color: #fff !important;
}

.login-options {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-block: 0.6rem 1.7rem;
}

.login-options :deep(.v-label) {
  color: rgba(241, 245, 249, 0.86);
}

.login-options a {
  color: #60a5fa;
  font-size: 0.95rem;
  text-decoration: none;
  cursor: pointer;
}

.login-button {
  min-block-size: 62px;
  border-radius: 16px;
  color: white !important;
  font-size: 1.08rem;
  font-weight: 800;
  letter-spacing: 0.01em;
  background:
    linear-gradient(90deg, #0f62fe 0%, #2563eb 48%, #00a6ff 100%) !important;
  box-shadow:
    0 20px 48px rgba(37, 99, 235, 0.48),
    inset 0 1px 0 rgba(255, 255, 255, 0.35);
  overflow: hidden;
}

.login-button::after {
  content: '';
  position: absolute;
  inset-block: 0;
  inset-inline-start: -35%;
  inline-size: 26%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.45), transparent);
  transform: skewX(-18deg);
  animation: buttonShine 3.2s ease-in-out infinite;
}

.secure-line {
  display: grid;
  align-items: center;
  gap: 16px;
  grid-template-columns: 1fr auto 1fr;
  margin-block: 2.2rem 1.2rem;
  color: rgba(203, 213, 225, 0.68);
}

.secure-line span {
  block-size: 1px;
  background: linear-gradient(90deg, transparent, rgba(148, 163, 184, 0.45), transparent);
}

.login-footer {
  margin: 0;
  color: rgba(226, 232, 240, 0.58);
  text-align: center;
}

/* Floating dashboard */
.floating-dashboard {
  position: absolute;
  z-index: 2;
  inset-block-start: 8%;
  inset-inline: 42px 3.5%;
  block-size: 84%;
  pointer-events: none;
  perspective: 1400px;
}

.holo-card {
  position: absolute;
  border: 1px solid rgba(103, 232, 249, 0.22);
  border-radius: 22px;
  background:
    linear-gradient(145deg, rgba(15, 23, 42, 0.72), rgba(8, 47, 73, 0.42)),
    rgba(2, 6, 23, 0.36);
  box-shadow:
    0 24px 70px rgba(0, 0, 0, 0.36),
    inset 0 1px 0 rgba(255, 255, 255, 0.13),
    0 0 38px rgba(34, 211, 238, 0.12);
  backdrop-filter: blur(22px);
  transform-style: preserve-3d;
}

.metric-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  inline-size: 270px;
  min-block-size: 116px;
  padding: 22px;
  animation: cardFloat 8s ease-in-out infinite;
}

.metric-card p,
.chart-card p,
.inventory-card p {
  margin: 0;
  color: rgba(226, 232, 240, 0.82);
  font-size: 0.88rem;
  font-weight: 700;
}

.metric-card strong {
  display: block;
  margin-block: 6px 5px;
  color: white;
  font-size: 1.75rem;
  font-weight: 900;
  letter-spacing: -0.04em;
}

.positive {
  color: #4ade80;
  font-size: 0.82rem;
  font-weight: 800;
}

.metric-icon {
  display: grid;
  place-items: center;
  inline-size: 54px;
  block-size: 54px;
  border-radius: 16px;
  color: white;
  background: linear-gradient(135deg, #2563eb, #0ea5e9);
  box-shadow: 0 14px 34px rgba(37, 99, 235, 0.4);
}

.metric-icon.cyan {
  background: linear-gradient(135deg, #0ea5e9, #22d3ee);
}

.metric-icon.mint {
  background: linear-gradient(135deg, #2dd4bf, #86efac);
}

.metric-card--revenue {
  inset-block-start: 2%;
  inset-inline-start: 43%;
  transform: rotateY(-10deg) rotateX(4deg);
}

.metric-card--orders {
  inset-block-start: 7%;
  inset-inline-start: 62%;
  transform: rotateY(-8deg) rotateX(5deg);
  animation-delay: -1.2s;
}

.metric-card--customers {
  inset-block-start: 11%;
  inset-inline-start: 79%;
  transform: rotateY(-12deg) rotateX(7deg);
  animation-delay: -2s;
}

.chart-card {
  inline-size: 410px;
  min-block-size: 214px;
  padding: 22px;
  animation: cardFloat 9s ease-in-out infinite;
}

.chart-card--line {
  inset-block-start: 24%;
  inset-inline-start: 43%;
  transform: rotateY(-12deg) rotateX(5deg);
}

.dash-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: white;
  font-weight: 800;
  margin-block-end: 18px;
}

.fake-chart {
  display: flex;
  align-items: end;
  gap: 16px;
  block-size: 126px;
  padding-block-start: 18px;
  border-block-end: 1px solid rgba(148, 163, 184, 0.22);
  background:
    linear-gradient(rgba(148, 163, 184, 0.09) 1px, transparent 1px);
  background-size: 100% 32px;
}

.fake-chart span {
  position: relative;
  flex: 1;
  block-size: var(--h);
  border-radius: 999px 999px 0 0;
  background: linear-gradient(180deg, #22d3ee, rgba(34, 211, 238, 0.12));
  box-shadow: 0 0 20px rgba(34, 211, 238, 0.32);
}

.fake-chart span::after {
  content: '';
  position: absolute;
  inset-inline: 50%;
  inset-block-start: -5px;
  inline-size: 9px;
  block-size: 9px;
  border-radius: 50%;
  background: #67e8f9;
  transform: translateX(-50%);
  box-shadow: 0 0 18px #22d3ee;
}

.donut-card {
  inset-block-start: 27%;
  inset-inline-start: 69%;
  inline-size: 300px;
  min-block-size: 210px;
  padding: 22px;
  transform: rotateY(-14deg) rotateX(6deg);
  animation: cardFloat 8.4s ease-in-out infinite -1.4s;
}

.donut-content {
  display: flex;
  align-items: center;
  gap: 20px;
}

.donut-ring {
  display: grid;
  place-items: center;
  inline-size: 118px;
  block-size: 118px;
  border-radius: 50%;
  background:
    radial-gradient(circle, rgba(15, 23, 42, 0.95) 0 42%, transparent 43%),
    conic-gradient(#34d399 0 68%, #6366f1 68% 92%, #38bdf8 92% 100%);
  box-shadow: 0 0 34px rgba(52, 211, 153, 0.24);
}

.donut-ring span {
  color: white;
  font-size: 1.35rem;
  font-weight: 900;
}

.donut-list {
  flex: 1;
}

.donut-list p {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin: 10px 0;
  color: rgba(226, 232, 240, 0.78);
  font-size: 0.8rem;
}

.dot {
  inline-size: 8px;
  block-size: 8px;
  border-radius: 50%;
}

.dot.online {
  background: #34d399;
}

.dot.pos {
  background: #6366f1;
}

.dot.other {
  background: #38bdf8;
}

.product-card {
  inset-block-start: 56%;
  inset-inline-start: 43%;
  inline-size: 330px;
  padding: 22px;
  transform: rotateY(-10deg) rotateX(5deg);
  animation: cardFloat 8.8s ease-in-out infinite -0.8s;
}

.product-row {
  display: grid;
  align-items: center;
  gap: 10px;
  grid-template-columns: 18px 1fr 70px 46px;
  margin-block: 12px;
  color: rgba(226, 232, 240, 0.84);
  font-size: 0.78rem;
}

.product-row p {
  margin: 0;
}

.product-row b {
  color: white;
  font-weight: 800;
}

.mini-bar {
  overflow: hidden;
  block-size: 7px;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.2);
}

.mini-bar i {
  display: block;
  block-size: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #2563eb, #22d3ee);
  box-shadow: 0 0 14px rgba(34, 211, 238, 0.3);
}

.inventory-card {
  inset-block-start: 58%;
  inset-inline-start: 66%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  inline-size: 170px;
  min-block-size: 170px;
  text-align: center;
  transform: rotateY(-12deg) rotateX(5deg);
  animation: cardFloat 7.6s ease-in-out infinite -2.2s;
}

.inventory-card .v-icon {
  color: #fbbf24;
  font-size: 2rem;
}

.inventory-card strong {
  color: white;
  font-size: 2rem;
  font-weight: 900;
  letter-spacing: -0.04em;
}

.inventory-card span {
  color: #fb923c;
  font-size: 0.78rem;
  font-weight: 800;
}

.staff-card {
  inset-block-start: 56%;
  inset-inline-start: 78%;
  inline-size: 250px;
  padding: 20px;
  transform: rotateY(-13deg) rotateX(6deg);
  animation: cardFloat 9.2s ease-in-out infinite -1.8s;
}

.staff-row {
  display: grid;
  align-items: center;
  gap: 10px;
  grid-template-columns: 34px 1fr 38px;
  margin-block: 12px;
}

.avatar-dot {
  display: grid;
  place-items: center;
  inline-size: 34px;
  block-size: 34px;
  border-radius: 50%;
  color: white;
  font-weight: 900;
  background: linear-gradient(135deg, #2563eb, #22d3ee);
}

.staff-row p {
  margin: 0 0 5px;
  color: rgba(226, 232, 240, 0.9);
  font-size: 0.78rem;
  font-weight: 700;
}

.staff-row b {
  color: white;
  font-size: 0.78rem;
}

.staff-progress {
  overflow: hidden;
  block-size: 6px;
  border-radius: 999px;
  background: rgba(148, 163, 184, 0.22);
}

.staff-progress i {
  display: block;
  block-size: 100%;
  border-radius: inherit;
  background: linear-gradient(90deg, #34d399, #22d3ee);
}

.kpi-strip {
  position: absolute;
  inset-block-end: 5%;
  inset-inline-start: 45%;
  display: flex;
  gap: 14px;
  transform: rotateY(-12deg) rotateX(5deg);
}

.kpi-chip {
  min-inline-size: 150px;
  padding: 16px 18px;
  border: 1px solid rgba(103, 232, 249, 0.18);
  border-radius: 18px;
  background: rgba(8, 47, 73, 0.42);
  box-shadow: 0 18px 44px rgba(0, 0, 0, 0.28);
  backdrop-filter: blur(18px);
  animation: cardFloat 7.4s ease-in-out infinite -0.6s;
}

.kpi-chip span {
  display: block;
  color: rgba(226, 232, 240, 0.68);
  font-size: 0.78rem;
}

.kpi-chip strong {
  display: block;
  margin-block-start: 6px;
  color: #4ade80;
  font-size: 1.25rem;
  font-weight: 900;
}

.dash-line {
  position: absolute;
  border: 1px solid rgba(250, 204, 21, 0.2);
  border-radius: 50%;
  filter: drop-shadow(0 0 18px rgba(250, 204, 21, 0.2));
}

.dash-line--one {
  inline-size: 780px;
  block-size: 260px;
  inset-block-start: 34%;
  inset-inline-start: 39%;
  transform: rotate(-10deg);
}

.dash-line--two {
  inline-size: 580px;
  block-size: 200px;
  inset-block-start: 50%;
  inset-inline-start: 54%;
  transform: rotate(18deg);
}

/* Motion */
@keyframes loginEnter {
  from {
    opacity: 0;
    transform: translateX(-34px) scale(0.98);
  }

  to {
    opacity: 1;
    transform: translateX(0) scale(1);
  }
}

@keyframes cardFloat {
  0%, 100% {
    translate: 0 0;
  }

  50% {
    translate: 0 -16px;
  }
}

@keyframes auroraFloat {
  0%, 100% {
    transform: translate3d(0, 0, 0) scale(1);
  }

  50% {
    transform: translate3d(28px, -22px, 0) scale(1.08);
  }
}

@keyframes buttonShine {
  0%, 45% {
    transform: translateX(0) skewX(-18deg);
  }

  70%, 100% {
    transform: translateX(520%) skewX(-18deg);
  }
}

.error-slide-enter-active {
  transition: opacity 0.28s ease, transform 0.28s cubic-bezier(0.22, 1, 0.36, 1);
}

.error-slide-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
}

.error-slide-enter-from,
.error-slide-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* Responsive */
@media (max-width: 1279px) {
  .floating-dashboard {
    opacity: 0.58;
    inset-inline-start: 28%;
    transform: scale(0.86);
    transform-origin: right center;
  }

  .login-card {
    inline-size: min(100%, 520px);
  }
}

@media (max-width: 959px) {
  .cinema-auth__shade {
    background:
      radial-gradient(circle at 80% 18%, rgba(134, 255, 221, 0.28), transparent 34%),
      linear-gradient(180deg, rgba(2, 6, 23, 0.78), rgba(2, 6, 23, 0.86));
  }

  .login-stage {
    justify-content: center;
    padding: 1.2rem;
  }

  .login-card {
    border-radius: 26px;
  }

  .login-brand {
    margin-block-end: 2.4rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
</style>