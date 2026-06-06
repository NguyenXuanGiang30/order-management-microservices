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

const submitLogin = async () => {
  errorMessage.value = ''

  const username = form.value.username.trim()
  const password = form.value.password

  if (!username || !password) {
    errorMessage.value = 'Vui lòng nhập tên tài khoản và mật khẩu.'

    return
  }

  try {
    await authStore.login({
      username,
      password,
      remember: form.value.remember,
    })

    const redirectPath = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/')
      ? route.query.redirect
      : '/dashboard'

    await router.push(redirectPath)
  }
  catch (error) {
    errorMessage.value = error instanceof AuthApiError
      ? error.message
      : 'Không thể đăng nhập. Vui lòng kiểm tra kết nối API và thử lại.'
  }
}
</script>

<template>
  <div class="retail-auth-page">
    <section class="auth-copy d-none d-lg-flex">
      <div class="d-flex align-center mb-10">
        <VAvatar
          color="primary"
          rounded="lg"
          size="42"
          class="me-3"
        >
          <VIcon icon="ri-store-2-line" />
        </VAvatar>
        <div>
          <div class="text-h6 font-weight-bold">
            RetailOps
          </div>
          <div class="text-body-2 text-medium-emphasis">
            Nền tảng quản lý bán lẻ
          </div>
        </div>
      </div>

      <VChip
        color="primary"
        variant="tonal"
        class="mb-4"
      >
        Không gian làm việc doanh nghiệp
      </VChip>
      <h1 class="auth-title mb-4">
        Điều hành bán lẻ và kho hàng trong một hệ thống.
      </h1>
      <p class="text-body-1 text-medium-emphasis mb-8">
        Đăng nhập để vào đúng workspace theo vai trò. Admin có sidebar đầy đủ, nhân viên bán hàng và thủ kho dùng màn hình thao tác riêng.
      </p>

      <div class="role-grid">
        <VCard
          v-for="role in roles"
          :key="role.title"
          class="role-card"
          :class="{ selected: selectedRole === role.title }"
          @click="selectedRole = role.title"
        >
          <VCardText>
            <VIcon
              :icon="role.icon"
              color="primary"
              size="24"
              class="mb-3"
            />
            <div class="font-weight-bold">
              {{ role.title }}
            </div>
            <div class="text-caption text-medium-emphasis">
              {{ role.subtitle }}
            </div>
          </VCardText>
        </VCard>
      </div>
    </section>

    <section class="auth-card-wrap">
      <VCard class="auth-card">
        <VCardItem>
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

        <VCardText>
          <h2 class="text-h4 font-weight-bold mb-2">
            Đăng nhập
          </h2>
          <p class="text-body-2 text-medium-emphasis mb-6">
            Sử dụng tài khoản được cấp để tiếp tục vào hệ thống.
          </p>

          <VForm @submit.prevent="submitLogin">
            <VAlert
              v-if="errorMessage"
              type="error"
              variant="tonal"
              density="comfortable"
              class="mb-4"
            >
              {{ errorMessage }}
            </VAlert>

            <VTextField
              v-model="form.username"
              label="Tên tài khoản"
              prepend-inner-icon="ri-user-line"
              autocomplete="username"
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
              <span class="text-primary text-body-2">Quên mật khẩu?</span>
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
.retail-auth-page {
  display: grid;
  min-block-size: 100dvh;
  align-items: center;
  background: rgb(var(--v-theme-background));
  gap: 5rem;
  grid-template-columns: minmax(0, 1fr) 430px;
  padding: 4rem clamp(1.5rem, 8vw, 9rem);
}

.auth-copy {
  flex-direction: column;
}

.auth-title {
  max-inline-size: 680px;
  font-size: clamp(2.6rem, 5vw, 4.5rem);
  font-weight: 800;
  letter-spacing: 0;
  line-height: 1.02;
}

.role-grid {
  display: grid;
  max-inline-size: 760px;
  gap: 1rem;
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.role-card {
  cursor: pointer;
  transition: border-color 160ms ease, transform 160ms ease;
}

.role-card.selected {
  border-color: rgb(var(--v-theme-primary));
}

.role-card:hover {
  transform: translateY(-2px);
}

.auth-card-wrap {
  inline-size: 100%;
}

.auth-card {
  padding: 1.25rem;
}

@media (max-width: 1279px) {
  .retail-auth-page {
    display: flex;
    justify-content: center;
    padding: 1.5rem;
  }

  .auth-card-wrap {
    max-inline-size: 460px;
  }
}
</style>
