<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'

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

const selectedRole = ref('Admin')
const isPasswordVisible = ref(false)
const errorMessage = ref('')

const roles = [
  { title: 'Admin', subtitle: 'Quản trị toàn hệ thống', icon: 'ri-shield-user-line' },
  { title: 'Sales', subtitle: 'POS, đơn hàng, công nợ KH', icon: 'ri-shopping-cart-2-line' },
  { title: 'Warehouse', subtitle: 'Tồn kho, nhập hàng, NCC', icon: 'ri-archive-line' },
]

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
    const session = await authStore.login({ username, password, remember: form.value.remember })

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
</script>

<template>
  <div class="retail-auth-page">
    <!-- ── LEFT BRAND PANEL ─────────────────────────────── -->
    <section class="auth-copy d-none d-lg-flex">
      <!-- decorative background elements -->
      <div class="auth-deco auth-deco--tl" />
      <div class="auth-deco auth-deco--br" />

      <div class="auth-copy__content">
        <!-- Logo -->
        <div class="d-flex align-center mb-10">
          <VAvatar
            color="white"
            rounded="lg"
            size="44"
            class="me-3"
          >
            <VIcon
              icon="ri-store-2-line"
              color="primary"
            />
          </VAvatar>
          <div>
            <div class="text-h6 font-weight-bold text-white">
              RetailOps
            </div>
            <div class="text-body-2 auth-muted">
              Nền tảng quản lý bán lẻ
            </div>
          </div>
        </div>

        <!-- Badge -->
        <div class="auth-badge mb-5">
          <VIcon
            icon="ri-building-4-line"
            size="13"
            class="me-1"
          />
          Không gian làm việc doanh nghiệp
        </div>

        <!-- Headline -->
        <h1 class="auth-title mb-5 text-white">
          Điều hành bán lẻ và kho hàng trong một hệ thống.
        </h1>

        <p class="text-body-1 auth-muted mb-10">
          Đăng nhập để vào đúng workspace theo vai trò. Admin có sidebar đầy đủ, nhân viên bán hàng và thủ kho dùng màn hình thao tác riêng.
        </p>

        <!-- Role cards -->
        <div class="role-grid">
          <div
            v-for="role in roles"
            :key="role.title"
            class="role-card"
            :class="{ 'role-card--selected': selectedRole === role.title }"
            @click="selectedRole = role.title"
          >
            <VIcon
              :icon="role.icon"
              size="22"
              class="mb-3 role-card__icon"
            />
            <div class="text-white text-body-2 font-weight-bold">
              {{ role.title }}
            </div>
            <div class="text-caption role-card__sub">
              {{ role.subtitle }}
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ── RIGHT FORM PANEL ──────────────────────────────── -->
    <section class="auth-form-panel">
      <VCard class="auth-card">
        <VCardItem class="pb-1">
          <template #prepend>
            <VAvatar
              color="primary"
              rounded="lg"
            >
              <VIcon icon="ri-store-2-line" />
            </VAvatar>
          </template>
          <VCardTitle>RetailOps</VCardTitle>
          <VCardSubtitle>Quản lý bán lẻ và kho hàng</VCardSubtitle>
        </VCardItem>

        <VDivider class="mt-3 mb-1 mx-5" />

        <VCardText class="pt-5">
          <h2 class="text-h4 font-weight-bold mb-1">
            Đăng nhập
          </h2>
          <p class="text-body-2 text-medium-emphasis mb-6">
            Sử dụng tài khoản được cấp để tiếp tục vào hệ thống.
          </p>

          <VForm @submit.prevent="submitLogin">
            <Transition name="error-slide">
              <VAlert
                v-if="errorMessage"
                type="error"
                variant="tonal"
                density="comfortable"
                class="mb-4"
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
              :disabled="authStore.loading"
              class="mb-4"
            />

            <VTextField
              v-model="form.password"
              label="Mật khẩu"
              :type="isPasswordVisible ? 'text' : 'password'"
              autocomplete="current-password"
              prepend-inner-icon="ri-lock-line"
              :append-inner-icon="isPasswordVisible ? 'ri-eye-off-line' : 'ri-eye-line'"
              variant="outlined"
              :disabled="authStore.loading"
              class="mb-4"
              @click:append-inner="isPasswordVisible = !isPasswordVisible"
            />

            <div class="d-flex align-center justify-space-between flex-wrap mb-6">
              <VCheckbox
                v-model="form.remember"
                label="Ghi nhớ thiết bị"
                density="compact"
                hide-details
                :disabled="authStore.loading"
              />
              <a class="text-primary text-body-2 text-decoration-none cursor-pointer">Quên mật khẩu?</a>
            </div>

            <VBtn
              block
              type="submit"
              size="large"
              :loading="authStore.loading"
              :disabled="authStore.loading"
              append-icon="ri-arrow-right-line"
            >
              Đăng nhập hệ thống
            </VBtn>
          </VForm>
        </VCardText>
      </VCard>
    </section>
  </div>
</template>

<style scoped>
/* ── Page layout ─────────────────────────────────────── */
.retail-auth-page {
  display: grid;
  min-block-size: 100dvh;
  grid-template-columns: minmax(0, 1fr) 480px;
  overflow: hidden;
}

/* ── Left brand panel ────────────────────────────────── */
.auth-copy {
  position: relative;
  flex-direction: column;
  background: rgb(var(--v-theme-primary));
  overflow: hidden;
}

/* Dark gradient overlay for depth */
.auth-copy::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(145deg, rgba(0, 0, 0, 0.32) 0%, rgba(0, 0, 0, 0.04) 100%);
  pointer-events: none;
  z-index: 0;
}

/* Decorative translucent circles */
.auth-deco {
  position: absolute;
  border-radius: 50%;
  pointer-events: none;
  z-index: 0;
}

.auth-deco--tl {
  inline-size: 480px;
  block-size: 480px;
  inset-block-start: -160px;
  inset-inline-end: -100px;
  background: rgba(255, 255, 255, 0.07);
}

.auth-deco--br {
  inline-size: 360px;
  block-size: 360px;
  inset-block-end: -120px;
  inset-inline-start: -80px;
  background: rgba(255, 255, 255, 0.04);
}

/* Content sits above decorative layers */
.auth-copy__content {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  block-size: 100%;
  padding: 4rem clamp(2.5rem, 6vw, 7rem);
}

.auth-muted {
  color: rgba(255, 255, 255, 0.62);
}

/* Pill badge */
.auth-badge {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  padding: 4px 14px;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 500;
  color: rgba(255, 255, 255, 0.85);
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.22);
}

/* Headline */
.auth-title {
  max-inline-size: 580px;
  font-size: clamp(2rem, 3.8vw, 3.4rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.06;
}

/* ── Role cards ──────────────────────────────────────── */
.role-grid {
  display: grid;
  max-inline-size: 680px;
  gap: 12px;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.role-card {
  cursor: pointer;
  padding: 16px;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.14);
  background: rgba(255, 255, 255, 0.07);
  transition: border-color 180ms ease, background 180ms ease, transform 180ms ease;
  user-select: none;
}

.role-card__icon {
  color: rgba(255, 255, 255, 0.7);
  display: block;
}

.role-card__sub {
  color: rgba(255, 255, 255, 0.5);
  margin-block-start: 2px;
}

.role-card:hover {
  background: rgba(255, 255, 255, 0.13);
  transform: translateY(-2px);
}

.role-card--selected {
  background: rgba(255, 255, 255, 0.18) !important;
  border-color: rgba(255, 255, 255, 0.6) !important;
}

.role-card--selected .role-card__icon {
  color: #fff;
}

/* ── Right form panel ────────────────────────────────── */
.auth-form-panel {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 2.5rem;
  background: rgb(var(--v-theme-background));
}

.auth-card {
  inline-size: 100%;
  max-inline-size: 420px;
  padding: 0.75rem;
}

/* ── Keyframes ───────────────────────────────────────── */
@keyframes fadeSlideIn {
  from {
    opacity: 0;
    transform: translateX(-22px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

@keyframes fadeSlideUp {
  from {
    opacity: 0;
    transform: translateY(24px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes blobDrift {
  0%, 100% { transform: translate(0, 0) scale(1); }
  33%       { transform: translate(18px, -22px) scale(1.07); }
  66%       { transform: translate(-12px, 12px) scale(0.96); }
}

@keyframes ringPulse {
  0%   { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.45); }
  70%  { box-shadow: 0 0 0 10px rgba(255, 255, 255, 0); }
  100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
}

/* ── Entrance: left panel stagger ────────────────────── */
.auth-copy__content > * {
  animation: fadeSlideIn 0.6s cubic-bezier(0.22, 1, 0.36, 1) both;
}

.auth-copy__content > *:nth-child(1) { animation-delay: 0.05s; }
.auth-copy__content > *:nth-child(2) { animation-delay: 0.16s; }
.auth-copy__content > *:nth-child(3) { animation-delay: 0.26s; }
.auth-copy__content > *:nth-child(4) { animation-delay: 0.36s; }
.auth-copy__content > *:nth-child(5) { animation-delay: 0.46s; }

/* ── Entrance: form card ─────────────────────────────── */
.auth-card {
  animation: fadeSlideUp 0.65s cubic-bezier(0.22, 1, 0.36, 1) 0.1s both;
}

/* ── Decorative blobs drift ──────────────────────────── */
.auth-deco--tl {
  animation: blobDrift 20s ease-in-out infinite;
}

.auth-deco--br {
  animation: blobDrift 28s ease-in-out infinite reverse;
}

/* ── Role card: ring pulse on select ─────────────────── */
.role-card--selected {
  animation: ringPulse 0.5s ease;
}

/* ── Error alert transition ──────────────────────────── */
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

/* ── Respect prefers-reduced-motion ──────────────────── */
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}

/* ── Mobile breakpoint ───────────────────────────────── */
@media (max-width: 1279px) {
  .retail-auth-page {
    display: flex;
    align-items: center;
    justify-content: center;
    min-block-size: 100dvh;
    background: rgb(var(--v-theme-background));
    padding: 1.5rem;
  }

  .auth-form-panel {
    padding: 0;
    block-size: auto;
  }

  .auth-card {
    max-inline-size: 460px;
  }
}
</style>
