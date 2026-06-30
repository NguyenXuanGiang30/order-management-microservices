<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { getApiBaseUrl } from '@/services/authApi'

interface ServiceInfo {
  status: string
  url: string
  error?: string
}

interface HealthCheckResponse {
  status: string
  services: Record<string, ServiceInfo>
}

const status = ref<'Healthy' | 'Unhealthy' | 'Checking'>('Checking')
const services = ref<Record<string, ServiceInfo>>({})
const lastChecked = ref<string>('')
const loading = ref(false)
const menuOpen = ref(false)

const checkHealth = async () => {
  if (loading.value) return
  loading.value = true
  try {
    const baseUrl = getApiBaseUrl()
    const res = await fetch(`${baseUrl}/health`)
    const data: HealthCheckResponse = await res.json()
    status.value = data.status as 'Healthy' | 'Unhealthy'
    services.value = data.services
    lastChecked.value = new Date().toLocaleTimeString('vi-VN')
  } catch (err) {
    console.error('Lỗi khi kiểm tra trạng thái dịch vụ:', err)
    status.value = 'Unhealthy'
    services.value = {
      ApiGateway: { status: 'Unhealthy', url: 'Self', error: 'Không thể kết nối đến API Gateway' }
    }
  } finally {
    loading.value = false
  }
}

let intervalId: any = null

onMounted(() => {
  checkHealth()
  intervalId = setInterval(checkHealth, 30000) // check every 30s
})

onUnmounted(() => {
  if (intervalId) clearInterval(intervalId)
})
</script>

<template>
  <VMenu
    v-model="menuOpen"
    :close-on-content-click="false"
    location="bottom end"
    transition="scale-transition"
  >
    <template #activator="{ props }">
      <VBtn
        icon
        variant="text"
        v-bind="props"
        class="me-2 position-relative"
        density="comfortable"
      >
        <VIcon
          icon="ri-server-line"
          :color="status === 'Healthy' ? 'success' : (status === 'Checking' ? 'warning' : 'error')"
        />
        <!-- Pulse Indicator -->
        <span
          class="status-pulse-dot"
          :class="status === 'Healthy' ? 'bg-success' : 'bg-error'"
        ></span>
      </VBtn>
    </template>

    <VCard width="320" class="elevation-10 rounded-lg">
      <VCardItem class="pb-2">
        <div class="d-flex justify-space-between align-center">
          <VCardTitle class="text-subtitle-1 font-weight-bold">Trạng thái hệ thống</VCardTitle>
          <VBtn
            icon="ri-refresh-line"
            variant="text"
            density="compact"
            :loading="loading"
            @click="checkHealth"
          />
        </div>
        <VCardSubtitle>Cập nhật lúc: {{ lastChecked || 'Vừa xong' }}</VCardSubtitle>
      </VCardItem>

      <VDivider />

      <VList density="compact">
        <VListItem
          v-for="(info, name) in services"
          :key="name"
          :title="name"
          :subtitle="info.url"
        >
          <template #prepend>
            <VIcon
              :icon="info.status === 'Healthy' ? 'ri-checkbox-circle-fill' : 'ri-close-circle-fill'"
              :color="info.status === 'Healthy' ? 'success' : 'error'"
              class="me-2"
            />
          </template>
          <template #append>
            <VChip
              size="x-small"
              :color="info.status === 'Healthy' ? 'success' : 'error'"
              variant="tonal"
            >
              {{ info.status }}
            </VChip>
          </template>
        </VListItem>
      </VList>
    </VCard>
  </VMenu>
</template>

<style scoped>
.status-pulse-dot {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  box-shadow: 0 0 0 0 rgba(76, 175, 80, 0.7);
  animation: pulse-animation 2s infinite;
}

@keyframes pulse-animation {
  0% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0.7);
  }
  70% {
    transform: scale(1);
    box-shadow: 0 0 0 5px rgba(76, 175, 80, 0);
  }
  100% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0);
  }
}
</style>
