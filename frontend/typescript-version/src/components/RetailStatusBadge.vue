<script setup lang="ts">
import { statusColor } from '@/data/retailOps'

const props = defineProps<{
  status: string
  dot?: boolean
  size?: 'x-small' | 'small' | 'default'
}>()

const chipColor = computed(() => statusColor(props.status))
</script>

<template>
  <VChip
    :color="chipColor"
    :size="size || 'small'"
    variant="tonal"
    class="font-weight-medium"
  >
    <span
      v-if="dot"
      class="retail-status-dot me-2"
      :style="{ backgroundColor: `rgb(var(--v-theme-${chipColor}))` }"
    />
    {{ status }}
  </VChip>
</template>
