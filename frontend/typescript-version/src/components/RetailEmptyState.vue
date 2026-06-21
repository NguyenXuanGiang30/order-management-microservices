<script setup lang="ts">
defineProps<{
  icon?: string
  title?: string
  subtitle?: string
  actionLabel?: string
  actionTo?: string
  colspan?: number
}>()
</script>

<template>
  <tr>
    <td
      :colspan="colspan || 1"
      class="text-center"
    >
      <div class="retail-empty-state">
        <VIcon
          :icon="icon || 'ri-inbox-2-line'"
          size="52"
          class="retail-empty-state__icon"
        />
        <div class="retail-empty-state__title">
          {{ title || 'Chưa có dữ liệu' }}
        </div>
        <div
          v-if="subtitle"
          class="retail-empty-state__subtitle"
        >
          {{ subtitle }}
        </div>
        <VBtn
          v-if="actionLabel"
          color="primary"
          variant="tonal"
          size="small"
          class="mt-2"
          :to="actionTo"
        >
          {{ actionLabel }}
        </VBtn>
      </div>
    </td>
  </tr>
</template>
