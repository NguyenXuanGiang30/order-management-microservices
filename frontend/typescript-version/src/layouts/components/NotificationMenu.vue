<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

import {
  type NotificationDto,
  getNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  markNotificationRead,
} from '@/services/adminApi'

const notifications = ref<NotificationDto[]>([])
const unreadCount = ref(0)
const loading = ref(false)
const errorMessage = ref('')

let intervalId: any = null

const hasUnread = computed(() => unreadCount.value > 0)

const badgeContent = computed(() => {
  if (unreadCount.value > 99)
    return '99+'

  return String(unreadCount.value)
})

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error)
    return error.message || fallback

  return fallback
}

const fetchNotifications = async () => {
  const data = await getNotifications({
    pageSize: 5,
  })

  notifications.value = data
}

const fetchUnreadCount = async () => {
  unreadCount.value = await getUnreadNotificationCount()
}

const loadAllData = async () => {
  loading.value = true
  errorMessage.value = ''

  try {
    await Promise.all([
      fetchNotifications(),
      fetchUnreadCount(),
    ])
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Không thể tải thông báo hệ thống.',
    )
  }
  finally {
    loading.value = false
  }
}

const handleMarkRead = async (notification: NotificationDto) => {
  if (notification.isRead)
    return

  loading.value = true
  errorMessage.value = ''

  try {
    await markNotificationRead(notification.id)
    await loadAllData()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Không thể đánh dấu thông báo đã đọc.',
    )
  }
  finally {
    loading.value = false
  }
}

const handleMarkAllRead = async () => {
  if (!hasUnread.value)
    return

  loading.value = true
  errorMessage.value = ''

  try {
    await markAllNotificationsRead()
    await loadAllData()
  }
  catch (error) {
    errorMessage.value = getErrorMessage(
      error,
      'Không thể đánh dấu tất cả thông báo đã đọc.',
    )
  }
  finally {
    loading.value = false
  }
}

const getSeverityColor = (severity?: string | null) => {
  const value = severity?.toLowerCase()

  if (value === 'error')
    return 'error'

  if (value === 'warning')
    return 'warning'

  if (value === 'info')
    return 'info'

  return 'primary'
}

const getSeverityIcon = (severity?: string | null) => {
  const value = severity?.toLowerCase()

  if (value === 'error')
    return 'ri-error-warning-line'

  if (value === 'warning')
    return 'ri-alert-line'

  if (value === 'info')
    return 'ri-information-line'

  return 'ri-notification-3-line'
}

const formatDate = (dateStr?: string | null) => {
  if (!dateStr)
    return ''

  const date = new Date(dateStr)

  if (Number.isNaN(date.getTime()))
    return ''

  const time = date.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
  })

  const dateText = date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
  })

  return `${time} ${dateText}`
}

onMounted(() => {
  void loadAllData()

  intervalId = window.setInterval(() => {
    void loadAllData()
  }, 15000)
})

onUnmounted(() => {
  if (intervalId !== null) {
    window.clearInterval(intervalId)
    intervalId = null
  }
})
</script>

<template>
  <VMenu width="380" location="bottom end" offset="14" :close-on-content-click="false">
    <template #activator="{ props }">
      <IconBtn v-bind="props" class="navbar-icon-btn me-2 notification-trigger">
        <VBadge v-if="hasUnread" :content="badgeContent" color="error" max="99">
          <VIcon icon="ri-notification-line" />
        </VBadge>

        <VIcon v-else icon="ri-notification-line" />
      </IconBtn>
    </template>

    <VCard class="notification-card">
      <div class="notification-head">
        <div>
          <span>Trung tâm</span>
          <strong>Thông báo hệ thống</strong>
        </div>

        <VBtn v-if="hasUnread" variant="text" density="comfortable" color="primary" size="small" :loading="loading"
          @click="handleMarkAllRead">
          Đọc tất cả
        </VBtn>
      </div>

      <VDivider />

      <VAlert v-if="errorMessage" type="error" variant="tonal" density="compact" class="ma-3" closable
        @click:close="errorMessage = ''">
        {{ errorMessage }}
      </VAlert>

      <div class="notification-body">
        <div v-if="loading && !notifications.length" class="notification-loading">
          <VSkeletonLoader type="list-item-avatar-three-line" />
          <VSkeletonLoader type="list-item-avatar-three-line" />
          <VSkeletonLoader type="list-item-avatar-three-line" />
        </div>

        <VList v-else-if="notifications.length" class="notification-list">
          <VListItem v-for="item in notifications" :key="item.id" class="notification-item"
            :class="{ 'notification-item--unread': !item.isRead }" @click="handleMarkRead(item)">
            <template #prepend>
              <VAvatar :color="getSeverityColor(item.severity)" variant="tonal" size="38" class="me-3">
                <VIcon :icon="getSeverityIcon(item.severity)" size="19" />
              </VAvatar>
            </template>

            <div class="notification-content">
              <div class="notification-title-row">
                <strong>{{ item.title }}</strong>
                <span>{{ formatDate(item.createdAt) }}</span>
              </div>

              <p>
                {{ item.message }}
              </p>
            </div>
          </VListItem>
        </VList>

        <div v-else class="notification-empty">
          <VIcon icon="ri-notification-off-line" size="38" color="secondary" />

          <strong>Không có thông báo nào</strong>

          <span>
            Các cảnh báo hệ thống mới sẽ xuất hiện tại đây.
          </span>
        </div>
      </div>

      <VDivider />

      <div class="notification-footer">
        <VBtn block variant="tonal" color="secondary" size="small" to="/settings">
          Xem tất cả trong Cài đặt
        </VBtn>
      </div>
    </VCard>
  </VMenu>
</template>

<style scoped>
.notification-trigger {
  border-radius: 14px;
}

.notification-card {
  border: 1px solid rgba(var(--v-border-color), 0.14);
  border-radius: 22px !important;
  background:
    linear-gradient(145deg,
      rgba(var(--v-theme-surface), 0.98),
      rgba(var(--v-theme-surface), 0.92));
  box-shadow:
    0 22px 60px rgba(15, 23, 42, 0.16),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  overflow: hidden;
}

.notification-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.95rem 1rem;
}

.notification-head span {
  display: block;
  color: rgba(var(--v-theme-on-surface), 0.56);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.notification-head strong {
  display: block;
  margin-block-start: 0.2rem;
  color: rgb(var(--v-theme-on-surface));
  font-size: 1rem;
  font-weight: 950;
}

.notification-body {
  max-block-size: 390px;
  overflow-y: auto;
}

.notification-loading {
  display: grid;
  gap: 0.2rem;
  padding: 0.4rem;
}

.notification-list {
  padding: 0;
}

.notification-item {
  min-block-size: 86px;
  border-block-end: 1px solid rgba(var(--v-border-color), 0.08);
  cursor: pointer;
  transition:
    background 160ms ease,
    transform 160ms ease;
}

.notification-item:hover {
  background: rgba(var(--v-theme-primary), 0.045);
}

.notification-item--unread {
  background:
    linear-gradient(90deg,
      rgba(var(--v-theme-primary), 0.08),
      transparent 70%);
}

.notification-item--unread::before {
  content: '';
  position: absolute;
  inset-block: 14px;
  inset-inline-start: 0;
  inline-size: 3px;
  border-radius: 999px;
  background: rgb(var(--v-theme-primary));
}

.notification-content {
  min-inline-size: 0;
}

.notification-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-block-end: 0.25rem;
}

.notification-title-row strong {
  overflow: hidden;
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.88rem;
  font-weight: 900;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notification-title-row span {
  flex: 0 0 auto;
  color: rgba(var(--v-theme-on-surface), 0.52);
  font-size: 0.72rem;
  font-weight: 700;
}

.notification-content p {
  display: -webkit-box;
  margin: 0;
  overflow: hidden;
  color: rgba(var(--v-theme-on-surface), 0.58);
  font-size: 0.8rem;
  line-height: 1.45;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.notification-empty {
  display: grid;
  place-items: center;
  gap: 0.45rem;
  min-block-size: 220px;
  padding: 2rem;
  color: rgba(var(--v-theme-on-surface), 0.58);
  text-align: center;
}

.notification-empty strong {
  color: rgb(var(--v-theme-on-surface));
  font-size: 0.95rem;
  font-weight: 900;
}

.notification-empty span {
  font-size: 0.82rem;
}

.notification-footer {
  padding: 0.75rem;
}

.notification-footer .v-btn {
  border-radius: 13px;
  font-weight: 850;
}
</style>