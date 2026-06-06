<script setup lang="ts">
import type { MetricItem } from '@/data/retailOps'

defineProps<{
  metric: MetricItem
}>()
</script>

<template>
  <VCard class="retail-metric-card h-100">
    <VCardText>
      <div class="d-flex align-start justify-space-between mb-4">
        <div>
          <div class="text-caption text-medium-emphasis mb-1">
            {{ metric.label }}
          </div>
          <div class="text-h5 font-weight-bold">
            {{ metric.value }}
          </div>
        </div>
        <VAvatar
          :color="metric.color"
          variant="tonal"
          rounded="lg"
          size="42"
        >
          <VIcon
            :icon="metric.icon"
            size="22"
          />
        </VAvatar>
      </div>
      <div class="text-caption text-medium-emphasis">
        {{ metric.helper }}
      </div>
    </VCardText>
  </VCard>
</template>

<style scoped>
.retail-metric-card {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  box-shadow: none;
}
</style>
